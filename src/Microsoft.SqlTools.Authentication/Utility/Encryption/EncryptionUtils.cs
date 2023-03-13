//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Security.Cryptography;

namespace Microsoft.SqlTools.Authentication.Utility
{
    public static class EncryptionUtils
    {
        /// <summary>
        /// Encrypts provided byte array with 'aes-256-cbc' algorithm.
        /// </summary>
        /// <param name="plainText">Plain text data</param>
        /// <param name="key">Encryption Key</param>
        /// <param name="iv">Encryption IV</param>
        /// <returns>Encrypted data in bytes</returns>
        /// <exception cref="ArgumentNullException">When arguments are null or empty.</exception>
        public static byte[] AesEncrypt(byte[] plainText, byte[] key, byte[] iv)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
            {
                throw new ArgumentNullException(nameof(plainText));
            }

            using var aes = CreateAes(key, iv);
            using var encryptor = aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(plainText, 0, plainText.Length);
        }

        /// <summary>
        /// Decrypts provided byte array with 'aes-256-cbc' algorithm.
        /// </summary>
        /// <param name="cipherText">Encrypted data</param>
        /// <param name="key">Encryption Key</param>
        /// <param name="iv">Encryption IV</param>
        /// <returns>Plain text data in bytes</returns>
        /// <exception cref="ArgumentNullException">When arguments are null or empty.</exception>
        public static byte[] AesDecrypt(byte[] cipherText, byte[] key, byte[] iv)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
            {
                throw new ArgumentNullException(nameof(cipherText));
            }

            using var aes = CreateAes(key, iv);
            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
        }

        private static Aes CreateAes(byte[] key, byte[] iv)
        {
            // Check arguments.
            if (key == null || key.Length <= 0)
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (iv == null || iv.Length <= 0)
            {
                throw new ArgumentNullException(nameof(iv));
            }

            var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Key = key;
            aes.IV = iv;
            return aes;
        }
    }
}
