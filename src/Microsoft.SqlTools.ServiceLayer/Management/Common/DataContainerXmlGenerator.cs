//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlTools.ServiceLayer.Management
{
    public static class SqlStudioConstants
    {
        // context
        public const string SSMSPrefix = "ssms:";
        public const string UrnPath = "ssms:UrnPath";
        public const string UrnPathSkeleton = "ssms:UrnPathSkelton";
        public const string UrnNotificationPath = "ssms:UrnNotificationPath";
        public const string Name = "ssms:Name";
        public const string Server = "ssms:Server";
        public const string HelpID = "ssms:HelpID";
        public const string ConnectionInfo = "ssms:ConnectionInfo";
        public const string UIConnectionInfo = "ssms:UIConnectionInfo";
        public const string CompatibilityLevel = "ssms:CompatibilityLevel";
        public const string NodeContexts = "ssms:NodeContexts";
        public const string QueryHint = "ssms:QueryHint";
        public const string PowerShellUrnHint = "ssms:PowerShellUrnHint";

        public const string ActionMoniker = "ssms:ActionMoniker";
        public const string ActionHandlerMoniker = "ssms:ActionHandlerMoniker";

        public const string SelectedExplorerViewItems = "ssms:SelectedExplorerViewItems";

        public static string    ContextDomain<TDomain>()
        {
            return "ssms:Domain:" + typeof(TDomain).FullName;
        }

        public static string    ContextInstance<TInstance>()
        {
            return "ssms:Instance:" + typeof(TInstance).FullName;
        }



        // user settings
        public const string CurrentUserDirectory = "ssms:CurrentUserDirectory";
        public const string DefaultUserDirectory = "ssms:DefaultUserDirectory";

        // log viewer
        public const string LogType = "ssms:LogType";
        public const string ShowNTLog = "ssms:ShowNTLog";

        // host attributes

        /// <summary>
        /// Set to a non-null value by non-SSMS action hosts. 
        /// "minimal" informs the action handler to act as a top level form
        /// "full" informs the action handler there's a full hosting shell
        /// </summary>
        public const string ShellType = "ssms:ShellType";

        /// <summary>
        /// Value of the ShellType property in a hosting app without its own top level form
        /// </summary>
        public const string  ShellTypeMinimal = "minimal";

        /// <summary>
        /// Value of the ShellType property in a hosting app with its own top level form
        /// </summary>
        public const string ShellTypeFull = "full";
    }

    public interface IPropertyDictionary : ISfcPropertySet
                                         , IDictionary<string, object>
    {
        /// <summary>
        /// Adding property to the dictionary
        /// </summary>
        /// <param name="property"></param>
        void Add(ISfcProperty property);

        /// <summary>
        /// Adding property to the dictionary
        /// </summary>
        /// <param name="property"></param>
        /// <param name="collisionResolution"></param>
        void Add(ISfcProperty property, PropertyCollisionResolution collisionResolution);

        /// <summary>
        /// Adding property to the dictionary
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        void Add(string name, Type type);

        /// <summary>
        /// Adding property to the dictionary
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="collisionResolution"></param>
        void Add(string name, Type type, PropertyCollisionResolution collisionResolution);

        /// <summary>
        /// Adding property to the dictionary
        /// </summary>
        /// <param name="items"></param>
        /// <param name="collisionResolution"></param>
        void Add(IEnumerable<KeyValuePair<string, object>> items, PropertyCollisionResolution collisionResolution);

        /// <summary>
        /// Adding property to the dictionary
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="collisionResolution"></param>
        void Add(string key, object value, PropertyCollisionResolution collisionResolution);

        /// <summary>
        /// Adding property to the dictionary
        /// </summary>
        /// <param name="item"></param>
        /// <param name="collisionResolution"></param>
        void Add(KeyValuePair<string, object> item, PropertyCollisionResolution collisionResolution);
    }

    public enum PropertyCollisionResolution
    {
        Throw,
        Ignore,
        Override,
    }

    public interface IContext : IPropertyDictionary
                            , IServiceProvider
                            , IEquatable<IContext>
    {
    }

    public interface INodeContext
	{
      /// <summary>
      /// Connection information.
      /// </summary>
      SqlOlapConnectionInfoBase Connection
		{
			get;
			set;
		}

      /// <summary>
      /// Enumerators urn for this node.
      /// </summary>
		string Context
		{
			get;
			set;
		}

        /// <summary>
        ///  returns scheleton Path for current context
        /// </summary>
        string UrnPath
        {
            get;
        }

        /// <summary>
        ///  returns Notifocation (folder) context
        /// </summary>
        string NavigationContext
        {
            get;
        }

        /// <summary>
        /// Creates an instance of the object 
        /// defined by Context and Connection properties
        /// </summary>
        /// <returns></returns>
        object CreateObjectInstance();
    }

    internal class DataContainerXmlGenerator
    {
        #region private members
        /// <summary>
        /// radditional xml to be passed to the dialog
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

        private INodeContext parent;
        /// <summary>
        /// The node in the hierarchy that owns this
        /// </summary>
        public virtual INodeContext Parent
        {
            get { return parent; }
            set { parent = value; }
        }

         private string mode = null;
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
        public DataContainerXmlGenerator()
        {
        }

        // /// <summary>
        // /// 
        // /// </summary>
        // /// <param name="item"></param>
        // public ToolsMenuItem(ToolsMenuItem item)
        //     : base(item)
        // {
        //     this.rawXml = item.rawXml;
        //     this.itemType = item.itemType;
        //     this.invokeMultiChildQueryXPath = item.invokeMultiChildQueryXPath;
        // }

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
            if (String.Compare(name, "rawxml", StringComparison.OrdinalIgnoreCase) == 0)
            {
                this.rawXml += value.ToString();
            }
            // ITEMTYPE is for new  menu items where we do not  want to pass in the information for this type
            // e.g. New Database menu   item on an existing database should not pass    the database name   through,
            // so we set ITEMTYPE as Database.
            else if (String.Compare(name, "itemtype", StringComparison.OrdinalIgnoreCase) == 0)
            {
                this.itemType = value.ToString();
            }
            // Allows us to query below the current level in the    enumerator and  pass the    results through to
            // the dialog. Usefull  for Do xyz on all   for menu's on folders.
            else if (String.Compare(name, "multichildqueryxpath", StringComparison.OrdinalIgnoreCase) == 0)
            {
                this.invokeMultiChildQueryXPath = value.ToString();
            }

            // switch (name.ToUpper(System.Globalization.CultureInfo.InvariantCulture))
            // {
            //     case "MENUTEXT":
            //         this.Text = value as string;
            //         break;
            //     case "TYPE":
            //         this.Type = value as string;
            //         break;
            //     case "ASSEMBLY":
            //         this.Assembly = value as string;
            //         break;
            //     case "MultiSelect":
            //         this.MultiSelect = false;//(de.Value as bool) ? true : false;
            //         break;
            //     case "MODE":
            //         this.Mode = value as string;
            //         break;
            //     case "NAME":
            //         this.name = value as string;
            //         break;
            //     case "GUID":
            //         this.commandGuid = new Guid(value as string);
            //         break;
            //     case "ITEMID":
            //         this.itemId = Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
            //         break;
            //     case "ACCESSHANDLER":
            //         this.accessHandlers.Add(value as IAccessModifier);
            //         break;
            //     case "ACTIONNAME":
            //         this.actionName = value as string;
            //         break;
            //     default:
            //         break;
            // }

        }
        #endregion

        // protected void Invoke()
        // {
        //     if (Parent != null)
        //     {
        //         try
        //         {
        //             // build the xml document that will be passed to the form
        //             XmlDocument doc = GenerateXmlDocument();

        //             ToolMenuItemHelper.Launch(doc, Parent.Connection, Parent);
        //         }
        //         catch (Exception e)
        //         {
        //             //StaticHelpers.ShowError(Parent, e);
        //         }
        //         finally
        //         {
        //             //Cursor.Current = oldCursor;
        //         }
        //     }
        // }


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
            ToolMenuItemHelper.StartXmlDocument(xmlWriter);
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
            ToolMenuItemHelper.GenerateConnectionXml(xmlWriter, Parent);
        }
        /// <summary>
        /// Generate SQL Server specific connection information
        /// </summary>
        /// <param name="xmlWriter">XmlWriter that these elements will be written to</param>
        protected virtual void GenerateSqlConnectionXml(XmlWriter xmlWriter)
        {
            ToolMenuItemHelper.GenerateSqlConnectionXml(xmlWriter, Parent);
        }

        // /// <summary>
        // /// Generate SQL CE Specific connection informatio
        // /// </summary>
        // /// <param name="xmlWriter">XmlWriter that these elements will be written to</param>
        // protected virtual void GenerateSqlCeConnectionXml(XmlWriter xmlWriter)
        // {
        //     ToolMenuItemHelper.GenerateSqlCeConnectionXml(xmlWriter, Parent);
        // }

        // /// <summary>
        // /// Generate Report Server Specific XML
        // /// </summary>
        // /// <param name="xmlWriter">XmlWriter that these elements will be written to</param>
        // protected virtual void GenerateRsConnectionXml(XmlWriter xmlWriter)
        // {
        //     ToolMenuItemHelper.GenerateRsConnectionXml(xmlWriter, Parent);
        // }
        // /// <summary>
        // /// Generate Olap Specific XML
        // /// </summary>
        // /// <param name="xmlWriter">XmlWriter that these elements will be written to</param>
        // protected virtual void GenerateOlapConnectionXml(XmlWriter xmlWriter)
        // {
        //     ToolMenuItemHelper.GenerateOlapConnectionXml(xmlWriter, Parent);

        // }
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
            ToolMenuItemHelper.GenerateIndividualItemContext(xmlWriter, itemType, Parent);
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
            request.Fields = new String[] { "Urn" };
            request.Urn = new Urn(this.Parent.Context + "/" + this.invokeMultiChildQueryXPath);

            DataTable dt;

            // run the query
            Enumerator enumerator = new Enumerator();
            EnumResult result = enumerator.Process(this.Parent.Connection, request);

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
            ToolMenuItemHelper.WriteUrnInformation(xmlWriter, urn, Parent);
        }
        /// <summary>
        /// Get the list of Urn attributes for this item.
        /// </summary>
        /// <param name="urn">Urn to be checked</param>
        /// <returns>String array of Urn attribute names. This can be zero length but will not be null</returns>
        protected virtual string[] GetUrnAttributes(Urn urn)
        {
            String[]? urnAttributes = null;

            if (urn.XPathExpression != null && urn.XPathExpression.Length > 0)
            {
                int index = urn.XPathExpression.Length - 1;
                if (index > 0)
                {
                    System.Collections.SortedList list = urn.XPathExpression[index].FixedProperties;
                    System.Collections.ICollection keys = list.Keys;

                    urnAttributes = new String[keys.Count];

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
            return urnAttributes != null ? urnAttributes : new String[0];
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
            // // assemblyname, if this isn't passed the dialog will not launch
            // xmlWriter.WriteElementString("assemblyname", this.Assembly);
            // // formtype, namespace and class to be invoked
            // xmlWriter.WriteElementString("formtype", this.Type);
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
    public static class ToolMenuItemHelper
    {
        // /// <summary>
        // /// Guid that denotes the name of a tag in XmlDocument that indicates whether
        // /// instances of the form initialized with XmlDocument containing this GUID
        // /// should be reusable [in case when user   clicks on the same menu item many times]
        // /// or not. If they are reusable, then the existing form will be activated if the
        // /// user had selected the same menu command many times. If they are not, then
        // /// the new instance of the form will be launched
        // /// </summary>
        // public const string ReusableFormIndicator = "A32942B7-FBDE-4ac3-B84E-F5EC89961094";

        // public static void Launch(XmlDocument doc, SqlOlapConnectionInfoBase connection, IServiceProvider parentProvider)
        // {
        //     //get string    that we'll use  as  form's moniker
        //     string formMoniker = doc.InnerXml;

        //     XmlNodeList creatableIndicator = doc.GetElementsByTagName(ReusableFormIndicator);
        //     bool formIsReusable = (creatableIndicator != null && creatableIndicator.Count != 0);

        //     if (formIsReusable)
        //     {
        //         //see   if  handler of this menu    command is already up and can   be  reused
        //         CheckExistAndActivateOptions activateOptions = CheckExistAndActivateOptions.ActivateIfVisible;
        //         if (Microsoft.SqlServer.Management.SqlMgmt.RunningFormsTable.Table.CheckExistAndActivateIfNeeded(formMoniker, activateOptions))
        //         {
        //             //it    returns true if it activated existing UI handler of this    command
        //             return;
        //         }
        //     }


        //     Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer.LaunchFormHost host = new Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer.LaunchFormHost(parentProvider);
        //     System.ComponentModel.Design.ServiceContainer svc = host.CreateServiceContainer();

        //     INotifyItemChanged notifyChanges = parentProvider.GetService(typeof(INotifyItemChanged)) as INotifyItemChanged;
        //     if (notifyChanges != null)
        //     {
        //         svc.AddService(typeof(INotifyItemChanged), notifyChanges);
        //     }

        //     IScriptingOptions scriptingOptions = parentProvider.GetService(typeof(IScriptingOptions)) as IScriptingOptions;
        //     if (scriptingOptions != null)
        //     {
        //         svc.AddService(typeof(IScriptingOptions), scriptingOptions);
        //     }

        //     ManagedConnectionFactoryServiceProvider managedConnectionFactoryServiceProvider = new ManagedConnectionFactoryServiceProvider(connection, svc);

        //     //this  call will block until the form is about to be shown, but is guaranteed to have done
        //     //the initialization successfully
        //     Microsoft.SqlServer.Management.SqlMgmt.RunningFormsTable.Table.CreateAndShowForm(managedConnectionFactoryServiceProvider,
        //                                                                                      doc, formIsReusable, formMoniker, new CreateAndShowFormEventHandler(ToolMenuItemHelper.OnCreateAndShowForm));
        // }


        // /// <summary>
        // /// Passed as parameter to IRunningFormsTable.CreateAndShowForm. 
        // /// It  is  responsible for creating an instance of Form-derived object
        // /// and returning it
        // /// </summary>
        // /// <param name="sp">
        // /// service provider that was passed    in  into 
        // /// Microsoft.SqlServer.Management.SqlMgmt.RunningFormsTable.Table.CreateAndShowForm
        // /// </param>
        // /// <param name="xmlDoc">
        // /// XML Document that   was passed in into 
        // /// Microsoft.SqlServer.Management.SqlMgmt.RunningFormsTable.Table.CreateAndShowForm
        // /// </param>
        // /// <returns>
        // /// Created form instance when  everything went as planned, 
        // /// NULL if failed  to  create and showed   an  error   message,
        // /// throws exception if failed  to  create and doesn't want to  show an error message
        // /// </returns>
        // public static Form OnCreateAndShowForm(IServiceProvider sp, XmlDocument doc)
        // {
        //     System.Diagnostics.Debug.Assert(sp != null);
        //     System.Diagnostics.Debug.Assert(doc != null);
        //     Object o = null;

        //     try
        //     {
        //         System.Type formType = null;

        //         string assembly = doc.DocumentElement.SelectSingleNode("/formdescription/params/assemblyname").InnerText;
        //         string type = doc.DocumentElement.SelectSingleNode("/formdescription/params/formtype").InnerText;


        //         System.Reflection.Assembly targetAssembly = StaticHelpers.LoadAssembly(assembly);
        //         if (targetAssembly != null)
        //         {
        //             formType = targetAssembly.GetType(type);
        //             // formType will be null    if  we're   trying to launch a dbCommander as they  no  longer
        //             // take an xmldocument in the constructor.
        //             // we now   go  through the dialog launcher to launch dialogs.
        //             if (formType != null)
        //             {
        //                 //we try    3 different ctors   in  the following order:    
        //                 //1) ctor that  takes   IServiceProvider and    XmlDocument
        //                 //2) ctor that  takes   INodeInformation and    XmlDocument
        //                 //3) ctor that  takes   just XmlDocument
        //                 System.Type[] ctorTypes = new Type[] { typeof(XmlDocument), typeof(IServiceProvider) };
        //                 ConstructorInfo constInfo = formType.GetConstructor(ctorTypes);
        //                 if (null != constInfo)
        //                 {
        //                     Object[] constArgs = new Object[2];
        //                     constArgs[0] = doc;
        //                     constArgs[1] = sp;
        //                     o = constInfo.Invoke(constArgs);
        //                 }
        //                 else
        //                 {
        //                     ctorTypes = new Type[] { typeof(XmlDocument), typeof(INodeInformation) };
        //                     constInfo = formType.GetConstructor(ctorTypes);
        //                     if (null != constInfo)
        //                     {
        //                         Object[] constArgs = new Object[2];
        //                         constArgs[0] = doc;
        //                         constArgs[1] = Microsoft.SqlServer.Management.ServiceProvider.GetService<INodeInformation>(sp);
        //                         o = constInfo.Invoke(constArgs);
        //                     }
        //                     else
        //                     {
        //                         ctorTypes = new Type[] { typeof(XmlDocument) };
        //                         constInfo = formType.GetConstructor(ctorTypes);
        //                         if (null != constInfo)
        //                         {
        //                             Object[] constArgs = new Object[1];
        //                             constArgs[0] = doc;
        //                             o = constInfo.Invoke(constArgs);
        //                         }
        //                     }
        //                 }
        //             }
        //         }

        //         if (o is Form)
        //         {
        //             IObjectWithSite ios = o as IObjectWithSite;
        //             if (ios != null)
        //             {
        //                 ios.SetSite(sp);
        //             }
        //         }
        //         // true if  it  is  a wizard
        //         else
        //         {
        //             // see  if  the launchform  can launch it
        //             o = new LaunchForm(doc, sp);
        //         }

        //         if (formType != null)
        //         {                    
        //             // use the thread name to indicate the dialog.
        //             // CLR thread name can only be set once, ignore the invalid operation exception thrown if it has already
        //             // been set.
        //             try
        //             {
        //                 System.Threading.Thread.CurrentThread.Name = "Management Dialog: " + formType.FullName;
        //             }
        //             catch (InvalidOperationException)
        //             { }
        //         }

        //         Form form = o as Form;
        //         if (form != null)
        //         {
        //             return form;
        //         }
        //         else
        //         {
        //             System.Diagnostics.Debug.Assert(false, "ToolsMenuItem.OnCreateAndShowForm: could not  get Form    object via Reflection!!!");
                    
        //             //BUGBUG - should we have internal exception in this case?
        //             ArgumentException innerEx = new ArgumentException(SRError.TypeShouldBeFromDerived, "doc");
        //             throw new ApplicationException(SRError.CannotExecuteMenuCommand, innerEx);
        //         }
        //     }
        //     catch (Exception e)
        //     {
        //         System.Diagnostics.Trace.TraceError(e.Message);
        //         System.Reflection.TargetInvocationException tie = e as System.Reflection.TargetInvocationException;
        //         // show inner exception as "Exception thrown by target of invocation" isn't
        //         // very user friendly                    
        //         if (tie != null)
        //         {
        //             ApplicationException outerEx = new ApplicationException(SRError.CannotExecuteMenuCommand, e.InnerException);
        //             outerEx.Source = "";
        //             throw outerEx;
        //         }
        //         else
        //         {
        //             throw;
        //         }
        //     }
        // }


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
        public static void WriteUrnInformation(XmlWriter xmlWriter, string urn, INodeContext context)
        {
            System.Diagnostics.Debug.Assert(xmlWriter != null, "xmlWriter should never be null.");

            // write the Urn
            xmlWriter.WriteElementString("urn", urn);

            // if nessesary write out the olap info
            if (context.Connection is OlapConnectionInfo)
            {
                xmlWriter.WriteStartElement("olappath");
                // ConvertUrnToDataPath returns XML so write it as raw
                xmlWriter.WriteRaw(UrnDataPathConverter.ConvertUrnToDataPath(new Urn(urn)));
                xmlWriter.WriteEndElement();
            }

        }

        /// <summary>
        /// Generate the XML that will allow the dialog to connect to the server
        /// </summary>
        /// <param name="xmlWriter">XmlWriter that these elements will be written to</param>
        public static void GenerateConnectionXml(XmlWriter xmlWriter, INodeContext context)
        {
            System.Diagnostics.Debug.Assert(xmlWriter != null, "xmlWriter should never be null.");

            // framework also needs to know the type
            String serverType = String.Empty;

            // Generate Connection specific XML.
            // TODO: This should be refactored away from if type == else...
            if (context.Connection is SqlConnectionInfo)
            {
                GenerateSqlConnectionXml(xmlWriter, context);
                serverType = "sql";
            }
//             else if (context.Connection is SqlCeConnectionInfo)
//             {
//                 GenerateSqlCeConnectionXml(xmlWriter, context);
//                 serverType = "sqlce";
//             }
// #if !SSMS_EXPRESS
//             else if (context.Connection.GetType().ToString() == "Microsoft.SqlServer.Management.UI.RSClient.RSConnectionInfo")
//             {
//                 GenerateRsConnectionXml(xmlWriter, context);
//                 serverType = "rss";
//             }
//             else if (context.Connection is OlapConnectionInfo)
//             {
//                 GenerateOlapConnectionXml(xmlWriter, context);
//                 serverType = "olap";
//             }
// #endif
            else
            {
                System.Diagnostics.Debug.Assert(false, "Warning: Parents  ConnectionInfo  type is unknown.");
            }

            System.Diagnostics.Debug.Assert(serverType.Length > 0, "serverType has not been defined");
            // xmlWriter.WriteElementString("connectionmoniker", ConnectionCache.GetConnectionKeyName(context.Connection));
            xmlWriter.WriteElementString("servertype", serverType);
        }

        /// <summary>
        /// Generate SQL Server specific connection information
        /// </summary>
        /// <param name="xmlWriter">XmlWriter that these elements will be written to</param>
        public static void GenerateSqlConnectionXml(XmlWriter xmlWriter, INodeContext context)
        {
            System.Diagnostics.Debug.Assert(xmlWriter != null, "xmlWriter should never be null.");

            // write the server name
            xmlWriter.WriteElementString("servername", context.Connection.ServerName);
        }

//         /// <summary>
//         /// Generate SQL CE Specific connection informatio
//         /// </summary>
//         /// <param name="xmlWriter">XmlWriter that these elements will be written to</param>
//         public static void GenerateSqlCeConnectionXml(XmlWriter xmlWriter, INodeContext context)
//         {
//             System.Diagnostics.Debug.Assert(xmlWriter != null, "xmlWriter should never be null.");

//             SqlCeConnectionInfo ci = (SqlCeConnectionInfo)context.Connection;

//             xmlWriter.WriteElementString("database", ci.ServerName);
//             xmlWriter.WriteElementString("connecttimeout", ci.ConnectionTimeout.ToString());
//             xmlWriter.WriteElementString("maxdatabasesize", ci.MaxDatabaseSize.ToString());
//             xmlWriter.WriteElementString("defaultlockescalation", ci.DefaultLockEscalation.ToString());
//         }

// #if !SSMS_EXPRESS
//         /// <summary>
//         /// Generate Report Server Specific XML
//         /// </summary>
//         /// <param name="xmlWriter">XmlWriter that these elements will be written to</param>
//         public static void GenerateRsConnectionXml(XmlWriter xmlWriter, INodeContext context)
//         {
//             xmlWriter.WriteElementString("servername", context.Connection.ServerName);

//             Type type = context.Connection.GetType();

//             PropertyInfo connStringProp = type.GetProperty("ConnectionString");

//             MethodInfo connStringGet = (connStringProp != null) ? connStringProp.GetGetMethod() : null;

//             string connectionString = (connStringGet != null) ? connStringGet.Invoke(context.Connection, null).ToString() : string.Empty;

//             xmlWriter.WriteElementString("connectionstring", connectionString);
//             // if you don't specify trusted or username, password you will get an
//             // exception.
//             xmlWriter.WriteElementString("trusted", "true");
//         }
//         /// <summary>
//         /// Generate Olap Specific XML
//         /// </summary>
//         /// <param name="xmlWriter">XmlWriter that these elements will be written to</param>
//         public static void GenerateOlapConnectionXml(XmlWriter xmlWriter,
//             Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer.INodeContext context)
//         {
//             System.Diagnostics.Debug.Assert(xmlWriter != null, "xmlWriter should never be null.");

//             xmlWriter.WriteElementString("olapservername", context.Connection.ServerName);
//             xmlWriter.WriteElementString("trusted", "true");
//         }
// #endif
        /// <summary>
        /// Generate the context for an individual item.
        /// While Generating the context we will break down the Urn to it's individual elements
        /// and pass each Type attribute in individually.
        /// </summary>
        /// <param name="xmlWriter">XmlWriter that these elements will be written to</param>
        public static void GenerateIndividualItemContext(XmlWriter xmlWriter,string itemType, INodeContext context)
        {
            System.Diagnostics.Debug.Assert(xmlWriter != null, "xmlWriter should never be null.");
            System.Diagnostics.Debug.Assert(context.Context != null, "No context available.");

            Urn urn = new Urn(context.Context);

            foreach (KeyValuePair<string, string> item in ExtractUrnPart(itemType, urn))
            {
                xmlWriter.WriteElementString(item.Key, item.Value);
            }

            // if we are filtering out the information for this level (e.g. new database on a database should not
            // pass in the information relating to the selected database. We need to make sure that the Urn we pass
            // in is trimmed as well.
            Urn sourceUrn = new Urn(context.Context);
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
                    if (String.Compare(urn.Type, itemType, StringComparison.OrdinalIgnoreCase) != 0)
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
    //     /// <summary>
    //     /// Generate the monikor for maintenanceplan Execute menuitem       
    //     /// </summary>

    //     public static XmlDocument GenerateXmlDocumentForMaintenanceplanExecute(INodeContext context)
    //     {
    //         MemoryStream memoryStream = new MemoryStream();
    //         XmlTextWriter xmlWriter = new XmlTextWriter(memoryStream, Encoding.UTF8);

    //         ToolMenuItemHelper.StartXmlDocument(xmlWriter);
    //         GenerateConnectionXml(xmlWriter, context);
    //         GenerateIndividualItemContext(xmlWriter, String.Empty, context);
    //         // write out MP execute properties to the document.
    //         //xmlWriter.WriteElementString(ReusableFormIndicator, null);
    //         xmlWriter.WriteElementString("assemblyname", "SqlManagerUi.dll");
    //         xmlWriter.WriteElementString("formtype", "Microsoft.SqlServer.Management.SqlManagerUI.MaintenancePlanMenu_Run");
    //         xmlWriter.WriteEndElement();
    //         xmlWriter.WriteEndElement();
    //         xmlWriter.Flush();
    //         memoryStream.Seek(0, SeekOrigin.Begin);

    //         XmlDocument doc = new XmlDocument();
    //         doc.PreserveWhitespace = true;
   	// 		// directly create the document from the memoryStream.
	// 		// We do this because using an xmlreader in between would an extra
	// 		// overhead and it also messes up the new line characters in the original
	// 		// stream (converts all \r to \n).-anchals
    //         doc.Load(memoryStream);
    //         return doc;
    //     }

    //     /// <summary>
    //     /// We have to diable the execute menuitem on MP if the "Execute window" is already up.       
    //     /// </summary>
    //     public static void CheckAndDisableMaintenancePlanExecute(ContextMenuStrip contextMenu,
    //         Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer.INodeContext context)
    //     {
    //         XmlDocument doc = ToolMenuItemHelper.GenerateXmlDocumentForMaintenanceplanExecute(context);
    //         //get string that we'll use  as  form's moniker
    //         string formMoniker = doc.InnerXml;
    //         //We have to disable the Execute menuitem if the form is already opened.
    //         if (Microsoft.SqlServer.Management.SqlMgmt.RunningFormsTable.Table.FormExists(formMoniker))
    //         {
    //             ToolStripItem tsi = contextMenu.Items[5];
    //             tsi.Enabled = false;                
    //         }
    //     }
       

    }//end ToolMenuItemHelperClass

    /// <summary>
    /// Action Handler designed to launch Sql2005-style dialogs
    /// </summary>
    public class LaunchFormActionHandler
    {
        #region IActionHandler Members

        //public event EventHandler<ActionCompletedEventArgs> ActionCompleted;

        // public void PerformAction(string actionMoniker, Microsoft.SqlServer.Management.Data.IContext context)
        // {
        //     Cursor oldCursor = Cursor.Current;
        //     Cursor.Current = Cursors.WaitCursor;
        //     try
        //     {
        //         if (string.IsNullOrEmpty(actionMoniker))
        //         {
        //             throw new ArgumentNullException("actionMoniker");
        //         }

        //         if (context == null)
        //         {
        //             throw new ArgumentNullException("context");
        //         }


        //         SqlOlapConnectionInfoBase connection;
        //         if (!context.TryGetPropertyValue<SqlOlapConnectionInfoBase>(SqlStudioConstants.ConnectionInfo, out connection))
        //         {
        //             // connection-specific properties
        //             IManagedConnection managedconnection = Microsoft.SqlServer.Management.ServiceProvider.GetService<IManagedConnection2>(context);
        //             if (managedconnection == null)
        //             {
        //                 managedconnection = Microsoft.SqlServer.Management.ServiceProvider.GetService<IManagedConnection>(context, true);
        //             }

        //             if (managedconnection != null)
        //             {
        //                 connection = managedconnection.Connection;
                        
        //                 managedconnection.Dispose();
        //             }
        //         }

        //         XmlDocument doc = GenerateXmlDocument(context, connection);

        //         ToolMenuItemHelper.Launch(doc, connection, context);
        //     }
        //     catch (Exception ex)
        //     {
        //         Cursor.Current = oldCursor;
        //         SqlStudioMessageBox.Show(ex);
        //     }
        //     finally
        //     {
        //         Cursor.Current = oldCursor;

        //         if (ActionCompleted != null)
        //         {
        //             ActionCompleted(this, new ActionCompletedEventArgs(actionMoniker, context, context));
        //         }
        //     }
        //}
        

        public XmlDocument GenerateXmlDocument(IContext context, SqlOlapConnectionInfoBase connection)
        {
            XmlDocument document = new XmlDocument();
            document.AppendChild(document.CreateElement("formdescription"));

            XmlElement parameters = (XmlElement)document.DocumentElement.AppendChild(document.CreateElement("params"));

            // connection-specific properties
            if (connection != null)
            {
                parameters.AppendChild(document.CreateElement("servername")).InnerText = connection.ServerName;
                //parameters.AppendChild(document.CreateElement("connectionmoniker")).InnerText = ConnectionCache.GetConnectionKeyName(connection);
                parameters.AppendChild(document.CreateElement("servertype")).InnerText = "sql";
            }

            string itemType;
            if (!context.TryGetPropertyValue<string>("ItemType", out itemType))
            {
                itemType = string.Empty;
            }
            parameters.AppendChild(document.CreateElement("itemtype")).InnerText = itemType;

            string val;
            if (!context.TryGetPropertyValue<string>("Mode", out val))
            {
                val = string.Empty;
            }
            parameters.AppendChild(document.CreateElement("mode")).InnerText = val;

            string singleton;
            if (context.TryGetPropertyValue<string>("Singleton", out singleton))
            {
               // parameters.AppendChild(document.CreateElement(ToolMenuItemHelper.ReusableFormIndicator));    
            }

            // context portion
            string urn;
            IContext[] nodeContext;
            if (context.TryGetPropertyValue<IContext[]>(SqlStudioConstants.NodeContexts, out nodeContext))
            {
                for (int i = 0; i < nodeContext.Length; i++)
                {
                    if (nodeContext[i].TryGetPropertyValue<string>(SqlStudioConstants.UrnPath, out urn))
                    {
                        parameters.AppendChild(document.CreateElement("urn")).InnerText = urn;
                    }
                }
            }
            else if (context.TryGetPropertyValue<string>(SqlStudioConstants.UrnPath, out urn))
            {
                parameters.AppendChild(document.CreateElement("urn")).InnerText = urn;

                // split and add component
                foreach (KeyValuePair<string, string> item in ToolMenuItemHelper.ExtractUrnPart(itemType, new Urn(urn)))
                {
                    parameters.AppendChild(document.CreateElement(item.Key)).InnerText = item.Value;
                }
            }

            // we have to split full type name into parts
            // string objectTypeName;
            // if (context.TryGetPropertyValue<string>("Type", out objectTypeName))
            // {
            //     IFactoryService factoryService = Microsoft.SqlServer.Management.ServiceProvider.GetService<IFactoryService>(context, true);

            //     Type objectType = factoryService.GetType(objectTypeName);

            //     parameters.AppendChild(document.CreateElement("formtype")).InnerText = objectType.FullName;
            //     parameters.AppendChild(document.CreateElement("assemblyname")).InnerText = objectType.Assembly.ManifestModule.Name;
            // }
            
            return document;
        }


        #endregion
    }


}
