//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.IO;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Linq;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.AutoParameterizaition.Exceptions;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.AutoParameterizaition.Telemetry;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.AutoParameterizaition
{
    public class SqlParameterizer
    {
        private const int maxStringLength = 300000;// Approximately 600 Kb
        private static readonly IList<CodeSenseItem> EmptyCodeSenseItemList = Enumerable.Empty<CodeSenseItem>().ToList();

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
        public void Parameterize(SqlCommand commandToParameterize)
        {
            bool parseSuccessful = false;

            try
            {
                TSqlFragment rootFragment = GetAbstractSyntaxTree(commandToParameterize);
                parseSuccessful = true;
                TsqlMultiVisitor multiVisitor = new TsqlMultiVisitor(isCodeSenseRequest: false); // Use the vistor pattern to examine the parse tree
                rootFragment.AcceptChildren(multiVisitor); // Now walk the tree

                //reformat and validate the transformed command
                SqlScriptGenerator scriptGenerator = GetScriptGenerator();
                scriptGenerator.GenerateScript(rootFragment, out string formattedSQL);

                commandToParameterize.CommandText = formattedSQL;
                commandToParameterize.Parameters.AddRange(multiVisitor.Parameters.ToArray());

                multiVisitor.Reset();
            }
            catch (Exception exception)
            {
                var eventProperties = new List<EventProperty>
                {
                    new EventProperty(EventProperty.EXCEPTION_TYPE, exception.GetType().ToString()),
                    new EventProperty(EventProperty.PARSE_SUCCESSFUL, parseSuccessful.ToString())
                };

                if (exception is ParameterizationFormatException parameterizationFormatException)
                {
                    eventProperties.Add(new EventProperty(EventProperty.LITERAL_SQL_DATA_TYPE, parameterizationFormatException.SqlDatatype));
                    eventProperties.Add(new EventProperty(EventProperty.LITERAL_CSHARP_DATA_TYPE, parameterizationFormatException.CSharpDataType));
                }
                else
                {
                    if (exception is ParameterizationScriptTooLargeException largeException)
                    {
                        eventProperties.Add(new EventProperty(EventProperty.SCRIPT_CHAR_LENGTH, largeException.ScriptLength.ToString()));
                    }
                }

                throw;
            }
        }

        /// <summary>
        /// Parses the given script to provide message, warning, error.
        /// </summary>
        /// <param name="scriptToParse">Script that will be parsed</param>
        /// <param name="telemetryManager">Used to emit telemetry events</param>
        /// <returns></returns>
        public virtual IList<CodeSenseItem> CodeSense(string scriptToParse)
        {
            if (scriptToParse == null)
            {
                return EmptyCodeSenseItemList;
            }

            int CurrentScriptlength = scriptToParse.Length;
            if (CurrentScriptlength >= maxStringLength)
            {
                CodeSenseItem maxStringLengthCodeSenseItem = new CodeSenseItem(SR.ScriptTooLarge(maxStringLength, CurrentScriptlength),
                                                                    startRow: 1, startCol: 1,
                                                                    endRow: 2, endCol: 1,
                                                                    type: CodeSenseItem.CodeSenseItemType.Error
                                                                );
                return new CodeSenseItem[] { maxStringLengthCodeSenseItem };
            }

            bool parseSuccessful = false;
            try
            {
                TSqlFragment rootFragment = GetAbstractSyntaxTree(scriptToParse);
                parseSuccessful = true;
                TsqlMultiVisitor multiVisitor = new TsqlMultiVisitor(isCodeSenseRequest: true); // Use the vistor pattern to examine the parse tree
                rootFragment.AcceptChildren(multiVisitor); // Now walk the tree

                if (multiVisitor.CodeSenseErrors != null && multiVisitor.CodeSenseErrors.Count != 0)
                {
                    multiVisitor.CodeSenseMessages.AddRange(multiVisitor.CodeSenseErrors);
                }

                return multiVisitor.CodeSenseMessages;
            }
            catch (Exception exception)
            {
                //If parsing is unsuccessful, the user might have entered incorrect syntax so we avoid emmiting telemetry for these exceptions
                if (parseSuccessful)
                {
                    List<EventProperty> eventProperties = new List<EventProperty>
                    {
                        new EventProperty(EventProperty.EXCEPTION_TYPE, exception.GetType().ToString())
                    };


                    if (exception is ParameterizationFormatException parameterizationFormatException)
                    {
                        eventProperties.Add(new EventProperty(EventProperty.LITERAL_SQL_DATA_TYPE, parameterizationFormatException.SqlDatatype));
                        eventProperties.Add(new EventProperty(EventProperty.LITERAL_CSHARP_DATA_TYPE, parameterizationFormatException.CSharpDataType));
                    }
                }

                return new List<CodeSenseItem>();
            }
        }

        private TSqlFragment GetAbstractSyntaxTree(SqlCommand commandToParameterize)
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

        private TSqlFragment GetAbstractSyntaxTree(string script)
        {
            TextReader textReader = new StringReader(script);

            TSqlParser parser = GetTSqlParser(true);
            TSqlFragment rootFragment = parser.Parse(textReader, out IList<ParseError> parsingErrors); // Get the parse tree
            textReader.Dispose(); // clean up resources

            // if we could not parse the SQL we will throw an exception. Better here than on the server
            if (parsingErrors.Count > 0)
            {
                throw new ParameterizationParsingException(
                                             parsingErrors[0].Line,
                                             parsingErrors[0].Column,
                                             parsingErrors[0].Message);
            }

            return rootFragment;
        }
    }
}
