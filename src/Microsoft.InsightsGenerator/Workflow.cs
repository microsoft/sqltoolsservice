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
        private readonly ConcurrentQueue<object> _siggenQueue = new ConcurrentQueue<object>();

        private readonly ConcurrentQueue<DataArray> _rulesQueue = new ConcurrentQueue<DataArray>();

        private static Workflow _instance;

        // Lock synchronization object
        private static readonly object syncLock = new object();

      
        public static Workflow Instance(CancellationToken cancellationToken = new CancellationToken())
        {
            // Uses lazy initialization. thread safe for now.
            if (_instance == null)
            {
                lock (syncLock)
                {

                    if (_instance == null)
                    {
                        _instance = new Workflow();
                        _instance.ProcessInputData(cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            return _instance;
        }

        public void IngestSiggen(object input)
        {
            if (_instance == null)
            {
                throw new ApplicationException("Please initialize the singleton class using Workflow.getInstance() and call _instance.IngestSiggen");
            }

            if (input == null)
            {
                throw new ArgumentException("Unable to ingest Siggen. Input object is null.");
            }
            _instance._siggenQueue.Enqueue(input);
        }

        public void IngestRules(DataArray input)
        {
            if (_instance == null)
            {
                throw new ApplicationException("Please initialize the singleton class using Workflow.getInstance() and call _instance.IngestRules");
            }

            if (input == null)
            {
                throw new ArgumentException(" Unable to ingest Rules. Input object is null.");
            }
            _instance._rulesQueue.Enqueue(input);
        }

        private async Task ProcessInputData(CancellationToken cancellationToken)
        {

            cancellationToken.ThrowIfCancellationRequested();

            //while (!cancellationToken.IsCancellationRequested)
            //{
            //    bool siggenInput = _instance._siggenQueue.TryDequeue(out object siggen);

            //    if (siggenInput && siggen != null)
            //    {
            //       // call the siggen processor
            //    }
            //    else
            //    {
            //        await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken).ConfigureAwait(false);
            //    }
            //}

            bool rulesInput = _instance._rulesQueue.TryDequeue(out DataArray rulesData);

            if (rulesInput && rulesData != null)
            {
                //Get the signature result
                SignatureGenerator siggen = new SignatureGenerator(rulesData);
                SignatureGeneratorResult result = siggen.Learn();
                // call the rules engine processor
                RulesEngine re = new RulesEngine(); 
                string insights = re.FindMatchedTemplate(result, rulesData);
                Console.WriteLine($"Good News! Insights generator has provided you the chart text: \n{insights}\n");
            }
            else
            {
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
