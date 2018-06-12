//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Resources;
using System.Xml;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Smo.Mail;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for SqlServerAgentPropertiesAlertSystem.
    /// </summary>
    internal class SqlServerAgentPropertiesAlertSystem : ManagementActionBase
    {
        public override void OnRunNow(object sender)
        {
            base.OnRunNow(sender);
            ApplyChanges();
        }

        #region ctors
        public SqlServerAgentPropertiesAlertSystem(CDataContainer dataContainer)
        {        
            DataContainer = dataContainer;
        }

        #endregion

        #region Implementation

        /// <summary>
        /// 
        /// </summary>
        private void ApplyChanges()
        {
            //this.ExecutionMode = ExecutionMode.Success;

            // JobServer agent = DataContainer.Server.JobServer;
            // bool isSQL2005 = this.DataContainer.Server.Information.Version.Major >= SQL2005;

            // bool AlterValues = false;
            // bool AlterAlertValues = false;
            // bool MustAlterProfile = false;

            // try
            // {
            //     // mail system
            //     AgentMailType mailType;
            //     if (isSQL2005)
            //     {
            //         mailType = agent.AgentMailType;
            //     }
            //     else
            //     {
            //         mailType = AgentMailType.SqlAgentMail;
            //     }
            //     if (null != this.comboBoxMailSystem.SelectedItem)
            //     {
            //         if (this.comboBoxMailSystem.SelectedItem.ToString() == SqlServerAgentSR.SQLMail)
            //         {
            //             if (mailType != AgentMailType.SqlAgentMail)
            //             {
            //                 AlterValues = true;
            //                 MustAlterProfile = true;
            //                 mailType = AgentMailType.SqlAgentMail;
            //                 agent.AgentMailType = AgentMailType.SqlAgentMail;
            //             }
            //         }
            //         else if (this.comboBoxMailSystem.SelectedItem.ToString() == SqlServerAgentSR.SQLIMail)
            //         {
            //             if (mailType != AgentMailType.DatabaseMail)
            //             {
            //                 AlterValues = true;
            //                 MustAlterProfile = true;
            //                 mailType = AgentMailType.DatabaseMail;
            //                 agent.AgentMailType = AgentMailType.DatabaseMail;
            //             }
            //         }
            //         else
            //         {
            //             System.Diagnostics.Debug.Assert(false, "unknown selection for mail system");
            //         }
            //     }

            //     if (this.checkEnableMailProfile.Checked == false)
            //     {
            //         // disable profiles
            //         if (
            //             (agent.SqlAgentMailProfile.Length != 0) ||
            //             (true == isSQL2005 && agent.DatabaseMailProfile.Length != 0)
            //             )
            //         {
            //             AlterValues = true;

            //             agent.SqlAgentMailProfile = String.Empty;
            //             if (true == isSQL2005)
            //             {
            //                 agent.DatabaseMailProfile = String.Empty;
            //             }
            //         }
            //     }
            //     else
            //     {
            //         // enable profiles
            //         if
            //             (
            //             (agent.SqlAgentMailProfile.Length == 0) &&
            //             (true == isSQL2005 && agent.DatabaseMailProfile.Length == 0)
            //             )
            //         {
            //             AlterValues = true;
            //             MustAlterProfile = true;
            //         }

            //         if
            //             (
            //             (mailType == AgentMailType.SqlAgentMail) &&
            //             (
            //                 (agent.SqlAgentMailProfile != this.comboMailProfiles.Text) ||
            //                 (MustAlterProfile == true)
            //                 )
            //             )
            //         {
            //             AlterValues = true;
            //             agent.SqlAgentMailProfile = this.comboMailProfiles.Text;
            //         }

            //         if
            //             ((true == isSQL2005) &&
            //              (mailType == AgentMailType.DatabaseMail) &&
            //              (
            //                  (agent.DatabaseMailProfile != this.comboMailProfiles.Text) ||
            //                  (MustAlterProfile == true)
            //                  )
            //             )
            //         {
            //             AlterValues = true;
            //             agent.DatabaseMailProfile = this.comboMailProfiles.Text;
            //         }
            //     }

            //     // save to sent folder

            //     if (this.checkSaveMailsToSentFolder.Checked != agent.SaveInSentFolder)
            //     {
            //         AlterValues = true;
            //         agent.SaveInSentFolder = this.checkSaveMailsToSentFolder.Checked;
            //     }

            //     // rest of ui

            //     string LineTemplate = this.textToPrefix.Text;

            //     if (true == this.checkToAddress.Checked)
            //     {
            //         LineTemplate += AlertEnginePagerAddressToken + this.textToSuffix.Text;
            //     }

            //     if (LineTemplate != agent.AlertSystem.PagerToTemplate)
            //     {
            //         AlterAlertValues = true;
            //         agent.AlertSystem.PagerToTemplate = LineTemplate;
            //     }

            //     LineTemplate = this.textCCPrfix.Text;
            //     if (true == this.checkCCAddress.Checked)
            //     {
            //         LineTemplate += AlertEnginePagerAddressToken + this.textCCSuffix.Text;
            //     }
            //     if (LineTemplate != agent.AlertSystem.PagerCCTemplate)
            //     {
            //         AlterAlertValues = true;
            //         agent.AlertSystem.PagerCCTemplate = LineTemplate;
            //     }

            //     LineTemplate = this.textSubjectPrefix.Text + AlertEngineSubjectToken + this.textSubjectSuffix.Text;
            //     if (LineTemplate != agent.AlertSystem.PagerSubjectTemplate &&
            //         agent.AlertSystem.PagerSubjectTemplate.Length > 0)
            //     {
            //         AlterAlertValues = true;
            //         agent.AlertSystem.PagerSubjectTemplate = LineTemplate;
            //     }

            //     if (true == isSQL2005)
            //     {
            //         bool isTokenRplacementEnabled = this.checkBoxTokenReplacement.Checked;
            //         if (isTokenRplacementEnabled != agent.ReplaceAlertTokensEnabled)
            //         {
            //             AlterValues = true;
            //             agent.ReplaceAlertTokensEnabled = isTokenRplacementEnabled;
            //         }
            //     }

            //     if (this.checkPagerIncludeBody.Checked != !agent.AlertSystem.PagerSendSubjectOnly)
            //     {
            //         AlterAlertValues = true;
            //         agent.AlertSystem.PagerSendSubjectOnly = !this.checkPagerIncludeBody.Checked;
            //     }

            //     string currentOperator = agent.AlertSystem.FailSafeOperator;
            //     string selectedOperator = Convert.ToString(this.comboOperators.SelectedItem,
            //         System.Globalization.CultureInfo.CurrentUICulture);

            //     if (this.checkEnableOperator.Checked &&
            //         (currentOperator.Length == 0 || (currentOperator != selectedOperator)))
            //     {
            //         // update the operator to the new value
            //         AlterAlertValues = true;
            //         agent.AlertSystem.FailSafeOperator = selectedOperator;
            //     }
            //     else if (!this.checkEnableOperator.Checked && currentOperator.Length > 0)
            //     {
            //         // reset the operator
            //         AlterAlertValues = true;
            //         agent.AlertSystem.FailSafeOperator = string.Empty;
            //     }

            //     int CurrentNotifyValue = 0;
            //     if (this.checkNotifyEmail.Checked == true)
            //     {
            //         CurrentNotifyValue |= (int) NotifyMethods.NotifyEmail;
            //     }
                
            //     if (this.checkNotifyPager.Checked == true)
            //     {
            //         CurrentNotifyValue |= (int) NotifyMethods.Pager;
            //     }

            //     if (true == this.checkEnableOperator.Checked)
            //     {
            //         if (CurrentNotifyValue != (int) agent.AlertSystem.NotificationMethod)
            //         {
            //             AlterAlertValues = true;
            //             agent.AlertSystem.NotificationMethod =
            //                 (Microsoft.SqlServer.Management.Smo.Agent.NotifyMethods) CurrentNotifyValue;
            //         }
            //     }
            //     else
            //     {
            //         if (agent.AlertSystem.NotificationMethod != NotifyMethods.None)
            //         {
            //             AlterAlertValues = true;
            //             agent.AlertSystem.NotificationMethod = NotifyMethods.None;
            //         }
            //     }

            //     if (true == AlterAlertValues)
            //     {
            //         agent.AlertSystem.Alter();
            //     }

            //     if (true == AlterValues)
            //     {
            //         agent.Alter();
            //     }
            // }
            // catch (SmoException smoex)
            // {
            //     DisplayExceptionMessage(smoex);
            //     this.ExecutionMode = ExecutionMode.Failure;
            // }

        }

        /// <summary>
        /// 
        /// </summary>
        // private void InitProperties()
        // {
        //     JobServer agent = DataContainer.Server.JobServer;

        //     bool isSql2005 = DataContainer.Server.Information.Version.Major >= SQL2005;
        //     bool isSqlIMailEnabled = isSql2005 && this.DataContainer.Server.Databases["msdb"].IsMailHost == true;

        //     ExtendedStoredProcedure sendMail =
        //         this.DataContainer.Server.Databases["master"].ExtendedStoredProcedures["xp_sendmail", "sys"];

        //     bool isSqlMailEnabled = (null != sendMail);


        //     //bool isSqlMailEnabled = this.DataContainer.Server.Databases["master"].ExtendedStoredProcedures.Contains("sys.xp_sendmail");

        //     bool isMailEnabled = isSqlIMailEnabled || isSqlMailEnabled;

        //     this.checkEnableMailProfile.Enabled = isMailEnabled;

        //     this.checkEnableMailProfile.Checked = (isMailEnabled == true) &&
        //                                           ((agent.SqlAgentMailProfile.Length != 0) ||
        //                                            (isSql2005 && agent.DatabaseMailProfile.Length != 0));

        //     if (true == isSql2005)
        //     {
        //         bool isTokenReplacementEnabled = agent.ReplaceAlertTokensEnabled;
        //         this.checkBoxTokenReplacement.Enabled = true;
        //         this.checkBoxTokenReplacement.Checked = isTokenReplacementEnabled;
        //     }
        //     else
        //     {
        //         this.checkBoxTokenReplacement.Enabled = false;
        //         this.checkBoxTokenReplacement.Checked = false;
        //     }

        //     string ToLineTemplate = agent.AlertSystem.PagerToTemplate;
        //     int pos = ToLineTemplate.IndexOf(AlertEnginePagerAddressToken, StringComparison.Ordinal);
        //     if (pos > 0)
        //     {
        //         this.textToPrefix.Text = ToLineTemplate.Substring(0, pos);
        //         this.checkToAddress.Checked = true;
        //         pos += AlertEnginePagerAddressToken.Length;
        //         this.textToSuffix.Text = ToLineTemplate.Substring(pos, ToLineTemplate.Length - pos);
        //     }
        //     else
        //     {
        //         this.textToPrefix.Text = ToLineTemplate;
        //         this.checkToAddress.Checked = false;
        //         this.textToSuffix.Enabled = false;
        //     }

        //     string CcLineTemplate = agent.AlertSystem.PagerCCTemplate;
        //     pos = CcLineTemplate.IndexOf(AlertEnginePagerAddressToken, StringComparison.Ordinal);
        //     if (pos > 0)
        //     {
        //         this.textCCPrfix.Text = CcLineTemplate.Substring(0, pos);
        //         this.checkCCAddress.Checked = true;
        //         pos += AlertEnginePagerAddressToken.Length;
        //         this.textCCSuffix.Text = CcLineTemplate.Substring(pos, CcLineTemplate.Length - pos);
        //     }
        //     else
        //     {
        //         this.textCCPrfix.Text = CcLineTemplate;
        //         this.checkCCAddress.Checked = false;
        //         this.textCCSuffix.Enabled = false;
        //     }

        //     string SubjectLineTemplate = agent.AlertSystem.PagerSubjectTemplate;
        //     pos = SubjectLineTemplate.IndexOf(AlertEngineSubjectToken, StringComparison.Ordinal);

        //     if (pos > -1)
        //     {
        //         this.textSubjectPrefix.Text = SubjectLineTemplate.Substring(0, pos);
        //         pos += AlertEngineSubjectToken.Length;
        //         this.textSubjectSuffix.Text = SubjectLineTemplate.Substring(pos, SubjectLineTemplate.Length - pos);
        //     }
        //     else
        //     {
        //         /// We should throw ex here 
        //     }


        //     try
        //     {
        //         this.checkPagerIncludeBody.Checked = !agent.AlertSystem.PagerSendSubjectOnly;
        //     }
        //     catch (SmoException)
        //     {
        //         this.checkPagerIncludeBody.Checked = false;
        //         this.checkPagerIncludeBody.Enabled = false;
        //     }

        //     PopulateOperatorsCombo();


        //     try
        //     {
        //         bool enable = this.checkEnableOperator.Checked;
        //         this.checkNotifyEmail.Enabled = enable;
        //         this.checkNotifyPager.Enabled = enable;
                
        //         this.checkNotifyEmail.Checked = ((int) NotifyMethods.NotifyEmail &
        //                                          (int) agent.AlertSystem.NotificationMethod) > 0;
                
        //         this.checkNotifyPager.Checked = ((int) NotifyMethods.Pager & (int) agent.AlertSystem.NotificationMethod) >
        //                                         0;
        //     }
        //     catch (SmoException)
        //     {
        //         this.checkNotifyEmail.Checked = false;
        //         this.checkNotifyPager.Checked = false;
        //         this.checkNotifyEmail.Enabled = false;
        //         this.checkNotifyPager.Enabled = false;
        //     }

        //     if (true == isMailEnabled)
        //     {
        //         AgentMailType mailType;
        //         if (isSql2005 == true)
        //         {
        //             if (agent.AgentMailType == AgentMailType.SqlAgentMail && true == isSqlMailEnabled &&
        //                 0 < agent.SqlAgentMailProfile.Length)
        //             {
        //                 mailType = agent.AgentMailType;
        //             }
        //             else
        //             {
        //                 mailType = AgentMailType.DatabaseMail;
        //             }
        //         }
        //         else
        //         {
        //             mailType = AgentMailType.SqlAgentMail;
        //         }

        //         PopulateMailSystemsCombo(mailType, DataContainer.Server.Information.Version);
        //         //PopulateMailProfilesCombo(mailType);
        //         EnableDisableProfilesUI(agent);
        //     }
        //     else
        //     {
        //         this.comboBoxMailSystem.Enabled = false;
        //         this.comboMailProfiles.Enabled = false;
        //         this.buttonTest.Enabled = false;
        //         this.checkSaveMailsToSentFolder.Enabled = false;
        //     }
        // }

        #endregion

        /// <summary>
        /// Enumerates the sqlmail profiles 
        /// </summary>
        /// <param name="server"></param>
        /// <param name="mailProfiles"></param>
        private void EnumerateSqlMailProfiles(Microsoft.SqlServer.Management.Smo.Server server,
            StringCollection mailProfiles)
        {
            DataSet ds =
                server.ConnectionContext.ExecuteWithResults("master.dbo.xp_sqlagent_notify N'M', null, null, null, N'E'");
            if (null != ds && ds.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    mailProfiles.Add(dr[0].ToString());
                }
            }
        }

        #region Dispose

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {              
            }
            base.Dispose(disposing);
        }


        #endregion
    }
}
