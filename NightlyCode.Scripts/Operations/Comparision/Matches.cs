using System.Text.RegularExpressions;
using NightlyCode.Scripting.Data;
using NightlyCode.Scripting.Errors;

namespace NightlyCode.Scripting.Operations.Comparision {

    /// <summary>
    /// determines whether a string matches a regex
    /// </summary>
    public class Matches : Comparator {
        internal Matches() {
        }

        /// <inheritdoc />
        protected override object Compare(object lhs, object rhs, ScriptContext context) {
            string pattern = rhs as string;
            if (pattern == null)
                throw new ScriptRuntimeException("Matching pattern must be a regex string");

            string value = lhs?.ToString();
            if (value == null)
                return false;

            return Regex.IsMatch(value, pattern);
        }

        /// <inheritdoc />
        public override Operator Operator => Operator.Matches;

        /// <inheritdoc />
        public override string ToString() {
            return $"{Lhs} ~~ {Rhs}";
        }
    }
}