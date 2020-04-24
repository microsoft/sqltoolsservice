//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.AutoParameterizaition.Exceptions;
using System.Data.Common;
using Microsoft.SqlTools.ServiceLayer.Workspace.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.AutoParameterizaition
{
    public static class SqlParameterizer
    {
        private const int maxStringLength = 300000;// Approximately 600 Kb
        private static readonly IList<ScriptFileMarker> EmptyCodeSenseItemList = Enumerable.Empty<ScriptFileMarker>().ToList();

        public static SqlScriptGenerator GetScriptGenerator()
        {
            return new Sql150ScriptGenerator();
        }

        public static TSqlParser GetTSqlParser(bool initialQuotedIdentifiers)
        {
            return new TSql150Parser(initialQuotedIdentifiers);
        }

        /// <summary>
        /// This method will parameterize the given SqlCommand.
        /// Any single literal on the RHS of a declare statement will be parameterized
        /// Any other literals will be ignored
        /// </summary>
        /// <param name="commandToParameterize">Command that will need to be parameterized</param>
        public static void Parameterize(this DbCommand commandToParameterize)
        {         
            TSqlFragment rootFragment = GetAbstractSyntaxTree(commandToParameterize);
            TsqlMultiVisitor multiVisitor = new TsqlMultiVisitor(isCodeSenseRequest: false); // Use the vistor pattern to examine the parse tree
            rootFragment.AcceptChildren(multiVisitor); // Now walk the tree

            //reformat and validate the transformed command
            SqlScriptGenerator scriptGenerator = GetScriptGenerator();
            scriptGenerator.GenerateScript(rootFragment, out string formattedSQL);

            commandToParameterize.CommandText = formattedSQL;
            commandToParameterize.Parameters.AddRange(multiVisitor.Parameters.ToArray());

            multiVisitor.Reset();            
        }

        /// <summary>
        /// Parses the given script to provide message, warning, error.
        /// </summary>
        /// <param name="scriptToParse">Script that will be parsed</param>
        /// <param name="telemetryManager">Used to emit telemetry events</param>
        /// <returns></returns>
        public static IList<ScriptFileMarker> CodeSense(string scriptToParse)
        {
            if (scriptToParse == null)
            {
                return EmptyCodeSenseItemList;
            }

            int CurrentScriptlength = scriptToParse.Length;
            if (CurrentScriptlength >= maxStringLength)
            {
                ScriptFileMarker maxStringLengthCodeSenseItem = new ScriptFileMarker
                {
                    Level = ScriptFileMarkerLevel.Error,
                    Message = SR.ScriptTooLarge(maxStringLength, CurrentScriptlength),
                    ScriptRegion = new ScriptRegion
                    {
                        StartLineNumber = 1,
                        StartColumnNumber = 1,
                        EndLineNumber = 2,
                        EndColumnNumber = 1
                    }
                };

                return new ScriptFileMarker[] { maxStringLengthCodeSenseItem };
            }

            TSqlFragment rootFragment = GetAbstractSyntaxTree(scriptToParse);
            TsqlMultiVisitor multiVisitor = new TsqlMultiVisitor(isCodeSenseRequest: true); // Use the vistor pattern to examine the parse tree
            rootFragment.AcceptChildren(multiVisitor); // Now walk the tree

            if (multiVisitor.CodeSenseErrors != null && multiVisitor.CodeSenseErrors.Count != 0)
            {
                multiVisitor.CodeSenseMessages.AddRange(multiVisitor.CodeSenseErrors);
            }

            return multiVisitor.CodeSenseMessages;            
        }

        private static TSqlFragment GetAbstractSyntaxTree(DbCommand commandToParameterize)
        {
            // Capture the current CommandText in a format that the parser can work with
            string commandText = commandToParameterize.CommandText;
            int currentScriptLength = commandText.Length;

            if (currentScriptLength > maxStringLength)
            {
                throw new ParameterizationScriptTooLargeException(currentScriptLength, errorMessage: SR.ScriptTooLarge(maxStringLength, currentScriptLength));
            }

            return GetAbstractSyntaxTree(commandText);
        }

        private static TSqlFragment GetAbstractSyntaxTree(string script)
        {
            using (TextReader textReader = new StringReader(script))
            {
                TSqlParser parser = GetTSqlParser(true);
                TSqlFragment rootFragment = parser.Parse(textReader, out IList<ParseError> parsingErrors); // Get the parse tree

                // if we could not parse the SQL we will throw an exception. Better here than on the server
                if (parsingErrors.Count > 0)
                {
                    throw new ParameterizationParsingException(parsingErrors[0].Line, parsingErrors[0].Column, parsingErrors[0].Message);
                }

                return rootFragment;
            }
        }
    }
}
