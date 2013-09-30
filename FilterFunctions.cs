using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace CommonRDF
{
    internal static class FilterFunctions
    {
        private static readonly Regex RegAndOr = new Regex(@"^\s*(.+)\s*(\|\||&&)\s*(.+?)\s*$", RegexOptions.Compiled);
        private static readonly Regex RegEquality = new Regex(@"^\s*([^<>=]+?)\s*(<\s*=|=\s*>|!\s*=|!|=|<|>)\s*(.+?)\s*$", RegexOptions.Compiled);
        private static readonly Regex RegNot = new Regex(@"^\s*!\s*(.+?)\s*$", RegexOptions.Compiled);
        private static readonly Regex RegSameTerm = new Regex(@"^\s*[Ss][Aa][Mm][Ee][Tt][Ee][Rr][Mm]\s*\(\s*(.+?)\s*,\s*(.+?)\s*\)\s*$", RegexOptions.Compiled);
        private static readonly Regex RegMulDiv = new Regex(@"^\s*(.+?)\s*(*|/)\s*(.+?)\s*$", RegexOptions.Compiled);
        private static readonly Regex RegSumSubtract = new Regex(@"^\s*(.+?)\s*(+|-)\s*(.+?)\s*$", RegexOptions.Compiled);

        internal static SparqlBase Create(string expression, SparqlBase last=null)
        {
            return AndOrExpression(expression, last);
        }
        public static BinaryExpression CreateBinaryExpression(ExpressionType equalExpressionType, string left, string right,
            List<FilterParameterInfo> parameters)
        {
            var binaryExpression = Expression.MakeBinary(equalExpressionType,
                ArithmeticExpression(left, parameters),
                ArithmeticExpression(right, parameters));
            return binaryExpression;
        }

        private static SparqlBase AndOrExpression(string s, SparqlBase last)
        {
            Match m = RegAndOr.Match(s);
            if (m.Success)
                if (m.Groups[2].Value == "||")
                    return new FilterOr(AndOrExpression(m.Groups[1].Value, null),
                        AndOrExpression(m.Groups[3].Value, null), last);
                else
                    return AndOrExpression(m.Groups[3].Value, AndOrExpression(m.Groups[1].Value, last));
            return FilterOnePredicateSparqlBase(s, last);
            //TODO Unary NOT
        }

        private static SparqlBase FilterOnePredicateSparqlBase(string s, SparqlBase last)
        {
            var m = RegSameTerm.Match(s);
            if (m.Success)
                return EqualOrAssign(m.Groups[1].Value, m.Groups[2].Value, last);
            if ((m = RegEquality.Match(s)).Success)
            {
                var equalityType = m.Groups[2].Value;
                if (equalityType == "=")
                    return EqualOrAssign(m.Groups[1].Value, m.Groups[3].Value, last);
                var parameters = new List<FilterParameterInfo>();
                return new FilterTest(
                    CreateBinaryExpression(
                        equalityType.Length > 1
                            ? (equalityType.StartsWith("!")
                                ? ExpressionType.NotEqual
                                : equalityType.StartsWith("<")
                                    ? ExpressionType.LessThanOrEqual
                                    : ExpressionType.GreaterThanOrEqual)
                            : (equalityType == ">"
                                ? ExpressionType.GreaterThan
                                : ExpressionType.LessThan), m.Groups[1].Value, m.Groups[3].Value, parameters)
                    , parameters, last);
            }
            //TODO boolean valiable
            throw new NotImplementedException();
        }

        private static Expression ArithmeticExpression(string s, List<FilterParameterInfo> parameters)
        {
            Match m;
            //Expressions
            if ((m = RegSumSubtract.Match(s)).Success)
                return m.Groups[2].Value == "+" //TODO unary -
                    ? Expression.Add(ArithmeticExpression(m.Groups[1].Value, parameters), ArithmeticExpression(m.Groups[3].Value, parameters))
                    : Expression.Subtract(ArithmeticExpression(m.Groups[1].Value, parameters), ArithmeticExpression(m.Groups[3].Value, parameters));
            if ((m = RegMulDiv.Match(s)).Success)
                return m.Groups[2].Value == "*"
                    ? Expression.Multiply(ArithmeticExpression(m.Groups[1].Value, parameters), ArithmeticExpression(m.Groups[3].Value, parameters))
                    : Expression.Divide(ArithmeticExpression(m.Groups[1].Value, parameters), ArithmeticExpression(m.Groups[3].Value, parameters));

            s = s.Trim();
            //variable
            if (s.StartsWith("?"))
                return GetParameterOrCreate(s, parameters).Parameter;
            //const
            if (s.StartsWith("\"")) //TODO < ns:
                return Expression.Constant(s.Trim('"'), typeof(string));
            long i;
            DateTime dt;
            if (Int64.TryParse(s, out i))
                return Expression.Constant(i);
            if (DateTime.TryParse(s, out dt))
                return Expression.Constant(dt.ToUniversalTime());
            return Expression.Constant(s);
        }

        private static FilterParameterInfo GetParameterOrCreate(string name, IList<FilterParameterInfo> parameters)
        {
            var existing = parameters.FirstOrDefault(p => p.Parameter.Name == name);
            if (existing == null)
            {
                var newParameter = FilterParameterInfo.TestNewParameter(name);
                parameters.Add(
                    existing =
                        new FilterParameterInfo
                        {
                            Parameter = Expression.Parameter(typeof(object), name),
                            IsAssigned = newParameter.Value,
                            Value = newParameter.Key
                        });
            }
            return existing;
        }

        private static SparqlBase EqualOrAssign(string left, string right, SparqlBase last)
        {
            bool isLeftParameter = left.StartsWith("?"), isRightParameter = left.StartsWith("?");
            var parameters = new List<FilterParameterInfo>();
            if (!isLeftParameter && !isRightParameter)
            {
                return new FilterTest(
                    CreateBinaryExpression(ExpressionType.Equal, left, right, parameters),
                    parameters,
                    last);
            }
            if (!isLeftParameter)// right parameter
                return EqualOrAssignWithOneSideParameter(left,
                    GetParameterOrCreate(right, parameters),
                    parameters,
                    last);
            //теперь знаем, то слева параметер, можем его создать/использовать
            var paramLeftInfo = GetParameterOrCreate(left, parameters);
            if (!isRightParameter) //left parameter
                return EqualOrAssignWithOneSideParameter(right, paramLeftInfo, parameters, last);
            //оба параметра
            var paramRightInfo = GetParameterOrCreate(right, parameters);
            if (paramLeftInfo.IsAssigned)
                if (paramRightInfo.IsAssigned)// оба известны - сравниваем
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

        private static SparqlBase EqualOrAssignWithOneSideParameter(string unparameterSide, FilterParameterInfo paramOneSideInfo, 
            List<FilterParameterInfo> parameters, SparqlBase last)
        {
            var leftParameters = new List<FilterParameterInfo>();
            var leftExpression = ArithmeticExpression(unparameterSide, leftParameters);
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