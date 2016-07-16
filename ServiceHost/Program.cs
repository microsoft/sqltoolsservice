using System;
using Microsoft.SqlTools.EditorServices.Protocol.Server;
using Microsoft.SqlTools.EditorServices.Session;

namespace Microsoft.SqlTools.ServiceHost
{        
    class Program
    {
        static void Main(string[] args)
        {
            var hostDetails = new HostDetails("name", "profileId", new Version(1,0));     
            var profilePaths = new ProfilePaths("hostProfileId", "baseAllUsersPath", "baseCurrentUserPath");
            var languageServer = new LanguageServer(hostDetails, profilePaths);
            
            languageServer.Start().Wait();
            
            languageServer.WaitForExit();
        }
    }
}
