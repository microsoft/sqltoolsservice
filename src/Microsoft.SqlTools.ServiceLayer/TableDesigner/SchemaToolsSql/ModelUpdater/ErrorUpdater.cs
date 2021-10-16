//------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Data.Tools.Components.Diagnostics;
using Microsoft.Data.Tools.Schema.Sql.Common;
using Microsoft.Data.Tools.Schema.Sql.Validation;

namespace Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlServer.ModelUpdater
{
    internal sealed class ErrorUpdater
    {
        private abstract class ErrorProxy<T> where T : class
        {
            private IEnumerable<T> _errors;
            private IEnumerator<T> _enumerator;

            protected SqlSchemaModel Model { private set; get; }

            public bool Updated { protected set; get; }

            public ErrorProxy(IEnumerable<T> errors, SqlSchemaModel model)
            {
                _errors = errors;
                _enumerator = _errors.GetEnumerator();

                Model = model;
                Updated = false;
            }

            public T GetNextError()
            {
                if (_enumerator.MoveNext())
                {
                    return _enumerator.Current;
                }
                else
                {
                    return null;
                }
            }

            public abstract void GetLineColumn(T error, out int line, out int column);
            public abstract void SetLineColumn(T error, int line, int column);
            public abstract void RemoveError(T error);
        }

        private sealed class SchemaErrorProxy : ErrorProxy<DataSchemaError>
        {
            public SchemaErrorProxy(IEnumerable<DataSchemaError> errors, SqlSchemaModel model)
                : base(errors, model)
            {
            }

            public override void GetLineColumn(DataSchemaError error, out int line, out int column)
            {
                line = error.Line - 1; //DataSchemaError.Line is 1-based
                column = error.Column - 1; // DataSchemaError.Column is 1-based
            }

            public override void SetLineColumn(DataSchemaError error, int line, int column)
            {
                Updated = true;
                error.Line = line + 1;
                error.Column = column + 1;
            }

            public override void RemoveError(DataSchemaError error)
            {
                Updated = true;
                Model.ErrorManager.Remove(error);   
            }
        }

        private sealed class InterpreterProblemProxy : ErrorProxy<SqlValidationProblemInfo>
        {
            public InterpreterProblemProxy(IEnumerable<SqlValidationProblemInfo> problems, SqlSchemaModel model)
                : base(problems, model)
            {
            }

            public override void GetLineColumn(SqlValidationProblemInfo problem, out int line, out int column)
            {
                line = problem.StartLine - 1; // SqlValidationProblemInfo.Line is 1-based
                column = problem.StartColumn - 1; // SqlValidationProblemInfo.Column is 1-based
            }

            public override void SetLineColumn(SqlValidationProblemInfo problem, int line, int column)
            {
                Updated = true;
                Model.UpdateInterpreterProblemPosition(problem, line + 1, column + 1);
            }

            public override void RemoveError(SqlValidationProblemInfo problem)
            {
                Updated = true;
                Model.RemoveInterpreterProblem(problem);
            }
        }

        private SqlSchemaModel _model;
        private IList<DataSchemaError> _allErrors;
        private IList<SqlValidationProblemInfo> _allProblems;

        public ErrorUpdater(SqlSchemaModel model)
        {
            _model = model;
            _allErrors = _model.ErrorManager.GetAllErrors();
            _allProblems = _model.GetAllInterpreterProblems();
        }

        public void UpdateErrors(
           SqlScriptUpdateInfo updateInfo,
           string newScript,
           StringPositionConverter newScriptPositionConverter)
        {
            if (updateInfo.Updates.Any() && (_allErrors.Count > 0 || _allProblems.Count > 0))
            {
                string filepath = updateInfo.ScriptCacheIdentifier;
                IEnumerable<DataSchemaError> errors = 
                    _allErrors
                        .Where(error => filepath.Equals(error.Document, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(error => error.Line)
                        .ThenBy(error => error.Column);
                IEnumerable<SqlValidationProblemInfo> problems = 
                    _allProblems
                        .Where(error => filepath.Equals(error.FileName, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(error => error.StartLine)
                        .ThenBy(error => error.StartColumn);

                if (errors.Any() || problems.Any())
                {
                    string oldScript = _model.ScriptCache.GetScript(filepath);
                    StringPositionConverter oldScriptPositionConverter = new StringPositionConverter(oldScript);

                    if (errors.Any())
                    {
                        SchemaErrorProxy errorProxy = new SchemaErrorProxy(errors, _model);
                        UpdateErrorsImpl(updateInfo, errorProxy, oldScriptPositionConverter, newScriptPositionConverter);
                        if (errorProxy.Updated)
                        {
                            _model.ErrorManager.Refresh();
                        }
                    }

                    if (problems.Any())
                    {
                        InterpreterProblemProxy problemProxy = new InterpreterProblemProxy(problems, _model);
                        UpdateErrorsImpl(updateInfo, problemProxy, oldScriptPositionConverter, newScriptPositionConverter);
                    }
                }
            }
        }

        // the fact that this method exercises on two pointers against a string makes it less straightforward to understand,
        // and the fact that there exist two strings involving the algorithm makes complicates the situation even further.
        // 
        // one or more updates are stored inside updateInfo, each of which represents a text manipulation that replaces a portion
        // of the text with a new string. either the portion to be removed or the added string can be empty.
        //
        // each error keeps line number and column number that indicate where the error occurs.
        // 
        // both errors and updates are defined against the original text (old script). the new script represents the resultant
        // text after the updates are applied to the old script.
        //
        // this method changes the line number and column number for each error to the corresponding position of the new script.
        private void UpdateErrorsImpl<T>(
            SqlScriptUpdateInfo updateInfo,
            ErrorProxy<T> errorProxy,
            StringPositionConverter oldScriptPositionConverter,
            StringPositionConverter newScriptPositionConverter)
            where T: class
        {
            int line;
            int column;
            int errorOffset;
            T error = errorProxy.GetNextError();
            errorProxy.GetLineColumn(error, out line, out column);
            oldScriptPositionConverter.GetOffsetFromLineColumn(line, column, out errorOffset);

            IEnumerator<SqlScriptUpdateItem> updateEnumerator = updateInfo.Updates.GetEnumerator();
            updateEnumerator.MoveNext();
            SqlScriptUpdateItem update = updateEnumerator.Current;
            int updateOffset = update.StartOffset;

            int delta = 0; // keep offset change caused by the updates (excluding the current update)
            while (true)
            {
                if (errorOffset < updateOffset)
                {
                    // the error occurs before the current update

                    if (delta != 0)
                    {
                        // adjust the offset by delta (notice that the delta doesn't include the current update)
                        errorOffset += delta;
                        newScriptPositionConverter.GetLineColumnFromOffset(errorOffset, out line, out column);
                        errorProxy.SetLineColumn(error, line, column);
                    }

                    if (TryGetNextError<T>(errorProxy, oldScriptPositionConverter, out error, out errorOffset) == false)
                    {
                        // no more errors
                        break;
                    }
                }
                else if (errorOffset >= updateOffset && errorOffset < updateOffset + update.Length)
                {
                    // the error is inside the text to be deleted
                    errorProxy.RemoveError(error);

                    if (TryGetNextError<T>(errorProxy, oldScriptPositionConverter, out error, out errorOffset) == false)
                    {
                        // no more errors
                        break;
                    }
                }
                else
                {
                    // the error is beyond the current update

                    delta += update.NewText.Length - update.Length;

                    if (updateEnumerator.MoveNext())
                    {
                        update = updateEnumerator.Current;
                        SqlTracer.AssertTraceEvent(update.StartOffset >= updateOffset, TraceEventType.Error, SqlTraceId.TSqlModel, "Updates have to be sorted.");
                        updateOffset = update.StartOffset;
                    }
                    else
                    {
                        // for all the subsequent iterations of this loop, 
                        // only the first clause of the if statement (if (errorOffset < updateOffset))
                        // will be executed, which will update errors beyond the last update
                        updateOffset = int.MaxValue;
                    }
                }
            }
        }

        private static bool TryGetNextError<T>(ErrorProxy<T> errorProxy, StringPositionConverter oldScriptPositionConverter, out T error, out int errorOffset) where T : class
        {
            bool success = true;

            error = errorProxy.GetNextError();
            errorOffset = -1;
            if (error == null) // no more errors
            {
                success = false;
            }
            else
            {
                int line;
                int column;
                errorProxy.GetLineColumn(error, out line, out column);
                oldScriptPositionConverter.GetOffsetFromLineColumn(line, column, out errorOffset);
            }

            return success;
        }
    }
}
