//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Xunit;

namespace Microsoft.SqlTools.Sts2.UnitTests.Drivers
{
    public sealed class SqlServerProbeTests
    {
        [Fact]
        public void UnavailableReasonDoesNotEchoProviderMessage()
        {
            const string SecretCanary = "Password=do-not-print;User ID=private-user";

            string reason = SqlServerProbe.UnavailableReason(
                new InvalidOperationException(SecretCanary));

            Assert.DoesNotContain(SecretCanary, reason, StringComparison.Ordinal);
            Assert.DoesNotContain("private-user", reason, StringComparison.Ordinal);
            Assert.Contains(nameof(InvalidOperationException), reason, StringComparison.Ordinal);
        }
    }
}
