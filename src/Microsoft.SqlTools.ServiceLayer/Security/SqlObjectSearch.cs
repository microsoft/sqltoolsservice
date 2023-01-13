//------------------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using Microsoft.SqlServer.Management.Smo;

using System;
using System.Collections;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Microsoft.SqlServer.Management.SqlMgmt
{
    /// <summary>
    /// SQL Object Search dialog.
    /// </summary>
#if DEBUG || EXPOSE_MANAGED_INTERNALS
    public
#else
    internal
#endif
    class SqlObjectSearch : System.Windows.Forms.Form
    {
        private System.Windows.Forms.Label selectedObjectTypeLabel;
        private System.Windows.Forms.TextBox selectedObjectTypes;
        private System.Windows.Forms.Button selectObjectTypes;
        private Microsoft.SqlServer.Management.SqlMgmt.KomodoLinkLabel examplesLink;
        private System.Windows.Forms.Button browse;
        private System.Windows.Forms.TextBox objectNames;
        private System.Windows.Forms.Button checkNames;
        private System.Windows.Forms.Button ok;
        private System.Windows.Forms.Button cancel;
        private System.Windows.Forms.Button help;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        private IHelpProvider                   helpProvider            = null;

        private SearchableObjectCollection      searchResults           = null;
        private object                          connectionInfo          = null;
        private SearchableObjectTypeCollection  allowedTypes            = null;
        private SearchableObjectTypeCollection  selectedTypes           = null;
        private bool                            namesChecked            = true;
        private string                          databaseName            = String.Empty;
        private bool                            checkCancelled          = false;
        private bool[]                          includeSystemObjects    = new bool[]{true};
        private CompareOptions                  compareOptions          = CompareOptions.Ordinal;
        private SearchableObjectCollection      objectExclusionList     = new SearchableObjectCollection();

        /// <summary>
        /// Get the searchable object types selected in the dialog
        /// </summary>
        public SearchableObjectTypeCollection   SelectedTypes
        {
            get
            {
                return this.selectedTypes;
            }
        }

        /// <summary>
        /// The searchable objects that were found
        /// </summary>
        public SearchableObjectCollection       SearchResults
        {
            get
            {
                return this.searchResults;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="font">The font for the dialog, should be same as parent dialog's</param>
        /// <param name="icon">The icon for the dialog, should be same as parent dialog's</param>
        /// <param name="helpProvider">Provider for displaying help topics</param>
        /// <param name="title">The title for the dialog</param>
        /// <param name="connectionInfo">ConnectionInfo object used to access the database</param>
        /// <param name="databaseName">The relevant database's name</param>
        /// <param name="allowedTypes">The SQL object types that the user is allowed to search</param>
        /// <param name="selectedTypes">The initial set of types to search</param>
        /// <param name="includeSystemObjects">Whether system or built-in objects of allowed types should be included in search results</param>
        /// <param name="objectExclusionList">The specific objects that we don't want to show in Search Object Dialog</param>
        public SqlObjectSearch(
                              Font font,
                              Icon icon,
                              IHelpProvider helpProvider,
                              string title,
                              object connectionInfo,
                              string databaseName,
                              SearchableObjectTypeCollection allowedTypes,
                              SearchableObjectTypeCollection selectedTypes,
                              bool[] includeSystemObjects,
                              SearchableObjectCollection objectExclusionList)
        {
            this.Initialize(
                           font,
                           icon,
                           helpProvider,
                           connectionInfo,
                           databaseName,
                           allowedTypes,
                           selectedTypes,
                           includeSystemObjects);

            if ((title != null) && (title.Length != 0))
            {
                this.Text = title;
            }

            if (objectExclusionList != null)
            {
                this.objectExclusionList = objectExclusionList;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="font">The font for the dialog, should be same as parent dialog's</param>
        /// <param name="icon">The icon for the dialog, should be same as parent dialog's</param>
        /// <param name="helpProvider">Provider for displaying help topics</param>
        /// <param name="title">The title for the dialog</param>
        /// <param name="connectionInfo">ConnectionInfo object used to access the database</param>
        /// <param name="databaseName">The relevant database's name</param>
        /// <param name="allowedTypes">The SQL object types that the user is allowed to search</param>
        /// <param name="selectedTypes">The initial set of types to search</param>
        public SqlObjectSearch(
                              Font                            font,
                              Icon                            icon,
                              IHelpProvider                   helpProvider,
                              string                          title,
                              object                          connectionInfo,
                              string                          databaseName,
                              SearchableObjectTypeCollection  allowedTypes,
                              SearchableObjectTypeCollection  selectedTypes)
        {
            this.Initialize(
                           font,
                           icon,
                           helpProvider,
                           connectionInfo,
                           databaseName,
                           allowedTypes,
                           selectedTypes,
                           new bool[] { true }
                           );

            if ((title != null) && (title.Length != 0))
            {
                this.Text = title;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="font">The font for the dialog, should be same as parent dialog's</param>
        /// <param name="icon">The icon for the dialog, should be same as parent dialog's</param>
        /// <param name="helpProvider">Provider for displaying help topics</param>
        /// <param name="title">The title for the dialog</param>
        /// <param name="connectionInfo">ConnectionInfo object used to access the database</param>
        /// <param name="databaseName">The relevant database's name</param>
        /// <param name="allowedTypes">The SQL object types that the user is allowed to search</param>
        /// <param name="selectedTypes">The initial set of types to search</param>
        /// <param name="includeSystemObjects">Whether system or built-in objects of allowed types should be included in search results</param>
        public SqlObjectSearch(
                              Font font,
                              Icon icon,
                              IHelpProvider helpProvider,
                              string title,
                              object connectionInfo,
                              string databaseName,
                              SearchableObjectTypeCollection allowedTypes,
                              SearchableObjectTypeCollection selectedTypes,
                              bool[] includeSystemObjects)
        {
            this.Initialize(
                           font,
                           icon,
                           helpProvider,
                           connectionInfo,
                           databaseName,
                           allowedTypes,
                           selectedTypes,
                           includeSystemObjects);

            if ((title != null) && (title.Length != 0))
            {
                this.Text = title;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="font">The font for the dialog, should be same as parent dialog's</param>
        /// <param name="icon">The icon for the dialog, should be same as parent dialog's</param>
        /// <param name="helpProvider">Provider for displaying help topics</param>
        /// <param name="title">The title for the dialog</param>
        /// <param name="connectionInfo">ConnectionInfo object used to access the database</param>
        /// <param name="databaseName">The relevant database's name</param>
        /// <param name="allowedTypes">The SQL object types that the user is allowed to search</param>
        /// <param name="selectedTypes">The initial set of types to search</param>
        /// <param name="includeSystemObjects">Whether system or built-in objects should be included in search results</param>
        public SqlObjectSearch(
                              Font                            font,
                              Icon                            icon,
                              IHelpProvider                   helpProvider,
                              string                          title,
                              object                          connectionInfo,
                              string                          databaseName,
                              SearchableObjectTypeCollection  allowedTypes,
                              SearchableObjectTypeCollection  selectedTypes,
                              bool                            includeSystemObjects)
        {
            this.Initialize(
                           font,
                           icon,
                           helpProvider,
                           connectionInfo,
                           databaseName,
                           allowedTypes,
                           selectedTypes,
                           new bool[] { includeSystemObjects }
                           );

            if ((title != null) && (title.Length != 0))
            {
                this.Text = title;
            }
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="font">The font for the dialog, should be same as parent dialog's</param>
        /// <param name="icon">The icon for the dialog, should be same as parent dialog's</param>
        /// <param name="helpProvider">Provider for displaying help topics</param>
        /// <param name="connectionInfo">ConnectionInfo object used to access the database</param>
        /// <param name="databaseName">The relevant database's name, ignored if database is irrelevent</param>
        /// <param name="allowedTypes">The SQL object types that the user is allowed to search</param>
        /// <param name="selectedTypes">The initial set of types to search</param>
        public SqlObjectSearch(
                              Font                            font,
                              Icon                            icon,
                              IHelpProvider                   helpProvider,
                              object                          connectionInfo,
                              string                          databaseName,
                              SearchableObjectTypeCollection  allowedTypes,
                              SearchableObjectTypeCollection  selectedTypes)
        {
            this.Initialize(
                           font,
                           icon,
                           helpProvider,
                           connectionInfo,
                           databaseName,
                           allowedTypes,
                           selectedTypes,
                           new bool[] { true }
                           );
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="font">The font for the dialog, should be same as parent dialog's</param>
        /// <param name="icon">The icon for the dialog, should be same as parent dialog's</param>
        /// <param name="helpProvider">Provider for displaying help topics</param>
        /// <param name="connectionInfo">ConnectionInfo object used to access the database</param>
        /// <param name="databaseName">The relevant database's name, ignored if database is irrelevent</param>
        /// <param name="allowedTypes">The SQL object types that the user is allowed to search</param>
        /// <param name="selectedTypes">The initial set of types to search</param>
        /// <param name="includeSystemObjects">Whether system or built-in objects of allowed types should be included in search results</param>
        public SqlObjectSearch(
                              Font font,
                              Icon icon,
                              IHelpProvider helpProvider,
                              object connectionInfo,
                              string databaseName,
                              SearchableObjectTypeCollection allowedTypes,
                              SearchableObjectTypeCollection selectedTypes,
                              bool[] includeSystemObjects)
        {
            this.Initialize(
                           font,
                           icon,
                           helpProvider,
                           connectionInfo,
                           databaseName,
                           allowedTypes,
                           selectedTypes,
                           includeSystemObjects);
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="font">The font for the dialog, should be same as parent dialog's</param>
        /// <param name="icon">The icon for the dialog, should be same as parent dialog's</param>
        /// <param name="helpProvider">Provider for displaying help topics</param>
        /// <param name="connectionInfo">ConnectionInfo object used to access the database</param>
        /// <param name="databaseName">The relevant database's name, ignored if database is irrelevent</param>
        /// <param name="allowedTypes">The SQL object types that the user is allowed to search</param>
        /// <param name="selectedTypes">The initial set of types to search</param>
        /// <param name="includeSystemObjects">Whether system or built-in objects should be included in search results</param>
        public SqlObjectSearch(
                              Font                            font,
                              Icon                            icon,
                              IHelpProvider                   helpProvider,
                              object                          connectionInfo,
                              string                          databaseName,
                              SearchableObjectTypeCollection  allowedTypes,
                              SearchableObjectTypeCollection  selectedTypes,
                              bool                            includeSystemObjects)
        {
            this.Initialize(
                           font,
                           icon,
                           helpProvider,
                           connectionInfo,
                           databaseName,
                           allowedTypes,
                           selectedTypes,
                           new bool[] { includeSystemObjects }
                           );
        }

        /// <summary>
        /// Shared initialization code
        /// </summary>
        /// <param name="font">The font for the dialog, should be same as parent dialog's</param>
        /// <param name="icon">The icon for the dialog, should be same as parent dialog's</param>
        /// <param name="helpProvider">Provider for displaying help topics</param>
        /// <param name="connectionInfo">ConnectionInfo object used to access the database</param>
        /// <param name="databaseName">The relevant database's name, ignored if database is irrelevent</param>
        /// <param name="allowedTypes">The SQL object types that the user is allowed to search</param>
        /// <param name="selectedTypes">The initial set of types to search</param>
        private void Initialize(
                               Font                            font,
                               Icon                            icon,
                               IHelpProvider                   helpProvider,
                               object                          connectionInfo,
                               string                          databaseName,
                               SearchableObjectTypeCollection  allowedTypes,
                               SearchableObjectTypeCollection  selectedTypes,
                               bool[]                          includeSystemObjects)
        {
            InitializeComponent();

            this.Font                   = font;
            this.helpProvider           = helpProvider;
            this.connectionInfo         = connectionInfo;
            this.allowedTypes           = allowedTypes;
            this.selectedTypes          = selectedTypes;
            this.databaseName           = databaseName;
            this.includeSystemObjects   = includeSystemObjects;

            if (icon != null)
            {
                this.Icon = icon;
            }
            else
            {
                this.Icon = ResourceUtils.LoadIcon("browse.ico");
            }

            string collation    = SearchableObject.GetSqlCollation(this.connectionInfo, this.databaseName);
            this.compareOptions = SqlSupport.GetCompareOptionsFromCollation(collation);

            var helpProvider2 = this.helpProvider as IHelpProvider2;
            if (helpProvider2 != null && !helpProvider2.ShouldDisplayHelp)
            {
                this.Controls.Remove(this.help);
            }
            else
            {
                this.help.Enabled = (this.helpProvider != null);
            }

            this.SetSelectedObjectTypeText();
            this.EnableControls();
            StartPosition = FormStartPosition.CenterParent;
        }

        /// <summary>
        /// Enable and disable controls on the dialog based on dialog state
        /// </summary>
        private void                EnableControls()
        {
            bool typesSelected      = (this.selectedTypes.Count != 0);
            bool namesEntered       = (0 < this.objectNames.Text.Length);
            bool readyToCheck       = typesSelected && namesEntered;

            this.checkNames.Enabled         = (readyToCheck && !this.namesChecked);
            this.ok.Enabled                 = readyToCheck;
            this.browse.Enabled             = typesSelected;
            this.objectNames.Enabled        = typesSelected;
            this.help.Enabled               = (this.helpProvider != null);
        }

        /// <summary>
        /// Set the text of the selected object type textbox
        /// </summary>
        private void                SetSelectedObjectTypeText()
        {
            if (this.SelectedTypes.Count != 0)
            {
                string text = SearchableObjectTypeDescription.GetDescription(this.selectedTypes[0]).DisplayTypeNamePlural;

                if (this.selectedTypes.Count!= 1)
                {
                    int index = 1;

                    while (index < this.selectedTypes.Count)
                    {
                        text = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                             "{0}, {1}",
                                             text,
                                             SearchableObjectTypeDescription.GetDescription(this.selectedTypes[index]).DisplayTypeNamePlural);

                        ++index;
                    }
                }

                this.selectedObjectTypes.Text = text;
            }
        }

        /// <summary>
        /// Handle text changed events on the object names textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void                OnObjectNamesChanged(object sender, System.EventArgs e)
        {
            this.namesChecked = false;
            this.EnableControls();
        }

        /// <summary>
        /// Handle click events on the select object types button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void                OnSelectObjectTypes(object sender, System.EventArgs e)
        {
            using (SqlObjectSearchSelectTypes dlg = new SqlObjectSearchSelectTypes(this.Font,
                                                                                   this.Icon,
                                                                                   this.helpProvider,
                                                                                   this.allowedTypes,
                                                                                   this.selectedTypes))
            {
                dlg.ShowDialog(this);

                if (DialogResult.OK == dlg.DialogResult)
                {
                    this.selectedTypes = dlg.SelectedTypes;
                }
            }



            this.SetSelectedObjectTypeText();
            this.namesChecked = false;
            this.EnableControls();
        }

        /// <summary>
        /// Handle click events on the check names button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void                OnCheckNames(object sender, System.EventArgs e)
        {
            System.Diagnostics.Debug.Assert(!this.namesChecked, "names didn't need checking, how did this get called?");
            this.CheckNames();
            this.EnableControls();
        }

        /// <summary>
        /// Check the names in the names text box
        /// </summary>
        /// <returns>True if all the names were valid, false otherwise</returns>
        private bool                CheckNames()
        {
            if (!this.namesChecked)
            {
                this.checkCancelled                 = false;
                StringBuilder   objectNamesBuilder  = new StringBuilder();
                bool            isFirstName         = true;
                ArrayList       resolvedNames       = new ArrayList();
                string          unresolvedNames     = String.Empty;

                this.ResolveNames(this.objectNames.Text, resolvedNames, ref unresolvedNames);

                foreach (ObjectName objectName in resolvedNames)
                {
                    if (isFirstName)
                    {
                        isFirstName = false;
                    }
                    else
                    {
                        objectNamesBuilder.Append("; ");
                    }

                    objectNamesBuilder.Append(objectName.ToString());
                }

                if (unresolvedNames.Length != 0)
                {
                    if (!isFirstName)
                    {
                        objectNamesBuilder.Append("; ");
                    }

                    objectNamesBuilder.Append(unresolvedNames);
                }

                // setting the objectNames.Text property sets namesChecked to false, which we don't want
                bool chk                = this.namesChecked;
                this.objectNames.Text   = objectNamesBuilder.ToString();
                this.namesChecked       = chk;
            }

            return this.namesChecked;
        }

        /// <summary>
        /// Check names and replace non-names with what the user meant.
        /// </summary>
        /// <remarks>
        /// Converts a single string of semicolon delimited names into a collection of
        /// individual names.  As a side effect, populates the search result collection.
        /// If all names in the result are valid object names, the namesChecked flag is set
        /// to true.
        /// </remarks>
        /// <param name="names">A semicolon delimited list of object names or name fragments</param>
        /// <returns>A collection of object names</returns>
        private void                ResolveNames(string names, ArrayList resolvedNames, ref string unresolvedNames)
        {
            this.searchResults              = new SearchableObjectCollection();

            ObjectName  objectName          = null;
            int         previousStartIndex  = 0;
            int         startIndex          = 0;
            this.checkCancelled             = false;

            while (
                  !this.checkCancelled        &&
                  (startIndex < names.Length) &&
                  SearchableObject.GetNextName(names, ref startIndex, this.compareOptions, out objectName))
            {
                bool nameResolved = false;

                while (!nameResolved && !this.checkCancelled)
                {
                    // search for the object name
                    SearchableObjectCollection sqlObjects = this.FindSqlObjects(objectName);

                    // try to resolve the names returned
                    nameResolved =
                        this.MatchSingleObjectExactly(sqlObjects, objectName, resolvedNames) ||
                        this.MatchFoundObjects(sqlObjects, objectName, resolvedNames);

                    // if the name wasn't resolved and the check wasn't cancelled,
                    // ask the user to correct the name
                    if (!nameResolved && !this.checkCancelled)
                    {
                        ObjectName  newName                     = null;
                        this.AskUserForNewName(objectName, out newName);

                        // if the user gave us a new name to look for, go look for it
                        if (newName != null)
                        {
                            objectName = newName;
                        }
                        // if the user gave us no name, consider the name resolved
                        else
                        {
                            nameResolved = true;
                        }
                    }

                    // if the check was cancelled, set the unresolved names string
                    // to the names we didn't resolve, which starts at the beginning
                    // of the name we gave up on checking
                    if (this.checkCancelled)
                    {
                        unresolvedNames = names.Substring(previousStartIndex);
                    }
                }

                // remember where we are starting the next name
                previousStartIndex = startIndex;
            }

            this.namesChecked = !this.checkCancelled;
        }

        /// <summary>
        /// If exactly one sql object matches the searched for name exactly,
        /// put the sql object in the search results and the objects's name
        /// in the resolved names list
        /// </summary>
        /// <param name="sqlObjects">The results of the search</param>
        /// <param name="objectNameSearchedFor">The name of the object we were looking for</param>
        /// <param name="resolvedNames">The list of resolved names</param>
        /// <returns>True if exactly one name was exactly matched, false otherwise</returns>
        private bool                MatchSingleObjectExactly(
                                                            SearchableObjectCollection  sqlObjects,
                                                            ObjectName                  objectNameSearchedFor,
                                                            ArrayList                   resolvedNames)
        {
            bool result = false;

            // if we got exactly one result and it has exactly the same multi-part
            // name as the input object name, just add it to the resolvedNames collection
            // and move on
            if (sqlObjects.Count == 1)
            {
                SearchableObject    foundObject = sqlObjects[0];
                ObjectName          foundName   = new ObjectName(foundObject);

                if (objectNameSearchedFor.IsSameAs(foundName))
                {
                    if (!searchResults.Contains(foundObject))
                    {
                        this.searchResults.Add(foundObject);
                        resolvedNames.Add(foundName);
                    }

                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// If multiple and/or inexactly matched objects were found in the search,
        /// ask the user which should be added to the found objects list, add them,
        /// and add the selected objects names to the resolved names list.
        /// </summary>
        /// <param name="sqlObjects">The results of the search</param>
        /// <param name="objectNameSearchedFor">The name of the object we were looking for</param>
        /// <param name="resolvedNames">The list of resolved names</param>
        /// <returns>True if any objects were selected by the user, false otherwise</returns>
        private bool                MatchFoundObjects(
                                                     SearchableObjectCollection  sqlObjects,
                                                     ObjectName                  objectNameSearchedFor,
                                                     ArrayList                   resolvedNames)
        {
            bool result = false;

            // We didn't get a single exact match.  It we got any results at all,
            // ask the user which of the results they meant.
            if (sqlObjects.Count != 0)
            {
                using (SqlObjectSearchMatches dlg = new SqlObjectSearchMatches(this.Font,
                                                                               this.Icon,
                                                                               this.helpProvider,
                                                                               objectNameSearchedFor,
                                                                               sqlObjects))
                {
                    dlg.ShowDialog(this);

                    if (DialogResult.OK == dlg.DialogResult)
                    {
                        foreach (SearchableObject obj in dlg.SelectedObjects)
                        {
                            if (!this.searchResults.Contains(obj))
                            {
                                this.searchResults.Add(obj);
                                resolvedNames.Add(new ObjectName(obj));
                            }
                        }

                        result = true;
                    }
                    else
                    {
                        this.checkCancelled = true;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// No object was found with the input name, ask the user for a new name to look for
        /// </summary>
        /// <param name="objectNameSearchedFor">The object name we tried to find</param>
        /// <returns>True if the user gave us a new name or chose to remove the name, false if the user cancelled</returns>
        private bool            AskUserForNewName(ObjectName objectNameSearchedFor, out ObjectName newName)
        {
            bool result = false;
            newName     = null;

            // no object was found, let the user correct the name
            using (SqlObjectSearchNameNotFound dlg = new SqlObjectSearchNameNotFound(this.Font,
                                                                                     this.Icon,
                                                                                     this.helpProvider,
                                                                                     objectNameSearchedFor.ToString(),
                                                                                     this.allowedTypes,
                                                                                     this.selectedTypes))

            {
                dlg.ShowDialog(this);

                if (DialogResult.OK == dlg.DialogResult)
                {
                    if (dlg.TryAgain)
                    {
                        int     startIndex  = 0;
                        bool    gotName     = SearchableObject.GetNextName(dlg.NewName, ref startIndex, this.compareOptions, out newName);
                        System.Diagnostics.Debug.Assert(gotName, "dialog claimed to have a new object name, but no name was found!");
                    }

                    result = true;
                }
                else
                {
                    this.checkCancelled = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Find one (or more) SQL objects identified by name
        /// </summary>
        /// <remarks>
        /// First we try to find a match for objects named exactly as specified.  If that
        /// fails and domain name capitalization might have been a factor, we try to find a match for
        /// a single object named like a canonicalized equivalent.  If that fails, we find all objects
        /// with names containing the "name" substring and let the user choose one or more of the result set.
        /// If that didn't work, we finally try to find all objects with names containing a canonical
        /// equivalent of the name.
        /// </remarks>
        /// <param name="name">The name of the object(s) we are looking for</param>
        /// <returns>A collection of matching objects</returns>
        private SearchableObjectCollection  FindSqlObjects(ObjectName objectName)
        {
            SearchableObjectCollection  result = null;

            // try exact match as written
            result = this.FindSqlObjectExact(objectName);

            if ((0 == result.Count) && !this.checkCancelled)
            {
                // End-users often type in NT domain names using lower case letters, like "redmond\myuser,"
                // but domain names have to be in all-caps, like "REDMOND\myuser."  If name canonicalization
                // (capitalizing what appears to be a domain) might affect the search, get the canonicalized
                // name equivalent.  This should only affect User names because they are often based on Login
                // names; \ characters are not significant for other object types.

                bool        canBeUser                       = this.selectedTypes.Contains(SearchableObjectType.User) || this.selectedTypes.Contains(SearchableObjectType.Login);
                ObjectName  canonicalizedName               = new ObjectName(objectName.Schema, (canBeUser ? this.Canonicalize(objectName.Name) : objectName.Name), this.compareOptions);
                bool        canonicalizedNameIsDifferent    = canBeUser && (canonicalizedName.Name != objectName.Name);

                // next, check canonicalized equivalent for exact match
                if (canonicalizedNameIsDifferent)
                {
                    result = this.FindSqlObjectExact(canonicalizedName);
                }

                // next, check whether there are any objects with names "like" name
                if ((0 == result.Count) && !this.checkCancelled)
                {
                    result = this.FindSqlObjectsLike(objectName);
                }

                // next, check whether there are any objects with names "like" the canonicalized name
                if ((0 == result.Count) && !this.checkCancelled && canonicalizedNameIsDifferent)
                {
                    result = this.FindSqlObjectsLike(canonicalizedName);
                }
            }


            return result;
        }

        /// <summary>
        /// Find the objects whose names exactly match the input identifier
        /// </summary>
        /// <param name="identifier">The multi-part name of the object to find</param>
        /// <returns>Collection containing the object if found, or an empty collection if not found</returns>
        private SearchableObjectCollection  FindSqlObjectExact(ObjectName identifier)
        {
            SearchableObjectCollection result = new SearchableObjectCollection();

            if (identifier.Name.Length != 0)
            {
                foreach (SearchableObjectType type in this.selectedTypes)
                {
                    SearchableObjectTypeDescription typeDescription = SearchableObjectTypeDescription.GetDescription(type);

                    bool incSystemObjects = this.IncludeSystemObjectsForType(type);

                    if (typeDescription.IsDatabaseObject)
                    {
                        System.Diagnostics.Debug.Assert(
                            !type.GetMultiSearchObjectSecurableInfo(serverVersion: null, synthetizeInfoWhenNotFound: false).Any(),
                            "MultiSearchObject NYI for database objects. Add necessary code, similarly to the non-Database objects!");
                        System.Diagnostics.Debug.Assert(
                                     ((this.databaseName != null) && (this.databaseName.Length != 0)),
                                     "database name not defined, but we're looking for database objects");

                        if (typeDescription.IsSchemaObject)
                        {
                            if (identifier.Schema.Length != 0)
                            {
                                SearchableObject.Search(
                                                       result,
                                                       type,
                                                       this.connectionInfo,
                                                       this.databaseName,
                                                       identifier.Name,
                                                       true,
                                                       identifier.Schema,
                                                       true,
                                                       incSystemObjects);
                            }
                        }
                        else
                        {
                            SearchableObject.Search(
                                                   result,
                                                   type,
                                                   this.connectionInfo,
                                                   this.databaseName,
                                                   identifier.Name,
                                                   true,
                                                   incSystemObjects);
                        }
                    }
                    else
                    {
                        // Seach all the SearchableObjectTypes that are applicable to this server version
                        // We iterate over the the possible SearchableObjectTypes to account for Securable Objects that may
                        // may to multiple
                        foreach (var searchableObjectType in type.GetMultiSearchObjectSecurableInfo().Select(x => x.SearchableObjectType))
                        { 
                            SearchableObject.Search(
                                                   result,
                                                   searchableObjectType,
                                                   this.connectionInfo,
                                                   identifier.Name,
                                                   true,
                                                   incSystemObjects);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Find SQL objects with names containing the input "name"
        /// </summary>
        /// <param name="identifier">The multi-part name that SQL objects' names should contain</param>
        /// <returns>A collection of matching objects</returns>
        private SearchableObjectCollection  FindSqlObjectsLike(ObjectName identifier)
        {
            SearchableObjectCollection result = new SearchableObjectCollection();

            foreach (SearchableObjectType type in this.selectedTypes)
            {
                SearchableObjectTypeDescription typeDescription = SearchableObjectTypeDescription.GetDescription(type);

                bool incSystemObjects = this.IncludeSystemObjectsForType(type);

                if (typeDescription.IsDatabaseObject)
                {
                    System.Diagnostics.Debug.Assert(
                        !type.GetMultiSearchObjectSecurableInfo(serverVersion: null, synthetizeInfoWhenNotFound: false).Any(),
                        "MultiSearchObject NYI for database objects. Add necessary code, similarly to the non-Database objects!");

                    System.Diagnostics.Debug.Assert(
                                 ((this.databaseName != null) && (this.databaseName.Length != 0)),
                                 "database name not defined, but we're looking for database objects");

                    if (typeDescription.IsSchemaObject)
                    {
                        SearchableObject.Search(
                                               result,
                                               type,
                                               this.connectionInfo,
                                               this.databaseName,
                                               identifier.Name,
                                               false,
                                               identifier.Schema,
                                               false,
                                               incSystemObjects);
                    }
                    else
                    {
                        SearchableObject.Search(
                                               result,
                                               type,
                                               this.connectionInfo,
                                               this.databaseName,
                                               identifier.Name,
                                               false,
                                               incSystemObjects);
                    }
                }
                else
                {
                    // Seach all the SearchableObjectTypes that are applicable to this server version
                    // We iterate over the the possible SearchableObjectTypes to account for Securable Objects that may
                    // may to multiple
                    foreach (var searchableObjectType in type.GetMultiSearchObjectSecurableInfo().Select(x => x.SearchableObjectType))
                    {
                        SearchableObject.Search(
                                               result,
                                               searchableObjectType,
                                               this.connectionInfo,
                                               identifier.Name,
                                               false,
                                               incSystemObjects);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Convert the domain portion of the input login or user name to capital letters
        /// </summary>
        /// <param name="loginName">The login or user name to convert</param>
        /// <returns>The canonical equivalent login name</returns>
        private string                      Canonicalize(string loginName)
        {
            int     lastBackslash   = loginName.LastIndexOf('\\');
            string  result          = String.Empty;

            if (-1 == lastBackslash)
            {
                result = loginName;
            }
            else
            {
                string domain   = loginName.Substring(0, lastBackslash).ToUpper(System.Globalization.CultureInfo.InvariantCulture);
                string rest     = loginName.Substring(lastBackslash);

                result          = String.Concat(domain, rest);
            }

            return result;
        }

        /// <summary>
        /// Finds if system objects to be included for a SearchableObjectType or not.
        /// </summary>
        /// <param name="type">SearchableObjectType</param>
        /// <returns>are system objects included</returns>
        private bool IncludeSystemObjectsForType(SearchableObjectType type)
        {
            if (this.includeSystemObjects.Length <= 1)
            {
                return this.includeSystemObjects[0];
            }

            bool incSystemObjects = true;
            for (int i = 0; i < this.allowedTypes.Count; i++)
            {
                if (this.allowedTypes[i] == type)
                {
                    incSystemObjects = this.includeSystemObjects[i];
                    break;
                }
            }
            return incSystemObjects;
        }


        /// <summary>
        /// Handled click events on the OK button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnOk(object sender, System.EventArgs e)
        {
            if ((this.namesChecked || this.CheckNames()) && (0 < searchResults.Count))
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                this.EnableControls();
            }
        }

        /// <summary>
        /// Handle click events on the cancel button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void            OnCancel(object sender, System.EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        /// <summary>
        /// Handle click events on the help button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void            OnHelp(object sender, System.EventArgs e)
        {
            if(this.helpProvider != null)
            {
                this.helpProvider.DisplayTopicFromF1Keyword(AssemblyVersionInfo.VersionHelpKeywordPrefix + ".swb.common.selectobjects.f1");
            }
        }

        /// <summary>
        /// Handle click events on the browse button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void            OnBrowse(object sender, System.EventArgs e)
        {
            bool[] incSystemObjectsForSelectedTypes = new bool[this.selectedTypes.Count];
            for (int i = 0; i < this.selectedTypes.Count; i++)
            {
                incSystemObjectsForSelectedTypes[i] = this.IncludeSystemObjectsForType(this.selectedTypes[i]);
            }

            using (SqlObjectSearchBrowser dlg = new SqlObjectSearchBrowser(this.Font,
                                                                           this.Icon,
                                                                           this.helpProvider,
                                                                           this.selectedTypes,
                                                                           this.connectionInfo,
                                                                           this.databaseName,
                                                                           incSystemObjectsForSelectedTypes,
                                                                           this.objectExclusionList))

            {
                dlg.ShowDialog(this);

                // if ok was clicked, add the selected objects names to the object names box
                // and to the search results
                if (DialogResult.OK == dlg.DialogResult)
                {
                    if (null == this.searchResults)
                    {
                        this.searchResults = new SearchableObjectCollection();
                    }

                    StringBuilder textBuilder = new StringBuilder(this.objectNames.Text);
                    SearchableObjectCollection selectedObjects = dlg.SelectedObjects;

                    bool        prependSemicolon    = (this.objectNames.Text.Length != 0);
                    IEnumerator enumerator          = selectedObjects.GetEnumerator();
                    enumerator.Reset();

                    while (enumerator.MoveNext())
                    {
                        SearchableObject foundObject = (SearchableObject) enumerator.Current;

                        if (prependSemicolon)
                        {
                            textBuilder.Append("; ");
                        }
                        else
                        {
                            prependSemicolon = true;
                        }

                        textBuilder.Append(foundObject.ToString());
                        this.searchResults.Add(foundObject);
                    }

                    // Set the objectName text.
                    bool namesWereChecked   = this.namesChecked;
                    this.objectNames.Text   = textBuilder.ToString();
                    this.namesChecked       = namesWereChecked && (selectedObjects.Count == 0);
                }
            }

            this.EnableControls();
        }

        /// <summary>
        /// Handle click events on the example link
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void            OnExample(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
        {
            SqlObjectSearchExample example = new SqlObjectSearchExample(this.Font);
            example.Location = new Point(
                                        this.Location.X + this.examplesLink.Location.X,
                                        this.Location.Y + this.examplesLink.Location.Y);

            example.Show();
        }


        /// <summary>
        /// Handle F1 help
        /// </summary>
        /// <param name="hevent"></param>
        protected override void OnHelpRequested(HelpEventArgs hevent)
        {
            base.OnHelpRequested (hevent);

            hevent.Handled  = true;

            if (this.helpProvider != null)
            {
                this.helpProvider.DisplayTopicFromF1Keyword(AssemblyVersionInfo.VersionHelpKeywordPrefix + ".swb.common.selectobjects.f1");
            }
        }

        private delegate void SimpleMethod();

        /// <summary>
        /// Perform specialized load-time tasks
        /// </summary>
        /// <param name="e"></param>
        protected override void  OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            this.BeginInvoke(new SimpleMethod(this.FocusObjectNames));
        }

        /// <summary>
        /// Set focus to the objectNames text box
        /// </summary>
        private void FocusObjectNames()
        {
            this.objectNames.Focus();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
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

#region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SqlObjectSearch));
            this.selectedObjectTypeLabel = new System.Windows.Forms.Label();
            this.selectedObjectTypes = new System.Windows.Forms.TextBox();
            this.selectObjectTypes = new System.Windows.Forms.Button();
            this.checkNames = new System.Windows.Forms.Button();
            this.ok = new System.Windows.Forms.Button();
            this.cancel = new System.Windows.Forms.Button();
            this.help = new System.Windows.Forms.Button();
            this.objectNames = new System.Windows.Forms.TextBox();
            this.examplesLink = new Microsoft.SqlServer.Management.SqlMgmt.KomodoLinkLabel();
            this.browse = new System.Windows.Forms.Button();
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            //
            // selectedObjectTypeLabel
            //
            resources.ApplyResources(this.selectedObjectTypeLabel, "selectedObjectTypeLabel");
            this.selectedObjectTypeLabel.Name = "selectedObjectTypeLabel";
            //
            // selectedObjectTypes
            //
            resources.ApplyResources(this.selectedObjectTypes, "selectedObjectTypes");
            this.selectedObjectTypes.Name = "selectedObjectTypes";
            this.selectedObjectTypes.ReadOnly = true;
            //
            // selectObjectTypes
            //
            resources.ApplyResources(this.selectObjectTypes, "selectObjectTypes");
            this.selectObjectTypes.Name = "selectObjectTypes";
            this.selectObjectTypes.Click += new System.EventHandler(this.OnSelectObjectTypes);
            //
            // checkNames
            //
            resources.ApplyResources(this.checkNames, "checkNames");
            this.checkNames.Name = "checkNames";
            this.checkNames.Click += new System.EventHandler(this.OnCheckNames);
            //
            // ok
            //
            resources.ApplyResources(this.ok, "ok");
            this.ok.Name = "ok";
            this.ok.Click += new System.EventHandler(this.OnOk);
            //
            // cancel
            //
            resources.ApplyResources(this.cancel, "cancel");
            this.cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancel.Name = "cancel";
            this.cancel.Click += new System.EventHandler(this.OnCancel);
            //
            // help
            //
            resources.ApplyResources(this.help, "help");
            this.help.Name = "help";
            this.help.Click += new System.EventHandler(this.OnHelp);
            //
            // objectNames
            //
            resources.ApplyResources(this.objectNames, "objectNames");
            this.objectNames.Name = "objectNames";
            this.objectNames.TextChanged += new System.EventHandler(this.OnObjectNamesChanged);
            //
            // examplesLink
            //
            resources.ApplyResources(this.examplesLink, "examplesLink");
            this.examplesLink.Name = "examplesLink";
            this.examplesLink.TabStop = true;
            this.examplesLink.UseCompatibleTextRendering = true;
            this.examplesLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnExample);
            //
            // browse
            //
            resources.ApplyResources(this.browse, "browse");
            this.browse.Name = "browse";
            this.browse.Click += new System.EventHandler(this.OnBrowse);
            //
            // SqlObjectSearch
            // Important: For narrator accessibility please make sure all Controls.Add are in order from top to bottom, left to right
            //
            this.AcceptButton = this.ok;
            resources.ApplyResources(this, "$this");
            this.CancelButton = this.cancel;
            this.Controls.Add(this.selectedObjectTypeLabel);
            this.Controls.Add(this.selectedObjectTypes);
            this.Controls.Add(this.selectObjectTypes);
            this.Controls.Add(this.examplesLink);
            this.Controls.Add(this.objectNames);
            this.Controls.Add(this.checkNames);
            this.Controls.Add(this.browse);
            this.Controls.Add(this.ok);
            this.Controls.Add(this.cancel);
            this.Controls.Add(this.help);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SqlObjectSearch";
            this.ResumeLayout(false);
            this.PerformLayout();

        }
#endregion

    }
}
