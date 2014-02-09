using System;
using System.Text.RegularExpressions;

namespace CommonRDF
{
    internal class Reg
    {
        internal static readonly Regex QueryPrefix = CreateRegex(@"^([Pp][Rr][Ee][Ff][Ii][Xx]\s+(?<shortName>[a-z1-9-_A-Z]+)\s*:\s*\<(?<url>[^>]*)\>\s*)*", RegexOptions.ExplicitCapture);
        internal static readonly Regex QuerySelect = CreateRegex(@"^((?<select>[Ss][Ee][Ll][Ee][Cc][Tt])|(?<descr>[Dd][Ee][Ss][Cc][Rr][Ii][Bb][Ee]))\s*((?<dist>[Dd][Ii][Ss][Tt][Ii][Nn][Cc][Tt])|(?<red>[Rr][Ee][Dd][Uu][Cc][Ee][Dd]))?\s*(((?<p>\?\S*)\s*)*|\*)\s*\s(?=\w)", RegexOptions.ExplicitCapture);
        internal static readonly Regex QueryConstruct = CreateRegex(@"^[Cc][Oo][Nn][Ss][Tt][Rr][Uu][Cc][Tt]\s*{\s*(?<firstS>\S+)\s+(?<firstP>\S+)\s+(?<firstO>\S+|'[^']*')(\s*\.\s*(?<s>\S+)\s+(?<p>\S+)\s+(?<o>\S+|'[^']*'))*\s*}\s*", RegexOptions.ExplicitCapture);
        internal static readonly Regex QueryWhere = CreateRegex(@"^[Ww][Hh][Ee][Rr][Ee]\s+\{\s*(?<insideWhere>[^{}]*(((?'Open'\{)[^{}]*)+((?'Close-Open'\})[^{}]*)+)*(?(Open)(?!)))\}");
        internal static readonly Regex OrderClause = CreateRegex(@"^[Oo][Rr][Dd][Ee][Rr]\s+[Bb][Yy]\s+(?<query>(([Aa][Ss][Cc])|([Dd][Ee][Ss][Cc]))?\s*(?<p>((xsd:)|\?)\S+)\s*)+", RegexOptions.ExplicitCapture);
        internal static readonly Regex Limit = CreateRegex(@"^limit\s+(?<count>[0-9]+)\s*", RegexOptions.ExplicitCapture);
        internal static readonly Regex Offset = CreateRegex(@"^offset\s+(?<count>[0-9]+)\s*", RegexOptions.ExplicitCapture);
        internal static Regex Triplet = CreateRegex(@"^(\S+)\s+(\S+)\s+(\S+|'[^']*')\s*$");
        internal static Regex TripletDot = CreateRegex(@"^((\S+)\s+(\S+)\s+(\S+|'[^']*')\s*\.(\s+|$))+");
        internal static Regex TripletOptional = CreateRegex(@"^[Oo][Pp][Tt][Ii][Oo][Nn][Aa][Ll]\s*{\s*(?<inside>[^{}]*(((?'Open'\{)[^{}]*)+((?'Close-Open'\})[^{}]*)+)*(?(Open)(?!)))\s*\}\s*", RegexOptions.ExplicitCapture);
        internal static Regex Filter = CreateRegex(@"^[Ff][Ii][Ll][Tt][Ee][Rr]\s+([^\s()]+)?\(\s*(?<filter>[^()]*(((?<Open>\()[^()]*)+((?<Close-Open>\))[^()]*)+)*(?(Open)(?!)))\s*\)\s*");
        //internal static Regex BracketsOfAndOrNot = CreateRegex(
        //    @"(?<inside>[^()]*(((?<Open>\()[^()]*)+((?<Close-Open>\))[^()]*)+)*(?(Open)(?!)))");
       // internal static readonly Regex InsideBraces = CreateRegex(@"^\{\s*(?<inside>[^{}]*(((?<Open>\{)[^()]*)+((?<Close-Open>\})[{}]*)+)*(?(Open)(?!)))\s*\)\s*", RegexOptions.ExplicitCapture);
        internal static readonly Regex Union = CreateRegex(@"^{\s*(?<inside>[^{}]*(((?<Open>{)[^{}]*)+((?<Close-Open>})[^{}]*)+)*(?(Open)(?!)))\s*}\s*([Uu][Nn][Ii][Oo][Nn]\s*{\s*(?<insideAlter>[^{}]*(((?<Open>{)[^{}]*)+((?<Close-Open>})[^{}]*)+)*(?(Open)(?!)))\s*\}\s*)*", RegexOptions.ExplicitCapture);

        
        private static Regex CreateRegex(string pattern, RegexOptions add=RegexOptions.None)
        {
            return new Regex(pattern, add|RegexOptions.Singleline, TimeSpan.FromMinutes(1.0));//RegexOptions.Compiled|
        }
        
        #region Filter

        internal static readonly Regex InsideBrackets = CreateRegex(@"^\(\s*(?<inside>[^()]*(((?<Open>\()[^()]*)+((?<Close-Open>\))[^()]*)+)*(?(Open)(?!)))\s*\)\s*$", RegexOptions.ExplicitCapture);
     
        /// <summary>
        /// действует только если строка начинается с отрицания, после отрицаний выражение в скобках.
        /// !!!!(....)
        /// отделяет первый символ отрицания, остальное помещает в группу insideOneNot
        /// !insideOneNot!!!(....)insideOneNot
        /// </summary>
        internal static readonly Regex ManyNotAllInBrackets = CreateRegex(@"^\!\s*(?<insideOneNot>(\!\s*)*\([^()]*(((?<Open>\()[^()]*)+((?<Close-Open>\))[^()]*)+)*(?(Open)(?!))\s*\))\s*$", RegexOptions.ExplicitCapture);
        
        /// <summary>
        /// Действует на строках, начинающихся с отрицания.
        /// !!!!.....
        /// отделяет первый символ отрицания, остальное помещает в группу  insideOneNot.
        /// !insideOneNot!!!.....insideOneNot
        /// </summary>
        internal static readonly Regex ManyNotAtom = CreateRegex(@"^\!\s*(?<insideOneNot>.*)\s*$", RegexOptions.ExplicitCapture);
        
        /// <summary>
        /// left and right $
        /// left or right $
        /// (left....) andor right $
        /// left andor (right...) $
        /// (left....) andor (right...) $
        /// </summary>
        internal static readonly Regex AndOr = CreateRegex(@"^(?<s>[^()]*(((?'Open'\()[^()]*)+((?'Close-Open'\))[^()]*?)+)*(?(Open)(?!)))(?<op>&&|\|\|)\s*", RegexOptions.ExplicitCapture);
        
        internal static readonly Regex Equality = CreateRegex(@"^([^<>=\s][^<>=]*)\s*(<\s*=|=\s*>|!\s*=|=|<|>)\s*(\S.*)\s*$");
        
        internal static readonly Regex RegSameTerm = CreateRegex(@"^[Ss][Aa][Mm][Ee][Tt][Ee][Rr][Mm]\s*\(\s*(\S.*)\s*,\s*(\S.*)\s*\)\s*$");

        internal static readonly Regex Bound = CreateRegex(@"^[Bb][Oo][Uu][Nn][Dd]\s*\(\s*(\S.*)\s*\)\s*$");
        internal static readonly Regex Lang = CreateRegex(@"^[Ll][Aa][Nn][Gg]\s*\(\s*(\S.*)\s*\)\s*$");
        internal static readonly Regex LangMatches = CreateRegex(@"^[Ll][Aa][Nn][Gg][Mm][Aa][Tt][Cc][Hh][Ee][Ss]\s*\(\s*(\S.*)\s*,\s*(\S.*)\s*\)\s*$");
        /// <summary>
        /// действует только если всё выражение в скобках, в начале может быть минус.
        /// -(....)
        /// отделяет первый символ минуса, остальное помещает в группу inside
        /// -(inside....inside)
        /// </summary>
        internal static readonly Regex USubtrAllInBrackets = CreateRegex(@"^(<?m>-)?\s*\(\s*(?<inside>[^()]*(((?<Open>\()[^()]*)+((?<Close-Open>\))[^()]*)+)*(?(Open)(?!)))\s*\)\s*$", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Действует на строках, начинающихся с унарного минуса.
        /// -.....
        /// отделяет первый символ минуса, остальное помещает в группу  inside.
        /// -inside.....inside
        /// </summary>
        internal static readonly Regex USubtrAtom = CreateRegex(@"^-\s*(?<inside>.*)\s*$", RegexOptions.ExplicitCapture);

        internal static readonly Regex MulDiv = CreateRegex(@"^(?<left>(\(\s*(?<insideLeft>[^()]*(((?<Open>\()[^()]*)+((?<Close-Open>\))[^()]*)+)*(?(Open)(?!)))\s*\))|\S.*)\s*(?<center>\*|/)\s*(?<right>(\(\s*(?<insideRight>[^()]*(((?<OpenL>\()[^()]*)+((?<CloseL-OpenL>\))[^()]*)+)*(?(OpenL)(?!)))\s*\))|\S.*)\s*$", RegexOptions.ExplicitCapture);
        internal static readonly Regex SumSubtract = CreateRegex(@"^(?<left>(\(\s*(?<insideLeft>[^()]*(((?<Open>\()[^()]*)+((?<Close-Open>\))[^()]*)+)*(?(Open)(?!)))\s*\))|\S.*)\s*(?<center>\+|-)\s*(?<right>(\(\s*(?<insideRight>[^()]*(((?<OpenL>\()[^()]*)+((?<CloseL-OpenL>\))[^()]*)+)*(?(OpenL)(?!)))\s*\))|\S.*)\s*$", RegexOptions.ExplicitCapture);
        // todo private static readonly Regex regParentes = new Regex("\\((?<inside>[^)^(])\\)", RegexOptions.Compiled);

        #endregion
    }
}