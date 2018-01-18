//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.ComponentModel;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    // these map to the values for @execution_status
    // that can be passed to sp_help_job (except the first one!)
    // also the same as the smo enum JobExecutionStatus.
    internal enum EnumStatus
    {
        All = -1,
        NotIdleOrSuspended = 0,
        Executing = 1,
        WaitingForWorkerThread = 2,        
        BetweenRetries = 3,
        Idle = 4,
        Suspended = 5,
        WaitingForStepToFinish = 6,
        PerformingCompletionAction = 7
    }

    //
    // these values map to CompletionResult values, except the first.
    //
    internal enum EnumCompletionResult
    {
        All = -1,    
        Failed = 0,        
        Succeeded = 1,
        Retry = 2,
        Cancelled = 3,
        InProgress = 4,    
        Unknown = 5
    }

    //
    // for boolean job properties
    //
    internal enum EnumThreeState
    {
        All,    
        Yes,
        No
    }

    /// <summary>
    /// JobsFilter class - used to allow user to set filtering options for All Jobs Panel
    /// </summary>
    internal class JobActivityFilter : IFilterDefinition
    {
        /// <summary>
        /// constructor
        /// </summary>
        public JobActivityFilter()
        {
        }


        #region Properties

        private DateTime lastRunDate = new DateTime();
        private DateTime nextRunDate = new DateTime();
        private string name = string.Empty;
        private string category = string.Empty;
        private EnumStatus status = EnumStatus.All;
        private EnumThreeState enabled = EnumThreeState.All;
        private EnumThreeState runnable = EnumThreeState.All;
        private EnumThreeState scheduled = EnumThreeState.All;
        private EnumCompletionResult lastRunOutcome = EnumCompletionResult.All;

        private bool filterdefinitionEnabled = false;

        public EnumCompletionResult LastRunOutcome
        {
            get
            {
                return lastRunOutcome;
            }
            set
            {
                lastRunOutcome = value;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value.Trim();
            }
        }

        public EnumThreeState Enabled
        {
            get
            {
                return enabled;
            }
            set
            {
                enabled = value;
            }
        }

        public EnumStatus Status
        {
            get
            {
                return status;
            }
            set
            {
                status = value;
            }
        }

        public DateTime LastRunDate
        {
            get
            {
                return lastRunDate;
            }
            set
            {
                lastRunDate = value;
            }
        }

        public DateTime NextRunDate
        {
            get
            {
                return nextRunDate;
            }
            set
            {
                nextRunDate = value;
            }
        }

        public string Category
        {
            get
            {
                return category;
            }
            set
            {
                category = value.Trim();
            }
        }

        public EnumThreeState Runnable
        {
            get
            {
                return runnable;
            }
            set
            {
                runnable = value;
            }
        }

        public EnumThreeState Scheduled
        {
            get
            {
                return scheduled;
            }
            set
            {
                scheduled = value;
            }
        }

        #endregion

        #region IFilterDefinition - inteface implementation
        /// <summary>
        /// resets values of this object to default contraint values
        /// </summary>
        void IFilterDefinition.ResetToDefault()
        {
            lastRunDate = new DateTime();
            nextRunDate = new DateTime();
            name = string.Empty;
            category = string.Empty;
            enabled = EnumThreeState.All;
            status = EnumStatus.All;
            runnable = EnumThreeState.All;
            scheduled = EnumThreeState.All;
            lastRunOutcome = EnumCompletionResult.All;
        }

        /// <summary>
        /// checks if the filter is the same with the default filter
        /// </summary>
        bool IFilterDefinition.IsDefault()
        {
            return (lastRunDate.Ticks == 0 &&
                   nextRunDate.Ticks == 0 &&
                   name.Length == 0 &&
                   category.Length == 0 &&
                   enabled == EnumThreeState.All &&
                   status == EnumStatus.All &&
                   runnable == EnumThreeState.All &&
                   scheduled == EnumThreeState.All &&
                   lastRunOutcome == EnumCompletionResult.All);
        }

        /// <summary>
        /// creates a shallow clone
        /// </summary>
        /// <returns></returns>
        object IFilterDefinition.ShallowClone()
        {
            JobActivityFilter clone = new JobActivityFilter();

            clone.LastRunDate = this.LastRunDate;
            clone.NextRunDate = this.NextRunDate;
            clone.Name = this.Name;
            clone.Category = this.Category;
            clone.Enabled = this.Enabled;
            clone.Status = this.Status;
            clone.Runnable = this.Runnable;
            clone.Scheduled = this.Scheduled;
            clone.LastRunOutcome = this.LastRunOutcome;

            (clone as IFilterDefinition).Enabled = (this as IFilterDefinition).Enabled;
            return clone;
        }

        /// <summary>
        /// setup-s filter definition based on a template
        /// </summary>
        /// <param name="template"></param>
        void IFilterDefinition.ShallowCopy(object template)
        {
            System.Diagnostics.Debug.Assert(template is JobActivityFilter);

            JobActivityFilter f = template as JobActivityFilter;

            this.LastRunDate = f.LastRunDate;
            this.NextRunDate = f.NextRunDate;
            this.Name = f.Name;
            this.Category = f.Category;
            this.Enabled = f.Enabled;
            this.Status = f.Status;
            this.Runnable = f.Runnable;
            this.Scheduled = f.Scheduled;
            this.LastRunOutcome = f.LastRunOutcome;

            (this as IFilterDefinition).Enabled = (template as IFilterDefinition).Enabled;
        }



        /// <summary>
        /// tells us if filtering is enabled or diabled
        /// a disabled filter lets everything pass and filters nothing out
        /// </summary>
        bool IFilterDefinition.Enabled
        {
            get
            {
                return filterdefinitionEnabled;
            }
            set
            {
                filterdefinitionEnabled = value;
            }
        }
        #endregion

        #region Build filter

        private void AddPrefix(StringBuilder sb, bool clauseAdded)
        {
            if (clauseAdded)
            {
                sb.Append(" and ( ");
            }
            else
            {
                sb.Append(" ( ");
            }
        }

        private void AddSuffix(StringBuilder sb)
        {
            sb.Append(" ) ");
        }


        /// <summary>
        /// fetch an xpath clause used for filtering 
        /// jobs fetched by the enumerator.  
        /// note that all other properties must be filtered on the client 
        /// because enumerator will not filter properties that are fetched 
        /// at post-process time.  We can't even filter on the job name here
        /// since we have to do a case-insensitive "contains" comparision on the name.
        /// </summary>
        public string GetXPathClause()
        {
            if (this.enabled == EnumThreeState.All)
            {
                return string.Empty;
            }

            bool clauseAdded = false;
            StringBuilder sb = new StringBuilder();
            sb.Append("[");

            //
            // enabled clause
            //
            if (this.enabled != EnumThreeState.All)
            {
                AddPrefix(sb, clauseAdded);
                sb.Append("@IsEnabled = " + (this.enabled == EnumThreeState.Yes ? "true() " : "false() "));
                AddSuffix(sb);
                clauseAdded = true;
            }

            sb.Append("]");
            return sb.ToString();
        }

        #endregion
    }
}



