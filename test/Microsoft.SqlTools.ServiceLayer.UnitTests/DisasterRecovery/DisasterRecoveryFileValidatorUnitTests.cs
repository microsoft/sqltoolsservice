//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.DisasterRecovery;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.DisasterRecovery
{
    /// <summary>
    /// Unit tests for disaster recovery file validator
    /// </summary>
    public class DisasterRecoveryFileValidatorUnitTests
    {
        [Test]
        public void ValidatorShouldReturnTrueForNullArgument()
        {
            string message;
            bool result = DisasterRecoveryFileValidator.ValidatePaths(null, out message);
            Assert.True(result);
        }

        [Test]
        public void GetMachineNameForLocalServer()
        {
            string machineName = DisasterRecoveryFileValidator.GetMachineName(DisasterRecoveryFileValidator.LocalSqlServer);
            Assert.True(System.Environment.MachineName == machineName);
        }

        [Test]
        public void GetMachineNameForNamedServer()
        {
            string testMachineName = "testmachine";
            string machineName = DisasterRecoveryFileValidator.GetMachineName(string.Format("{0}\\testserver", testMachineName));
            Assert.True(testMachineName == machineName);
        }
    }
}
