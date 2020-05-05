//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.InteropServices;

namespace Microsoft.SqlTools.Credentials
{
    internal partial class Interop
    {
        internal partial class Security
        {
            [DllImport(Libraries.SecurityLibrary, CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern OSStatus SecItemCopyMatching(IntPtr query, [Out] IntPtr result);

            [DllImport(Libraries.SecurityLibrary, CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern OSStatus SecKeychainAddGenericPassword(IntPtr keyChainRef, UInt32 serviceNameLength, string serviceName,
                UInt32 accountNameLength, string accountName, UInt32 passwordLength, IntPtr password, [Out] IntPtr itemRef);

            /// <summary>
            /// Find a generic password based on the attributes passed            
            /// </summary>
            /// <param name="keyChainRef">
            /// A reference to an array of keychains to search, a single keychain, or NULL to search the user's default keychain search list.
            /// </param>
            /// <param name="serviceNameLength">The length of the buffer pointed to by serviceName.</param>
            /// <param name="serviceName">A pointer to a string containing the service name.</param>
            /// <param name="accountNameLength">The length of the buffer pointed to by accountName.</param>
            /// <param name="accountName">A pointer to a string containing the account name.</param>
            /// <param name="passwordLength">On return, the length of the buffer pointed to by passwordData.</param>
            /// <param name="password">
            /// On return, a pointer to a data buffer containing the password. 
            /// Your application must call SecKeychainItemFreeContent(NULL, passwordData) 
            /// to release this data buffer when it is no longer needed.Pass NULL if you are not interested in retrieving the password data at
            /// this time, but simply want to find the item reference.
            /// </param>
            /// <param name="itemRef">On return, a reference to the keychain item which was found.</param>
            /// <returns>A result code that should be in <see cref="OSStatus"/></returns>
            /// <remarks>
            /// The SecKeychainFindGenericPassword function finds the first generic password item which matches the attributes you provide. 
            /// Most attributes are optional; you should pass only as many as you need to narrow the search sufficiently for your application's intended use. 
            /// SecKeychainFindGenericPassword optionally returns a reference to the found item.
            /// </remarks>
            [DllImport(Libraries.SecurityLibrary, CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern OSStatus SecKeychainFindGenericPassword(IntPtr keyChainRef, UInt32 serviceNameLength, string serviceName,
                UInt32 accountNameLength, string accountName, out UInt32 passwordLength, out IntPtr password, out IntPtr itemRef);

            /// <summary>
            /// Releases the memory used by the keychain attribute list and the keychain data retrieved in a previous call to SecKeychainItemCopyContent.
            /// </summary>
            /// <param name="attrList">A pointer to the attribute list to release. Pass NULL to ignore this parameter.</param>
            /// <param name="data">A pointer to the data buffer to release. Pass NULL to ignore this parameter.</param>
            /// <returns>A result code that should be in <see cref="OSStatus"/></returns>
            [DllImport(Libraries.SecurityLibrary, SetLastError = true)]
            internal static extern OSStatus SecKeychainItemFreeContent([In] IntPtr attrList, [In] IntPtr data);

            /// <summary>
            /// Deletes a keychain item from the default keychain's permanent data store.
            /// </summary>
            /// <param name="itemRef">A keychain item reference of the item to delete.</param>
            /// <returns>A result code that should be in <see cref="OSStatus"/></returns>
            /// <remarks>
            /// If itemRef has not previously been added to the keychain, SecKeychainItemDelete does nothing and returns ErrSecSuccess. 
            /// IMPORTANT: SecKeychainItemDelete does not dispose the memory occupied by the item reference itself; 
            /// use the CFRelease function when you are completely * * finished with an item.
            /// </remarks>
            [DllImport(Libraries.SecurityLibrary, SetLastError = true)]
            internal static extern OSStatus SecKeychainItemDelete(SafeHandle itemRef);

            #region OSStatus Codes
            /// <summary>Common Unix errno error codes.</summary>
            internal enum OSStatus
            {
                ErrSecSuccess = 0,       /* No error. */
                ErrSecUnimplemented = -4,      /* Function or operation not implemented. */
                ErrSecDskFull = -34,
                ErrSecIO = -36,     /*I/O error*/

                ErrSecParam = -50,     /* One or more parameters passed to a function were not valid. */
                ErrSecWrPerm = -61,     /* write permissions error*/
                ErrSecAllocate = -108,    /* Failed to allocate memory. */
                ErrSecUserCanceled = -128,    /* User canceled the operation. */
                ErrSecBadReq = -909,    /* Bad parameter or invalid state for operation. */

                ErrSecInternalComponent = -2070,
                ErrSecCoreFoundationUnknown = -4960,

                ErrSecNotAvailable = -25291,  /* No keychain is available. You may need to restart your computer. */
                ErrSecReadOnly = -25292,  /* This keychain cannot be modified. */
                ErrSecAuthFailed = -25293,  /* The user name or passphrase you entered is not correct. */
                ErrSecNoSuchKeychain = -25294,  /* The specified keychain could not be found. */
                ErrSecInvalidKeychain = -25295,  /* The specified keychain is not a valid keychain file. */
                ErrSecDuplicateKeychain = -25296,  /* A keychain with the same name already exists. */
                ErrSecDuplicateCallback = -25297,  /* The specified callback function is already installed. */
                ErrSecInvalidCallback = -25298,  /* The specified callback function is not valid. */
                ErrSecDuplicateItem = -25299,  /* The specified item already exists in the keychain. */
                ErrSecItemNotFound = -25300,  /* The specified item could not be found in the keychain. */
                ErrSecBufferTooSmall = -25301,  /* There is not enough memory available to use the specified item. */
                ErrSecDataTooLarge = -25302,  /* This item contains information which is too large or in a format that cannot be displayed. */
                ErrSecNoSuchAttr = -25303,  /* The specified attribute does not exist. */
                ErrSecInvalidItemRef = -25304,  /* The specified item is no longer valid. It may have been deleted from the keychain. */
                ErrSecInvalidSearchRef = -25305,  /* Unable to search the current keychain. */
                ErrSecNoSuchClass = -25306,  /* The specified item does not appear to be a valid keychain item. */
                ErrSecNoDefaultKeychain = -25307,  /* A default keychain could not be found. */
                ErrSecInteractionNotAllowed = -25308,  /* User interaction is not allowed. */
                ErrSecReadOnlyAttr = -25309,  /* The specified attribute could not be modified. */
                ErrSecWrongSecVersion = -25310,  /* This keychain was created by a different version of the system software and cannot be opened. */
                ErrSecKeySizeNotAllowed = -25311,  /* This item specifies a key size which is too large. */
                ErrSecNoStorageModule = -25312,  /* A required component (data storage module) could not be loaded. You may need to restart your computer. */
                ErrSecNoCertificateModule = -25313,  /* A required component (certificate module) could not be loaded. You may need to restart your computer. */
                ErrSecNoPolicyModule = -25314,  /* A required component (policy module) could not be loaded. You may need to restart your computer. */
                ErrSecInteractionRequired = -25315,  /* User interaction is required, but is currently not allowed. */
                ErrSecDataNotAvailable = -25316,  /* The contents of this item cannot be retrieved. */
                ErrSecDataNotModifiable = -25317,  /* The contents of this item cannot be modified. */
                ErrSecCreateChainFailed = -25318,  /* One or more certificates required to validate this certificate cannot be found. */
                ErrSecInvalidPrefsDomain = -25319,  /* The specified preferences domain is not valid. */
                ErrSecInDarkWake = -25320,  /* In dark wake, no UI possible */

                ErrSecACLNotSimple = -25240,  /* The specified access control list is not in standard (simple) form. */
                ErrSecPolicyNotFound = -25241,  /* The specified policy cannot be found. */
                ErrSecInvalidTrustSetting = -25242,  /* The specified trust setting is invalid. */
                ErrSecNoAccessForItem = -25243,  /* The specified item has no access control. */
                ErrSecInvalidOwnerEdit = -25244,  /* Invalid attempt to change the owner of this item. */
                ErrSecTrustNotAvailable = -25245,  /* No trust results are available. */
                ErrSecUnsupportedFormat = -25256,  /* Import/Export format unsupported. */
                ErrSecUnknownFormat = -25257,  /* Unknown format in import. */
                ErrSecKeyIsSensitive = -25258,  /* Key material must be wrapped for export. */
                ErrSecMultiplePrivKeys = -25259,  /* An attempt was made to import multiple private keys. */
                ErrSecPassphraseRequired = -25260,  /* Passphrase is required for import/export. */
                ErrSecInvalidPasswordRef = -25261,  /* The password reference was invalid. */
                ErrSecInvalidTrustSettings = -25262,  /* The Trust Settings Record was corrupted. */
                ErrSecNoTrustSettings = -25263,  /* No Trust Settings were found. */
                ErrSecPkcs12VerifyFailure = -25264,  /* MAC verification failed during PKCS12 import (wrong password?) */
                ErrSecNotSigner = -26267,  /* A certificate was not signed by its proposed parent. */

                ErrSecDecode = -26275,  /* Unable to decode the provided data. */

                ErrSecServiceNotAvailable = -67585,   /* The required service is not available. */
                ErrSecInsufficientClientID = -67586,   /* The client ID is not correct. */
                ErrSecDeviceReset = -67587,   /* A device reset has occurred. */
                ErrSecDeviceFailed = -67588,   /* A device failure has occurred. */
                ErrSecAppleAddAppACLSubject = -67589,   /* Adding an application ACL subject failed. */
                ErrSecApplePublicKeyIncomplete = -67590,   /* The public key is incomplete. */
                ErrSecAppleSignatureMismatch = -67591,   /* A signature mismatch has occurred. */
                ErrSecAppleInvalidKeyStartDate = -67592,   /* The specified key has an invalid start date. */
                ErrSecAppleInvalidKeyEndDate = -67593,   /* The specified key has an invalid end date. */
                ErrSecConversionError = -67594,   /* A conversion error has occurred. */
                ErrSecAppleSSLv2Rollback = -67595,   /* A SSLv2 rollback error has occurred. */
                ErrSecDiskFull = -34,      /* The disk is full. */
                ErrSecQuotaExceeded = -67596,   /* The quota was exceeded. */
                ErrSecFileTooBig = -67597,   /* The file is too big. */
                ErrSecInvalidDatabaseBlob = -67598,   /* The specified database has an invalid blob. */
                ErrSecInvalidKeyBlob = -67599,   /* The specified database has an invalid key blob. */
                ErrSecIncompatibleDatabaseBlob = -67600,   /* The specified database has an incompatible blob. */
                ErrSecIncompatibleKeyBlob = -67601,   /* The specified database has an incompatible key blob. */
                ErrSecHostNameMismatch = -67602,   /* A host name mismatch has occurred. */
                ErrSecUnknownCriticalExtensionFlag = -67603,   /* There is an unknown critical extension flag. */
                ErrSecNoBasicConstraints = -67604,   /* No basic constraints were found. */
                ErrSecNoBasicConstraintsCA = -67605,   /* No basic CA constraints were found. */
                ErrSecInvalidAuthorityKeyID = -67606,   /* The authority key ID is not valid. */
                ErrSecInvalidSubjectKeyID = -67607,   /* The subject key ID is not valid. */
                ErrSecInvalidKeyUsageForPolicy = -67608,   /* The key usage is not valid for the specified policy. */
                ErrSecInvalidExtendedKeyUsage = -67609,   /* The extended key usage is not valid. */
                ErrSecInvalidIDLinkage = -67610,   /* The ID linkage is not valid. */
                ErrSecPathLengthConstraintExceeded = -67611,   /* The path length constraint was exceeded. */
                ErrSecInvalidRoot = -67612,   /* The root or anchor certificate is not valid. */
                ErrSecCRLExpired = -67613,   /* The CRL has expired. */
                ErrSecCRLNotValidYet = -67614,   /* The CRL is not yet valid. */
                ErrSecCRLNotFound = -67615,   /* The CRL was not found. */
                ErrSecCRLServerDown = -67616,   /* The CRL server is down. */
                ErrSecCRLBadURI = -67617,   /* The CRL has a bad Uniform Resource Identifier. */
                ErrSecUnknownCertExtension = -67618,   /* An unknown certificate extension was encountered. */
                ErrSecUnknownCRLExtension = -67619,   /* An unknown CRL extension was encountered. */
                ErrSecCRLNotTrusted = -67620,   /* The CRL is not trusted. */
                ErrSecCRLPolicyFailed = -67621,   /* The CRL policy failed. */
                ErrSecIDPFailure = -67622,   /* The issuing distribution point was not valid. */
                ErrSecSMIMEEmailAddressesNotFound = -67623,   /* An email address mismatch was encountered. */
                ErrSecSMIMEBadExtendedKeyUsage = -67624,   /* The appropriate extended key usage for SMIME was not found. */
                ErrSecSMIMEBadKeyUsage = -67625,   /* The key usage is not compatible with SMIME. */
                ErrSecSMIMEKeyUsageNotCritical = -67626,   /* The key usage extension is not marked as critical. */
                ErrSecSMIMENoEmailAddress = -67627,   /* No email address was found in the certificate. */
                ErrSecSMIMESubjAltNameNotCritical = -67628,   /* The subject alternative name extension is not marked as critical. */
                ErrSecSSLBadExtendedKeyUsage = -67629,   /* The appropriate extended key usage for SSL was not found. */
                ErrSecOCSPBadResponse = -67630,   /* The OCSP response was incorrect or could not be parsed. */
                ErrSecOCSPBadRequest = -67631,   /* The OCSP request was incorrect or could not be parsed. */
                ErrSecOCSPUnavailable = -67632,   /* OCSP service is unavailable. */
                ErrSecOCSPStatusUnrecognized = -67633,   /* The OCSP server did not recognize this certificate. */
                ErrSecEndOfData = -67634,   /* An end-of-data was detected. */
                ErrSecIncompleteCertRevocationCheck = -67635,   /* An incomplete certificate revocation check occurred. */
                ErrSecNetworkFailure = -67636,   /* A network failure occurred. */
                ErrSecOCSPNotTrustedToAnchor = -67637,   /* The OCSP response was not trusted to a root or anchor certificate. */
                ErrSecRecordModified = -67638,   /* The record was modified. */
                ErrSecOCSPSignatureError = -67639,   /* The OCSP response had an invalid signature. */
                ErrSecOCSPNoSigner = -67640,   /* The OCSP response had no signer. */
                ErrSecOCSPResponderMalformedReq = -67641,   /* The OCSP responder was given a malformed request. */
                ErrSecOCSPResponderInternalError = -67642,   /* The OCSP responder encountered an internal error. */
                ErrSecOCSPResponderTryLater = -67643,   /* The OCSP responder is busy, try again later. */
                ErrSecOCSPResponderSignatureRequired = -67644,   /* The OCSP responder requires a signature. */
                ErrSecOCSPResponderUnauthorized = -67645,   /* The OCSP responder rejected this request as unauthorized. */
                ErrSecOCSPResponseNonceMismatch = -67646,   /* The OCSP response nonce did not match the request. */
                ErrSecCodeSigningBadCertChainLength = -67647,   /* Code signing encountered an incorrect certificate chain length. */
                ErrSecCodeSigningNoBasicConstraints = -67648,   /* Code signing found no basic constraints. */
                ErrSecCodeSigningBadPathLengthConstraint = -67649,   /* Code signing encountered an incorrect path length constraint. */
                ErrSecCodeSigningNoExtendedKeyUsage = -67650,   /* Code signing found no extended key usage. */
                ErrSecCodeSigningDevelopment = -67651,   /* Code signing indicated use of a development-only certificate. */
                ErrSecResourceSignBadCertChainLength = -67652,   /* Resource signing has encountered an incorrect certificate chain length. */
                ErrSecResourceSignBadExtKeyUsage = -67653,   /* Resource signing has encountered an error in the extended key usage. */
                ErrSecTrustSettingDeny = -67654,   /* The trust setting for this policy was set to Deny. */
                ErrSecInvalidSubjectName = -67655,   /* An invalid certificate subject name was encountered. */
                ErrSecUnknownQualifiedCertStatement = -67656,   /* An unknown qualified certificate statement was encountered. */
                ErrSecMobileMeRequestQueued = -67657,   /* The MobileMe request will be sent during the next connection. */
                ErrSecMobileMeRequestRedirected = -67658,   /* The MobileMe request was redirected. */
                ErrSecMobileMeServerError = -67659,   /* A MobileMe server error occurred. */
                ErrSecMobileMeServerNotAvailable = -67660,   /* The MobileMe server is not available. */
                ErrSecMobileMeServerAlreadyExists = -67661,   /* The MobileMe server reported that the item already exists. */
                ErrSecMobileMeServerServiceErr = -67662,   /* A MobileMe service error has occurred. */
                ErrSecMobileMeRequestAlreadyPending = -67663,   /* A MobileMe request is already pending. */
                ErrSecMobileMeNoRequestPending = -67664,   /* MobileMe has no request pending. */
                ErrSecMobileMeCSRVerifyFailure = -67665,   /* A MobileMe CSR verification failure has occurred. */
                ErrSecMobileMeFailedConsistencyCheck = -67666,   /* MobileMe has found a failed consistency check. */
                ErrSecNotInitialized = -67667,   /* A function was called without initializing CSSM. */
                ErrSecInvalidHandleUsage = -67668,   /* The CSSM handle does not match with the service type. */
                ErrSecPVCReferentNotFound = -67669,   /* A reference to the calling module was not found in the list of authorized callers. */
                ErrSecFunctionIntegrityFail = -67670,   /* A function address was not within the verified module. */
                ErrSecInternalError = -67671,   /* An internal error has occurred. */
                ErrSecMemoryError = -67672,   /* A memory error has occurred. */
                ErrSecInvalidData = -67673,   /* Invalid data was encountered. */
                ErrSecMDSError = -67674,   /* A Module Directory Service error has occurred. */
                ErrSecInvalidPointer = -67675,   /* An invalid pointer was encountered. */
                ErrSecSelfCheckFailed = -67676,   /* Self-check has failed. */
                ErrSecFunctionFailed = -67677,   /* A function has failed. */
                ErrSecModuleManifestVerifyFailed = -67678,   /* A module manifest verification failure has occurred. */
                ErrSecInvalidGUID = -67679,   /* An invalid GUID was encountered. */
                ErrSecInvalidHandle = -67680,   /* An invalid handle was encountered. */
                ErrSecInvalidDBList = -67681,   /* An invalid DB list was encountered. */
                ErrSecInvalidPassthroughID = -67682,   /* An invalid passthrough ID was encountered. */
                ErrSecInvalidNetworkAddress = -67683,   /* An invalid network address was encountered. */
                ErrSecCRLAlreadySigned = -67684,   /* The certificate revocation list is already signed. */
                ErrSecInvalidNumberOfFields = -67685,   /* An invalid number of fields were encountered. */
                ErrSecVerificationFailure = -67686,   /* A verification failure occurred. */
                ErrSecUnknownTag = -67687,   /* An unknown tag was encountered. */
                ErrSecInvalidSignature = -67688,   /* An invalid signature was encountered. */
                ErrSecInvalidName = -67689,   /* An invalid name was encountered. */
                ErrSecInvalidCertificateRef = -67690,   /* An invalid certificate reference was encountered. */
                ErrSecInvalidCertificateGroup = -67691,   /* An invalid certificate group was encountered. */
                ErrSecTagNotFound = -67692,   /* The specified tag was not found. */
                ErrSecInvalidQuery = -67693,   /* The specified query was not valid. */
                ErrSecInvalidValue = -67694,   /* An invalid value was detected. */
                ErrSecCallbackFailed = -67695,   /* A callback has failed. */
                ErrSecACLDeleteFailed = -67696,   /* An ACL delete operation has failed. */
                ErrSecACLReplaceFailed = -67697,   /* An ACL replace operation has failed. */
                ErrSecACLAddFailed = -67698,   /* An ACL add operation has failed. */
                ErrSecACLChangeFailed = -67699,   /* An ACL change operation has failed. */
                ErrSecInvalidAccessCredentials = -67700,   /* Invalid access credentials were encountered. */
                ErrSecInvalidRecord = -67701,   /* An invalid record was encountered. */
                ErrSecInvalidACL = -67702,   /* An invalid ACL was encountered. */
                ErrSecInvalidSampleValue = -67703,   /* An invalid sample value was encountered. */
                ErrSecIncompatibleVersion = -67704,   /* An incompatible version was encountered. */
                ErrSecPrivilegeNotGranted = -67705,   /* The privilege was not granted. */
                ErrSecInvalidScope = -67706,   /* An invalid scope was encountered. */
                ErrSecPVCAlreadyConfigured = -67707,   /* The PVC is already configured. */
                ErrSecInvalidPVC = -67708,   /* An invalid PVC was encountered. */
                ErrSecEMMLoadFailed = -67709,   /* The EMM load has failed. */
                ErrSecEMMUnloadFailed = -67710,   /* The EMM unload has failed. */
                ErrSecAddinLoadFailed = -67711,   /* The add-in load operation has failed. */
                ErrSecInvalidKeyRef = -67712,   /* An invalid key was encountered. */
                ErrSecInvalidKeyHierarchy = -67713,   /* An invalid key hierarchy was encountered. */
                ErrSecAddinUnloadFailed = -67714,   /* The add-in unload operation has failed. */
                ErrSecLibraryReferenceNotFound = -67715,   /* A library reference was not found. */
                ErrSecInvalidAddinFunctionTable = -67716,   /* An invalid add-in function table was encountered. */
                ErrSecInvalidServiceMask = -67717,   /* An invalid service mask was encountered. */
                ErrSecModuleNotLoaded = -67718,   /* A module was not loaded. */
                ErrSecInvalidSubServiceID = -67719,   /* An invalid subservice ID was encountered. */
                ErrSecAttributeNotInContext = -67720,   /* An attribute was not in the context. */
                ErrSecModuleManagerInitializeFailed = -67721,   /* A module failed to initialize. */
                ErrSecModuleManagerNotFound = -67722,   /* A module was not found. */
                ErrSecEventNotificationCallbackNotFound = -67723,   /* An event notification callback was not found. */
                ErrSecInputLengthError = -67724,   /* An input length error was encountered. */
                ErrSecOutputLengthError = -67725,   /* An output length error was encountered. */
                ErrSecPrivilegeNotSupported = -67726,   /* The privilege is not supported. */
                ErrSecDeviceError = -67727,   /* A device error was encountered. */
                ErrSecAttachHandleBusy = -67728,   /* The CSP handle was busy. */
                ErrSecNotLoggedIn = -67729,   /* You are not logged in. */
                ErrSecAlgorithmMismatch = -67730,   /* An algorithm mismatch was encountered. */
                ErrSecKeyUsageIncorrect = -67731,   /* The key usage is incorrect. */
                ErrSecKeyBlobTypeIncorrect = -67732,   /* The key blob type is incorrect. */
                ErrSecKeyHeaderInconsistent = -67733,   /* The key header is inconsistent. */
                ErrSecUnsupportedKeyFormat = -67734,   /* The key header format is not supported. */
                ErrSecUnsupportedKeySize = -67735,   /* The key size is not supported. */
                ErrSecInvalidKeyUsageMask = -67736,   /* The key usage mask is not valid. */
                ErrSecUnsupportedKeyUsageMask = -67737,   /* The key usage mask is not supported. */
                ErrSecInvalidKeyAttributeMask = -67738,   /* The key attribute mask is not valid. */
                ErrSecUnsupportedKeyAttributeMask = -67739,   /* The key attribute mask is not supported. */
                ErrSecInvalidKeyLabel = -67740,   /* The key label is not valid. */
                ErrSecUnsupportedKeyLabel = -67741,   /* The key label is not supported. */
                ErrSecInvalidKeyFormat = -67742,   /* The key format is not valid. */
                ErrSecUnsupportedVectorOfBuffers = -67743,   /* The vector of buffers is not supported. */
                ErrSecInvalidInputVector = -67744,   /* The input vector is not valid. */
                ErrSecInvalidOutputVector = -67745,   /* The output vector is not valid. */
                ErrSecInvalidContext = -67746,   /* An invalid context was encountered. */
                ErrSecInvalidAlgorithm = -67747,   /* An invalid algorithm was encountered. */
                ErrSecInvalidAttributeKey = -67748,   /* A key attribute was not valid. */
                ErrSecMissingAttributeKey = -67749,   /* A key attribute was missing. */
                ErrSecInvalidAttributeInitVector = -67750,   /* An init vector attribute was not valid. */
                ErrSecMissingAttributeInitVector = -67751,   /* An init vector attribute was missing. */
                ErrSecInvalidAttributeSalt = -67752,   /* A salt attribute was not valid. */
                ErrSecMissingAttributeSalt = -67753,   /* A salt attribute was missing. */
                ErrSecInvalidAttributePadding = -67754,   /* A padding attribute was not valid. */
                ErrSecMissingAttributePadding = -67755,   /* A padding attribute was missing. */
                ErrSecInvalidAttributeRandom = -67756,   /* A random number attribute was not valid. */
                ErrSecMissingAttributeRandom = -67757,   /* A random number attribute was missing. */
                ErrSecInvalidAttributeSeed = -67758,   /* A seed attribute was not valid. */
                ErrSecMissingAttributeSeed = -67759,   /* A seed attribute was missing. */
                ErrSecInvalidAttributePassphrase = -67760,   /* A passphrase attribute was not valid. */
                ErrSecMissingAttributePassphrase = -67761,   /* A passphrase attribute was missing. */
                ErrSecInvalidAttributeKeyLength = -67762,   /* A key length attribute was not valid. */
                ErrSecMissingAttributeKeyLength = -67763,   /* A key length attribute was missing. */
                ErrSecInvalidAttributeBlockSize = -67764,   /* A block size attribute was not valid. */
                ErrSecMissingAttributeBlockSize = -67765,   /* A block size attribute was missing. */
                ErrSecInvalidAttributeOutputSize = -67766,   /* An output size attribute was not valid. */
                ErrSecMissingAttributeOutputSize = -67767,   /* An output size attribute was missing. */
                ErrSecInvalidAttributeRounds = -67768,   /* The number of rounds attribute was not valid. */
                ErrSecMissingAttributeRounds = -67769,   /* The number of rounds attribute was missing. */
                ErrSecInvalidAlgorithmParms = -67770,   /* An algorithm parameters attribute was not valid. */
                ErrSecMissingAlgorithmParms = -67771,   /* An algorithm parameters attribute was missing. */
                ErrSecInvalidAttributeLabel = -67772,   /* A label attribute was not valid. */
                ErrSecMissingAttributeLabel = -67773,   /* A label attribute was missing. */
                ErrSecInvalidAttributeKeyType = -67774,   /* A key type attribute was not valid. */
                ErrSecMissingAttributeKeyType = -67775,   /* A key type attribute was missing. */
                ErrSecInvalidAttributeMode = -67776,   /* A mode attribute was not valid. */
                ErrSecMissingAttributeMode = -67777,   /* A mode attribute was missing. */
                ErrSecInvalidAttributeEffectiveBits = -67778,   /* An effective bits attribute was not valid. */
                ErrSecMissingAttributeEffectiveBits = -67779,   /* An effective bits attribute was missing. */
                ErrSecInvalidAttributeStartDate = -67780,   /* A start date attribute was not valid. */
                ErrSecMissingAttributeStartDate = -67781,   /* A start date attribute was missing. */
                ErrSecInvalidAttributeEndDate = -67782,   /* An end date attribute was not valid. */
                ErrSecMissingAttributeEndDate = -67783,   /* An end date attribute was missing. */
                ErrSecInvalidAttributeVersion = -67784,   /* A version attribute was not valid. */
                ErrSecMissingAttributeVersion = -67785,   /* A version attribute was missing. */
                ErrSecInvalidAttributePrime = -67786,   /* A prime attribute was not valid. */
                ErrSecMissingAttributePrime = -67787,   /* A prime attribute was missing. */
                ErrSecInvalidAttributeBase = -67788,   /* A base attribute was not valid. */
                ErrSecMissingAttributeBase = -67789,   /* A base attribute was missing. */
                ErrSecInvalidAttributeSubprime = -67790,   /* A subprime attribute was not valid. */
                ErrSecMissingAttributeSubprime = -67791,   /* A subprime attribute was missing. */
                ErrSecInvalidAttributeIterationCount = -67792,   /* An iteration count attribute was not valid. */
                ErrSecMissingAttributeIterationCount = -67793,   /* An iteration count attribute was missing. */
                ErrSecInvalidAttributeDLDBHandle = -67794,   /* A database handle attribute was not valid. */
                ErrSecMissingAttributeDLDBHandle = -67795,   /* A database handle attribute was missing. */
                ErrSecInvalidAttributeAccessCredentials = -67796,   /* An access credentials attribute was not valid. */
                ErrSecMissingAttributeAccessCredentials = -67797,   /* An access credentials attribute was missing. */
                ErrSecInvalidAttributePublicKeyFormat = -67798,   /* A public key format attribute was not valid. */
                ErrSecMissingAttributePublicKeyFormat = -67799,   /* A public key format attribute was missing. */
                ErrSecInvalidAttributePrivateKeyFormat = -67800,   /* A private key format attribute was not valid. */
                ErrSecMissingAttributePrivateKeyFormat = -67801,   /* A private key format attribute was missing. */
                ErrSecInvalidAttributeSymmetricKeyFormat = -67802,   /* A symmetric key format attribute was not valid. */
                ErrSecMissingAttributeSymmetricKeyFormat = -67803,   /* A symmetric key format attribute was missing. */
                ErrSecInvalidAttributeWrappedKeyFormat = -67804,   /* A wrapped key format attribute was not valid. */
                ErrSecMissingAttributeWrappedKeyFormat = -67805,   /* A wrapped key format attribute was missing. */
                ErrSecStagedOperationInProgress = -67806,   /* A staged operation is in progress. */
                ErrSecStagedOperationNotStarted = -67807,   /* A staged operation was not started. */
                ErrSecVerifyFailed = -67808,   /* A cryptographic verification failure has occurred. */
                ErrSecQuerySizeUnknown = -67809,   /* The query size is unknown. */
                ErrSecBlockSizeMismatch = -67810,   /* A block size mismatch occurred. */
                ErrSecPublicKeyInconsistent = -67811,   /* The public key was inconsistent. */
                ErrSecDeviceVerifyFailed = -67812,   /* A device verification failure has occurred. */
                ErrSecInvalidLoginName = -67813,   /* An invalid login name was detected. */
                ErrSecAlreadyLoggedIn = -67814,   /* The user is already logged in. */
                ErrSecInvalidDigestAlgorithm = -67815,   /* An invalid digest algorithm was detected. */
                ErrSecInvalidCRLGroup = -67816,   /* An invalid CRL group was detected. */
                ErrSecCertificateCannotOperate = -67817,   /* The certificate cannot operate. */
                ErrSecCertificateExpired = -67818,   /* An expired certificate was detected. */
                ErrSecCertificateNotValidYet = -67819,   /* The certificate is not yet valid. */
                ErrSecCertificateRevoked = -67820,   /* The certificate was revoked. */
                ErrSecCertificateSuspended = -67821,   /* The certificate was suspended. */
                ErrSecInsufficientCredentials = -67822,   /* Insufficient credentials were detected. */
                ErrSecInvalidAction = -67823,   /* The action was not valid. */
                ErrSecInvalidAuthority = -67824,   /* The authority was not valid. */
                ErrSecVerifyActionFailed = -67825,   /* A verify action has failed. */
                ErrSecInvalidCertAuthority = -67826,   /* The certificate authority was not valid. */
                ErrSecInvaldCRLAuthority = -67827,   /* The CRL authority was not valid. */
                ErrSecInvalidCRLEncoding = -67828,   /* The CRL encoding was not valid. */
                ErrSecInvalidCRLType = -67829,   /* The CRL type was not valid. */
                ErrSecInvalidCRL = -67830,   /* The CRL was not valid. */
                ErrSecInvalidFormType = -67831,   /* The form type was not valid. */
                ErrSecInvalidID = -67832,   /* The ID was not valid. */
                ErrSecInvalidIdentifier = -67833,   /* The identifier was not valid. */
                ErrSecInvalidIndex = -67834,   /* The index was not valid. */
                ErrSecInvalidPolicyIdentifiers = -67835,   /* The policy identifiers are not valid. */
                ErrSecInvalidTimeString = -67836,   /* The time specified was not valid. */
                ErrSecInvalidReason = -67837,   /* The trust policy reason was not valid. */
                ErrSecInvalidRequestInputs = -67838,   /* The request inputs are not valid. */
                ErrSecInvalidResponseVector = -67839,   /* The response vector was not valid. */
                ErrSecInvalidStopOnPolicy = -67840,   /* The stop-on policy was not valid. */
                ErrSecInvalidTuple = -67841,   /* The tuple was not valid. */
                ErrSecMultipleValuesUnsupported = -67842,   /* Multiple values are not supported. */
                ErrSecNotTrusted = -67843,   /* The trust policy was not trusted. */
                ErrSecNoDefaultAuthority = -67844,   /* No default authority was detected. */
                ErrSecRejectedForm = -67845,   /* The trust policy had a rejected form. */
                ErrSecRequestLost = -67846,   /* The request was lost. */
                ErrSecRequestRejected = -67847,   /* The request was rejected. */
                ErrSecUnsupportedAddressType = -67848,   /* The address type is not supported. */
                ErrSecUnsupportedService = -67849,   /* The service is not supported. */
                ErrSecInvalidTupleGroup = -67850,   /* The tuple group was not valid. */
                ErrSecInvalidBaseACLs = -67851,   /* The base ACLs are not valid. */
                ErrSecInvalidTupleCredendtials = -67852,   /* The tuple credentials are not valid. */
                ErrSecInvalidEncoding = -67853,   /* The encoding was not valid. */
                ErrSecInvalidValidityPeriod = -67854,   /* The validity period was not valid. */
                ErrSecInvalidRequestor = -67855,   /* The requestor was not valid. */
                ErrSecRequestDescriptor = -67856,   /* The request descriptor was not valid. */
                ErrSecInvalidBundleInfo = -67857,   /* The bundle information was not valid. */
                ErrSecInvalidCRLIndex = -67858,   /* The CRL index was not valid. */
                ErrSecNoFieldValues = -67859,   /* No field values were detected. */
                ErrSecUnsupportedFieldFormat = -67860,   /* The field format is not supported. */
                ErrSecUnsupportedIndexInfo = -67861,   /* The index information is not supported. */
                ErrSecUnsupportedLocality = -67862,   /* The locality is not supported. */
                ErrSecUnsupportedNumAttributes = -67863,   /* The number of attributes is not supported. */
                ErrSecUnsupportedNumIndexes = -67864,   /* The number of indexes is not supported. */
                ErrSecUnsupportedNumRecordTypes = -67865,   /* The number of record types is not supported. */
                ErrSecFieldSpecifiedMultiple = -67866,   /* Too many fields were specified. */
                ErrSecIncompatibleFieldFormat = -67867,   /* The field format was incompatible. */
                ErrSecInvalidParsingModule = -67868,   /* The parsing module was not valid. */
                ErrSecDatabaseLocked = -67869,   /* The database is locked. */
                ErrSecDatastoreIsOpen = -67870,   /* The data store is open. */
                ErrSecMissingValue = -67871,   /* A missing value was detected. */
                ErrSecUnsupportedQueryLimits = -67872,   /* The query limits are not supported. */
                ErrSecUnsupportedNumSelectionPreds = -67873,   /* The number of selection predicates is not supported. */
                ErrSecUnsupportedOperator = -67874,   /* The operator is not supported. */
                ErrSecInvalidDBLocation = -67875,   /* The database location is not valid. */
                ErrSecInvalidAccessRequest = -67876,   /* The access request is not valid. */
                ErrSecInvalidIndexInfo = -67877,   /* The index information is not valid. */
                ErrSecInvalidNewOwner = -67878,   /* The new owner is not valid. */
                ErrSecInvalidModifyMode = -67879,   /* The modify mode is not valid. */
                ErrSecMissingRequiredExtension = -67880,   /* A required certificate extension is missing. */
                ErrSecExtendedKeyUsageNotCritical = -67881,   /* The extended key usage extension was not marked critical. */
                ErrSecTimestampMissing = -67882,   /* A timestamp was expected but was not found. */
                ErrSecTimestampInvalid = -67883,   /* The timestamp was not valid. */
                ErrSecTimestampNotTrusted = -67884,   /* The timestamp was not trusted. */
                ErrSecTimestampServiceNotAvailable = -67885,   /* The timestamp service is not available. */
                ErrSecTimestampBadAlg = -67886,   /* An unrecognized or unsupported Algorithm Identifier in timestamp. */
                ErrSecTimestampBadRequest = -67887,   /* The timestamp transaction is not permitted or supported. */
                ErrSecTimestampBadDataFormat = -67888,   /* The timestamp data submitted has the wrong format. */
                ErrSecTimestampTimeNotAvailable = -67889,   /* The time source for the Timestamp Authority is not available. */
                ErrSecTimestampUnacceptedPolicy = -67890,   /* The requested policy is not supported by the Timestamp Authority. */
                ErrSecTimestampUnacceptedExtension = -67891,   /* The requested extension is not supported by the Timestamp Authority. */
                ErrSecTimestampAddInfoNotAvailable = -67892,   /* The additional information requested is not available. */
                ErrSecTimestampSystemFailure = -67893,   /* The timestamp request cannot be handled due to system failure. */
                ErrSecSigningTimeMissing = -67894,   /* A signing time was expected but was not found. */
                ErrSecTimestampRejection = -67895,   /* A timestamp transaction was rejected. */
                ErrSecTimestampWaiting = -67896,   /* A timestamp transaction is waiting. */
                ErrSecTimestampRevocationWarning = -67897,   /* A timestamp authority revocation warning was issued. */
                ErrSecTimestampRevocationNotification = -67898,   /* A timestamp authority revocation notification was issued. */
            }

            #endregion
        }
    }
}

