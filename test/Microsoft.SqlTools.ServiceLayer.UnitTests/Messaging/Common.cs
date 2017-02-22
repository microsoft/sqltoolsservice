//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Text;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Messaging
{
    public class Common
    {
        public const string TestEventString = @"{""type"":""event"",""event"":""testEvent"",""body"":null}";
        public const string TestEventFormatString = @"{{""event"":""testEvent"",""body"":{{""someString"":""{0}""}},""seq"":0,""type"":""event""}}";
        public static readonly int ExpectedMessageByteCount = Encoding.UTF8.GetByteCount(TestEventString);

    }
}
