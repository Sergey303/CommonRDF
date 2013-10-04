using System;
using System.Text.RegularExpressions;

namespace CommonRDF
{
    internal class Reg
    {
        internal static readonly Regex QuerySelect = CreateRegex(@"^\s*[Ss][Ee][Ll][Ee][Cc][Tt]\s+((\?\w+\s+)+|\*)\s*");
        internal static readonly Regex QueryWhere = CreateRegex(@"^\s*[Ww][Hh][Ee][Rr][Ee]\s+\{(([^{}]*\{[^{}]*\}[^{}]*)*|[^{}]*)\}\s*");

        internal static Regex Triplet = CreateRegex(
            @"^\s*([\S]+)\s+([\S]+|'.*')\s+([\S]+|'.*')\.(\s+|$)");
        internal static Regex TripletOptional = CreateRegex(
            @"^\s*[Oo][Pp][Tt][Ii][Oo][Nn][Aa][Ll]\s*{\s*([\S]+)\s+([\S]+|'.*')\s+([\S]+|'.*')\s*}\s*");
        internal static Regex Filter = CreateRegex(
            @"^\s*[Ff][Ii][Ll][Tt][Ee][Rr]\s+([^\s()]+)?\(\s*(?<filter>[^()]*(((?<Open>\()[^()]*)+((?<Close-Open>\))[^()]*)+)*(?(Open)(?!)))\s*\)\s*");
        private static Regex CreateRegex(string pattern, RegexOptions add=RegexOptions.None)
        {
            return new Regex(pattern, add|
                RegexOptions.Compiled|RegexOptions.Singleline, TimeSpan.FromMinutes(1.0));
        }
        
        #region Filter

        internal static readonly Regex AndOr = CreateRegex(@"^\s*(\S.*?)\s*(\|\||&&)\s*(\S.*)\s*$");

        internal static readonly Regex Equality =
            CreateRegex(@"^\s*([^<>=\s][^<>=]*?)\s*(<\s*=|=\s*>|!\s*=|=|<|>)\s*(\S.*?)\s*$");

        internal static readonly Regex Not = CreateRegex(@"^\s*!\s*(\S.*?)\s*$");

        internal static readonly Regex RegSameTerm =
            CreateRegex(@"^\s*[Ss][Aa][Mm][Ee][Tt][Ee][Rr][Mm]\s*\(\s*(\S.*?)\s*,\s*(\S.*?)\s*\)\s*$");
        internal static readonly Regex Bound = CreateRegex(@"^\s*[Bb][Oo][Uu][Nn][Dd]\s*\(\s*(\S.*?)\s*\)\s*$");
        internal static readonly Regex MulDiv = CreateRegex(@"^\s*(\S.*?)\s*(\*|/)\s*(\S.*?)\s*$");
        internal static readonly Regex SumSubtract = CreateRegex(@"^\s*(\S.*?)\s*(\+|-)\s*(\S.*?)\s*$");
        // todo private static readonly Regex regParentes = new Regex("\\((?<inside>[^)^(])\\)", RegexOptions.Compiled);

        #endregion


    }
}