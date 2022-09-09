//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.Rename;
using Microsoft.SqlTools.ServiceLayer.Rename.Requests;
using Microsoft.SqlTools.ServiceLayer.Test.Common.RequestContextMocking;
using NUnit.Framework;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.Rename
{
    public class RenameServiceTests
    {
        [Test]
        public async Task HandleProcessRenameEditRequestNullTest()
        {
            //Arrange
            RenameService renameService = new RenameService();
            var contextMock = RequestContextMocks.Create<bool>(null);

            //Act & assert
            Assert.That(() => renameService.HandleProcessRenameEditRequest(null, contextMock.Object), Throws.ArgumentNullException);
        }

        [Test]
        public async Task HandleProcessRenameEditRequestNewTable()
        {
            //Arrange
            RenameTableInfo renameTableInfo = new RenameTableInfo
            {
            };
            ProcessRenameEditRequestParams renameProcessEditRequestParams = new ProcessRenameEditRequestParams
            {
                TableInfo = renameTableInfo,
                ChangeInfo = null,
            };

            RenameService renameService = new RenameService();
            var contextMock = RequestContextMocks.Create<bool>(null);

            //Act & Assert
            Assert.That(() => renameService.HandleProcessRenameEditRequest(renameProcessEditRequestParams, contextMock.Object), Throws.InvalidOperationException);
        }
    }
}