﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Kusto.ServiceLayer.Connection.Contracts;

namespace Microsoft.Kusto.ServiceLayer.Connection
{
    /// <summary>
    /// Used to uniquely identify a CancellationTokenSource associated with both
    /// a string URI and a string connection type.
    /// </summary>
    public class CancelTokenKey : CancelConnectParams, IEquatable<CancelTokenKey>
    {
        public override bool Equals(object obj)
        {
            CancelTokenKey other = obj as CancelTokenKey;
            if (other == null)
            {
                return false;
            }

            return other.OwnerUri == OwnerUri && other.Type == Type;
        }

        public bool Equals(CancelTokenKey obj)
        {
            return obj.OwnerUri == OwnerUri && obj.Type == Type;
        }

        public override int GetHashCode()
        {
            return OwnerUri.GetHashCode() ^ Type.GetHashCode();
        }
    }
}
