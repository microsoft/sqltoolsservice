//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.Admin.Contracts 
{
    public class DatabaseInfoWrapper
    {
        public string name;
        public string owner;
        public string collation;
        public string recoveryModel;
        public string databaseState;
        
        public DatabaseInfoWrapper(DatabaseInfo info)
        {
            object placeholder = null;
            
            info.Options.TryGetValue(AdminServicesProviderOptionsHelper.Name, out placeholder);
            
            if (placeholder != null)
            {
                name = (string) placeholder;
            }

            placeholder = null;

            info.Options.TryGetValue(AdminServicesProviderOptionsHelper.Owner, out placeholder);
            
            if (placeholder != null)
            {
                owner = (string) placeholder;
            }

            placeholder = null;

            info.Options.TryGetValue(AdminServicesProviderOptionsHelper.Collation, out placeholder);
            
            if (placeholder != null)
            {
                collation = (string) placeholder;
            }

            placeholder = null;

            info.Options.TryGetValue(AdminServicesProviderOptionsHelper.RecoveryModel, out placeholder);
            
            if (placeholder != null)
            {
                recoveryModel = (string) placeholder;
            }

            placeholder = null;

            info.Options.TryGetValue(AdminServicesProviderOptionsHelper.DatabaseState, out placeholder);
            
            if (placeholder != null)
            {
                databaseState = (string) placeholder;
            }
        }
    }
}