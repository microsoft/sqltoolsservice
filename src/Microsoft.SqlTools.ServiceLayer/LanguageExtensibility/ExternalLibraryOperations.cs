//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlTools.ServiceLayer.LanguageExtensibility.Contracts;
using Microsoft.SqlTools.ServiceLayer.Utility;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.LanguageExtensibility
{
    public class ExternalLibraryOperations
    {
        public List<ExternalLibraryModel> GetExternalLibraries(ServerConnection serverConnection, string databaeName, string languageName)
        {
            List<ExternalLibraryModel> list = new List<ExternalLibraryModel>();
            Server server = new Server(serverConnection);
            Database database = new Database(server, databaeName);
            for (int i = 0; i < database.ExternalLibraries.Count; i++)
            {
                var item = database.ExternalLibraries[i];
                list.Add(new ExternalLibraryModel
                {
                    Name = item.Name,
                    Owner = item.Owner
                });
            }
            return list;
        }

        public void AddLibrary(ServerConnection serverConnection, string databaeName, ExternalLibraryModel library)
        {
            Server server = new Server(serverConnection);
            Database database = new Database(server, databaeName);
            ExternalLibrary libraryObject = new ExternalLibrary
            {
                Name = library.Name,
                ExternalLibraryLanguage = library.LanguageName
            };
            
            string fileContent = FileUtilities.GetContentInHex(library.FilePath);
            libraryObject.Create(fileContent, ExternalLibraryContentType.Binary);
            database.ExternalLibraries.Add(libraryObject);
        }
    }
}
