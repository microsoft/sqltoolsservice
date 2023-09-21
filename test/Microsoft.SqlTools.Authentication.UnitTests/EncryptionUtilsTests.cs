//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Authentication.Utility;

namespace Microsoft.SqlTools.Authentication.UnitTests
{
    [TestFixture]
    public class EncryptionUtilsTests
    {
        [Test]
        public void AesEncryptDecrypt_ValidData_EncryptionDecryptionSuccessful()
        {
            // Arrange
            byte[] originalData = new byte[] { 0, 1, 2, 3, 4 };
            byte[] key = new byte[32]; // 256-bit key
            byte[] iv = new byte[16]; // 128-bit IV

            // Act
            byte[] encryptedData = EncryptionUtils.AesEncrypt(originalData, key, iv);
            byte[] decryptedData = EncryptionUtils.AesDecrypt(encryptedData, key, iv);

            // Assert
            CollectionAssert.AreEqual(originalData, decryptedData);
        }

        [Test]
        public void AesEncrypt_PaddingIsCorrectlyAdded()
        {
            // Arrange
            byte[] originalData = new byte[] { 0, 1, 2, 3, 4, 5 };
            byte[] key = new byte[32]; // 256-bit key
            byte[] iv = new byte[16]; // 128-bit IV

            // Act
            byte[] encryptedData = EncryptionUtils.AesEncrypt(originalData, key, iv);

            // Assert
            // Check if the encrypted data length is a multiple of the block size (128 bits or 16 bytes)
            int blockSizeInBytes = 16;
            int expectedPaddedLength = ((originalData.Length / blockSizeInBytes) + 1) * blockSizeInBytes;
            Assert.AreEqual(expectedPaddedLength, encryptedData.Length);
        }

        [Test]
        public void AesEncryptDecrypt_LargeData_PaddingIsCorrectlyAdded_TransformationIsSuccessful()
        {
            // Arrange
            byte[] originalData = new byte[2096];
            for (int i = 0; i < 2096; i++)
            {
                originalData[i] = (byte)Random.Shared.Next(0, 4);
            }
            byte[] key = new byte[32]; // 256-bit key
            byte[] iv = new byte[16]; // 128-bit IV

            // Act
            byte[] encryptedData = EncryptionUtils.AesEncrypt(originalData, key, iv);

            // Assert
            // Check if the encrypted data length is a multiple of the block size (128 bits or 16 bytes)
            int blockSizeInBytes = 16;
            int expectedPaddedLength = ((originalData.Length / blockSizeInBytes) + 1) * blockSizeInBytes;
            Assert.AreEqual(expectedPaddedLength, encryptedData.Length);

            // Act
            byte[] decryptedData = EncryptionUtils.AesDecrypt(encryptedData, key, iv);

            //Assert
            CollectionAssert.AreEqual(originalData, decryptedData);
        }

        [Test]
        public void AesEncrypt_NullPlainText_ThrowsArgumentNullException()
        {
            // Arrange
            byte[] key = new byte[32]; // 256-bit key
            byte[] iv = new byte[16]; // 128-bit IV

            // Act
            Assert.Throws<ArgumentNullException>(() => EncryptionUtils.AesEncrypt(null, key, iv));
        }

        [Test]
        public void AesEncrypt_NullIV_ThrowsArgumentNullException()
        {
            // Arrange
            byte[] key = new byte[32]; // 256-bit key
            byte[] data = new byte[32]; // 256-bit data

            // Act
            Assert.Throws<ArgumentNullException>(() => EncryptionUtils.AesEncrypt(data, key, null));
        }

        [Test]
        public void AesEncrypt_NullKey_ThrowsArgumentNullException()
        {
            // Arrange
            byte[] iv = new byte[16]; // 128-bit IV
            byte[] data = new byte[32]; // 256-bit data

            // Act
            Assert.Throws<ArgumentNullException>(() => EncryptionUtils.AesEncrypt(data, null, iv));
        }

        [Test]
        public void AesDecrypt_NullCipherText_ThrowsArgumentNullException()
        {
            // Arrange
            byte[] key = new byte[32]; // 256-bit key
            byte[] iv = new byte[16]; // 128-bit IV

            // Act
            Assert.Throws<ArgumentNullException>(() => EncryptionUtils.AesDecrypt(null, key, iv));
        }

        [Test]
        public void AesDecrypt_NullIV_ThrowsArgumentNullException()
        {
            // Arrange
            byte[] key = new byte[32]; // 256-bit key
            byte[] data = new byte[32]; // 256-bit data

            // Act
            Assert.Throws<ArgumentNullException>(() => EncryptionUtils.AesDecrypt(data, key, null));
        }

        [Test]
        public void AesDecrypt_NullKey_ThrowsArgumentNullException()
        {
            // Arrange
            byte[] iv = new byte[16]; // 128-bit IV
            byte[] data = new byte[32]; // 256-bit data

            // Act
            Assert.Throws<ArgumentNullException>(() => EncryptionUtils.AesDecrypt(data, null, iv));
        }
    }
}
