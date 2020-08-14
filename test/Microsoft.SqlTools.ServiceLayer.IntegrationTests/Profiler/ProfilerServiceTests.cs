//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Profiler;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;
using NUnit.Framework;
using System.Reflection;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using System.Linq;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Profiler
{
    public class ProfilerServiceTests
    {

        /// <summary>
        /// Verify that a start profiling request starts a profiling session
        /// </summary>
        [Test]
        public async Task TestHandleStartAndStopProfilingRequests()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {

                var connectionResult = await LiveConnectionHelper.InitLiveConnectionInfoAsync("master", queryTempFile.FilePath);
                var sqlConnection = ConnectionService.OpenSqlConnection(connectionResult.ConnectionInfo);
                SqlStoreConnection connection = new SqlStoreConnection(sqlConnection);
                var xeStore = new XEStore(connection);
                ProfilerService profilerService = new ProfilerService();
                var sessionName = await StartStandardSession(profilerService, connectionResult.ConnectionInfo.OwnerUri);
                xeStore.Sessions.Refresh();
                Assert.Multiple(() =>
                {
                    Assert.That(xeStore.Sessions.Cast<Session>().Select(s => s.Name), Has.Member(sessionName), "ProfilerService should have created the session");
                    Assert.That(xeStore.Sessions[sessionName].IsRunning, Is.True, "Session should be running when created by ProfilerService");
                });


                try
                {
                    var xeSession = xeStore.Sessions[sessionName];
                    xeSession.Stop();
                    // start a new session
                    var startParams = new StartProfilingParams
                    {
                        OwnerUri = connectionResult.ConnectionInfo.OwnerUri,
                        SessionName = sessionName
                    };

                    string sessionId = null;
                    var startContext = new Mock<RequestContext<StartProfilingResult>>();
                    startContext.Setup(rc => rc.SendResult(It.IsAny<StartProfilingResult>()))
                        .Returns<StartProfilingResult>((result) =>
                        {
                            // capture the session id for sending the stop message
                            sessionId = result.UniqueSessionId;
                            return Task.FromResult(0);
                        });

                    
                    await profilerService.HandleStartProfilingRequest(startParams, startContext.Object);
                    Assert.That(sessionId, Does.Contain(connectionResult.ConnectionInfo.ConnectionDetails.ServerName), "UniqueSessionId");
                    startContext.VerifyAll();

                    // wait a bit for the session monitoring to initialize
                    Thread.Sleep(TimeSpan.FromSeconds(30));

                    xeSession.Refresh();
                    Assert.That(xeSession.IsRunning, Is.True, "Session should be running due to HandleStartProfilingRequest");

                    // stop the session
                    var stopParams = new StopProfilingParams()
                    {
                        OwnerUri = connectionResult.ConnectionInfo.OwnerUri
                    };

                    var stopContext = new Mock<RequestContext<StopProfilingResult>>();
                    stopContext.Setup(rc => rc.SendResult(It.IsAny<StopProfilingResult>()))
                        .Returns(Task.FromResult(0));

                    await profilerService.HandleStopProfilingRequest(stopParams, stopContext.Object);

                    xeSession.Refresh();
                    Assert.That(xeSession.IsRunning, Is.False, "Session should be stopped due to HandleStopProfilingRequest");
                    stopContext.VerifyAll();
                }
                finally
                {
                    try
                    {
                        xeStore.Sessions.Refresh();
                        if (xeStore.Sessions.Contains(sessionName))
                        {
                            try
                            {
                                xeStore.Sessions[sessionName].Stop();
                            }
                            catch 
                            { }
                            xeStore.Sessions[sessionName].Drop();
                        }
                    }
                    catch
                    { }
                }
            }
        }


        private async Task<string> StartStandardSession(ProfilerService profilerService, string ownerUri)
        {
            const string sessionName = "ADS_Standard_Test";
            var template = Newtonsoft.Json.JsonConvert.DeserializeObject<ProfilerSessionTemplate>(standardSessionJson);

            var createParams = new CreateXEventSessionParams() { OwnerUri = ownerUri, SessionName = sessionName, Template = template };
            var requestContext = new Mock<RequestContext<CreateXEventSessionResult>>();
            requestContext.Setup(c => c.SendResult(It.IsAny<CreateXEventSessionResult>()))
              .Returns<CreateXEventSessionResult>((result) => { return Task.FromResult(0); });
            await profilerService.HandleCreateXEventSessionRequest(createParams, requestContext.Object);
            return sessionName;
        }

        

        const string standardSessionJson = @"{
			name: 'Standard_OnPrem',
			defaultView: 'Standard View',
			engineTypes: ['Standalone'],
			createStatement:
				'CREATE EVENT SESSION [{sessionName}] ON SERVER
					ADD EVENT sqlserver.attention(
						ACTION(package0.event_sequence,sqlserver.client_app_name,sqlserver.client_pid,sqlserver.database_id,sqlserver.nt_username,sqlserver.query_hash,sqlserver.server_principal_name,sqlserver.session_id)
						WHERE ([package0].[equal_boolean]([sqlserver].[is_system],(0)))),
					ADD EVENT sqlserver.existing_connection(SET collect_options_text=(1)
						ACTION(package0.event_sequence,sqlserver.client_app_name,sqlserver.client_pid,sqlserver.nt_username,sqlserver.server_principal_name,sqlserver.session_id)),
					ADD EVENT sqlserver.login(SET collect_options_text=(1)
						ACTION(package0.event_sequence,sqlserver.client_app_name,sqlserver.client_pid,sqlserver.nt_username,sqlserver.server_principal_name,sqlserver.session_id)),
					ADD EVENT sqlserver.logout(
						ACTION(package0.event_sequence,sqlserver.client_app_name,sqlserver.client_pid,sqlserver.nt_username,sqlserver.server_principal_name,sqlserver.session_id)),
					ADD EVENT sqlserver.rpc_completed(
						ACTION(package0.event_sequence,sqlserver.client_app_name,sqlserver.client_pid,sqlserver.database_id,sqlserver.database_name,sqlserver.nt_username,sqlserver.query_hash,sqlserver.server_principal_name,sqlserver.session_id)
						WHERE ([package0].[equal_boolean]([sqlserver].[is_system],(0)))),
					ADD EVENT sqlserver.sql_batch_completed(
						ACTION(package0.event_sequence,sqlserver.client_app_name,sqlserver.client_pid,sqlserver.database_id,sqlserver.database_name,sqlserver.nt_username,sqlserver.query_hash,sqlserver.server_principal_name,sqlserver.session_id)
						WHERE ([package0].[equal_boolean]([sqlserver].[is_system],(0)))),
					ADD EVENT sqlserver.sql_batch_starting(
						ACTION(package0.event_sequence,sqlserver.client_app_name,sqlserver.client_pid,sqlserver.database_id,sqlserver.database_name,sqlserver.nt_username,sqlserver.query_hash,sqlserver.server_principal_name,sqlserver.session_id)
						WHERE ([package0].[equal_boolean]([sqlserver].[is_system],(0))))
					ADD TARGET package0.ring_buffer(SET max_events_limit=(1000),max_memory=(51200))
					WITH (MAX_MEMORY=8192 KB,EVENT_RETENTION_MODE=ALLOW_SINGLE_EVENT_LOSS,MAX_DISPATCH_LATENCY=5 SECONDS,MAX_EVENT_SIZE=0 KB,MEMORY_PARTITION_MODE=PER_CPU,TRACK_CAUSALITY=ON,STARTUP_STATE=OFF)'
		}";
    }
}