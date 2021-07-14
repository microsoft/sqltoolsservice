//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.SqlAssessment.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlAssessment
{
    /// <summary>
    /// Generates a script storing SQL Assessment results to a table.
    /// </summary>
    internal sealed class GenerateScriptOperation : ITaskOperation, IDisposable
    {
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        private bool disposed = false;

        /// <summary>
        /// Gets the unique id associated with this instance.
        /// </summary>
        public string OperationId { get; set; }

        /// <summary>
        /// Gets the parameters containing assessment results
        /// to be stored in a data table.
        /// </summary>
        public GenerateScriptParams Parameters { get; }

        /// <summary>
        /// Gets or sets the error message text
        /// if an error occurred during task execution
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the sql task that's executing the operation
        /// </summary>
        public SqlTask SqlTask { get; set; }

        public GenerateScriptOperation(GenerateScriptParams parameters)
        {
            Validate.IsNotNull(nameof(parameters), parameters);
            Parameters = parameters;
        }

        /// <summary>
        /// Execute a task
        /// </summary>
        /// <param name="mode">Task execution mode (e.g. script or execute)</param>
        /// <exception cref="InvalidOperationException">
        /// The method has been called twice in parallel for the same instance.
        /// </exception>
        public void Execute(TaskExecutionMode mode)
        {
            try
            {
                var scriptText = GenerateScript(Parameters, cancellation.Token);
                if (scriptText != null)
                {
                    SqlTask?.AddScript(SqlTaskStatus.Succeeded, scriptText);
                }
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                Logger.Write(TraceEventType.Error, string.Format(
                    CultureInfo.InvariantCulture,
                    "SQL Assessment: generate script operation failed with exception {0}",
                    e.Message));

                throw;
            }
        }

        public void Cancel()
        {
            cancellation.Cancel();
        }

        #region Helpers

        internal static string GenerateScript(GenerateScriptParams generateScriptParams,
            CancellationToken cancellationToken)
        {
            const string scriptPrologue =
                @"IF (NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'AssessmentResult'))
BEGIN
    CREATE TABLE [dbo].[AssessmentResult](
    [CheckName] [nvarchar](max) NOT NULL,
    [CheckId] [nvarchar](max) NOT NULL,
    [RulesetName] [nvarchar](max) NOT NULL,
    [RulesetVersion] [nvarchar](max) NOT NULL,
    [Severity] [nvarchar](max) NOT NULL,
    [Message] [nvarchar](max) NOT NULL,
    [TargetPath] [nvarchar](max) NOT NULL,
    [TargetType] [nvarchar](max) NOT NULL,
    [HelpLink] [nvarchar](max) NOT NULL,
    [Timestamp] [datetimeoffset](7) NOT NULL
    )
END
GO
INSERT INTO [dbo].[AssessmentResult] ([CheckName],[CheckId],[RulesetName],[RulesetVersion],[Severity],[Message],[TargetPath],[TargetType],[HelpLink],[Timestamp])
VALUES";

            var sb = new StringBuilder();
            if (generateScriptParams.Items != null)
            {
                sb.Append(scriptPrologue);
                foreach (var item in generateScriptParams.Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (item.Kind == AssessmentResultItemKind.Note)
                    {
                        sb.Append(
                            $"\r\n('{CUtils.EscapeStringSQuote(item.DisplayName)}','{CUtils.EscapeStringSQuote(item.CheckId)}','{CUtils.EscapeStringSQuote(item.RulesetName)}','{item.RulesetVersion}','{item.Level}','{CUtils.EscapeStringSQuote(item.Message)}','{CUtils.EscapeStringSQuote(item.TargetName)}','{item.TargetType}','{CUtils.EscapeStringSQuote(item.HelpLink)}','{item.Timestamp:yyyy-MM-dd hh:mm:ss.fff zzz}'),");
                    }
                }

                sb.Length -= 1;
            }

            return sb.ToString();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!disposed)
            {
                Cancel();
                cancellation.Dispose();
                disposed = true;
            }
        }

        #endregion
    }
}
