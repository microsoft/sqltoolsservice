//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using System.Collections;

namespace Microsoft.Kusto.ServiceLayer.Formatter
{
    internal class ReplacementQueue : IEnumerable
    {
        internal int offset = 0;

        private Queue<Replacement> Replacements { get; set; }

        public ReplacementQueue()
        {
            Replacements = new Queue<Replacement>();
        }

        /// <summary>
        ///  Adds a replace action to the queue and adjusts its absolute
        ///  offset to reflect the global indexing after applying the replacements
        ///  in the queue.
        /// 
        ///     NOTE: The method assumes the replacements occur in front-to-back order
        ///     and that they never overlap.
        ///     
        /// </summary>
        /// <param name="r">The latest replacement to be added to the queue.</param>
        public void Add(Replacement r)
        {
            if (!r.IsIdentity())
            {
                r.CumulativeOffset = offset;
                Replacements.Enqueue(r);
                offset += r.InducedOffset;
            }
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return Replacements.GetEnumerator();
        }
    }
}
