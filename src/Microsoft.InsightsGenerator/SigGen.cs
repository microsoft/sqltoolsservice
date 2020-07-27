using System;
using System.Collections.Generic;

namespace Microsoft.InsightsGenerator
{
    class SignatureGenerator
    {
        private DataArray table;
        private SignatureGeneratorResult result;
        public SignatureGenerator(DataArray table)
        {
            this.table = table;
            result = new SignatureGeneratorResult();
        }

        public SignatureGeneratorResult Learn()
        {
            return result;
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