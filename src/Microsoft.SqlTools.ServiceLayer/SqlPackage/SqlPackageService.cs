//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Data.Tools.Schema.CommandLineTool;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.DacFx;
using Microsoft.SqlTools.ServiceLayer.Hosting;
using Microsoft.SqlTools.ServiceLayer.SqlPackage.Contracts;
using Microsoft.SqlTools.Utility;

namespace Microsoft.SqlTools.ServiceLayer.SqlPackage
{
    /// <summary>
    /// Service for generating SqlPackage command-line strings from structured parameters.
    /// </summary>
    public class SqlPackageService
    {
        private static readonly Lazy<SqlPackageService> instance = new Lazy<SqlPackageService>(() => new SqlPackageService());
        public static SqlPackageService Instance => instance.Value;

        /// <summary>
        /// Initializes the SqlPackage service by registering request handlers with the service host.
        /// </summary>
        /// <param name="serviceHost">The service host to register handlers with</param>
        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(GenerateSqlPackageCommandRequest.Type, this.HandleGenerateSqlPackageCommandRequest, isParallelProcessingSupported: true);
        }

        /// <summary>
        /// Handles requests to generate SqlPackage command-line strings.
        /// Maps STS command-line arguments to DacFx types, applies action-specific options,
        /// and builds the final command string using SqlPackageCommandBuilder.
        /// </summary>
        /// <param name="parameters">Parameters containing command-line arguments and action-specific options</param>
        /// <param name="requestContext">Context for sending the result back to the client</param>
        /// <returns>Task representing the async operation</returns>
        public async Task HandleGenerateSqlPackageCommandRequest(
            SqlPackageCommandParams parameters,
            RequestContext<SqlPackageCommandResult> requestContext)
        {
            try
            {
                if (parameters == null) throw new ArgumentNullException(nameof(parameters));
                if (parameters.CommandLineArguments == null) throw new ArgumentNullException(nameof(parameters.CommandLineArguments));

                // Map STS DTO → DacFx fields with no hardcoded names (cached reflection)
                var dacfxArgs = ReflectionMapper.MapByName(
                    source: parameters.CommandLineArguments,
                    destinationFactory: () => new Microsoft.Data.Tools.Schema.CommandLineTool.CommandLineArguments(),
                    configure: dest =>
                    {
                        // Ensure nested object exists; avoids NREs downstream
                        dest.CommandLineProperties = dest.CommandLineProperties ?? new CommandLineProperty();

                        // Action first so validation has it
                        dest.Action = parameters.CommandLineArguments.Action;
                    });

                // Builder fluent API
                var builder = new SqlPackageCommandBuilder()
                    .WithArguments(dacfxArgs)
                    .WithVariables(parameters.Variables);

                // Action-specific options via strategy table (no switch noise)
                ActionOptions.Apply(parameters.CommandLineArguments.Action, parameters, builder);

                // Build command — validation exceptions are collected by builder
                var command = builder.Build().ToString();

                await requestContext.SendResult(new SqlPackageCommandResult
                {
                    Command = command,
                    Success = true,
                    ErrorMessage = string.Empty
                });
            }
            catch (SqlPackageCommandException ex)
            {
                Logger.Error($"SqlPackage command validation failed: {ex.Message}");
                await requestContext.SendResult(new SqlPackageCommandResult
                {
                    Command = null,
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }
            catch (Exception e)
            {
                Logger.Error($"SqlPackage GenerateCommand failed: {e.Message}");
                await requestContext.SendResult(new SqlPackageCommandResult
                {
                    Command = null,
                    Success = false,
                    ErrorMessage = e.Message
                });
            }
        }
    }

    /// <summary>
    /// Maps source properties -> destination fields by name, with cached reflection and
    /// centralized skip/default policies. No hardcoded member names.
    /// </summary>
    internal static class ReflectionMapper
    {
        private static readonly Dictionary<Type, PropertyInfo[]> _propCache = new Dictionary<Type, PropertyInfo[]>();
        private static readonly Dictionary<Type, FieldInfo[]> _fieldCache = new Dictionary<Type, FieldInfo[]>();

        public static TDestination MapByName<TSource, TDestination>(
            TSource source,
            Func<TDestination> destinationFactory,
            Action<TDestination> configure = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var dest = destinationFactory != null ? destinationFactory() : Activator.CreateInstance<TDestination>();
            configure?.Invoke(dest);

            var srcType = typeof(TSource);
            var dstType = typeof(TDestination);

            var srcProps = GetPublicInstanceProperties(srcType);
            var dstFields = GetPublicInstanceFields(dstType);

            // Index destination fields by name (Ordinal)
            var destByName = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
            for (int i = 0; i < dstFields.Length; i++)
            {
                var f = dstFields[i];
                destByName[f.Name] = f;
            }

            for (int i = 0; i < srcProps.Length; i++)
            {
                var prop = srcProps[i];
                if (!prop.CanRead) continue;

                // Read property value with null safety
                object? value;
                try { value = prop.GetValue(source, index: null); }
                catch { continue; }

                // Skip null, empty strings, default values (0, false, empty arrays, enum=0) to keep CLI clean
                if (DefaultValuePolicy.ShouldSkip(prop.PropertyType, value))
                    continue;

                // Skip if destination doesn't have a matching field (name-based mapping)
                FieldInfo? destField;
                if (!destByName.TryGetValue(prop.Name, out destField) || destField == null)
                    continue;

                try
                {
                    // Try type-compatible assignment or enum conversions (int→enum, string→enum)
                    // Skip incompatible types; builder validation will catch missing required fields
                    if (TypeConversion.TryAssign(dest, destField, prop.PropertyType, value))
                    {
                        continue;
                    }
                }
                catch
                {
                    continue;
                }
            }

            // Safety: if Action defaulted somewhere, restore from source (if present)
            var actionProp = srcType.GetProperty(nameof(Microsoft.SqlTools.ServiceLayer.SqlPackage.Contracts.SqlPackageCommandLineArguments.Action));
            var actionField = dstType.GetField(nameof(Microsoft.Data.Tools.Schema.CommandLineTool.CommandLineArguments.Action));
            if (actionProp != null && actionField != null)
            {
                var srcAction = actionProp.GetValue(source, null);
                var destAction = actionField.GetValue(dest);
                // Treat 0 as default for enums
                if (srcAction != null && Convert.ToInt32(destAction) == 0)
                {
                    actionField.SetValue(dest, srcAction);
                }
            }

            return dest;
        }

        /// <summary>
        /// Gets public instance properties for a type with caching to avoid repeated reflection calls.
        /// </summary>
        private static PropertyInfo[] GetPublicInstanceProperties(Type t)
        {
            PropertyInfo[]? props;
            if (_propCache.TryGetValue(t, out props)) return props;
            props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            _propCache[t] = props;
            return props;
        }

        /// <summary>
        /// Gets public instance fields for a type with caching to avoid repeated reflection calls.
        /// </summary>
        private static FieldInfo[] GetPublicInstanceFields(Type t)
        {
            FieldInfo[]? fields;
            if (_fieldCache.TryGetValue(t, out fields)) return fields;
            fields = t.GetFields(BindingFlags.Public | BindingFlags.Instance);
            _fieldCache[t] = fields;
            return fields;
        }
    }

    /// <summary>
    /// Central policy for deciding whether a value should be skipped to keep CLI clean.
    /// </summary>
    internal static class DefaultValuePolicy
    {
        public static bool ShouldSkip(Type type, object value)
        {
            if (value == null) return true;

            // Skip empty strings
            var s = value as string;
            if (s != null && string.IsNullOrWhiteSpace(s)) return true;

            // Skip empty arrays
            if (type.IsArray)
            {
                var array = value as Array;
                if (array != null && array.Length == 0) return true;
            }

            // Skip 0 for ints
            if (type == typeof(int) && (int)value == 0) return true;

            // Skip false for bools
            if (type == typeof(bool) && ((bool)value) == false) return true;

            // Skip default enum (0)
            if (type.IsEnum && Convert.ToInt32(value) == 0) return true;

            // Extend here for nullable primitives if you add them later (int?, bool?, etc.)
            return false;
        }
    }

    /// <summary>
    /// Handles compatible assignment and flexible enum conversions (int->enum, string->enum name).
    /// </summary>
    internal static class TypeConversion
    {
        public static bool TryAssign(object destinationInstance, FieldInfo destField, Type sourceType, object value)
        {
            var destType = destField.FieldType;

            // Direct assignable
            if (destType.IsAssignableFrom(sourceType))
            {
                destField.SetValue(destinationInstance, value);
                return true;
            }

            // Enum conversions
            if (destType.IsEnum)
            {
                // int -> enum
                if (sourceType == typeof(int))
                {
                    destField.SetValue(destinationInstance, Enum.ToObject(destType, (int)value));
                    return true;
                }

                // string -> enum by name (case-insensitive)
                if (sourceType == typeof(string))
                {
                    object parsed;
                    if (EnumTryParse(destType, (string)value, out parsed))
                    {
                        destField.SetValue(destinationInstance, parsed);
                        return true;
                    }
                }
            }

            // Add small conversions here when you introduce nullable primitives on source
            // e.g., int? -> int, bool? -> bool

            return false;
        }

        // C# 7.3-friendly enum parse helper
        private static bool EnumTryParse(Type enumType, string s, out object parsed)
        {
            parsed = null;
            if (string.IsNullOrEmpty(s)) return false;
            try
            {
                parsed = Enum.Parse(enumType, s, ignoreCase: true);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Action-specific options (Publish/Script/Extract/Export/Import) applied via strategy table.
    /// Keeps logic localized without switch blocks or hardcoded field names.
    /// </summary>
    internal static class ActionOptions
    {
        private static readonly IDictionary<CommandLineToolAction, Action<SqlPackageCommandParams, SqlPackageCommandBuilder>> _appliers
            = new Dictionary<CommandLineToolAction, Action<SqlPackageCommandParams, SqlPackageCommandBuilder>>
            {
                {
                    CommandLineToolAction.Publish, (p, b) =>
                    {
                        if (p.DeploymentOptions != null)
                        {
                            p.DeploymentOptions.NormalizePublishDefaults();
                            b.WithDeployOptions(DacFxUtils.CreateDeploymentOptions(p.DeploymentOptions));
                        }
                    }
                },
                {
                    CommandLineToolAction.Script, (p, b) =>
                    {
                        if (p.DeploymentOptions != null)
                        {
                            p.DeploymentOptions.NormalizePublishDefaults();
                            b.WithDeployOptions(DacFxUtils.CreateDeploymentOptions(p.DeploymentOptions));
                        }
                    }
                },
                {
                    CommandLineToolAction.DeployReport, (p, b) =>
                    {
                        if (p.DeploymentOptions != null)
                        {
                            b.WithDeployOptions(DacFxUtils.CreateDeploymentOptions(p.DeploymentOptions));
                        }
                    }
                },
                {
                    CommandLineToolAction.Extract, (p, b) =>
                    {
                        if (p.ExtractOptions != null)
                        {
                            b.WithExtractOptions(p.ExtractOptions);
                        }
                    }
                },
                {
                    CommandLineToolAction.Export, (p, b) =>
                    {
                        if (p.ExportOptions != null)
                        {
                            b.WithExportOptions(p.ExportOptions);
                        }
                    }
                },
                {
                    CommandLineToolAction.Import, (p, b) =>
                    {
                        if (p.ImportOptions != null)
                        {
                            b.WithImportOptions(p.ImportOptions);
                        }
                    }
                },
            };

        /// <summary>
        /// Applies action-specific options to the SqlPackageCommandBuilder.
        /// Uses strategy pattern to call the appropriate With*Options() method based on action type:
        /// - Publish/Script → b.WithDeployOptions(deployOptions)
        /// - Extract → b.WithExtractOptions(extractOptions)
        /// - Export → b.WithExportOptions(exportOptions)
        /// - Import → b.WithImportOptions(importOptions)
        /// </summary>
        public static void Apply(CommandLineToolAction action, SqlPackageCommandParams p, SqlPackageCommandBuilder b)
        {
            Action<SqlPackageCommandParams, SqlPackageCommandBuilder>? applier;
            if (_appliers.TryGetValue(action, out applier))
            {
                applier(p, b);
            }
        }
    }
}
