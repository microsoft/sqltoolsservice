//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;
using Microsoft.SqlTools.ServiceLayer.EditData.UpdateManagement;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;

namespace Microsoft.SqlTools.ServiceLayer.EditData
{
    public class Session
    {

        #region Member Variables

        private readonly List<RowUpdateBase> updateCache;

        #endregion

        public Session(Query query)
        {
            // Setup the internal state
            AssociatedQuery = query;
            updateCache = new List<RowUpdateBase>();
        }

        #region Properties

        public Query AssociatedQuery { get; set; }

        public IEnumerable<RowUpdateBase> UpdateCache => updateCache;

        #endregion

        #region Public Methods



        #endregion

    }
}
