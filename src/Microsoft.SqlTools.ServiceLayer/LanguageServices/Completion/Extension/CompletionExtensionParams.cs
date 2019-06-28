using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion.Extension
{
    
    [Serializable]
    public class CompletionExtensionParams
    {
        public string Assembly;
        public string TypeName;
        public Dictionary<string, object> Properties;
    }

    [Serializable]
    public class CompletionExtensionLoadStatus
    {
        public bool IsLoaded;
        public string ErrorMsg;
    }
}
