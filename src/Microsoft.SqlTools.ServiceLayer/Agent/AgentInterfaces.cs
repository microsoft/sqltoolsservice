//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
