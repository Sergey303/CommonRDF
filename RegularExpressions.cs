using System;
using System.Text.RegularExpressions;

namespace CommonRDF
{
    internal class Re
    {
        internal static readonly Regex QuerySelectReg = CreateRegex(@"[Ss][Ee][Ll][Ee][Cc][Tt]\s+((\?\w+\s+)+|\*)");
        internal static readonly Regex QueryWhereReg = CreateRegex(@"[Ww][Hh][Ee][Rr][Ee]\s+\{(([^{}]*\{[^{}]*\}[^{}]*)*|[^{}]*)\}");

        internal static Regex TripletsReg = CreateRegex(
            //@"((?<s>[^\s]+|'.*')\s+(?<p>[^\s]+|'.*')\s+(?<o>[^\s]+|'.*')\.(\s|$))|([Oo][Pp][Tt][Ii][Oo][Nn][Aa][Ll]\s+{\s*(?<os>[^\s]+|'.*')\s+(?<op>[^\s]+|'.*')\s+(?<oo>[^\s]+|'.*')\s*}(\s|$))|[Ff][Ii][Ll][Tt][Ee][Rr]\s+(?<filterttype>[^\s()]+)?\((?<filter>.*)\)"
            @"(([^\s]+|'.*')\s+([^\s]+|'.*')\s+([^\s]+|'.*')\.(\s|$))|([Oo][Pp][Tt][Ii][Oo][Nn][Aa][Ll]\s+{\s*([^\s]+|'.*')\s+([^\s]+|'.*')\s+([^\s]+|'.*')\s*}(\s|$))|[Ff][Ii][Ll][Tt][Ee][Rr]\s+([^\s()]+)?\((.*?)\)"
            );

        private static Regex CreateRegex(string pattern, RegexOptions add=RegexOptions.None)
        {
            return new Regex(pattern, add|
                RegexOptions.Compiled|RegexOptions.Singleline, TimeSpan.FromMinutes(1.0));
        }
        
        #region Filter

        internal static readonly Regex RegAndOr = CreateRegex(@"^\s*(\S.*?)\s*(\|\||&&)\s*(\S.*)\s*$");

        internal static readonly Regex RegEquality =
            CreateRegex(@"^\s*([^<>=\s][^<>=]*?)\s*(<\s*=|=\s*>|!\s*=|=|<|>)\s*(\S.*?)\s*$");

        internal static readonly Regex RegNot = CreateRegex(@"^\s*!\s*(\S.*?)\s*$");

        internal static readonly Regex RegSameTerm =
            CreateRegex(@"^\s*[Ss][Aa][Mm][Ee][Tt][Ee][Rr][Mm]\s*\(\s*(\S.*?)\s*,\s*(\S.*?)\s*\)\s*$");

        internal static readonly Regex RegMulDiv = CreateRegex(@"^\s*(\S.*?)\s*(\*|/)\s*(\S.*?)\s*$");
        internal static readonly Regex RegSumSubtract = CreateRegex(@"^\s*(\S.*?)\s*(\+|-)\s*(\S.*?)\s*$");
        // todo private static readonly Regex regParentes = new Regex("\\((?<inside>[^)^(])\\)", RegexOptions.Compiled);

        #endregion


    }
}