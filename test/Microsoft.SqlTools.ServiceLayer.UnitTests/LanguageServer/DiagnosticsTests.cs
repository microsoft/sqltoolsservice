//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


}
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.LanguageService.Connection.Contracts;
using Microsoft.SqlTools.LanguageService.LanguageServices;
using Microsoft.SqlTools.LanguageService.LanguageServices.Contracts;
using Microsoft.SqlTools.LanguageService.Workspace.Contracts;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.LanguageServer
{
    public class DiagnosticsTests : LanguageServiceTestBase<Diagnostic>
    {
        [Test]
        public async Task PublishSemanticMarkersAsyncLanguageChangesToSqlCmdDoesNotPublishStaleDiagnostics()
        {
            InitializeTestObjects();
            TaskCompletionSource<ScriptFileMarker[]> semanticMarkers =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock<EventContext> eventContext = new();
            eventContext
                .Setup(context => context.SendEvent(
                    PublishDiagnosticsNotification.Type,
                    It.IsAny<PublishDiagnosticsNotification>()))
                .Returns(Task.FromResult(new object()));
            Task publishTask = langService.PublishSemanticMarkersAsync(
                scriptFile.Object,
                semanticMarkers.Task,
                eventContext.Object);

            await langService.HandleDidChangeLanguageFlavorNotification(
                new LanguageFlavorChangeParams
                {
                    Uri = testScriptUri,
                    Language = TSqlLanguageService.SQL_CMD_LANG,
                    Flavor = "MSSQL"
                },
                eventContext.Object);
            semanticMarkers.SetResult(
                new[]
                {
                    new ScriptFileMarker
                    {
                        Message = "Stale diagnostic",
                        Level = ScriptFileMarkerLevel.Error,
                        ScriptRegion = new ScriptRegion
                        {
                            StartLineNumber = 1,
                            StartColumnNumber = 1,
                            EndLineNumber = 1,
                            EndColumnNumber = 2
                        }
                    }
                });

            await publishTask;

            eventContext.Verify(
                context => context.SendEvent(
                    PublishDiagnosticsNotification.Type,
                    It.Is<PublishDiagnosticsNotification>(notification => notification.Diagnostics.Length > 0)),
                Times.Never);
        }
    }
}
