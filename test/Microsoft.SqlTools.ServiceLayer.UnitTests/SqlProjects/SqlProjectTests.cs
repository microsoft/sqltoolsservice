//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.ServiceLayer.SqlProjects.Contracts;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.SqlProjects
{
    public class SqlProjectTests
    {
        [Test]
        public void DatabaseReferenceValidationTest()
        {
            // Verify that Validate() throws when both DatabaseLiteral and DatabaseVariable are set 
            AddUserDatabaseReferenceParams reference = new AddDacpacReferenceParams() // any concrete class will do
            {
                DatabaseLiteral = "DatabaseName",
                DatabaseVariable = "$(DatabaseVariable)"
            };

            Assert.Throws<ArgumentException>(reference.Validate, $"Validate() for a reference with both {nameof(reference.DatabaseLiteral)} and {nameof(reference.DatabaseVariable)} should have failed");

            // Verify that Validate() passes any other time
            reference = new AddDacpacReferenceParams() { DatabaseLiteral = "DatabaseName" };
            reference.Validate();

            reference = new AddDacpacReferenceParams() { DatabaseVariable = "$(DatabaseVariable)" };
            reference.Validate();

            reference = new AddDacpacReferenceParams();
            reference.Validate();
        }
    }
}
