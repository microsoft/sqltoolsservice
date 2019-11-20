//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Text;
using System.Data;
using System.Globalization;
using System.Collections.Generic;

using SMO = Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.Kusto.ServiceLayer.Agent.Contracts;

namespace Microsoft.Kusto.ServiceLayer.Agent
{
    internal class JobFetcher
    {
        private Enumerator enumerator = null;
        private ServerConnection connection = null;
        private SMO.Server server = null;

        public JobFetcher(ServerConnection connection)
        {
            System.Diagnostics.Debug.Assert(connection != null, "ServerConnection is null");
            this.enumerator = new Enumerator();
            this.connection = connection;
            this.server = new SMO.Server(connection);
        }

        //
        // ServerConnection object should be passed from caller,
        // who gets it from CDataContainer.ServerConnection
        //
        public Dictionary<Guid, JobProperties> FetchJobs(JobActivityFilter filter)
        {
            string urn = server.JobServer.Urn.Value + "/Job";

            if (filter != null)
            {
                urn += filter.GetXPathClause();
                return FilterJobs(FetchJobs(urn), filter);
            }

            return FetchJobs(urn);
        }

        /// <summary>
        /// Filter Jobs that matches criteria specified in JobActivityFilter
        /// here we filter jobs by properties that enumerator doesn't
        /// support filtering on.
        /// $ISSUE - - DevNote: Filtering Dictionaries can be easily done with Linq and System.Expressions in .NET 3.5
        /// This requires re-design of current code and might impact functionality / performance due to newer dependencies
        /// We need to consider this change in future enhancements for Job Activity monitor
        /// </summary>
        /// <param name="unfilteredJobs"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        private Dictionary<Guid, JobProperties> FilterJobs(Dictionary<Guid, JobProperties> unfilteredJobs, 
                                           JobActivityFilter filter)
        {
            if (unfilteredJobs == null)
            {
                return null;
            }

            if (filter == null ||
                (filter is IFilterDefinition &&
                 ((filter as IFilterDefinition).Enabled == false ||
                  (filter as IFilterDefinition).IsDefault())))
            {
                return unfilteredJobs;
            }

            Dictionary<Guid, JobProperties> filteredJobs = new Dictionary<Guid, JobProperties>();
            
            // Apply Filter
            foreach (JobProperties jobProperties in unfilteredJobs.Values)
            {
                // If this job passed all filter criteria then include in filteredJobs Dictionary
                if (this.CheckIfNameMatchesJob(filter, jobProperties) &&
                    this.CheckIfCategoryMatchesJob(filter, jobProperties) &&
                    this.CheckIfEnabledStatusMatchesJob(filter, jobProperties) &&
                    this.CheckIfScheduledStatusMatchesJob(filter, jobProperties) &&
                    this.CheckIfJobStatusMatchesJob(filter, jobProperties) &&
                    this.CheckIfLastRunOutcomeMatchesJob(filter, jobProperties) &&
                    this.CheckIfLastRunDateIsGreater(filter, jobProperties) &&
                    this.CheckifNextRunDateIsGreater(filter, jobProperties) &&
                    this.CheckJobRunnableStatusMatchesJob(filter, jobProperties))
                {
                    filteredJobs.Add(jobProperties.JobID, jobProperties);
                }
            }

            return filteredJobs;
        }

        /// <summary>
        /// check if job runnable status in filter matches given job property
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="jobProperties"></param>
        /// <returns></returns>
        private bool CheckJobRunnableStatusMatchesJob(JobActivityFilter filter, JobProperties jobProperties)
        {
            bool isRunnableMatched = false;
            // filter based on job runnable
            switch (filter.Runnable)
            {
                // if All was selected, include in match
                case EnumThreeState.All:
                    isRunnableMatched = true;
                    break;

                // if Yes was selected, include only if job that is runnable
                case EnumThreeState.Yes:
                    if (jobProperties.Runnable)
                    {
                        isRunnableMatched = true;
                    }
                    break;

                // if Yes was selected, include only if job is not runnable
                case EnumThreeState.No:
                    if (!jobProperties.Runnable)
                    {
                        isRunnableMatched = true;
                    }
                    break;
            }
            return isRunnableMatched;
        }

        /// <summary>
        /// Check if next run date for given job property is greater than the one specified in the filter
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="jobProperties"></param>
        /// <returns></returns>
        private  bool CheckifNextRunDateIsGreater(JobActivityFilter filter, JobProperties jobProperties)
        {
            bool isNextRunOutDateMatched = false;
            // filter next run date
            if (filter.NextRunDate.Ticks == 0 ||
                jobProperties.NextRun >= filter.NextRunDate)
            {
                isNextRunOutDateMatched = true;
            }
            return isNextRunOutDateMatched;
        }

        /// <summary>
        /// Check if last run date for given job property is greater than the one specified in the filter
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="jobProperties"></param>
        /// <returns></returns>
        private bool CheckIfLastRunDateIsGreater(JobActivityFilter filter, JobProperties jobProperties)
        {
            bool isLastRunOutDateMatched = false;
            // filter last run date
            if (filter.LastRunDate.Ticks == 0 ||
                jobProperties.LastRun >= filter.LastRunDate)
            {
                isLastRunOutDateMatched = true;
            }

            return isLastRunOutDateMatched;
        }

        /// <summary>
        /// check if last run status filter matches given job property
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="jobProperties"></param>
        /// <returns></returns>
        private bool CheckIfLastRunOutcomeMatchesJob(JobActivityFilter filter, JobProperties jobProperties)
        {
            bool isLastRunOutcomeMatched = false;
            // filter - last run outcome
            if (filter.LastRunOutcome == EnumCompletionResult.All ||
                jobProperties.LastRunOutcome == (int)filter.LastRunOutcome)
            {
                isLastRunOutcomeMatched = true;
            }

            return isLastRunOutcomeMatched;
        }

        /// <summary>
        /// Check if job status filter matches given jobproperty
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="jobProperties"></param>
        /// <returns></returns>
        private  bool CheckIfJobStatusMatchesJob(JobActivityFilter filter, JobProperties jobProperties)
        {
            bool isStatusMatched = false;
            // filter - job run status
            if (filter.Status == EnumStatus.All ||
                jobProperties.CurrentExecutionStatus == (int)filter.Status)
            {
                isStatusMatched = true;
            }

            return isStatusMatched;
        }

        /// <summary>
        /// Check if job scheduled status filter matches job
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="jobProperties"></param>
        /// <returns></returns>
        private bool CheckIfScheduledStatusMatchesJob(JobActivityFilter filter, JobProperties jobProperties)
        {
            bool isScheduledMatched = false;
            // apply filter - if job has schedules or not
            switch (filter.Scheduled)
            {
                // if All was selected, include in match
                case EnumThreeState.All:
                    isScheduledMatched = true;
                    break;

                // if Yes was selected, include only if job has schedule
                case EnumThreeState.Yes:
                    if (jobProperties.HasSchedule)
                    {
                        isScheduledMatched = true;
                    }
                    break;

                // if Yes was selected, include only if job does not have schedule
                case EnumThreeState.No:
                    if (!jobProperties.HasSchedule)
                    {
                        isScheduledMatched = true;
                    }
                    break;
            }

            return isScheduledMatched;
        }

        /// <summary>
        /// Check if job enabled status matches job
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="jobProperties"></param>
        /// <returns></returns>
        private bool CheckIfEnabledStatusMatchesJob(JobActivityFilter filter, JobProperties jobProperties)
        {
            bool isEnabledMatched = false;
            // apply filter - if job was enabled or not
            switch (filter.Enabled)
            {
                // if All was selected, include in match
                case EnumThreeState.All:
                    isEnabledMatched = true;
                    break;

                // if Yes was selected, include only if job has schedule
                case EnumThreeState.Yes:
                    if (jobProperties.Enabled)
                    {
                        isEnabledMatched = true;
                    }
                    break;

                // if Yes was selected, include only if job does not have schedule
                case EnumThreeState.No:
                    if (!jobProperties.Enabled)
                    {
                        isEnabledMatched = true;
                    }
                    break;
            }

            return isEnabledMatched;
        }

        /// <summary>
        /// Check if a category matches given jobproperty
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="jobProperties"></param>
        /// <returns></returns>
        private  bool CheckIfCategoryMatchesJob(JobActivityFilter filter, JobProperties jobProperties)
        {
            bool isCategoryMatched = false;
            // Apply category filter if specified
            if (filter.Category.Length > 0)
            {
                //
                // we count it as a match if the job category contains 
                // a case-insensitive match for the filter string.
                //
                string jobCategory = jobProperties.Category.ToLower(CultureInfo.CurrentCulture);
                if (String.Compare(jobCategory, filter.Category.Trim().ToLower(CultureInfo.CurrentCulture), StringComparison.Ordinal) == 0)
                {
                    isCategoryMatched = true;
                }
            }
            else
            {
                // No category filter was specified
                isCategoryMatched = true;
            }

            return isCategoryMatched;
        }

        /// <summary>
        /// Check if name filter specified matches given jobproperty
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="jobProperties"></param>
        /// <returns></returns>
        private bool CheckIfNameMatchesJob(JobActivityFilter filter, JobProperties jobProperties)
        {
            bool isNameMatched = false;

            //
            // job name (can be comma-separated list)
            // we count it as a match if the job name contains 
            // a case-insensitive match for any of the filter strings.
            //
            if (filter.Name.Length > 0)
            {
                string jobname = jobProperties.Name.ToLower(CultureInfo.CurrentCulture);
                string[] jobNames = filter.Name.ToLower(CultureInfo.CurrentCulture).Split(',');
                int length = jobNames.Length;

                for (int j = 0; j < length; ++j)
                {
                    if (jobname.IndexOf(jobNames[j].Trim(), StringComparison.Ordinal) > -1)
                    {
                        isNameMatched = true;
                        break;
                    }
                }
            }
            else
            {
                // No name filter was specified
                isNameMatched = true;
            }

            return isNameMatched;
        }

        /// <summary>
        /// Fetch jobs for a given Urn
        /// </summary>
        /// <param name="urn"></param>
        /// <returns></returns>
        public Dictionary<Guid, JobProperties> FetchJobs(string urn)          
        {
            if(String.IsNullOrEmpty(urn))
            {
                throw new ArgumentNullException("urn");
            }

            Request request = new Request(); 
            request.Urn = urn;
            request.Fields = new string[] 
                {
                    "Name",
                    "IsEnabled",
                    "Category",
                    "CategoryID",
                    "CategoryType",
                    "CurrentRunStatus",
                    "CurrentRunStep",
                    "HasSchedule",
                    "HasStep",
                    "HasServer",
                    "LastRunOutcome",
                    "JobID",
                    "Description",
                    "LastRunDate",
                    "NextRunDate",
                    "OperatorToEmail",
                    "OperatorToNetSend",
                    "OperatorToPage",
                    "OwnerLoginName",
                    "PageLevel",
                    "StartStepID",
                    "NetSendLevel",
                    "EventLogLevel",
                    "EmailLevel",
                    "DeleteLevel"
                };

            DataTable dt = enumerator.Process(connection, request);
            int numJobs = dt.Rows.Count;
            if (numJobs == 0)
            {
                return null;
            }

            Dictionary<Guid, JobProperties> foundJobs = new Dictionary<Guid, JobProperties>(numJobs);
            for (int i = 0; i < numJobs; ++i)
            {
                JobProperties jobProperties = new JobProperties(dt.Rows[i]);
                foundJobs.Add(jobProperties.JobID, jobProperties);
            }

            return foundJobs;
        }
    }
}
