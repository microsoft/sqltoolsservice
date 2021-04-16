//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlTools.Hosting.Utility;
using Newtonsoft.Json;

namespace Microsoft.SqlTools.CoreServices.SqlContext
{
    
    /// <summary>
    /// Handles backwards compatibility of settings by checking for settings in a priority list. If a settings
    /// group such as Intellisense is defined on a serialized setting it's used in the order of mssql, then sql, then
    /// falls back to a default value.
    /// </summary>
    public class CompoundToolsSettingsValues: ISqlToolsSettingsValues
    {
        private List<ISqlToolsSettingsValues> priorityList = new List<ISqlToolsSettingsValues>();
        private SqlToolsSettingsValues defaultValues;
        public CompoundToolsSettingsValues(ISqlToolsSettingsValues mssql, ISqlToolsSettingsValues all)
        {
            Validate.IsNotNull(nameof(mssql), mssql);
            Validate.IsNotNull(nameof(all), all);
            priorityList.Add(mssql);
            priorityList.Add(all);
            // Always add in a fallback which has default values to be used.
            defaultValues = new SqlToolsSettingsValues(createDefaults: true);
            priorityList.Add(defaultValues);
        }

        private T GetSettingOrDefault<T>(Func<ISqlToolsSettingsValues, T> lookup)
            where T : new()
        {
            T value = priorityList.Select( (settings) => lookup(settings)).Where(val => val != null).FirstOrDefault();
            return value != null ? value : new T();
        }

        /// <summary>
        /// Gets or sets the detailed IntelliSense settings
        /// </summary>
        public IntelliSenseSettings IntelliSense
        { 
            get
            {
                return GetSettingOrDefault((settings) => settings.IntelliSense);
            } 
            set
            {
                priorityList[0].IntelliSense = value;
            }
        }

        // /// <summary>
        // /// Gets or sets the query execution settings
        // /// </summary>
        // public QueryExecutionSettings QueryExecutionSettings
        // { 
        //     get
        //     {
        //         return GetSettingOrDefault((settings) => settings.QueryExecutionSettings);
        //     } 
        //     set
        //     {
        //         priorityList[0].QueryExecutionSettings = value;
        //     }
        // }

        // /// <summary>
        // /// Gets or sets the formatter settings
        // /// </summary>
        // public FormatterSettings Format
        // { 
        //     get
        //     {
        //         return GetSettingOrDefault((settings) => settings.Format);
        //     } 
        //     set
        //     {
        //         priorityList[0].Format = value;
        //     }
        // }

        /// <summary>
        /// Gets or sets the object explorer settings
        /// </summary>
        public ObjectExplorerSettings ObjectExplorer
        { 
            get
            {
                return GetSettingOrDefault((settings) => settings.ObjectExplorer);
            } 
            set
            {
                priorityList[0].ObjectExplorer = value;
            }
        }
    }
}
