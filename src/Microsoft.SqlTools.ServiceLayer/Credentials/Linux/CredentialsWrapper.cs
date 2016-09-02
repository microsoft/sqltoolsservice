using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Credentials.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Credentials.Linux
{
    /// <summary>
    /// Simplified class to enable writing a set of credentials to/from disk
    /// </summary>
    public class CredentialsWrapper
    {
        public List<Credential> Credentials { get; set; }
    }
}
