using System.Text.RegularExpressions;
using Pooshit.Scripting.Data;
using Pooshit.Scripting.Errors;

namespace Pooshit.Scripting.Operations.Comparision {

    /// <summary>
    /// determines whether a string matches a regex
    /// </summary>
    public class MatchesNot : Comparator {
        internal MatchesNot() {
        }

        /// <inheritdoc />
        protected override object Compare(object lhs, object rhs, ScriptContext context) {
            if (!(rhs is string pattern))
                throw new ScriptRuntimeException("Matching pattern must be a regex string", this);

            string value = lhs?.ToString();
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

        /// <inheritdoc />
        public override string Literal => "!~";
    }
}