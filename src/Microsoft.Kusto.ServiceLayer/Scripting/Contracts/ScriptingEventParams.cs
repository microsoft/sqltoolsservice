//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Microsoft.SqlTools.ServiceLayer.Scripting.Contracts
{
    /// <summary>
    /// Base class for all scripting event parameters.
    /// </summary>
    public abstract class ScriptingEventParams
    {
        /// <summary>
        /// Gets or sets the operation id of the scripting operation this event is associated with.
        /// </summary>
        public string OperationId { get; set; }

        /// <summary>
        /// Gets or sets the sequence number.  The sequence number starts at 1, and is incremented each time a scripting event is 
        /// raised for the current scripting operation.
        /// </summary>
        public int SequenceNumber { get; set; }
    }
}
