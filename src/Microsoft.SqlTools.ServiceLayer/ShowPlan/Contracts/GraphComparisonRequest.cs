using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.ShowPlan.Contracts
{
    public class GetGraphComparisonParams
    {

    }

    public class GetGraphComparisonResult
    {

    }

    public class GraphComparisonRequest
    {
        public static readonly
            RequestType<GetGraphComparisonParams, GetGraphComparisonResult> Type =
                RequestType<GetGraphComparisonParams, GetGraphComparisonResult>.Create("showplan/compareshowplans");
    }
}
