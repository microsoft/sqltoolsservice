//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.Hosting.Protocol.Contracts;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Messaging
{
    [TestFixture]
    public class ProtocolEndpointTests
    {
        private static readonly RequestType<int, int> EchoRequest = RequestType<int, int>.Create("test/echo");

        [Test]
        public async Task ConcurrentRequestsGetUniqueIdsAndTheirOwnResponses()
        {
            const int requestsPerRound = 64;
            const int rounds = 20;
            using var connection = new InMemoryProtocolConnection(parallelMessageProcessing: true);

            // The client answers each request with its own parameter, so each caller can check it got its own response.
            var requestIds = new List<MessageId>();
            Task client = Task.Run(async () =>
            {
                for (int i = 0; i < requestsPerRound * rounds; i++)
                {
                    Message request = await connection.ReadMessageAsync();
                    requestIds.Add(request.Id);
                    await connection.SendResponseAsync(request, request.Contents.ToObject<int>());
                }
            });

            for (int round = 0; round < rounds; round++)
            {
                Task<int>[] responses = SendRequestsAtOnce(connection.Endpoint, requestsPerRound);
                int[] results = await Task.WhenAll(responses).WaitAsync(InMemoryProtocolConnection.Timeout);

                Assert.That(results, Is.EqualTo(Enumerable.Range(0, requestsPerRound)), $"Responses were routed to the wrong requests in round {round}");
            }

            await client.WaitAsync(InMemoryProtocolConnection.Timeout);
            Assert.That(requestIds, Is.Unique);
        }

        /// <summary>
        /// Sends <paramref name="count"/> requests from as many threads, released at the same time.
        /// </summary>
        private static Task<int>[] SendRequestsAtOnce(ProtocolEndpoint endpoint, int count)
        {
            var responses = new Task<int>[count];
            using var barrier = new Barrier(count);
            Thread[] threads = Enumerable.Range(0, count)
                .Select(i => new Thread(() =>
                {
                    barrier.SignalAndWait();
                    responses[i] = endpoint.SendRequest(EchoRequest, i, waitForResponse: true);
                }))
                .ToArray();

            foreach (Thread thread in threads)
            {
                thread.Start();
            }

            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            return responses;
        }
    }
}
