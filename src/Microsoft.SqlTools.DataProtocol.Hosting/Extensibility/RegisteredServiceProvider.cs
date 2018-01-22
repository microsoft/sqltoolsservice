//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.SqlTools.DataProtocol.Hosting.Utility;

namespace Microsoft.SqlTools.DataProtocol.Hosting.Extensibility
{

    /// <summary>
    /// A service provider implementation that allows registering of specific services
    /// </summary>
    public class RegisteredServiceProvider : ServiceProviderBase
    {
        public delegate IEnumerable ServiceLookup();

        protected readonly Dictionary<Type, ServiceLookup> services = new Dictionary<Type, ServiceLookup>();

        /// <summary>
        /// Registers a singular service to be returned during lookup
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>this provider, to simplify fluent declarations</returns>
        /// <exception cref="ArgumentNullException">If service is null</exception>
        /// <exception cref="InvalidOperationException">If an existing service is already registered</exception>
        public virtual RegisteredServiceProvider RegisterSingleService<T>(T service)
        {
            Validate.IsNotNull(nameof(service), service);
            ThrowIfAlreadyRegistered<T>();
            services.Add(typeof(T), () => service.AsSingleItemEnumerable());
            return this;
        }

        /// <summary>
        /// Registers a singular service to be returned during lookup
        /// </summary>
        /// <param name="type">
        /// Type or interface this service should be registed as. Any <see cref="IMultiServiceProvider.GetServices{T}"/> request
        /// for that type will return this service
        /// </param>
        /// <param name="service">service object to be added</param>
        /// <returns>this provider, to simplify fluent declarations</returns>
        /// <exception cref="ArgumentNullException">If service is null</exception>
        /// <exception cref="InvalidOperationException">If an existing service is already registered</exception>
        public virtual RegisteredServiceProvider RegisterSingleService(Type type, object service)
        {
            Validate.IsNotNull(nameof(type), type);
            Validate.IsNotNull(nameof(service), service);
            ThrowIfAlreadyRegistered(type);
            ThrowIfIncompatible(type, service);
            services.Add(type, service.AsSingleItemEnumerable);
            return this;
        }

        /// <summary>
        /// Registers a function that can look up multiple services
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>this provider, to simplify fluent declarations</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="serviceLookup"/> is null</exception>
        /// <exception cref="InvalidOperationException">If an existing service is already registered</exception>
        public virtual RegisteredServiceProvider Register<T>(Func<IEnumerable<T>> serviceLookup)
        {
            Validate.IsNotNull(nameof(serviceLookup), serviceLookup);
            ThrowIfAlreadyRegistered<T>();
            services.Add(typeof(T), () => serviceLookup());
            return this;
        }

        public virtual void RegisterHostedServices()
        {
            // Register all hosted services the service provider for their requested service type.
            // This ensures that when searching for the ConnectionService (eg) you can get it
            // without searching for an IHosted service of type ConnectionService
            foreach (IHostedService service in GetServices<IHostedService>())
            {
                RegisterSingleService(service.ServiceType, service);
            }
        }
        
        private void ThrowIfAlreadyRegistered<T>()
        {
            ThrowIfAlreadyRegistered(typeof(T));
        }

        private void ThrowIfAlreadyRegistered(Type type)
        {
            if (services.ContainsKey(type))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, SR.ServiceAlreadyRegistered, type.Name));
            }

        }

        private void ThrowIfIncompatible(Type type, object service)
        {
            if (!type.IsInstanceOfType(service))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, SR.ServiceNotOfExpectedType, service.GetType().Name, type.Name));
            }

        }

        protected override IEnumerable<T> GetServicesImpl<T>()
        {
            ServiceLookup serviceLookup;
            if (services.TryGetValue(typeof(T), out serviceLookup))
            {
                return serviceLookup().Cast<T>();
            }
            return Enumerable.Empty<T>();
        }
    }
}
