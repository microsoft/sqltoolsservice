using Microsoft.SqlServer.Management.Sdk.Sfc;
using System;
using System.Drawing;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Xml;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Smo.Mail;
using Microsoft.SqlServer.Management.Common;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Resources;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Admin;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for SqlServerAgentPropertiesAlertSystem.
    /// </summary>
    // internal class SqlServerAgentPropertiesAlertSystem : UserControl
    internal class SqlServerAgentPropertiesAlertSystem : ManagementActionBase
    {
        #region Consts

        private const string AlertEnginePagerAddressToken = "<#A#>";
        private const string AlertEngineSubjectToken = "<#S#>";
        private string PagerAddressConst = "<PagerAddress>";
        private string SubjectConst = "<Subject>";

        private const int indexSQLMail = 0;
        private const int indexSQLIMail = 1;
        private const int SQL2005 = 9;

        #endregion

        #region UI controls members

        /// <summary>
        /// Required designer variable.
        /// </summary>
        // private System.ComponentModel.Container components = null;

        // private Microsoft.SqlServer.Management.Controls.Separator separatorMailProfile;
        // private System.Windows.Forms.CheckBox checkEnableMailProfile;
        // private System.Windows.Forms.Label labelMailProfile;
        // private System.Windows.Forms.ComboBox comboMailProfiles;
        // private System.Windows.Forms.CheckBox checkSaveMailsToSentFolder;
        // private Microsoft.SqlServer.Management.Controls.Separator separatorPager;
        // private System.Windows.Forms.Label labelPagerFormatting;
        // private System.Windows.Forms.Label labelPrefix;
        // private System.Windows.Forms.Label labelPagerAddress;
        // private System.Windows.Forms.Label labelSuffix;
        // private System.Windows.Forms.Label labelToLine;
        // private System.Windows.Forms.TextBox textToPrefix;
        // private System.Windows.Forms.CheckBox checkToAddress;
        // private System.Windows.Forms.TextBox textToSuffix;
        // private System.Windows.Forms.Label labelCCLine;
        // private System.Windows.Forms.TextBox textCCPrfix;
        // private System.Windows.Forms.CheckBox checkCCAddress;
        // private System.Windows.Forms.TextBox textCCSuffix;
        // private System.Windows.Forms.Label labelSubject;
        // private System.Windows.Forms.TextBox textSubjectPrefix;
        // private System.Windows.Forms.TextBox textSubjectSuffix;
        // private System.Windows.Forms.TextBox textPagerExample;
        // private Microsoft.SqlServer.Management.Controls.Separator separatorOperator;
        // private System.Windows.Forms.CheckBox checkPagerIncludeBody;
        // private System.Windows.Forms.Label labelOperator;
        // private System.Windows.Forms.CheckBox checkEnableOperator;
        // private System.Windows.Forms.ComboBox comboOperators;
        // private System.Windows.Forms.Label labelNotifyUsing;
        // private System.Windows.Forms.CheckBox checkNotifyEmail;
        // private System.Windows.Forms.CheckBox checkNotifyPager;
        // private System.Windows.Forms.Button buttonTest;
        // private System.Windows.Forms.Label labelMailSystem;
        // private System.Windows.Forms.ComboBox comboBoxMailSystem;
        // private Microsoft.SqlServer.Management.Controls.Separator separatorTokenReplacement;
        // private CheckBox checkBoxTokenReplacement;

        #endregion

        #region Trace support

        public const string m_strComponentName = "SqlServerAgentPropAdvanced";

        private string ComponentName
        {
            get { return m_strComponentName; }
        }

        #endregion

        #region IPanenForm Implementation

        // UserControl IPanelForm.Panel
        // {
        //     get { return this; }
        // }


        /// <summary>
        /// IPanelForm.OnInitialization
        /// 
        /// TODO - in order to reduce IPanelForm container load time
        /// and to improve performance, IPanelForm-s should be able
        /// to lazy-initialize themself when IPanelForm.OnInitialization
        /// is called (a continer like TreePanelForm calls the
        /// OnInitialization() method before first OnSelection())
        /// </summary>
        // void IPanelForm.OnInitialization()
        // {
        //     InitProperties();
        // }


        // public override void OnRunNow(object sender)
        // {
        //     base.OnRunNow(sender);
        //     ApplyChanges();
        // }


        // public override void OnReset(object sender)
        // {
        //     base.OnReset(sender);

        //     this.DataContainer.Server.JobServer.Refresh();
        //     this.DataContainer.Server.JobServer.AlertSystem.Refresh();
        //     InitProperties();
        // }


        // void IPanelForm.OnSelection(TreeNode node)
        // {
        // }


        // void IPanelForm.OnPanelLoseSelection(TreeNode node)
        // {
        // }

        #endregion

        #region ctors

        public SqlServerAgentPropertiesAlertSystem()
        {
        }

        public SqlServerAgentPropertiesAlertSystem(CDataContainer dataContainer)
        {        
            DataContainer = dataContainer;
            // PagerAddressConst = SqlServerAgentSR.ConstPagerAddress;
            // SubjectConst = SqlServerAgentSR.ConstSubject;
            // this.HelpF1Keyword = AssemblyVersionInfo.VersionHelpKeywordPrefix + @".ag.agent.alert.f1";
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

        #region Helpers

        // public void PopulateOperatorsCombo()
        // {
        //     this.comboOperators.Items.Clear();

        //     JobServer agent = DataContainer.Server.JobServer;

        //     foreach (Microsoft.SqlServer.Management.Smo.Agent.Operator op in agent.Operators)
        //     {
        //         this.comboOperators.Items.Add(op.Name);
        //     }

        //     if (agent.AlertSystem.FailSafeOperator.Length != 0 &&
        //         this.comboOperators.Items.Contains(agent.AlertSystem.FailSafeOperator))
        //     {
        //         this.comboOperators.SelectedItem = agent.AlertSystem.FailSafeOperator;
        //     }
        //     else
        //     {
        //         if (this.comboOperators.Items.Count > 0)
        //         {
        //             this.comboOperators.SelectedIndex = 0;
        //         }
        //     }

        //     bool haveOperators = (0 != this.comboOperators.Items.Count);
        //     this.checkEnableOperator.Enabled = haveOperators;
        //     this.checkEnableOperator.Checked = agent.AlertSystem.FailSafeOperator.Length != 0;
        //     this.comboOperators.Enabled = haveOperators && this.checkEnableOperator.Checked;
        // }

        // public void PopulateMailSystemsCombo(AgentMailType agentMailType, Version version)
        // {
        //     this.comboBoxMailSystem.Items.Clear();

        //     bool isMsdbMailHost = (int) (DataContainer.Server.Information.Version.Major) >= SQL2005 &&
        //                           this.DataContainer.Server.Databases["msdb"].IsMailHost == true;
        //     //bool isXpSendMailRegistered = this.DataContainer.Server.Databases["master"].ExtendedStoredProcedures.Contains("xp_sendmail");
        //     ExtendedStoredProcedure sendMail =
        //         this.DataContainer.Server.Databases["master"].ExtendedStoredProcedures["xp_sendmail", "sys"];

        //     bool isXpSendMailRegistered = (null != sendMail);

        //     if (false == isMsdbMailHost || false == isXpSendMailRegistered)
        //     {
        //         this.comboBoxMailSystem.Enabled = false;
        //     }
        //     if (isXpSendMailRegistered == true)
        //     {
        //         this.comboBoxMailSystem.Items.Add(SqlServerAgentSR.SQLMail);
        //     }
        //     if ((int) (version.Major) >= SQL2005 && true == isMsdbMailHost)
        //     {
        //         this.comboBoxMailSystem.Items.Add(SqlServerAgentSR.SQLIMail);
        //     }
        //     if (this.comboBoxMailSystem.Items.Count > 1)
        //     {
        //         this.comboBoxMailSystem.Enabled = true;
        //         switch (agentMailType)
        //         {
        //             case AgentMailType.SqlAgentMail:
        //                 this.comboBoxMailSystem.SelectedIndex = indexSQLMail;
        //                 break;
        //             case AgentMailType.DatabaseMail:
        //                 this.comboBoxMailSystem.SelectedIndex = indexSQLIMail;
        //                 break;
        //             default:
        //                 System.Diagnostics.Debug.Assert(false, "unknown agent mail type");
        //                 this.comboBoxMailSystem.Enabled = false;
        //                 break;
        //         }
        //     }
        //     else
        //     {
        //         this.comboBoxMailSystem.Enabled = false;
        //         this.comboBoxMailSystem.SelectedIndex = 0;
        //     }
        // }

        // public void PopulateMailProfilesCombo(AgentMailType agentMailType)
        // {
        //     this.comboMailProfiles.Items.Clear();

        //     switch (agentMailType)
        //     {
        //         case AgentMailType.SqlAgentMail:
        //             PopulateMailProfileComboInternal(agentMailType, DataContainer.Server.JobServer.SqlAgentMailProfile);
        //             break;

        //         case AgentMailType.DatabaseMail:
        //             PopulateMailProfileComboInternal(agentMailType, DataContainer.Server.JobServer.DatabaseMailProfile);
        //             break;

        //         default:
        //             System.Diagnostics.Debug.Assert(false, "unknown AgentMailType");
        //             this.comboMailProfiles.Enabled = false;
        //             break;
        //     }
        // }

        // private void PopulateMailProfileComboInternal(AgentMailType agentMailType, string selectedProfile)
        // {
        //     this.comboMailProfiles.Items.Clear();
        //     if (agentMailType == AgentMailType.SqlAgentMail)
        //     {
        //         // MAPI
        //         StringCollection profiles = new StringCollection();

        //         if (selectedProfile.Length == 0)
        //         {
        //             selectedProfile = DataContainer.Server.JobServer.SqlAgentMailProfile;
        //         }

        //         try
        //         {
        //             EnumerateSqlMailProfiles(this.DataContainer.Server, profiles);
        //         }
        //         catch (Exception ex)
        //         {
        //             System.Diagnostics.Debug.WriteLine(ex.Message);
        //         }

        //         int ProfilesCount = profiles.Count;

        //         if (0 == ProfilesCount)
        //         {
        //             // if no profiles use outlook default profile
        //             this.comboMailProfiles.Enabled = false;
        //             this.comboMailProfiles.Items.Add(SqlServerAgentSR.OutlookDefault);
        //             this.comboMailProfiles.SelectedIndex = 0;
        //         }
        //         else
        //         {
        //             // load profiles
        //             int selidx = 0;
        //             foreach (string profileString in profiles)
        //             {
        //                 string profile = profileString.Trim();
        //                 int idx = this.comboMailProfiles.Items.Add(profile);
        //                 if (profile == selectedProfile)
        //                 {
        //                     selidx = idx;
        //                 }
        //             }

        //             this.comboMailProfiles.Text = selectedProfile;
        //             if (selectedProfile.Length == 0)
        //             {
        //                 this.comboMailProfiles.SelectedIndex = 0;
        //             }
        //             else
        //             {
        //                 this.comboMailProfiles.SelectedItem = selidx;

        //             }
        //         }
        //     }
        //     else if (agentMailType == AgentMailType.DatabaseMail)
        //     {
        //         // SMTP
        //         // if (DataContainer.Server.Mail.Profiles.Count > 0)
        //         // {
        //         //     if (selectedProfile.Length == 0)
        //         //     {
        //         //         selectedProfile = DataContainer.Server.JobServer.DatabaseMailProfile;
        //         //     }

        //         //     foreach (MailProfile mailProfile in DataContainer.Server.Mail.Profiles)
        //         //     {
        //         //         this.comboMailProfiles.Items.Add(mailProfile.Name);
        //         //     }

        //         //     this.comboMailProfiles.SelectedItem = selectedProfile;
        //         // }
        //         // else
        //         // {
        //         //     this.comboMailProfiles.Text = string.Empty;
        //         //     this.comboMailProfiles.Enabled = false;
        //         // }
        //     }
        //     else
        //     {
        //         System.Diagnostics.Debug.Assert(false);
        //     }
        // }

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

        // private void EnableDisableProfilesUI(JobServer agent)
        // {
        //     try
        //     {
        //         bool IsChecked = this.checkEnableMailProfile.Checked;

        //         this.comboBoxMailSystem.Enabled = IsChecked && (this.DataContainer.Server.Information.Version.Major > 8);
        //         this.comboMailProfiles.Enabled = IsChecked;
        //         this.buttonTest.Enabled = IsChecked && (this.comboMailProfiles.Text.Length != 0);

        //         bool isSqlIMailSelected = false;

        //         // $FUTURE - if SMO adds support for testing SQLIMail profiles (SMTP) for Agent account enable it
        //         if (this.comboBoxMailSystem.SelectedItem.ToString() == SqlServerAgentSR.SQLIMail)
        //         {
        //             this.buttonTest.Enabled = false;
        //             isSqlIMailSelected = true;
        //         }

        //         bool mailProfileEnabled = IsChecked;
        //         bool haveProfileSelected = this.comboMailProfiles.Text.Length > 0;
        //         if (true == isSqlIMailSelected)
        //         {
        //             this.checkSaveMailsToSentFolder.Enabled = false;
        //             this.checkSaveMailsToSentFolder.Checked = true;
        //         }
        //         else
        //         {
        //             this.checkSaveMailsToSentFolder.Enabled = IsChecked && haveProfileSelected;
        //             this.checkSaveMailsToSentFolder.Checked = agent.SaveInSentFolder;
        //         }
        //     }
        //     catch (Exception)
        //     {
        //         /// UI not yet fully initialized , do nothing
        //     }

        // }

        #endregion

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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        // private void InitializeComponent()
        // {
        //     System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SqlServerAgentPropertiesAlertSystem));
        //     this.separatorMailProfile = new Microsoft.SqlServer.Management.Controls.Separator();
        //     this.checkEnableMailProfile = new System.Windows.Forms.CheckBox();
        //     this.labelMailProfile = new System.Windows.Forms.Label();
        //     this.comboMailProfiles = new System.Windows.Forms.ComboBox();
        //     this.checkSaveMailsToSentFolder = new System.Windows.Forms.CheckBox();
        //     this.separatorPager = new Microsoft.SqlServer.Management.Controls.Separator();
        //     this.labelPagerFormatting = new System.Windows.Forms.Label();
        //     this.labelPrefix = new System.Windows.Forms.Label();
        //     this.labelPagerAddress = new System.Windows.Forms.Label();
        //     this.labelSuffix = new System.Windows.Forms.Label();
        //     this.labelToLine = new System.Windows.Forms.Label();
        //     this.textToPrefix = new System.Windows.Forms.TextBox();
        //     this.checkToAddress = new System.Windows.Forms.CheckBox();
        //     this.textToSuffix = new System.Windows.Forms.TextBox();
        //     this.labelCCLine = new System.Windows.Forms.Label();
        //     this.textCCPrfix = new System.Windows.Forms.TextBox();
        //     this.checkCCAddress = new System.Windows.Forms.CheckBox();
        //     this.textCCSuffix = new System.Windows.Forms.TextBox();
        //     this.labelSubject = new System.Windows.Forms.Label();
        //     this.textSubjectPrefix = new System.Windows.Forms.TextBox();
        //     this.textSubjectSuffix = new System.Windows.Forms.TextBox();
        //     this.textPagerExample = new System.Windows.Forms.TextBox();
        //     this.checkPagerIncludeBody = new System.Windows.Forms.CheckBox();
        //     this.separatorOperator = new Microsoft.SqlServer.Management.Controls.Separator();
        //     this.labelOperator = new System.Windows.Forms.Label();
        //     this.checkEnableOperator = new System.Windows.Forms.CheckBox();
        //     this.comboOperators = new System.Windows.Forms.ComboBox();
        //     this.labelNotifyUsing = new System.Windows.Forms.Label();
        //     this.checkNotifyEmail = new System.Windows.Forms.CheckBox();
        //     this.checkNotifyPager = new System.Windows.Forms.CheckBox();
        //     this.buttonTest = new System.Windows.Forms.Button();
        //     this.labelMailSystem = new System.Windows.Forms.Label();
        //     this.comboBoxMailSystem = new System.Windows.Forms.ComboBox();
        //     this.separatorTokenReplacement = new Microsoft.SqlServer.Management.Controls.Separator();
        //     this.checkBoxTokenReplacement = new System.Windows.Forms.CheckBox();
        //     this.SuspendLayout();
        //     // 
        //     // separatorMailProfile
        //     // 
        //     resources.ApplyResources(this.separatorMailProfile, "separatorMailProfile");
        //     this.separatorMailProfile.Name = "separatorMailProfile";
        //     // 
        //     // checkEnableMailProfile
        //     // 
        //     resources.ApplyResources(this.checkEnableMailProfile, "checkEnableMailProfile");
        //     this.checkEnableMailProfile.Name = "checkEnableMailProfile";
        //     this.checkEnableMailProfile.CheckedChanged += new System.EventHandler(this.checkEnableMailProfile_CheckedChanged);
        //     // 
        //     // labelMailProfile
        //     // 
        //     resources.ApplyResources(this.labelMailProfile, "labelMailProfile");
        //     this.labelMailProfile.Name = "labelMailProfile";
        //     // 
        //     // comboMailProfiles
        //     // 
        //     resources.ApplyResources(this.comboMailProfiles, "comboMailProfiles");
        //     this.comboMailProfiles.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        //     this.comboMailProfiles.FormattingEnabled = true;
        //     this.comboMailProfiles.Name = "comboMailProfiles";
        //     this.comboMailProfiles.SelectedIndexChanged += new System.EventHandler(this.comboMailProfiles_SelectedIndexChanged);
        //     // 
        //     // checkSaveMailsToSentFolder
        //     // 
        //     resources.ApplyResources(this.checkSaveMailsToSentFolder, "checkSaveMailsToSentFolder");
        //     this.checkSaveMailsToSentFolder.Name = "checkSaveMailsToSentFolder";
        //     this.checkSaveMailsToSentFolder.CheckedChanged += new System.EventHandler(this.checkSaveMailsToSentFolder_CheckedChanged);
        //     // 
        //     // separatorPager
        //     // 
        //     resources.ApplyResources(this.separatorPager, "separatorPager");
        //     this.separatorPager.Name = "separatorPager";
        //     // 
        //     // labelPagerFormatting
        //     // 
        //     resources.ApplyResources(this.labelPagerFormatting, "labelPagerFormatting");
        //     this.labelPagerFormatting.Name = "labelPagerFormatting";
        //     // 
        //     // labelPrefix
        //     // 
        //     resources.ApplyResources(this.labelPrefix, "labelPrefix");
        //     this.labelPrefix.Name = "labelPrefix";
        //     // 
        //     // labelPagerAddress
        //     // 
        //     resources.ApplyResources(this.labelPagerAddress, "labelPagerAddress");
        //     this.labelPagerAddress.Name = "labelPagerAddress";
        //     // 
        //     // labelSuffix
        //     // 
        //     resources.ApplyResources(this.labelSuffix, "labelSuffix");
        //     this.labelSuffix.Name = "labelSuffix";
        //     // 
        //     // labelToLine
        //     // 
        //     resources.ApplyResources(this.labelToLine, "labelToLine");
        //     this.labelToLine.Name = "labelToLine";
        //     // 
        //     // textToPrefix
        //     // 
        //     resources.ApplyResources(this.textToPrefix, "textToPrefix");
        //     this.textToPrefix.Name = "textToPrefix";
        //     this.textToPrefix.TextChanged += new System.EventHandler(this.textToPrefix_TextChanged);
        //     // 
        //     // checkToAddress
        //     // 
        //     resources.ApplyResources(this.checkToAddress, "checkToAddress");
        //     this.checkToAddress.Name = "checkToAddress";
        //     this.checkToAddress.CheckedChanged += new System.EventHandler(this.checkToAddress_CheckedChanged);
        //     // 
        //     // textToSuffix
        //     // 
        //     resources.ApplyResources(this.textToSuffix, "textToSuffix");
        //     this.textToSuffix.Name = "textToSuffix";
        //     this.textToSuffix.TextChanged += new System.EventHandler(this.textToSuffix_TextChanged);
        //     // 
        //     // labelCCLine
        //     // 
        //     resources.ApplyResources(this.labelCCLine, "labelCCLine");
        //     this.labelCCLine.Name = "labelCCLine";
        //     // 
        //     // textCCPrfix
        //     // 
        //     resources.ApplyResources(this.textCCPrfix, "textCCPrfix");
        //     this.textCCPrfix.Name = "textCCPrfix";
        //     this.textCCPrfix.TextChanged += new System.EventHandler(this.textCCPrfix_TextChanged);
        //     // 
        //     // checkCCAddress
        //     // 
        //     resources.ApplyResources(this.checkCCAddress, "checkCCAddress");
        //     this.checkCCAddress.Name = "checkCCAddress";
        //     this.checkCCAddress.CheckedChanged += new System.EventHandler(this.checkCCAddress_CheckedChanged);
        //     // 
        //     // textCCSuffix
        //     // 
        //     resources.ApplyResources(this.textCCSuffix, "textCCSuffix");
        //     this.textCCSuffix.Name = "textCCSuffix";
        //     this.textCCSuffix.TextChanged += new System.EventHandler(this.textCCSuffix_TextChanged);
        //     // 
        //     // labelSubject
        //     // 
        //     resources.ApplyResources(this.labelSubject, "labelSubject");
        //     this.labelSubject.Name = "labelSubject";
        //     // 
        //     // textSubjectPrefix
        //     // 
        //     resources.ApplyResources(this.textSubjectPrefix, "textSubjectPrefix");
        //     this.textSubjectPrefix.Name = "textSubjectPrefix";
        //     this.textSubjectPrefix.TextChanged += new System.EventHandler(this.textSubjectPrefix_TextChanged);
        //     // 
        //     // textSubjectSuffix
        //     // 
        //     resources.ApplyResources(this.textSubjectSuffix, "textSubjectSuffix");
        //     this.textSubjectSuffix.Name = "textSubjectSuffix";
        //     this.textSubjectSuffix.TextChanged += new System.EventHandler(this.textSubjectSuffix_TextChanged);
        //     // 
        //     // textPagerExample
        //     // 
        //     resources.ApplyResources(this.textPagerExample, "textPagerExample");
        //     this.textPagerExample.Name = "textPagerExample";
        //     this.textPagerExample.ReadOnly = true;
        //     // 
        //     // checkPagerIncludeBody
        //     // 
        //     resources.ApplyResources(this.checkPagerIncludeBody, "checkPagerIncludeBody");
        //     this.checkPagerIncludeBody.Name = "checkPagerIncludeBody";
        //     // 
        //     // separatorOperator
        //     // 
        //     resources.ApplyResources(this.separatorOperator, "separatorOperator");
        //     this.separatorOperator.Name = "separatorOperator";
        //     // 
        //     // labelOperator
        //     // 
        //     resources.ApplyResources(this.labelOperator, "labelOperator");
        //     this.labelOperator.Name = "labelOperator";
        //     // 
        //     // checkEnableOperator
        //     // 
        //     resources.ApplyResources(this.checkEnableOperator, "checkEnableOperator");
        //     this.checkEnableOperator.Name = "checkEnableOperator";
        //     this.checkEnableOperator.CheckedChanged += new System.EventHandler(this.checkEnableOperator_CheckedChanged);
        //     // 
        //     // comboOperators
        //     // 
        //     resources.ApplyResources(this.comboOperators, "comboOperators");
        //     this.comboOperators.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        //     this.comboOperators.FormattingEnabled = true;
        //     this.comboOperators.Name = "comboOperators";
        //     // 
        //     // labelNotifyUsing
        //     // 
        //     resources.ApplyResources(this.labelNotifyUsing, "labelNotifyUsing");
        //     this.labelNotifyUsing.Name = "labelNotifyUsing";
        //     // 
        //     // checkNotifyEmail
        //     // 
        //     resources.ApplyResources(this.checkNotifyEmail, "checkNotifyEmail");
        //     this.checkNotifyEmail.Name = "checkNotifyEmail";
        //     // 
        //     // checkNotifyPager
        //     // 
        //     resources.ApplyResources(this.checkNotifyPager, "checkNotifyPager");
        //     this.checkNotifyPager.Name = "checkNotifyPager";
        //     // 
        //     // buttonTest
        //     // 
        //     resources.ApplyResources(this.buttonTest, "buttonTest");
        //     this.buttonTest.Name = "buttonTest";
        //     this.buttonTest.Click += new System.EventHandler(this.buttonTest_Click);
        //     // 
        //     // labelMailSystem
        //     // 
        //     resources.ApplyResources(this.labelMailSystem, "labelMailSystem");
        //     this.labelMailSystem.Name = "labelMailSystem";
        //     // 
        //     // comboBoxMailSystem
        //     // 
        //     resources.ApplyResources(this.comboBoxMailSystem, "comboBoxMailSystem");
        //     this.comboBoxMailSystem.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        //     this.comboBoxMailSystem.FormattingEnabled = true;
        //     this.comboBoxMailSystem.Name = "comboBoxMailSystem";
        //     this.comboBoxMailSystem.SelectedIndexChanged += new System.EventHandler(this.comboBoxMailSystem_SelectedIndexChanged);
        //     // 
        //     // separatorTokenReplacement
        //     // 
        //     resources.ApplyResources(this.separatorTokenReplacement, "separatorTokenReplacement");
        //     this.separatorTokenReplacement.Name = "separatorTokenReplacement";
        //     // 
        //     // checkBoxTokenReplacement
        //     // 
        //     resources.ApplyResources(this.checkBoxTokenReplacement, "checkBoxTokenReplacement");
        //     this.checkBoxTokenReplacement.Name = "checkBoxTokenReplacement";
        //     this.checkBoxTokenReplacement.CheckedChanged += new System.EventHandler(this.checkBoxTokenReplacement_CheckedChanged);
        //     // 
        //     // SqlServerAgentPropertiesAlertSystem
        //     // 
        //     resources.ApplyResources(this, "$this");
        //     this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        //     this.Controls.Add(this.checkBoxTokenReplacement);
        //     this.Controls.Add(this.separatorTokenReplacement);
        //     this.Controls.Add(this.comboBoxMailSystem);
        //     this.Controls.Add(this.labelMailSystem);
        //     this.Controls.Add(this.buttonTest);
        //     this.Controls.Add(this.checkNotifyPager);
        //     this.Controls.Add(this.checkNotifyEmail);
        //     this.Controls.Add(this.labelNotifyUsing);
        //     this.Controls.Add(this.comboOperators);
        //     this.Controls.Add(this.checkEnableOperator);
        //     this.Controls.Add(this.labelOperator);
        //     this.Controls.Add(this.separatorOperator);
        //     this.Controls.Add(this.checkPagerIncludeBody);
        //     this.Controls.Add(this.textPagerExample);
        //     this.Controls.Add(this.textSubjectSuffix);
        //     this.Controls.Add(this.textSubjectPrefix);
        //     this.Controls.Add(this.labelSubject);
        //     this.Controls.Add(this.textCCSuffix);
        //     this.Controls.Add(this.checkCCAddress);
        //     this.Controls.Add(this.textCCPrfix);
        //     this.Controls.Add(this.labelCCLine);
        //     this.Controls.Add(this.textToSuffix);
        //     this.Controls.Add(this.checkToAddress);
        //     this.Controls.Add(this.textToPrefix);
        //     this.Controls.Add(this.labelToLine);
        //     this.Controls.Add(this.labelSuffix);
        //     this.Controls.Add(this.labelPagerAddress);
        //     this.Controls.Add(this.labelPrefix);
        //     this.Controls.Add(this.labelPagerFormatting);
        //     this.Controls.Add(this.separatorPager);
        //     this.Controls.Add(this.checkSaveMailsToSentFolder);
        //     this.Controls.Add(this.comboMailProfiles);
        //     this.Controls.Add(this.labelMailProfile);
        //     this.Controls.Add(this.checkEnableMailProfile);
        //     this.Controls.Add(this.separatorMailProfile);
        //     this.Name = "SqlServerAgentPropertiesAlertSystem";
        //     this.ResumeLayout(false);
        //     this.PerformLayout();

        // }

        #endregion

        #region UI controls event handlers

//         private void checkToAddress_CheckedChanged(object sender, System.EventArgs e)
//         {
//             bool IsChecked = this.checkToAddress.Checked;
//             this.textToSuffix.Enabled = IsChecked;

//             string PagerAddress = (IsChecked == true) ? PagerAddressConst : string.Empty;
//             string Suffix = (IsChecked == true) ? this.textToSuffix.Text : string.Empty;

//             this.textPagerExample.Text = SqlServerAgentSR.To + this.textToPrefix.Text + PagerAddress + Suffix + "\r\n" +
//                                          this.textPagerExample.Lines[1] + "\r\n" + this.textPagerExample.Lines[2];
//         }

//         private void checkCCAddress_CheckedChanged(object sender, System.EventArgs e)
//         {
//             bool IsChecked = this.checkCCAddress.Checked;
//             this.textCCSuffix.Enabled = IsChecked;

//             string PagerAddress = (IsChecked == true) ? PagerAddressConst : string.Empty;
//             string Suffix = (IsChecked == true) ? this.textCCSuffix.Text : string.Empty;

//             this.textPagerExample.Text = this.textPagerExample.Lines[0] + "\r\n" + SqlServerAgentSR.CC +
//                                          this.textCCPrfix.Text + PagerAddress + Suffix + "\r\n" +
//                                          this.textPagerExample.Lines[2];
//         }

//         private void checkEnableMailProfile_CheckedChanged(object sender, System.EventArgs e)
//         {
//             EnableDisableProfilesUI(this.DataContainer.Server.JobServer);
//         }

//         private void checkEnableOperator_CheckedChanged(object sender, System.EventArgs e)
//         {
//             bool IsChecked = this.checkEnableOperator.Checked;

//             if (true == IsChecked)
//             {
//                 this.comboOperators.Enabled = (0 != this.comboOperators.Items.Count);
//             }
//             else
//             {
//                 this.comboOperators.Enabled = IsChecked;
//             }

//             this.checkNotifyEmail.Enabled = IsChecked;
//             this.checkNotifyPager.Enabled = IsChecked;            
//         }

//         private void textToPrefix_TextChanged(System.Object sender, System.EventArgs e)
//         {
//             string PagerAddress = (this.checkToAddress.Checked == true) ? PagerAddressConst : string.Empty;
//             string Suffix = (this.checkToAddress.Checked == true) ? this.textToSuffix.Text : string.Empty;
//             this.textPagerExample.Text = SqlServerAgentSR.To + this.textToPrefix.Text + PagerAddress + Suffix + "\r\n" +
//                                          this.textPagerExample.Lines[1] + "\r\n" + this.textPagerExample.Lines[2];
//         }

//         private void textCCPrfix_TextChanged(System.Object sender, System.EventArgs e)
//         {
//             string PagerAddress = (this.checkCCAddress.Checked == true) ? PagerAddressConst : string.Empty;
//             string Suffix = (this.checkCCAddress.Checked == true) ? this.textCCSuffix.Text : string.Empty;
//             this.textPagerExample.Text = this.textPagerExample.Lines[0] + "\r\n" + SqlServerAgentSR.CC +
//                                          this.textCCPrfix.Text + PagerAddress + Suffix + "\r\n" +
//                                          this.textPagerExample.Lines[2];
//         }

//         private void textSubjectPrefix_TextChanged(System.Object sender, System.EventArgs e)
//         {
//             this.textPagerExample.Text = this.textPagerExample.Lines[0] + "\r\n" + this.textPagerExample.Lines[1] +
//                                          "\r\n" + SqlServerAgentSR.Subject + this.textSubjectPrefix.Text + SubjectConst +
//                                          this.textSubjectSuffix.Text;
//         }

//         private void textToSuffix_TextChanged(System.Object sender, System.EventArgs e)
//         {
//             string PagerAddress = (this.checkToAddress.Checked == true) ? PagerAddressConst : string.Empty;
//             this.textPagerExample.Text = SqlServerAgentSR.To + this.textToPrefix.Text + PagerAddress +
//                                          this.textToSuffix.Text + "\r\n" + this.textPagerExample.Lines[1] + "\r\n" +
//                                          this.textPagerExample.Lines[2];
//         }

//         private void textCCSuffix_TextChanged(System.Object sender, System.EventArgs e)
//         {
//             string PagerAddress = (this.checkCCAddress.Checked == true) ? PagerAddressConst : string.Empty;
//             this.textPagerExample.Text = this.textPagerExample.Lines[0] + "\r\n" + SqlServerAgentSR.CC +
//                                          this.textCCPrfix.Text + PagerAddress + this.textCCSuffix.Text + "\r\n" +
//                                          this.textPagerExample.Lines[2];
//         }

//         private void textSubjectSuffix_TextChanged(System.Object sender, System.EventArgs e)
//         {
//             this.textPagerExample.Text = this.textPagerExample.Lines[0] + "\r\n" + this.textPagerExample.Lines[1] +
//                                          "\r\n" + SqlServerAgentSR.Subject + this.textSubjectPrefix.Text + SubjectConst +
//                                          this.textSubjectSuffix.Text;
//         }

//         private void comboBoxMailSystem_SelectedIndexChanged(object sender, System.EventArgs e)
//         {
//             AgentMailType agentMailType = AgentMailType.DatabaseMail;
//             string selectedItem = comboBoxMailSystem.SelectedItem.ToString();

//             if (selectedItem == SqlServerAgentSR.SQLIMail)
//             {
//                 agentMailType = AgentMailType.DatabaseMail;
//             }
//             else
//             {
//                 agentMailType = AgentMailType.SqlAgentMail;
//             }

//             PopulateMailProfileComboInternal(agentMailType, comboMailProfiles.Text);
//             EnableDisableProfilesUI(this.DataContainer.Server.JobServer);
//         }

//         private void comboMailProfiles_SelectedIndexChanged(object sender, System.EventArgs e)
//         {
//             EnableDisableProfilesUI(this.DataContainer.Server.JobServer);
//         }

//         private void buttonTest_Click(object sender, System.EventArgs e)
//         {
//             Cursor cur = Cursor.Current;
//             try
//             {
//                 Cursor.Current = Cursors.WaitCursor;
//                 if (this.comboBoxMailSystem.SelectedItem.ToString() == SqlServerAgentSR.SQLMail)
//                 {
//                     // test SQLMail (MAPI) profile - using Agent notification xp (not SqlServer xp)
//                     JobServer agent = this.DataContainer.Server.JobServer;
//                     agent.TestMailProfile(this.comboMailProfiles.Text);
//                     DisplayExceptionInfoMessage(new Exception(SqlMailPropertiesSR.SqlMailTestSuccessful));
//                 }
//                 else if (this.comboBoxMailSystem.SelectedItem.ToString() == SqlServerAgentSR.SQLIMail)
//                 {
//                     // test DatabaseMail (SMTP) profile - using Agent notification xp - not yet implemented by SMO
//                     System.Diagnostics.Debug.Assert(false,
//                         "test button should be disabled for DatabaseMail since SMO support for testing DatabaseMail profiles (SMTP) using agent pipe is not yet available");
// //					JobServer agent = this.DataContainer.Server.JobServer;
// //					agent.TestIMailProfile(this.comboMailProfiles.Text);
// //					DisplayExceptionInfoMessage(new Exception(SqlMailPropertiesSR.SqlIMailTestSuccessful));
//                 }
//                 else
//                 {
//                     System.Diagnostics.Debug.Assert(false, "unknown mail system selected");
//                 }
//             }
//             catch (Exception ex)
//             {
//                 STrace.LogExCatch(ex);
//                 DisplayExceptionMessage(ex);
//             }
//             finally
//             {
//                 Cursor.Current = cur;
//             }
//         }

//         private void checkBoxTokenReplacement_CheckedChanged(object sender, EventArgs e)
//         {

//         }

//         private void checkSaveMailsToSentFolder_CheckedChanged(object sender, System.EventArgs e)
//         {
//         }

        #endregion

    }
}








