using System;
using System.Collections.Generic;

namespace Microsoft.SqlTools.ShowPlan
{
    public class Parameter 
    {   
        public Parameter(string colRef, Boolean unknown)
        {
            ColumnReference = colRef;
            CompiledValueUnknown = unknown;
            ProblemNodeIds = new List<int>();
        }
        public string ColumnReference { get; set; }
        public Boolean CompiledValueUnknown{ get; set; }
        public List<int> ProblemNodeIds { get; set; }
    }
}
