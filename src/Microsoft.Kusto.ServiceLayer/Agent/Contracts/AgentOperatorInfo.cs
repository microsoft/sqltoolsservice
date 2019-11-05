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
    public enum WeekDays
    {
        Sunday = 1,
        Monday = 2,
        Tuesday = 4,
        Wednesday = 8,
        Thursday = 16,
        Friday = 32,
        WeekDays = 62,
        Saturday = 64,
        WeekEnds = 65,
        EveryDay = 127
    }

    /// <summary>
    /// a class for storing various properties of agent operators
    /// </summary>
    public class AgentOperatorInfo
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public string EmailAddress { get; set; }
        public bool Enabled { get; set; }
        public string LastEmailDate { get; set; }
        public string LastNetSendDate { get; set; }        
        public string LastPagerDate { get; set; }        
        public string PagerAddress { get; set; }        
        public string CategoryName { get; set; }        
        public WeekDays PagerDays { get; set; }
        public string SaturdayPagerEndTime { get; set; }
        public string SaturdayPagerStartTime { get; set; }
        public string SundayPagerEndTime { get; set; }
        public string SundayPagerStartTime { get; set; }
        public string NetSendAddress { get; set; }
        public string WeekdayPagerStartTime { get; set; }
        public string WeekdayPagerEndTime { get; set; }
    }
}
