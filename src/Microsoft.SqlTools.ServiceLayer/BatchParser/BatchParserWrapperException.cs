//------------------------------------------------------------------------------
// <copyright file="BatchParserWrapperException.cs" company="Microsoft">
//         Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
using System;
using System.Runtime.Serialization;

namespace Microsoft.SqlTools.ServiceLayer.BatchParser
{
	/// <summary>This exception is used to report that can come from the Batch Parser inside BatchParserWrapper.</summary>
    internal sealed class BatchParserWrapperException : Exception
	{
        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        private string _description;

        public BatchParserWrapperException()
           : base()
        {
        }

    }
}
