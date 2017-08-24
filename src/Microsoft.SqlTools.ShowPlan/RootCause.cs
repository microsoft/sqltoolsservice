using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.SqlTools.ShowPlan
{
    /// <summary>
    /// A class that lists String Root Cause constants common to XML Show Plan Nodes 
    ///     /// to be used like nodeOri2[NodeBuilderConstants.ActualExecutions]
    /// </summary>
    public class RootCause : IRootCause
    {
        private readonly string _link = "HERE";
        private readonly Dictionary<string, string> _rootCauseTypes = new Dictionary<string, string>()
        {
            {"Parameter", "The predicate for this operator depends on parameter {0}. The compile-time value was {1} so the estimate may not be accurate for the run-time value. Refer to Parameters for more details. Click <{2}> for recommendations on possible workarounds." },
            {"Statistics", "One of the common reasons for estimation differences is the use of different statistics. Check if statistics for table {0} are different or stale. Refer to Statistics for more information."}
        };

        public string RootCauseColParam { get; set; }
        public string RootCauseString { get; set; }
        public string TypeOfRootCause { get; set; }
        public double Weight { get; set; }//weight represents significance of recs, determines sort/print order

        public RootCause(string typeSimple) //basic constructor, does not format strings
        {
            if(typeSimple.Equals("Statistics"))
            {
                Weight = 0.5;
            }
            else if (typeSimple.Equals("Parameter"))
            {
                Weight = 0.9;
            }
            else
            {
                Weight = 1; //high weight because unknown case/future root cause cases
            }
            RootCauseString = _rootCauseTypes[typeSimple];
        }
        
        /// <summary>
        /// Given table names, format the statistics root cause string to contain these names
        /// </summary>
        /// <param name="tableNames">Hash set of strings containing table names from column references</param>
        public void SetStatisticsRootCause(HashSet<string> tableNames)
        {
            TypeOfRootCause = "Statistics"; // Root cause type is Statistics
            string rootCauseDescription = _rootCauseTypes["Statistics"];
            StringBuilder sb = new StringBuilder();
            int count = 0;
            foreach (string tableName in tableNames)
            {
                if(count == 0)
                {
                    sb.Append(tableName);
                }
                else
                {
                    sb.Append(" OR ");
                    sb.Append(tableName);
                }
                ++count;
            }
            if(count ==0) // did not find any...
            {
                sb.Append("Unknown");
            }
            RootCauseString = String.Format(rootCauseDescription, sb);
            Weight = 0.5; //lower weight than param, i.e. < 0.9
        }

        /// <summary>
        /// Given a parameter, format the root cause string with that parameter
        /// </summary>
        /// <param name="parameter">Parameter whose root cause string must be formatted</param>
        public void SetParameterRootCause(Parameter parameter)
        {
            TypeOfRootCause = "Parameter"; // Root cause type is Parameter
            string rootCauseDescription = _rootCauseTypes["Parameter"];
            RootCauseString = String.Format(rootCauseDescription, parameter.ColumnReference, parameter.CompiledValueUnknown ? "unknown" : "different from the runtime value", _link);
            RootCauseColParam = parameter.ColumnReference;
            Weight = 0.9; //from ssms doc
        }
    }
}
