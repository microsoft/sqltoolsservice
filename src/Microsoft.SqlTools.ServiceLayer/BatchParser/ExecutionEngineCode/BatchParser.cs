//------------------------------------------------------------------------------
// <copyright file="BatchParser.cs" company="Microsoft">
//	 Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using Microsoft.SqlTools.ServiceLayer.BatchParser;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser.ExecutionEngineCode
{
    internal class BatchParser : 
        ICommandHandler, 
        IVariableResolver
    {
        #region Public delegates
        public  delegate void HaltParserDelegate();
        public delegate void ScriptMessageDelegate(String message);
        public delegate void ScriptErrorDelegate(String message, ScriptMessageType messageType);
        public delegate bool ExecuteDelegate(String batchScript, int num, int lineNumber);        
        #endregion

        #region Constructors / Destructor
        public BatchParser()
        {
        }
        #endregion

        #region Public properties
        public ScriptMessageDelegate Message
        {
            get { return _scriptMessageDelegate; }
            set { _scriptMessageDelegate = value; }
        }

        public ScriptErrorDelegate ErrorMessage
        {
            get { return _scriptErrorDelegate; }
            set { _scriptErrorDelegate = value; }
        }
        
        public ExecuteDelegate Execute
        {
            get { return _executeDelegate; }
            set { _executeDelegate = value; }
        }

        public HaltParserDelegate HaltParser
        {
            get { return _haltParserDelegate; }
            set { _haltParserDelegate = value; }
        }

        public int StartingLine
        {
            get { return _startingLine; }
            set { _startingLine = value; }
        }

        #endregion

        #region ICommandHandler Members

        public BatchParserAction Go(TextBlock batch, int repeatCount)
        {
            String str;
            LineInfo lineInfo;

            batch.GetText(!_variableSubstitutionDisabled, out str, out lineInfo);

            bool executeResult = false;
            if (_executeDelegate != null)
            {
                executeResult = _executeDelegate(str, repeatCount, lineInfo.GetStreamPositionForOffset(0).Line + _startingLine - 1);
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
        protected void RaiseScriptError(String message, ScriptMessageType messageType)
        {
            if (_scriptErrorDelegate != null)
            {
                _scriptErrorDelegate(message, messageType);
            }
        }

        /// <summary>
        /// Called on parsing info message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messageType"></param>
        protected void RaiseScriptMessage(String message)
        {
            if (_scriptMessageDelegate != null)
            {
                _scriptMessageDelegate(message);
            }
        }

        /// <summary>
        /// Called on parsing info message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messageType"></param>
        protected void RaiseHaltParser()
        {
            if (_haltParserDelegate != null)
            {
                _haltParserDelegate();
            }
        }
        #endregion

        #region Private fields
        protected ScriptMessageDelegate _scriptMessageDelegate;
        protected ScriptErrorDelegate _scriptErrorDelegate;
        protected ExecuteDelegate _executeDelegate;
        protected HaltParserDelegate _haltParserDelegate;
        private int _startingLine = 0;
        protected bool _variableSubstitutionDisabled = false;
        
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


        internal void DisableVariableSubstitution()
        {
            _variableSubstitutionDisabled = true;
        }
    }
}
