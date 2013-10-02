using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace CommonRDF
{
    internal static class FilterFunctions
    {
      
        internal static SparqlBase Create(string expression, SparqlBase last=null)
        {
            return AndOrExpression(expression, last);
        }

        private static SparqlBase AndOrExpression(string s, SparqlBase last)
        {
            Match m = Re.RegAndOr.Match(s);
            if (m.Success)
                if (m.Groups[2].Value == "||")
                    return new FilterOr(AndOrExpression(m.Groups[1].Value, null),
                        AndOrExpression(m.Groups[3].Value, null), last);
                else
                    return AndOrExpression(m.Groups[3].Value, AndOrExpression(m.Groups[1].Value, last));
            return FilterAtomPredicateSparqlBase(s, last);
            //TODO Unary NOT
        }

        private static SparqlBase FilterAtomPredicateSparqlBase(string s, SparqlBase last)
        {
            var m = Re.RegSameTerm.Match(s);
            if (m.Success)
                return EqualOrAssign(m.Groups[1].Value, m.Groups[2].Value, last);
            if ((m = Re.RegEquality.Match(s)).Success)
            {
                var equalityType = m.Groups[2].Value;
                if (equalityType == "=")
                    return EqualOrAssign(m.Groups[1].Value, m.Groups[3].Value, last);
                var parameters = new List<FilterParameterInfo>();
                return new FilterTestDoubles(
                    Expression.MakeBinary(equalityType.Length > 1
                        ? (equalityType.StartsWith("!")
                            ? ExpressionType.NotEqual
                            : equalityType.StartsWith("<")
                                ? ExpressionType.LessThanOrEqual
                                : ExpressionType.GreaterThanOrEqual)
                        : (equalityType == ">"
                            ? ExpressionType.GreaterThan
                            : ExpressionType.LessThan),
                        GetDoubleArithmeticOrConst(m.Groups[1].Value, parameters),
                        GetDoubleArithmeticOrConst(m.Groups[3].Value, parameters))
                    , parameters, last);
            }
            //TODO boolean valiable
            throw new NotImplementedException();
        }

        private static Expression GetStringOrArithmetic(string s, List<FilterParameterInfo> parameters,
            ref bool isArithmetic)
        {
            Match m;
            if ((m = Re.RegSumSubtract.Match(s)).Success)
            {
                //todo string concatenation +
                isArithmetic = true;
                return m.Groups[2].Value == "+" //TODO unary -
                    ? Expression.Add(GetDoubleArithmeticOrConst(m.Groups[1].Value, parameters), GetDoubleArithmeticOrConst(m.Groups[3].Value, parameters))
                    : Expression.Subtract(GetDoubleArithmeticOrConst(m.Groups[1].Value, parameters), GetDoubleArithmeticOrConst(m.Groups[3].Value, parameters));}
            if ((m = Re.RegMulDiv.Match(s)).Success)
            {
                isArithmetic = true;
                return m.Groups[2].Value == "*"
                    ? Expression.Multiply(GetDoubleArithmeticOrConst(m.Groups[1].Value, parameters), GetDoubleArithmeticOrConst(m.Groups[3].Value, parameters))
                    : Expression.Divide(GetDoubleArithmeticOrConst(m.Groups[1].Value, parameters), GetDoubleArithmeticOrConst(m.Groups[3].Value, parameters));}

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
        private static Expression GetDoubleArithmeticOrConst(string s, List<FilterParameterInfo> parameters)
        {
            Match m;
            //Expressions
            if ((m = Re.RegSumSubtract.Match(s)).Success)
                return m.Groups[2].Value == "+" //TODO unary -
                    ? Expression.Add(GetDoubleArithmeticOrConst(m.Groups[1].Value, parameters), GetDoubleArithmeticOrConst(m.Groups[3].Value, parameters))
                    : Expression.Subtract(GetDoubleArithmeticOrConst(m.Groups[1].Value, parameters), GetDoubleArithmeticOrConst(m.Groups[3].Value, parameters));
            if ((m = Re.RegMulDiv.Match(s)).Success)
                return m.Groups[2].Value == "*"
                    ? Expression.Multiply(GetDoubleArithmeticOrConst(m.Groups[1].Value, parameters), GetDoubleArithmeticOrConst(m.Groups[3].Value, parameters))
                    : Expression.Divide(GetDoubleArithmeticOrConst(m.Groups[1].Value, parameters), GetDoubleArithmeticOrConst(m.Groups[3].Value, parameters));

            s = s.Trim();
            //variable
            if (s.StartsWith("?"))
                return GetParameterOrCreate(s, parameters, typeof(double)).Parameter;
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

        private static FilterParameterInfo GetParameterOrCreate(string name, IList<FilterParameterInfo> parameters,
           Type type)
        {
            var existing = parameters.FirstOrDefault(p => p.Parameter.Name == name);
            if (existing == null)
            {
                //todo add dictionary of query parameters as parameter for async
                var newParameter = FilterParameterInfo.TestNewParameter(name);
                parameters.Add(
                    existing =
                        new FilterParameterInfo
                        {
                            Parameter = Expression.Parameter(type, name),
                            IsAssigned = newParameter.Value,
                            Value = newParameter.Key
                        });
            }
            return existing;
        }

        private static SparqlBase EqualOrAssign(string left, string right, SparqlBase last)
        {
            bool isLeftParameter = left.StartsWith("?"), isRightParameter = right.StartsWith("?");
            var parameters = new List<FilterParameterInfo>();
            if (!isLeftParameter && !isRightParameter)
            {
                bool isArithmetic = false;
                //todo
                var leftExpr = GetStringOrArithmetic(left, parameters, ref isArithmetic);
                Expression rightExpr;
                if (isArithmetic)
                    rightExpr = GetDoubleArithmeticOrConst(right, parameters);
                else
                {
                    bool isArithmeticSecond = false;
                    rightExpr = GetStringOrArithmetic(right, parameters, ref isArithmeticSecond);
                    if (isArithmeticSecond)
                        leftExpr = GetDoubleArithmeticOrConst(left, parameters);
                    isArithmetic = isArithmeticSecond;
                }
                return isArithmetic
                    ? new FilterTestDoubles(Expression.Equal(leftExpr, rightExpr), parameters, last)
                    : new FilterTest(Expression.Equal(leftExpr, rightExpr), parameters, last);
            }
            if (!isLeftParameter)// right parameter
                return EqualOrAssignWithOneSideParameter(left,
                  right,
                    parameters,
                    last);
          
            if (!isRightParameter) //left parameter
                return EqualOrAssignWithOneSideParameter(right, left, parameters, last);
            //теперь знаем, то слева параметер, можем его создать/использовать
            var paramLeftInfo = GetParameterOrCreate(left, parameters, typeof(string));  
            //оба параметра
            var paramRightInfo = GetParameterOrCreate(right, parameters, typeof(string));
            if (paramLeftInfo.IsAssigned)
                if (paramRightInfo.IsAssigned)// todo оба параметра тут известны - как сравнивать?
                    return new FilterTest(Expression.Equal(paramLeftInfo.Parameter, paramLeftInfo.Parameter),
                        new List<FilterParameterInfo>{ paramLeftInfo, paramRightInfo }, last);
                else
                {
                    // правый параметер неизвестный
                    paramRightInfo.IsAssigned = true;
                    return new FilterAssign(paramRightInfo.Value, paramLeftInfo.Value, last);
                }
            
            if (!paramRightInfo.IsAssigned) //оба не известны - сравниваем
                return new FilterTest(Expression.Equal(paramLeftInfo.Parameter, paramLeftInfo.Parameter),
                    new List<FilterParameterInfo> { paramLeftInfo, paramRightInfo }, last);
            // левый параметер неизвестный
            paramLeftInfo.IsAssigned = true;
            return new FilterAssign(paramLeftInfo.Value, paramRightInfo.Value, last);
        }

        private static SparqlBase EqualOrAssignWithOneSideParameter(string unparameterSide, string paramOneSide, 
            List<FilterParameterInfo> parameters, SparqlBase last)
        {
            var leftParameters = new List<FilterParameterInfo>();
            bool isArithmetic=false;
            var leftExpression = GetStringOrArithmetic(unparameterSide, leftParameters, ref isArithmetic);
            FilterParameterInfo paramOneSideInfo = GetParameterOrCreate(paramOneSide, parameters,
                isArithmetic ? typeof (double) : typeof (string));
            if (paramOneSideInfo.IsAssigned)
                return new FilterTest(Expression.Equal(leftExpression, paramOneSideInfo.Parameter), parameters, last);
            paramOneSideInfo.IsAssigned = true;
            if (leftParameters.Count == 0) //knoun is const
                return new FilterAssign(paramOneSideInfo.Value,
                    new TValue
                    {
                        Value = (Expression.Lambda<Func<dynamic>>(leftExpression, new ParameterExpression[] { })).Compile()()
                    },
                    last);
            if (leftParameters.Contains(paramOneSideInfo)) //не исключаем и  ?newp=?newp
            {
                //TODO always true/false
                return new FilterTest(Expression.Equal(leftExpression, paramOneSideInfo.Parameter),
                    parameters.Concat(leftParameters).Distinct().ToList(), last);
            }
            return new FilterAssignCalculated(paramOneSideInfo.Value, leftExpression, leftParameters, last);
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