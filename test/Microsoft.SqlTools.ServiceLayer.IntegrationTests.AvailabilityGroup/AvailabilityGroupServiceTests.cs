//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SMO = Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.AvailabilityGroup;
using Microsoft.SqlTools.ServiceLayer.AvailabilityGroup.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.AvailabilityGroup
{
    public class AvailabilityGroupServiceTests
    {
        /// <summary>
        /// Verify that availability groups requests returns the correct availability groups
        /// </summary>
        [Fact]
        public async Task TestHandleAvailabilityGroupsRequest()
        {
            using (SelfCleaningTempFile queryTempFile = new SelfCleaningTempFile())
            {
                var connectionResults = await LiveConnectionHelper.GetHADRConnections(new[] { queryTempFile.FilePath });

                var requestParams = new AvailabilityGroupsRequestParams()
                {
                    OwnerUri = connectionResults.FirstOrDefault().ConnectionInfo.OwnerUri
                };

                var requestContext = new Mock<RequestContext<AvailabilityGroupsResult>>();

                AvailabilityGroupService service = new AvailabilityGroupService();
                await service.HandleAvailabilityGroupsRequest(requestParams, requestContext.Object);
                requestContext.VerifyAll();
            }
        }
    }
}