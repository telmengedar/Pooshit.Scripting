using System.Collections.Generic;
using NightlyCode.Scripting.Data;

namespace NightlyCode.Scripting.Parser.Operators {

    /// <summary>
    /// node in an operator tree
    /// </summary>
    public class OperatorNode {
        readonly Dictionary<char, OperatorNode> children = new Dictionary<char, OperatorNode>();

        /// <summary>
        /// creates a new <see cref="OperatorNode"/>
        /// </summary>
        /// <param name="character">character of node</param>
        public OperatorNode(char character) {
            Character = character;
        }

        /// <summary>
        /// access to children of node
        /// </summary>
        /// <param name="character">character identifying the child</param>
        /// <returns>node mapping to the specified character</returns>
        public OperatorNode this[char character] {
            get => GetChildOrDefault(character);
            set => children[character] = value;
        }

        /// <summary>
        /// character defining the leaf
        /// </summary>
        public char Character { get; }

        /// <summary>
        /// operator mapped to the node
        /// </summary>
        public Operator Operator { get; internal set; }

        /// <summary>
        /// determines whether the operator node has children
        /// </summary>
        public bool HasChildren => children.Count > 0;

        /// <summary>
        /// clears all children of this node
        /// </summary>
        public void Clear() {
            children.Clear();
        }

        /// <summary>
        /// get child if it exists in node or null if no child is found for character
        /// </summary>
        /// <param name="character">character leading to the node</param>
        /// <returns>node if any is found or null</returns>
        public OperatorNode GetChildOrDefault(char character) {
            children.TryGetValue(character, out OperatorNode value);
            return value;
        }
    }
}