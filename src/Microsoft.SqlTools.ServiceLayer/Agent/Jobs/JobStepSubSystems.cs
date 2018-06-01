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
            : this(dataContainer, null, null)
        {
        }

        public JobStepSubSystems(CDataContainer dataContainer, JobStepData data, IServiceProvider serviceProvider)
        {
            this.data = data;
            var availableSystems =
                dataContainer.Server.JobServer.EnumSubSystems()
                    .Rows.OfType<DataRow>()
                    .Select(r => (AgentSubSystem)Convert.ToInt32(r["subsystem_id"]));

            foreach (var agentSubSystemId in availableSystems)
            {
                var agentSubSystem = CreateJobStepSubSystem(agentSubSystemId, dataContainer, data, serviceProvider);
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
            JobStepData data, 
            IServiceProvider serviceProvider)
        {
            // switch (agentSubSystem)
            // {
            //     case AgentSubSystem.TransactSql:
            //         return new JobStepSubSystem(
            //             AgentSubSystem.TransactSql,
            //             (data == null) ? null : new TSqlJobSubSystemDefinition(dataContainer, data),
            //             (data == null)
            //                 ? null
            //                 : new TSqlSubSystemAdvancedProperties(dataContainer, data, serviceProvider));

            //     case AgentSubSystem.CmdExec:
            //         return new JobStepSubSystem(
            //             AgentSubSystem.CmdExec,
            //             (data == null) ? null : new CmdExecJobSubSystemDefinition(),
            //             (data == null) ? null : new JobStepAdvancedLogging(dataContainer, messageProvider, data));

            //     // case AgentSubSystem.Distribution:
            //     //     return new JobStepSubSystem(
            //     //         AgentSubSystem.Distribution,
            //     //         (data == null) ? null : new ReplicationJobSubSystemDefinitionNoDb(dataContainer),
            //     //         (data == null) ? null : new NoAdvancedProperties());

            //     // case AgentSubSystem.Merge:
            //     //     return new JobStepSubSystem(
            //     //         AgentSubSystem.Merge,
            //     //         (data == null) ? null : new ReplicationJobSubSystemDefinitionNoDb(dataContainer),
            //     //         (data == null) ? null : new NoAdvancedProperties());

            //     // case AgentSubSystem.QueueReader:
            //     //     return new JobStepSubSystem(
            //     //         AgentSubSystem.QueueReader,
            //     //         (data == null) ? null : new ReplicationJobSubSystemDefinitionWithDb(dataContainer),
            //     //         (data == null) ? null : new ReplicationJobSubSystemAdvancedProperties());

            //     // case AgentSubSystem.Snapshot:
            //     //     return new JobStepSubSystem(
            //     //         AgentSubSystem.Snapshot,
            //     //         (data == null) ? null : new ReplicationJobSubSystemDefinitionNoDb(dataContainer),
            //     //         (data == null) ? null : new NoAdvancedProperties());

            //     // case AgentSubSystem.LogReader:
            //     //     return new JobStepSubSystem(
            //     //         AgentSubSystem.LogReader,
            //     //         (data == null) ? null : new ReplicationJobSubSystemDefinitionNoDb(dataContainer),
            //     //         (data == null) ? null : new NoAdvancedProperties());

            //     // case AgentSubSystem.AnalysisCommand:
            //     //     return new JobStepSubSystem(
            //     //         AgentSubSystem.AnalysisCommand,
            //     //         (data == null) ? null : new ASCmdJobSubSystemDefinition(dataContainer),
            //     //         (data == null) ? null : new JobStepAdvancedLogging(dataContainer, messageProvider, data));

            //     // case AgentSubSystem.AnalysisQuery:
            //     //     return new JobStepSubSystem(
            //     //         AgentSubSystem.AnalysisQuery,
            //     //         (data == null) ? null : new ASQueryJobSubSystemDefinition(dataContainer, messageProvider),
            //     //         (data == null) ? null : new JobStepAdvancedLogging(dataContainer, messageProvider, data));

            //     // case AgentSubSystem.Ssis:
            //     //     // return null if IntegrationServicesGui shouldn't be shown
            //     //     return SsmsInformation.CanShowIntegrationServicesGui ? CreateJobStepSubSystemSsis(agentSubSystem, dataContainer, data, messageProvider) : null;

            //     case AgentSubSystem.PowerShell:
            //         return new JobStepSubSystem(
            //             AgentSubSystem.PowerShell,
            //             (data == null) ? null : new PowerShellJobSubSystemDefinition(dataContainer),
            //             (data == null) ? null : new JobStepAdvancedLogging(dataContainer, messageProvider, data));

            //     // case AgentSubSystem.ActiveScripting:
            //     //     return new JobStepSubSystem(
            //     //         AgentSubSystem.ActiveScripting,
            //     //         (data == null) ? null : new ActiveXJobSubSystemDefinition(dataContainer),
            //     //         (data == null) ? null : new NoAdvancedProperties());

            //     default:
            //         return null;
            // }
            return null;
        }

        // Separate function to create JobStepSubSystem for SSIS so that dependency on DTS is not exposed when IS components aren't present
        // private static JobStepSubSystem CreateJobStepSubSystemSsis(AgentSubSystem agentSubSystem, CDataContainer dataContainer, JobStepData datar)
        // {
        //     JobStepAdvancedLogging loggingPane = null;
        //     if (dataContainer.Server.Information.Version.Major > 9 ||
        //         dataContainer.Server.Information.Version.Build > 2047)
        //     {
        //         // show this only for Yukon SP2, downlevel does not support 
        //         // the logging features.
        //         loggingPane = new JobStepAdvancedLogging(dataContainer, messageProvider, data);
        //     }
        //     return new JobStepSubSystem(
        //         AgentSubSystem.Ssis,
        //         (data == null) ? null : new DTSJobSubSystemDefinition(dataContainer, messageProvider),
        //         (data == null) ? null : loggingPane);
        // }
    }

    internal class JobStepSubSystem
    {
        #region data members
        private AgentSubSystem subSystemKey;
        // private Control definition;
        // private Control stepSpecificAdvanced;
        #endregion

        #region construction
        public JobStepSubSystem(AgentSubSystem key)
        {
            this.subSystemKey = key;
            // this.definition = Definition;
            // this.stepSpecificAdvanced = Advanced;
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








