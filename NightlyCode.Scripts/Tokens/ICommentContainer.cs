using System.Collections.Generic;

namespace NightlyCode.Scripting.Tokens {

    /// <summary>
    /// token containing comments
    /// </summary>
    public interface ICommentContainer : IScriptToken {

        /// <summary>
        /// comments linked to this statement
        /// </summary>
        IEnumerable<Comment> Comments { get; }

        /// <summary>
        /// adds a comment to the container
        /// </summary>
        /// <param name="comment">comment to add</param>
        void AddComment(Comment comment);
    }
}