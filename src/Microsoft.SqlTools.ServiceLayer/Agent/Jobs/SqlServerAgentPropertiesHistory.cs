//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Sdk.Sfc;
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Xml;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Resources;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlTools.ServiceLayer.Management;
using Microsoft.SqlTools.ServiceLayer.Admin;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Summary description for SqlServerAgentPropertiesHistory.
    /// </summary>	
    internal class SqlServerAgentPropertiesHistory : ManagementActionBase
    {
        #region Consts

        private const string constMinRows = "2";

        #endregion

        #region UI controls members

        /// <summary>
        /// Required designer variable.
        /// </summary>
        // private System.ComponentModel.Container components = null;

        // private System.Windows.Forms.Label labelHistoryLogSize;
        // private System.Windows.Forms.Label textHistoryLogSize;
        // private System.Windows.Forms.CheckBox checkLimitHistorySize;
        // private System.Windows.Forms.Label labelMaxHistoryLogRows;
        // private System.Windows.Forms.Label labelMaxHistoryRowsPerJob;
        // private System.Windows.Forms.CheckBox checkRemoveHistory;
        // private System.Windows.Forms.Label labelOlderThan;
        // private System.Windows.Forms.NumericUpDown numTimeUnits;
        // private System.Windows.Forms.ComboBox comboTimeUnits;
        // private System.Windows.Forms.NumericUpDown textMaxHistoryRows;
        // private System.Windows.Forms.NumericUpDown textMaxHistoryRowsPerJob;

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

        // bool ISupportValidation.Validate()
        // {
        //     if (false == VerifyUI())
        //     {
        //         return false;
        //     }
        //     return base.Validate();
        // }

        #endregion

        #region ctors

        public SqlServerAgentPropertiesHistory()
        {
        }

        public SqlServerAgentPropertiesHistory(CDataContainer dataContainer)
        {         
            DataContainer = dataContainer;
        }

        #endregion

        #region Implementation

        // private void ApplyChanges()
        // {
        //     this.ExecutionMode = ExecutionMode.Success;

        //     JobServer agent = DataContainer.Server.JobServer;

        //     bool LimitHistory = this.checkLimitHistorySize.Checked;
        //     bool DeleteHistory = this.checkRemoveHistory.Checked;
        //     bool AlterValues = false;
        //     int MaxLogRows = -1;
        //     int MaxRowsJob = -1;

        //     try
        //     {
        //         if (true == LimitHistory)
        //         {
        //             MaxLogRows = (int) this.textMaxHistoryRows.Value;
        //             MaxRowsJob = (int) this.textMaxHistoryRowsPerJob.Value;
        //         }
        //         if (MaxLogRows != agent.MaximumHistoryRows)
        //         {
        //             agent.MaximumHistoryRows = MaxLogRows;
        //             AlterValues = true;
        //         }
        //         if (MaxRowsJob != agent.MaximumJobHistoryRows)
        //         {
        //             agent.MaximumJobHistoryRows = MaxRowsJob;
        //             AlterValues = true;
        //         }
        //         if (true == DeleteHistory)
        //         {
        //             int timeunits = (int) this.numTimeUnits.Value;
        //             JobHistoryFilter jobHistoryFilter = new JobHistoryFilter();
        //             jobHistoryFilter.EndRunDate = CUtils.GetOldestDate(timeunits,
        //                 (TimeUnitType) (this.comboTimeUnits.SelectedIndex));
        //             agent.PurgeJobHistory(jobHistoryFilter);
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
        //         /// select weeks 
        //         this.comboTimeUnits.SelectedIndex = 1;

        //         this.numTimeUnits.Enabled = false;
        //         this.comboTimeUnits.Enabled = false;

        //         this.textHistoryLogSize.Text = "";

        //         JobServer agent = DataContainer.Server.JobServer;

        //         bool IsHistoryLimited = (agent.MaximumHistoryRows > 0);

        //         this.checkLimitHistorySize.Checked = IsHistoryLimited;
        //         this.textMaxHistoryRowsPerJob.Enabled = IsHistoryLimited;
        //         this.textMaxHistoryRows.Enabled = IsHistoryLimited;

        //         if (true == IsHistoryLimited)
        //         {
        //             this.textMaxHistoryRows.Value = agent.MaximumHistoryRows;
        //             this.textMaxHistoryRowsPerJob.Value = agent.MaximumJobHistoryRows;
        //         }

        //         this.textHistoryLogSize.Text = "";
        //     }
        //     catch (SmoException smoex)
        //     {
        //         ShowMessage(smoex,
        //             ExceptionMessageBoxButtons.OK,
        //             ExceptionMessageBoxSymbol.Error);
        //     }
        // }


        // private bool VerifyUI()
        // {
        //     bool allOK = true;
        //     allOK = CUtils.ValidateNumeric(this.textMaxHistoryRows,
        //         SRError.InvalidNumericalValue(SRError.ControlMaximumJobHistoryLogSize, this.textMaxHistoryRows.Minimum,
        //             this.textMaxHistoryRows.Maximum), true);
        //     if (false == allOK)
        //     {
        //         return allOK;
        //     }
        //     allOK = CUtils.ValidateNumeric(this.textMaxHistoryRowsPerJob,
        //         SRError.InvalidNumericalValue(SRError.ControlMaximumJobHistoryRowsPerJob,
        //             this.textMaxHistoryRowsPerJob.Minimum, this.textMaxHistoryRowsPerJob.Maximum), true);
        //     if (false == allOK)
        //     {
        //         return allOK;
        //     }
        //     allOK = CUtils.ValidateNumeric(this.numTimeUnits,
        //         SRError.InvalidNumericalValue(SRError.ControlRemoveAgentHistoryOlderThan, this.numTimeUnits.Minimum,
        //             this.numTimeUnits.Maximum), true);
        //     if (false == allOK)
        //     {
        //         return allOK;
        //     }
        //     return allOK;
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
        //     System.ComponentModel.ComponentResourceManager resources =
        //         new System.ComponentModel.ComponentResourceManager(typeof (SqlServerAgentPropertiesHistory));
        //     this.labelHistoryLogSize = new System.Windows.Forms.Label();
        //     this.checkLimitHistorySize = new System.Windows.Forms.CheckBox();
        //     this.textHistoryLogSize = new System.Windows.Forms.Label();
        //     this.labelMaxHistoryLogRows = new System.Windows.Forms.Label();
        //     this.labelMaxHistoryRowsPerJob = new System.Windows.Forms.Label();
        //     this.checkRemoveHistory = new System.Windows.Forms.CheckBox();
        //     this.labelOlderThan = new System.Windows.Forms.Label();
        //     this.numTimeUnits = new System.Windows.Forms.NumericUpDown();
        //     this.comboTimeUnits = new System.Windows.Forms.ComboBox();
        //     this.textMaxHistoryRows = new System.Windows.Forms.NumericUpDown();
        //     this.textMaxHistoryRowsPerJob = new System.Windows.Forms.NumericUpDown();
        //     ((System.ComponentModel.ISupportInitialize) (this.numTimeUnits)).BeginInit();
        //     ((System.ComponentModel.ISupportInitialize) (this.textMaxHistoryRows)).BeginInit();
        //     ((System.ComponentModel.ISupportInitialize) (this.textMaxHistoryRowsPerJob)).BeginInit();
        //     this.SuspendLayout();
        //     this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        //     this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        //     // 
        //     // labelHistoryLogSize
        //     // 
        //     resources.ApplyResources(this.labelHistoryLogSize, "labelHistoryLogSize");
        //     this.labelHistoryLogSize.Name = "labelHistoryLogSize";
        //     // 
        //     // checkLimitHistorySize
        //     // 
        //     resources.ApplyResources(this.checkLimitHistorySize, "checkLimitHistorySize");
        //     this.checkLimitHistorySize.Name = "checkLimitHistorySize";
        //     this.checkLimitHistorySize.CheckedChanged +=
        //         new System.EventHandler(this.checkLimitHistorySize_CheckedChanged);
        //     // 
        //     // textHistoryLogSize
        //     // 
        //     resources.ApplyResources(this.textHistoryLogSize, "textHistoryLogSize");
        //     this.textHistoryLogSize.Name = "textHistoryLogSize";
        //     // 
        //     // labelMaxHistoryLogRows
        //     // 
        //     resources.ApplyResources(this.labelMaxHistoryLogRows, "labelMaxHistoryLogRows");
        //     this.labelMaxHistoryLogRows.Name = "labelMaxHistoryLogRows";
        //     // 
        //     // labelMaxHistoryRowsPerJob
        //     // 
        //     resources.ApplyResources(this.labelMaxHistoryRowsPerJob, "labelMaxHistoryRowsPerJob");
        //     this.labelMaxHistoryRowsPerJob.Name = "labelMaxHistoryRowsPerJob";
        //     // 
        //     // checkRemoveHistory
        //     // 
        //     resources.ApplyResources(this.checkRemoveHistory, "checkRemoveHistory");
        //     this.checkRemoveHistory.Name = "checkRemoveHistory";
        //     this.checkRemoveHistory.CheckedChanged += new System.EventHandler(this.checkRemoveHistory_CheckedChanged);
        //     // 
        //     // labelOlderThan
        //     // 
        //     resources.ApplyResources(this.labelOlderThan, "labelOlderThan");
        //     this.labelOlderThan.Name = "labelOlderThan";
        //     // 
        //     // numTimeUnits
        //     // 
        //     resources.ApplyResources(this.numTimeUnits, "numTimeUnits");
        //     this.numTimeUnits.Maximum = new decimal(new int[]
        //     {
        //         99,
        //         0,
        //         0,
        //         0
        //     });
        //     this.numTimeUnits.Minimum = new decimal(new int[]
        //     {
        //         1,
        //         0,
        //         0,
        //         0
        //     });
        //     this.numTimeUnits.Name = "numTimeUnits";
        //     this.numTimeUnits.Value = new decimal(new int[]
        //     {
        //         4,
        //         0,
        //         0,
        //         0
        //     });
        //     // 
        //     // comboTimeUnits
        //     // 
        //     resources.ApplyResources(this.comboTimeUnits, "comboTimeUnits");
        //     this.comboTimeUnits.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        //     this.comboTimeUnits.FormattingEnabled = true;
        //     this.comboTimeUnits.Items.AddRange(new object[]
        //     {
        //         resources.GetString("comboTimeUnits.Items"),
        //         resources.GetString("comboTimeUnits.Items1"),
        //         resources.GetString("comboTimeUnits.Items2")
        //     });
        //     this.comboTimeUnits.Name = "comboTimeUnits";
        //     // 
        //     // textMaxHistoryRows
        //     // 
        //     resources.ApplyResources(this.textMaxHistoryRows, "textMaxHistoryRows");
        //     this.textMaxHistoryRows.Maximum = new decimal(new int[]
        //     {
        //         999999,
        //         0,
        //         0,
        //         0
        //     });
        //     this.textMaxHistoryRows.Minimum = new decimal(new int[]
        //     {
        //         2,
        //         0,
        //         0,
        //         0
        //     });
        //     this.textMaxHistoryRows.Name = "textMaxHistoryRows";
        //     this.textMaxHistoryRows.Value = new decimal(new int[]
        //     {
        //         1000,
        //         0,
        //         0,
        //         0
        //     });
        //     // 
        //     // textMaxHistoryRowsPerJob
        //     // 
        //     resources.ApplyResources(this.textMaxHistoryRowsPerJob, "textMaxHistoryRowsPerJob");
        //     this.textMaxHistoryRowsPerJob.Maximum = new decimal(new int[]
        //     {
        //         999999,
        //         0,
        //         0,
        //         0
        //     });
        //     this.textMaxHistoryRowsPerJob.Minimum = new decimal(new int[]
        //     {
        //         2,
        //         0,
        //         0,
        //         0
        //     });
        //     this.textMaxHistoryRowsPerJob.Name = "textMaxHistoryRowsPerJob";
        //     this.textMaxHistoryRowsPerJob.Value = new decimal(new int[]
        //     {
        //         100,
        //         0,
        //         0,
        //         0
        //     });
        //     // 
        //     // SqlServerAgentPropertiesHistory
        //     // 
        //     this.Controls.Add(this.textMaxHistoryRowsPerJob);
        //     this.Controls.Add(this.textMaxHistoryRows);
        //     this.Controls.Add(this.comboTimeUnits);
        //     this.Controls.Add(this.numTimeUnits);
        //     this.Controls.Add(this.labelOlderThan);
        //     this.Controls.Add(this.checkRemoveHistory);
        //     this.Controls.Add(this.labelMaxHistoryRowsPerJob);
        //     this.Controls.Add(this.labelMaxHistoryLogRows);
        //     this.Controls.Add(this.textHistoryLogSize);
        //     this.Controls.Add(this.checkLimitHistorySize);
        //     this.Controls.Add(this.labelHistoryLogSize);
        //     this.Name = "SqlServerAgentPropertiesHistory";
        //     resources.ApplyResources(this, "$this");
        //     ((System.ComponentModel.ISupportInitialize) (this.numTimeUnits)).EndInit();
        //     ((System.ComponentModel.ISupportInitialize) (this.textMaxHistoryRows)).EndInit();
        //     ((System.ComponentModel.ISupportInitialize) (this.textMaxHistoryRowsPerJob)).EndInit();
        //     this.ResumeLayout(false);

        // }

        #endregion

        #region UI controls event handlers

        // private void checkRemoveHistory_CheckedChanged(object sender, System.EventArgs e)
        // {
        //     bool IsChecked = this.checkRemoveHistory.Checked;

        //     this.numTimeUnits.Enabled = IsChecked;
        //     this.comboTimeUnits.Enabled = IsChecked;
        // }

        // private void checkLimitHistorySize_CheckedChanged(object sender, System.EventArgs e)
        // {
        //     bool IsChecked = this.checkLimitHistorySize.Checked;
        //     this.textMaxHistoryRowsPerJob.Enabled = IsChecked;
        //     this.textMaxHistoryRows.Enabled = IsChecked;
        // }

        #endregion
    }

}
