//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using Microsoft.Kusto.ServiceLayer.Agent;

namespace Microsoft.Kusto.ServiceLayer.Agent.Contracts
{
    [Flags]
    public enum FrequencyTypes
    {
        Unknown = 0,
        OneTime = 1,
        Daily = 4,
        Weekly = 8,
        Monthly = 16,
        MonthlyRelative = 32,
        AutoStart = 64,
        OnIdle = 128
    }

    [Flags]
    public enum FrequencySubDayTypes
    {
        Unknown = 0,
        Once = 1,
        Second = 2,
        Minute = 4,
        Hour = 8
    }

    [Flags]
    public enum FrequencyRelativeIntervals
    {
        First = 1,
        Second = 2,
        Third = 4,
        Fourth = 8,
        Last = 16
    }

    /// <summary>
    /// a class for storing various properties of agent schedules 
    /// </summary>
    public class AgentScheduleInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string JobName { get; set; }
        public bool IsEnabled { get; set; }
        public FrequencyTypes FrequencyTypes { get; set; }
        public FrequencySubDayTypes FrequencySubDayTypes { get; set; }
        public int FrequencySubDayInterval { get; set; }
        public FrequencyRelativeIntervals FrequencyRelativeIntervals { get; set; }
        public int FrequencyRecurrenceFactor { get; set; }
        public int FrequencyInterval { get; set; }
        public DateTime DateCreated { get; set; }
        public TimeSpan ActiveStartTimeOfDay { get; set; }
        public DateTime ActiveStartDate { get; set; }
        public TimeSpan ActiveEndTimeOfDay { get; set; }
        public int JobCount { get; set; }
        public DateTime ActiveEndDate { get; set; }
        public Guid ScheduleUid { get; set; }
        public string Description { get; set; }
    }
}
