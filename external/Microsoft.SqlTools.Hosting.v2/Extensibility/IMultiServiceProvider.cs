//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlTools.Hosting.Utility;

namespace Microsoft.SqlTools.Hosting.Extensibility
{
    public interface IMultiServiceProvider
    {
        /// <summary>
        /// Gets a service of a specific type. It is expected that only 1 instance of this type will be
        /// available
        /// </summary>
        /// <typeparam name="T">Type of service to be found</typeparam>
        /// <returns>Instance of T or null if not found</returns>
        /// <exception cref="InvalidOperationException">The input sequence contains more than one element.-or-The input sequence is empty.</exception>
        T GetService<T>();

        /// <summary>
        /// Gets a service of a specific type. The first service matching the specified filter will be returned
        /// available
        /// </summary>
        /// <typeparam name="T">Type of service to be found</typeparam>
        /// <param name="filter">Filter to use in </param>
        /// <returns>Instance of T or null if not found</returns>
        /// <exception cref="InvalidOperationException">The input sequence contains more than one element.-or-The input sequence is empty.</exception>
        T GetService<T>(Predicate<T> filter);

        /// <summary>
        /// Gets multiple services of a given type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>An enumerable of matching services</returns>
        IEnumerable<T> GetServices<T>();

        /// <summary>
        /// Gets multiple services of a given type, where they match a filter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filter"></param>
        /// <returns></returns>
        IEnumerable<T> GetServices<T>(Predicate<T> filter);
    }
    
    
    public abstract class ServiceProviderBase : IMultiServiceProvider
    {

        public T GetService<T>()
        {
            return GetServices<T>().SingleOrDefault();
        }

        public T GetService<T>(Predicate<T> filter)
        {
            Validate.IsNotNull(nameof(filter), filter);
            return GetServices<T>().SingleOrDefault(t => filter(t));
        }

        public IEnumerable<T> GetServices<T>(Predicate<T> filter)
        {
            Validate.IsNotNull(nameof(filter), filter);
            return GetServices<T>().Where(t => filter(t));
        }

        public virtual IEnumerable<T> GetServices<T>()
        {
            var services = GetServicesImpl<T>();
            if (services == null)
            {
                return Enumerable.Empty<T>();
            }

            return services.Select(t =>
            {
                InitComposableService(t);
                return t;
            });
        }

        private void InitComposableService<T>(T t)
        {
            IComposableService c = t as IComposableService;
            c?.SetServiceProvider(this);
        }
        
        /// <summary>
        /// Gets all services using the build in implementation
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected abstract IEnumerable<T> GetServicesImpl<T>();

    }

}
