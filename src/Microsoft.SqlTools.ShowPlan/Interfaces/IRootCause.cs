using System.Collections.Generic;

namespace Microsoft.SqlTools.ShowPlan
{
    public interface IRootCause
    {
        string RootCauseColParam { get; set; }
        string RootCauseString { get; set; }
        string TypeOfRootCause { get; set; }
        double Weight { get; set; }

        void SetParameterRootCause(Parameter parameter);
        void SetStatisticsRootCause(HashSet<string> tableNames);
    }
}