//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

extern alias ASAScriptDom;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Microsoft.SqlTools.ServiceLayer.DacFx.Contracts;
using Microsoft.SqlTools.Utility;

using ASA = ASAScriptDom::Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Microsoft.SqlTools.ServiceLayer.DacFx
{
    /// <summary>
    /// Class to represent
    /// </summary>
    class ParseTSQlOperation
    {
        public ParseTSqlParams Parameters { get; }

        public ParseTSQlOperation(ParseTSqlParams parameters)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public ParseTSqlResult Parse()
        {
            try
            {
                TSqlParser parser = new TSql150Parser(initialQuotedIdentifiers: true);

                TSqlFragment fragment = parser.Parse(new StringReader(Parameters.ObjectTsql), out IList<ParseError> errors);

                if (((TSqlScript)fragment).Batches.Count != 1)
                {
                    throw new ArgumentException(SR.FragmentShouldHaveOnlyOneBatch);
                }

                TSqlBatch batch = ((TSqlScript)fragment).Batches[0];
                TSqlStatement statement = batch.Statements[0];

                CreateTableStatement createStatement = statement as CreateTableStatement;

                if (createStatement == null)
                {
                    return new ParseTSqlResult()
                    {
                        Success = false,
                        objectName = null,
                        isTable = false
                    };
                }

                // wrap each part of the name in brackets

                return new ParseTSqlResult()
                {
                    Success = true,
                    objectName = string.Join('.', createStatement.SchemaObjectName.Identifiers.Select(x => string.Format("[{0}]", x.Value)).ToArray()),
                    isTable = true
                };
            }
            catch (Exception ex)
            {
                return new ParseTSqlResult()
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    isTable = false,
                    objectName = null
                };
            }
        }

        /// <summary>
        /// Extracts the streaming job's name and transformation statement/query from the TSQL script
        /// </summary>
        /// <param name="createStreamingJobTsql"></param>
        /// <returns></returns>
        //private (string JobName, string JobStatement) ExtractStreamingJobData(string createStreamingJobTsql)
        //{
        //    TSqlParser parser = new TSql150Parser(initialQuotedIdentifiers: true);

        //    TSqlFragment fragment = parser.Parse(new StringReader(createStreamingJobTsql), out IList<ParseError> errors);

        //    if (((TSqlScript)fragment).Batches.Count != 1)
        //    {
        //        throw new ArgumentException(SR.FragmentShouldHaveOnlyOneBatch);
        //    }

        //    TSqlBatch batch = ((TSqlScript)fragment).Batches[0];
        //    TSqlStatement statement = batch.Statements[0];

        //    CreateTableStatement createStatement = statement as CreateTableStatement;

        //    // if the TSQL doesn't contain a CreateExternalStreamingJobStatement, we're in a bad path.

        //    if (createStatement == null)
        //    {
        //        throw new ArgumentException(SR.NoCreateStreamingJobStatementFound);
        //    }

        //    return (createStatement.Name.Value, createStatement.Statement.Value);
        //}

        private ASA::ParseResult ParseStatement(string query)
        {
            ASA::TSqlNRTParser parser = new ASA::TSqlNRTParser(initialQuotedIdentifiers: true);
            ASA::ParseResult result;

            try
            {
                ASA::TSqlFragmentExtensions.Parse(parser, new StringReader(query), out result);
            }
            catch (Exception arg)
            {
                Console.WriteLine($"Failed to parse query. [{arg}]");
                throw;
            }

            return result;
        }
    }
}

