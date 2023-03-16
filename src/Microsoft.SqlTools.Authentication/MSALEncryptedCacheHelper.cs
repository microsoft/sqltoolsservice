//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Text;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using Logger = Microsoft.SqlTools.Utility.Logger;

namespace Microsoft.SqlTools.Authentication.Utility
{
    /// <summary>
    /// This class provides capability to register MSAL Token cache and uses the beforeCacheAccess and afterCacheAccess callbacks 
    /// to read and write cache to file system. This is done as cache encryption/decryption algorithm is shared between NodeJS and .NET.
    /// Because, we cannot use msal-node-extensions in NodeJS, we also cannot use MSAL Extensions Dotnet NuGet package.
    /// Ref https://docs.microsoft.com/en-us/azure/active-directory/develop/msal-node-migration#enable-token-caching
    /// In future we should use msal extensions to not have to maintain encryption logic in our applications, and also introduce support for
    /// token storage options in system keychain/credential store.
    /// However - as of now msal-node-extensions does not come with pre-compiled native libraries that causes runtime issues
    /// Ref https://github.com/AzureAD/microsoft-authentication-library-for-js/issues/3332
    /// </summary>
    public class MsalEncryptedCacheHelper
    {
        /// <summary>
        /// Callback delegate to be implemented by Services in Service Host, where authentication is performed. e.g. Connection Service.
        /// This delegate will be called to retrieve key and IV data if found absent or during instantiation.
        /// </summary>
        /// <param name="key">(out) Key used for encryption/decryption</param>
        /// <param name="iv">(out) IV used for encryption/decryption</param>
        public delegate void IvKeyReadCallback(out string key, out string iv);

        /// <summary>
        /// Lock objects for serialization
        /// </summary>
        private readonly object _lockObject = new object();
        private CrossPlatLock? _cacheLock = null;

        private AuthenticatorConfiguration _config;
        private StorageCreationProperties _storageCreationProperties;
        private IvKeyReadCallback _ivKeyReadCallback;

        private byte[]? _iv;
        private byte[]? _key;

        /// <summary>
        /// Storage that handles the storing of the MSAL cache file on disk.
        /// </summary>
        private Storage _cacheStorage { get; }

        #region Public Methods

        /// <summary>
        /// Instantiates cache encryption helper instance.
        /// </summary>
        /// <param name="config">Configuration containing cache location and name.</param>
        /// <param name="callback">Delegate callback to retrieve IV and Key from Credential Store when needed.</param>
        public MsalEncryptedCacheHelper(AuthenticatorConfiguration config, IvKeyReadCallback callback)
        {
            this._config = config;

            this._storageCreationProperties = new StorageCreationPropertiesBuilder(config.CacheFileName, config.CacheFolderPath)
                .WithCacheChangedEvent(config.AppClientId)
                .WithUnprotectedFile().Build();

            this._cacheStorage = Storage.Create(_storageCreationProperties, Logger.TraceSource);

            this._ivKeyReadCallback = callback;

            this.fillIvKeyIfNeeded();
        }

        /// <summary>
        /// Registers <paramref name="tokenCache"/> before and after access methods that are fired on cache access.
        /// </summary>
        /// <param name="tokenCache">Access token cache from MSAL.NET</param>
        /// <exception cref="ArgumentNullException">When token cache is not provided.</exception>
        public void RegisterCache(ITokenCache tokenCache)
        {
            if (tokenCache == null)
            {
                throw new ArgumentNullException(nameof(tokenCache));
            }

            Logger.Information($"Registering MSAL token cache with encrypted file storage");

            // If the token cache was already registered, this operation does nothing
            tokenCache.SetBeforeAccess(BeforeAccessNotification);
            tokenCache.SetAfterAccess(AfterAccessNotification);
        }

        #endregion

        #region Private Methods

        private void fillIvKeyIfNeeded()
        {
            if (this._key == null || this._iv == null)
            {
                this._ivKeyReadCallback(out string key, out string iv);

                if (key != null)
                {
                    this._key = Encoding.Unicode.GetBytes(key);
                }

                if (iv != null)
                {
                    this._iv = Encoding.Unicode.GetBytes(iv);
                }

                Logger.Verbose($"Received IV and Key from callback");
            }
        }

        /// <summary>
        /// Triggered after cache is accessed, <paramref name="args"/> provides updated cache data that
        /// needs to be updated in File Storage. We encrypt cache data here and store it in file system.
        /// </summary>
        /// <param name="args">Access token cache notification arguments.</param>
        private void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            try
            {
                Logger.Verbose($"After access");
                byte[]? data = null;
                // if the access operation resulted in a cache update
                if (args.HasStateChanged)
                {
                    Logger.Verbose($"After access, cache in memory HasChanged");
                    try
                    {
                        data = args.TokenCache.SerializeMsalV3();
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"An exception was encountered while serializing the {nameof(MsalCacheHelper)} : {e}");
                        Logger.Error($"No data found in the store, clearing the cache in memory.");

                        // The cache is corrupt clear it out
                        this._cacheStorage.Clear(ignoreExceptions: true);
                    }

                    if (data != null)
                    {
                        Logger.Verbose($"Serializing '{data.Length}' bytes");

                        try
                        {
                            fillIvKeyIfNeeded();
                            var encryptedData = EncryptionUtils.AesEncrypt(data, this._key!, this._iv!);
                            File.WriteAllText(this._storageCreationProperties.CacheFileName, Convert.ToBase64String(encryptedData));
                        }
                        catch (Exception e)
                        {
                            Logger.Error($"Could not write the token cache. Ignoring. {e.Message}");
                        }
                    }
                    else
                    {
                        Logger.Verbose($"No data read from Token Cache");
                    }
                }
            }
            finally
            {
                ReleaseFileLock();
            }
        }

        /// <summary>
        /// Triggered before cache is accessed, we update <paramref name="args"/> with data from file storage.
        /// Cache file is decrypted and cache data is synced with MSAL.NET memory token cache.
        /// </summary>
        /// <param name="args">Access token cache notification arguments.</param>
        private void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            Logger.Verbose($"Before cache access, acquiring lock for token cache");

            // We have two nested locks here. We need to maintain a clear ordering to avoid deadlocks.
            // This is critical to prevent cache corruption and only 1 process accesses cache file at a time.
            // 1. Use the CrossPlatLock which is respected by all processes and is used around all cache accesses.
            //    This lock (using lockfile) is also shared with NodeJS application.
            // 2. Use _lockObject which is used in UnregisterCache, and is needed for all accesses of _registeredCaches.
            this._cacheLock = CreateCrossPlatLock(_storageCreationProperties);

            Logger.Verbose($"Before access, the store has changed");

            byte[]? cachedStoreData = null;
            byte[]? decryptedData = null;

            try
            {
                var text = File.ReadAllText(_storageCreationProperties.CacheFilePath);
                if (text != null)
                {
                    cachedStoreData = Convert.FromBase64String(text);
                    fillIvKeyIfNeeded();
                    decryptedData = EncryptionUtils.AesDecrypt(cachedStoreData, this._key!, this._iv!);
                }
                else
                {
                    Logger.Information($"Token cache not received. Ignoring.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not read the token cache. Ignoring. Exception: {ex}");
                return;

            }
            Logger.Verbose($"Read '{cachedStoreData?.Length}' bytes from storage");

            if (decryptedData != null)
            {
                lock (_lockObject)
                {
                    try
                    {
                        Logger.Verbose($"Deserializing the store");
                        args.TokenCache.DeserializeMsalV3(decryptedData, shouldClearExistingCache: true);
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"An exception was encountered while deserializing the {nameof(MsalCacheHelper)} : {e}");
                        Logger.Error($"No data found in the store, clearing the cache in memory.");

                        // Clear the memory cache without taking the lock over again
                        this._cacheStorage.Clear(ignoreExceptions: true);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a new instance of a lock for synchronizing against a cache made with the same creation properties.
        /// </summary>
        private static CrossPlatLock CreateCrossPlatLock(StorageCreationProperties storageCreationProperties)
        {
            return new CrossPlatLock(
                storageCreationProperties.CacheFilePath + ".lockfile",
                storageCreationProperties.LockRetryDelay,
                storageCreationProperties.LockRetryCount);
        }

        /// <summary>
        /// Releases file lock by disposing it.
        /// </summary>
        private void ReleaseFileLock()
        {
            // Get a local copy and call null before disposing because when the lock is disposed the next thread will replace CacheLock with its instance,
            // therefore we do not want to null out CacheLock after dispose since this may orphan a CacheLock.
            var localDispose = this._cacheLock;
            this._cacheLock = null;
            localDispose?.Dispose();
            Logger.Information($"Released local lock");
        }

        #endregion
    }
}
