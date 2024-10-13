using System.Collections.Generic;
using Pooshit.Scripting.Tokens;

namespace Pooshit.Scripting.Control {

    /// <summary>
    /// block containing several statement tokens
    /// </summary>
    public interface ITokenContainer {

        /// <summary>
        /// tokens contained in the container
        /// </summary>
        IEnumerable<IScriptToken> Children { get; }
    }
}