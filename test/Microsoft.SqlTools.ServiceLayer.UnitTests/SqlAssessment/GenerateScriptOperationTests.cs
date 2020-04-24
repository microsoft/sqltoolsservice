//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Assessment;
using Microsoft.SqlTools.ServiceLayer.SqlAssessment;
using Microsoft.SqlTools.ServiceLayer.SqlAssessment.Contracts;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Moq;
using NUnit.Framework;
using Xunit;
using Assert = Xunit.Assert;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.SqlAssessment
{
    public class GenerateScriptOperationTests
    {
            private static readonly GenerateScriptParams SampleParams = new GenerateScriptParams
            {
                Items = new List<AssessmentResultItem>
                {
                    new AssessmentResultItem
                    {
                        CheckId = "C1",
                        Description = "Desc1",
                        DisplayName = "DN1",
                        HelpLink = "HL1",
                        Kind = AssessmentResultItemKind.Note,
                        Level = "Information",
                        Message = "Msg'1",
                        TargetName = "proj[*]_dev",
                        TargetType = SqlObjectType.Server,
                        Timestamp = new DateTimeOffset(2001, 5, 25, 13, 42, 00, TimeSpan.Zero),
                        RulesetName = "Microsoft Ruleset",
                        RulesetVersion = "1.3"
                    },
                    new AssessmentResultItem
                    {
                        CheckId = "C-2",
                        Description = "Desc2",
                        DisplayName = "D N2",
                        HelpLink = "http://HL2",
                        Kind = AssessmentResultItemKind.Warning,
                        Level = "Warning",
                        Message = "Msg'1",
                        TargetName = "proj[*]_devW",
                        TargetType = SqlObjectType.Database,
                        Timestamp = new DateTimeOffset(2001, 5, 25, 13, 42, 00, TimeSpan.FromHours(3)),
                        RulesetName = "Microsoft Ruleset",
                        RulesetVersion = "1.3"
                    },
                    new AssessmentResultItem
                    {
                        CheckId = "C'3",
                        Description = "Des'c3",
                        DisplayName = "D'N1",
                        HelpLink = "HL'1",
                        Kind = AssessmentResultItemKind.Error,
                        Level = "Critical",
                        Message = "Msg'1",
                        TargetName = "proj[*]_devE",
                        TargetType = SqlObjectType.Server,
                        Timestamp = new DateTimeOffset(2001, 5, 25, 13, 42, 00, TimeSpan.FromMinutes(-90)),
                        RulesetName = "Microsoft Ruleset",
                        RulesetVersion = "1.3"
                    },
                    new AssessmentResultItem
                    {
                        CheckId = "C-2",
                        Description = "Desc2",
                        DisplayName = "D N2",
                        HelpLink = "http://HL2",
                        Kind = AssessmentResultItemKind.Note,
                        Level = "Warning",
                        Message = "Msg'1",
                        TargetName = "proj[*]_dev",
                        TargetType = SqlObjectType.Database,
                        Timestamp = new DateTimeOffset(2001, 5, 25, 13, 42, 00, TimeSpan.FromHours(3)),
                        RulesetName = "Microsoft Ruleset",
                        RulesetVersion = "1.3"
                    },
                    new AssessmentResultItem
                    {
                        CheckId = "C'3",
                        Description = "Des'c3",
                        DisplayName = "D'N1",
                        HelpLink = "HL'1",
                        Kind = AssessmentResultItemKind.Note,
                        Level = "Critical",
                        Message = "Msg'1",
                        TargetName = "proj[*]_dev",
                        TargetType = SqlObjectType.Server,
                        Timestamp = new DateTimeOffset(2001, 5, 25, 13, 42, 00, TimeSpan.FromMinutes(-90)),
                        RulesetName = "Microsoft Ruleset",
                        RulesetVersion = "1.3"
                    }
                }
            };
            
            private const string SampleScript =
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
VALUES
('DN1','C1','Microsoft Ruleset','1.3','Information','Msg''1','proj[*]_dev','Server','HL1','2001-05-25 01:42:00.000 +00:00'),
('D N2','C-2','Microsoft Ruleset','1.3','Warning','Msg''1','proj[*]_dev','Database','http://HL2','2001-05-25 01:42:00.000 +03:00'),
('D''N1','C''3','Microsoft Ruleset','1.3','Critical','Msg''1','proj[*]_dev','Server','HL''1','2001-05-25 01:42:00.000 -01:30')";

        [Fact]
        public void GenerateScriptTest()
        {
            var scriptText = GenerateScriptOperation.GenerateScript(SampleParams, CancellationToken.None);
            Assert.Equal(SampleScript, scriptText);
        }

        [Fact]
        public void ExecuteTest()
        {
            var subject = new GenerateScriptOperation(SampleParams);
            var taskMetadata = new TaskMetadata();
            using (var sqlTask = new SqlTask(taskMetadata, DummyOpFunction, DummyOpFunction))
            {
                subject.SqlTask = sqlTask;
                sqlTask.ScriptAdded += ValidateScriptAdded;
                subject.Execute(TaskExecutionMode.Script);
            }
        }

        private void ValidateScriptAdded(object sender, TaskEventArgs<TaskScript> e)
        {
            Assert.Equal(SqlTaskStatus.Succeeded, e.TaskData.Status);
            Assert.Equal(SampleScript, e.TaskData.Script);
        }

        private static Task<TaskResult> DummyOpFunction(SqlTask _)
        {
            return Task.FromResult(new TaskResult() {TaskStatus = SqlTaskStatus.Succeeded});
        }
    }
}
