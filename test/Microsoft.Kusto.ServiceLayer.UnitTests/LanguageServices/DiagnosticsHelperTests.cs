using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Kusto.ServiceLayer.LanguageServices;
using Microsoft.Kusto.ServiceLayer.LanguageServices.Contracts;
using Microsoft.Kusto.ServiceLayer.Workspace.Contracts;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Moq;
using NUnit.Framework;

namespace Microsoft.Kusto.ServiceLayer.UnitTests.LanguageServices
{
    public class DiagnosticsHelperTests
    {
        [TestCase("")]
        [TestCase(null)]
        public void ClearScriptDiagnostics_Throws_Exception_InvalidUri(string uri)
        {
            Assert.ThrowsAsync<ArgumentException>(() => DiagnosticsHelper.ClearScriptDiagnostics(uri, new EventContext()));
        }

        [Test]
        public void ClearScriptDiagnostics_Throws_Exception_InvalidEventContext()
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => DiagnosticsHelper.ClearScriptDiagnostics("uri", null));
        }

        [Test]
        public void ClearScriptDiagnostics_SendsEvent_ValidParams()
        {
            var uri = "uri";
            var eventContextMock = new Mock<EventContext>();
            var task = DiagnosticsHelper.ClearScriptDiagnostics(uri, eventContextMock.Object);
            task.Wait();
            
            eventContextMock.Verify(
                e => e.SendEvent(PublishDiagnosticsNotification.Type,
                    It.Is<PublishDiagnosticsNotification>(x => x.Uri == uri && x.Diagnostics.Length == 0)), Times.Once);
        }

        [TestCase(ScriptFileMarkerLevel.Error, DiagnosticSeverity.Error)]
        [TestCase(ScriptFileMarkerLevel.Warning, DiagnosticSeverity.Warning)]
        [TestCase(ScriptFileMarkerLevel.Information, DiagnosticSeverity.Information)]
        public async Task PublishScriptDiagnostics_Maps_Severity(ScriptFileMarkerLevel markerLevel, DiagnosticSeverity expected)
        {
            var uri = "uri";
            var scriptFile = new ScriptFile("", uri, "");
            ScriptFileMarker[] semanticMarkers =
            {
                new ScriptFileMarker
                {
                    Level = markerLevel,
                    ScriptRegion = new ScriptRegion
                    {
                        StartLineNumber = 1,
                        StartColumnNumber = 1,
                        EndLineNumber = 2,
                        EndColumnNumber = 2
                    }
                }
            };

            var actualEventType = new EventType<PublishDiagnosticsNotification>();
            var actualNotification = new PublishDiagnosticsNotification();
            var eventContextMock = new Mock<EventContext>();
            eventContextMock.Setup(x => x.SendEvent(It.IsAny<EventType<PublishDiagnosticsNotification>>(),
                    It.IsAny<PublishDiagnosticsNotification>()))
                .Callback<EventType<PublishDiagnosticsNotification>, PublishDiagnosticsNotification>(
                    (eventType, notification) =>
                    {
                        actualEventType = eventType;
                        actualNotification = notification;
                    })
                .Returns(Task.FromResult(0));
            await DiagnosticsHelper.PublishScriptDiagnostics(scriptFile, semanticMarkers, eventContextMock.Object);

            eventContextMock.Verify(e => e.SendEvent(PublishDiagnosticsNotification.Type,
                It.IsAny<PublishDiagnosticsNotification>()), Times.Once);
            
            Assert.AreEqual(PublishDiagnosticsNotification.Type.MethodName, actualEventType.MethodName);
            Assert.AreEqual(uri, actualNotification.Uri);
            Assert.AreEqual(1, actualNotification.Diagnostics.Length);
            
            var diagnostic = actualNotification.Diagnostics.First();
            Assert.AreEqual(expected, diagnostic.Severity);
        }
        
        [Test]
        public async Task PublishScriptDiagnostics_Creates_Diagnostic()
        {
            var uri = "uri";
            var scriptFile = new ScriptFile("", uri, "");
            var fileMarker = new ScriptFileMarker
            {
                Message = "Message",
                Level = ScriptFileMarkerLevel.Information,
                ScriptRegion = new ScriptRegion
                {
                    StartLineNumber = 1,
                    StartColumnNumber = 1,
                    EndLineNumber = 2,
                    EndColumnNumber = 2
                }
            };

            var actualEventType = new EventType<PublishDiagnosticsNotification>();
            var actualNotification = new PublishDiagnosticsNotification();
            var eventContextMock = new Mock<EventContext>();
            eventContextMock.Setup(x => x.SendEvent(It.IsAny<EventType<PublishDiagnosticsNotification>>(),
                    It.IsAny<PublishDiagnosticsNotification>()))
                .Callback<EventType<PublishDiagnosticsNotification>, PublishDiagnosticsNotification>(
                    (eventType, notification) =>
                    {
                        actualEventType = eventType;
                        actualNotification = notification;
                    })
                .Returns(Task.FromResult(0));
            
            await DiagnosticsHelper.PublishScriptDiagnostics(scriptFile, new[] {fileMarker}, eventContextMock.Object);
            
            Assert.AreEqual(PublishDiagnosticsNotification.Type.MethodName, actualEventType.MethodName);
            Assert.AreEqual(uri, actualNotification.Uri);
            Assert.AreEqual(1, actualNotification.Diagnostics.Length);
            
            var diagnostic = actualNotification.Diagnostics.First();
            Assert.AreEqual(null, diagnostic.Code);
            Assert.AreEqual(fileMarker.Message, diagnostic.Message);
            Assert.AreEqual(0, diagnostic.Range.Start.Character);
            Assert.AreEqual(0, diagnostic.Range.Start.Line);
            Assert.AreEqual(1, diagnostic.Range.End.Character);
            Assert.AreEqual(1, diagnostic.Range.End.Line);
            Assert.AreEqual(DiagnosticSeverity.Information, diagnostic.Severity);
        }
    }
}