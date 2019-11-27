// <copyright file="ValidationUtils.cs" company="Microsoft">
// Copyright (c) Microsoft. All Rights Reserved.
// </copyright>

namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    /// <summary>
    /// Represents validation utilities.
    /// </summary>
    public static class ValidationUtils
    {
        /// <summary>
        /// Validates whether an argument is not null.
        /// </summary>
        /// <param name="param">The parameter.</param>
        /// <param name="paramName">The parameter name.</param>
        public static void IsArgumentNotNull(object param, [Localizable(false)] string paramName)
        {
            if (param == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        /// <summary>
        /// Validates whether a string argument is not null or white space.
        /// </summary>
        /// <param name="param">The parameter.</param>
        /// <param name="paramName">The parameter name.</param>
        public static void IsArgumentNotNullOrWhiteSpace(string param, [Localizable(false)] string paramName)
        {
            if (string.IsNullOrWhiteSpace(param))
            {
                throw new ArgumentNullException(paramName, $"{paramName} cannot be null or white space.");
            }
        }

        /// <summary>
        /// Validates whether a string argument is null or empty.
        /// </summary>
        /// <param name="param">The parameter.</param>
        /// <param name="paramName">The parameter name.</param>
        public static void IsArgumentNullOrEmpty(string param, [Localizable(false)] string paramName)
        {
            if (!string.IsNullOrEmpty(param))
            {
                throw new ArgumentException($"{paramName} must be null or empty.", paramName);
            }
        }

        /// <summary>
        /// Validates whether an argument equals an expected value.
        /// </summary>
        /// <typeparam name="T">The type of the argument.</typeparam>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        /// <param name="paramName"></param>
        /// <param name="comparer"></param>
        public static void IsArgumentNotEqual<T>(T actual, T expected, [Localizable(false)] string paramName, IEqualityComparer<T> comparer = null)
        {
            comparer = comparer ?? EqualityComparer<T>.Default;
            if (comparer.Equals(actual, expected))
            {
                throw new ArgumentException($"{paramName} is unsupported: '{actual}'.", paramName);
            }
        }

        /// <summary>
        /// Validates whether an argument equals an expected value.
        /// </summary>
        /// <typeparam name="T">The type of the argument.</typeparam>
        /// <param name="actual"></param>
        /// <param name="expected"></param>
        /// <param name="paramName"></param>
        /// <param name="comparer"></param>
        public static void IsArgumentEqual<T>(T actual, T expected, [Localizable(false)] string paramName, IEqualityComparer<T> comparer = null)
        {
            comparer = comparer ?? EqualityComparer<T>.Default;
            if (!comparer.Equals(actual, expected))
            {
                throw new ArgumentException($"{paramName} value is unexpected: actual '{actual}', expected '{expected}'.", paramName);
            }
        }

        /// <summary>
        /// Validates whether an object is not null.
        /// </summary>
        /// <param name="value">The object value.</param>
        /// <param name="name">The object name.  May optionally include an exception message.</param>
        /// <exception cref="InvalidOperationException">The object is null.</exception>
        public static void IsNotNull(object value, [Localizable(false)] string name)
        {
            if (value == null)
            {
                throw new InvalidOperationException($"{name} cannot be null.");
            }
        }

        /// <summary>
        /// Validates whether an object is not null.
        /// </summary>
        /// <typeparam name="TException">The type of the exception to throw.</typeparam>
        /// <param name="value">The object value.</param>
        /// <param name="message">The exception message.</param>
        public static void IsNotNull<TException>(object value, [Localizable(false)] string message)
            where TException : Exception, new()
        {
            if (value == null)
            {
                throw (TException)Activator.CreateInstance(typeof(TException), message);
            }
        }

        /// <summary>
        /// Validates whether a string is not null or white space.
        /// </summary>
        /// <param name="value">The string value.</param>
        /// <param name="name">The string name.  May optionally include an exception message.</param>
        /// <exception cref="InvalidOperationException">The value is null or white-space.</exception>
        public static void IsNotNullOrWhitespace(string value, [Localizable(false)] string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"{name} cannot be null or white space.");
            }
        }

        /// <summary>
        /// Validates whether a condition is true.  Throws an exception if not.
        /// </summary>
        /// <typeparam name="T">The type of the exception.</typeparam>
        /// <param name="condition">The condition.</param>
        /// <param name="message">The exception message.</param>
        public static void IsTrue<T>(bool condition, string message)
            where T : Exception
        {
            if (!condition)
            {
                throw (T)Activator.CreateInstance(typeof(T), message);
            }
        }
    }
}