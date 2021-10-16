//---------------------------------------------------------------------
// <copyright file="EditableContentControl.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//---------------------------------------------------------------------

namespace Microsoft.Data.Tools.Design.Core.Controls
{
    /// <summary>
    /// Enum to indicate the result of an edit operation in designer
    /// </summary>
    public enum PerformEditResult
    {
        NotAttempted,
        Success, //Successful edit
        FailRetry, //Failed Edit, however we will let the consumer to retry
        FailAbort //Failed Edit and we won't let the consumer to retry
    };
}
