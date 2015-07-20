using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace CommonRDF
{
    internal static class FilterFunctions
    {
        internal static void AndOrExpression(this SparqlChainParametred sparqlChain, string s, bool isOptionals, bool isNot = false)
        {
            Match m;
            if ((m = Reg.ManyNotAllInBrackets.Match(s)).Success)
                AndOrExpression(sparqlChain, m.Groups["insideOneNot"].Value, isOptionals, !isNot);
            if ((m = Reg.InsideBrackets.Match(s)).Success)
                AndOrExpression(sparqlChain, m.Groups["inside"].Value, isOptionals, !isNot);
            if ((m = Reg.AndOr.Match(s)).Success)
                {
                    var left = m.Groups["insideLeft"].Value;
                    if (left == string.Empty)
                        left = m.Groups["left"].Value;
                    var right = m.Groups["insideRight"].Value;
                    if (right == string.Empty)
                        right = m.Groups["right"].Value;

                    if ((!isNot) && (m.Groups["center"].Value == "||") || (isNot && (m.Groups["center"].Value == "&&")))
                        sparqlChain.Add(new FilterOr(left, right, sparqlChain, isNot, isOptionals));
                    else //if ((m.Groups["center"].Value == "&&") || (isNot && (m.Groups["center"].Value == "||"))) //&&
                    {
                        AndOrExpression(sparqlChain, left, isOptionals, isNot);
                        AndOrExpression(sparqlChain, right, isOptionals, isNot);
                    }

                }
            else
            {
                //todo must be if and recursive?
                while ((m = Reg.ManyNotAtom.Match(s)).Success)
                {
                    isNot = !isNot;
                    s = m.Groups["insideOneNot"].Value;
                }
                AtomPredicate(sparqlChain, s, isNot, isOptionals);
            }
        }

        private static void AtomPredicate(SparqlChainParametred sparqlChain, string s, bool isNot, bool isOptional)
        {
            var m = Reg.RegSameTerm.Match(s);
            var localParameters = new List<FilterParameterInfo>();
            bool isArithmetic = false;
            if (m.Success)
            {
                if (isOptional) throw new Exception("optional filter just for math expressions");
                if (isNot)
                {
                    var left = GetParameterOrStringOrMath(sparqlChain, m.Groups[1].Value.Trim(), localParameters, ref isArithmetic);
                    var right = GetParameterOrStringOrMath(sparqlChain, m.Groups[3].Value.Trim(), localParameters, ref isArithmetic);
                    if (isArithmetic) throw new Exception("same term is not for math");
                    CreateSelectsNewParameters(new FilterTest(Expression.NotEqual(left, right), localParameters),
                        localParameters, sparqlChain);
                }
                else
                    EqualOrAssign(sparqlChain, m.Groups[1].Value, m.Groups[2].Value);}
            else if ((m = Reg.Bound.Match(s)).Success)
            {
                if (isOptional) throw new Exception("optional filter just for math expressions");
                var parameter = GetParameterOrCreate(sparqlChain, m.Groups[1].Value, localParameters);
                var getValueFromParameter = Expression.Field(parameter.Parameter, typeof (TValue).GetField("Value"));
                if (!parameter.IsAssigned) throw new ArgumentNullException(m.Groups[1].Value);
                Expression equalExpression = 
                    Expression.NotEqual(
                        getValueFromParameter, Expression.Constant(string.Empty));
                sparqlChain.Add(
                    new FilterTest(isNot ? Expression.Not(equalExpression) : equalExpression, localParameters));
            }
            else if ((m = Reg.LangMatches.Match(s)).Success)
            {
                if(isOptional) throw new Exception("optional filter just for math expressions");
                var langValue = m.Groups[1].Value.Trim();
                if (m.Groups[2].Value == "*" || m.Groups[2].Value == "\"*\"")
                {
                    Expression firstExpression = null;
                    if (!TestLang(sparqlChain, langValue, localParameters, ref firstExpression))
                        if (langValue.StartsWith("?"))
                        {
                            FilterParameterInfo firstParameterInfo = GetParameterOrCreate(sparqlChain, langValue, localParameters);
                            firstExpression = Expression.Field(firstParameterInfo.Parameter,typeof(TValue).GetField("Value"));
                        }
                        else throw new Exception("lang match with unknown left side");
                    sparqlChain.Add(new FilterTest(Expression.MakeBinary(
                        isNot? ExpressionType.Equal : ExpressionType.NotEqual,
                        firstExpression, Expression.Constant(string.Empty)),
                        localParameters));
                }
                else if (isNot)
                {
                    var left = GetParameterOrStringOrMath(sparqlChain, m.Groups[1].Value.Trim(), localParameters, ref isArithmetic);
                    var right = GetParameterOrStringOrMath(sparqlChain, m.Groups[3].Value.Trim(), localParameters, ref isArithmetic);
                    if (isArithmetic) throw new Exception("lang match is not for math");
                    CreateSelectsNewParameters(new FilterTest(Expression.NotEqual(left, right), localParameters),
                        localParameters, sparqlChain);
                }
                else
                    EqualOrAssign(sparqlChain, m.Groups[1].Value, m.Groups[2].Value);
            }
            else if ((m = Reg.Equality.Match(s)).Success)
            {
                var equalityType = m.Groups[2].Value;
                if (equalityType == "=" && !isNot)
                {
                    EqualOrAssign(sparqlChain, m.Groups[1].Value, m.Groups[3].Value, isOptional);
                    return;
                }
                Expression expression = Expression.MakeBinary(equalityType.Length > 1
                    ? (equalityType.StartsWith("!")
                        ? ExpressionType.NotEqual
                        : equalityType.StartsWith("<")
                            ? ExpressionType.LessThanOrEqual
                            : ExpressionType.GreaterThanOrEqual)
                    : (equalityType == ">"
                        ? ExpressionType.GreaterThan
                        : equalityType == "<"
                            ? ExpressionType.LessThan
                            : ExpressionType.Equal),
                  sparqlChain.GetDoubleArithmeticOrConst(m.Groups[1].Value, localParameters),
                    sparqlChain.GetDoubleArithmeticOrConst(m.Groups[3].Value, localParameters));
                CreateSelectsNewParameters(isOptional 
                    ? (FilterTest) 
                  new FilterTestDoublesOptional(isNot ? Expression.Not(expression) : expression
                        , localParameters)
                : new FilterTestDoubles(
                    isNot ? Expression.Not(expression) : expression
                    , localParameters), localParameters, sparqlChain);
            }
            else
                //TODO boolean valiable
                throw new NotImplementedException();
        }

        private static Expression GetParameterOrStringOrMath(this SparqlChainParametred scp, string leftString, List<FilterParameterInfo> localParameters,
            ref bool isArithmetic)
        {
            var left = leftString.StartsWith("?")
                ? Expression.Field(scp.GetParameterOrCreate(leftString, localParameters).Parameter,
                    typeof (TValue).GetField("Value"))
                : scp.GetStringOrArithmetic(leftString, localParameters, ref isArithmetic);
            return left;
        }

        public static void CreateSelectsNewParameters(FilterTest test,
            List<FilterParameterInfo> parameters, SparqlChain sparqlChain)
        {
            sparqlChain.Add(parameters
                .Where(p => !p.IsAssigned)
                .Select(p => new SelectAllSubjects(p.Value) as SparqlBase).ToArray());
            //todo replase all subjects by all subj and data
            sparqlChain.Add(test);
        }

        private static Expression GetStringOrArithmetic(this SparqlChainParametred scp, string s, List<FilterParameterInfo> localParameters, ref bool isArithmetic)
        {
            s = s.Trim();
            //todo concatenation
            bool isData = true;
          s =  scp.TestDataConst(s, ref isData);
            if (isData || s.StartsWith("http")) return Expression.Constant(s);
            
            Expression expression = null;
            if (scp.TestDoubleArithmeticExpression(s, localParameters, ref expression))
            {
                isArithmetic = true;
                return expression;
            }

            if (scp.TestLang(s, localParameters, ref expression))
            {
                return expression;
            }
            DateTime dt;
            double i;
            if (Double.TryParse(s, out i))
            {
                isArithmetic = true;
                return Expression.Constant(i);
            }
            if (DateTime.TryParse(s, out dt))
            {
                isArithmetic = true;
                return Expression.Constant((double) dt.ToUniversalTime().Ticks, typeof (double));
            }
            return Expression.Constant(scp.ReplaceNamespacePrefix(s));
        }

        private static bool TestLang(this SparqlChainParametred scp, string s,
            List<FilterParameterInfo> localParameters, ref Expression field)
        {
            Match langMatch = Reg.Lang.Match(s);
            if (langMatch.Success)
            {
                var langParameterName = langMatch.Groups[1].Value.Trim();
                if (!langParameterName.StartsWith("?")) throw new Exception("lang must contains parameter");
                var langParameter = scp.GetParameterOrCreate(langParameterName, localParameters);
                if (!langParameter.IsAssigned) throw new Exception("lang must contains parameter with value");
                {
                    field = Expression.Field(langParameter.Parameter, typeof (TValue).GetField("Lang"));
                    return true;
                }
            }
            return false;
        }

        private static Expression GetDoubleArithmeticOrConst(this SparqlChainParametred scp, string s, List<FilterParameterInfo> parameters)
        {
            //Expressions
            Expression expression = null;
            if (scp.TestDoubleArithmeticExpression(s, parameters, ref expression))
                return expression;
            s = s.Trim();
            //variable
            if (s.StartsWith("?"))
                return Expression.Field(scp.GetParameterOrCreate(s, parameters).Parameter,
                    typeof (TValue).GetField("DoubleValue"));
            //const
            double i;
            DateTime dt;
            if (Double.TryParse(s, out i))
                return Expression.Constant(i);
            if (DateTime.TryParse(s, out dt))
                return Expression.Constant((double) dt.ToUniversalTime().Ticks, typeof (double));
            throw new Exception("undefined arithmetic: " + s);
        }

        private static bool TestDoubleArithmeticExpression(this SparqlChainParametred scp, string s, List<FilterParameterInfo> parameters, ref Expression expression)
        {
            Match m;
            if ((m = Reg.USubtrAllInBrackets.Match(s)).Success)
            {
                s = m.Groups["inside"].Value;
                expression = Expression.Subtract(Expression.Constant(0.0),
                    scp.GetDoubleArithmeticOrConst(s, parameters));
                return true;
            }
            if ((m = Reg.SumSubtract.Match(s)).Success)
            {
                expression = m.Groups["center"].Value == "+"
                    ? Expression.Add(scp.GetDoubleArithmeticOrConst(m.Groups["left"].Value, parameters),
                        scp.GetDoubleArithmeticOrConst(m.Groups["right"].Value, parameters))
                    : Expression.Subtract(scp.GetDoubleArithmeticOrConst(m.Groups["left"].Value, parameters),
                        scp.GetDoubleArithmeticOrConst(m.Groups["right"].Value, parameters));
                return true;
            }
            if ((m = Reg.MulDiv.Match(s)).Success)
            {
                expression = m.Groups["center"].Value == "*"
                    ? Expression.Multiply(scp.GetDoubleArithmeticOrConst(m.Groups["left"].Value, parameters),
                        scp.GetDoubleArithmeticOrConst(m.Groups["right"].Value, parameters))
                    : Expression.Divide(scp.GetDoubleArithmeticOrConst(m.Groups["left"].Value, parameters),
                        scp.GetDoubleArithmeticOrConst(m.Groups["right"].Value, parameters));
                return true;
            }
            if ((m = Reg.USubtrAtom.Match(s)).Success)
            {
                expression = Expression.Subtract(Expression.Constant(0.0),
                    scp.GetDoubleArithmeticOrConst(m.Groups["inside"].Value, parameters));
                return true;
            }
            return false;
        }

        private static FilterParameterInfo GetParameterOrCreate(this SparqlChainParametred scp, string name, IList<FilterParameterInfo> localParameters)
        {
            var existing = localParameters.FirstOrDefault(p => p.Parameter.Name == name);
            if (existing == null)
            {
                existing = new FilterParameterInfo
                {
                    Parameter = Expression.Parameter(typeof (TValue), name)
                };
                if (!(existing.IsAssigned = scp.valuesByName.TryGetValue(name, out existing.Value)))
                    scp.valuesByName.Add(name, existing.Value = new TValue());

                if (existing.IsAssigned && existing.Value.Value == "hasParellellValue")
                    existing.IsAssigned = false;
                localParameters.Add(existing);
            }
            return existing;
        }

        private static void EqualOrAssign(this SparqlChainParametred sparqlChain, string left, string right, bool isOptional=false)
        {
            bool isLeftParameter = left.StartsWith("?"), isRightParameter = right.StartsWith("?");
            var localParameters = new List<FilterParameterInfo>();
            if (!isLeftParameter && !isRightParameter)
            {
                bool isArithmetic = false;
                var leftExpr = sparqlChain.GetStringOrArithmetic(left, localParameters, ref isArithmetic);
                Expression rightExpr;
                if (isArithmetic)
                    rightExpr = sparqlChain.GetDoubleArithmeticOrConst(right, localParameters);
                else
                {
                    bool isArithmeticSecond = false;
                    rightExpr = sparqlChain.GetStringOrArithmetic(right, localParameters, ref isArithmeticSecond);
                    if (isArithmeticSecond)
                        leftExpr = sparqlChain.GetDoubleArithmeticOrConst(left, localParameters);
                    isArithmetic = isArithmeticSecond;
                }
                if (!isArithmetic && isOptional) throw new Exception("optional filter just for math expressions");
                CreateSelectsNewParameters(isArithmetic
                    ? isOptional ? (FilterTest) new FilterTestDoublesOptional(Expression.Equal(leftExpr, rightExpr), localParameters)
                                 : new FilterTestDoubles(Expression.Equal(leftExpr, rightExpr), localParameters)
                    : new FilterTest(Expression.Equal(leftExpr, rightExpr), localParameters),
                    localParameters, sparqlChain);
                return;
            }
            if (!isLeftParameter) // right parameter
            {
                EqualOrAssignWithOneSideParameter(sparqlChain, left, right, localParameters, isOptional);
                return;
            }

            if (!isRightParameter) //left parameter
            {
                EqualOrAssignWithOneSideParameter(sparqlChain, right, left, localParameters, isOptional);
                return;
            }
            //теперь знаем, то слева параметер, можем его создать/использовать
            var paramLeftInfo = sparqlChain.GetParameterOrCreate(left, localParameters);
            //оба параметра
            var paramRightInfo = sparqlChain.GetParameterOrCreate(right, localParameters);
            if (paramLeftInfo.IsAssigned)
                if (paramRightInfo.IsAssigned) // todo оба параметра тут известны - как сравнивать?
                {
                    CreateSelectsNewParameters(new FilterTest(
                        Expression.Equal(
                            Expression.Field(paramLeftInfo.Parameter, typeof (TValue).GetField("Value")),
                            Expression.Field(paramRightInfo.Parameter, typeof (TValue).GetField("Value"))),
                        localParameters), localParameters, sparqlChain);
                    return;
                }
                else
                {
                    // правый параметер неизвестный
                    paramRightInfo.IsAssigned = true;
                    sparqlChain.Add(new FilterAssign(paramRightInfo.Value, paramLeftInfo.Value));
                    return;
                }

            if (!paramRightInfo.IsAssigned) //оба не известны - сравниваем
            {
                CreateSelectsNewParameters(
                    new FilterTest(
                        Expression.Equal(
                            Expression.Field(paramLeftInfo.Parameter, typeof (TValue).GetField("Value")),
                            Expression.Field(paramRightInfo.Parameter, typeof (TValue).GetField("Value"))),
                        localParameters), localParameters, sparqlChain);
                return;
            }
            // левый параметер неизвестный
            paramLeftInfo.IsAssigned = true;
            sparqlChain.Add(new FilterAssign(paramLeftInfo.Value, paramRightInfo.Value));
        }

        private static void EqualOrAssignWithOneSideParameter(this SparqlChainParametred sparqlChain, string unparameterSide, string paramOneSide, List<FilterParameterInfo> localParameters, bool isOptional)
        {
            var leftParameters = new List<FilterParameterInfo>();
            bool isArithmetic = false;
            var leftExpression = sparqlChain.GetStringOrArithmetic(unparameterSide, leftParameters, ref isArithmetic);
            FilterParameterInfo paramOneSideInfo = sparqlChain.GetParameterOrCreate(paramOneSide, localParameters);
            if (paramOneSideInfo.IsAssigned)
            {
                if(!isArithmetic && isOptional) throw new Exception("optional filter just for math expressions");
                CreateSelectsNewParameters(isArithmetic
                    ? isOptional ? (FilterTest) new FilterTestDoublesOptional(Expression.Equal(leftExpression,
                        Expression.Field(paramOneSideInfo.Parameter, typeof(TValue).GetField("DoubleValue"))),
                        localParameters) 
                    : new FilterTestDoubles(Expression.Equal(leftExpression,
                        Expression.Field(paramOneSideInfo.Parameter, typeof (TValue).GetField("DoubleValue"))),
                        localParameters)
                    : new FilterTest(Expression.Equal(leftExpression,
                        Expression.Field(paramOneSideInfo.Parameter, typeof (TValue).GetField("Value"))),
                        localParameters), leftParameters, sparqlChain);
                return;
            }
            paramOneSideInfo.IsAssigned = true;
            if (leftParameters.Count == 0) //knoun is const
            {
                sparqlChain.Add(new FilterAssign(paramOneSideInfo.Value,
                    new TValue
                    {
                        Value =
                            (Expression.Lambda<Func<dynamic>>(leftExpression, new ParameterExpression[] {})).Compile()()
                    }));
                return;
            }
            if (leftParameters.Contains(paramOneSideInfo)) //не исключаем и  ?newp=?newp
            {
                //TODO always true/false
                CreateSelectsNewParameters(new FilterTest(Expression.Equal(leftExpression, paramOneSideInfo.Parameter),
                    localParameters = localParameters.Concat(leftParameters).Distinct().ToList()), localParameters,
                    sparqlChain);
                return;
            }
            CreateSelectsNewParameters(
                new FilterAssignCalculated(paramOneSideInfo.Value, leftExpression, leftParameters), leftParameters,
                sparqlChain);
        }
    }
}