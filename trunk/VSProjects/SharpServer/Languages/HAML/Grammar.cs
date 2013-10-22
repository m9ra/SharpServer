﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Parsing;

namespace SharpServer.Languages.HAML
{
    public class Grammar : GrammarBase
    {
        /// <summary>
        /// Number of spaces for one tabulator
        /// Is used for indentation
        /// </summary>
        readonly int TabWidth = 4;

        /// <summary>
        /// Special terminal for indentation
        /// </summary>
        readonly Terminal INDENT = T_SPEC(IndentOutliner.Indent);

        /// <summary>
        /// Special terminal for dedentation
        /// </summary>
        readonly Terminal DEDENT = T_SPEC(IndentOutliner.Dedent);

        /// <summary>
        /// Special terminal for end of line
        /// </summary>
        readonly Terminal EOL = T_SPEC(IndentOutliner.EOL);

        /// <summary>
        /// Nonterminal for hash expression
        /// </summary>
        readonly NonTerminal hash = NT("hash");

        /// <summary>
        /// NonTerminal for param usage
        /// </summary>
        readonly NonTerminal param = NT("param");

        /// <summary>
        /// NonTerminal for block definition
        /// </summary>
        readonly NonTerminal block = NT("block");

        /// <summary>
        /// NonTerminal for blocks definition
        /// </summary>
        readonly NonTerminal blocks = NT("blocks");

        readonly Terminal codePrefix = T_REG("[-=~]", "codePrefix");

        public Grammar()
        {
            var statement = generateStatementGrammar();
            generateTemplateGrammar(statement);

            MarkPunctuation("", "!!!", ".", "#", "%", "render", "=>", ",",
                ")", "(", "}", "{", "@", "else", "if"
                );
        }

        private NonTerminal generateStatementGrammar()
        {
            #region Terminals/Neterminals definitions

            var statement = NT("statement");
            var expression = NT("expression");
            var ifStatement = NT("ifStatement");
            var render = NT("render");

            var condition = NT("condition");
            var ifBranch = NT("ifBranch");
            var elseBranch = NT("elseBranch");
            var branch = NT("branch");

            var call = NT("call");
            var callName = NT("callName");
            var yield = NT("yield");

            var argList = NT("argList");
            var args = NT("args");

            var keyPair = NT("keyPair");
            var keyPairs = NT("keyPairs");

            var value = NT("value");

            var symbol = T_REG(":[a-zA-Z][a-zA-Z01-9_]*", "symbol");
            var shortKey = T_REG("[a-zA-Z][a-zA-Z01-9_]*:", "shortKey");
            var identifier = T_REG("[a-zA-Z][a-zA-Z01-9_]*", "identifier")
                .Exclude("yield", "if", "else");

            var numberLiteral = T_REG("/d+", "number");
            var stringLiteral = T_REG(@""" [^""]* """, "string");

            #endregion

            //statement
            statement.Rule = render | expression | ifStatement;
            render.Rule = "render" + argList;

            //if
            ifStatement.Rule = "if" + condition + ifBranch + Q(elseBranch);
            condition.Rule = expression;
            ifBranch.Rule = branch;
            elseBranch.Rule = codePrefix + "else" + branch;

            //let last EOL be consumed from parent
            branch.Rule = statement | (EOL + INDENT + blocks + DEDENT);

            //arguments
            argList.Rule = ("(" + args + ")") | args | Empty;
            args.Rule = (expression + "," + args) | expression;

            //call
            call.Rule = callName + argList;
            callName.Rule = identifier;

            //expression           
            yield.Rule = "yield" + (symbol | Empty);
            expression.Rule = yield | keyPair | symbol | value | call | identifier | param;
            keyPair.Rule = (symbol + "=>" + expression) | (shortKey + expression);
            keyPairs.Rule = MakeStarRule(keyPair, ",");

            //literals
            value.Rule = stringLiteral | numberLiteral;

            //hash
            hash.Rule = "{" + keyPairs + "}";
            return statement;
        }

        private void generateTemplateGrammar(NonTerminal statement)
        {
            #region Terminals/Neterminals definitions

            var view = NT("view");
            var doctype = NT("doctype");

            var contentBlock = NT("contentBlock");
            var containerBlock = NT("containerBlock");

            var head = NT("head");
            var content = NT("content");
            var code = NT("code");

            var attrib = NT("attribute");
            var attribs = NT("attributes");

            var extAttrib = NT("extAttribute");
            var extAttribs = NT("extAttributes");

            var id = NT("id");
            var cls = NT("class");
            var tag = NT("tag");

            var paramDeclarations = NT("paramDeclarations");
            var paramDeclaration = NT("paramDeclaration");

            var type = T_REG("\\w+", "type");
            var identifier = T_REG("[a-zA-Z_][a-zA-Z01-9_]*", "identifier");
            var rawOutput = T_REG("[^!%#={.][^\\r\\n]*", "rawOutput");

            #endregion

            #region Grammar rules

            //block rules
            block.Rule = containerBlock | contentBlock;
            blocks.Rule = MakeStarRule(block);

            containerBlock.Rule = head + EOL + INDENT + blocks + DEDENT;
            contentBlock.Rule = (head + EOL + Q(INDENT + content + DEDENT)) | (Q(head) + content + EOL);

            //content rules
            content.Rule = code | rawOutput;
            code.Rule = codePrefix + statement;

            //head rules
            head.Rule = (tag + Q(hash) + Q(attribs)) | attribs;

            //attribute rules            
            attrib.Rule = id | cls;
            attribs.Rule = MakePlusRule(attrib);

            id.Rule = "#" + identifier;
            cls.Rule = "." + identifier;

            //tag rules
            tag.Rule = "%" + identifier;

            //root
            doctype.Rule = "!!!" + Q(identifier);
            view.Rule = BOF + paramDeclarations + Q(doctype + EOL) + blocks + EOF;
            this.Root = view;

            //params
            paramDeclarations.Rule = MakeStarRule(paramDeclaration);
            paramDeclaration.Rule = param + type + EOL;
            param.Rule = "@" + identifier;

            MarkTransient(attrib);

            #endregion
        }


        protected override IEnumerable<Token> OutlineTokens(IEnumerable<Token> tokens)
        {
            //let tokens process by standard way
            tokens = base.OutlineTokens(tokens);

            var outliner = new IndentOutliner(tokens, TabWidth);
            var result = outliner.Outline();

            return result;
        }
    }
}
