//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.SqlTools.Azure.Core
{
    /// <summary>
    /// Contains the data that a service wants to returns plus the errors happened during getting some of the data
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ServiceResponse<T>
    {
        /// <summary>
        /// Creates new instance given data and errors
        /// </summary>
        public ServiceResponse(IEnumerable<T> data, IEnumerable<Exception> errors)
        {
            Data = data;
            Errors = errors;
        }

        /// <summary>
        /// Creates new instance given errors
        /// </summary>
        public ServiceResponse(IEnumerable<Exception> errors) : this(Enumerable.Empty<T>(), errors)
        {           
        }

        /// <summary>
        /// Creates new instance given data
        /// </summary>
        public ServiceResponse(IEnumerable<T> data) : this(data, Enumerable.Empty<Exception>())
        {           
        }

        /// <summary>
        /// Creating new empry instance
        /// </summary>
        public ServiceResponse() : this(Enumerable.Empty<T>(), Enumerable.Empty<Exception>())
        {
        }

        /// <summary>
        /// Creates new instance given exception to create the error list
        /// </summary>
        public ServiceResponse(Exception ex) : this(Enumerable.Empty<T>(), new List<Exception> {ex})
        {
        }

        /// <summary>
        /// Information a service wants to returns
        /// </summary>
        public IEnumerable<T> Data { get; private set; }

        /// <summary>
        /// The errors that heppend during retrieving data
        /// </summary>
        public IEnumerable<Exception> Errors { get; private set; }

        /// <summary>
        /// Return true if the response includes errors
        /// </summary>
        public bool HasError
        {
            get { return Errors != null && Errors.Any(); }
        }

        /// <summary>
        /// Concatenates the messages into one error message
        /// </summary>
        public string ErrorMessage
        {
            get
            {
                string message = string.Empty;
                if (HasError)
                {
                    message = string.Join(Environment.NewLine, Errors.Select(x => x.GetExceptionMessage()));
                }
                return message;
            }
        }      

        /// <summary>
        /// Returns true if a response already found. it's used when we need to filter the responses
        /// </summary>
        public bool Found { get; set; }
    }
}
