//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.Management;

namespace Microsoft.SqlTools.ServiceLayer.Utility
{
    /// <summary>
    /// Utility functions for working with server languages
    /// </summary>
    internal class LanguageUtils
    {
        /// <summary>
        /// Gets alias for a language name.
        /// </summary>
        /// <param name="connectedServer"></param>
        /// <param name="languageName"></param>
        /// <returns>Returns string.Empty in case it doesn't find a matching languageName on the server</returns>
        public static string GetLanguageAliasFromName(Server connectedServer, string languageName)
        {
            string languageAlias = string.Empty;

            SetLanguageDefaultInitFieldsForDefaultLanguages(connectedServer);

            foreach (Language lang in connectedServer.Languages)
            {
                if (lang.Name == languageName)
                {
                    languageAlias = lang.Alias;
                    break;
                }
            }

            return languageAlias;
        }        

        /// <summary>
        /// Gets name for a language alias.
        /// </summary>
        /// <param name="connectedServer"></param>
        /// <param name="languageAlias"></param>
        /// <returns>Returns string.Empty in case it doesn't find a matching languageAlias on the server</returns>
        public static string GetLanguageNameFromAlias(Server connectedServer, string languageAlias)
        {
            string languageName = string.Empty;

            SetLanguageDefaultInitFieldsForDefaultLanguages(connectedServer);

            foreach (Language lang in connectedServer.Languages)
            {
                if (lang.Alias == languageAlias)
                {
                    languageName = lang.Name;
                    break;
                }
            }

            return languageName;
        }

        /// <summary>
        /// Gets lcid for a languageId.
        /// </summary>
        /// <param name="connectedServer"></param>
        /// <param name="languageAlias"></param>
        /// <returns>Throws exception in case it doesn't find a matching languageId on the server</returns>
        public static int GetLcidFromLangId(Server connectedServer, int langId)
        {
            int lcid = -1; //Unacceptable Lcid.            

            SetLanguageDefaultInitFieldsForDefaultLanguages(connectedServer);

            foreach (Language lang in connectedServer.Languages)
            {
                if (lang.LangID == langId)
                {
                    lcid = lang.LocaleID;
                    break;
                }
            }

            if (lcid == -1) //Ideally this will never happen.
            {
                throw new ArgumentOutOfRangeException("langId", "This language id is not present in sys.syslanguages catalog.");
            }

            return lcid;
        }

        /// <summary>
        /// Gets languageId for a lcid.
        /// </summary>
        /// <param name="connectedServer"></param>
        /// <param name="languageAlias"></param>
        /// <returns>Throws exception in case it doesn't find a matching lcid on the server</returns>
        public static int GetLangIdFromLcid(Server connectedServer, int lcid)
        {
            int langId = -1; //Unacceptable LangId.            

            SetLanguageDefaultInitFieldsForDefaultLanguages(connectedServer);

            foreach (Language lang in connectedServer.Languages)
            {
                if (lang.LocaleID == lcid)
                {
                    langId = lang.LangID;
                    break;
                }
            }

            if (langId == -1) //Ideally this will never happen.
            {
                throw new ArgumentOutOfRangeException("lcid", "This locale id is not present in sys.syslanguages catalog.");
            }

            return langId;
        }

        /// <summary>
        /// returns a language choice alias for that language
        /// </summary>
        /// <param name="langid"></param>
        /// <returns></returns>
        public static LanguageChoice GetLanguageChoiceAlias(Server connectedServer, int lcid)
        {
            SetLanguageDefaultInitFieldsForDefaultLanguages(connectedServer);

            foreach (Language smoL in connectedServer.Languages)
            {
                if (smoL.LocaleID == lcid)
                {
                    string alias = smoL.Alias;
                    return new LanguageChoice(alias, lcid);
                }
            }
            return new LanguageChoice(String.Empty, lcid);
        }

        /// <summary>
        /// Sets exhaustive fields required for displaying and working with default languages in server, 
        /// database and user dialogs as default init fields so that queries are not sent again and again.
        /// </summary>
        /// <param name="connectedServer">server on which languages will be enumerated</param>
        public static void SetLanguageDefaultInitFieldsForDefaultLanguages(Server? connectedServer)
        {
            string[] fieldsNeeded = new string[] { "Alias", "Name", "LocaleID", "LangID" };
            connectedServer.SetDefaultInitFields(typeof(Language), fieldsNeeded);
        }

        public static IList<LanguageDisplay> GetDefaultLanguageOptions(CDataContainer dataContainer)
        {
            // sort the languages alphabetically by alias
            SortedList sortedLanguages = new SortedList(Comparer.Default);

            LanguageUtils.SetLanguageDefaultInitFieldsForDefaultLanguages(dataContainer.Server);
            if (dataContainer.Server != null && dataContainer.Server.Languages != null)
            {
                foreach (Language language in dataContainer.Server.Languages)
                {
                    LanguageDisplay listValue = new LanguageDisplay(language);
                    sortedLanguages.Add(language.Alias, listValue);
                }
            }

            IList<LanguageDisplay> res = new List<LanguageDisplay>();
            foreach (LanguageDisplay ld in sortedLanguages.Values)
            {
                res.Add(ld);
            }

            return res;
        }

        public static string GetLanguageAliasFromDisplayText(string? displayText)
        {
            string[] parts = displayText?.Split(" - ");
            return (parts != null && parts.Length > 1) ? parts[0] : displayText;            
        }

        public static bool IsDefaultLanguageSupported(Server server)
        {
            //Default Language was not supported before Denali.
            return SqlMgmtUtils.IsSql11OrLater(server.ConnectionContext.ServerVersion)
                && server.ServerType != DatabaseEngineType.SqlAzureDatabase;
        }

        public static string FormatLanguageDisplay(LanguageDisplay? l)
        {
            if (l == null) 
            {
                return null;
            }
            return string.Format("{0} - {1}", l.Language.Alias, l.Language.Name);
        }
    }

    internal class LanguageChoice
    {
        public string alias;
        public System.Int32 lcid;
        public LanguageChoice(string alias, System.Int32 lcid)
        {
            this.alias = alias;
            this.lcid = lcid;
        }

        public override string ToString()
        {
            return alias;
        }
    }
}
