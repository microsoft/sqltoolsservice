//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.AsyncRequest;
using System;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.AsyncRequests
{
    public class SqlServiceStub
    {
        public void Stop()
        {
            IsStopped = true;
        }

        public void Fail()
        {
            Failed = true;
        }

        public bool IsStopped { get; set; }

        public bool Failed { get; set; }

        public AsyncSqlResponse FunctionToRun(AsyncRequestParams asyncRequestParams)
        {
            AsyncSqlResponse response = new AsyncSqlResponse();
            while (!IsStopped)
            {
                //Just keep running
                if (Failed)
                {
                    throw new InvalidOperationException();
                }
            }

            return response;
        }
    }
}
