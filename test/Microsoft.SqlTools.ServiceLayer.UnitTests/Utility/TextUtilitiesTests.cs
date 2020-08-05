//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Utility;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Utility
{
    /// <summary>
    /// Tests for the TextUtilitiesTests class
    /// </summary>
    public class TextUtilitiesTests
    {
        [Test]
        public void PositionOfCursorFirstLine()
        {
            string sql = "EXEC sys.fn_isrolemember ";

            int prevNewLine;
            int cursorPosition = TextUtilities.PositionOfCursor(sql, 0, sql.Length, out prevNewLine);

            Assert.AreEqual(0, prevNewLine);
            Assert.AreEqual(cursorPosition, sql.Length);
        }

        [Test]
        public void PositionOfCursorSecondLine()
        {
            string sql = "--lineone\nEXEC sys.fn_isrolemember ";

            int prevNewLine;
            int cursorPosition = TextUtilities.PositionOfCursor(sql, 1, 15, out prevNewLine);

            Assert.AreEqual(10, prevNewLine);
            Assert.AreEqual(25, cursorPosition);
        }
    }
}
