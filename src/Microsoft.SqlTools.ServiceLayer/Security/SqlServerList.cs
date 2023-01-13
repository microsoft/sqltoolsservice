using System;
using System.Drawing;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

using Microsoft.SqlServer.Management.Smo;


namespace Microsoft.SqlTools.ServiceLayer.Security
{
    /// <summary>
    /// The class displays the dialog with a list of 
    /// available sql servers on a network in a ListView.
    /// </summary>
#if DEBUG || EXPOSE_MANAGED_INTERNALS
    public
#else
    internal
#endif
    class SqlServerList : System.Windows.Forms.Form
    {
        private List<int> ignoredVersions = null;
        private string selectedServer = null;
        private System.Windows.Forms.Label labelSelectServer;
        private System.Windows.Forms.ListView listviewSqlServers;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.ColumnHeader columnHeaderSqlServer;

        private System.ComponentModel.Container components = null;

        public SqlServerList()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            buttonOK.Enabled = false;
        }

        protected override void Dispose( bool disposing )
        {
            if ( disposing )
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose( disposing );
        }

        /// <summary>
        /// The selected SQL Server name is returned. If the server
        /// was not selected or if the user had selected Cancel button
        /// then the name will be null.
        /// </summary>
        public string SelectedSqlServer
        {
            get
            {
                return selectedServer;
            }
        }

        /// <summary>
        /// Add the versions which has to be ignored from the list
        /// </summary>
        /// <param name="serverVersion"></param>
        public void AddVersionsToIgnore(int majorVersion)
        {
            if (ignoredVersions == null)
            {
                ignoredVersions = new List<int>();
            }
            ignoredVersions.Add(majorVersion);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.MinimumSize = this.Size;
        }

#region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SqlServerList));
            this.labelSelectServer = new System.Windows.Forms.Label();
            this.listviewSqlServers = new System.Windows.Forms.ListView();
            this.columnHeaderSqlServer = new System.Windows.Forms.ColumnHeader();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            // 
            // labelSelectServer
            // 
            resources.ApplyResources(this.labelSelectServer, "labelSelectServer");
            this.labelSelectServer.Name = "labelSelectServer";
            // 
            // listviewSqlServers
            // 
            resources.ApplyResources(this.listviewSqlServers, "listviewSqlServers");
            this.listviewSqlServers.CheckBoxes = true;
            this.listviewSqlServers.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderSqlServer});
            this.listviewSqlServers.FullRowSelect = true;
            this.listviewSqlServers.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.listviewSqlServers.MultiSelect = false;
            this.listviewSqlServers.Name = "listviewSqlServers";
            this.listviewSqlServers.UseCompatibleStateImageBehavior = false;
            this.listviewSqlServers.View = System.Windows.Forms.View.Details;
            this.listviewSqlServers.SelectedIndexChanged += new System.EventHandler(this.listviewSqlServers_SelectedIndexChanged);
            this.listviewSqlServers.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.listviewSqlServers_ItemCheck);
            // 
            // columnHeaderSqlServer
            // 
            resources.ApplyResources(this.columnHeaderSqlServer, "columnHeaderSqlServer");
            // 
            // buttonOK
            // 
            resources.ApplyResources(this.buttonOK, "buttonOK");
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonCancel
            // 
            resources.ApplyResources(this.buttonCancel, "buttonCancel");
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // SqlServerList
            // 
            this.AcceptButton = this.buttonOK;
            resources.ApplyResources(this, "$this");
            this.CancelButton = this.buttonCancel;
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.listviewSqlServers);
            this.Controls.Add(this.labelSelectServer);
            this.MinimizeBox = false;
            this.Name = "SqlServerList";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Load += new System.EventHandler(this.SqlServerList_Load);
            this.ResumeLayout(false);

        }
#endregion

#region Event handlers
        /// <summary>
        /// The SQL Server names are listed in the control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SqlServerList_Load(object sender, System.EventArgs e)
        {
            selectedServer  = null;
            SqlServerEnumerator sqlEnum = new SqlServerEnumerator();

            if (ignoredVersions != null)
            {
                foreach (int serverVersion in ignoredVersions)
                {
                    sqlEnum.AddVersionsToIgnore(serverVersion);
                }
            }

            if (sqlEnum.EnumSQLServers() > 0)
            {
                StringCollection serverNames = sqlEnum.SqlServers;
                foreach(string sqlServer in serverNames)
                {
                    listviewSqlServers.Items.Add(sqlServer);
                }
            }
        }

        /// <summary>
        /// SQL Server is checked by user. This method takes care of single checking in the list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listviewSqlServers_ItemCheck(object sender, System.Windows.Forms.ItemCheckEventArgs e)
        {
            if ( e.NewValue == CheckState.Checked )
            {
                ListView.CheckedListViewItemCollection col = listviewSqlServers.CheckedItems;
                foreach( ListViewItem Item in col )
                {
                    Item.Checked= false;
                }
                listviewSqlServers.Items[e.Index].Selected = true;
            }

            buttonOK.Enabled = e.NewValue == CheckState.Checked;
        }
        /// <summary>
        /// The Ok button is clicked. If SQL Server is selected
        /// then the name is set to the control object.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonOK_Click(object sender, System.EventArgs e)
        {
            selectedServer = (listviewSqlServers.CheckedItems.Count > 0) ? 
                             listviewSqlServers.CheckedItems[0].Text :
                             null;

            this.DialogResult = DialogResult.OK;
        }
        /// <summary>
        /// On cancel button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonCancel_Click(object sender, System.EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void listviewSqlServers_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            if (listviewSqlServers.CheckedItems.Count > 0 &&  listviewSqlServers.SelectedItems.Count > 0 )
            {
                ListView.CheckedListViewItemCollection col = listviewSqlServers.CheckedItems;

                foreach (ListViewItem Item in col)
                {
                    Item.Checked = false;
                }
                listviewSqlServers.SelectedItems[0].Checked = true;
            }
        }
#endregion

    }

    internal class SqlServerEnumerator
    {
        private StringCollection sqlServerNames = new StringCollection();
        private List<int> ignoredVersions = null;

        public SqlServerEnumerator()
        {
        }

        /// <summary>
        /// The names of SQL Servers in the network
        /// </summary>
        public StringCollection SqlServers
        {
            get
            {
                return sqlServerNames;
            }
        }

        /// <summary>
        /// Gets the major version value as integer from the string passed
        /// </summary>
        /// <param name="versionString"></param>
        /// <returns></returns>
        private int GetMajorVersionAsInt(string versionString)
        {
            if (versionString == null)
            {
                return -1;
            }

            int index = versionString.IndexOf('.');

            if (index < 0)
            {
                return -1;
            }

            string majorVersion = versionString.Substring(0,index);
            return Convert.ToInt32(majorVersion);
        }

        /// <summary>
        /// Add the versions which has to be ignored from the list
        /// </summary>
        /// <param name="serverVersion"></param>
        public void AddVersionsToIgnore(int majorVersion)
        {
            if (ignoredVersions == null)
            {
                ignoredVersions = new List<int>();
            }
            ignoredVersions.Add(majorVersion);
        }

        /// <summary>
        /// Has to be invoked to refresh the list of SQL Servers. First time this has to be invoked
        /// before accessing the property SqlServers if the names are required.
        /// </summary>
        /// <returns></returns>
        public int EnumSQLServers()
        {
            sqlServerNames.Clear();
            DataTable dataTable = Smo.SmoApplication.EnumAvailableSqlServers();

            if (dataTable != null)
            {
                string[] sa = new string[dataTable.Rows.Count];
                int entriesRead = 0;

                foreach( DataRow dataRow in dataTable.Rows )
                {
                    if (dataRow["Name"] != DBNull.Value)
                    {
                        string serverName = dataRow["Name"] as string;
                        if (serverName != null && serverName.Length > 0)
                        {
                            if (ignoredVersions != null && dataRow["Version"] != DBNull.Value)
                            {
                                string versionString = (string) dataRow["Version"];
                                int majorVersion = GetMajorVersionAsInt(versionString);

                                if (majorVersion == -1 ||
                                    ignoredVersions.Contains(majorVersion))
                                {
                                    continue;
                                }
                            }
                            sa[entriesRead++] = serverName;
                        }
                    }
                }

                //
                // we have to sort them ourselves because 
                // enumerator won't do it for us, see bug 298626.
                //
                Array.Sort(sa);
                foreach (string s in sa)
                {
                    if (s != null && s.Length > 0)
                    {
                        sqlServerNames.Add(s);
                    }
                }

                return entriesRead;
            }


            return -1;
        }
    }
}
