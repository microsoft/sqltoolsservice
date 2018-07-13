//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Data;
using System.Globalization;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// a class for storing various properties of agent jobs, 
    /// used by the Job Activity Monitor
    /// </summary>
    public class JobProperties
    {
        private string name;
        
        private int currentExecutionStatus;
        private int lastRunOutcome;
        private string currentExecutionStep;
        private bool enabled;
        private bool hasTarget;
        private bool hasSchedule;
        private bool hasStep;
        private bool runnable;
        private string category;
        private int categoryID;
        private int categoryType;
        private DateTime lastRun;
        private DateTime nextRun;
        private Guid jobId;
        private string description;

        private JobProperties()
        {
        }

        public JobProperties(DataRow row)
        {
            System.Diagnostics.Debug.Assert(row["Name"]             != DBNull.Value, "Name is null!");
            System.Diagnostics.Debug.Assert(row["IsEnabled"]        != DBNull.Value, "IsEnabled is null!");
            System.Diagnostics.Debug.Assert(row["Category"]         != DBNull.Value, "Category is null!");
            System.Diagnostics.Debug.Assert(row["CategoryID"]       != DBNull.Value, "CategoryID is null!");
            System.Diagnostics.Debug.Assert(row["CategoryType"]     != DBNull.Value, "CategoryType is null!");
            System.Diagnostics.Debug.Assert(row["CurrentRunStatus"] != DBNull.Value, "CurrentRunStatus is null!");
            System.Diagnostics.Debug.Assert(row["CurrentRunStep"]   != DBNull.Value, "CurrentRunStep is null!");
            System.Diagnostics.Debug.Assert(row["HasSchedule"]      != DBNull.Value, "HasSchedule is null!");
            System.Diagnostics.Debug.Assert(row["HasStep"]          != DBNull.Value, "HasStep is null!");
            System.Diagnostics.Debug.Assert(row["HasServer"]        != DBNull.Value, "HasServer is null!");
            System.Diagnostics.Debug.Assert(row["LastRunOutcome"]   != DBNull.Value, "LastRunOutcome is null!");
            System.Diagnostics.Debug.Assert(row["JobID"]            != DBNull.Value, "JobID is null!");            

            this.name                    = row["Name"].ToString();
            this.enabled                 = Convert.ToBoolean(row["IsEnabled"], CultureInfo.InvariantCulture);
            this.category                = row["Category"].ToString();
            this.categoryID              = Convert.ToInt32(row["CategoryID"], CultureInfo.InvariantCulture);
            this.categoryType            = Convert.ToInt32(row["CategoryType"], CultureInfo.InvariantCulture);
            this.currentExecutionStatus  = Convert.ToInt32(row["CurrentRunStatus"], CultureInfo.InvariantCulture);
            this.currentExecutionStep    = row["CurrentRunStep"].ToString();
            this.hasSchedule             = Convert.ToBoolean(row["HasSchedule"], CultureInfo.InvariantCulture);
            this.hasStep                 = Convert.ToBoolean(row["HasStep"], CultureInfo.InvariantCulture);
            this.hasTarget               = Convert.ToBoolean(row["HasServer"], CultureInfo.InvariantCulture);
            this.lastRunOutcome          = Convert.ToInt32(row["LastRunOutcome"], CultureInfo.InvariantCulture);
            this.jobId                   = Guid.Parse(row["JobID"].ToString());
            this.description             = row["Description"].ToString();

            // for a job to be runnable, it must:
            // 1. have a target server
            // 2. have some steps
            this.runnable = this.hasTarget && this.hasStep;

            if (row["LastRunDate"] != DBNull.Value)
            {
                this.lastRun = Convert.ToDateTime(row["LastRunDate"], CultureInfo.InvariantCulture);
            }

            if (row["NextRunDate"] != DBNull.Value)
            {
                this.nextRun = Convert.ToDateTime(row["NextRunDate"], CultureInfo.InvariantCulture);
            }
        }

        public bool Runnable
        {
            get{ return runnable;}
        }

        public string Name
        {
            get{ return name;}
        }

        public string Category
        {
            get{ return category;}
        }

        public int CategoryID
        {
            get{ return categoryID;}
        }

        public int CategoryType
        {
            get{ return categoryType;}
        }

        public int LastRunOutcome
        {
            get{ return lastRunOutcome;}
        }

        public int CurrentExecutionStatus
        {
            get{ return currentExecutionStatus;}
        }

        public string CurrentExecutionStep
        {
            get{ return currentExecutionStep;}
        }

        public bool Enabled
        {
            get{ return enabled;}
        }

        public bool HasTarget
        {
            get{ return hasTarget;}
        }

        public bool HasStep
        {
            get{ return hasStep;}
        }

        public bool HasSchedule
        {
            get{ return hasSchedule;}
        }

        public DateTime NextRun
        {
            get{ return nextRun;}
        }

        public DateTime LastRun
        {
            get{ return lastRun;}
        }

        public Guid JobID
        {
            get
            {
                return this.jobId;
            }
        }

        public string Description
        {
            get
            {
                return this.description;
            }
        }
    }
}