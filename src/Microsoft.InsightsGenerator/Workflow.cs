//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.InsightsGenerator
{
    public class Workflow
    {
        // Lock synchronization object
        public class Workflow
        {

            public async Task<string> ProcessInputData(DataArray rulesData,
                CancellationToken cancellationToken = new CancellationToken())
            {
                // added cancellationToken just in case for future
                cancellationToken.ThrowIfCancellationRequested();

                //Get the signature result
                SignatureGenerator siggen = new SignatureGenerator(rulesData);

                string insights = null;

                await Task.Run(() =>
                {
                    try
                    {
                        DataTransformer transformer = new DataTransformer();
                        rulesData = transformer.Transform(rulesData);
                        SignatureGeneratorResult result = siggen.Learn();
                        // call the rules engine processor
                        if (result?.Insights == null)
                        {
                            Console.WriteLine("Failure in generating insights, Input not recognized!");
                        }
                        else
                        {
                            insights = RulesEngine.FindMatchedTemplate(result.Insights, rulesData);
                            Console.WriteLine(
                                $"Good News! Insights generator has provided you the chart text: \n{insights}\n");
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        throw;
                    }

                }, cancellationToken);

                return insights;
            }
        }
    }
}
