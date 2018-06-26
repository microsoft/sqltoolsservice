//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// Localizable Job category. SMO just reads the string names of 
    /// job categories from msdb.dbo.sysjobcategories, which is not localized.
    /// To show localized strings in the UI we have to convert it ourselves. We will
    /// use this object to do that.
    /// </summary>
    internal class LocalizableCategory
    {
        string categoryName = String.Empty;
        JobCategory category = null;

        public LocalizableCategory(JobCategory category)
        {
            if (category == null)
            {
                throw new ArgumentNullException("category");
            }

            this.category = category;
            categoryName = LookupLocalizableName(category);
        }

        public LocalizableCategory(int categoryId, string defaultName)
        {
            this.category = null;
            categoryName = LookupLocalizableName(categoryId, defaultName);
        }

        public string Name { get { return this.categoryName; } }
        public JobCategory SmoCategory { get { return this.category; } }

        public override string ToString()
        {
            return this.categoryName;
        }

        private static string LookupLocalizableName(int categoryId, string defaultName)
        {
            string localisableCategory;
            switch (categoryId)
            {
                case 0:
                    localisableCategory = SR.CategoryLocal;
                    break;
                case 1:
                    localisableCategory = SR.CategoryFromMsx;
                    break;
                case 2:
                    localisableCategory = SR.CategoryMultiServer;
                    break;
                case 3:
                    localisableCategory = SR.CategoryDBMaint;
                    break;
                case 4:
                    localisableCategory = SR.CategoryWebAssistant;
                    break;
                case 5:
                    localisableCategory = SR.CategoryFullText;
                    break;
                case 6:
                    localisableCategory = "Log Shippings"; // SR.CategoryLogShippings;
                    break;
                case 7:
                    localisableCategory = SR.CategoryDBEngineTuningAdvisor;
                    break;
                case 8:
                    localisableCategory = SR.CategoryDataCollector;
                    break;
                case 10:
                    localisableCategory = SR.CategoryReplDistribution;
                    break;
                case 11:
                    localisableCategory = SR.CategoryReplDistributionCleanup;
                    break;
                case 12:
                    localisableCategory = SR.CategoryReplHistoryCleanup;
                    break;
                case 13:
                    localisableCategory = SR.CategoryReplLogReader;
                    break;
                case 14:
                    localisableCategory = SR.CategoryReplMerge;
                    break;
                case 15:
                    localisableCategory = SR.CategoryReplSnapShot;
                    break;
                case 16:
                    localisableCategory = SR.CategoryReplCheckup;
                    break;
                case 17:
                    localisableCategory = SR.CategoryReplCleanup;
                    break;
                case 18:
                    localisableCategory = SR.CategoryReplAlert;
                    break;
                case 19:
                    localisableCategory = SR.CategoryReplQReader;
                    break;
                case 20:
                    localisableCategory = SR.CategoryReplication;
                    break;
                case 98:
                case 99:
                    localisableCategory = SR.CategoryUncategorized;
                    break;
                default:
                    localisableCategory = defaultName;
                    break;
            }

            return localisableCategory;
        }

        private static string LookupLocalizableName(JobCategory category)
        {
            return LookupLocalizableName(category.ID, category.Name);
        }
    }
}
