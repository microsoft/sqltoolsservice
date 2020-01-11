//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Globalization;
using System.IO;
using Microsoft.SqlTools.ManagedBatchParser;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    /// <summary>
    /// Class for handling SQL CMD by Batch Parser
    /// </summary>
    public class BatchParserSqlCmd : BatchParser
    {
        /// <summary>
        /// The internal variables that can be used in SqlCommand substitution.
        /// These variables take precedence over environment variables.
        /// </summary>
        private Dictionary<string, string> internalVariables = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
        private ConnectionChangedDelegate connectionChangedDelegate;
        private ErrorActionChangedDelegate errorActionChangedDelegate;

        public delegate void ConnectionChangedDelegate(SqlConnectionStringBuilder connectionstringBuilder);
        public delegate void ErrorActionChangedDelegate(OnErrorAction ea);

        /// <summary>
        /// Constructor taking a Parser instance
        /// </summary>
        /// <param name="parser"></param>
        public BatchParserSqlCmd()
            : base()
        {
            // nothing
        }

        public ConnectionChangedDelegate ConnectionChanged
        {
            get { return connectionChangedDelegate; }
            set { connectionChangedDelegate = value; }
        }

        public ErrorActionChangedDelegate ErrorActionChanged
        {
            get { return errorActionChangedDelegate; }
            set { errorActionChangedDelegate = value; }
        }

        /// <summary>
        /// Looks for any environment variable or internal variable.
        /// </summary>
        public override string GetVariable(PositionStruct pos, string name)
        {
            if (variableSubstitutionDisabled)
            {
                return null;
            }

            string value;

            // Internally defined variables have higher precedence over environment variables.
            if (!internalVariables.TryGetValue(name, out value))
            {
                value = Environment.GetEnvironmentVariables()[name] as string;
            }
            if (value == null)
            {
                RaiseScriptError(string.Format(CultureInfo.CurrentCulture, SR.EE_ExecutionError_VariableNotFound, name), ScriptMessageType.FatalError);
                RaiseHaltParser();
                // TODO: Halt the parser, should get/set variable have ParserAction.Abort/Continue (like original?)
            }

            return value;
        }

        /// <summary>
        /// Set environment or internal variable
        /// </summary>
        public override void SetVariable(PositionStruct pos, string name, string value)
        {
            if (variableSubstitutionDisabled)
            {
                return;
            }

            if (value == null)
            {
                if (internalVariables.ContainsKey(name))
                {
                    internalVariables.Remove(name);
                }
            }
            else
            {
                internalVariables[name] = value;
            }
        }

        public Dictionary<string, string> InternalVariables
        {
            get { return internalVariables; }
            set { internalVariables = value; }
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "ppIBatchSource")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "fileName")]
        public override BatchParserAction Include(TextBlock filename, out TextReader stream, out string newFilename)
        {
            stream = null;
            newFilename = null;
            LineInfo lineInfo;

            if (filename == null)
            {
                stream = null;
                return BatchParserAction.Abort;
            }

            filename.GetText(resolveVariables: true, text: out newFilename, lineInfo: out lineInfo);
            string resolvedFileNameWithFullPath = GetFilePath(newFilename);

            if (!File.Exists(resolvedFileNameWithFullPath))
            {
                stream = null;
                return BatchParserAction.Abort;
            }
            else
            {
                stream = new StreamReader(resolvedFileNameWithFullPath);
            }
            return BatchParserAction.Continue;
        }

        private string GetFilePath(string fileName)
        {
            //try appending the file name with current working directory path
            string fullFileName = null;
            try
            {
                if (Environment.CurrentDirectory != null && !File.Exists(fileName))
                {
                    string currentWorkingDirectory = Environment.CurrentDirectory;
                    if (currentWorkingDirectory != null)
                    {
                        fullFileName = Path.GetFullPath(Path.Combine(currentWorkingDirectory, fileName));
                        if (!File.Exists(fullFileName))
                        {
                            fullFileName = null;
                        }
                    }

                }
                if (fullFileName == null)
                {
                    fullFileName = Path.GetFullPath(fileName);
                }

                return fullFileName;
            }
            catch (ArgumentException)
            {
                //path contains invalid path characters.
                throw new SqlCmdException(SR.SqlCmd_PathInvalid);
            }
            catch (PathTooLongException)
            {
                //path is too long
                throw new SqlCmdException(SR.SqlCmd_PathLong);
            }
            catch (Exception)
            {
                //catch all other exceptions and report generic error
                throw new SqlCmdException(string.Format(SR.SqlCmd_FailedInclude, fileName));
            }
        }

        /// <summary>
        /// Method to deal with errors
        /// </summary>
        public override BatchParserAction OnError(Token token, OnErrorAction ea)
        {
            if (errorActionChangedDelegate != null)
            {
                errorActionChangedDelegate(ea);
            }
            return BatchParserAction.Continue;
        }

    }
}
