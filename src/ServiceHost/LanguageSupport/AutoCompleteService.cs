//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.EditorServices.Connection;
using System;
using System.Collections.Generic;

namespace Microsoft.SqlTools.LanguageSupport
{
    /// <summary>
    /// Main class for Autocomplete functionality
    /// </summary>
    public class AutoCompleteService
    {
        /// <summary>
        /// Singleton service instance
        /// </summary>
        private static Lazy<AutoCompleteService> instance 
            = new Lazy<AutoCompleteService>(() => new AutoCompleteService());

        private IEnumerable<string> autoCompleteList;

        public IEnumerable<string> AutoCompleteList
        {
            get
            {
                return this.autoCompleteList;
            }
        }

        /// <summary>
        /// Gets the singleton service instance
        /// </summary>
        public static AutoCompleteService Instance 
        {
            get
            {
                return instance.Value;
            }
        }

        public void UpdateAutoCompleteCache(ISqlConnection connection)
        {
            this.autoCompleteList = connection.GetServerObjects();
        }
    }
}
