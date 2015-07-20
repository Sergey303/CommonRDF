using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace CommonRDF
{
    /// <summary>
    /// FILTER regex(?title, "^SPARQL")
    /// </summary>
<<<<<<< HEAD
    class SparqlFilterRegex : SparqlBase
    {
        private static readonly Regex RegFilteRregex = new Regex(@"^(\?\w+), " + "\"(.*?)\"" + @"(,\s*" + "\"(?<flags>[ismx]*)?\")*$", RegexOptions.Compiled);
        public TValue Parameter;
=======
    class SparqlFilterRegex : SparqlNodeBase
    {
        private static readonly Regex RegFilteRregex = new Regex(@"^(\?\w+), " + "\"(.*?)\"" + @"(,\s*" + "\"(?<flags>[ismx]*)?\")*$", RegexOptions.Compiled);
        public SparqlVariable Parameter;
>>>>>>> 5b07a7d99da1a84c4d159acd03a3aad69dc94ef7
        public readonly string ParameterName;
        private readonly Regex regularExpression;

        public SparqlFilterRegex(string parameterExpressionFlags)
        {
            var regMatch = RegFilteRregex.Match(parameterExpressionFlags);
            ParameterName = regMatch.Groups[1].Value;
            var flagsMatch = regMatch.Groups["flags"];
            RegexOptions options = RegexOptions.Compiled | RegexOptions.CultureInvariant;
            if (flagsMatch.Success)
            {
                if (flagsMatch.Value.Contains("i"))
                    options = options | RegexOptions.IgnoreCase;

                if (flagsMatch.Value.Contains("s"))
                    options = options | RegexOptions.Singleline;

                if (flagsMatch.Value.Contains("m"))
                    options = options | RegexOptions.Multiline;
                if (flagsMatch.Value.Contains("x"))
                    options = options | RegexOptions.IgnorePatternWhitespace;
            }
            regularExpression = new Regex(regMatch.Groups[2].Value, options);
        }
        public override bool Match()
        {
            return regularExpression.Match(Parameter.Value).Success && NextMatch();
        }
    }

<<<<<<< HEAD
    /// <summary>
    /// filter expr1 || expr2
    /// </summary>
    internal class FilterOr : SparqlBase
    {
        /// содержит два условия, обёрнутые в цепи SparqlChain.
        private readonly SparqlChainParametred first;
        private readonly SparqlChainParametred second;

        public FilterOr(string first, string second, SparqlChainParametred sparqlChainRoot, bool isNot, bool isOptionals)
        {
            //запомним какие параметры уже были
            var copy = new Dictionary<string, TValue>(sparqlChainRoot.valuesByName);
            (this.first = new SparqlChainParametred(sparqlChainRoot)).AndOrExpression(first, isOptionals, isNot);
            // с помощью копии узнаём какие параметры были добавлены в первой ветви.
            var newParametersInLeft = sparqlChainRoot.valuesByName.Where(p => !copy.ContainsKey(p.Key)).ToList();
            foreach (var parameter in newParametersInLeft)
            {
                //помечаем их
                parameter.Value.Value = "hasParellellValue";
            }
            //новые параметры в одной ветви будут новыми и во второй
            (this.second = new SparqlChainParametred(sparqlChainRoot)).AndOrExpression(second, isOptionals, isNot);
            //обе цепи ведут к следующему после этого звену.
            
            //каждое условие - ветвь = цепь, в случае успеха вызовет свой послендний NextMatch
            this.first.NextMatch = this.second.NextMatch = () => NextMatch();
            //снимаем метку.
            foreach (var parameter in newParametersInLeft)
                parameter.Value.Value = string.Empty;
        }

        /// <summary>
        /// Метод, проверяющий соответствующие части фильтра. 
        /// Должен вызвать Match() у обоих внутренних фильтров, т.к. они возможно лишь присваивают ?newP="newV"
        /// </summary>
        /// <returns></returns>
        public override bool Match()
        {
            bool any = first.Match();
            return (second.Match() || any);
        }
    }

    /// <summary>
    /// replaced by sequence of two Filters.
    /// </summary>
    //internal class FilterAnd : SparqlBase

    //class SparqlFilterSameTerm:SparqlBase
    //{
    //    public static bool SameTerm(string termLeft, string termRight)
    //    {
    //        var leftSubStrings = termLeft.Split(new[] { "^^" }, StringSplitOptions.RemoveEmptyEntries);
    //        var rightSubStrings = termRight.Split(new[] { "^^" }, StringSplitOptions.RemoveEmptyEntries);
    //        if (leftSubStrings.Length == 2 && rightSubStrings.Length == 2)
    //        {
    //            //different types
    //            if (!String.Equals(rightSubStrings[1], leftSubStrings[1], StringComparison.CurrentCultureIgnoreCase)) return false;
    //            ///TODO: different namespaces and same types
    //        }
    //        double leftDouble, rightDouble;
    //        DateTime leftDate, rightDate;
    //        return rightSubStrings[0] == leftSubStrings[0]
    //               ||
    //               (Double.TryParse(leftSubStrings[0].Replace("\"", ""), out leftDouble)
    //                &&
    //                Double.TryParse(rightSubStrings[0].Replace("\"", ""), out rightDouble)
    //                &&
    //                leftDouble == rightDouble)
    //               ||
    //               (DateTime.TryParse(leftSubStrings[0].Replace("\"", ""), out leftDate)
    //                &&
    //                DateTime.TryParse(rightSubStrings[0].Replace("\"", ""), out rightDate)
    //                &&
    //                leftDate == rightDate);
    //    }

    //    public override void Match()
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
    internal class FilterAssign : SparqlBase
    {
        private readonly TValue newParamter;
        private readonly TValue value;

        public FilterAssign(TValue newParamter, TValue value)
=======
    internal class FilterAssign : SparqlNodeBase
    {
        private readonly SparqlVariable newParamter;
        private readonly SparqlVariable value;

        public FilterAssign(SparqlVariable newParamter, SparqlVariable value)
>>>>>>> 5b07a7d99da1a84c4d159acd03a3aad69dc94ef7
        {
            this.newParamter = newParamter;
            this.value = value;
        }

        public override bool Match()
        {
            newParamter.Value = value.Value;
            return NextMatch();
        }
    }
    internal class FilterAssignCalculated : FilterTest
    {
<<<<<<< HEAD
        private readonly TValue newParamter;

        public FilterAssignCalculated(TValue newParamter,
=======
        private readonly SparqlVariable newParamter;

        public FilterAssignCalculated(SparqlVariable newParamter,
>>>>>>> 5b07a7d99da1a84c4d159acd03a3aad69dc94ef7
            Expression calcExpression,
            List<FilterParameterInfo> parameters)
            : base(calcExpression, parameters)
        {
            this.newParamter = newParamter;
        }

        public override bool Match()
        {
            newParamter.Value = Method.DynamicInvoke(AllParameters).ToString();
            return NextMatch();
        }
    }
<<<<<<< HEAD
    internal class FilterTest : SparqlBase
=======
    internal class FilterTest : SparqlNodeBase
>>>>>>> 5b07a7d99da1a84c4d159acd03a3aad69dc94ef7
    {
        protected readonly object[] AllParameters;
        protected readonly Delegate Method;

        public FilterTest(Expression equalExpression, List<FilterParameterInfo> parameters)
        {
            Method = Expression.Lambda(equalExpression, parameters.Select(p => p.Parameter)).Compile();
            AllParameters = parameters.Select(p => p.Value).ToArray();
        }

        public override bool Match()
        {
            return (bool)Method.DynamicInvoke(AllParameters) && NextMatch();
        }
    }
    internal class FilterTestDoubles : FilterTest
    {
        public FilterTestDoubles(Expression equalExpression, List<FilterParameterInfo> parameters)
            : base(equalExpression, parameters)
        {
        }

        public override bool Match()
        {
<<<<<<< HEAD
            return AllParameters.All(t => (t as TValue).IsDouble) && base.Match();
        }
    }
    internal class FilterTestDoublesOptional : FilterTest
=======
            return AllParameters.All(t => ((SparqlVariable) t).IsDouble) && base.Match();
        }
    }

    internal class FilterTestOptional : OptionalSparqlTripletBase
    {
        protected readonly object[] AllParameters;
        protected readonly Delegate Method;

        public FilterTestOptional(Expression equalExpression, List<FilterParameterInfo> parameters)
        {
            Method = Expression.Lambda(equalExpression, parameters.Select(p => p.Parameter)).Compile();
            AllParameters = parameters.Select(p => p.Value).ToArray();
        }

        public override bool Match()
        {
            bool isCurrent = (bool)Method.DynamicInvoke(AllParameters);
            return isCurrent ? NextMatch() : OptionalFailMatch();
        }
    }
    internal class FilterTestDoublesOptional : FilterTestOptional
>>>>>>> 5b07a7d99da1a84c4d159acd03a3aad69dc94ef7
    {
        public FilterTestDoublesOptional(Expression equalExpression, List<FilterParameterInfo> parameters)
            : base(equalExpression, parameters)
        {
        }

        public override bool Match()
        {
<<<<<<< HEAD
            return (!AllParameters.All(t => (t as TValue).IsDouble) && NextMatch()) || base.Match();
=======
            var currentSuccesful = AllParameters.All(t => ((SparqlVariable) t).IsDouble) && (bool) Method.DynamicInvoke(AllParameters);
            return currentSuccesful ? NextMatch() : OptionalFailMatch();
>>>>>>> 5b07a7d99da1a84c4d159acd03a3aad69dc94ef7
        }
    }

    internal class FilterParameterInfo
    {
        public ParameterExpression Parameter;
        public bool IsAssigned;
<<<<<<< HEAD
        public TValue Value;
=======
        public SparqlVariable Value;
>>>>>>> 5b07a7d99da1a84c4d159acd03a3aad69dc94ef7
    }

   
}
