//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.SqlTools.Extensibility;
using Microsoft.SqlTools.ResourceProvider.Core;
using Microsoft.SqlTools.ResourceProvider.Core.Authentication;
using Microsoft.SqlTools.ResourceProvider.Core.Extensibility;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Fakes
{
    [Exportable(ServerTypes.SqlServer, Categories.Azure,
        typeof(IAccountManager), 
        "Microsoft.SqlTools.ServiceLayer.UnitTests.ResourceProvider.Fakes.FakeAccountManager", 1)]
    public class FakeAccountManager : IAccountManager
    {
        public FakeAccountManager(IExportableMetadata metadata)
        {
            Metadata = metadata;
        }

        public ITrace Trace { get; set; }
        public Task<bool> GetUserNeedsReauthenticationAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IUserAccount> AuthenticateAsync()
        {
            throw new System.NotImplementedException();
        }

        public bool IsCachExpired { get; private set; }
        public bool SessionIsCached { get; private set; }
        public void ResetSession()
        {
            throw new System.NotImplementedException();
        }

        public Task<IUserAccount> AddUserAccountAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task<IUserAccount> SetCurrentAccountAsync(object account)
        {
            throw new System.NotImplementedException();
        }

        public Task<IUserAccount> SetCurrentAccountFromLoginDialogAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IUserAccount> GetCurrentAccountAsync()
        {
            throw new System.NotImplementedException();
        }

        public bool HasLoginDialog { get; }

        public event EventHandler CurrentAccountChanged;

        public IUserAccount SetCurrentAccount(object account)
        {
            if (CurrentAccountChanged != null)
            {
                CurrentAccountChanged(this, null);
            }
            return null;
        }

        public void SetServiceProvider(IMultiServiceProvider provider)
        {
            throw new NotImplementedException();
        }

        public IExportableMetadata Metadata { get; set; }
        public ExportableStatus Status { get; }
    }
}
