//------------------------------------------------------------------------------
// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Text;
using Microsoft.Data.Tools.Components.Diagnostics;
using Microsoft.Win32;

namespace Microsoft.Data.Tools.Schema.Utilities.Sql.Common.Exceptions
{
    internal static class SqlExceptionUtils
    {
        private const string RegistryKeyPath = @"Software\Microsoft\DataTools";
        private const string RegistryValueName = "DisableExceptionFilter";

        private static readonly object _syncRoot = new object();
        private static volatile bool _initialized;
        private static bool _disableFiltering;

        /// <summary>
        /// Returns true if the exception indicates that something beyond the operation that was underway at the time might be screwed up.
        /// </summary>
        public static bool IsIrrecoverableException(Exception e)
        {
            return e is NullReferenceException ||
                   e is OutOfMemoryException ||
                   e is System.Threading.ThreadAbortException ||
                   e is AppDomainUnloadedException ||
                   e is CannotUnloadAppDomainException ||
                   e is InvalidProgramException ||
                   e is BadImageFormatException ||
                   e is OperationCanceledException ||
                   e is System.Runtime.InteropServices.COMException ||
                   e is System.Runtime.InteropServices.SEHException;
        }

        internal static void ValidateNullParameter<T>(T parameter, string parameterName, SqlTraceId traceId = SqlTraceId.CoreServices) where T : class
        {
            if (parameter == null)
            {
                SqlTracer.TraceEvent(TraceEventType.Critical, traceId, string.Concat("Null argument: ", parameterName));
                throw new ArgumentNullException(parameterName);
            }
        }

        internal static void ValidateNullOrEmptyParameter(string parameter, string parameterName, SqlTraceId traceId = SqlTraceId.CoreServices)
        {
            if (string.IsNullOrEmpty(parameter))
            {
                SqlTracer.TraceEvent(TraceEventType.Critical, traceId, string.Concat("Null argument: ", parameterName));
                throw new ArgumentNullException(parameterName);
            }
        }

        internal static void ValidateParameterLength(string parameter, string parameterName, int maxLength, SqlTraceId traceId = SqlTraceId.CoreServices)
        {
            if (parameter != null && parameter.Length > maxLength)
            {
                SqlTracer.TraceEvent(TraceEventType.Critical, traceId, string.Concat("Invalid length: ", parameterName));
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }

        /// <summary>
        ///		Gets whether exception filtering is not-enabled based on registry settings.
        /// </summary>
        internal static bool DisableExceptionFilter
        {
            get
            {
                if (!_initialized)
                {
                    lock (_syncRoot)
                    {
                        if (!_initialized)
                        {
                            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false))
                            {
                                object value = key != null ? key.GetValue(RegistryValueName) : null;
                                if (value != null && value.ToString() != "0")
                                {
                                    _disableFiltering = true;
                                }
                            }
                            _initialized = true;
                        }
                    }
                }
                return _disableFiltering;
            }
        }

        /// <summary>
        ///		Gets whether exception is a critical one and can't be ignored with corrupting
        ///		AppDomain state.
        /// </summary>
        /// <param name="ex">Exception to test.</param>
        /// <returns>True if exception should not be swallowed.</returns>
        internal static bool IsCriticalException(Exception ex)
        {
            // When filtering is not-enabled, all exceptions are critical and should be reported to Watson.
            if (DisableExceptionFilter)
            {
                return true;
            }

            if (ex is NullReferenceException
                || ex is OutOfMemoryException
                || ex is System.Threading.ThreadAbortException)
            {
                return true;
            }

            if (ex.InnerException != null)
            {
                return IsCriticalException(ex.InnerException);
            }

            return false;
        }

        /// <summary>
        ///		Shows non-critical exceptions to the user and returns false or
        ///		returns true for critical exceptions.
        /// </summary>
        /// <param name="serviceProvider">Service provider to use to display error message.</param>
        /// <param name="ex">Exception to handle.</param>
        /// <returns>True if exception is critical and can't be ignored.</returns>
        internal static bool ThrowOrShow(IServiceProvider serviceProvider, Exception ex)
        {
            if (IsCriticalException(ex))
            {
                return true;
            }

            // if (serviceProvider != null)
            // {
            //     IUIService uiService = serviceProvider.GetService(typeof(IUIService)) as IUIService;
            //     if (uiService != null)
            //     {
            //         uiService.ShowError(ex);
            //         return false;
            //     }
            // }
            return false;
        }
        /// <summary>
        /// Populate messages of inner exceptions
        /// </summary>
        /// <param name="ex"> Exception </param>
        /// <returns></returns>
        internal static string PopulateErrorMessage(this Exception ex)
        {
            StringBuilder message = new StringBuilder();
            while (ex != null)
            {
                message.Append(ex.Message).Append(Environment.NewLine);
                ex = ex.InnerException;
            }
            if (message.Length != 0)
            {
                message.Length -= Environment.NewLine.Length;
            }
            return message.ToString();
        }
    }
}