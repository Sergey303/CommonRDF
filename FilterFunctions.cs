using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace CommonRDF
{
    internal partial class SparqlChainParametred
    {
        internal IEnumerable<SparqlNodeBase> AndOrExpression(SparqlNodeBase root, string s, bool isOptionals, bool isNot)
        {
            Match m;
            if (s.StartsWith("!") && (m = Reg.ManyNotAllInBrackets.Match(s)).Success)
                return AndOrExpression(root, m.Groups["insideOneNot"].Value, isOptionals, !isNot);
            if (s.StartsWith("("))
            {
                s = s.TrimEnd();
                if (s.EndsWith(")") && (m = Reg.InsideBrackets.Match(s)).Success)
                    return AndOrExpression(root, m.Groups["inside"].Value, isOptionals, !isNot);
            }
            //if ((m = Reg.AndOr.Match(s)).Success)

            var leftAndOr = Reg.AndOr.Match(s);
            if (leftAndOr.Success)
            {
                var left = leftAndOr.Groups["s"].Value;
                var right = s.Remove(0, leftAndOr.Length);
                var center = leftAndOr.Groups["op"].Value;
                if ((!isNot) && (center.StartsWith("||")) || (isNot && (center.StartsWith("&&"))))
                {

                    var aLternativesChains = new ALternativesChains(left,
                       (insideString, startInsideNode) => AndOrExpression(startInsideNode, insideString, isOptionals, isNot), valuesByName, right);
                    return aLternativesChains.Ends;
                }
                //else //if ((m.Groups["center"].Value == "&&") || (isNot && (m.Groups["center"].Value == "||"))) //&&
                return
                    AndOrExpression(root, left, isOptionals, isNot)
                        .SelectMany(node => AndOrExpression(node, right, isOptionals, isNot));
            }
            var next = AtomPredicate(s, isNot, isOptionals);
            root.NextMatch = next.Match;
            if (isOptionals && root is OptionalSparqlTripletBase)
            {
                (root as OptionalSparqlTripletBase).NextOptionalFailMatch =
                    (next as OptionalSparqlTripletBase).OptionalFailMatch;
            }
            return Enumerable.Repeat(next, 1);
        }

        public SparqlNodeBase AtomPredicate(string s, bool isNot, bool isOptional)
        {
            Match m;
            //todo must be if and recursive?
            if (s.StartsWith("!"))
                while ((m = Reg.ManyNotAtom.Match(s)).Success)
                {
                    isNot = !isNot;
                    s = m.Groups["insideOneNot"].Value;
                }
            m = Reg.RegSameTerm.Match(s);
            var localParameters = new List<FilterParameterInfo>();
            bool isArithmetic = false;
            if (m.Success)
            {
                if (isOptional) throw new Exception("optional filter just for math expressions");
                if (isNot)
                {
                    var left = GetParameterOrStringOrMath(m.Groups[1].Value.Trim(), localParameters, ref isArithmetic);
                    var right = GetParameterOrStringOrMath(m.Groups[3].Value.Trim(), localParameters, ref isArithmetic);
                    if (isArithmetic) throw new Exception("same term is not for math");
                    return CreateSelectsNewParameters(
                        new FilterTest(Expression.NotEqual(left, right), localParameters), localParameters);
                }
                else
                    return EqualOrAssign(m.Groups[1].Value, m.Groups[2].Value);
            }
            else if ((m = Reg.Bound.Match(s)).Success)
            {
                if (isOptional) throw new Exception("optional filter just for math expressions");
                var parameter = GetParameterOrCreate(m.Groups[1].Value, localParameters);
                var getValueFromParameter = Expression.Field(parameter.Parameter, typeof (SparqlVariable).GetField("Value"));
                if (!parameter.IsAssigned) throw new ArgumentNullException(m.Groups[1].Value);
                Expression equalExpression = Expression.NotEqual(getValueFromParameter,
                    Expression.Constant(string.Empty));
                return new FilterTest(isNot ? Expression.Not(equalExpression) : equalExpression, localParameters);
            }
            else if ((m = Reg.LangMatches.Match(s)).Success)
                return LangMatch(isNot, isOptional, m.Groups[1].Value.Trim(), m.Groups[2].Value.Trim());
            else if ((m = Reg.Equality.Match(s)).Success)
            {
                var equalityType = m.Groups[2].Value;
                if (equalityType == "=" && !isNot)
                {
                    return EqualOrAssign(m.Groups[1].Value, m.Groups[3].Value, isOptional);
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
                    GetDoubleArithmeticOrConst(m.Groups[1].Value, localParameters),
                    GetDoubleArithmeticOrConst(m.Groups[3].Value, localParameters));
                return CreateSelectsNewParameters(isOptional
                    ? 
                        (SparqlNodeBase)new FilterTestDoublesOptional(isNot ? Expression.Not(expression) : expression, localParameters)
                    : new FilterTestDoubles(isNot ? Expression.Not(expression) : expression, localParameters), localParameters);
            }
            else
                //TODO boolean valiable
                throw new NotImplementedException();
        }

        internal SparqlNodeBase LangMatch(bool isNot, bool isOptional, string first, string second)
        {
            var localParameters = new List<FilterParameterInfo>();
            bool isArithmetic = false;
            if (isOptional) throw new Exception("optional filter just for math expressions");
            if (second == "*" || second == "\"*\"")
            {
                Expression firstExpression = null;
                if (!TestLang(first, localParameters, ref firstExpression))
                    if (first.StartsWith("?"))
                    {
                        firstExpression = Expression.Field(GetParameterOrCreate(first, localParameters)
                            .Parameter, typeof (SparqlVariable).GetField("Value"));
                    }
                    else throw new Exception("lang match with unknown left side");
                return new FilterTest(Expression.MakeBinary(
                    isNot ? ExpressionType.Equal : ExpressionType.NotEqual,
                    firstExpression, Expression.Constant(string.Empty)),
                    localParameters);
            }
            else if (isNot)
            {
                var left = GetParameterOrStringOrMath(first, localParameters, ref isArithmetic);
                var right = GetParameterOrStringOrMath(second, localParameters, ref isArithmetic);
                if (isArithmetic) throw new Exception("lang match is not for math");
                return CreateSelectsNewParameters(new FilterTest(Expression.NotEqual(left, right), localParameters),
                    localParameters);
            }
            else
                return EqualOrAssign(first, second);
        }

        private Expression GetParameterOrStringOrMath(string leftString, List<FilterParameterInfo> localParameters,
            ref bool isArithmetic)
        {
            var left = leftString.StartsWith("?")
                ? Expression.Field(GetParameterOrCreate(leftString, localParameters).Parameter,
                    typeof (SparqlVariable).GetField("Value"))
                : GetStringOrArithmetic(leftString, localParameters, ref isArithmetic);
            return left;
        }

        public SparqlNodeBase CreateSelectsNewParameters(SparqlNodeBase test,
            List<FilterParameterInfo> parameters)
        {
            //todo replase all subjects by all subj and data
            var selectParamNodes= parameters
                .Where(p => !p.IsAssigned);
            var selectParameter = selectParamNodes.FirstOrDefault();
            if (selectParameter==null) return test;
            var selectNode = new SelectAllSubjects(selectParameter.Value);
            foreach (var selectParamNode in selectParamNodes.Skip(1).Select(p => new SelectAllSubjects(p.Value)))
            {
                selectNode.NextMatch = selectParamNode.Match;
                selectNode = selectParamNode;
            }

            return test;


        }

        private Expression GetStringOrArithmetic(string s, List<FilterParameterInfo> localParameters,
            ref bool isArithmetic)
        {
            s = s.Trim();
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
            //todo concatenation
            bool isData = true;
            s = TestDataConst(s, ref isData);
            if (isData || s.StartsWith("http")) return Expression.Constant(s);

            Expression expression = null;
            if (TestDoubleArithmeticExpression(s, localParameters, ref expression))
            {
                isArithmetic = true;
                return expression;
            }

            if (TestLang(s, localParameters, ref expression))
            {
                return expression;
            }

            return Expression.Constant(ReplaceNamespacePrefix(s));
        }

        private bool TestLang(string s, List<FilterParameterInfo> localParameters, ref Expression field)
        {
            Match langMatch = Reg.Lang.Match(s);
            if (!langMatch.Success) return false;
            var langParameterName = langMatch.Groups[1].Value.Trim();
            if (!langParameterName.StartsWith("?")) throw new Exception("lang must contains parameter");
            var langParameter = GetParameterOrCreate(langParameterName, localParameters);
            if (!langParameter.IsAssigned) throw new Exception("lang must contains parameter with value");
            field = Expression.Field(langParameter.Parameter, typeof (SparqlVariable).GetField("Lang"));
            return true;
        }

        private Expression GetDoubleArithmeticOrConst(string s, List<FilterParameterInfo> parameters)
        {
            s = s.Trim().Trim('"');
            //const
            double i;
            DateTime dt;
            if (Double.TryParse(s, out i))
                return Expression.Constant(i);
            if (DateTime.TryParse(s, out dt))
                return Expression.Constant((double) dt.ToUniversalTime().Ticks, typeof (double));
            //Expressions
            Expression expression = null;
            if (TestDoubleArithmeticExpression(s, parameters, ref expression))
                return expression;
            //variable
            if (s.StartsWith("?"))
                return Expression.Field(GetParameterOrCreate(s, parameters).Parameter,
                    typeof (SparqlVariable).GetField("DoubleValue"));

            throw new Exception("undefined arithmetic: " + s);
        }

        private bool TestDoubleArithmeticExpression(string s, List<FilterParameterInfo> parameters,
            ref Expression expression)
        {
            Match m;
            if ((m = Reg.USubtrAllInBrackets.Match(s)).Success)
            {
                s = m.Groups["inside"].Value;
                if (m.Groups["m"].Success) //минус
                {
                    expression = Expression.Subtract(Expression.Constant(0.0),
                        GetDoubleArithmeticOrConst(s, parameters));
                    return true;
                }
                return TestDoubleArithmeticExpression(s, parameters, ref expression);
            }
            if ((m = Reg.SumSubtract.Match(s)).Success)
            {
                expression = m.Groups["center"].Value == "+"
                    ? Expression.Add(GetDoubleArithmeticOrConst(m.Groups["left"].Value, parameters),
                        GetDoubleArithmeticOrConst(m.Groups["right"].Value, parameters))
                    : Expression.Subtract(GetDoubleArithmeticOrConst(m.Groups["left"].Value, parameters),
                        GetDoubleArithmeticOrConst(m.Groups["right"].Value, parameters));
                return true;
            }
            if ((m = Reg.MulDiv.Match(s)).Success)
            {
                expression = m.Groups["center"].Value == "*"
                    ? Expression.Multiply(GetDoubleArithmeticOrConst(m.Groups["left"].Value, parameters),
                        GetDoubleArithmeticOrConst(m.Groups["right"].Value, parameters))
                    : Expression.Divide(GetDoubleArithmeticOrConst(m.Groups["left"].Value, parameters),
                        GetDoubleArithmeticOrConst(m.Groups["right"].Value, parameters));
                return true;
            }
            if ((m = Reg.USubtrAtom.Match(s)).Success)
            {
                expression = Expression.Subtract(Expression.Constant(0.0),
                    GetDoubleArithmeticOrConst(m.Groups["inside"].Value, parameters));
                return true;
            }
            return false;
        }

        private FilterParameterInfo GetParameterOrCreate(string name, IList<FilterParameterInfo> localParameters)
        {
            var existing = localParameters.FirstOrDefault(p => p.Parameter.Name == name);
            if (existing == null)
            {
                existing = new FilterParameterInfo
                {
                    Parameter = Expression.Parameter(typeof (SparqlVariable), name)
                };
                if (!(existing.IsAssigned = valuesByName.TryGetValue(name, out existing.Value)))
                    valuesByName.Add(name, existing.Value = new SparqlVariable());

                if (existing.IsAssigned && existing.Value.Value == "hasParellellValue")
                    existing.IsAssigned = false;
                localParameters.Add(existing);
            }
            return existing;
        }

        private SparqlNodeBase EqualOrAssign(string left, string right, bool isOptional = false)
        {
            bool isLeftParameter = left.StartsWith("?"), isRightParameter = right.StartsWith("?");
            var localParameters = new List<FilterParameterInfo>();
            if (!isLeftParameter && !isRightParameter)
            {
                bool isArithmetic = false;
                var leftExpr = GetStringOrArithmetic(left, localParameters, ref isArithmetic);
                Expression rightExpr;
                if (isArithmetic)
                    rightExpr = GetDoubleArithmeticOrConst(right, localParameters);
                else
                {
                    bool isArithmeticSecond = false;
                    rightExpr = GetStringOrArithmetic(right, localParameters, ref isArithmeticSecond);
                    if (isArithmeticSecond)
                        leftExpr = GetDoubleArithmeticOrConst(left, localParameters);
                    isArithmetic = isArithmeticSecond;
                }
                if (!isArithmetic && isOptional) throw new Exception("optional filter just for math expressions");
                return CreateSelectsNewParameters(isArithmetic
                    ? isOptional
                        ? (SparqlNodeBase)new FilterTestDoublesOptional(Expression.Equal(leftExpr, rightExpr), localParameters)
                        : new FilterTestDoubles(Expression.Equal(leftExpr, rightExpr), localParameters)
                    : new FilterTest(Expression.Equal(leftExpr, rightExpr), localParameters),
                    localParameters);

            }
            if (!isLeftParameter) // right parameter
            {

                return EqualOrAssignWithOneSideParameter(left, right, localParameters, isOptional);
            }

            if (!isRightParameter) //left parameter
            {
                return EqualOrAssignWithOneSideParameter(right, left, localParameters, isOptional);
            }
            //теперь знаем, то слева параметер, можем его создать/использовать
            var paramLeftInfo = GetParameterOrCreate(left, localParameters);
            //оба параметра
            var paramRightInfo = GetParameterOrCreate(right, localParameters);
            if (paramLeftInfo.IsAssigned)
                if (paramRightInfo.IsAssigned) // todo оба параметра тут известны - как сравнивать?
                {
                    return CreateSelectsNewParameters(new FilterTest(
                        Expression.Equal(
                            Expression.Field(paramLeftInfo.Parameter, typeof (SparqlVariable).GetField("Value")),
                            Expression.Field(paramRightInfo.Parameter, typeof (SparqlVariable).GetField("Value"))),
                        localParameters), localParameters);
                }
                else
                {
                    // правый параметер неизвестный
                    paramRightInfo.IsAssigned = true;

                    return new FilterAssign(paramRightInfo.Value, paramLeftInfo.Value);
                }

            if (!paramRightInfo.IsAssigned) //оба не известны - сравниваем
            {
                return CreateSelectsNewParameters(
                    new FilterTest(
                        Expression.Equal(
                            Expression.Field(paramLeftInfo.Parameter, typeof (SparqlVariable).GetField("Value")),
                            Expression.Field(paramRightInfo.Parameter, typeof (SparqlVariable).GetField("Value"))),
                        localParameters), localParameters);

            }
            // левый параметер неизвестный
            paramLeftInfo.IsAssigned = true;
            return new FilterAssign(paramLeftInfo.Value, paramRightInfo.Value);
        }

        private SparqlNodeBase EqualOrAssignWithOneSideParameter(string unparameterSide, string paramOneSide,
            List<FilterParameterInfo> localParameters, bool isOptional)
        {
            var leftParameters = new List<FilterParameterInfo>();
            bool isArithmetic = false;
            var leftExpression = GetStringOrArithmetic(unparameterSide, leftParameters, ref isArithmetic);
            FilterParameterInfo paramOneSideInfo = GetParameterOrCreate(paramOneSide, localParameters);
            if (paramOneSideInfo.IsAssigned)
            {
                if (!isArithmetic && isOptional) throw new Exception("optional filter just for math expressions");
                return CreateSelectsNewParameters(isArithmetic
                    ? isOptional
                        ? (SparqlNodeBase) new FilterTestDoublesOptional(Expression.Equal(leftExpression,
                            Expression.Field(paramOneSideInfo.Parameter, typeof (SparqlVariable).GetField("DoubleValue"))),
                            localParameters)
                        : new FilterTestDoubles(Expression.Equal(leftExpression,
                            Expression.Field(paramOneSideInfo.Parameter, typeof (SparqlVariable).GetField("DoubleValue"))),
                            localParameters)
                    : new FilterTest(Expression.Equal(leftExpression,
                        Expression.Field(paramOneSideInfo.Parameter, typeof (SparqlVariable).GetField("Value"))),
                        localParameters), leftParameters);
            }
            paramOneSideInfo.IsAssigned = true;
            if (leftParameters.Count == 0) //knoun is const
            {
                return new FilterAssign(paramOneSideInfo.Value,
                    new SparqlVariable
                    {
                        Value =
                            (Expression.Lambda<Func<dynamic>>(leftExpression, new ParameterExpression[] {})).Compile()()
                    });
                ;
            }
            if (leftParameters.Contains(paramOneSideInfo)) //не исключаем и  ?newp=?newp
            {
                //TODO always true/false
                return
                    CreateSelectsNewParameters(
                        new FilterTest(Expression.Equal(leftExpression, paramOneSideInfo.Parameter),
                            localParameters = localParameters.Concat(leftParameters).Distinct().ToList()),
                        localParameters);
            }
            return
                CreateSelectsNewParameters(
                    new FilterAssignCalculated(paramOneSideInfo.Value, leftExpression, leftParameters), leftParameters);
        }

       
    }
}