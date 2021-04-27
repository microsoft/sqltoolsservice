//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.Hosting.Contracts;

namespace Microsoft.SqlTools.DataProtocol.Contracts
{
    /// <summary>
    /// Parameters provided with an initialize request with extra options provided by the client
    /// </summary>
    /// <typeparam name="TInitializationOptions">
    /// Type of the initialization options expected from the client, can be any reference type
    /// </typeparam>
    public class InitializeParametersWithOptions<TInitializationOptions> : InitializeParameters where TInitializationOptions : class
    {
        /// <summary>
        /// Initialization options provided by the client, can be null
        /// </summary>
        public TInitializationOptions InitializationOptions { get; set; }
    }

    /// <summary>
    /// Request definition for initialization of the server. This version provides optional extra
    /// parameters that can be used to pass extra information between client and server during
    /// initialization. If these extra options aren't needed, use <see cref="InitializeRequest"/>.
    /// </summary>
    /// <typeparam name="TInitializeOptions">
    /// Type to use for extra parameters in the initialize request. These appear as
    /// <see cref="InitializeParametersWithOptions{TInitializationOptions}.InitializationOptions"/>.
    /// If these extra options are not expected, but extra response options are, simply use
    /// <c>object</c> as the type. 
    /// </typeparam>
    /// <typeparam name="TInitializeResponse">
    /// Type to use for the initialize response. Due to VSCode's protocol definition, any extra
    /// params that should be sent back to the client are defined as dictionary of key/value pairs.
    /// This can be emulated by defining a class that extends <see cref="InitializeResponse"/> with
    /// whatever extra params are expected. If no extra options are needed, but extra request
    /// options are, simply use <see cref="InitializeResponse"/> as the type.
    /// </typeparam>
    public class InitializeRequestWithOptions<TInitializeOptions, TInitializeResponse> 
        where TInitializeOptions : class
        where TInitializeResponse : InitializeResponse
    {
        public static readonly RequestType<InitializeParametersWithOptions<TInitializeOptions>, TInitializeResponse> Type =
            RequestType<InitializeParametersWithOptions<TInitializeOptions>, TInitializeResponse>.Create("initialize");
    }
}