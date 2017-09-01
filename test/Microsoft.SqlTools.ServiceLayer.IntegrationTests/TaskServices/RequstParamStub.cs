using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.TaskServices
{
    public class RequstParamStub : IScriptableRequestParams
    {
        public TaskExecutionMode TaskExecutionMode { get; set; }
        public string OwnerUri { get; set; }
        public string DatabaseName { get; set; }
    }
}
