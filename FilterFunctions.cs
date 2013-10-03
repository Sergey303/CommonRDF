using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace CommonRDF
{
    internal static class FilterFunctions
    {
      
        internal static void CreateFilterChain(this SparqlChain sparqlChain,  string expression, Dictionary<string, TValue> paramByName)
        {
            AndOrExpression(sparqlChain, expression, paramByName);
        }

        private static void AndOrExpression(SparqlChain sparqlChain, string s, Dictionary<string, TValue> paramByName)
        {
            Match m = Re.RegAndOr.Match(s);
            if (m.Success)
                if (m.Groups[2].Value == "||")
                    sparqlChain.Add(new FilterOr(m.Groups[1].Value, m.Groups[3].Value, paramByName));
                else
                {
                    AndOrExpression(sparqlChain, m.Groups[1].Value, paramByName);
                    AndOrExpression(sparqlChain, m.Groups[3].Value, paramByName);
                }
            else AtomPredicateSparqlBase(sparqlChain,s, paramByName);
            //TODO Unary NOT
        }

        private static void AtomPredicateSparqlBase(SparqlChain sparqlChain, string s, Dictionary<string, TValue> paramByName)
        {
            var m = Re.RegSameTerm.Match(s);
            if (m.Success)
            {
                EqualOrAssign(m.Groups[1].Value, m.Groups[2].Value, paramByName, sparqlChain);
                return;
            }
            if ((m = Re.RegEquality.Match(s)).Success)
            {
                var equalityType = m.Groups[2].Value;
                if (equalityType == "=")
                {
                   EqualOrAssign(m.Groups[1].Value, m.Groups[3].Value, paramByName, sparqlChain);
                    return;
                }
                var localParameters = new List<FilterParameterInfo>();
                CreateSelectsNewParameters(new FilterTestDoubles(
                    Expression.MakeBinary(equalityType.Length > 1
                        ? (equalityType.StartsWith("!")
                            ? ExpressionType.NotEqual
                            : equalityType.StartsWith("<")
                                ? ExpressionType.LessThanOrEqual
                                : ExpressionType.GreaterThanOrEqual)
                        : (equalityType == ">"
                            ? ExpressionType.GreaterThan
                            : ExpressionType.LessThan),
                        GetDoubleArithmeticOrConst(m.Groups[1].Value, paramByName, localParameters),
                        GetDoubleArithmeticOrConst(m.Groups[3].Value, paramByName, localParameters))
                    , localParameters), localParameters, sparqlChain);
                return;
            }
            //TODO boolean valiable
            throw new NotImplementedException();
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

        private static Expression GetStringOrArithmetic(string s, List<FilterParameterInfo> localParameters, ref bool isArithmetic, Dictionary<string, TValue> paramByName)
        {
            Match m;
            if ((m = Re.RegSumSubtract.Match(s)).Success)
            {
                //todo string concatenation +
                isArithmetic = true;
                return m.Groups[2].Value == "+" //TODO unary -
                    ? Expression.Add(GetDoubleArithmeticOrConst(m.Groups[1].Value, paramByName, localParameters), GetDoubleArithmeticOrConst(m.Groups[3].Value, paramByName, localParameters))
                    : Expression.Subtract(GetDoubleArithmeticOrConst(m.Groups[1].Value, paramByName, localParameters), GetDoubleArithmeticOrConst(m.Groups[3].Value, paramByName, localParameters));}
            if ((m = Re.RegMulDiv.Match(s)).Success)
            {
                isArithmetic = true;
                return m.Groups[2].Value == "*"
                    ? Expression.Multiply(GetDoubleArithmeticOrConst(m.Groups[1].Value, paramByName, localParameters), GetDoubleArithmeticOrConst(m.Groups[3].Value, paramByName, localParameters))
                    : Expression.Divide(GetDoubleArithmeticOrConst(m.Groups[1].Value, paramByName, localParameters), GetDoubleArithmeticOrConst(m.Groups[3].Value, paramByName, localParameters));}

            s = s.Trim();
            DateTime dt;
            double i;
            s = s.Trim('"');
            if (Double.TryParse(s, out i))
            {
                isArithmetic = true;
                return Expression.Constant(i);
            }
            if (DateTime.TryParse(s, out dt))
            {
                isArithmetic = true;
                return Expression.Constant((double) dt.ToUniversalTime().Ticks, typeof (double));
                //: Expression.Constant(dt.ToUniversalTime().ToString("s"));
            }
            //variable
            if (s.StartsWith("?"))
                throw new Exception("wrong usages");
              //  return GetParameterOrCreate(s, parameters, isArithmetic!=null && isArithmetic.Value ? typeof(double) : typeof(string)).Parameter;
            //const
            //TODO < ns:
            
            return Expression.Constant(s);
        }
        private static Expression GetDoubleArithmeticOrConst(string s, Dictionary<string, TValue> paramByName, List<FilterParameterInfo> parameters)
        {
            Match m;
            //Expressions
            if ((m = Re.RegSumSubtract.Match(s)).Success)
                return m.Groups[2].Value == "+" //TODO unary -
                    ? Expression.Add(GetDoubleArithmeticOrConst(m.Groups[1].Value, paramByName, parameters), GetDoubleArithmeticOrConst(m.Groups[3].Value, paramByName, parameters))
                    : Expression.Subtract(GetDoubleArithmeticOrConst(m.Groups[1].Value, paramByName, parameters), GetDoubleArithmeticOrConst(m.Groups[3].Value, paramByName, parameters));
            if ((m = Re.RegMulDiv.Match(s)).Success)
                return m.Groups[2].Value == "*"
                    ? Expression.Multiply(GetDoubleArithmeticOrConst(m.Groups[1].Value, paramByName, parameters), GetDoubleArithmeticOrConst(m.Groups[3].Value, paramByName, parameters))
                    : Expression.Divide(GetDoubleArithmeticOrConst(m.Groups[1].Value, paramByName, parameters), GetDoubleArithmeticOrConst(m.Groups[3].Value, paramByName, parameters));

            s = s.Trim();
            //variable
            if (s.StartsWith("?"))
                return GetParameterOrCreate(s, parameters, paramByName, typeof(double)).Parameter;
            //const
          //TODO < ns:
            s = s.Trim('"');
            double i;
            DateTime dt;
            if (Double.TryParse(s, out i))
                return Expression.Constant(i);
            if (DateTime.TryParse(s, out dt))
                return Expression.Constant((double)dt.ToUniversalTime().Ticks, typeof(double));
            throw new Exception("undefined arithmetic: "+s);
        }

        private static FilterParameterInfo GetParameterOrCreate(string name, IList<FilterParameterInfo> localParameters, Dictionary<string, TValue> paramByName, Type type)
        {
            var existing = localParameters.FirstOrDefault(p => p.Parameter.Name == name);
            if (existing == null)
            {
                existing = new FilterParameterInfo
                {
                    Parameter = Expression.Parameter(type, name)
                };
                if (!(existing.IsAssigned = paramByName.TryGetValue(name, out existing.Value)))
                    paramByName.Add(name, existing.Value = new TValue());

                if (existing.IsAssigned && existing.Value.Value == "hasParellellValue")
                    existing.IsAssigned = false;
                localParameters.Add(existing);
            }
            return existing;
        }

        private static void EqualOrAssign(string left, string right, Dictionary<string, TValue> paramByName, SparqlChain sparqlChain)
        {
            bool isLeftParameter = left.StartsWith("?"), isRightParameter = right.StartsWith("?");
            var localParameters = new List<FilterParameterInfo>();
            if (!isLeftParameter && !isRightParameter)
            {
                bool isArithmetic = false;
                //todo
                var leftExpr = GetStringOrArithmetic(left, localParameters, ref isArithmetic, paramByName);
                Expression rightExpr;
                if (isArithmetic)
                    rightExpr = GetDoubleArithmeticOrConst(right, paramByName, localParameters);
                else
                {
                    bool isArithmeticSecond = false;
                    rightExpr = GetStringOrArithmetic(right, localParameters, ref isArithmeticSecond, paramByName);
                    if (isArithmeticSecond)
                        leftExpr = GetDoubleArithmeticOrConst(left, paramByName, localParameters);
                    isArithmetic = isArithmeticSecond;
                }
                 CreateSelectsNewParameters(isArithmetic
                    ? new FilterTestDoubles(Expression.Equal(leftExpr, rightExpr), localParameters)
                    : new FilterTest(Expression.Equal(leftExpr, rightExpr), localParameters),
                    localParameters, sparqlChain);
                return;
            }
            if (!isLeftParameter) // right parameter
            {
                EqualOrAssignWithOneSideParameter(left, right, localParameters, paramByName, sparqlChain);
                return;
            }

            if (!isRightParameter) //left parameter
            {
                EqualOrAssignWithOneSideParameter(right, left, localParameters, paramByName, sparqlChain);
                return;
            }
            //теперь знаем, то слева параметер, можем его создать/использовать
            var paramLeftInfo = GetParameterOrCreate(left, localParameters,paramByName, typeof(string));  
            //оба параметра
            var paramRightInfo = GetParameterOrCreate(right, localParameters, paramByName, typeof(string));
            if (paramLeftInfo.IsAssigned)
                if (paramRightInfo.IsAssigned)// todo оба параметра тут известны - как сравнивать?
                {  CreateSelectsNewParameters(new FilterTest(Expression.Equal(paramLeftInfo.Parameter, paramLeftInfo.Parameter),
                        localParameters),localParameters, sparqlChain);
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
            { CreateSelectsNewParameters(new FilterTest(Expression.Equal(paramLeftInfo.Parameter, paramLeftInfo.Parameter),
                    localParameters), localParameters , sparqlChain);
                return;
            }
            // левый параметер неизвестный
            paramLeftInfo.IsAssigned = true;
            sparqlChain.Add(new FilterAssign(paramLeftInfo.Value, paramRightInfo.Value));
        }

        private static void EqualOrAssignWithOneSideParameter(string unparameterSide, string paramOneSide, List<FilterParameterInfo> localParameters, Dictionary<string, TValue> paramByName, SparqlChain sparqlChain)
        {
            var leftParameters = new List<FilterParameterInfo>();
            bool isArithmetic=false;
            var leftExpression = GetStringOrArithmetic(unparameterSide, leftParameters, ref isArithmetic, paramByName);
            FilterParameterInfo paramOneSideInfo = GetParameterOrCreate(paramOneSide, localParameters, paramByName, isArithmetic ? typeof (double) : typeof (string));
            if (paramOneSideInfo.IsAssigned)
            {
                CreateSelectsNewParameters(new FilterTest(Expression.Equal(leftExpression, paramOneSideInfo.Parameter), localParameters), leftParameters, sparqlChain);
                return;
            }
            paramOneSideInfo.IsAssigned = true;
            if (leftParameters.Count == 0) //knoun is const
            {
                sparqlChain.Add(new FilterAssign(paramOneSideInfo.Value,
                    new TValue
                    {
                        Value = (Expression.Lambda<Func<dynamic>>(leftExpression, new ParameterExpression[] { })).Compile()()
                    }));
                return;
            }
            if (leftParameters.Contains(paramOneSideInfo)) //не исключаем и  ?newp=?newp
            {
                //TODO always true/false
                CreateSelectsNewParameters(new FilterTest(Expression.Equal(leftExpression, paramOneSideInfo.Parameter),
                    localParameters=localParameters.Concat(leftParameters).Distinct().ToList()), localParameters, sparqlChain);
                return;
            }
            CreateSelectsNewParameters(new FilterAssignCalculated(paramOneSideInfo.Value, leftExpression, leftParameters), leftParameters, sparqlChain);
        }

        public static bool LangMatches(string languageTag, string languageRange)
        {
            if (languageRange == "*") return languageTag != String.Empty;
            return languageTag.ToLower().Contains(languageRange.ToLower());
        }

        public static string Lang(string term)
        {
            var substrings = term.Split('@');
            if (substrings.Length == 2)
                return substrings[1];
            return String.Empty;
        }
    }
}