//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.Scripting
{
    public class ScriptingException : Exception
    {
        public ScriptingException()
            : base()
        {
        }

        public ScriptingException(string message, Exception exception)
            : base(message, exception)
        {
        }


        public ScriptingException(string message)
            : base(message)
        {
        }
    }
}