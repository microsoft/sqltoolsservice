//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.SqlTools.Authentication.Utility;

namespace Microsoft.SqlTools.Authentication.UnitTests
{
    /// <summary>
    /// Tests <see cref="MsalEncryptedCacheHelper"/> against a real MSAL token cache, with cache files written the way the client writes them.
    /// </summary>
    [TestFixture]
    public class MsalEncryptedCacheHelperTests
    {
        private const string ClientId = "a69788c6-1d43-44ed-9ca3-b83e194da255";
        private const string TenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
        private const string ObjectId = "0d3c5a1e-8b52-4a8e-9f39-1b2c3d4e5f60";
        private const string Scope = "https://database.windows.net/.default";

        private string cacheFolder = null!;
        private string cacheFilePath = null!;
        private (string key, string iv) currentKeys;
        private IPublicClientApplication app = null!;

        [SetUp]
        public void SetUp()
        {
            cacheFolder = Path.Combine(Path.GetTempPath(), $"{nameof(MsalEncryptedCacheHelperTests)}-{Guid.NewGuid()}");
            Directory.CreateDirectory(cacheFolder);
            cacheFilePath = Path.Combine(cacheFolder, "tokenCache.bin");

            currentKeys = CreateKeys();

            var helper = new MsalEncryptedCacheHelper(
                new AuthenticatorConfiguration(ClientId, "AppName", cacheFolder, "tokenCache.bin"),
                () => currentKeys);

            app = PublicClientApplicationBuilder.Create(ClientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, TenantId)
                .WithInstanceDiscovery(false)
                .Build();
            helper.RegisterCache(app.UserTokenCache);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                Directory.Delete(cacheFolder, recursive: true);
            }
            catch (IOException)
            {
                // Best effort cleanup of the temp folder.
            }
        }

        [Test]
        public async Task ReadsCacheFileWithKeysChangedAfterFirstAccess()
        {
            WriteCacheFile("token-before-clear", currentKeys);
            Assert.AreEqual("token-before-clear", await AcquireTokenSilentAsync());

            // The client clears its token cache: it generates new keys, notifies STS, and rewrites the cache file with them.
            currentKeys = CreateKeys();
            WriteCacheFile("token-after-clear", currentKeys);

            Assert.AreEqual("token-after-clear", await AcquireTokenSilentAsync());
        }

        [Test]
        public async Task WritesCacheFileWithKeysChangedAfterFirstAccess()
        {
            WriteCacheFile("token", currentKeys);
            IAccount account = (await app.GetAccountsAsync()).Single();

            currentKeys = CreateKeys();
            WriteCacheFile("token", currentKeys);

            // Removing the account changes the cache, so it's written back to the file.
            await app.RemoveAsync(account);

            string cacheContents = ReadCacheFile(currentKeys);
            StringAssert.DoesNotContain(ObjectId, cacheContents, "The account should have been removed from the cache file.");
        }

        [Test]
        public async Task UndecryptableCacheFileClearsTokensInMemory()
        {
            WriteCacheFile("stale-token", currentKeys);
            IAccount account = (await app.GetAccountsAsync()).Single();
            Assert.AreEqual("stale-token", await AcquireTokenSilentAsync());

            // The cache file was rewritten with keys STS doesn't have.
            WriteCacheFile("new-token", CreateKeys());

            Assert.IsEmpty(await app.GetAccountsAsync());
            Assert.ThrowsAsync<MsalUiRequiredException>(() => app.AcquireTokenSilent(new[] { Scope }, account).ExecuteAsync());
        }

        [Test]
        public async Task MissingCacheFileClearsTokensInMemory()
        {
            WriteCacheFile("stale-token", currentKeys);
            IAccount account = (await app.GetAccountsAsync()).Single();
            Assert.AreEqual("stale-token", await AcquireTokenSilentAsync());

            File.Delete(cacheFilePath);

            Assert.IsEmpty(await app.GetAccountsAsync());
            Assert.ThrowsAsync<MsalUiRequiredException>(() => app.AcquireTokenSilent(new[] { Scope }, account).ExecuteAsync());
        }

        [Test]
        public async Task CorruptCacheFileClearsTokensInMemory()
        {
            WriteCacheFile("stale-token", currentKeys);
            Assert.AreEqual("stale-token", await AcquireTokenSilentAsync());

            byte[] encrypted = EncryptionUtils.AesEncrypt(Encoding.UTF8.GetBytes("not a token cache"), Encoding.Unicode.GetBytes(currentKeys.key), Encoding.Unicode.GetBytes(currentKeys.iv));
            File.WriteAllText(cacheFilePath, Convert.ToBase64String(encrypted));

            Assert.IsEmpty(await app.GetAccountsAsync());
        }

        [Test]
        public async Task MissingKeysClearTokensInMemory()
        {
            WriteCacheFile("stale-token", currentKeys);
            Assert.AreEqual("stale-token", await AcquireTokenSilentAsync());

            currentKeys = (null!, null!);

            Assert.IsEmpty(await app.GetAccountsAsync());
        }

        private async Task<string> AcquireTokenSilentAsync()
        {
            IAccount account = (await app.GetAccountsAsync()).Single();
            AuthenticationResult result = await app.AcquireTokenSilent(new[] { Scope }, account).ExecuteAsync();
            return result.AccessToken;
        }

        private static (string key, string iv) CreateKeys()
        {
            // The helper uses the UTF-16 bytes of the strings: 16 characters for a 256 bit key, 8 for a 128 bit IV.
            string key = Convert.ToHexString(RandomNumberGenerator.GetBytes(8));
            string iv = Convert.ToHexString(RandomNumberGenerator.GetBytes(4));
            return (key, iv);
        }

        /// <summary>
        /// Writes an encrypted MSAL cache file with one account, its refresh token and an access token for <see cref="Scope"/>.
        /// </summary>
        private void WriteCacheFile(string accessToken, (string key, string iv) keys)
        {
            string homeAccountId = $"{ObjectId}.{TenantId}";
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long expiresOn = now + 3600;
            string cache = $$"""
                {
                  "Account": {
                    "{{homeAccountId}}-login.microsoftonline.com-{{TenantId}}": {
                      "home_account_id": "{{homeAccountId}}",
                      "environment": "login.microsoftonline.com",
                      "realm": "{{TenantId}}",
                      "local_account_id": "{{ObjectId}}",
                      "username": "johndoe@contoso.com",
                      "authority_type": "MSSTS"
                    }
                  },
                  "RefreshToken": {
                    "{{homeAccountId}}-login.microsoftonline.com-refreshtoken-{{ClientId}}--": {
                      "home_account_id": "{{homeAccountId}}",
                      "environment": "login.microsoftonline.com",
                      "credential_type": "RefreshToken",
                      "client_id": "{{ClientId}}",
                      "secret": "refresh-token"
                    }
                  },
                  "AccessToken": {
                    "{{homeAccountId}}-login.microsoftonline.com-accesstoken-{{ClientId}}-{{TenantId}}-{{Scope}}": {
                      "home_account_id": "{{homeAccountId}}",
                      "environment": "login.microsoftonline.com",
                      "credential_type": "AccessToken",
                      "client_id": "{{ClientId}}",
                      "secret": "{{accessToken}}",
                      "realm": "{{TenantId}}",
                      "target": "{{Scope}}",
                      "token_type": "Bearer",
                      "cached_at": "{{now}}",
                      "expires_on": "{{expiresOn}}",
                      "extended_expires_on": "{{expiresOn}}"
                    }
                  }
                }
                """;

            byte[] encrypted = EncryptionUtils.AesEncrypt(Encoding.UTF8.GetBytes(cache), Encoding.Unicode.GetBytes(keys.key), Encoding.Unicode.GetBytes(keys.iv));
            File.WriteAllText(cacheFilePath, Convert.ToBase64String(encrypted));
        }

        private string ReadCacheFile((string key, string iv) keys)
        {
            byte[] encrypted = Convert.FromBase64String(File.ReadAllText(cacheFilePath));
            byte[] decrypted = EncryptionUtils.AesDecrypt(encrypted, Encoding.Unicode.GetBytes(keys.key), Encoding.Unicode.GetBytes(keys.iv));
            return Encoding.UTF8.GetString(decrypted);
        }
    }
}
