using System;
using System.Collections.Generic;

namespace Microsoft.InsightsGenerator
{
    class SignatureGenerator
    {
        private DataArray table;
        private SignatureGeneratorResult results;
        public SignatureGenerator(DataArray table)
        {
            this.table = table;
            results = new SignatureGeneratorResult();
        }

        public SignatureGeneratorResult Learn()
        {
            return results;
            
        }
    }
}


public class SignatureGeneratorResult
{
    public SignatureGeneratorResult(){
        insights = new Dictionary<string, string[]>();
    }
    public Dictionary<string, string[]> insights;
}