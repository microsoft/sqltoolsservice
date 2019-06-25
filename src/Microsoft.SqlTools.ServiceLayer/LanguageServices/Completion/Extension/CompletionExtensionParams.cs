using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion.Extension
{
    
    [Serializable]
    public class CompletionExtensionParams
    {
        public string assembly;
        public string typeName;
        public Dictionary<string, object> properties;
    }
}
