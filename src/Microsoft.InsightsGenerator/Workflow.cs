//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.InsightsGenerator
{
    public class Workflow
    {

        public Task<string?> ProcessInputData(DataArray rulesData,
            CancellationToken cancellationToken = new CancellationToken())
        {
            // added cancellationToken just in case for future
            cancellationToken.ThrowIfCancellationRequested();

            //Get the signature result
            SignatureGenerator siggen = new SignatureGenerator(rulesData);

            return Task.Run(() =>
            {
                try
                {
                    DataTransformer transformer = new DataTransformer();
                    transformer.Transform(rulesData);
                    SignatureGeneratorResult result = siggen.Learn();
                        // call the rules engine processor

                    string? insights = null;
                    if (result?.Insights == null)
                    {
                        // Console.WriteLine("Failure in generating insights, Input not recognized!");
                    }
                    else
                    {
                        insights = RulesEngine.FindMatchedTemplate(result.Insights, rulesData);
                        // Console.WriteLine(
                        //    $"Good News! Insights generator has provided you the chart text: \n{insights}\n");
                    }

                    return insights;
                }
                catch (Exception)
                {
                    // Console.WriteLine(ex.ToString());
                    throw;
                }

            }, cancellationToken);
        }
    }
}
