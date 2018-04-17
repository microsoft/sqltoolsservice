using Microsoft.SqlServer.Management.Sdk.Sfc;
#region using
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Globalization;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlTools.ServiceLayer.Admin;

#endregion

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
	/// <summary>
	/// Agent alert histroy page
	/// </summary>
	internal class AgentAlertHistory : AgentControlBase
	{
		#region Members

		

		#endregion

		#region Constructors

		private AgentAlertHistory()
		{			
		}

		public AgentAlertHistory(CDataContainer dataContainer, string agentAlertName) : this()
		{
			if (dataContainer == null)
				throw new ArgumentNullException("dataContainer");
			if (agentAlertName == null)
				throw new ArgumentNullException("agentAlertName");

			DataContainer		= dataContainer;
            // this.AllUIEnabled			= true;
            // this.agentAlertName	= agentAlertName;

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			// Read data from server
			InitializeControls();
		}

		#endregion

		#region Public methods

		/// <summary>
		/// Updates alert fields with data from this page
		/// </summary>
		/// <param name="alert"></param>
		public void UpdateAlert(Alert alert)
		{
			if (alert == null)
				throw new ArgumentNullException("alert");

			// if (this.agentAlertName != null)
			// 	this.agentAlertName = alert.Name;

			// if (this.resetCount.Checked)
			// 	alert.ResetOccurrenceCount();
		}

		#endregion

		#region Overrides

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		// protected override void Dispose( bool disposing )
		// {
		// 	if( disposing )
		// 	{
		// 		if(components != null)
		// 		{
		// 			components.Dispose();
		// 		}
		// 	}
		// 	base.Dispose( disposing );
		// }

		#endregion

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
//          System.ComponentModel.ComponentResourceManager resources=new System.ComponentModel.ComponentResourceManager(typeof(AgentAlertHistory));
//          this.dateLastOccuredLabel=new System.Windows.Forms.Label();
//          this.lastOccurred=new System.Windows.Forms.TextBox();
//          this.lastResponded=new System.Windows.Forms.TextBox();
//          this.dateLastRespondedToLabel=new System.Windows.Forms.Label();
//          this.occurenceCountLabel=new System.Windows.Forms.Label();
//          this.occurrenceCount=new System.Windows.Forms.TextBox();
//          this.resetCount=new System.Windows.Forms.CheckBox();
//          this.SuspendLayout();
//          this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
//          this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
// // 
// // dateLastOccuredLabel
// // 
//          resources.ApplyResources(this.dateLastOccuredLabel,"dateLastOccuredLabel");
//          this.dateLastOccuredLabel.Name="dateLastOccuredLabel";
// // 
// // lastOccurred
// // 
//          resources.ApplyResources(this.lastOccurred,"lastOccurred");
//          this.lastOccurred.BorderStyle=System.Windows.Forms.BorderStyle.None;
//          this.lastOccurred.Name="lastOccurred";
//          this.lastOccurred.ReadOnly=true;
// // 
// // lastResponded
// // 
//          resources.ApplyResources(this.lastResponded,"lastResponded");
//          this.lastResponded.BorderStyle=System.Windows.Forms.BorderStyle.None;
//          this.lastResponded.Name="lastResponded";
//          this.lastResponded.ReadOnly=true;
// // 
// // dateLastRespondedToLabel
// // 
//          resources.ApplyResources(this.dateLastRespondedToLabel,"dateLastRespondedToLabel");
//          this.dateLastRespondedToLabel.Name="dateLastRespondedToLabel";
//          this.dateLastRespondedToLabel.Click+=new System.EventHandler(this.dateLastRespondedToLabel_Click);
// // 
// // occurenceCountLabel
// // 
//          resources.ApplyResources(this.occurenceCountLabel,"occurenceCountLabel");
//          this.occurenceCountLabel.Name="occurenceCountLabel";
//          this.occurenceCountLabel.Click+=new System.EventHandler(this.occurenceCountLabel_Click);
// // 
// // occurrenceCount
// // 
//          resources.ApplyResources(this.occurrenceCount,"occurrenceCount");
//          this.occurrenceCount.BorderStyle=System.Windows.Forms.BorderStyle.None;
//          this.occurrenceCount.Name="occurrenceCount";
//          this.occurrenceCount.ReadOnly=true;
// // 
// // resetCount
// // 
//          resources.ApplyResources(this.resetCount,"resetCount");
//          this.resetCount.Name="resetCount";
// // 
// // AgentAlertHistory
// // 
//          this.Controls.Add(this.resetCount);
//          this.Controls.Add(this.occurrenceCount);
//          this.Controls.Add(this.occurenceCountLabel);
//          this.Controls.Add(this.lastResponded);
//          this.Controls.Add(this.dateLastRespondedToLabel);
//          this.Controls.Add(this.lastOccurred);
//          this.Controls.Add(this.dateLastOccuredLabel);
//          this.Name="AgentAlertHistory";
//          resources.ApplyResources(this,"$this");
//          this.ResumeLayout(false);
//          this.PerformLayout();

		}
		#endregion

		#region IPanelForm

		// void IPanelForm.OnSelection(TreeNode node)
		// {
		// }

		// public override void OnReset(object sender)
		// {
		// 	InitializeControls();
        //     base.OnReset(sender);
		// }


		// void IPanelForm.OnPanelLoseSelection(TreeNode node)
		// {
		// }

		// void IPanelForm.OnInitialization()
		// {
        //     if (this.readOnly)
        //     {
        //         this.resetCount.Enabled = false;
        //     }
		// }

		// UserControl IPanelForm.Panel
		// {
		// 	get
		// 	{                
		// 		return this;
		// 	}
		// }

		#endregion

		#region Private helpers

		/// <summary>
		/// Initializes controls with data from server
		/// </summary>
		void InitializeControls()
		{
			// Alert agentAlert = this.DataContainer.Server.JobServer.Alerts[this.agentAlertName];

            // this.readOnly = !this.DataContainer.Server.ConnectionContext.IsInFixedServerRole(FixedServerRoles.SysAdmin);

			// if (agentAlert.LastOccurrenceDate == DateTime.MinValue)
			// 	this.lastOccurred.Text	= AgentAlertHistorySR.NeverOccurred;
			// else
			// 	this.lastOccurred.Text	= agentAlert.LastOccurrenceDate.ToString("F", CultureInfo.CurrentCulture);

			// if (agentAlert.LastResponseDate == DateTime.MinValue)
			// 	this.lastResponded.Text	= AgentAlertHistorySR.NeverResponded;
			// else
			// 	this.lastResponded.Text	= agentAlert.LastResponseDate.ToString("F", CultureInfo.CurrentCulture);
			// this.occurrenceCount.Text	= agentAlert.OccurrenceCount.ToString(CultureInfo.CurrentCulture);
			// this.resetCount.Checked		= false;
		}

		#endregion    

      private void dateLastRespondedToLabel_Click(object sender, System.EventArgs e)
      {
      
      }

      private void occurenceCountLabel_Click(object sender, System.EventArgs e)
      {
      
      }
	}
}
