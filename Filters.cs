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
    class SparqlFilterRegex : SparqlNodeBase
    {
        private static readonly Regex RegFilteRregex = new Regex(@"^(\?\w+), " + "\"(.*?)\"" + @"(,\s*" + "\"(?<flags>[ismx]*)?\")*$", RegexOptions.Compiled);
        public SparqlVariable Parameter;
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

    internal class FilterAssign : SparqlNodeBase
    {
        private readonly SparqlVariable newParamter;
        private readonly SparqlVariable value;

        public FilterAssign(SparqlVariable newParamter, SparqlVariable value)
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
        private readonly SparqlVariable newParamter;

        public FilterAssignCalculated(SparqlVariable newParamter,
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
    internal class FilterTest : SparqlNodeBase
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
    {
        public FilterTestDoublesOptional(Expression equalExpression, List<FilterParameterInfo> parameters)
            : base(equalExpression, parameters)
        {
        }

        public override bool Match()
        {
            var currentSuccesful = AllParameters.All(t => ((SparqlVariable) t).IsDouble) && (bool) Method.DynamicInvoke(AllParameters);
            return currentSuccesful ? NextMatch() : OptionalFailMatch();
        }
    }

    internal class FilterParameterInfo
    {
        public ParameterExpression Parameter;
        public bool IsAssigned;
        public SparqlVariable Value;
    }

   
}
