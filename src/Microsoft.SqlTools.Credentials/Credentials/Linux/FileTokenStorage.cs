//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Credentials.Contracts;
using Microsoft.SqlTools.Utility;
using Newtonsoft.Json;

namespace Microsoft.SqlTools.ServiceLayer.Credentials.Linux
{

#if !WINDOWS_ONLY_BUILD

    public class FileTokenStorage
    {
        private const int OwnerAccessMode = 384; // Permission 0600 - owner read/write, nobody else has access

        private object lockObject = new object();

        private string fileName;

        public FileTokenStorage(string fileName)
        {
            Validate.IsNotNullOrEmptyString("fileName", fileName);
            this.fileName = fileName;
        }

        public void AddEntries(IEnumerable<Credential> newEntries, IEnumerable<Credential> existingEntries)
        {
            var allEntries = existingEntries.Concat(newEntries);
            this.SaveEntries(allEntries);
        }

        public void Clear()
        {
            this.SaveEntries(new List<Credential>());
        }

        public IEnumerable<Credential> LoadEntries()
        {            
            if(!File.Exists(this.fileName))
            {
                return Enumerable.Empty<Credential>();
            }

            string serializedCreds;
            lock (lockObject)
            {
                serializedCreds = File.ReadAllText(this.fileName);
            }

            CredentialsWrapper creds = JsonConvert.DeserializeObject<CredentialsWrapper>(serializedCreds, Constants.JsonSerializerSettings);
            if(creds != null)
            {
                return creds.Credentials;
            }
            return Enumerable.Empty<Credential>();
        }

        public void SaveEntries(IEnumerable<Credential> entries)
        {
            CredentialsWrapper credentials = new CredentialsWrapper() { Credentials = entries.ToList() };
            string serializedCreds = JsonConvert.SerializeObject(credentials, Constants.JsonSerializerSettings);

            lock(lockObject)
            {
                WriteToFile(this.fileName, serializedCreds);
            }
        }

        private static void WriteToFile(string filePath, string fileContents)
        {
            string dir = Path.GetDirectoryName(filePath);
            if(!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Overwrite file, then use ChMod to ensure we have 
            File.WriteAllText(filePath, fileContents);
            // set appropriate permissions so only current user can read/write
            Interop.Sys.ChMod(filePath, OwnerAccessMode);            
        }
    }

#endif

}
