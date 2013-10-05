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
        internal static Regex BracketsOfAndOrNot = CreateRegex(
            @"(?<inside>[^()]*(((?<Open>\()[^()]*)+((?<Close-Open>\))[^()]*)+)*(?(Open)(?!)))");
        private static Regex CreateRegex(string pattern, RegexOptions add=RegexOptions.None)
        {
            return new Regex(pattern, add|
                RegexOptions.Compiled|RegexOptions.Singleline, TimeSpan.FromMinutes(1.0));
        }
        
        #region Filter

        internal static readonly Regex InsideBrackets = CreateRegex(@"^\s*\(\s*(?<inside>[^()]*(((?<Open>\()[^()]*)+((?<Close-Open>\))[^()]*)+)*(?(Open)(?!)))\s*\)\s*$", RegexOptions.ExplicitCapture);
     
        /// <summary>
        /// действует только если строка начинается с отрицания, после отрицаний выражение в скобках.
        /// !!!!(....)
        /// отделяет первый символ отрицания, остальное помещает в группу insideOneNot
        /// !insideOneNot!!!(....)insideOneNot
        /// </summary>
        internal static readonly Regex ManyNotAllInBrackets = CreateRegex(@"^\s*\!\s*(?<insideOneNot>(\!\s*)*\([^()]*(((?<Open>\()[^()]*)+((?<Close-Open>\))[^()]*)+)*(?(Open)(?!))\s*\))\s*$", RegexOptions.ExplicitCapture);
        
        /// <summary>
        /// Действует на строках, начинающихся с отрицания.
        /// !!!!.....
        /// отделяет первый символ отрицания, остальное помещает в группу  insideOneNot.
        /// !insideOneNot!!!.....insideOneNot
        /// </summary>
        internal static readonly Regex ManyNotAtom = CreateRegex(@"^\s*\!\s*(?<insideOneNot>.*)\s*$", RegexOptions.ExplicitCapture);
        
        /// <summary>
        /// left and right $
        /// left or right $
        /// (left....) andor right $
        /// left andor (right...) $
        /// (left....) andor (right...) $
        /// </summary>
        internal static readonly Regex AndOr = CreateRegex(@"^\s*(?<left>(\(\s*(?<insideLeft>[^()]*(((?<Open>\()[^()]*)+((?<Close-Open>\))[^()]*)+)*(?(Open)(?!)))\s*\))|\S.*?)\s*(?<center>\|\||&&)\s*(?<right>(\(\s*(?<insideRight>[^()]*(((?<OpenL>\()[^()]*)+((?<CloseL-OpenL>\))[^()]*)+)*(?(OpenL)(?!)))\s*\))|\S.*)\s*$", RegexOptions.ExplicitCapture);

        internal static readonly Regex Equality =
            CreateRegex(@"^\s*([^<>=\s][^<>=]*?)\s*(<\s*=|=\s*>|!\s*=|=|<|>)\s*(\S.*?)\s*$");
        
        internal static readonly Regex RegSameTerm =
            CreateRegex(@"^\s*[Ss][Aa][Mm][Ee][Tt][Ee][Rr][Mm]\s*\(\s*(\S.*?)\s*,\s*(\S.*?)\s*\)\s*$");

        internal static readonly Regex Bound = CreateRegex(@"^\s*[Bb][Oo][Uu][Nn][Dd]\s*\(\s*(\S.*?)\s*\)\s*$");
        /// <summary>
        /// действует только если строка начинается с унарного минуса, затем всё выражение в скобках.
        /// -(....)
        /// отделяет первый символ минуса, остальное помещает в группу inside
        /// -(inside....inside)
        /// </summary>
        internal static readonly Regex USubtrAllInBrackets = CreateRegex(@"^\s*-\s*\(\s*(?<inside>[^()]*(((?<Open>\()[^()]*)+((?<Close-Open>\))[^()]*)+)*(?(Open)(?!)))\s*\)\s*$", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Действует на строках, начинающихся с унарного минуса.
        /// -.....
        /// отделяет первый символ минуса, остальное помещает в группу  inside.
        /// -inside.....inside
        /// </summary>
        internal static readonly Regex USubtrAtom = CreateRegex(@"^\s*-\s*(?<inside>.*)\s*$", RegexOptions.ExplicitCapture);

        internal static readonly Regex MulDiv = CreateRegex(@"^\s*(?<left>(\(\s*(?<insideLeft>[^()]*(((?<Open>\()[^()]*)+((?<Close-Open>\))[^()]*)+)*(?(Open)(?!)))\s*\))|\S.*?)\s*(?<center>\+|-)\s*(?<right>(\(\s*(?<insideRight>[^()]*(((?<OpenL>\()[^()]*)+((?<CloseL-OpenL>\))[^()]*)+)*(?(OpenL)(?!)))\s*\))|\S.*)\s*$", RegexOptions.ExplicitCapture);
        internal static readonly Regex SumSubtract = CreateRegex(@"^\s*(?<left>(\(\s*(?<insideLeft>[^()]*(((?<Open>\()[^()]*)+((?<Close-Open>\))[^()]*)+)*(?(Open)(?!)))\s*\))|\S.*?)\s*(?<center>\*|/)\s*(?<right>(\(\s*(?<insideRight>[^()]*(((?<OpenL>\()[^()]*)+((?<CloseL-OpenL>\))[^()]*)+)*(?(OpenL)(?!)))\s*\))|\S.*)\s*$", RegexOptions.ExplicitCapture);
        // todo private static readonly Regex regParentes = new Regex("\\((?<inside>[^)^(])\\)", RegexOptions.Compiled);

        #endregion


    }
}