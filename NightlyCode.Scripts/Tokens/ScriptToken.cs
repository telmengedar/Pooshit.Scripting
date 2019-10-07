using System;
using System.Collections.Generic;
using NightlyCode.Scripting.Errors;
using NightlyCode.Scripting.Extern;

namespace NightlyCode.Scripting.Tokens {
    /// <summary>
    /// base implementation of a script token with basic error handling
    /// </summary>
    public abstract class ScriptToken : ICommentContainer {
        readonly List<Comment> comments = new List<Comment>();

        /// <inheritdoc />
        public abstract string Literal { get; }

        /// <inheritdoc />
        public object Execute(ScriptContext context) {
            try {
                return ExecuteToken(context);
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (ScriptException) {
                throw;
            }
            catch (Exception e) {
                throw new ScriptRuntimeException($"Unable to execute '{this}'", e.Message, e);
            }
        }

        /// <inheritdoc />
        public T Execute<T>(ScriptContext context) {
            return Converter.Convert<T>(Execute(context));
        }

        /// <summary>
        /// evaluates the result of the token
        /// </summary>
        /// <returns>result of statement</returns>
        protected abstract object ExecuteToken(ScriptContext context);

        /// <inheritdoc />
        public IEnumerable<Comment> Comments => comments;

        /// <inheritdoc />
        public void AddComment(Comment comment) {
            comments.Add(comment);
        }
    }
}