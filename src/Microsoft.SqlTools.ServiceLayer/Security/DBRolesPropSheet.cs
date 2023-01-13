using Microsoft.SqlServer.Management.Sdk.Sfc;

using System;
using System.Windows.Forms;
using Microsoft.SqlServer.Management.SqlMgmt;
using SMO = Microsoft.SqlServer.Management.Smo;
using System.Data;


namespace Microsoft.SqlTools.ServiceLayer.Security
{
    /// <summary>
    /// Summary description for DBPropSheet.
    /// </summary>
    internal class DBRolesPropSheet : SqlMgmtTreeViewControl
    {
        public DBRolesPropSheet()
        {
        }

        public DBRolesPropSheet(CDataContainer dataContainer)
        {
            DataContainer = dataContainer;
            Init(dataContainer);
        }


        private void CheckObjects(string database, string role)
        {
            Request req = new Request();
            Enumerator en = null;
            DataSet ds = null;
            SMO.Server srv = null;

            srv = DataContainer.Server;

            if (null == database || 0 == database.Length)
            {
                throw new Exception("No Database context");
            }

            en = new Enumerator();

            req.Urn = "Server/Database[@Name='" + Urn.EscapeString(database) + "']";
            ds = en.Process(ServerConnection, req);

            if (ds.Tables[0].Rows.Count == 0)
            {
                throw new Exception(SRError.InvalidDatabase);
            }

            if (0 != role.Length)
            {
                req.Urn = "Server/Database[@Name='" + Urn.EscapeString(database) + "']/Role[@Name='" + Urn.EscapeString(role) + "']";
                ds = en.Process(ServerConnection, req);

                if (ds.Tables[0].Rows.Count == 0)
                {
                    throw new Exception(SRError.InvalidDatabaseRole);
                }
            }

        }

        public void Init(CDataContainer dataContainer)
        {
            PanelTreeNode node;
            PanelTreeNode auxNode;

            CUtils util = new CUtils();
            string szDBRoleName = String.Empty;
            string szDatabase = "";
            STParameters param;
            bool bStatus;

            InitFormLayout();

            param = new STParameters();

            param.SetDocument(dataContainer.Document);

            bStatus = param.GetParam("database", ref szDatabase);
            bStatus = param.GetParam("role", ref szDBRoleName);

            CheckObjects(szDatabase, szDBRoleName);


            this.Icon = util.LoadIcon("database.ico");
            int pageIndex = 0;

            node        = new PanelTreeNode();
            node.Text   = DBRolesPropSheetSR.DatabaseRoleProperties;
            node.Type   = eNodeType.Folder;
            node.Tag    = pageIndex++;

            UserControl dbRolesPropGeneral = new DatabaseRoleGeneral(dataContainer);
            AddView(dbRolesPropGeneral);

            auxNode         = new PanelTreeNode();
            auxNode.Text    = DBRolesPropSheetSR.General;
            auxNode.Tag     = pageIndex++;
            auxNode.Type    = eNodeType.Item;
            node.Nodes.Add(auxNode);

            SelectNode(auxNode);

            // if the role is not a system role, show the permissions page
            if (dataContainer.IsNewObject || !((SMO.DatabaseRole) dataContainer.SqlDialogSubject).IsFixedRole)
            {
                UserControl dbRolePermissions = new PermissionsDatabasePrincipal(dataContainer, SMO.PrincipalType.DatabaseRole);
                AddView(dbRolePermissions);

                auxNode         = new PanelTreeNode();
                auxNode.Text    = DBRolesPropSheetSR.Permissions;
                auxNode.Tag     = pageIndex++;
                auxNode.Type    = eNodeType.Item;
                node.Nodes.Add(auxNode);
            }

            // only yukon supports extended props on roles
            if (dataContainer.Server.Information.Version.Major > 8)
            {
                UserControl extendedProperties = new ExtendedProperties(dataContainer);
                AddView(extendedProperties);

                auxNode         = new PanelTreeNode();
                auxNode.Text    = DBRolesPropSheetSR.ExtendedProperties;
                auxNode.Tag     = pageIndex++;
                auxNode.Type    = eNodeType.Item;
                node.Nodes.Add(auxNode);
            }

            AddNode(node);

            if (null == szDBRoleName || 0 == szDBRoleName.Length)
            {
                Text = DBRolesPropSheetSR.TitleNew;
            }
            else
            {
                Text = DBRolesPropSheetSR.TitleProperties(szDBRoleName);
            }

        }
    }
}








