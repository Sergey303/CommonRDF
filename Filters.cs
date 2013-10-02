﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace CommonRDF
{
    /// <summary>
    /// FILTER regex(?title, "^SPARQL")
    /// </summary>
    class SparqlFilterRegex : SparqlBase
    {
        private static readonly Regex RegFilteRregex = new Regex(@"^(\?\w+), " + "\"(.*?)\"" + @"(,\s*" + "\"(?<flags>[ismx]*)?\")*$", RegexOptions.Compiled);
        public TValue Parameter;
        public readonly string ParameterName;
        private readonly Regex regularExpression;

        public SparqlFilterRegex(string parameterExpressionFlags, SparqlBase last)
            : base(last)
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

    /// <summary>
    /// filter expr1 || expr2
    /// </summary>
    internal class FilterOr : SparqlBase
    {
        // содержит два условия, обёрнутые в узлы SparqlBase типов одного из фильтор SparqlBase.
        private readonly SparqlBase first;
        private readonly SparqlChain second;

        public FilterOr(string first, string second, Dictionary<string, TValue> paramByName, SparqlBase last)
            :base(last)
        {
            //запомним какие параметры уже были
            var copy = new Dictionary<string, TValue>(paramByName);
            this.first = FilterFunctions.Create(first, paramByName, null);
            // с помощью копии узнаём какие параметры были добавлены в первой ветви.
            var newParametersInLeft = paramByName.Where(p => !copy.ContainsKey(p.Key)).ToList();
            foreach (var parameter in newParametersInLeft)
            {
                //помечаем их
                parameter.Value.Value = "hasParellellValue";
            }
            //новые параметры в одной ветви будут новыми и во второй
            this.second.Add(FilterFunctions.Create(second, paramByName, null));
            //оба звена ведут к консантной истине.
            
            //каждое условие - ветвь = цепь, в случае успеха вызовет свой послендний NextMatch
            
            this.first.NextMatch = this.second.NextMatch = () => NextMatch();
            //снимаем метку.
            foreach (var parameter in newParametersInLeft)
                parameter.Value.Value = null;
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

        public FilterAssign(TValue newParamter, TValue value, SparqlBase last)
            : base(last)
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
        private readonly TValue newParamter;

        public FilterAssignCalculated(TValue newParamter,
            Expression calcExpression,
            List<FilterParameterInfo> parameters,
            SparqlBase last)
            : base(calcExpression, parameters, last)
        {
            this.newParamter = newParamter;
        }

        public override bool Match()
        {
            newParamter.Value = Method.DynamicInvoke(AllParameters.Select(p => p.Value)).ToString();
            return NextMatch();
        }
    }
    internal class FilterTest : SparqlBase
    {
        protected readonly TValue[] AllParameters;
        protected readonly Delegate Method;
        public FilterTest(Expression equalExpression, List<FilterParameterInfo> parameters, SparqlBase last)
            : base(last)
        {
            Method = Expression.Lambda(equalExpression, parameters.Select(p => p.Parameter)).Compile();
            AllParameters = parameters.Select(p => p.Value).ToArray();
            last = parameters
                  .Where(p => !p.IsAssigned)
                  .Select(p => p.Value)
                  .Aggregate(last,
                  (current, unknownVariable) => new SelectAllSubjects(unknownVariable, current));
            //todo replase all subjects by all subj and data
            last.NextMatch=this.Match;
        }

        public override bool Match()
        {
            return (bool)Method.DynamicInvoke(AllParameters.Select(p => p.Value));
        }
    }

    internal class FilterTestDoubles : FilterTest
    {
        public FilterTestDoubles(Expression equalExpression, List<FilterParameterInfo> parameters, SparqlBase last)
            : base(equalExpression, parameters, last)
        {}
        public override bool Match()
        {
            return  (bool)Method.DynamicInvoke(new List<double>(
                AllParameters.Select(parameter=>
                {
                    double caster;
                    if (!double.TryParse(parameter.Value, out caster))
                        throw new ArgumentException(parameter.Value + " must be double");
                    return caster;
                })));
        }
    }

    internal class FilterParameterInfo
    {
        public ParameterExpression Parameter;
        public bool IsAssigned;
        public TValue Value;
    }

   
}
