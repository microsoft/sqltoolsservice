//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.Agent;

namespace Microsoft.SqlTools.ServiceLayer.Agent.Contracts
{
    [Flags]
    public enum NotifyMethods
    {
        None = 0,
        NotifyEmail = 1,
        Pager = 2,
        NetSend = 4,
        NotifyAll = 7
    }

    public enum AlertType
    {
        SqlServerEvent = 1,
        SqlServerPerformanceCondition = 2,
        NonSqlServerEvent = 3,
        WmiEvent = 4
    }

    /// <summary>
    /// a class for storing various properties of agent alerts
    /// </summary>
    public class AgentAlertInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int DelayBetweenResponses { get; set; }
        public string EventDescriptionKeyword { get; set; }
        public string EventSource { get; set; }
        public int HasNotification { get; set; }
        public NotifyMethods IncludeEventDescription { get; set; }
        public bool IsEnabled { get; set; }
        public string JobId { get; set; }
        public string JobName { get; set; }
        public string LastOccurrenceDate { get; set; }
        public string LastResponseDate { get; set; }
        public int MessageId { get; set; }
        public string NotificationMessage { get; set; }
        public int OccurrenceCount { get; set; }
        public string PerformanceCondition { get; set; }        
        public int Severity { get; set; }
        public string DatabaseName { get; set; }
        public string CountResetDate { get; set; }
        public string CategoryName { get; set; }
        public AlertType AlertType { get; set; }
        public string WmiEventNamespace { get; set; }
        public string WmiEventQuery { get; set; }
    }
}
