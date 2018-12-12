//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    /// <summary>
    /// Class that parses queries into batches
    /// </summary>
    public class BatchParser : 
        ICommandHandler, 
        IVariableResolver
    {
        #region Private fields
        protected ScriptMessageDelegate scriptMessageDelegate;
        protected ScriptErrorDelegate scriptErrorDelegate;
        protected ExecuteDelegate executeDelegate;
        protected HaltParserDelegate haltParserDelegate;
        private int startingLine = 0;
        protected bool variableSubstitutionDisabled = false;
        
        #endregion

        #region Public delegates
        public  delegate void HaltParserDelegate();
        public delegate void ScriptMessageDelegate(string message);
        public delegate void ScriptErrorDelegate(string message, ScriptMessageType messageType);
        public delegate bool ExecuteDelegate(string batchScript, int num, int lineNumber);        
        #endregion

        #region Constructors / Destructor
        public BatchParser()
        {
        }
        #endregion

        #region Public properties
        public ScriptMessageDelegate Message
        {
            get { return scriptMessageDelegate; }
            set { scriptMessageDelegate = value; }
        }

        public ScriptErrorDelegate ErrorMessage
        {
            get { return scriptErrorDelegate; }
            set { scriptErrorDelegate = value; }
        }
        
        public ExecuteDelegate Execute
        {
            get { return executeDelegate; }
            set { executeDelegate = value; }
        }

        public HaltParserDelegate HaltParser
        {
            get { return haltParserDelegate; }
            set { haltParserDelegate = value; }
        }

        public int StartingLine
        {
            get { return startingLine; }
            set { startingLine = value; }
        }

        #endregion

        #region ICommandHandler Members

        /// <summary>
        /// Take approptiate action on the parsed batches
        /// </summary>
        public BatchParserAction Go(TextBlock batch, int repeatCount)
        {
            string str;
            LineInfo lineInfo;

            batch.GetText(!variableSubstitutionDisabled, out str, out lineInfo);

            bool executeResult = false;
            if (executeDelegate != null)
            {
                executeResult = executeDelegate(str, repeatCount, lineInfo.GetStreamPositionForOffset(0).Line + startingLine - 1);
            }
            return executeResult ? BatchParserAction.Continue : BatchParserAction.Abort;
        }

        #endregion

        #region Protected methods
        /// <summary>
        /// Called when the script parsing has errors/warnings
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messageType"></param>
        protected void RaiseScriptError(string message, ScriptMessageType messageType)
        {
            if (scriptErrorDelegate != null)
            {
                scriptErrorDelegate(message, messageType);
            }
        }

        /// <summary>
        /// Called on parsing info message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messageType"></param>
        protected void RaiseScriptMessage(string message)
        {
            if (scriptMessageDelegate != null)
            {
                scriptMessageDelegate(message);
            }
        }

        /// <summary>
        /// Called on parsing info message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messageType"></param>
        protected void RaiseHaltParser()
        {
            if (haltParserDelegate != null)
            {
                haltParserDelegate();
            }
        }
        #endregion

        public virtual BatchParserAction OnError(Token token, OnErrorAction action)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        public virtual BatchParserAction Include(TextBlock filename, out System.IO.TextReader stream, out string newFilename)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        public virtual string GetVariable(PositionStruct pos, string name)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        public virtual void SetVariable(PositionStruct pos, string name, string value)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        public void DisableVariableSubstitution()
        {
            variableSubstitutionDisabled = true;
        }
    }
}
