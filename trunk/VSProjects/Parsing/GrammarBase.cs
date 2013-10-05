﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing
{
    public abstract class GrammarBase
    {
        /// <summary>
        /// Set of punctuation - terminals with punctuation data will be removed from parsed tree
        /// </summary>
        private readonly HashSet<string> _punctuation = new HashSet<string>();

        /// <summary>
        /// Set of transient nodes - non terminals marked as transient will be removed from parsed tree
        /// </summary>
        private readonly HashSet<NonTerminal> _transient = new HashSet<NonTerminal>();

        /// <summary>
        /// First sets indexed by terminals
        /// <remarks>When processing non terminal, at least one of first sets element has to be ready on input to match it.</remarks>
        /// </summary>
        private Dictionary<NonTerminal, HashSet<Terminal>> _firstSets = new Dictionary<NonTerminal, HashSet<Terminal>>();

        /// <summary>
        /// Root non terminal of grammar
        /// </summary>
        internal protected NonTerminal Root { get; protected set; }

        /// <summary>
        /// Determine that grammar has already been builded
        /// </summary>
        internal bool IsBuilded { get; private set; }

        /// <summary>
        /// Terminal which succeeds on every input (without matching any data from source)
        /// </summary>
        protected readonly Terminal Empty = new EmptyTerminal();

        /// <summary>
        /// Creates non terminal of given name
        /// </summary>
        /// <param name="name">Name of created non terminal</param>
        /// <returns>Created non terminal</returns>
        protected NonTerminal NT(string name)
        {
            return new NonTerminal(name);
        }

        /// <summary>
        /// Create terminal matching given word
        /// </summary>
        /// <param name="word">Word that will be matched by terminal</param>
        /// <param name="name">Name of terminal</param>
        /// <returns>Created terminal</returns>
        protected Terminal T(string word,string name=null)
        {
            return new KeyTerminal(word,name);
        }

        /// <summary>
        /// Create terminal matching input according to given pattern
        /// </summary>
        /// <param name="pattern">Pattern used for input matching</param>
        /// <param name="name">Name of terminal</param>
        /// <returns>Created terminal</returns>
        protected Terminal T_REG(string pattern, string name=null)
        {
            return new PatternTerminal(pattern, name);
        }

        /// <summary>
        /// Set grammar rule as optional (Question mark notation)
        /// </summary>
        /// <param name="rule">Rule that can be ommited in grammar</param>
        /// <returns>Created rule</returns>
        protected GrammarRule Q(GrammarRule rule)
        {
            return rule | Empty;
        }

        /// <summary>
        /// Make grammar rule according to regex plus notation (with optional delimiter)
        /// </summary>
        /// <param name="rule">Rule where plus notation is applied</param>
        /// <param name="delimiter">Delimiter of rule occurances</param>
        /// <returns>Created rule</returns>
        protected GrammarRule MakePlusRule(GrammarRule rule, string delimiter = null)
        {
            //TODO set delimiter
            var T = new NonTerminal(null);
            if (delimiter == null)
            {
                T.Rule = rule | (T + rule);
            }
            else
            {
                T.Rule = rule | (T + delimiter + rule);
            }
            return T;
        }

        /// <summary>
        /// Make grammar rule according to regex start notation (with optional delimiter)
        /// </summary>
        /// <param name="rule">Rule where star notation is applied</param>
        /// <param name="delimiter">Delimiter of rule occurances</param>
        /// <returns>Created rule</returns>
        protected GrammarRule MakeStarRule(GrammarRule rule,string delimiter=null)
        {
            var T = new NonTerminal(null);
            T.Rule = Empty | rule | (T + rule);
            return T;
        }


        /// <summary>
        /// Collect non terminals from rules reachable from root
        /// </summary>
        /// <param name="root">Root, where non terminals are searched</param>
        /// <returns>Collected non terminals</returns>
        internal NonTerminal[] CollectNonTerminals(NonTerminal root)
        {
            var nonTerminals = new HashSet<NonTerminal>();
            var toProcess = new Queue<NonTerminal>();
            nonTerminals.Add(root);
            toProcess.Enqueue(root);

            while (toProcess.Count > 0)
            {
                var processed = toProcess.Dequeue();

                foreach (var child in processed.Rule.NonTerminals)
                {
                    if (nonTerminals.Add(child))
                    {
                        toProcess.Enqueue(child);
                    }
                }
            }
            return nonTerminals.ToArray();
        }

        /// <summary>
        /// Determine that given non terminal is transient (skipped in parsed tree)
        /// </summary>
        /// <param name="nonTerminal"></param>
        /// <returns></returns>
        internal bool IsTransient(Term nonTerminal)
        {
            return _transient.Contains(nonTerminal);
        }

        /// <summary>
        /// Determine that given data is punctuation
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        internal bool IsPunctuation(string data)
        {
            return _punctuation.Contains(data);
        }

        protected void MarkTransient(params NonTerminal[] transientNonTerminals)
        {
            _transient.UnionWith(transientNonTerminals);
        }

        protected void MarkPunctuation(params string[] punctuation)
        {
            _punctuation.UnionWith(punctuation);
        }

        /// <summary>
        /// Build grammar 
        /// </summary>
        internal void Build()
        {
            if (IsBuilded)
                return;

            Root.IsFinal = true;

            var nonTerminals = new Queue<NonTerminal>();
            nonTerminals.Enqueue(Root);

            while (nonTerminals.Count > 0)
            {
                var nonTerminal = nonTerminals.Dequeue();
                _firstSets.Add(nonTerminal, getFirstSet(nonTerminal));

                enqueueNewNonTerminals(nonTerminal, nonTerminals);
            }

            foreach (var nonTerm in CollectNonTerminals(Root))
            {
                foreach (var seq in nonTerm.Rule.Sequences)
                {
                    seq.SetParent(nonTerm);
                }
            }
        }

        /// <summary>
        /// enqueue non terminals discovered in nonTerminal rule that doesn't beeing discovered yet
        /// </summary>
        /// <param name="nonTerminal"></param>
        /// <param name="nonTerminals"></param>
        private void enqueueNewNonTerminals(NonTerminal nonTerminal, Queue<NonTerminal> nonTerminals)
        {
            foreach (var childNonTerm in nonTerminal.Rule.NonTerminals)
            {
                if (_firstSets.ContainsKey(childNonTerm) || nonTerminals.Contains(childNonTerm))
                    continue;

                nonTerminals.Enqueue(childNonTerm);
            }
        }

        /// <summary>
        /// Get set of terminals that can be resolved as first substitutions in nonTerminal
        /// </summary>
        /// <param name="nonTerminal">Resolved nonterminal</param>
        /// <param name="visitedNonTerminals">Set of non terminals that has already been visited</param>
        /// <returns>First set</returns>
        private HashSet<Terminal> getFirstSet(NonTerminal nonTerminal, HashSet<NonTerminal> visitedNonTerminals = null)
        {
            if (visitedNonTerminals == null)
                visitedNonTerminals = new HashSet<NonTerminal>();

            visitedNonTerminals.Add(nonTerminal);

            var result = new HashSet<Terminal>();
            foreach (var sequence in nonTerminal.Rule.Sequences)
            {
                var firstTerm = sequence.Terms.First();

                switch (firstTerm.Kind)
                {
                    case TermKind.Terminal:
                        result.Add(firstTerm as Terminal);
                        break;
                    case TermKind.NonTerminal:
                        var nonTerm = firstTerm as NonTerminal;
                        if (visitedNonTerminals.Contains(nonTerm))
                            //this non terminal has already been visited
                            continue;

                        result.UnionWith(getFirstSet(nonTerm, visitedNonTerminals));
                        break;

                    default:
                        throw new NotSupportedException("Unsupported node kind");
                }
            }

            return result;
        }

    }
}