﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#nullable disable

using System;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    internal sealed class ComparisonFailureException : InvalidOperationException
    {
        internal string FullMessageWithDiff { get; private set; }
        internal string EditAndCopyMessage { get; private set; }

        internal ComparisonFailureException(string fullMessageWithDiff, string editAndCopyMessage)
            : base(fullMessageWithDiff)
        {
            FullMessageWithDiff = fullMessageWithDiff;
            EditAndCopyMessage = editAndCopyMessage;
        }

        internal ComparisonFailureException(string editAndCopyMessage)
            : base(editAndCopyMessage)
        {
            EditAndCopyMessage = FullMessageWithDiff = editAndCopyMessage;
        }
    }
}
