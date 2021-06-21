//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.CoreServices.Connection
{
    public class DatabaseFullAccessException: Exception
    {
        public DatabaseFullAccessException()
            : base()
        {
        }

        public DatabaseFullAccessException(string message, Exception exception)
            : base(message, exception)
        {
        }


        public DatabaseFullAccessException(string message)
            : base(message)
        {
        }
    }
}
