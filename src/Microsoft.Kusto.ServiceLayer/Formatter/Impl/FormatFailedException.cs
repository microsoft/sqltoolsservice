//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
{
    public class FormatFailedException : Exception
    {
        public FormatFailedException()
            : base()
        {
        }

        public FormatFailedException(string message, Exception exception)
            : base(message, exception)
        {
        }


        public FormatFailedException(string message)
            : base(message)
        {
        }
    }
}