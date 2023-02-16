//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Management
{


    public class ActionContext
    {
        #region private members
        /// <summary>
        /// Name of the object
        /// </summary>
        string name;
        /// <summary>
        /// connection to the server
        /// </summary>
        private ServerConnection connection;
        /// <summary>
        /// Connection context
        /// </summary>
        private string contextUrn;
        /// <summary>
        /// Parent node in the tree
        /// </summary>
        //private INodeInformation parent;
        /// <summary>
        /// Weak reference to the tree node this is paired with
        /// </summary>
        WeakReference NavigableItemReference;
        /// <summary>
        /// Property handlers
        /// </summary>
        //private IList<IPropertyHandler> propertyHandlers;
        /// <summary>
        /// Property bag
        /// </summary>
        NameObjectCollection properties;
        /// <summary>
        /// Object to lock on when we are modifying public state
        /// </summary>
        private object itemStateLock = new object();
        /// <summary>
        /// Cached UrnPath
        /// </summary>
        private string urnPath;
        #endregion

        #region constructors
        
        public ActionContext(ServerConnection connection, string name, string contextUrn)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }
            if (contextUrn == null)
            {
                throw new ArgumentNullException("context");
            }
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            this.connection = connection;
            this.contextUrn = contextUrn;
            this.name = name;

            properties = new NameObjectCollection();
            //propertyHandlers = null;
            NavigableItemReference = null;
        }
        #endregion

        #region INodeInformation implementation
        public ServerConnection Connection
        {
            get
            {
                return this.connection;
            }
            set
            {
                lock (this.itemStateLock)
                {
                    this.connection = value;
                }
            }
        }
        public string ContextUrn
        {
            get
            {
                return this.contextUrn;
            }
            set
            {
                lock (this.itemStateLock)
                {
                    this.contextUrn = value;
                }
            }
        }

        public string NavigationContext
        {
            get
            {
                return GetNavigationContext(this);
            }
        }

        public string UrnPath
        {
            get
            {
                this.urnPath ??= ActionContext.BuildUrnPath(this.NavigationContext);
                return this.urnPath;
            }
        }

        public string InvariantName
        {
            get
            {
                string name = this["UniqueName"] as string;

                if (!string.IsNullOrEmpty(name))
                    return name;

                StringBuilder uniqueName = new StringBuilder();

                foreach (string urnValue in GetUrnPropertyValues())
                {
                    if (uniqueName.Length > 0)
                        uniqueName.Append(".");

                    uniqueName.Append(urnValue);
                }

                return (uniqueName.Length > 0) ? uniqueName.ToString() : new Urn(ContextUrn).Type;
            }
        }

        /// <summary>
        /// property bag for this node
        /// </summary>
        public object this[string name] => properties[name];

        public object CreateObjectInstance()
        {
            return CreateObjectInstance(this.ContextUrn, this.Connection);
        }

        #endregion

        #region ISfcPropertyProvider implementation

        public NameObjectCollection GetPropertySet()
        {
            return this.properties;
        }

        #endregion

        #region NodeName helper
        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                lock (this.itemStateLock)
                {
                    this.name = value;
                }
            }
        }
        #endregion

        #region property bag support

        public NameObjectCollection Properties
        {
            get
            {
                return this.properties;
            }
            set
            {
                lock (this.itemStateLock)
                {
                    this.properties = value;
                }
            }
        }

        #endregion

        #region helpers

        public static string GetNavigationContext(ActionContext source)
        {
            string context = source.ContextUrn;
            // see if this is a folder
            string name = source["UniqueName"] as string;
            if (name == null || name.Length == 0)
            {
                name = source.Name;
            }
            string queryHint = source["QueryHint"] as string;
            if (queryHint == null || queryHint.Length == 0)
            {
                context = string.Format(
                    System.Globalization.CultureInfo.InvariantCulture
                    , "{0}/Folder[@Name='{1}']"
                    , source.ContextUrn
                    , Urn.EscapeString(name));
            }
            else
            {
                context = string.Format(
                    System.Globalization.CultureInfo.InvariantCulture
                    , "{0}/Folder[@Name='{1}' and @Type='{2}']"
                    , source.ContextUrn
                    , Urn.EscapeString(name)
                    , Urn.EscapeString(queryHint));

            }
            return context;
        }

        /// <summary>
        /// Get the values of the keys in the current objects Urn
        /// e.g. For Table[@Name='Foo' and @Schema='Bar'] return Foo and Bar
        /// </summary>
        /// <returns></returns>
        private IEnumerable<string> GetUrnPropertyValues()
        {
            Urn urn = new Urn(ContextUrn);
            Enumerator enumerator = new Enumerator();
            RequestObjectInfo request = new RequestObjectInfo(urn, RequestObjectInfo.Flags.UrnProperties);

            ObjectInfo info = enumerator.Process(connection, request);

            if (info == null || info.UrnProperties == null)
                yield break;
        
            // Special order for Schema and Name
            if (properties.Contains("Schema"))
                yield return urn.GetAttribute("Schema");

            if (properties.Contains("Name"))
                yield return urn.GetAttribute("Name");       

            foreach (ObjectProperty obj in info.UrnProperties)
            {
                if (obj.Name.Equals("Name", StringComparison.OrdinalIgnoreCase) || obj.Name.Equals("Schema", StringComparison.OrdinalIgnoreCase))
                    continue;
                yield return urn.GetAttribute(obj.Name);
            }
        }

        public static string BuildUrnPath(string urn)
        {
            StringBuilder urnPathBuilder = new StringBuilder(urn != null ? urn.Length : 0);

            string folderName = string.Empty;
            bool replaceLeafValueInQuery = false;

            if (!string.IsNullOrEmpty(urn))
            {
                Urn urnObject = new Urn(urn);

                while (urnObject != null)
                {
                    string objectType = urnObject.Type;

                    if (string.CompareOrdinal(objectType, "Folder") == 0)
                    {
                        folderName = urnObject.GetAttribute("Name").Replace(" ", "");
                        if (folderName != null)
                        {
                            objectType = string.Format("{0}Folder", folderName);
                        }
                    }

                    // Build the path
                    if (urnPathBuilder.Length > 0)
                    {
                        urnPathBuilder.Insert(0, '/');
                    }

                    if (objectType.Length > 0)
                    {
                        urnPathBuilder.Insert(0, objectType);
                    }

                    // Remove one element from the urn
                    urnObject = urnObject.Parent;
                }

                // Build the query
                if (replaceLeafValueInQuery)
                {
                    // This is another special case for DTS urns.
                    // When we want to request data for an individual package
                    // we need to use a special urn with Leaf="2" attribute,
                    // replacing the Leaf='1' that comes from OE.
                    urnObject = new Urn(urn.Replace("@Leaf='1'", "@Leaf='2'"));
                }
                else
                {
                    urnObject = new Urn(urn);
                }
            }

            return urnPathBuilder.ToString();
        }

        public static object CreateObjectInstance(string urn, ServerConnection serverConnection)
        {
            if (string.IsNullOrEmpty(urn))
            {
                return null;
            }

            try
            {
                SfcObjectQuery oq = null;
                Urn urnObject = new Microsoft.SqlServer.Management.Sdk.Sfc.Urn(urn);

                // i have to find domain from Urn.
                // DomainInstanceName thrown NotImplemented Exception
                // so, i have to walk Urn tree to the top
                Urn current = urnObject;
                while (current.Parent != null)
                {
                    current = current.Parent;
                }
                string domainName = current.Type;

                if (domainName == "Server")
                {
                    oq = new SfcObjectQuery(new Microsoft.SqlServer.Management.Smo.Server(serverConnection));
                }
                else
                {
                    SqlConnection connection = serverConnection.SqlConnectionObject;
                    if (connection == null)
                    {
                        return null;
                    }

                    // no need to check return value - this method will throw, if domain is incorrect
                    SfcDomainInfo ddi = Microsoft.SqlServer.Management.Sdk.Sfc.SfcRegistration.Domains[domainName];

                    ISfcDomain domain = (ISfcDomain)Activator.CreateInstance(ddi.RootType, new SqlStoreConnection(connection));

                    oq = new SfcObjectQuery(domain);
                }

                foreach (object obj in oq.ExecuteIterator(new SfcQueryExpression(urn), null, null))
                {
                    return obj;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(ex.Message);
                return null;
            }

            return null;
        }

        #endregion
    }

    public class DataContainerXmlGenerator
    {
        #region private members
        /// <summary>
        /// additional xml to be passed to the dialog
        /// </summary>
        protected string rawXml = string.Empty;
        /// <summary>
        /// do not pass this type information to the dialog.
        /// e.g. New Database menu item on an existing database should not pass the database name through,
        /// so we set itemType as Database.
        /// </summary>
        protected string? itemType = string.Empty;
        /// <summary>
        /// Additional query to perform and pass the results to the dialog.
        /// </summary>
        protected string? invokeMultiChildQueryXPath = null;

        private ActionContext context;
        /// <summary>
        /// The node in the hierarchy that owns this
        /// </summary>
        public virtual ActionContext Context
        {
            get { return context; }
            set { context = value; }
        }

        private string mode;
        /// <summary>
        /// mode
        /// </summary>
        /// <example>
        /// "new" "properties"
        /// </example>
        public string Mode
        {
            get { return mode; }
            set { mode = value; }
        }

        #endregion

        #region construction
        /// <summary>
        /// 
        /// </summary>
        public DataContainerXmlGenerator(ActionContext context, string mode = "new")
        {
            this.context = context;
            this.mode = mode;
        }

        #endregion

        #region IObjectBuilder implementation
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void AddProperty(string name, object value)
        {
            //  RAWXML is xml that is added to the document we're passing to the dialog with no additional
            //  processing
            if (string.Compare(name, "rawxml", StringComparison.OrdinalIgnoreCase) == 0)
            {
                this.rawXml += value.ToString();
            }
            // ITEMTYPE is for new  menu items where we do not  want to pass in the information for this type
            // e.g. New Database menu   item on an existing database should not pass    the database name   through,
            // so we set ITEMTYPE as Database.
            else if (string.Compare(name, "itemtype", StringComparison.OrdinalIgnoreCase) == 0)
            {
                this.itemType = value.ToString();
            }
            // Allows us to query below the current level in the    enumerator and  pass the    results through to
            // the dialog. Usefull  for Do xyz on all   for menu's on folders.
            else if (string.Compare(name, "multichildqueryxpath", StringComparison.OrdinalIgnoreCase) == 0)
            {
                this.invokeMultiChildQueryXPath = value.ToString();
            }


        }
        #endregion

        #region xml
        #region xml document generation
        /// <summary>
        /// Generate an XmlDocument that contains all of the context needed to launch a dialog
        /// </summary>
        /// <returns>XmlDocument</returns>
        public virtual XmlDocument GenerateXmlDocument()
        {
            MemoryStream memoryStream = new MemoryStream();
            // build the xml
            XmlTextWriter xmlWriter = new XmlTextWriter(memoryStream, Encoding.UTF8);

            // write out the document headers
            StartXmlDocument(xmlWriter);
            // write xml specific to each connection type
            GenerateConnectionXml(xmlWriter);
            // generate the xml specific to the item we are being launched against
            GenerateItemContext(xmlWriter);
            // write out any of out properties to the document
            WritePropertiesToXml(xmlWriter);
            // close the document headers
            EndXmlDocument(xmlWriter);

            // make sure everything is commited
            xmlWriter.Flush();

            //  Resets the stream to the beginning
            memoryStream.Seek(0, SeekOrigin.Begin);

            // done composing the XML string, now build the document
            XmlDocument doc = new XmlDocument();

            // don't lose leading or trailing whitespace
            doc.PreserveWhitespace = true;

            // directly create the document from the memoryStream.
            // We do this because using an xmlreader in between would an extra
            // overhead and it also messes up the new line characters in the original
            // stream (converts all \r to \n).-anchals
            doc.Load(memoryStream);

            return doc;
        }
        #endregion

        #region document start/end
        /// <summary>
        /// Write the starting elements needed by the dialog framework
        /// </summary>
        /// <param name="xmlWriter">XmlWriter that these elements will be written to</param>
        protected virtual void StartXmlDocument(XmlWriter xmlWriter)
        {
            XmlGeneratorHelper.StartXmlDocument(xmlWriter);
        }
        /// <summary>
        /// Close the elements needed by the dialog framework
        /// </summary>
        /// <param name="xmlWriter">XmlWriter that these elements will be written to</param>
        protected virtual void EndXmlDocument(XmlWriter xmlWriter)
        {
            System.Diagnostics.Debug.Assert(xmlWriter != null, "xmlWriter should never be null.");

            // close params
            xmlWriter.WriteEndElement();
            // close formdescription
            xmlWriter.WriteEndElement();
        }
        #endregion

        #region server specific generation
        /// <summary>
        /// Generate the XML that will allow the dialog to connect to the server
        /// </summary>
        /// <param name="xmlWriter">XmlWriter that these elements will be written to</param>
        protected virtual void GenerateConnectionXml(XmlWriter xmlWriter)
        {
            XmlGeneratorHelper.GenerateConnectionXml(xmlWriter, this.Context);
        }
        /// <summary>
        /// Generate SQL Server specific connection information
        /// </summary>
        /// <param name="xmlWriter">XmlWriter that these elements will be written to</param>
        protected virtual void GenerateSqlConnectionXml(XmlWriter xmlWriter)
        {
            XmlGeneratorHelper.GenerateSqlConnectionXml(xmlWriter, this.Context);
        }

        #endregion

        #region item context generation
        /// <summary>
        /// Generate context specific to the node this menu item is being launched against.
        /// </summary>
        /// <param name="xmlWriter">XmlWriter that these elements will be written to</param>
        protected virtual void GenerateItemContext(XmlWriter xmlWriter)
        {
            System.Diagnostics.Debug.Assert(xmlWriter != null, "xmlWriter should never be null.");

            // There are two ways we can add context information. 
            // The first is just off of the node we were launched against. We will break the urn down
            // into it's individual components. And pass them to the dialog.
            // The second is by performing a query relative to the node we were launched against
            // and adding any urns that are returned. No other process will be performed on the urn

            // see if we are invoking on  single, or multiple items
            if (InvokeOnSingleItemOnly())
            {
                // no query, just an individual item
                GenerateIndividualItemContext(xmlWriter);
            }
            else
            {
                GenerateMultiItemContext(xmlWriter);
            }
        }
        /// <summary>
        /// Generate the context for an individual item.
        /// While Generating the context we will break down the Urn to it's individual elements
        /// and pass each Type attribute in individually.
        /// </summary>
        /// <param name="xmlWriter">XmlWriter that these elements will be written to</param>
        protected virtual void GenerateIndividualItemContext(XmlWriter xmlWriter)
        {
            XmlGeneratorHelper.GenerateIndividualItemContext(xmlWriter, itemType, this.Context);
        }

        /// <summary>
        /// Generate Context for multiple items.
        /// </summary>
        /// <param name="xmlWriter">XmlWriter that these elements will be written to</param>
        protected virtual void GenerateMultiItemContext(XmlWriter xmlWriter)
        {
            // there will be a query performed
            GenerateItemContextFromQuery(xmlWriter);
        }

        /// <summary>
        /// Generate Context with the results of a Query. We will just pass in the multiple
        /// Urn's if any that are the results of the query.
        /// </summary>
        /// <param name="xmlWriter">XmlWriter that these elements will be written to</param>
        protected virtual void GenerateItemContextFromQuery(XmlWriter xmlWriter)
        {
            System.Diagnostics.Debug.Assert(xmlWriter != null, "xmlWriter should never be null.");

            // generate the request
            Request request = new Request();
            // only need urn
            request.Fields = new string[] { "Urn" };
            request.Urn = new Urn(this.Context.ContextUrn + "/" + this.invokeMultiChildQueryXPath);

            DataTable dt;

            // run the query
            Enumerator enumerator = new Enumerator();
            EnumResult result = enumerator.Process(this.Context.Connection, request);

            if (result.Type == ResultType.DataTable)
            {
                dt = result;
            }
            else
            {
                dt = ((DataSet)result).Tables[0];
            }

            //TODO: Consider throwing if there are no results.
            // Write the results to the XML document
            foreach (DataRow row in dt.Rows)
            {
                WriteUrnInformation(xmlWriter, row[0].ToString());
            }

        }
        /// <summary>
        /// Writes a Urn to the XML. If this is an Olap connection we will also write out
        /// the Olap Path, which is the AMO equivelent of a Urn.
        /// </summary>
        /// <param name="xmlWriter">XmlWriter that these elements will be written to</param>
        /// <param name="urn">Urn to be written</param>
        protected virtual void WriteUrnInformation(XmlWriter xmlWriter, string? urn)
        {
            XmlGeneratorHelper.WriteUrnInformation(xmlWriter, urn, this.Context);
        }
        /// <summary>
        /// Get the list of Urn attributes for this item.
        /// </summary>
        /// <param name="urn">Urn to be checked</param>
        /// <returns>string array of Urn attribute names. This can be zero length but will not be null</returns>
        protected virtual string[] GetUrnAttributes(Urn urn)
        {
            string[]? urnAttributes = null;

            if (urn.XPathExpression != null && urn.XPathExpression.Length > 0)
            {
                int index = urn.XPathExpression.Length - 1;
                if (index > 0)
                {
                    System.Collections.SortedList list = urn.XPathExpression[index].FixedProperties;
                    System.Collections.ICollection keys = list.Keys;

                    urnAttributes = new string[keys.Count];

                    int i = 0;
                    foreach (object o in keys)
                    {
                        string? key = o.ToString();
                        if (key != null)
                        {
                            urnAttributes[i++] = key;
                        }
                    }
                }
            }
            return urnAttributes != null ? urnAttributes : new string[0];
        }
        #endregion

        #region write properties
        /// <summary>
        /// Write properties set for this menu item. These can be set to pass different information
        /// to the dialog independently of the node type.
        /// </summary>
        /// <param name="xmlWriter">XmlWriter that these elements will be written to</param>
        protected virtual void WritePropertiesToXml(XmlWriter xmlWriter)
        {
            System.Diagnostics.Debug.Assert(xmlWriter != null, "xmlWriter should never be null.");

            // mode could indicate properties or new
            if (Mode != null && Mode.Length > 0)
            {
                xmlWriter.WriteElementString("mode", Mode);
            }
            // raw xml to be passed to the dialog.
            // mostly used to control instance awareness.
            if (rawXml != null && rawXml.Length > 0)
            {
                xmlWriter.WriteRaw(rawXml);
            }
            // mostly used to restrict the context for new item off of an item of that type
            // some dialogs require this is passed in so they know what item type they are
            // supposed to be creating.
            if (this.itemType.Length > 0)
            {
                xmlWriter.WriteElementString("itemtype", this.itemType);
            }
        }
        #endregion
        #endregion

        #region protected helpers
        /// <summary>
        /// Inidicates whether the source is a single or multiple items.
        /// </summary>
        /// <returns></returns>
        protected virtual bool InvokeOnSingleItemOnly()
        {
            return (this.invokeMultiChildQueryXPath == null || this.invokeMultiChildQueryXPath.Length == 0);
        }
        #endregion
    }

    /// <summary>
    /// provides helper methods to generate LaunchForm XML and launch certain wizards and dialogs
    /// </summary>
    public static class XmlGeneratorHelper
    {
        /// <summary>
        /// Write the starting elements needed by the dialog framework
        /// </summary>
        /// <param name="xmlWriter">XmlWriter that these elements will be written to</param>
        public static void StartXmlDocument(XmlWriter xmlWriter)
        {
            System.Diagnostics.Debug.Assert(xmlWriter != null, "xmlWriter should never be null.");

            xmlWriter.WriteStartElement("formdescription");
            xmlWriter.WriteStartElement("params");
        }

        /// <summary>
        /// Writes a Urn to the XML. If this is an Olap connection we will also write out
        /// the Olap Path, which is the AMO equivelent of a Urn.
        /// </summary>
        /// <param name="xmlWriter">XmlWriter that these elements will be written to</param>
        /// <param name="urn">Urn to be written</param>
        public static void WriteUrnInformation(XmlWriter xmlWriter, string urn, ActionContext context)
        {
            System.Diagnostics.Debug.Assert(xmlWriter != null, "xmlWriter should never be null.");

            // write the Urn
            xmlWriter.WriteElementString("urn", urn);
        }

        /// <summary>
        /// Generate the XML that will allow the dialog to connect to the server
        /// </summary>
        /// <param name="xmlWriter">XmlWriter that these elements will be written to</param>
        public static void GenerateConnectionXml(XmlWriter xmlWriter, ActionContext context)
        {
            System.Diagnostics.Debug.Assert(xmlWriter != null, "xmlWriter should never be null.");

            // framework also needs to know the type
            string serverType = string.Empty;

            // Generate Connection specific XML.
            if (context.Connection is ServerConnection)
            {
                GenerateSqlConnectionXml(xmlWriter, context);
                serverType = "sql";
            }
            else
            {
                System.Diagnostics.Debug.Assert(false, "Warning: Connection type is unknown.");
            }

            System.Diagnostics.Debug.Assert(serverType.Length > 0, "serverType has not been defined");
            xmlWriter.WriteElementString("servertype", serverType);
        }

        /// <summary>
        /// Generate SQL Server specific connection information
        /// </summary>
        /// <param name="xmlWriter">XmlWriter that these elements will be written to</param>
        public static void GenerateSqlConnectionXml(XmlWriter xmlWriter, ActionContext context)
        {
            System.Diagnostics.Debug.Assert(xmlWriter != null, "xmlWriter should never be null.");

            // write the server name
            xmlWriter.WriteElementString("servername", context.Connection.ServerInstance);
        }

        /// <summary>
        /// Generate the context for an individual item.
        /// While Generating the context we will break down the Urn to it's individual elements
        /// and pass each Type attribute in individually.
        /// </summary>
        /// <param name="xmlWriter">XmlWriter that these elements will be written to</param>
        public static void GenerateIndividualItemContext(XmlWriter xmlWriter, string itemType, ActionContext context)
        {
            System.Diagnostics.Debug.Assert(xmlWriter != null, "xmlWriter should never be null.");
            System.Diagnostics.Debug.Assert(context.ContextUrn != null, "No context available.");

            Urn urn = new Urn(context.ContextUrn);

            foreach (KeyValuePair<string, string> item in ExtractUrnPart(itemType, urn))
            {
                xmlWriter.WriteElementString(item.Key, item.Value);
            }

            // if we are filtering out the information for this level (e.g. new database on a database should not
            // pass in the information relating to the selected database. We need to make sure that the Urn we pass
            // in is trimmed as well.
            Urn sourceUrn = new Urn(context.ContextUrn);
            if (itemType != null
                && itemType.Length > 0
                && sourceUrn.Type == itemType)
            {
                sourceUrn = sourceUrn.Parent;
            }

            // as well as breaking everything down we will write the Urn directly
            // into the XML. Some dialogs will use the individual items, some will
            // use the Urn.
            WriteUrnInformation(xmlWriter, sourceUrn, context);
        }

        public static IEnumerable<KeyValuePair<string, string>> ExtractUrnPart(string itemType, Urn urn)
        {
            // break the urn up into individual xml elements, and add each item
            // so Database[@Name='foo']/User[@Name='bar']
            // will become
            // <database>foo</database>
            // <user>bar</user>
            // Note: We don't care about server. It is taken care of elsewhere.
            // The dialogs need every item to be converted to lower case or they will not
            // be able to retrieve the information.
            do
            {
                // server information has already gone in, and is server type specific
                // don't get it from the urn
                if (urn.Parent != null)
                {
                    // get the attributes for this part of the Urn. For Olap this is ID, for
                    // everything else it is usually Name, although Schema may also be used for SQL
                    string[] urnAttributes = UrnUtils.GetUrnAttributes(urn);

                    // make sure we are not supposed to skip this type. The skip allows us to bring up a "new"
                    // dialog on an item of that type without passing in context.
                    // e.g. New Database... on AdventureWorks should not pass in <database>AdventureWorks</Database>
                    if (string.Compare(urn.Type, itemType, StringComparison.OrdinalIgnoreCase) != 0)
                    {
                        for (int i = 0; i < urnAttributes.Length; ++i)
                        {
                            // Some Urn attributes require special handling. Don't ask me why
                            string thisUrnAttribute = urnAttributes[i].ToLower(CultureInfo.InvariantCulture);
                            string elementName;
                            switch (thisUrnAttribute)
                            {
                                case "schema":
                                case "categoryid":
                                    elementName = thisUrnAttribute;
                                    break;
                                default:
                                    elementName = urn.Type.ToLower(CultureInfo.InvariantCulture); // I think it's always the same as thisUrnAttribute but I'm not sure
                                    break;
                            }
                            yield return new KeyValuePair<string, string>(elementName, urn.GetAttribute(urnAttributes[i]));
                        }
                    }
                }
                urn = urn.Parent;
            }
            while (urn != null);
        }
    }
}
