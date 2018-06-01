using Microsoft.SqlServer.Management.Sdk.Sfc;
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Xml;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Common;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Resources;
using Microsoft.SqlServer.Management.Diagnostics;
using System.Globalization;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Admin;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for SqlServerAgentPropertiesConnection.
    /// </summary>	
    internal class SqlServerAgentPropertiesConnection : ManagementActionBase
    {        
        bool SqlPasswordChanged = false;

        #region UI controls members
        /// <summary>
        /// Required designer variable.
        /// </summary>
        //private System.ComponentModel.Container components = null;
        // private System.Windows.Forms.Label labelAlias;
        // private System.Windows.Forms.TextBox comboAliases;
        // private Microsoft.SqlServer.Management.Controls.Separator separatorSqlConnection;
        // private System.Windows.Forms.RadioButton radioWinAuth;
        // private System.Windows.Forms.RadioButton radioSQLAuth;
        // private System.Windows.Forms.Label labelLogin;
        // private System.Windows.Forms.TextBox textPassword;
        // private System.Windows.Forms.ComboBox comboLogin;
        // private System.Windows.Forms.Label labelPasswd;
        #endregion

        #region Trace support
        public const string m_strComponentName = "SqlServerAgentPropAdvanced";
        private string ComponentName
        {
            get
            {
                return m_strComponentName;
            }
        }
        #endregion

        #region Implementation

        // private void ApplyChanges()
        // {
        //     this.ExecutionMode = ExecutionMode.Success;

        //     JobServer agent = DataContainer.Server.JobServer;

        //     string OriginalLogin = agent.HostLoginName;
        //     string CurrentLogin = "";

        //     bool AlterValues = false;

        //     try
        //     {
        //         if (true == this.radioSQLAuth.Checked)
        //         {
        //             CurrentLogin = (this.comboLogin.SelectedItem).ToString();
        //         }
        //         if (String.Compare(CurrentLogin, OriginalLogin, StringComparison.OrdinalIgnoreCase) != 0 || true == SqlPasswordChanged)
        //         {
        //             if (CurrentLogin.Length > 0)
        //             {
        //                 agent.SetHostLoginAccount(CurrentLogin, this.textPassword.Text);
        //                 VerifyLogin();
        //             }
        //             else
        //             {
        //                 agent.ClearHostLoginAccount();
        //             }
        //         }

        //         string SelectedAlias = this.comboAliases.Text;                

        //         if (String.Compare(SelectedAlias, agent.LocalHostAlias, StringComparison.OrdinalIgnoreCase) != 0)
        //         {
        //             AlterValues = true;

        //             agent.LocalHostAlias = SelectedAlias;

        //         }
        //         if (true == AlterValues)
        //         {
        //             agent.Alter();
        //         }
        //     }
        //     catch (SmoException smoex)
        //     {
        //         DisplayExceptionMessage(smoex);
        //         this.ExecutionMode = ExecutionMode.Failure;
        //     }

        // }

        // private void InitProperties()
        // {            
        //     try
        //     {                
        //         JobServer agent = DataContainer.Server.JobServer;                

        //         if (this.DataContainer.Server.Information.Version.Major < 9)
        //         {

        //             PopulateLoginCombo();

        //             bool IsWinAuth = (agent.HostLoginName.Length == 0);
        //             this.radioWinAuth.Checked = IsWinAuth;
        //             this.radioSQLAuth.Checked = !IsWinAuth;
        //             if (false == IsWinAuth)
        //             {
        //                 string SqlLogin = agent.HostLoginName;
        //                 if (!this.comboLogin.Items.Contains(SqlLogin))
        //                 {
        //                     this.comboLogin.Items.Add(SqlLogin);
        //                 }
        //                 this.comboLogin.SelectedItem = SqlLogin;
        //                 this.textPassword.Text = "**********";
        //                 SqlPasswordChanged = false;
        //             }
        //         }
        //         else
        //         {
        //             this.radioWinAuth.Checked = true;
        //             this.radioWinAuth.Enabled = this.radioSQLAuth.Enabled = this.comboLogin.Enabled = false;
        //             this.textPassword.Enabled = this.labelLogin.Enabled = this.labelPasswd.Enabled = false;
        //         }

        //         string ServerAliasHost = agent.LocalHostAlias;
        //         this.comboAliases.Text = ServerAliasHost;                

        //         // Managed Instances do not allow changing
        //         // "alias local host server"
        //         //
        //         this.comboAliases.Enabled = DataContainer.Server.DatabaseEngineEdition != DatabaseEngineEdition.SqlManagedInstance;
        //     }
        //     catch (Exception)
        //     {                
        //     }
        // }

        #endregion

        #region Helpers        

        // private void PopulateLoginCombo()
        // {
        //     Request req = new Request();
        //     Enumerator enu = new Enumerator();

        //     req.Urn = "Server/Login";

        //     DataSet ds = enu.Process(ServerConnection, req);

        //     for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
        //     {
        //         String szItem = (ds.Tables[0].Rows[i]["Name"]).ToString();
        //         this.comboLogin.Items.Add(szItem);
        //     }
        //     if (this.comboLogin.Items.Count > 0)
        //     {
        //         this.comboLogin.SelectedIndex = 0;
        //     }
        //     else
        //     {
        //         this.comboLogin.Enabled = false;
        //     }
        // }

        // private void VerifyLogin()
        // {
        //     try
        //     {
        //         ServerConnection c = new ServerConnection(this.DataContainer.ServerName, (this.comboLogin.SelectedItem).ToString(), this.textPassword.Text);
        //         c.Connect();
        //     }
        //     catch (SmoException smoex)
        //     {
        //         throw new SmoException(smoex.Message, smoex);                
        //     }
        // }

        #endregion

        #region IPanenForm Implementation

        // UserControl IPanelForm.Panel
        // {
        //     get
        //     {
        //         return this;
        //     }
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

        public SqlServerAgentPropertiesConnection()
        {          
        }

        public SqlServerAgentPropertiesConnection(CDataContainer dataContainer)
        {
            DataContainer = dataContainer;
            //this.HelpF1Keyword = AssemblyVersionInfo.VersionHelpKeywordPrefix + @".ag.agent.connection.f1";
        }

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
        //     System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SqlServerAgentPropertiesConnection));
        //     this.labelAlias = new System.Windows.Forms.Label();
        //     this.comboAliases = new System.Windows.Forms.TextBox();
        //     this.separatorSqlConnection = new Microsoft.SqlServer.Management.Controls.Separator();
        //     this.radioWinAuth = new System.Windows.Forms.RadioButton();
        //     this.radioSQLAuth = new System.Windows.Forms.RadioButton();
        //     this.labelLogin = new System.Windows.Forms.Label();
        //     this.textPassword = new System.Windows.Forms.TextBox();
        //     this.comboLogin = new System.Windows.Forms.ComboBox();
        //     this.labelPasswd = new System.Windows.Forms.Label();
        //     this.SuspendLayout();
        //     this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        //     this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        //     // 
        //     // labelAlias
        //     // 
        //     resources.ApplyResources(this.labelAlias, "labelAlias");
        //     this.labelAlias.Margin = new System.Windows.Forms.Padding(0);
        //     this.labelAlias.Name = "labelAlias";
        //     // 
        //     // comboAliases
        //     // 
        //     resources.ApplyResources(this.comboAliases, "comboAliases");
        //     this.comboAliases.Name = "comboAliases";
        //     // 
        //     // separatorSqlConnection
        //     // 
        //     resources.ApplyResources(this.separatorSqlConnection, "separatorSqlConnection");
        //     this.separatorSqlConnection.Margin = new System.Windows.Forms.Padding(0);
        //     this.separatorSqlConnection.Name = "separatorSqlConnection";
        //     // 
        //     // radioWinAuth
        //     // 
        //     resources.ApplyResources(this.radioWinAuth, "radioWinAuth");
        //     this.radioWinAuth.Name = "radioWinAuth";
        //     this.radioWinAuth.CheckedChanged += new System.EventHandler(this.radioWinAuth_CheckedChanged);
        //     // 
        //     // radioSQLAuth
        //     // 
        //     resources.ApplyResources(this.radioSQLAuth, "radioSQLAuth");
        //     this.radioSQLAuth.Name = "radioSQLAuth";
        //     this.radioSQLAuth.CheckedChanged += new System.EventHandler(this.radioSQLAuth_CheckedChanged);
        //     // 
        //     // labelLogin
        //     // 
        //     resources.ApplyResources(this.labelLogin, "labelLogin");
        //     this.labelLogin.Name = "labelLogin";
        //     // 
        //     // textPassword
        //     // 
        //     resources.ApplyResources(this.textPassword, "textPassword");
        //     this.textPassword.Name = "textPassword";
        //     this.textPassword.TextChanged += new System.EventHandler(this.textPassword_TextChanged);
        //     // 
        //     // comboLogin
        //     // 
        //     resources.ApplyResources(this.comboLogin, "comboLogin");
        //     this.comboLogin.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        //     this.comboLogin.FormattingEnabled = true;
        //     this.comboLogin.Name = "comboLogin";
        //     // 
        //     // labelPasswd
        //     // 
        //     resources.ApplyResources(this.labelPasswd, "labelPasswd");
        //     this.labelPasswd.Name = "labelPasswd";
        //     // 
        //     // SqlServerAgentPropertiesConnection
        //     // 
        //     this.Controls.Add(this.labelPasswd);
        //     this.Controls.Add(this.comboLogin);
        //     this.Controls.Add(this.textPassword);
        //     this.Controls.Add(this.labelLogin);
        //     this.Controls.Add(this.radioSQLAuth);
        //     this.Controls.Add(this.radioWinAuth);
        //     this.Controls.Add(this.separatorSqlConnection);
        //     this.Controls.Add(this.comboAliases);
        //     this.Controls.Add(this.labelAlias);
        //     this.Name = "SqlServerAgentPropertiesConnection";
        //     resources.ApplyResources(this, "$this");
        //     this.ResumeLayout(false);
        //     this.PerformLayout();

        // }
        #endregion

        #region UI controls event handlers
        // private void radioWinAuth_CheckedChanged(object sender, System.EventArgs e)
        // {
        //     bool IsChecked = this.radioWinAuth.Checked;
        //     this.radioSQLAuth.Checked = !IsChecked;
        //     this.comboLogin.Enabled = !IsChecked;
        //     this.textPassword.Enabled = !IsChecked;
        // }

        // private void radioSQLAuth_CheckedChanged(object sender, System.EventArgs e)
        // {
        //     bool IsChecked = this.radioSQLAuth.Checked;
        //     this.radioWinAuth.Checked = !IsChecked;
        //     this.comboLogin.Enabled = IsChecked;
        //     this.textPassword.Enabled = IsChecked;
        // }        

        // private void textPassword_TextChanged(System.Object sender, System.EventArgs e)
        // {
        //     SqlPasswordChanged = true;
        // }
        #endregion
    }
}
