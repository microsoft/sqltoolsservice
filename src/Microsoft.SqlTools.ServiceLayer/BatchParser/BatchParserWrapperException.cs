//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
	/// <summary>This exception is used to report that can come from the Batch Parser inside BatchParserWrapper.</summary>
    internal sealed class BatchParserWrapperException : Exception
	{
        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        private string description;

        public BatchParserWrapperException()
           : base()
        {
        }

    }
}
