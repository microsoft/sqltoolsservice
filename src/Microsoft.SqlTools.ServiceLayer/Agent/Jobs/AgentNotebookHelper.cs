//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Data.SqlClient;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Agent.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    internal class AgentNotebookHelper
    {
        /// <summary>
        /// executes sql queries required by other agent notebook helper functions
        /// </summary>
        /// <param name="connInfo">connectionInfo generated from OwnerUri</param>
        /// <param name="sqlQuery">actual sql query to be executed</param>
        /// <param name="queryParameters">sql parameters required by the query</param>
        /// <param name="targetDatabase">the database on which the query will be executed</param>
        /// <returns></returns>
        public static async Task<DataSet> ExecuteSqlQueries(
            ConnectionInfo connInfo,
            string sqlQuery,
            List<SqlParameter> queryParameters = null,
            string targetDatabase = null)
        {
            DataSet result = new DataSet();
            string originalConnectionDatabase = connInfo.ConnectionDetails.DatabaseName;
            if (!string.IsNullOrWhiteSpace(targetDatabase))
            {
                connInfo.ConnectionDetails.DatabaseName = targetDatabase;
            }
            using (SqlConnection connection = new SqlConnection(ConnectionService.BuildConnectionString(connInfo.ConnectionDetails)))
            {
                await connection.OpenAsync();
                using (SqlCommand sqlQueryCommand = new SqlCommand(sqlQuery, connection))
                {
                    if (queryParameters != null)
                    {
                        sqlQueryCommand.Parameters.AddRange(queryParameters.ToArray());
                    }
                    SqlDataAdapter sqlCommandAdapter = new SqlDataAdapter(sqlQueryCommand);
                    sqlCommandAdapter.Fill(result);
                }
            }
            connInfo.ConnectionDetails.DatabaseName = originalConnectionDatabase;
            return result;
        }

        /// <summary>
        /// a function which fetches notebooks jobs accessible to the user
        /// </summary>
        /// <param name="connInfo">connectionInfo generated from OwnerUri</param>
        /// <returns>array of agent notebooks</returns>
        public static async Task<AgentNotebookInfo[]> GetAgentNotebooks(ConnectionInfo connInfo)
        {
            AgentNotebookInfo[] result;
            // Fetching all agent Jobs accessible to the user
            var serverConnection = ConnectionService.OpenServerConnection(connInfo);
            var fetcher = new JobFetcher(serverConnection);
            var filter = new JobActivityFilter();
            var jobs = fetcher.FetchJobs(filter);


            Dictionary<Guid, JobProperties> allJobsHashTable = new Dictionary<Guid, JobProperties>();
            if (jobs != null)
            {
                foreach (var job in jobs.Values)
                {
                    allJobsHashTable.Add(job.JobID, job);
                }
            }
            // Fetching notebooks across all databases accessible by the user
            string getJobIdsFromDatabaseQueryString =
            @"
            DECLARE @script AS VARCHAR(MAX)
            SET @script =
            '
            USE [?];
            IF EXISTS 
            (   
                SELECT * FROM INFORMATION_SCHEMA.TABLES 
                WHERE 
                TABLE_SCHEMA = N''notebooks'' 
                AND 
                TABLE_NAME = N''nb_template''
            )
            BEGIN
                SELECT 
                [notebooks].[nb_template].job_id,
                [notebooks].[nb_template].template_id,
                [notebooks].[nb_template].last_run_notebook_error,
                [notebooks].[nb_template].execute_database,
                DB_NAME() AS db_name                            
                FROM [?].notebooks.nb_template
                INNER JOIN
                msdb.dbo.sysjobs 
                ON
                [?].notebooks.nb_template.job_id = msdb.dbo.sysjobs.job_id
            END
            '
            EXEC sp_MSforeachdb @script";
            var agentNotebooks = new List<AgentNotebookInfo>();
            DataSet jobIdsDataSet = await ExecuteSqlQueries(connInfo, getJobIdsFromDatabaseQueryString);
            foreach (DataTable templateTable in jobIdsDataSet.Tables)
            {
                foreach (DataRow templateRow in templateTable.Rows)
                {
                    AgentNotebookInfo notebookJob =
                        AgentUtilities.ConvertToAgentNotebookInfo(allJobsHashTable[(Guid)templateRow["job_id"]]);
                    notebookJob.TemplateId = templateRow["template_id"] as string;
                    notebookJob.TargetDatabase = templateRow["db_name"] as string;
                    notebookJob.LastRunNotebookError = templateRow["last_run_notebook_error"] as string;
                    notebookJob.ExecuteDatabase = templateRow["execute_database"] as string;
                    agentNotebooks.Add(notebookJob);
                }
            }
            result = agentNotebooks.ToArray();
            return result;
        }


        public static async Task<AgentNotebookInfo> CreateNotebook(
          AgentService agentServiceInstance,
          string ownerUri,
          AgentNotebookInfo notebook,
          string templatePath,
          RunType runType)
        {
            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException();
            }
            AgentNotebookInfo result;
            ConnectionInfo connInfo;
            agentServiceInstance.ConnectionServiceInstance.TryFindConnection(ownerUri, out connInfo);

            // creating notebook job step
            notebook.JobSteps = CreateNotebookPowerShellStep(notebook.Name, notebook.TargetDatabase);

            // creating sql agent job 
            var jobCreationResult = await agentServiceInstance.ConfigureAgentJob(
                ownerUri,
                notebook.Name,
                notebook,
                ConfigAction.Create,
                runType);

            if (jobCreationResult.Item1 == false)
            {
                throw new Exception(jobCreationResult.Item2);
            }

            // creating notebook metadata for the job
            string jobId =
            await SetUpNotebookAndGetJobId(
                connInfo,
                notebook.Name,
                notebook.TargetDatabase,
                templatePath,
                notebook.ExecuteDatabase);
            notebook.JobId = jobId;
            result = notebook;
            return result;
        }

        internal static async Task DeleteNotebook(
            AgentService agentServiceInstance,
            string ownerUri,
            AgentNotebookInfo notebook,
            RunType runType)
        {
            ConnectionInfo connInfo;
            agentServiceInstance.ConnectionServiceInstance.TryFindConnection(ownerUri, out connInfo);

            // deleting job from sql agent
            var deleteJobResult = await agentServiceInstance.ConfigureAgentJob(
                ownerUri,
                notebook.Name,
                notebook,
                ConfigAction.Drop,
                runType);

            if(!deleteJobResult.Item1)
            {
                throw new Exception(deleteJobResult.Item2);
            }
            // deleting notebook metadata from target database
            await DeleteNotebookMetadata(
                connInfo,
                notebook.JobId,
                notebook.TargetDatabase);

           
        }

        internal static async Task UpdateNotebook(
            AgentService agentServiceInstance,
            string ownerUri,
            string originalNotebookName,
            AgentNotebookInfo notebook,
            string templatePath,
            RunType runType)
        {

            ConnectionInfo connInfo;
            agentServiceInstance.ConnectionServiceInstance.TryFindConnection(ownerUri, out connInfo);

            if (!string.IsNullOrEmpty(templatePath) && !File.Exists(templatePath))
            {
                throw new FileNotFoundException();
            }

            // updating notebook agent job
            var updateJobResult =
            await agentServiceInstance.ConfigureAgentJob(
                ownerUri,
                originalNotebookName,
                notebook,
                ConfigAction.Update,
                runType);
            
            if(!updateJobResult.Item1)
            {
                throw new Exception(updateJobResult.Item2);
            }

            // update notebook metadata
            UpdateNotebookInfo(
                connInfo,
                notebook.JobId,
                templatePath,
                notebook.ExecuteDatabase,
                notebook.TargetDatabase);

        }

        /// <summary>
        /// fetches all notebook histories for a particular notebook job
        /// </summary>
        /// <param name="connInfo">connectionInfo generated from OwnerUri</param>
        /// <param name="JobId">unique ID of the sql agent notebook job</param>
        /// <param name="targetDatabase">database used to store notebook metadata</param>
        /// <returns>array of notebook history info</returns>
        public static async Task<DataTable> GetAgentNotebookHistories(
            ConnectionInfo connInfo,
            string JobId,
            string targetDatabase)
        {
            DataTable result;
            string getNotebookHistoryQueryString =
            @"
            SELECT
            materialized_id,
            run_time,
            run_date,
            notebook_error
            FROM 
            notebooks.nb_materialized 
            WHERE JOB_ID = @jobId";
            List<SqlParameter> getNotebookHistoryQueryParams = new List<SqlParameter>();
            getNotebookHistoryQueryParams.Add(new SqlParameter("jobId", JobId));
            DataSet notebookHistoriesDataSet =
            await ExecuteSqlQueries(
                connInfo,
                getNotebookHistoryQueryString,
                getNotebookHistoryQueryParams,
                targetDatabase);
            result = notebookHistoriesDataSet.Tables[0];
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connInfo"></param>
        /// <param name="materializedId"></param>
        /// <param name="targetDatabase"></param>
        /// <returns></returns>
        public static async Task<string> GetMaterializedNotebook(
            ConnectionInfo connInfo,
            int materializedId,
            string targetDatabase)
        {
            string materializedNotebookQueryString =
            @"
            SELECT
            notebook 
            FROM 
            notebooks.nb_materialized 
            WHERE 
            materialized_id = @notebookMaterializedID";
            List<SqlParameter> materializedNotebookQueryParams = new List<SqlParameter>();
            materializedNotebookQueryParams.Add(new SqlParameter("notebookMaterializedID", materializedId));
            DataSet materializedNotebookDataSet =
            await ExecuteSqlQueries(
                connInfo,
                materializedNotebookQueryString,
                materializedNotebookQueryParams,
                targetDatabase);
            DataTable materializedNotebookTable = materializedNotebookDataSet.Tables[0];
            DataRow materializedNotebookRows = materializedNotebookTable.Rows[0];
            return materializedNotebookRows["notebook"] as string;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="notebookName"></param>
        /// <param name="storageDatabase"></param>
        /// <returns></returns>
        public static AgentJobStepInfo[] CreateNotebookPowerShellStep(
            string notebookName,
            string storageDatabase)
        {
            AgentJobStepInfo[] result;
            var assembly = Assembly.GetAssembly(typeof(AgentService));
            string execNotebookScript;
            string notebookScriptResourcePath = "Microsoft.SqlTools.ServiceLayer.Agent.NotebookResources.NotebookJobScript.ps1";
            using (Stream scriptStream = assembly.GetManifestResourceStream(notebookScriptResourcePath))
            {
                using (StreamReader reader = new StreamReader(scriptStream))
                {
                    execNotebookScript =
                    "$TargetDatabase = \"" +
                    storageDatabase +
                    "\"" +
                    Environment.NewLine +
                    reader.ReadToEnd();
                }
            }
            result = new AgentJobStepInfo[1];
            result[0] = new AgentJobStepInfo()
            {
                AppendLogToTable = false,
                AppendToLogFile = false,
                AppendToStepHist = false,
                Command = execNotebookScript,
                CommandExecutionSuccessCode = 0,
                DatabaseName = "",
                DatabaseUserName = null,
                FailStepId = 0,
                FailureAction = StepCompletionAction.QuitWithFailure,
                Id = 1,
                JobId = null,
                JobName = notebookName,
                OutputFileName = null,
                ProxyName = null,
                RetryAttempts = 0,
                RetryInterval = 0,
                Script = execNotebookScript,
                ScriptName = null,
                Server = "",
                StepName = "Exec-Notebook",
                SubSystem = AgentSubSystem.PowerShell,
                SuccessAction = StepCompletionAction.QuitWithSuccess,
                SuccessStepId = 0,
                WriteLogToTable = false,
            };
            return result;
        }

        public static async Task<string> SetUpNotebookAndGetJobId(
            ConnectionInfo connInfo,
            string notebookName,
            string targetDatabase,
            string templatePath,
            string executionDatabase)
        {
            string jobId;
            string notebookDatabaseSetupQueryString =
            @"
            IF NOT EXISTS (
            SELECT  schema_name
            FROM    information_schema.schemata
            WHERE   schema_name = 'notebooks' ) 
            BEGIN
            EXEC sp_executesql N'CREATE SCHEMA notebooks'
            END

            IF  NOT EXISTS (SELECT * FROM sys.objects 
            WHERE object_id = OBJECT_ID(N'[notebooks].[nb_template]') AND TYPE IN (N'U'))
            BEGIN
            CREATE TABLE [notebooks].[nb_template](
                template_id INT PRIMARY KEY IDENTITY(1,1), 
                job_id UNIQUEIDENTIFIER NOT NULL, 
                notebook NVARCHAR(MAX),
                execute_database NVARCHAR(MAX),
                last_run_notebook_error NVARCHAR(MAX)
            ) 
            END

            IF  NOT EXISTS (SELECT * FROM sys.objects 
            WHERE object_id = OBJECT_ID(N'[notebooks].[nb_materialized]') AND TYPE IN (N'U'))
            BEGIN
            CREATE TABLE [notebooks].[nb_materialized](
                materialized_id INT PRIMARY KEY IDENTITY(1,1), 
                job_id UNIQUEIDENTIFIER NOT NULL, 
                run_time VARCHAR(100), 
                run_date VARCHAR(100), 
                notebook NVARCHAR(MAX),
                notebook_error NVARCHAR(MAX)
            ) 
            END
            USE [msdb];
            SELECT 
            job_id
            FROM
            msdb.dbo.sysjobs 
            WHERE 
            name= @jobName;
            ";
            List<SqlParameter> notebookDatabaseSetupQueryParams = new List<SqlParameter>();
            notebookDatabaseSetupQueryParams.Add(new SqlParameter("jobName", notebookName));
            DataSet jobIdDataSet =
            await ExecuteSqlQueries(
                connInfo,
                notebookDatabaseSetupQueryString,
                notebookDatabaseSetupQueryParams,
                targetDatabase);
            DataTable jobIdDataTable = jobIdDataSet.Tables[0];
            DataRow jobIdDataRow = jobIdDataTable.Rows[0];
            jobId = ((Guid)jobIdDataRow["job_id"]).ToString();
            StoreNotebookTemplate(
                connInfo,
                jobId,
                templatePath,
                targetDatabase,
                executionDatabase);
            return jobId;
        }

        static async void StoreNotebookTemplate(
            ConnectionInfo connInfo,
            string jobId,
            string templatePath,
            string targetDatabase,
            string executionDatabase)
        {
            string templateFileContents = File.ReadAllText(templatePath);
            string insertTemplateJsonQuery =
            @"
            INSERT 
            INTO 
            notebooks.nb_template(
                job_id, 
                notebook, 
                last_run_notebook_error, 
                execute_database) 
            VALUES 
            (@jobId, @templateFileContents, N'', @executeDatabase)
            ";
            List<SqlParameter> insertTemplateJsonQueryParams = new List<SqlParameter>();
            insertTemplateJsonQueryParams.Add(new SqlParameter("jobId", jobId));
            insertTemplateJsonQueryParams.Add(new SqlParameter("templateFileContents", templateFileContents));
            insertTemplateJsonQueryParams.Add(new SqlParameter("executeDatabase", executionDatabase));
            await ExecuteSqlQueries(
                connInfo,
                insertTemplateJsonQuery,
                insertTemplateJsonQueryParams,
                targetDatabase);
        }

        public static async Task DeleteNotebookMetadata(ConnectionInfo connInfo, string jobId, string targetDatabase)
        {
            string deleteNotebookRowQuery =
            @"
            DELETE FROM notebooks.nb_template
            WHERE 
            job_id = @jobId;
            DELETE FROM notebooks.nb_materialized
            WHERE
            job_id = @jobId;
            IF NOT EXISTS (SELECT * FROM notebooks.nb_template)
            BEGIN
                DROP TABLE notebooks.nb_template;
                DROP TABLE notebooks.nb_materialized;
                DROP SCHEMA notebooks;
            END
            ";
            List<SqlParameter> deleteNotebookRowQueryParams = new List<SqlParameter>();
            deleteNotebookRowQueryParams.Add(new SqlParameter("jobId", jobId));
            await ExecuteSqlQueries(connInfo, deleteNotebookRowQuery, deleteNotebookRowQueryParams, targetDatabase);
        }

        public static async void UpdateNotebookInfo(
            ConnectionInfo connInfo,
            string jobId,
            string templatePath,
            string executionDatabase,
            string targetDatabase)
        {
            if (templatePath != null)
            {
                string templateFileContents = File.ReadAllText(templatePath);
                string insertTemplateJsonQuery =
                @"
                UPDATE notebooks.nb_template 
                SET 
                notebook = @templateFileContents 
                WHERE 
                job_id = @jobId
                ";
                List<SqlParameter> insertTemplateJsonQueryParams = new List<SqlParameter>();
                insertTemplateJsonQueryParams.Add(new SqlParameter("templateFileContents", templateFileContents));
                insertTemplateJsonQueryParams.Add(new SqlParameter("jobId", jobId));
                await ExecuteSqlQueries(
                    connInfo,
                    insertTemplateJsonQuery,
                    insertTemplateJsonQueryParams,
                    targetDatabase);
            }
            string updateExecuteDatabaseQuery =
            @"
                UPDATE notebooks.nb_template 
                SET 
                execute_database = @executeDatabase 
                WHERE 
                job_id = @jobId
            ";
            List<SqlParameter> updateExecuteDatabaseQueryParams = new List<SqlParameter>();
            updateExecuteDatabaseQueryParams.Add(new SqlParameter("executeDatabase", executionDatabase));
            updateExecuteDatabaseQueryParams.Add(new SqlParameter("jobId", jobId));
            await ExecuteSqlQueries(
                connInfo,
                updateExecuteDatabaseQuery,
                updateExecuteDatabaseQueryParams,
                targetDatabase);
        }

        public static async Task<string> GetTemplateFile(
            ConnectionInfo connInfo,
            string job_id,
            string targetDatabase,
            string templateFileContents)
        {
            String getNotebookTemplateQuery =
            @"
            SELECT notebook 
            from 
            notebooks.nb_template
            where
            job_id = @jobId;
            ";
            List<SqlParameter> getNotebookTemplateQueryParams = new List<SqlParameter>();
            getNotebookTemplateQueryParams.Add(new SqlParameter("job_id", getNotebookTemplateQueryParams));
            DataSet templateDataSet = await AgentNotebookHelper.ExecuteSqlQueries(
                connInfo,
                getNotebookTemplateQuery,
                getNotebookTemplateQueryParams,
                targetDatabase);

            DataTable templateDataTable = templateDataSet.Tables[0];
            DataRow templateDataRow = templateDataTable.Rows[0];
            return templateDataRow["notebook"] as string;
        }
    }
}