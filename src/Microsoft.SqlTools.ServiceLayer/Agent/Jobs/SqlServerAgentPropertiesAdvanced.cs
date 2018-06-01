using Microsoft.SqlServer.Management.Sdk.Sfc;
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Xml;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo.Agent;
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
    /// Summary description for SqlServerAgentPropertiesAdvanced.
    /// </summary>
    //public class SqlServerAgentPropertiesAdvanced : System.Windows.Forms.Form
    internal class SqlServerAgentPropertiesAdvanced : ManagementActionBase
    {
        #region UI controls members
        /// <summary>
        /// Required designer variable.
        /// </summary>
        // private System.ComponentModel.Container components = null;
        // private Microsoft.SqlServer.Management.Controls.Separator separatorEventForwarding;
        // private System.Windows.Forms.CheckBox checkForwardEVents;
        // private System.Windows.Forms.Label labelServerName;
        // private System.Windows.Forms.Label labelEvents;
        // private System.Windows.Forms.RadioButton radioEventsUnhandled;
        // private System.Windows.Forms.RadioButton radioEventsAll;
        // private System.Windows.Forms.Label labelEventSeverity;
        // private System.Windows.Forms.ComboBox comboEventSeverity;
        // private Microsoft.SqlServer.Management.Controls.Separator separatorIdleCPU;
        // private System.Windows.Forms.CheckBox checkCPUIdleCondition;
        // private System.Windows.Forms.Label labelCPUAverageUsage;
        // private System.Windows.Forms.NumericUpDown numCPUUsage;
        // private System.Windows.Forms.Label labelCPUPercent;
        // private System.Windows.Forms.Label labelCPUTImeBelow;
        // private System.Windows.Forms.Label labelCPUSeconds;
        // private System.Windows.Forms.NumericUpDown textCPUTimeBelow;
        // private System.Windows.Forms.TextBox textBoxForwardingServer;
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

        #region ctors
        public SqlServerAgentPropertiesAdvanced()
        {
        }

        public SqlServerAgentPropertiesAdvanced(CDataContainer dataContainer)
        {
            DataContainer       = dataContainer;                        
            //this.HelpF1Keyword  = AssemblyVersionInfo.VersionHelpKeywordPrefix + @".ag.agent.advanced.f1";
        }

        #endregion

        #region Utils
        
        // private bool VerifyUI()
        // {
        //     bool allOK = true;
        //     allOK = CUtils.ValidateNumeric(this.numCPUUsage,SRError.InvalidNumericalValue(SRError.ControlCpuUsage,this.numCPUUsage.Minimum,this.numCPUUsage.Maximum),true);
        //     if (false == allOK)
        //     {
        //         return allOK;
        //     }
        //     allOK = CUtils.ValidateNumeric(this.textCPUTimeBelow,SRError.InvalidNumericalValue(SRError.ControlRemainsBelowLevelFor,this.textCPUTimeBelow.Minimum,this.textCPUTimeBelow.Maximum),true);                                      
        //     if (false == allOK)
        //     {
        //         return allOK;
        //     }
        //     return allOK;
        // }
        
        // private void SetControlsForForwarding(bool enabled)
        // {
        //     this.textBoxForwardingServer.Enabled           = enabled;
        //     this.radioEventsAll.Enabled         = enabled;
        //     this.radioEventsUnhandled.Enabled   = enabled;
        //     this.comboEventSeverity.Enabled     = enabled; 
        // }
        
        // private void SetControlsForIdleCondition(bool enabled)
        // {
        //     this.numCPUUsage.Enabled        = enabled;
        //     this.textCPUTimeBelow.Enabled   = enabled;
        // }

        // private void PopulateEventSeverityCombo()
        // {
        //     this.comboEventSeverity.Items.Clear();            
        //     this.comboEventSeverity.Items.Add(SqlServerAgentSR.EventSeverity_001);
        //     this.comboEventSeverity.Items.Add(SqlServerAgentSR.EventSeverity_002);
        //     this.comboEventSeverity.Items.Add(SqlServerAgentSR.EventSeverity_003);
        //     this.comboEventSeverity.Items.Add(SqlServerAgentSR.EventSeverity_004);
        //     this.comboEventSeverity.Items.Add(SqlServerAgentSR.EventSeverity_005);
        //     this.comboEventSeverity.Items.Add(SqlServerAgentSR.EventSeverity_006);
        //     this.comboEventSeverity.Items.Add(SqlServerAgentSR.EventSeverity_007);
        //     this.comboEventSeverity.Items.Add(SqlServerAgentSR.EventSeverity_008);
        //     this.comboEventSeverity.Items.Add(SqlServerAgentSR.EventSeverity_009);
        //     this.comboEventSeverity.Items.Add(SqlServerAgentSR.EventSeverity_010);
        //     this.comboEventSeverity.Items.Add(SqlServerAgentSR.EventSeverity_011);
        //     this.comboEventSeverity.Items.Add(SqlServerAgentSR.EventSeverity_012);
        //     this.comboEventSeverity.Items.Add(SqlServerAgentSR.EventSeverity_013);
        //     this.comboEventSeverity.Items.Add(SqlServerAgentSR.EventSeverity_014);
        //     this.comboEventSeverity.Items.Add(SqlServerAgentSR.EventSeverity_015);
        //     this.comboEventSeverity.Items.Add(SqlServerAgentSR.EventSeverity_016);
        //     this.comboEventSeverity.Items.Add(SqlServerAgentSR.EventSeverity_017);
        //     this.comboEventSeverity.Items.Add(SqlServerAgentSR.EventSeverity_018);
        //     this.comboEventSeverity.Items.Add(SqlServerAgentSR.EventSeverity_019);
        //     this.comboEventSeverity.Items.Add(SqlServerAgentSR.EventSeverity_020);
        //     this.comboEventSeverity.Items.Add(SqlServerAgentSR.EventSeverity_021);
        //     this.comboEventSeverity.Items.Add(SqlServerAgentSR.EventSeverity_022);
        //     this.comboEventSeverity.Items.Add(SqlServerAgentSR.EventSeverity_023);
        //     this.comboEventSeverity.Items.Add(SqlServerAgentSR.EventSeverity_024);
        //     this.comboEventSeverity.Items.Add(SqlServerAgentSR.EventSeverity_025);
        //     this.comboEventSeverity.SelectedIndex   = 0;
        // }

        #endregion
        
        #region Implementation
        // private void ApplyChanges()
        // {
        //     this.ExecutionMode  = ExecutionMode.Success;            
            
        //     JobServer   agent   = DataContainer.Server.JobServer;
            
        //     bool    AlterValuesAgent = false;            
        //     bool    AlterValuesAlert = false;            

        //     string      OrigialAlertForwardingServer    = agent.AlertSystem.ForwardingServer;
        //     string      CurrentAlertForwardingServer    = "";            
        //     bool        CurrentForwardedAlways          = this.radioEventsAll.Checked;

        //     if(this.checkForwardEVents.Checked  == true)
        //     {
        //         CurrentAlertForwardingServer = this.textBoxForwardingServer.Text;
        //     }

        //     try
        //     {
        //         if(CurrentAlertForwardingServer != OrigialAlertForwardingServer)
        //         {
        //             AlterValuesAlert                    = true;
        //             agent.AlertSystem.ForwardingServer  = CurrentAlertForwardingServer;                    
        //         }
                
        //         if(CurrentAlertForwardingServer.Length > 0)
        //         {
                    
        //             if(agent.AlertSystem.IsForwardedAlways != CurrentForwardedAlways)
        //             {
        //                 AlterValuesAlert                        = true;
        //                 agent.AlertSystem.IsForwardedAlways     = CurrentForwardedAlways;
        //             }
                    
        //             int SelectedSevIndex                           = this.comboEventSeverity.SelectedIndex;
                    
        //             if(agent.AlertSystem.ForwardingSeverity != SelectedSevIndex + 1)
        //             {
        //                 AlterValuesAlert                        = true;
        //                 agent.AlertSystem.ForwardingSeverity    = SelectedSevIndex +1;
        //             }
        //         }
                
        //         if(agent.IsCpuPollingEnabled != this.checkCPUIdleCondition.Checked)
        //         {
        //             AlterValuesAgent            = true;
        //             agent.IsCpuPollingEnabled   = this.checkCPUIdleCondition.Checked;
        //         }
        //         if(true == this.checkCPUIdleCondition.Checked)
        //         {
        //             if(this.numCPUUsage.Value != agent.IdleCpuPercentage)
        //             {
        //                 AlterValuesAgent            = true;
        //                 agent.IdleCpuPercentage     = (int)this.numCPUUsage.Value;
        //             }
        //             if(this.textCPUTimeBelow.Value != agent.IdleCpuDuration)
        //             {
        //                 AlterValuesAgent            = true;
        //                 agent.IdleCpuDuration       = (int)this.textCPUTimeBelow.Value;
        //             }

        //         }
                
        //         if(true == AlterValuesAlert)
        //         {
        //             agent.AlertSystem.Alter();                    
        //         }
        //         if(true == AlterValuesAgent)
        //         {
        //             agent.Alter();
        //         }
        //     }
        //     catch(SmoException smoex)
        //     {
        //         DisplayExceptionMessage(smoex);
        //         this.ExecutionMode  = ExecutionMode.Failure;
        //     }
        //     catch (Exception ex)
        //     {
        //         DisplayExceptionMessage(ex);
        //         this.ExecutionMode = ExecutionMode.Failure;
        //     }
        
        // }
        
        // private void InitProperties()
        // {
        //     PopulateEventSeverityCombo();

        //     DataContainer.Server.Refresh();
        //     DataContainer.Server.JobServer.Refresh();
        //     JobServer   agent                   = DataContainer.Server.JobServer;
        //     string      AlertForwardingServer   = agent.AlertSystem.ForwardingServer;
        //     bool managedInstance = DataContainer.Server.DatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance;

        //     if(AlertForwardingServer.Length != 0 && !managedInstance)
        //     {                
        //         this.checkForwardEVents.Checked = true;
        //         SetControlsForForwarding(true);
        //         this.textBoxForwardingServer.Text = AlertForwardingServer;
        //     }
        //     else
        //     {
        //         this.checkForwardEVents.Checked = false;
        //         SetControlsForForwarding(false);

        //         // Managed Instance does not allow forwarding events
        //         // to a different server
        //         //
        //         this.checkForwardEVents.Enabled = !managedInstance;

        //     }
        //     this.radioEventsAll.Checked         = agent.AlertSystem.IsForwardedAlways;
            
        //     /// Assume that the items in the combo are int ordered from 1- 25
        //     /// 
 
        //     try
        //     {
        //         int RetrievedSeverityValue = agent.AlertSystem.ForwardingSeverity - 1;
        //         if (RetrievedSeverityValue > this.comboEventSeverity.Items.Count - 1)
        //         {
        //             RetrievedSeverityValue = 0;
        //         }

        //         this.comboEventSeverity.SelectedIndex = RetrievedSeverityValue;
        //     }
        //     catch (SmoException )
        //     {
        //         //DisplayExceptionMessage(se);
        //         this.comboEventSeverity.Enabled = false;
        //         this.comboEventSeverity.SelectedIndex   = 0;
        //     }

        //     bool enable =   this.checkCPUIdleCondition.Checked      = agent.IsCpuPollingEnabled;
        //     SetControlsForIdleCondition(enable);
        //     this.numCPUUsage.Tag        = this.numCPUUsage.Value        = agent.IdleCpuPercentage;
        //     this.numCPUUsage.Text       = this.numCPUUsage.Value.ToString(System.Globalization.CultureInfo.CurrentCulture);
        //     this.numCPUUsage.Update();

        //     this.textCPUTimeBelow.Tag   = this.textCPUTimeBelow.Value   = agent.IdleCpuDuration;
        //     this.textCPUTimeBelow.Text  = this.textCPUTimeBelow.Value.ToString(System.Globalization.CultureInfo.CurrentCulture);
        //     this.textCPUTimeBelow.Update();         
            
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


        // /// <summary>
        // /// IPanelForm.OnInitialization
        // /// 
        // /// TODO - in order to reduce IPanelForm container load time
        // /// and to improve performance, IPanelForm-s should be able
        // /// to lazy-initialize themself when IPanelForm.OnInitialization
        // /// is called (a continer like TreePanelForm calls the
        // /// OnInitialization() method before first OnSelection())
        // /// </summary>
        // void IPanelForm.OnInitialization()
        // {            
        //     InitProperties();
        // }


        // public override void OnRunNow (object sender)
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


        // bool ISupportValidation.Validate()
        // {
        //     if (false == VerifyUI())
        //     {
        //         return false;
        //     }
        //     return base.Validate();
        // }

        #endregion

        #region Dispose
        
        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        // protected override void Dispose( bool disposing )
        // {
        //     if( disposing )
        //     {
        //         if(components != null)
        //         {
        //             components.Dispose();
        //         }
        //     }
        //     base.Dispose( disposing );
        // }

        // #endregion

        // #region Windows Form Designer generated code
        // /// <summary>
        // /// Required method for Designer support - do not modify
        // /// the contents of this method with the code editor.
        // /// </summary>
        // private void InitializeComponent()
        // {
        //     System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SqlServerAgentPropertiesAdvanced));
        //     this.separatorEventForwarding = new Microsoft.SqlServer.Management.Controls.Separator();
        //     this.checkForwardEVents = new System.Windows.Forms.CheckBox();
        //     this.labelServerName = new System.Windows.Forms.Label();
        //     this.labelEvents = new System.Windows.Forms.Label();
        //     this.radioEventsUnhandled = new System.Windows.Forms.RadioButton();
        //     this.radioEventsAll = new System.Windows.Forms.RadioButton();
        //     this.labelEventSeverity = new System.Windows.Forms.Label();
        //     this.comboEventSeverity = new System.Windows.Forms.ComboBox();
        //     this.separatorIdleCPU = new Microsoft.SqlServer.Management.Controls.Separator();
        //     this.checkCPUIdleCondition = new System.Windows.Forms.CheckBox();
        //     this.labelCPUAverageUsage = new System.Windows.Forms.Label();
        //     this.numCPUUsage = new System.Windows.Forms.NumericUpDown();
        //     this.labelCPUPercent = new System.Windows.Forms.Label();
        //     this.labelCPUTImeBelow = new System.Windows.Forms.Label();
        //     this.labelCPUSeconds = new System.Windows.Forms.Label();
        //     this.textCPUTimeBelow = new System.Windows.Forms.NumericUpDown();
        //     this.textBoxForwardingServer = new System.Windows.Forms.TextBox();
        //     ((System.ComponentModel.ISupportInitialize)(this.numCPUUsage)).BeginInit();
        //     ((System.ComponentModel.ISupportInitialize)(this.textCPUTimeBelow)).BeginInit();
        //     this.SuspendLayout();
        //     this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        //     this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        //     // 
        //     // separatorEventForwarding
        //     // 
        //     resources.ApplyResources(this.separatorEventForwarding, "separatorEventForwarding");
        //     this.separatorEventForwarding.Name = "separatorEventForwarding";
        //     // 
        //     // checkForwardEVents
        //     // 
        //     resources.ApplyResources(this.checkForwardEVents, "checkForwardEVents");
        //     this.checkForwardEVents.Name = "checkForwardEVents";
        //     this.checkForwardEVents.CheckedChanged += new System.EventHandler(this.checkForwardEVents_CheckedChanged);
        //     // 
        //     // labelServerName
        //     // 
        //     resources.ApplyResources(this.labelServerName, "labelServerName");
        //     this.labelServerName.Name = "labelServerName";
        //     // 
        //     // labelEvents
        //     // 
        //     resources.ApplyResources(this.labelEvents, "labelEvents");
        //     this.labelEvents.Name = "labelEvents";
        //     // 
        //     // radioEventsUnhandled
        //     // 
        //     resources.ApplyResources(this.radioEventsUnhandled, "radioEventsUnhandled");
        //     this.radioEventsUnhandled.Checked = true;
        //     this.radioEventsUnhandled.Name = "radioEventsUnhandled";
        //     this.radioEventsUnhandled.TabStop = true;
        //     // 
        //     // radioEventsAll
        //     // 
        //     resources.ApplyResources(this.radioEventsAll, "radioEventsAll");
        //     this.radioEventsAll.Name = "radioEventsAll";
        //     // 
        //     // labelEventSeverity
        //     // 
        //     resources.ApplyResources(this.labelEventSeverity, "labelEventSeverity");
        //     this.labelEventSeverity.Name = "labelEventSeverity";
        //     // 
        //     // comboEventSeverity
        //     // 
        //     resources.ApplyResources(this.comboEventSeverity, "comboEventSeverity");
        //     this.comboEventSeverity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        //     this.comboEventSeverity.FormattingEnabled = true;
        //     this.comboEventSeverity.Name = "comboEventSeverity";
        //     // 
        //     // separatorIdleCPU
        //     // 
        //     resources.ApplyResources(this.separatorIdleCPU, "separatorIdleCPU");
        //     this.separatorIdleCPU.Name = "separatorIdleCPU";
        //     // 
        //     // checkCPUIdleCondition
        //     // 
        //     resources.ApplyResources(this.checkCPUIdleCondition, "checkCPUIdleCondition");
        //     this.checkCPUIdleCondition.Name = "checkCPUIdleCondition";
        //     this.checkCPUIdleCondition.CheckedChanged += new System.EventHandler(this.checkCPUIdleCondition_CheckedChanged);
        //     // 
        //     // labelCPUAverageUsage
        //     // 
        //     resources.ApplyResources(this.labelCPUAverageUsage, "labelCPUAverageUsage");
        //     this.labelCPUAverageUsage.Name = "labelCPUAverageUsage";
        //     // 
        //     // numCPUUsage
        //     // 
        //     resources.ApplyResources(this.numCPUUsage, "numCPUUsage");
        //     this.numCPUUsage.Minimum = new decimal(new int[] {
        //     10,
        //     0,
        //     0,
        //     0});
        //     this.numCPUUsage.Name = "numCPUUsage";
        //     this.numCPUUsage.Value = new decimal(new int[] {
        //     10,
        //     0,
        //     0,
        //     0});
        //     this.numCPUUsage.Enter += new System.EventHandler(this.numCPUUsage_Enter);
        //     this.numCPUUsage.ValueChanged += new System.EventHandler(this.numCPUUsage_ValueChanged);
        //     this.numCPUUsage.Leave += new System.EventHandler(this.numCPUUsage_Leave);
        //     // 
        //     // labelCPUPercent
        //     // 
        //     resources.ApplyResources(this.labelCPUPercent, "labelCPUPercent");
        //     this.labelCPUPercent.Name = "labelCPUPercent";
        //     // 
        //     // labelCPUTImeBelow
        //     // 
        //     resources.ApplyResources(this.labelCPUTImeBelow, "labelCPUTImeBelow");
        //     this.labelCPUTImeBelow.Name = "labelCPUTImeBelow";
        //     // 
        //     // labelCPUSeconds
        //     // 
        //     resources.ApplyResources(this.labelCPUSeconds, "labelCPUSeconds");
        //     this.labelCPUSeconds.Name = "labelCPUSeconds";
        //     // 
        //     // textCPUTimeBelow
        //     // 
        //     resources.ApplyResources(this.textCPUTimeBelow, "textCPUTimeBelow");
        //     this.textCPUTimeBelow.Maximum = new decimal(new int[] {
        //     86400,
        //     0,
        //     0,
        //     0});
        //     this.textCPUTimeBelow.Minimum = new decimal(new int[] {
        //     20,
        //     0,
        //     0,
        //     0});
        //     this.textCPUTimeBelow.Name = "textCPUTimeBelow";
        //     this.textCPUTimeBelow.Value = new decimal(new int[] {
        //     600,
        //     0,
        //     0,
        //     0});
        //     this.textCPUTimeBelow.Enter += new System.EventHandler(this.textCPUTimeBelow_Enter);
        //     this.textCPUTimeBelow.ValueChanged += new System.EventHandler(this.textCPUTimeBelow_ValueChanged);
        //     this.textCPUTimeBelow.Leave += new System.EventHandler(this.textCPUTimeBelow_Leave);
        //     // 
        //     // textBoxForwardingServer
        //     // 
        //     resources.ApplyResources(this.textBoxForwardingServer, "textBoxForwardingServer");
        //     this.textBoxForwardingServer.Name = "textBoxForwardingServer";
        //     // 
        //     // SqlServerAgentPropertiesAdvanced
        //     // 
        //     this.Controls.Add(this.textBoxForwardingServer);
        //     this.Controls.Add(this.textCPUTimeBelow);
        //     this.Controls.Add(this.labelCPUSeconds);
        //     this.Controls.Add(this.labelCPUTImeBelow);
        //     this.Controls.Add(this.labelCPUPercent);
        //     this.Controls.Add(this.numCPUUsage);
        //     this.Controls.Add(this.labelCPUAverageUsage);
        //     this.Controls.Add(this.checkCPUIdleCondition);
        //     this.Controls.Add(this.separatorIdleCPU);
        //     this.Controls.Add(this.comboEventSeverity);
        //     this.Controls.Add(this.labelEventSeverity);
        //     this.Controls.Add(this.radioEventsAll);
        //     this.Controls.Add(this.radioEventsUnhandled);
        //     this.Controls.Add(this.labelEvents);
        //     this.Controls.Add(this.labelServerName);
        //     this.Controls.Add(this.checkForwardEVents);
        //     this.Controls.Add(this.separatorEventForwarding);
        //     this.Name = "SqlServerAgentPropertiesAdvanced";
        //     resources.ApplyResources(this, "$this");
        //     ((System.ComponentModel.ISupportInitialize)(this.numCPUUsage)).EndInit();
        //     ((System.ComponentModel.ISupportInitialize)(this.textCPUTimeBelow)).EndInit();
        //     this.ResumeLayout(false);
        //     this.PerformLayout();

        // }
        #endregion

        #region UI controls event handlers

        // private void checkForwardEVents_CheckedChanged(object sender, System.EventArgs e)
        // {
        //     bool IsChecked  = this.checkForwardEVents.Checked;
        //     SetControlsForForwarding(IsChecked);                   
        // }

        // private void checkCPUIdleCondition_CheckedChanged(object sender, System.EventArgs e)
        // {
        //     bool IsChecked                      = this.checkCPUIdleCondition.Checked;
        //     SetControlsForIdleCondition(IsChecked);                    
        // }        

        // private void numCPUUsage_Leave(System.Object sender, System.EventArgs e)
        // {           
        //     //bool bOK  = CUtils.ValidateNumeric(this.numCPUUsage,SRError.InvalidNumericalValue(SRError.ControlCpuUsage,this.numCPUUsage.Minimum,this.numCPUUsage.Maximum),true);                                       
        // }

        // private void textCPUTimeBelow_Leave(System.Object sender, System.EventArgs e)
        // {
        //     //bool bOK  = CUtils.ValidateNumeric(this.textCPUTimeBelow,SRError.InvalidNumericalValue(SRError.ControlRemainsBelowLevelFor,this.textCPUTimeBelow.Minimum,this.textCPUTimeBelow.Maximum),true);                                        
        // }

        // private void numCPUUsage_ValueChanged(System.Object sender, System.EventArgs e)
        // {           
        //     STrace.Trace(m_strComponentName,numCPUUsage.Value.ToString(System.Globalization.CultureInfo.CurrentCulture));         
        // }

        // private void textCPUTimeBelow_ValueChanged(System.Object sender, System.EventArgs e)
        // {           
        //     STrace.Trace(m_strComponentName,textCPUTimeBelow.Value.ToString(System.Globalization.CultureInfo.CurrentCulture));            
        // }

        // private void numCPUUsage_Enter(System.Object sender, System.EventArgs e)
        // {
        //     this.numCPUUsage.Tag        = this.numCPUUsage.Value;       
        // }

        // private void textCPUTimeBelow_Enter(System.Object sender, System.EventArgs e)
        // {
        //     this.textCPUTimeBelow.Tag   = this.textCPUTimeBelow.Value;      
        // }

        #endregion

        
    }
}








