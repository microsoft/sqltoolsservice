//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// Change something to trigger a CI build

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    interface IFilterDefinition
    {
        object ShallowClone();

        void ShallowCopy(object template);

        void ResetToDefault();

        bool IsDefault();

        bool Enabled { get; set; }
    }
}
