//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using Microsoft.SqlTools.ServiceLayer.Test.Utility;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.QueryExecution
{
    public class SpecialActionTests
    {

        [Fact]
        public void SpecialActionInstantiation()
        {
            // If:
            // ... I create a special action object 
            var specialAction = new SpecialAction();

            // Then:
            // ... The special action should be set to none and only none
            Assert.Equal(true, specialAction.None);
            Assert.Equal(false, specialAction.ExpectYukonXMLShowPlan);
        }

        [Fact]
        public void SpecialActionNoneProperty()
        {
            // If:
            // ... I create a special action object and add properties but set it back to none 
            var specialAction = new SpecialAction();
            specialAction.ExpectYukonXMLShowPlan = true;
            specialAction.None = true;

            // Then:
            // ... The special action should be set to none and only none
            Assert.Equal(true, specialAction.None);
            Assert.Equal(false, specialAction.ExpectYukonXMLShowPlan);
        }

        [Fact]
        public void SpecialActionExpectYukonXmlShowPlanReset()
        {
            // If:
            // ... I create a special action object and add properties but set the property back to false 
            var specialAction = new SpecialAction();
            specialAction.ExpectYukonXMLShowPlan = true;
            specialAction.ExpectYukonXMLShowPlan = false;

            // Then:
            // ... The special action should be set to none and only none
            Assert.Equal(true, specialAction.None);
            Assert.Equal(false, specialAction.ExpectYukonXMLShowPlan);
        }

        [Fact]
        public void SpecialActionCombiningProperties()
        {
            // If:
            // ... I create a special action object and add properties and combine with the same property 
            var specialAction = new SpecialAction();
            specialAction.ExpectYukonXMLShowPlan = true;

            var specialAction2 = new SpecialAction();
            specialAction2.ExpectYukonXMLShowPlan = true;

            specialAction.CombineSpecialAction(specialAction2);

            // Then:
            // ... The special action should be set to none and only none
            Assert.Equal(false, specialAction.None);
            Assert.Equal(true, specialAction.ExpectYukonXMLShowPlan);
        }


    }
}
