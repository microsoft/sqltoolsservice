//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System.Globalization;
using NUnit.Framework;

using LanguageServiceSr = Microsoft.SqlTools.LanguageService.SR;

namespace Microsoft.SqlTools.LanguageService.UnitTests.Utility
{
    public class SrTests
    {
        [Test]
        public void SrPropertiesTest()
        {
            LanguageServiceSr.Culture = CultureInfo.CurrentCulture;

            Assert.NotNull(LanguageServiceSr.Culture);
            Assert.NotNull(LanguageServiceSr.ErrorEmptyStringReplacement);
            Assert.NotNull(LanguageServiceSr.WorkspaceServiceBufferPositionOutOfOrder(0, 0, 0, 0));
            Assert.NotNull(LanguageServiceSr.WorkspaceServicePositionLineOutOfRange);
            Assert.NotNull(LanguageServiceSr.WorkspaceServicePositionColumnOutOfRange(0));
        }
    }
}
