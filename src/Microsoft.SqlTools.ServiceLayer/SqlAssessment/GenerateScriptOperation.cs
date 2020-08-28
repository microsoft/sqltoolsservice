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
    [CheckName] [nvarchar](max),
    [CheckId] [nvarchar](max),
    [RulesetName] [nvarchar](max),
    [RulesetVersion] [nvarchar](max),
    [Severity] [nvarchar](max),
    [Message] [nvarchar](max),
    [TargetPath] [nvarchar](max),
    [TargetType] [nvarchar](max),
    [HelpLink] [nvarchar](max),
    [Timestamp] [datetimeoffset](7)
    )
END
GO
INSERT INTO [dbo].[AssessmentResult] ([CheckName],[CheckId],[RulesetName],[RulesetVersion],[Severity],[Message],[TargetPath],[TargetType],[HelpLink],[Timestamp])
    SELECT rpt.[CheckName],rpt.[CheckId],rpt.[RulesetName],rpt.[RulesetVersion],rpt.[Severity],rpt.[Message],rpt.[TargetPath],rpt.[TargetType],rpt.[HelpLink],rpt.[Timestamp]
    FROM (VALUES ";

        const string scriptEpilogue = 
        @"
    ) rpt([CheckName],[CheckId],[RulesetName],[RulesetVersion],[Severity],[Message],[TargetPath],[TargetType],[HelpLink],[Timestamp])";

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
                            $@"
        ('{CUtils.EscapeStringSQuote(item.DisplayName)}','{CUtils.EscapeStringSQuote(item.CheckId)}','{CUtils.EscapeStringSQuote(item.RulesetName)}','{item.RulesetVersion}','{item.Level}','{CUtils.EscapeStringSQuote(item.Message)}','{CUtils.EscapeStringSQuote(item.TargetName)}','{item.TargetType}','{CUtils.EscapeStringSQuote(item.HelpLink)}','{item.Timestamp:yyyy-MM-dd hh:mm:ss.fff zzz}'),");
                    }
                }

                sb.Length -= 1;

                sb.Append(scriptEpilogue);
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
