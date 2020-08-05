//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.QueryExecution;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution
{
    public class SpecialActionTests
    {

        [Test]
        public void SpecialActionInstantiation()
        {
            // If:
            // ... I create a special action object 
            var specialAction = new SpecialAction();

            // Then:
            // ... The special action should be set to none and only none
            Assert.AreEqual(true, specialAction.None);
            Assert.AreEqual(false, specialAction.ExpectYukonXMLShowPlan);
        }

        [Test]
        public void SpecialActionNoneProperty()
        {
            // If:
            // ... I create a special action object and add properties but set it back to none 
            var specialAction = new SpecialAction();
            specialAction.ExpectYukonXMLShowPlan = true;
            specialAction.None = true;

            // Then:
            // ... The special action should be set to none and only none
            Assert.AreEqual(true, specialAction.None);
            Assert.AreEqual(false, specialAction.ExpectYukonXMLShowPlan);
        }

        [Test]
        public void SpecialActionExpectYukonXmlShowPlanReset()
        {
            // If:
            // ... I create a special action object and add properties but set the property back to false 
            var specialAction = new SpecialAction();
            specialAction.ExpectYukonXMLShowPlan = true;
            specialAction.ExpectYukonXMLShowPlan = false;

            // Then:
            // ... The special action should be set to none and only none
            Assert.AreEqual(true, specialAction.None);
            Assert.AreEqual(false, specialAction.ExpectYukonXMLShowPlan);
        }

        [Test]
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
            Assert.AreEqual(false, specialAction.None);
            Assert.AreEqual(true, specialAction.ExpectYukonXMLShowPlan);
        }


    }
}
