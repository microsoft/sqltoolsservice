//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Main class for Agent Service functionality
    /// </summary>
    public class AgentService
    {
        private ConnectionService connectionService = null;
        private static readonly Lazy<AgentService> instance = new Lazy<AgentService>(() => new AgentService());

        /// <summary>
        /// Construct a new AgentService instance with default parameters
        /// </summary>
        public AgentService()
        {
        }

        /// <summary>
        /// Gets the singleton instance object
        /// </summary>
        public static AgentService Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Internal for testing purposes only
        /// </summary>
        internal ConnectionService ConnectionServiceInstance
        {
            get
            {
                if (connectionService == null)
                {
                    connectionService = ConnectionService.Instance;
                }
                return connectionService;
            }

            set
            {
                connectionService = value;
            }
        }

        /// <summary>
        /// Service host object for sending/receiving requests/events.
        /// Internal for testing purposes.
        /// </summary>
        internal IProtocolEndpoint ServiceHost
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes the service instance
        /// </summary>
        public void InitializeService(ServiceHost serviceHost)
        {
            this.ServiceHost = serviceHost;

            // Jobs request handlers
            this.ServiceHost.SetRequestHandler(AgentJobsRequest.Type, HandleAgentJobsRequest);
            this.ServiceHost.SetRequestHandler(AgentJobHistoryRequest.Type, HandleJobHistoryRequest);
            this.ServiceHost.SetRequestHandler(AgentJobActionRequest.Type, HandleJobActionRequest);

            this.ServiceHost.SetRequestHandler(CreateAgentJobRequest.Type, HandleCreateAgentJobRequest);
            this.ServiceHost.SetRequestHandler(UpdateAgentJobRequest.Type, HandleUpdateAgentJobRequest);
            this.ServiceHost.SetRequestHandler(DeleteAgentJobRequest.Type, HandleDeleteAgentJobRequest);

            this.ServiceHost.SetRequestHandler(AgentJobDefaultsRequest.Type, HandleAgentJobDefaultsRequest);

            // Job Steps request handlers
            this.ServiceHost.SetRequestHandler(CreateAgentJobStepRequest.Type, HandleCreateAgentJobStepRequest);
            this.ServiceHost.SetRequestHandler(UpdateAgentJobStepRequest.Type, HandleUpdateAgentJobStepRequest);
            this.ServiceHost.SetRequestHandler(DeleteAgentJobStepRequest.Type, HandleDeleteAgentJobStepRequest);

            // Alerts request handlers
            this.ServiceHost.SetRequestHandler(AgentAlertsRequest.Type, HandleAgentAlertsRequest);
            this.ServiceHost.SetRequestHandler(CreateAgentAlertRequest.Type, HandleCreateAgentAlertRequest);
            this.ServiceHost.SetRequestHandler(UpdateAgentAlertRequest.Type, HandleUpdateAgentAlertRequest);
            this.ServiceHost.SetRequestHandler(DeleteAgentAlertRequest.Type, HandleDeleteAgentAlertRequest);

            // Operators request handlers
            this.ServiceHost.SetRequestHandler(AgentOperatorsRequest.Type, HandleAgentOperatorsRequest);
            this.ServiceHost.SetRequestHandler(CreateAgentOperatorRequest.Type, HandleCreateAgentOperatorRequest);
            this.ServiceHost.SetRequestHandler(UpdateAgentOperatorRequest.Type, HandleUpdateAgentOperatorRequest);
            this.ServiceHost.SetRequestHandler(DeleteAgentOperatorRequest.Type, HandleDeleteAgentOperatorRequest);

            // Proxy Accounts request handlers
            this.ServiceHost.SetRequestHandler(AgentProxiesRequest.Type, HandleAgentProxiesRequest);
            this.ServiceHost.SetRequestHandler(CreateAgentProxyRequest.Type, HandleCreateAgentProxyRequest);
            this.ServiceHost.SetRequestHandler(UpdateAgentProxyRequest.Type, HandleUpdateAgentProxyRequest);
            this.ServiceHost.SetRequestHandler(DeleteAgentProxyRequest.Type, HandleDeleteAgentProxyRequest);

            // Schedule request handlers
            this.ServiceHost.SetRequestHandler(AgentSchedulesRequest.Type, HandleAgentSchedulesRequest);
            this.ServiceHost.SetRequestHandler(CreateAgentScheduleRequest.Type, HandleCreateAgentScheduleRequest);
            this.ServiceHost.SetRequestHandler(UpdateAgentScheduleRequest.Type, HandleUpdateAgentScheduleRequest);
            this.ServiceHost.SetRequestHandler(DeleteAgentScheduleRequest.Type, HandleDeleteAgentScheduleRequest);

        }

        #region "Jobs Handlers"

        /// <summary>
        /// Handle request to get Agent job activities
        /// </summary>
        internal async Task HandleAgentJobsRequest(AgentJobsParams parameters, RequestContext<AgentJobsResult> requestContext)
        {
            await Task.Run(async () =>
            {
                try
                {
                    var result = new AgentJobsResult();
                    ConnectionInfo connInfo;
                    ConnectionServiceInstance.TryFindConnection(
                        parameters.OwnerUri,
                        out connInfo);

                    if (connInfo != null)
                    {
                        var serverConnection = ConnectionService.OpenServerConnection(connInfo);
                        var fetcher = new JobFetcher(serverConnection);
                        var filter = new JobActivityFilter();
                        var jobs = fetcher.FetchJobs(filter);
                        var agentJobs = new List<AgentJobInfo>();
                        if (jobs != null)
                        {
                            foreach (var job in jobs.Values)
                            {
                                agentJobs.Add(AgentUtilities.ConvertToAgentJobInfo(job));
                            }
                        }
                        result.Success = true;
                        result.Jobs = agentJobs.ToArray();
                        serverConnection.SqlConnectionObject.Close();
                    }
                    await requestContext.SendResult(result);
                }
                catch (Exception e)
                {
                    await requestContext.SendError(e);
                }
            });
        }

        /// <summary>
        /// Handle request to get Agent Job history
        /// </summary>
        internal async Task HandleJobHistoryRequest(AgentJobHistoryParams parameters, RequestContext<AgentJobHistoryResult> requestContext)
        {
            await Task.Run(async () =>
            {
                try
                {
                    var result = new AgentJobHistoryResult();
                    ConnectionInfo connInfo;
                    ConnectionServiceInstance.TryFindConnection(
                        parameters.OwnerUri,
                        out connInfo);
                    if (connInfo != null)
                    {
                        CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
                        var jobServer = dataContainer.Server.JobServer;
                        var jobs = jobServer.Jobs;
                        Tuple<SqlConnectionInfo, DataTable, ServerConnection> tuple = CreateSqlConnection(connInfo, parameters.JobId);
                        SqlConnectionInfo sqlConnInfo = tuple.Item1;
                        DataTable dt = tuple.Item2;
                        ServerConnection connection = tuple.Item3;
                        
                        // Send Steps, Alerts and Schedules with job history in background
                        // Add steps to the job if any
                        JobStepCollection steps = jobs[parameters.JobName].JobSteps;
                        var jobSteps = new List<AgentJobStepInfo>();
                        foreach (JobStep step in steps)
                        {
                            jobSteps.Add(AgentUtilities.ConvertToAgentJobStepInfo(step, parameters.JobId, parameters.JobName));
                        }
                        result.Steps = jobSteps.ToArray();

                        // Add schedules to the job if any
                        JobScheduleCollection schedules = jobs[parameters.JobName].JobSchedules;
                        var jobSchedules = new List<AgentScheduleInfo>();
                        foreach (JobSchedule schedule in schedules)
                        {
                            jobSchedules.Add(AgentUtilities.ConvertToAgentScheduleInfo(schedule));
                        }
                        result.Schedules = jobSchedules.ToArray();

                        // Alerts
                        AlertCollection alerts = jobServer.Alerts;
                        var jobAlerts = new List<Alert>();
                        foreach (Alert alert in alerts)
                        {
                            if (alert.JobName == parameters.JobName)
                            {
                                jobAlerts.Add(alert);
                            }
                        }
                        result.Alerts = AgentUtilities.ConvertToAgentAlertInfo(jobAlerts);

                        // Add histories
                        int count = dt.Rows.Count;
                        List<AgentJobHistoryInfo> jobHistories = new List<AgentJobHistoryInfo>();
                        if (count > 0)
                        {
                            var job = dt.Rows[0];
                            Guid jobId = (Guid)job[AgentUtilities.UrnJobId];
                            int runStatus = Convert.ToInt32(job[AgentUtilities.UrnRunStatus], System.Globalization.CultureInfo.InvariantCulture);
                            var t = new LogSourceJobHistory(parameters.JobName, sqlConnInfo, null, runStatus, jobId, null);
                            var tlog = t as ILogSource;
                            tlog.Initialize();
                            var logEntries = t.LogEntries;

                            // Finally add the job histories
                            jobHistories = AgentUtilities.ConvertToAgentJobHistoryInfo(logEntries, job, steps);
                            result.Histories = jobHistories.ToArray();
                            result.Success = true;
                            tlog.CloseReader();
                        }
                        await requestContext.SendResult(result);
                    }
                }
                catch (Exception e)
                {
                    await requestContext.SendError(e);
                }
            });
        }

        /// <summary>
        /// Handle request to Run a Job
        /// </summary>
        internal async Task HandleJobActionRequest(AgentJobActionParams parameters, RequestContext<ResultStatus> requestContext)
        {
            await Task.Run(async () =>
            {
                var result = new ResultStatus();
                try
                {
                    ConnectionInfo connInfo;
                    ConnectionServiceInstance.TryFindConnection(
                        parameters.OwnerUri,
                        out connInfo);
                    if (connInfo != null)
                    {
                        var serverConnection = ConnectionService.OpenServerConnection(connInfo);
                        var jobHelper = new JobHelper(serverConnection);
                        jobHelper.JobName = parameters.JobName;
                        switch(parameters.Action)
                        {
                            case "run":
                                jobHelper.Start();
                                break;
                            case "stop":
                                jobHelper.Stop();
                                break;
                            case "delete":
                                jobHelper.Delete();
                                break;
                            case "enable":
                                jobHelper.Enable(true);
                                break;
                            case "disable":
                                jobHelper.Enable(false);
                                break;
                            default:
                                break;
                        }
                        result.Success = true;
                        await requestContext.SendResult(result);
                    }
                }
                catch (Exception e)
                {
                    result.Success = false;
                    result.ErrorMessage = e.Message;
                    Exception exception = e.InnerException;
                    while (exception != null)
                    {
                        result.ErrorMessage += Environment.NewLine + "\t" + exception.Message;
                        exception = exception.InnerException;
                    }
                    await requestContext.SendResult(result);
                }
            });
        }

        internal async Task HandleCreateAgentJobRequest(CreateAgentJobParams parameters, RequestContext<CreateAgentJobResult> requestContext)
        {
            var result = await ConfigureAgentJob(
                parameters.OwnerUri,
                parameters.Job.Name,
                parameters.Job,
                ConfigAction.Create,
                ManagementUtils.asRunType(parameters.TaskExecutionMode));

            await requestContext.SendResult(new CreateAgentJobResult()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }

        internal async Task HandleUpdateAgentJobRequest(UpdateAgentJobParams parameters, RequestContext<UpdateAgentJobResult> requestContext)
        {
            var result = await ConfigureAgentJob(
                parameters.OwnerUri,
                parameters.OriginalJobName,
                parameters.Job,
                ConfigAction.Update,
                ManagementUtils.asRunType(parameters.TaskExecutionMode));

            await requestContext.SendResult(new UpdateAgentJobResult()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }

        internal async Task HandleDeleteAgentJobRequest(DeleteAgentJobParams parameters, RequestContext<ResultStatus> requestContext)
        {
            var result = await ConfigureAgentJob(
               parameters.OwnerUri,
               parameters.Job.Name,
               parameters.Job,
               ConfigAction.Drop,
               ManagementUtils.asRunType(parameters.TaskExecutionMode));

            await requestContext.SendResult(new ResultStatus()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }

        internal async Task HandleCreateAgentJobStepRequest(CreateAgentJobStepParams parameters, RequestContext<CreateAgentJobStepResult> requestContext)
        {
            Tuple<bool, string> result = await ConfigureAgentJobStep(
                parameters.OwnerUri,
                parameters.Step,
                ConfigAction.Create,
                RunType.RunNow);

            await requestContext.SendResult(new CreateAgentJobStepResult()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }

        internal async Task HandleUpdateAgentJobStepRequest(UpdateAgentJobStepParams parameters, RequestContext<UpdateAgentJobStepResult> requestContext)
        {
            Tuple<bool, string> result = await ConfigureAgentJobStep(
                parameters.OwnerUri,
                parameters.Step,
                ConfigAction.Update,
                RunType.RunNow);

            await requestContext.SendResult(new UpdateAgentJobStepResult()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }

        internal async Task HandleDeleteAgentJobStepRequest(DeleteAgentJobStepParams parameters, RequestContext<ResultStatus> requestContext)
        {
            Tuple<bool, string> result = await ConfigureAgentJobStep(
                parameters.OwnerUri,
                parameters.Step,
                ConfigAction.Drop,
                RunType.RunNow);

            await requestContext.SendResult(new ResultStatus()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }

        internal async Task HandleAgentJobDefaultsRequest(AgentJobDefaultsParams parameters, RequestContext<AgentJobDefaultsResult> requestContext)
        {
            await Task.Run(async () =>
            {
                var result = new AgentJobDefaultsResult();
                try
                {
                    JobData jobData;
                    CDataContainer dataContainer;
                    CreateJobData(parameters.OwnerUri, "default", out dataContainer, out jobData);

                    // current connection user name for
                    result.Owner = dataContainer.ServerConnection.TrueLogin;

                    var categories = jobData.Categories;
                    result.Categories = new AgentJobCategory[categories.Length];
                    for (int i = 0; i < categories.Length; ++i)
                    {
                        result.Categories[i] = new AgentJobCategory
                        {
                            Id = categories[i].SmoCategory.ID,
                            Name = categories[i].SmoCategory.Name
                        };
                    }

                    result.Success = true;
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = ex.ToString();
                }

                await requestContext.SendResult(result);
            });
        }

        #endregion // "Jobs Handlers"

        #region "Alert Handlers"

        /// <summary>
        /// Handle request to get the alerts list
        /// </summary>
        internal async Task HandleAgentAlertsRequest(AgentAlertsParams parameters, RequestContext<AgentAlertsResult> requestContext)
        {
            await Task.Run(async () =>
            {
                var result = new AgentAlertsResult();
                try
                {
                    ConnectionInfo connInfo;
                    ConnectionServiceInstance.TryFindConnection(parameters.OwnerUri, out connInfo);
                    CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);

                    int alertsCount = dataContainer.Server.JobServer.Alerts.Count;
                    var alerts = new AgentAlertInfo[alertsCount];
                    for (int i = 0; i < alertsCount; ++i)
                    {
                        var alert = dataContainer.Server.JobServer.Alerts[i];
                        alerts[i] = new AgentAlertInfo
                        {
                            Id = alert.ID,
                            Name = alert.Name,
                            DelayBetweenResponses = alert.DelayBetweenResponses,
                            EventDescriptionKeyword = alert.EventDescriptionKeyword,
                            EventSource = alert.EventSource,
                            HasNotification = alert.HasNotification,
                            IncludeEventDescription = (Contracts.NotifyMethods)alert.IncludeEventDescription,
                            IsEnabled = alert.IsEnabled,
                            JobId = alert.JobID.ToString(),
                            JobName = alert.JobName,
                            LastOccurrenceDate =alert.LastOccurrenceDate.ToString(),
                            LastResponseDate = alert.LastResponseDate.ToString(),
                            MessageId = alert.MessageID,
                            NotificationMessage = alert.NotificationMessage,
                            OccurrenceCount = alert.OccurrenceCount,
                            PerformanceCondition = alert.PerformanceCondition,
                            Severity = alert.Severity,
                            DatabaseName = alert.DatabaseName,
                            CountResetDate = alert.CountResetDate.ToString(),
                            CategoryName = alert.CategoryName,
                            AlertType = (Contracts.AlertType)alert.AlertType,
                            WmiEventNamespace = alert.WmiEventNamespace,
                            WmiEventQuery = alert.WmiEventQuery
                        };
                    }

                    result.Alerts = alerts;
                    result.Success = true;
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = ex.ToString();
                }
                await requestContext.SendResult(result);
            });
        }

        /// <summary>
        /// Handle request to create an alert
        /// </summary>
        internal async Task HandleCreateAgentAlertRequest(CreateAgentAlertParams parameters, RequestContext<CreateAgentAlertResult> requestContext)
        {
            var result = await ConfigureAgentAlert(
                parameters.OwnerUri,
                parameters.Alert.Name,
                parameters.Alert,
                ConfigAction.Create,
                RunType.RunNow);

            await requestContext.SendResult(new CreateAgentAlertResult()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }

        /// <summary>
        /// Handle request to update an alert
        /// </summary>
        internal async Task HandleUpdateAgentAlertRequest(UpdateAgentAlertParams parameters, RequestContext<UpdateAgentAlertResult> requestContext)
        {
            var result = await ConfigureAgentAlert(
                parameters.OwnerUri,
                parameters.OriginalAlertName,
                parameters.Alert,
                ConfigAction.Update,
                RunType.RunNow);

            await requestContext.SendResult(new UpdateAgentAlertResult()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }

        /// <summary>
        /// Handle request to delete an alert
        /// </summary>
        internal async Task HandleDeleteAgentAlertRequest(DeleteAgentAlertParams parameters, RequestContext<ResultStatus> requestContext)
        {
            var result = await ConfigureAgentAlert(
                parameters.OwnerUri,
                parameters.Alert.Name,
                parameters.Alert,
                ConfigAction.Drop,
                RunType.RunNow);

            await requestContext.SendResult(new ResultStatus()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }

        #endregion // "Alert Handlers"

        #region "Operator Handlers"

        internal async Task HandleAgentOperatorsRequest(AgentOperatorsParams parameters, RequestContext<AgentOperatorsResult> requestContext)
        {
            await Task.Run(async () =>
            {
                var result = new AgentOperatorsResult();
                try
                {
                    ConnectionInfo connInfo;
                    ConnectionServiceInstance.TryFindConnection(parameters.OwnerUri, out connInfo);
                    CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);

                    int operatorCount = dataContainer.Server.JobServer.Operators.Count;
                    var operators = new AgentOperatorInfo[operatorCount];
                    for (int i = 0; i < operatorCount; ++i)
                    {
                        var item = dataContainer.Server.JobServer.Operators[i];
                        operators[i] = new AgentOperatorInfo
                        {
                            Name = item.Name,
                            Id = item.ID,
                            EmailAddress = item.EmailAddress,
                            Enabled = item.Enabled,
                            LastEmailDate = item.LastEmailDate.ToString(),
                            LastNetSendDate = item.LastNetSendDate.ToString(),
                            LastPagerDate = item.LastPagerDate.ToString(),
                            PagerAddress = item.PagerAddress,
                            CategoryName = item.CategoryName,
                            PagerDays = (Contracts.WeekDays)item.PagerDays,
                            SaturdayPagerEndTime = item.SaturdayPagerEndTime.ToString(),
                            SaturdayPagerStartTime = item.SaturdayPagerEndTime.ToString(),
                            SundayPagerEndTime = item.SundayPagerEndTime.ToString(),
                            SundayPagerStartTime = item.SundayPagerStartTime.ToString(),
                            NetSendAddress = item.NetSendAddress,
                            WeekdayPagerStartTime = item.WeekdayPagerStartTime.ToString(),
                            WeekdayPagerEndTime = item.WeekdayPagerEndTime.ToString()
                        };
                    }

                    result.Operators = operators;
                    result.Success = true;
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = ex.ToString();
                }
                await requestContext.SendResult(result);
            });
        }

        internal async Task HandleCreateAgentOperatorRequest(
            CreateAgentOperatorParams parameters,
            RequestContext<AgentOperatorResult> requestContext)
        {
            var result = await ConfigureAgentOperator(
                parameters.OwnerUri,
                parameters.Operator,
                ConfigAction.Create,
                RunType.RunNow);

            await requestContext.SendResult(new AgentOperatorResult()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2,
                Operator = parameters.Operator
            });
        }

        internal async Task HandleUpdateAgentOperatorRequest(
            UpdateAgentOperatorParams parameters,
            RequestContext<AgentOperatorResult> requestContext)
        {
            var result = await ConfigureAgentOperator(
                parameters.OwnerUri,
                parameters.Operator,
                ConfigAction.Update,
                RunType.RunNow);

            await requestContext.SendResult(new AgentOperatorResult()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2,
                Operator = parameters.Operator
            });
        }

        internal async Task HandleDeleteAgentOperatorRequest(
            DeleteAgentOperatorParams parameters,
            RequestContext<ResultStatus> requestContext)
        {
            var result = await ConfigureAgentOperator(
                parameters.OwnerUri,
                parameters.Operator,
                ConfigAction.Drop,
                RunType.RunNow);

            await requestContext.SendResult(new ResultStatus()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }

        #endregion // "Operator Handlers"


        #region "Proxy Handlers"

        internal async Task HandleAgentProxiesRequest(AgentProxiesParams parameters, RequestContext<AgentProxiesResult> requestContext)
        {
            await Task.Run(async () =>
            {
                var result = new AgentProxiesResult();
                try
                {
                    ConnectionInfo connInfo;
                    ConnectionServiceInstance.TryFindConnection(parameters.OwnerUri, out connInfo);
                    CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);

                    int proxyCount = dataContainer.Server.JobServer.ProxyAccounts.Count;
                    var proxies = new AgentProxyInfo[proxyCount];
                    for (int i = 0; i < proxyCount; ++i)
                    {
                        var proxy = dataContainer.Server.JobServer.ProxyAccounts[i];
                        proxies[i] = new AgentProxyInfo
                        {
                            AccountName = proxy.Name,
                            Description = proxy.Description,
                            CredentialName = proxy.CredentialName
                        };
                    }
                    result.Proxies = proxies;
                    result.Success = true;
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = ex.ToString();
                }

                await requestContext.SendResult(result);
            });
        }

        internal async Task HandleCreateAgentProxyRequest(CreateAgentProxyParams parameters, RequestContext<AgentProxyResult> requestContext)
        {
            var result = await ConfigureAgentProxy(
                parameters.OwnerUri,
                parameters.Proxy.AccountName,
                parameters.Proxy,
                ConfigAction.Create,
                RunType.RunNow);

            await requestContext.SendResult(new AgentProxyResult()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2,
                Proxy = parameters.Proxy
            });
        }

        internal async Task HandleUpdateAgentProxyRequest(UpdateAgentProxyParams parameters, RequestContext<AgentProxyResult> requestContext)
        {
            var result = await ConfigureAgentProxy(
                parameters.OwnerUri,
                parameters.Proxy.AccountName,
                parameters.Proxy,
                ConfigAction.Update,
                RunType.RunNow);

            await requestContext.SendResult(new AgentProxyResult()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2,
                Proxy = parameters.Proxy
            });
        }

        internal async Task HandleDeleteAgentProxyRequest(DeleteAgentProxyParams parameters, RequestContext<ResultStatus> requestContext)
        {
            var result = await ConfigureAgentProxy(
                parameters.OwnerUri,
                parameters.Proxy.AccountName,
                parameters.Proxy,
                ConfigAction.Drop,
                RunType.RunNow);

            await requestContext.SendResult(new ResultStatus()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }

        #endregion // "Proxy Handlers"

        #region "Schedule Handlers"

        internal async Task HandleAgentSchedulesRequest(AgentSchedulesParams parameters, RequestContext<AgentSchedulesResult> requestContext)
        {
            await Task.Run(async () =>
            {
                var result = new AgentSchedulesResult();
                try
                {
                    ConnectionInfo connInfo;
                    ConnectionServiceInstance.TryFindConnection(parameters.OwnerUri, out connInfo);
                    CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);

                    int scheduleCount = dataContainer.Server.JobServer.SharedSchedules.Count;
                    var schedules = new AgentScheduleInfo[scheduleCount];
                    for (int i = 0; i < scheduleCount; ++i)
                    {
                        var schedule = dataContainer.Server.JobServer.SharedSchedules[i];
                        var scheduleData = new JobScheduleData(schedule);
                        schedules[i] = new AgentScheduleInfo();
                        schedules[i].Id = schedule.ID;
                        schedules[i].Name = schedule.Name;
                        schedules[i].IsEnabled = schedule.IsEnabled;
                        schedules[i].FrequencyTypes = (Contracts.FrequencyTypes)schedule.FrequencyTypes;
                        schedules[i].FrequencySubDayTypes = (Contracts.FrequencySubDayTypes)schedule.FrequencySubDayTypes;
                        schedules[i].FrequencySubDayInterval = schedule.FrequencySubDayInterval;
                        schedules[i].FrequencyRelativeIntervals = (Contracts.FrequencyRelativeIntervals)schedule.FrequencyRelativeIntervals;
                        schedules[i].FrequencyRecurrenceFactor = schedule.FrequencyRecurrenceFactor;
                        schedules[i].FrequencyInterval = schedule.FrequencyInterval;
                        schedules[i].DateCreated = schedule.DateCreated;
                        schedules[i].ActiveStartTimeOfDay = schedule.ActiveStartTimeOfDay;
                        schedules[i].ActiveStartDate = schedule.ActiveStartDate;
                        schedules[i].ActiveEndTimeOfDay = schedule.ActiveEndTimeOfDay;
                        schedules[i].JobCount = schedule.JobCount;
                        schedules[i].ActiveEndDate = schedule.ActiveEndDate;
                        schedules[i].ScheduleUid = schedule.ScheduleUid;
                        schedules[i].Description = scheduleData.Description;
                    }
                    result.Schedules = schedules;
                    result.Success = true;
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = ex.ToString();
                }

                await requestContext.SendResult(result);
            });
        }

        internal async Task HandleCreateAgentScheduleRequest(CreateAgentScheduleParams parameters, RequestContext<AgentScheduleResult> requestContext)
        {
            var result = await ConfigureAgentSchedule(
                parameters.OwnerUri,
                parameters.Schedule,
                ConfigAction.Create,
                RunType.RunNow);

            await requestContext.SendResult(new AgentScheduleResult()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2,
                Schedule = parameters.Schedule
            });
        }

        internal async Task HandleUpdateAgentScheduleRequest(UpdateAgentScheduleParams parameters, RequestContext<AgentScheduleResult> requestContext)
        {
            var result = await ConfigureAgentSchedule(
                parameters.OwnerUri,
                parameters.Schedule,
                ConfigAction.Update,
                RunType.RunNow);

            await requestContext.SendResult(new AgentScheduleResult()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2,
                Schedule = parameters.Schedule
            });
        }

        internal async Task HandleDeleteAgentScheduleRequest(DeleteAgentScheduleParams parameters, RequestContext<ResultStatus> requestContext)
        {
            var result = await ConfigureAgentSchedule(
                parameters.OwnerUri,
                parameters.Schedule,
                ConfigAction.Drop,
                RunType.RunNow);

            await requestContext.SendResult(new ResultStatus()
            {
                Success = result.Item1,
                ErrorMessage = result.Item2
            });
        }

        #endregion // "Schedule Handlers"

        #region "Helpers"

        internal void ExecuteAction(ManagementActionBase action, RunType runType)
        {
            var executionHandler = new ExecutonHandler(action);
            executionHandler.RunNow(runType, this);
            if (executionHandler.ExecutionResult == ExecutionMode.Failure)
            {
                if (executionHandler.ExecutionFailureException != null)
                {
                    throw executionHandler.ExecutionFailureException;
                }
                else
                {
                    throw new Exception("Failed to execute action");
                }
            }
        }

        internal async Task<Tuple<bool, string>> ConfigureAgentJob(
            string ownerUri,
            string originalJobName,
            AgentJobInfo jobInfo,
            ConfigAction configAction,
            RunType runType)
        {
            return await Task<Tuple<bool, string>>.Run(async () =>
            {
                try
                {
                    JobData jobData;
                    CDataContainer dataContainer;
                    CreateJobData(ownerUri, originalJobName, out dataContainer, out jobData, configAction, jobInfo);

                    using (JobActions actions = new JobActions(dataContainer, jobData, configAction))
                    {
                        ExecuteAction(actions, runType);
                    }


                    ConnectionInfo connInfo;
                    ConnectionServiceInstance.TryFindConnection(ownerUri, out connInfo);
                    if (connInfo != null)
                    {
                        dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
                    }

                    // Execute step actions if they exist
                    if (jobInfo.JobSteps != null && jobInfo.JobSteps.Length > 0)
                    {
                        foreach (AgentJobStepInfo step in jobInfo.JobSteps)
                        {   
                            configAction = ConfigAction.Create;
                            foreach(JobStep jobStep in dataContainer.Server.JobServer.Jobs[originalJobName].JobSteps)
                            {
                                // any changes made to step other than name or ordering
                                if ((step.StepName == jobStep.Name && step.Id == jobStep.ID) ||
                                    // if the step name was changed
                                    (step.StepName != jobStep.Name && step.Id == jobStep.ID) ||
                                    // if the step ordering was changed
                                    (step.StepName == jobStep.Name && step.Id != jobStep.ID))
                                {
                                    configAction = ConfigAction.Update;
                                    break;
                                } 
                            }
                            await ConfigureAgentJobStep(ownerUri, step, configAction, runType, jobData, dataContainer);
                        }
                    }

                    // Execute schedule actions if they exist
                    if (jobInfo.JobSchedules != null && jobInfo.JobSchedules.Length > 0)
                    {
                        foreach (AgentScheduleInfo schedule in jobInfo.JobSchedules)
                        {
                            await ConfigureAgentSchedule(ownerUri, schedule, configAction, runType, jobData, dataContainer);
                        }
                    }

                    // Execute alert actions if they exist
                    if (jobInfo.Alerts != null && jobInfo.Alerts.Length > 0)
                    {
                        foreach (AgentAlertInfo alert in jobInfo.Alerts)
                        {
                            alert.JobId = jobData.Job.JobID.ToString();
                            await ConfigureAgentAlert(ownerUri, alert.Name, alert, configAction, runType, jobData, dataContainer);
                        }
                    }

                    return new Tuple<bool, string>(true, string.Empty);
                }
                catch (Exception ex)
                {
                    return new Tuple<bool, string>(false, ex.ToString());
                }
            });
        }

        internal async Task<Tuple<bool, string>> ConfigureAgentJobStep(
            string ownerUri,
            AgentJobStepInfo stepInfo,
            ConfigAction configAction,
            RunType runType,
            JobData jobData = null,
            CDataContainer dataContainer = null)
        {
            return await Task<Tuple<bool, string>>.Run(() =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(stepInfo.JobName))
                    {
                        return new Tuple<bool, string>(false, "JobName cannot be null");
                    }

                    if (jobData == null)
                    {
                        CreateJobData(ownerUri, stepInfo.JobName, out dataContainer, out jobData);
                    }

                    using (var actions = new JobStepsActions(dataContainer, jobData, stepInfo, configAction))
                    {
                        ExecuteAction(actions, runType);
                    }

                    return new Tuple<bool, string>(true, string.Empty);
                }
                catch (Exception ex)
                {
                    // log exception here
                    return new Tuple<bool, string>(false, ex.ToString());
                }
            });
        }

        internal async Task<Tuple<bool, string>> ConfigureAgentAlert(
            string ownerUri,
            string alertName,
            AgentAlertInfo alert,
            ConfigAction configAction,
            RunType runType,
            JobData jobData = null,
            CDataContainer dataContainer = null)
        {
            return await Task<Tuple<bool, string>>.Run(() =>
            {
                try
                {
                    // If the alert is being created outside of a job
                    if (string.IsNullOrWhiteSpace(alert.JobName))
                    {
                        ConnectionInfo connInfo;
                        ConnectionServiceInstance.TryFindConnection(ownerUri, out connInfo);
                        dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
                    } 
                    else 
                    {   
                        if (jobData == null)
                        {
                            // If the alert is being created inside a job
                            CreateJobData(ownerUri, alert.JobName, out dataContainer, out jobData);
                        }
                    }
                    STParameters param = new STParameters(dataContainer.Document);
                    param.SetParam("alert", alertName);
                    if (alert != null)
                    {
                        using (AgentAlertActions actions = new AgentAlertActions(dataContainer, alertName, alert, configAction, jobData))
                        {
                            ExecuteAction(actions, runType);
                        }
                    }

                    return new Tuple<bool, string>(true, string.Empty);
                }
                catch (Exception ex)
                {
                    return new Tuple<bool, string>(false, ex.ToString());
                }
            });
        }

        internal async Task<Tuple<bool, string>> ConfigureAgentOperator(
            string ownerUri,
            AgentOperatorInfo operatorInfo,
            ConfigAction configAction,
            RunType runType)
        {
            return await Task<Tuple<bool, string>>.Run(() =>
            {
                try
                {
                    ConnectionInfo connInfo;
                    ConnectionServiceInstance.TryFindConnection(ownerUri, out connInfo);
                    CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
                    STParameters param = new STParameters(dataContainer.Document);
                    param.SetParam("operator", operatorInfo.Name);

                    using (AgentOperatorActions actions = new AgentOperatorActions(dataContainer, operatorInfo, configAction))
                    {
                        ExecuteAction(actions, runType);
                    }

                    return new Tuple<bool, string>(true, string.Empty);
                }
                catch (Exception ex)
                {
                    return new Tuple<bool, string>(false, ex.ToString());
                }
            });
        }

        internal async Task<Tuple<bool, string>> ConfigureAgentProxy(
            string ownerUri,
            string accountName,
            AgentProxyInfo proxy,
            ConfigAction configAction,
            RunType runType)
        {
            return await Task<bool>.Run(() =>
            {
                try
                {
                    ConnectionInfo connInfo;
                    ConnectionServiceInstance.TryFindConnection(ownerUri, out connInfo);
                    CDataContainer dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);
                    STParameters param = new STParameters(dataContainer.Document);
                    param.SetParam("proxyaccount", accountName);

                    using (AgentProxyAccountActions actions = new AgentProxyAccountActions(dataContainer, proxy, configAction))
                    {
                        ExecuteAction(actions, runType);
                    }

                    return new Tuple<bool, string>(true, string.Empty);
                }
                catch (Exception ex)
                {
                    return new Tuple<bool, string>(false, ex.ToString());
                }
            });
        }

        internal async Task<Tuple<bool, string>> ConfigureAgentSchedule(
            string ownerUri,
            AgentScheduleInfo schedule,
            ConfigAction configAction,
            RunType runType,
            JobData jobData = null,
            CDataContainer dataContainer = null)
        {
            return await Task<bool>.Run(() =>
            {
                try
                {
                    if (jobData == null)
                    {
                        CreateJobData(ownerUri, schedule.JobName, out dataContainer, out jobData);
                    }

                    const string UrnFormatStr = "Server[@Name='{0}']/JobServer[@Name='{0}']/Job[@Name='{1}']/Schedule[@Name='{2}']";
                    string serverName = dataContainer.Server.Name.ToUpper();
                    string scheduleUrn = string.Format(UrnFormatStr, serverName, jobData.Job.Name, schedule.Name);

                    STParameters param = new STParameters(dataContainer.Document);
                    param.SetParam("urn", scheduleUrn);

                    using (JobSchedulesActions actions = new JobSchedulesActions(dataContainer, jobData, schedule, configAction))
                    {
                        ExecuteAction(actions, runType);
                    }

                    return new Tuple<bool, string>(true, string.Empty);
                }
                catch (Exception ex)
                {
                    return new Tuple<bool, string>(false, ex.ToString());
                }
            });
        }

        private void CreateJobData(
            string ownerUri,
            string jobName,
            out CDataContainer dataContainer,
            out JobData jobData,
            ConfigAction configAction = ConfigAction.Create,
            AgentJobInfo jobInfo = null)
        {
            ConnectionInfo connInfo;
            ConnectionServiceInstance.TryFindConnection(ownerUri, out connInfo);
            dataContainer = CDataContainer.CreateDataContainer(connInfo, databaseExists: true);

            XmlDocument jobDoc = CreateJobXmlDocument(dataContainer.Server.Name.ToUpper(), jobName);
            dataContainer.Init(jobDoc.InnerXml);

            STParameters param = new STParameters(dataContainer.Document);
            string originalName = jobInfo != null && !string.Equals(jobName, jobInfo.Name) ? jobName : string.Empty;
            param.SetParam("job",  configAction == ConfigAction.Update ? jobName : string.Empty);
            param.SetParam("jobid", string.Empty);

            jobData = new JobData(dataContainer, jobInfo, configAction);
        }

        public static XmlDocument CreateJobXmlDocument(string svrName, string jobName)
        {
            // XML element strings
            const string XmlFormDescElementName = "formdescription";
            const string XmlParamsElementName = "params";
            const string XmlJobElementName = "job";
            const string XmlUrnElementName = "urn";
            const string UrnFormatStr = "Server[@Name='{0}']/JobServer[@Name='{0}']/Job[@Name='{1}']";

            // Write out XML.
            StringWriter textWriter = new StringWriter();
            XmlTextWriter xmlWriter = new XmlTextWriter(textWriter);

            xmlWriter.WriteStartElement(XmlFormDescElementName);
            xmlWriter.WriteStartElement(XmlParamsElementName);

            xmlWriter.WriteElementString(XmlJobElementName, jobName);
            xmlWriter.WriteElementString(XmlUrnElementName, string.Format(UrnFormatStr, svrName, jobName));

            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();

            xmlWriter.Close();

            // Create an XML document.
            XmlDocument doc = new XmlDocument();
            XmlTextReader rdr = new XmlTextReader(new System.IO.StringReader(textWriter.ToString()));
            rdr.MoveToContent();
            doc.LoadXml(rdr.ReadOuterXml());
            return doc;
        }

        private Tuple<SqlConnectionInfo, DataTable, ServerConnection> CreateSqlConnection(ConnectionInfo connInfo, String jobId)
        {
            var serverConnection = ConnectionService.OpenServerConnection(connInfo);
            var server = new Server(serverConnection);
            var filter = new JobHistoryFilter();
            filter.JobID = new Guid(jobId);
            var dt = server.JobServer.EnumJobHistory(filter);
            var sqlConnInfo = new SqlConnectionInfo(serverConnection, SqlServer.Management.Common.ConnectionType.SqlConnection);
            return new Tuple<SqlConnectionInfo, DataTable, ServerConnection>(sqlConnInfo, dt, serverConnection);
        }

        #endregion // "Helpers"
    }
}
