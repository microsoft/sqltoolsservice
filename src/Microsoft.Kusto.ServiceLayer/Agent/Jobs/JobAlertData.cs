//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

using SMO = Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;

namespace Microsoft.Kusto.ServiceLayer.Agent
{
    internal class JobAlertData
    {
        #region data members
        private string currentName;
        private string originalName;
        private bool enabled;
        private AlertType type;
        bool alreadyCreated;
        private bool isReadOnly;
        Urn urn;

        #endregion

        #region construction
        public JobAlertData()
        {
            SetDefaults();
        }
        public JobAlertData(Alert source)
            : this(source, false)
        {
        }
        public JobAlertData(Alert source, bool toBeCreated)
        {
            LoadData(source);
            this.alreadyCreated = toBeCreated;
        }
        #endregion

        #region public members
        public string Name
        {
            get
            {
                return this.currentName;
            }
            set
            {
                this.currentName = value.Trim();
            }
        }
        public bool Enabled
        {
            get
            {
                return this.enabled;
            }
            set
            {
                this.enabled = value;
            }
        }
        public string Type
        {
            get
            {
                return this.type.ToString();
            }
        }
        public bool Created
        {
            get
            {
                return this.alreadyCreated;
            }
        }

        public bool IsReadOnly
        {
            get { return this.isReadOnly; }
            set { this.isReadOnly = value; }

        }
        #endregion

        #region load data
        private void LoadData(Alert source)
        {
            currentName = originalName = source.Name;
            this.urn = source.Urn;
            this.alreadyCreated = true;

            this.enabled = source.IsEnabled;
            this.type = source.AlertType;
        }
        private void SetDefaults()
        {
            this.alreadyCreated = false;
            currentName = originalName = String.Empty;
            this.enabled = true;
        }
        #endregion
    }
}
