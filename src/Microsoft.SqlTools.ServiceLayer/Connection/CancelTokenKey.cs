using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Connection.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.Connection
{
    /// <summary>
    /// Used to uniquely identify a CancellationTokenSource associated with both
    /// a string URI and a string connection type.
    /// </summary>
    public class CancelTokenKey : CancelConnectParams
    {
        public override bool Equals(object obj)
        {
            if (!(obj is CancelTokenKey))
            {
                return false;
            }

            CancelTokenKey other = obj as CancelTokenKey;
            return String.Equals(other.OwnerUri, OwnerUri) && String.Equals(other.Type, Type);
        }

        public override int GetHashCode()
        {
            return OwnerUri.GetHashCode() ^ Type.GetHashCode();
        }
    }
}
