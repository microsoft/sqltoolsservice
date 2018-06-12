//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.ServiceLayer.Admin;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for JobStepSubSystems.
    /// </summary>
    internal class JobStepSubSystems
    {
        private IDictionary<AgentSubSystem, JobStepSubSystem> subSystems = new Dictionary<AgentSubSystem, JobStepSubSystem>();
        JobStepData data;

        public JobStepSubSystems(CDataContainer dataContainer)
            : this(dataContainer, null)
        {
        }

        public JobStepSubSystems(CDataContainer dataContainer, JobStepData data)
        {
            this.data = data;
            var availableSystems =
                dataContainer.Server.JobServer.EnumSubSystems()
                    .Rows.OfType<DataRow>()
                    .Select(r => (AgentSubSystem)Convert.ToInt32(r["subsystem_id"]));

            foreach (var agentSubSystemId in availableSystems)
            {
                var agentSubSystem = CreateJobStepSubSystem(agentSubSystemId, dataContainer, data);
                // The server might have some new subsystem we don't know about, just ignore it.
                if (agentSubSystem != null)
                {
                    subSystems[agentSubSystemId] = agentSubSystem;
                }
            }            
        }

        public JobStepSubSystem[] AvailableSubSystems
        {
            get { return this.subSystems.Keys.OrderBy(k => (int) k).Select(k => this.subSystems[k]).ToArray(); }
        }

        public JobStepSubSystem Lookup(AgentSubSystem key)
        {
            JobStepSubSystem rv = null;
            if (this.subSystems.ContainsKey(key))
            {
                return this.subSystems[key];
            }
            return rv;
        }

        private static TypeConverter typeConverter = TypeDescriptor.GetConverter(typeof (AgentSubSystem));
        // Returns name of the subsystem for a given enum value
        public static string LookupFriendlyName(AgentSubSystem key)
        {
            return (string)typeConverter.ConvertToString((Enum)key);
        }
        
        // Returns name of the subsystem for a given enum value
        public static string LookupName(AgentSubSystem key)
        {
            // Have to subtract first enum value to bring the 
            // index to 0-based index
            return typeConverter.ConvertToInvariantString((Enum) key);
        }

        private static JobStepSubSystem CreateJobStepSubSystem(
            AgentSubSystem agentSubSystem, 
            CDataContainer dataContainer, 
            JobStepData data)
        {
            switch (agentSubSystem)
            {
                case AgentSubSystem.TransactSql:
                    return new JobStepSubSystem(AgentSubSystem.TransactSql);

                case AgentSubSystem.CmdExec:
                    return new JobStepSubSystem(AgentSubSystem.CmdExec);

                case AgentSubSystem.Distribution:
                    return new JobStepSubSystem(AgentSubSystem.Distribution);

                case AgentSubSystem.Merge:
                    return new JobStepSubSystem(AgentSubSystem.Merge);

                case AgentSubSystem.QueueReader:
                    return new JobStepSubSystem(AgentSubSystem.QueueReader);

                case AgentSubSystem.Snapshot:
                    return new JobStepSubSystem(AgentSubSystem.Snapshot);

                case AgentSubSystem.LogReader:
                    return new JobStepSubSystem(AgentSubSystem.LogReader);

                case AgentSubSystem.AnalysisCommand:
                    return new JobStepSubSystem(AgentSubSystem.AnalysisCommand);

                case AgentSubSystem.AnalysisQuery:
                    return new JobStepSubSystem(AgentSubSystem.AnalysisQuery);

                case AgentSubSystem.PowerShell:
                    return new JobStepSubSystem(AgentSubSystem.PowerShell);

                default:
                    return null;
            }
        }
    }

    internal class JobStepSubSystem
    {
        #region data members
        private AgentSubSystem subSystemKey;
        #endregion

        #region construction
        public JobStepSubSystem(AgentSubSystem key)
        {
            this.subSystemKey = key;
        }
        #endregion

        #region overrides
        public override string ToString()
        {
            return this.FriendlyName;
        }
        #endregion

        #region properties
        public AgentSubSystem Key
        {
            get
            {
                return this.subSystemKey;
            }
        }

        public string Name
        {
            get
            {
                return JobStepSubSystems.LookupName(this.subSystemKey);
            }
        }

        public string FriendlyName
        {
            get
            {
                return JobStepSubSystems.LookupFriendlyName(this.subSystemKey);
            }
        }

        #endregion
    }
}
