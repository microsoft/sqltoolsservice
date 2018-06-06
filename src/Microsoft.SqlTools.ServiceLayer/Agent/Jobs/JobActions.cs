//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Globalization;
using System.Xml;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for JobPropertiesGeneral.
    /// </summary>
    internal class JobActions : ManagementActionBase
    {
        private JobData data;
        private IManagedConnection managedConnection = null;

        public JobActions(CDataContainer dataContainer, JobData data)
        {          
            this.DataContainer = dataContainer;
            this.data = data;
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {               
            }
            base.Dispose(disposing);

            // Dispose of the data member (a reference to JobProperties data).
            if (this.data != null)
            {
                //release the object
                this.data = null;
            }
        }

        /// <summary>
        /// called by ManagementActionBase.PreProcessExecution to enable derived
        /// classes to take over execution and do entire execution in this method
        /// rather than having the framework to execute
        /// </summary>
        /// <param name="runType"></param>
        /// <param name="executionResult"></param>
        /// <returns>
        /// true if regular execution should take place, false if everything,
        /// has been done by this function
        /// </returns>
        protected override bool DoPreProcessExecution(RunType runType, out ExecutionMode executionResult)
        {
            base.DoPreProcessExecution(runType, out executionResult);
            this.data.ApplyChanges(creating: true);
            if (!IsScripting(runType)) 
            {
                this.DataContainer.SqlDialogSubject	= this.data.Job;
            }

            return false;
        }

        // Create the XML document that will be used to pass to the LogViewerForm dialog.
        private XmlDocument CreateLogViewerXmlDocument()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<params><sourcetype>jobhistory</sourcetype><nt>false</nt></params>");

            string jobName = this.data.Job.Name;

            XmlElement jobElement = doc.CreateElement("job");
            jobElement.InnerText = jobName;
            doc.DocumentElement.AppendChild(jobElement);

            XmlElement serverNameElement = doc.CreateElement("servername");
            serverNameElement.InnerText = this.managedConnection.Connection.ServerName;
            doc.DocumentElement.AppendChild(serverNameElement);

            XmlElement urnElement = doc.CreateElement("urn");
            urnElement.InnerText = "Server[@Name='" + this.managedConnection.Connection.ServerName;

            urnElement.InnerText += "']/JobServer/Job[@Name='" + Urn.EscapeString(jobName) + "' and @CategoryID='" + this.data.Job.CategoryID + "']";
            doc.DocumentElement.AppendChild(urnElement);

            return doc;
        }
    }
}
