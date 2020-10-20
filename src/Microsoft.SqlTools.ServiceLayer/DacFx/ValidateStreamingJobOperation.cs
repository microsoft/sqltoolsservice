//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

extern alias ASAScriptDom;

using System;
using System.Collections.Generic;
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
    /// Class to represent an in-progress deploy operation
    /// </summary>
    class ValidateStreamingJobOperation
    {
        public ValidateStreamingJobParams Parameters { get; }

        public ValidateStreamingJobOperation(ValidateStreamingJobParams parameters)
        {
            Validate.IsNotNull("parameters", parameters);
            this.Parameters = parameters;
        }

        public ValidateStreamingJobResult ValidateQuery()
        {
            try
            {
                TSqlModel model = TSqlModel.LoadFromDacpac(Parameters.PackageFilePath, new ModelLoadOptions(SqlServer.Dac.DacSchemaModelStorageType.Memory, loadAsScriptBackedModel: true));

                ASA::ParseResult referencedStreams = ParseStatement(Parameters.Statement);

                List<TSqlObject> streams = model.GetObjects(DacQueryScopes.Default, ExternalStream.TypeClass).ToList();
                HashSet<string> identifiers = streams.Select(x => x.Name.Parts[^1]).ToHashSet();

                List<string> errors = new List<string>();

                foreach (ASA::SchemaObjectName stream in referencedStreams.Inputs.Values)
                {
                    if (!identifiers.Contains(stream.BaseIdentifier.Value))
                    {
                        errors.Add(SR.StreamNotFoundInModel(SR.Input, stream.BaseIdentifier.Value));
                    }
                }

                foreach (ASA::SchemaObjectName stream in referencedStreams.Outputs.Values)
                {
                    if (!identifiers.Contains(stream.BaseIdentifier.Value))
                    {
                        errors.Add(SR.StreamNotFoundInModel(SR.Output, stream.BaseIdentifier.Value));
                    }
                }

                return new ValidateStreamingJobResult()
                {
                    Success = errors.Count == 0,
                    ErrorMessage = errors.Count == 0 ? null : String.Join(Environment.NewLine, errors)
                };
            }
            catch (Exception ex)
            {
                return new ValidateStreamingJobResult()
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

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

