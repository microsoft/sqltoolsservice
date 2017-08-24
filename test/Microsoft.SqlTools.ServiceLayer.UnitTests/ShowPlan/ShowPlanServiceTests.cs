//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.TaskServices;
using Microsoft.SqlTools.ServiceLayer.TaskServices.Contracts;
using Microsoft.SqlTools.ServiceLayer.UnitTests.Utility;
using Microsoft.SqlTools.ServiceLayer.ShowPlan;
using Moq;
using Xunit;
using Microsoft.SqlTools.ServiceLayer.ShowPlan.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ShowPlanServiceTests
{
    public class ShowPlanServiceTests
    {

        public ShowPlanServiceTests()
        {
        }

        [Fact]
        public async Task TaskListRequestErrorsIfParameterIsNull()
        {
            var requestContext = new Mock<RequestContext<ShowPlanGetStatementInfoResults>>();
            requestContext.Setup(rc => rc.SendResult(It.IsAny<ShowPlanGetStatementInfoResults>()))
                .Returns(Task.FromResult(0));

            var showPlanParams = new ShowPlanGetStatementInfoParams()
            {
                PlanXml = "xml test content"
            };

            await ShowPlanService.HandleGetStatementInfoRequest(showPlanParams, requestContext.Object);

            requestContext.VerifyAll();
            
        }
    }
}
