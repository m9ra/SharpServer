﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Irony.Parsing;

namespace SharpServer.Compiling
{
    class CompilerBase
    {
        #region AST utilities

        protected string getName(ParseTreeNode node)
        {
            if (node == null)
                return null;

            return node.Term.Name;
        }

        protected ParseTreeNode getChild(string name, ParseTreeNode parent)
        {
            foreach (var child in parent.ChildNodes)
            {
                var named = skipUnnamedChildren(child);
                if (named == null)
                    break;

                if (getName(named) == name)
                    return named;
            }

            return null;
        }

        /// <summary>
        /// Step to single child of parent. Skips unnamed nodes
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        protected ParseTreeNode stepToChild(ParseTreeNode parent)
        {
            var count = parent.ChildNodes.Count;
            switch (count)
            {
                case 0:
                    return null;
                case 1:
                    return skipUnnamedChildren(parent.ChildNodes[0]);
                default:
                    throw new NotSupportedException("Cannot step to child, invalid child count");
            }
        }

        protected string getChildName(ParseTreeNode parent)
        {
            var child = stepToChild(parent);

            return getName(child);
        }

        protected IEnumerable<ParseTreeNode> getChilds(string selector, ParseTreeNode parent)
        {
            var pathParts = selector.Split('.');
            var targetName = pathParts.Last();
            var path = pathParts.Take(pathParts.Length - 1);

            //get node where children will be searched
            var node = getNode(parent, path.ToArray());

            node = skipUnnamedChildren(node);

            var result = new List<ParseTreeNode>();

            foreach (var child in node.ChildNodes)
            {
                if (getName(child) == targetName)
                {
                    result.Add(child);
                }
            }

            return result;
        }

        protected ParseTreeNode skipUnnamedChildren(ParseTreeNode node)
        {
            while (!hasName(node))
            {
                var count = node.ChildNodes.Count;
                switch (count)
                {
                    case 0:
                        return null;
                    case 1:
                        node = node.ChildNodes[0];
                        break;
                    default:
                        throw new NotSupportedException("Unsupported node");
                }
            }

            return node;
        }

        protected ParseTreeNode findNode(ParseTreeNode root, string name)
        {
            var nodes = findNodes(root, name);
            if (nodes.Any())
                return nodes.First();

            return null;
        }

        protected IEnumerable<ParseTreeNode> findNodes(ParseTreeNode root, string name)
        {
            var queue = new Queue<ParseTreeNode>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();

                node = skipUnnamedChildren(node);
                if (node == null)
                {
                    continue;
                }

                if (getName(node) == name)
                    yield return node;

                foreach (var child in node.ChildNodes)
                    queue.Enqueue(child);
            }
        }

        protected ParseTreeNode getNode(ParseTreeNode root, params string[] pathParts)
        {
            var node = root;
            foreach (var pathPart in pathParts)
            {
                node = skipUnnamedChildren(node);

                bool found = false;
                foreach (var child in node.ChildNodes)
                {
                    var name = getName(skipUnnamedChildren(child));

                    if (name == pathPart)
                    {
                        node = child;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    throw new KeyNotFoundException("Node hasn't been found");
                }
            }
            return node;
        }

        protected bool hasName(ParseTreeNode node)
        {
            return !getName(node).StartsWith("Unnamed");
        }

        protected string[] findTerminals(string name, ParseTreeNode parent, string defaultValue = null)
        {
            var nodes = new Stack<ParseTreeNode>();
            nodes.Push(parent);

            var terminals = new List<string>();
            while (nodes.Count > 0)
            {
                var node = nodes.Pop();
                foreach (var child in node.ChildNodes)
                {
                    nodes.Push(child);
                }

                if (getName(node) == name)
                {
                    terminals.Add(node.ChildNodes[0].Token.Text);
                }
            }

            if (terminals.Count == 0 && defaultValue != null)
            {
                terminals.Add(defaultValue);
            }

            terminals.Reverse();
            return terminals.ToArray();
        }

        protected string getTerminalText(ParseTreeNode terminal)
        {
            if (terminal.Token == null)
            {
                return null;
            }
            return terminal.Token.Text;
        }

        protected string getSubTerminal(ParseTreeNode node, string defaultValue = null, params string[] pathParts)
        {
            node = getNode(node, pathParts);

            if (node.ChildNodes.Count != 1)
            {
                return defaultValue;
            }
            var termNode = node.ChildNodes[0];
            if (termNode.Token == null)
                //is not terminal
                return defaultValue;
            return getTerminalText(termNode);
        }

        protected string[] getTerminals(string name, ParseTreeNode parent, string defaultValue = null)
        {
            if (parent == null)
                throw new ArgumentNullException("parent");

            var nodes = getChilds(name, parent);
            var terminals = (from node in nodes select getSubTerminal(node)).ToArray();

            if (terminals.Length == 0 && defaultValue != null)
            {
                return new string[] { defaultValue };
            }

            return terminals;
        }
        #endregion
    }
}