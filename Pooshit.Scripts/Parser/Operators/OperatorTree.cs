using System;
using Pooshit.Scripting.Data;

namespace Pooshit.Scripting.Parser.Operators {

    /// <summary>
    /// tree containing operator information
    /// </summary>
    public class OperatorTree {
        readonly OperatorNode root = new OperatorNode('\0');

        /// <summary>
        /// tree trunk
        /// </summary>
        public OperatorNode Root => root;

        /// <summary>
        /// clears all entries of the tree
        /// </summary>
        public void Clear() {
            root.Clear();
        }

        /// <summary>
        /// adds an operator to the tree
        /// </summary>
        /// <param name="data">data identifying the operator</param>
        /// <param name="op">operator type</param>
        public void Add(string data, Operator op) {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentNullException(nameof(data));

            OperatorNode node = root;
            foreach (char character in data) {
                OperatorNode child = node[character];
                if (child == null)
                    node[character] = child = new OperatorNode(character);
                node = child;
            }

            node.Operator = op;
        }

        /// <summary>
        /// get the operator node matching to the data
        /// </summary>
        /// <param name="data">data making up the operator</param>
        /// <returns>node matching the operator data</returns>
        public OperatorNode Get(string data) {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentNullException(nameof(data));

            OperatorNode node = root;
            foreach (char character in data)
            {
                OperatorNode child = node[character];
                if (child == null)
                    return null;
                node = child;
            }

            return node;
        }
    }
}