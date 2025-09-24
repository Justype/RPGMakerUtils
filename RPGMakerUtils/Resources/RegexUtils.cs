using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RPGMakerUtils.Resources
{
    internal static class RegexUtils
    {
        /// <summary>
        /// Regex to match escape sequences like \varName, \{text}, \c[0]
        /// </summary>
        public static Regex EscapeRegex { get; } = new Regex(@"\\[a-zA-Z0-9_]+|\\[a-zA-Z0-9_]+\[(?:\d+)\]|\\\{(?:[^}]*)\}", RegexOptions.Compiled);

        public static Regex VariableOrNumberRegex { get; } = new Regex(@"^([a-zA-Z_][a-zA-Z0-9_]*|\d+(\.\d+)?)$", RegexOptions.Compiled);

        public static Regex PersonNameRegex { get; } = new Regex(@"<([^<>]+)>|【([^【】]+)】", RegexOptions.Compiled | RegexOptions.Singleline);

        public static Regex ObjectNoteRegex { get; } = new Regex(@"<([^<>:]+):([^<>]*)>", RegexOptions.Compiled | RegexOptions.Singleline);

        public static Regex CommonPluginValueRegex { get; } = new Regex(@"^(?:
            [\d,.]+| # match numbers and commas
            # common CSS color names
            white|black|red|blue|green|yellow|purple|cyan|magenta|gray|grey|orange|brown|pink|lime|
            navy|teal|olive|maroon|silver|gold|
            rgba?\([^\)]*\)|hsla?\([^\)]*\)|       # color functions
            \#[0-9a-fA-F]{3,6,8}|                  # hex colors
            true|false|null|undefined|NaN|Infinity| # JavaScript literals
            top|bottom|left|right|center|justify|inherit|initial|unset # CSS literals
        )$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        public static Regex LeadingSpacesRegex { get; } = new Regex(@"^(\\n)*[ 　]*", RegexOptions.Compiled);

        public static Regex TrailingSpacesRegex { get; } = new Regex(@"[ 　:：]*(\\n)*$", RegexOptions.Compiled);

        public static Regex LineBreakRegex { get; } = new Regex(@"\\r?\\n|\\n", RegexOptions.Compiled);
    }
}
