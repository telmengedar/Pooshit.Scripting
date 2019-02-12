using System.Text.RegularExpressions;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Parser;

namespace NightlyCode.Scripting.Operations.Comparision {

    /// <summary>
    /// determines whether a string matches a regex
    /// </summary>
    public class MatchesNot : Comparator {
        internal MatchesNot() {
        }

        /// <inheritdoc />
        protected override object Compare(IVariableProvider arguments) {
            string pattern = Rhs.Execute(arguments) as string;
            if (pattern == null)
                throw new ScriptRuntimeException("Matching pattern must be a regex string");

            string value = Lhs.Execute(arguments)?.ToString();
            if (value == null)
                return false;

            return !Regex.IsMatch(value, pattern);
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.NotMatches;

        /// <inheritdoc />
        public override string ToString() {
            return $"{Lhs} !~ {Rhs}";
        }
    }
}