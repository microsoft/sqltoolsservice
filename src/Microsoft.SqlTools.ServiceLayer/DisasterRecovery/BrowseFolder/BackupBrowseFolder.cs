//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

// button: btBrowse

/*
using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using Microsoft.NetEnterpriseServers;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.SqlMgmt;

namespace Microsoft.SqlServer.Management.SqlManagerUI
{
    /// <summary>
    /// Dialog for choosing a path on the SQL server for backup
    /// </summary>
    internal class SelBakDest : Form
    {
        private eType m_eType;
        private String m_strSelectedBakDevice;
        private int m_numType = -1;
        private Hashtable m_aExclusionList;
        private CDataContainer DataContainer;
        private Enumerator m_en;
        private IMessageBoxProvider mbProvider;
        private IServiceProvider serviceProvider;

        private bool IsForRestore;
        private bool IsLocal = true;
        private Panel gbBakDest;
        private Label lbHeader;
        private Label lbDescription;
        private ComboBox cbBakDevice;
        private RadioButton rbBakDevice;
        private Button btBrowse;
        private TextBox txtFilePath;
        private RadioButton rbPath;
        private ComboBox cbTape;
        private Button btCancel;
        private Button btOK;
        protected ServerConnection sqlConnection;

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private Container components = null;

        /// <summary>
        ///  Path was already validated inside browse dialog
        /// </summary>
        private bool pathValidated;

        public enum eType
        {
            File = 0,
            Tape = 1
        }

        public SelBakDest()
        {
            InitializeComponent();
        }

        public SelBakDest(CDataContainer dataContainer, ServerConnection sqlConnection, Hashtable ExclusionList,
            eType BakType, IServiceProvider sp, bool isForRestore)
        {
            CUtils util = new CUtils();

            this.sqlConnection = sqlConnection;

            m_eType = BakType;
            IsForRestore = isForRestore;
            m_aExclusionList = new Hashtable(ExclusionList);

            DataContainer = new CDataContainer();
            DataContainer = dataContainer;

            InitializeComponent();

            SqlBackupRestoreBase backUtil = new SqlBackupRestoreBase();

            if (0 ==
                string.Compare(backUtil.GetMachineName(this.sqlConnection.ServerInstance), Environment.MachineName,
                    StringComparison.OrdinalIgnoreCase))
            {
                IsLocal = true;
            }
            else
            {
                IsLocal = false;
            }

            Icon = util.LoadIcon("database.ico");
            serviceProvider = sp;

            if (null == serviceProvider)
            {
                mbProvider = new DefaultMessageBoxProvider(this);
            }
            else
            {
                mbProvider = (IMessageBoxProvider)serviceProvider.GetService(typeof(IMessageBoxProvider));
            }

            InitStrings();
            InitControls();
            InitProp();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            MinimumSize = Size;
        }

        private void InitStrings()
        {
            rbBakDevice.Text = SelBakDestSR.BackupDevice;
            btCancel.Text = SelBakDestSR.Cancel;
            btOK.Text = SelBakDestSR.OK;
            Text = GetTitle(m_eType);
            btBrowse.AccessibleName = SelBakDestSR.AABrowse;
            cbBakDevice.AccessibleName = SelBakDestSR.AADevice;
            cbTape.AccessibleName = SelBakDestSR.AATape;
            txtFilePath.AccessibleName = SelBakDestSR.AAFile;
        }

        public String SelectedBackupDestination
        {
            get { return m_strSelectedBakDevice; }
        }

        public int Type
        {
            get { return m_numType; }
        }

        private string GetTitle(eType TypeOfDevice)
        {
            if (IsForRestore == false)
            {
                return SelBakDestSR.TitleBackup;
            }
            /// disk
            if (TypeOfDevice == eType.File)
            {
                return SelBakDestSR.TitleRestoreDisk;
            }
            /// tape 
            return SelBakDestSR.TitleRestoreTape;
        }

        private string GetDescription(eType TypeOfDevice)
        {
            /// launched from restore
            if (IsForRestore)
            {
                /// disk
                if (TypeOfDevice == eType.File)
                {
                    return SelBakDestSR.DescriptionFileRestore;
                }
                /// tape 
                return SelBakDestSR.DescriptionTapeRestore;
            }
            /// launched from backup
            /// disk
            if (TypeOfDevice == eType.File)
            {
                return SelBakDestSR.DescriptionFileBackup;
            }
            /// tape 
            return SelBakDestSR.DescriptionTapeBackup;
        }

        private string GetHeader(eType TypeOfDevice)
        {
            /// launched from restore
            if (IsForRestore)
            {
                /// disk
                if (TypeOfDevice == eType.File)
                {
                    return SelBakDestSR.HeaderFileRestore;
                }
                /// tape 
                return SelBakDestSR.HeaderTapeRestore;
            }
            /// launched from backup
            /// disk
            if (TypeOfDevice == eType.File)
            {
                return SelBakDestSR.HeaderFileBackup;
            }
            /// tape 
            return SelBakDestSR.HeaderTapeBackup;
        }

        private void InitProp()
        {
            EnumerateBakDevices();
            if (eType.Tape == m_eType)
            {
                EnumerateTapes();
            }
            else
            {
                ActiveControl = txtFilePath;
            }
        }

        private void EnumerateBakDevices()
        {

            Request req = new Request();
            int iCount;
            DataSet ds;

            cbBakDevice.Items.Clear();

            m_en = new Enumerator();

            req.Urn = "Server/BackupDevice";

            ds = m_en.Process(sqlConnection, req);
            iCount = ds.Tables[0].Rows.Count;

            int Count = 0;

            for (int i = 0; i < iCount; i++)
            {

                string szControllerType = Convert.ToString(ds.Tables[0].Rows[i]["BackupDeviceType"],
                    CultureInfo.InvariantCulture);
                string szDeviceName = (Convert.ToString(ds.Tables[0].Rows[i]["Name"], CultureInfo.InvariantCulture));
                string PhysicalLoc =
                    (Convert.ToString(ds.Tables[0].Rows[i]["PhysicalLocation"], CultureInfo.InvariantCulture))
                        .ToLowerInvariant();

                // file based backup device
                if (m_eType == eType.File)
                {
                    if ("2" == szControllerType)
                    {
                        if ((false == m_aExclusionList.Contains(szDeviceName)) &&
                            (false == m_aExclusionList.Contains(PhysicalLoc.ToLowerInvariant())))
                        {
                            cbBakDevice.Items.Add(szDeviceName);
                            Count = Count + 1;
                        }
                    }
                }
                // tape based backup device
                else
                {
                    if ("5" == szControllerType)
                    {
                        if (false == m_aExclusionList.Contains(szDeviceName) &&
                            (false == m_aExclusionList.Contains(PhysicalLoc)))
                        {
                            cbBakDevice.Items.Add(szDeviceName);
                            Count = Count + 1;
                        }
                    }
                }
            }
            if (Count > 0)
            {
                cbBakDevice.SelectedIndex = 0;
            }
            else
            {
                cbBakDevice.Enabled = false;
                rbBakDevice.Enabled = false;
                rbBakDevice.Checked = false;
            }
        }

        private void EnumerateTapes()
        {
            try
            {
                Request req = new Request();
                DataSet ds;
                Enumerator en;
                int iCount;


                cbTape.Items.Clear();

                en = new Enumerator();

                req.Urn = "Server/TapeDevice";

                ds = en.Process(sqlConnection, req);
                iCount = ds.Tables[0].Rows.Count;

                int Count = 0;

                for (int i = 0; i < iCount; i++)
                {
                    string TapeName = Convert.ToString(ds.Tables[0].Rows[i]["Name"], CultureInfo.InvariantCulture);

                    //if((false == m_aExclusionList.Contains(TapeName)) && (false == IsAssocDeviceIn(TapeName)))
                    if ((false == m_aExclusionList.Contains(TapeName)))
                    {
                        cbTape.Items.Add(TapeName);
                        Count++;
                    }
                }
                if (Count > 0)
                {
                    cbTape.SelectedIndex = 0;
                }
            }
            catch (Exception e)
            {
                mbProvider.ShowMessage(e, null,
                    ExceptionMessageBoxButtons.OK,
                    ExceptionMessageBoxSymbol.Error, this);
            }

        }

        private bool IsAssocDeviceIn(string physicalPath)
        {
            Request req = new Request();
            DataSet ds;
            Enumerator en;
            int iCount;
            bool Result = false;

            try
            {
                en = new Enumerator();

                req.Urn = "Server/BackupDevice[@PhysicalLocation='" + Urn.EscapeString(physicalPath) + "']";

                ds = en.Process(sqlConnection, req);
                iCount = ds.Tables[0].Rows.Count;

                if (iCount > 0)
                {
                    string DeviceName = Convert.ToString(ds.Tables[0].Rows[0]["Name"], CultureInfo.InvariantCulture);
                    if (m_aExclusionList.Contains(DeviceName))
                    {
                        Result = true;
                    }
                }
                return Result;
            }
            catch (SmoException e)
            {
                mbProvider.ShowMessage(e, null,
                    ExceptionMessageBoxButtons.OK,
                    ExceptionMessageBoxSymbol.Error, this);
                return Result;
            }
            catch (Exception e)
            {
                mbProvider.ShowMessage(e, null,
                    ExceptionMessageBoxButtons.OK,
                    ExceptionMessageBoxSymbol.Error, this);
                return Result;
            }
        }

        private void InitControls()
        {
            rbPath.Checked = true;
            cbBakDevice.Enabled = false;
            gbBakDest.Controls.Clear();
            if (m_eType == eType.File)
            {
                // Disk backup
                gbBakDest.Controls.AddRange(
                    new Control[]
                    {
                        lbHeader,
                        lbDescription,
                        cbBakDevice, rbBakDevice, btBrowse, txtFilePath,
                        rbPath, btCancel, btOK
                    });
                rbPath.Text = SelBakDestSR.FileName;
                lbHeader.Text = GetHeader(m_eType);
                lbDescription.Text = GetDescription(m_eType);

                SqlBackupRestoreBase backupRestoreUtil = new SqlBackupRestoreBase(DataContainer, sqlConnection);
                string defaultNewBackupLocation = backupRestoreUtil.GetDefaultBackupFolder();
                if (defaultNewBackupLocation.Length != 0)
                {
                    txtFilePath.Text = defaultNewBackupLocation +
                                       PathWrapper.PathSeparatorFromServerConnection(sqlConnection);
                }
            }
            else
            {
                // Tape backup
                gbBakDest.Controls.AddRange(
                    new Control[]
                    {
                        lbHeader,
                        lbDescription,
                        cbBakDevice, rbBakDevice,
                        rbPath, cbTape, btCancel, btOK
                    });
                rbPath.Text = SelBakDestSR.Tape;
                lbHeader.Text = GetHeader(m_eType);
                lbDescription.Text = GetDescription(m_eType);
            }
            CancelButton = btCancel;
            AcceptButton = btOK;
        }

        private bool Validate(BrowseFolderBase sender)
        {
            BrowseFolder dlg = sender as BrowseFolder;
            string path = dlg.SelectedFullFileName;
            bool result = ValidatePath(path);
            if (false == result)
            {
                if (IsForRestore)
                {
                    mbProvider.ShowMessage(SRError.InvalidFileLocation, SRError.Error,
                        ExceptionMessageBoxButtons.OK,
                        ExceptionMessageBoxSymbol.Error, this);
                }
            }
            return result;
        }

        private bool IsDestinationValid(string path, ref bool IsFolder)
        {
            Enumerator en = null;
            DataSet ds = new DataSet();
            ds.Locale = CultureInfo.InvariantCulture;
            Request req = new Request();

            en = new Enumerator();
            req.Urn = "Server/File[@FullName='" + Urn.EscapeString(path) + "']";

            ds = en.Process(sqlConnection, req);

            int iCount = ds.Tables[0].Rows.Count;

            if (iCount > 0)
            {
                IsFolder = !(Convert.ToBoolean(ds.Tables[0].Rows[0]["IsFile"], CultureInfo.InvariantCulture));
                return true;
            }
            IsFolder = false;
            return false;
        }

        private bool ValidatePath(string path)
        {
            CUtils Util = new CUtils();
            DialogResult DlgRes = DialogResult.None;
            bool IsFolder = false;

            bool Existing = IsDestinationValid(path, ref IsFolder);
            if (IsForRestore && false == Existing)
            {
                return false;
            }

            if (Existing)
            {
                if (IsFolder)
                {
                    DlgRes = mbProvider.ShowMessage(SRError.ErrorBackupPathIsFolder, SRError.SQLWorkbench,
                        ExceptionMessageBoxButtons.OK, ExceptionMessageBoxSymbol.Exclamation, this);
                    return false;
                }
                return true;
            }
            string FolderPath = PathWrapper.GetDirectoryName(path);
            if (IsLocal)
            {
                if (null != FolderPath && FolderPath.Length > 0)
                {
                    if (Directory.Exists(FolderPath))
                    {
                        return true;
                    }
                    DlgRes = mbProvider.ShowMessage(SRError.ErrorBackupInvalidPath, SRError.SQLWorkbench,
                        ExceptionMessageBoxButtons.YesNo, ExceptionMessageBoxSymbol.Question, this);
                    if (DlgRes == DialogResult.Yes)
                    {
                        return true;
                    }
                    return false;
                }
                return true;
            }
            bool isFolderOnRemote = true;
            bool existsOnRemote = IsDestinationValid(FolderPath, ref isFolderOnRemote);
            if (existsOnRemote)
            {
                return true;
            }
            DlgRes = mbProvider.ShowMessage(SRError.ErrorBackupInvalidPath, SRError.SQLWorkbench,
                ExceptionMessageBoxButtons.YesNo, ExceptionMessageBoxSymbol.Question, this);
            if (DlgRes == DialogResult.Yes)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources =
                new System.ComponentModel.ComponentResourceManager(typeof(SelBakDest));
            this.gbBakDest = new System.Windows.Forms.Panel();
            this.lbHeader = new System.Windows.Forms.Label();
            this.lbDescription = new System.Windows.Forms.Label();
            this.cbBakDevice = new System.Windows.Forms.ComboBox();
            this.rbBakDevice = new System.Windows.Forms.RadioButton();
            this.btBrowse = new System.Windows.Forms.Button();
            this.txtFilePath = new System.Windows.Forms.TextBox();
            this.rbPath = new System.Windows.Forms.RadioButton();
            this.cbTape = new System.Windows.Forms.ComboBox();
            this.btCancel = new System.Windows.Forms.Button();
            this.btOK = new System.Windows.Forms.Button();
            this.gbBakDest.SuspendLayout();
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            // 
            // gbBakDest
            // 
            resources.ApplyResources(this.gbBakDest, "gbBakDest");
            this.gbBakDest.Controls.Add(this.lbHeader);
            this.gbBakDest.Controls.Add(this.lbDescription);
            this.gbBakDest.Controls.Add(this.cbBakDevice);
            this.gbBakDest.Controls.Add(this.rbBakDevice);
            this.gbBakDest.Controls.Add(this.btBrowse);
            this.gbBakDest.Controls.Add(this.txtFilePath);
            this.gbBakDest.Controls.Add(this.rbPath);
            this.gbBakDest.Controls.Add(this.cbTape);
            this.gbBakDest.Controls.Add(this.btCancel);
            this.gbBakDest.Controls.Add(this.btOK);
            this.gbBakDest.Name = "gbBakDest";
            // 
            // lbHeader
            // 
            resources.ApplyResources(this.lbHeader, "lbHeader");
            this.lbHeader.Name = "lbHeader";
            // 
            // lbDescription
            // 
            resources.ApplyResources(this.lbDescription, "lbDescription");
            this.lbDescription.Name = "lbDescription";
            // 
            // cbBakDevice
            // 
            resources.ApplyResources(this.cbBakDevice, "cbBakDevice");
            this.cbBakDevice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbBakDevice.DropDownWidth = 320;
            this.cbBakDevice.FormattingEnabled = true;
            this.cbBakDevice.Name = "cbBakDevice";
            this.cbBakDevice.DropDown += new System.EventHandler(this.cbBakDevice_DropDown);
            // 
            // rbBakDevice
            // 
            resources.ApplyResources(this.rbBakDevice, "rbBakDevice");
            this.rbBakDevice.Name = "rbBakDevice";
            this.rbBakDevice.CheckedChanged += new System.EventHandler(this.rbBakDevice_CheckedChanged);
            // 
            // btBrowse
            // 
            resources.ApplyResources(this.btBrowse, "btBrowse");
            this.btBrowse.Name = "btBrowse";
            this.btBrowse.Click += new System.EventHandler(this.btBrowse_Click);
            // 
            // txtFilePath
            // 
            resources.ApplyResources(this.txtFilePath, "txtFilePath");
            this.txtFilePath.Name = "txtFilePath";
            this.txtFilePath.TextChanged += new System.EventHandler(this.txtFilePath_TextChanged);
            // 
            // rbPath
            // 
            resources.ApplyResources(this.rbPath, "rbPath");
            this.rbPath.Checked = true;
            this.rbPath.Name = "rbPath";
            this.rbPath.TabStop = true;
            this.rbPath.CheckedChanged += new System.EventHandler(this.rbPath_CheckedChanged);
            // 
            // cbTape
            // 
            resources.ApplyResources(this.cbTape, "cbTape");
            this.cbTape.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbTape.DropDownWidth = 320;
            this.cbTape.FormattingEnabled = true;
            this.cbTape.Name = "cbTape";
            this.cbTape.DropDown += new System.EventHandler(this.cbTape_DropDown);
            // 
            // btCancel
            // 
            resources.ApplyResources(this.btCancel, "btCancel");
            this.btCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btCancel.Name = "btCancel";
            this.btCancel.Click += new System.EventHandler(this.btCancel_Click);
            // 
            // btOK
            // 
            resources.ApplyResources(this.btOK, "btOK");
            this.btOK.Name = "btOK";
            this.btOK.Click += new System.EventHandler(this.btOK_Click);
            // 
            // SelBakDest
            // 
            resources.ApplyResources(this, "$this");
            this.CancelButton = this.btCancel;
            this.Controls.Add(this.gbBakDest);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SelBakDest";
            this.Load += new System.EventHandler(this.SelBakDest_Load);
            this.gbBakDest.ResumeLayout(false);
            this.gbBakDest.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion


        /// <summary>
        /// hook with standard help processing
        /// </summary>
        /// <param name="hevent"></param>
        protected override void OnHelpRequested(HelpEventArgs hevent)
        {
            ShowHelp();

            hevent.Handled = true;
            base.OnHelpRequested(hevent);
        }

        private void ShowHelp()
        {
            STrace.Assert(serviceProvider != null);
            ILaunchFormHost2 host2 = serviceProvider.GetService(typeof(ILaunchFormHost2)) as ILaunchFormHost2;
            STrace.Assert(host2 != null,
                "Service Provider could not provide us the ILaunchFormHost2 service required for displaying books online");
            if (host2 == null)
            {
                return;
            }

            host2.ShowHelp(AssemblyVersionInfo.VersionHelpKeywordPrefix + @".swb.selectbackupdest.f1");

        }

        private void btBrowse_Click(object sender, EventArgs e)
        {
            using (BrowseFolder browse =
                new BrowseFolder(sqlConnection,
                    true,
                    mbProvider,
                    new[] { new FileFilter(SelBakDestSR.BackupFilesFilter, "*.bak;*.trn") },
                    0)
                )
            {
                browse.SetFileValidityChecker(Validate);

                if (null != txtFilePath && txtFilePath.Text.Length > 0)
                {
                    string path = txtFilePath.Text;

                    browse.StartPath = PathWrapper.GetDirectoryName(path);
                }

                if (browse.Init())
                {
                    // path is not validated
                    pathValidated = false;

                    // show browse dialog
                    browse.ShowDialog(this);

                    if (DialogResult.OK == browse.DialogResult)
                    {
                        string filePath = browse.SelectedFullFileName;

                        // Path vas validated in browse dialog
                        pathValidated = true;

                        txtFilePath.Text = filePath;
                    }
                }
            }
        }

        private void btOK_Click(object sender, EventArgs e)
        {
            bool canClose = true;

            try
            {
                if (m_eType == eType.File)
                {
                    if (rbPath.Checked)
                    {
                        string Destination = txtFilePath.Text;

                        if (false == pathValidated)
                        {
                            /// path was directly typed in , validate it 
                            if (false == ValidatePath(Destination))
                            {
                                /// ex have been diaplayed
                                canClose = false;
                            }
                        }
                        m_strSelectedBakDevice = Destination;
                        m_numType = 1;
                    }
                    if (rbBakDevice.Checked)
                    {
                        m_strSelectedBakDevice = cbBakDevice.Text;
                        m_numType = 0;
                    }
                }
                if (m_eType == eType.Tape)
                {
                    if (rbPath.Checked)
                    {
                        m_strSelectedBakDevice = cbTape.Text;
                        m_numType = 2;
                    }
                    if (rbBakDevice.Checked)
                    {
                        m_strSelectedBakDevice = cbBakDevice.Text;
                        m_numType = 0;
                    }
                }
                if (m_aExclusionList.Contains(m_strSelectedBakDevice))
                {
                    m_strSelectedBakDevice = null;
                    throw new Exception(SRError.DestinationAlreadySelected);
                }

                if (canClose)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    txtFilePath.Focus();
                }
            }
            catch (Exception e1)
            {
                mbProvider.ShowMessage(e1, SRError.SQLWorkbench,
                    ExceptionMessageBoxButtons.OK,
                    ExceptionMessageBoxSymbol.Error, this);
                txtFilePath.Focus();
            }
        }

        private void btCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void rbPath_CheckedChanged(object sender, EventArgs e)
        {
            if (rbPath.Checked)
            {

                cbBakDevice.Enabled = false;

                if (null != txtFilePath)
                {
                    txtFilePath.Enabled = true;
                    btOK.Enabled = txtFilePath.Text.Length > 0 &&
                                   false ==
                                   txtFilePath.Text.EndsWith(this.DataContainer.Server.PathSeparator,
                                       StringComparison.Ordinal);
                }
                if (null != btBrowse)
                {
                    btBrowse.Enabled = true;
                }
                if (null != cbTape)
                {
                    cbTape.Enabled = true;
                }
            }
            else
            {

                cbBakDevice.Enabled = true;

                if (null != txtFilePath)
                {
                    txtFilePath.Enabled = false;
                    btOK.Enabled = true;
                }
                if (null != btBrowse)
                {
                    btBrowse.Enabled = false;
                }
                if (null != cbTape)
                {
                    cbTape.Enabled = false;
                }
            }
        }

        private void rbBakDevice_CheckedChanged(object sender, EventArgs e)
        {
            if (rbBakDevice.Enabled)
            {

                cbBakDevice.Enabled = true;
                if (null != txtFilePath)
                {
                    txtFilePath.Enabled = false;
                }
                if (null != btBrowse)
                {
                    btBrowse.Enabled = false;
                }
                if (null != cbTape)
                {
                    cbTape.Enabled = false;
                }

            }
            else
            {

                cbBakDevice.Enabled = false;
                if (null != txtFilePath)
                {
                    txtFilePath.Enabled = true;
                }
                if (null != btBrowse)
                {
                    btBrowse.Enabled = true;
                }
                if (null != cbTape)
                {
                    cbTape.Enabled = true;
                }
            }
        }

        private void cbBakDevice_DropDown(object sender, EventArgs e)
        {
            EnumerateBakDevices();
        }

        private void cbTape_DropDown(object sender, EventArgs e)
        {
            EnumerateTapes();
        }

        private void txtFilePath_TextChanged(object sender, EventArgs e)
        {
            string LocPath = txtFilePath.Text;
            bool IsObviousFolder = false;
            if (LocPath.Length != 0)
            {

                IsObviousFolder = LocPath.EndsWith(this.DataContainer.Server.PathSeparator, StringComparison.Ordinal);

                if (IsAssocDeviceIn(LocPath))
                {
                    // file already in use, display an error message
                    Exception ex = new Exception(SelBakDestSR.ExFileTaken);

                    mbProvider.ShowMessage(
                        ex, null,
                        ExceptionMessageBoxButtons.OK,
                        ExceptionMessageBoxSymbol.Error, this);

                    btBrowse.Focus();
                    txtFilePath.Text = string.Empty;
                }
            }


            if (rbPath.Checked && (0 == LocPath.Length || IsObviousFolder))
            {
                btOK.Enabled = false;
            }
            else
            {
                btOK.Enabled = true;
            }
        }

        private void SelBakDest_Load(object sender, EventArgs e)
        {
        }
    }
}
*/