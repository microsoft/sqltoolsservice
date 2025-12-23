//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
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
    public class SqlPackageService
    {
        private static readonly Lazy<SqlPackageService> instance = new Lazy<SqlPackageService>(() => new SqlPackageService());
        public static SqlPackageService Instance => instance.Value;

        public void InitializeService(ServiceHost serviceHost)
        {
            serviceHost.SetRequestHandler(GenerateSqlPackageCommandRequest.Type, this.HandleGenerateSqlPackageCommandRequest, true);
        }

        public async Task HandleGenerateSqlPackageCommandRequest(
            SqlPackageCommandParams parameters,
            RequestContext<SqlPackageCommandResult> requestContext)
        {
            try
            {
                if (parameters == null) throw new ArgumentNullException(nameof(parameters));
                if (parameters.CommandLineArguments == null) throw new ArgumentNullException(nameof(parameters.CommandLineArguments));

                // Normalize STS-overridden defaults ONLY for Publish/Script (keeps /p: clean)
                var action = parameters.CommandLineArguments.Action;
                if (parameters.DeploymentOptions != null &&
                    (action == CommandLineToolAction.Publish || action == CommandLineToolAction.Script))
                {
                    parameters.DeploymentOptions.NormalizePublishDefaults();
                }

                // Reflective mapping — STS DTO → DacFx fields (single pass, no hardcoded names)
                var dacfxArgs = MapStsArgsToDacFx(parameters.CommandLineArguments);

                var apiParams = new Microsoft.Data.Tools.Schema.CommandLineTool.GenerateSqlPackageCommandParams
                {
                    CommandLineArguments = dacfxArgs,
                    DeploymentOptions = parameters.DeploymentOptions != null
                        ? DacFxUtils.CreateDeploymentOptions(parameters.DeploymentOptions)
                        : null,
                    ExtractOptions = parameters.ExtractOptions,
                    ExportOptions = parameters.ExportOptions,
                    ImportOptions = parameters.ImportOptions,
                    Variables = parameters.Variables
                };

                // DacFx will run ValidationUtil.ValidateArgs inside the generator
                var command = Microsoft.Data.Tools.Schema.CommandLineTool.SqlPackageCommandGenerator
                    .GenerateSqlPackageCommand(apiParams);

                await requestContext.SendResult(new SqlPackageCommandResult
                {
                    Command = command,
                    Success = true,
                    ErrorMessage = string.Empty
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


        private static Microsoft.Data.Tools.Schema.CommandLineTool.CommandLineArguments
            MapStsArgsToDacFx(Microsoft.SqlTools.ServiceLayer.SqlPackage.Contracts.SqlPackageCommandLineArguments sts)
        {
            if (sts == null) throw new ArgumentNullException(nameof(sts));

            // DacFx model (public fields)
            var dac = new Microsoft.Data.Tools.Schema.CommandLineTool.CommandLineArguments();

            // Set Action first so ValidationUtil has it
            dac.Action = sts.Action;

            // Ensure nested object exists so downstream reflection in the builder never NREs
            dac.CommandLineProperties = dac.CommandLineProperties ?? new CommandLineProperty();

            // Reflect STS *properties* (your DTO)
            var stsType = typeof(Microsoft.SqlTools.ServiceLayer.SqlPackage.Contracts.SqlPackageCommandLineArguments);
            var stsProps = stsType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Reflect DacFx *fields*
            var dacType = typeof(Microsoft.Data.Tools.Schema.CommandLineTool.CommandLineArguments);
            var dacFields = dacType.GetFields(BindingFlags.Public | BindingFlags.Instance);

            // Index DacFx fields by name
            var destByName = new System.Collections.Generic.Dictionary<string, FieldInfo>(System.StringComparer.Ordinal);
            for (int i = 0; i < dacFields.Length; i++)
            {
                var f = dacFields[i];
                destByName[f.Name] = f;
            }

            // Map STS properties → DacFx fields with the same name
            for (int i = 0; i < stsProps.Length; i++)
            {
                var prop = stsProps[i];
                if (!prop.CanRead) continue;

                // Skip Action (already set)
                if (prop.Name == nameof(sts.Action)) continue;

                FieldInfo? dest;
                if (!destByName.TryGetValue(prop.Name, out dest) || dest == null)
                    continue; // name didn't match a DacFx field — skip

                // Null-safe read
                object? valTmp;
                try { valTmp = prop.GetValue(sts, null); }
                catch { continue; }
                if (valTmp == null) continue;

                // Skip empty strings to keep CLI clean
                var s = valTmp as string;
                if (s != null && string.IsNullOrWhiteSpace(s)) continue;

                try
                {
                    // If types are compatible, assign directly
                    if (dest.FieldType.IsAssignableFrom(prop.PropertyType))
                    {
                        dest.SetValue(dac, valTmp);
                        continue;
                    }

                    // Enum flexibility (int → enum, string → enum name)
                    if (dest.FieldType.IsEnum)
                    {
                        if (valTmp is int)
                        {
                            dest.SetValue(dac, System.Enum.ToObject(dest.FieldType, (int)valTmp));
                            continue;
                        }
                        if (valTmp is string)
                        {
                            object? parsed = null;
                            try { parsed = System.Enum.Parse(dest.FieldType, (string)valTmp, ignoreCase: true); } catch { }
                            if (parsed != null)
                            {
                                dest.SetValue(dac, parsed);
                                continue;
                            }
                        }
                    }

                    // If you later add nullable primitives in STS (e.g., int?, bool?), add small conversions here.
                }
                catch
                {
                    // Defensive: skip incompatible assignments; DacFx will validate later.
                    continue;
                }
            }

            // Safety: if Action defaulted to 0 somewhere, restore from STS
            if (dac.Action == 0) dac.Action = sts.Action;

            return dac;
        }


        // C# 7.3-friendly enum parse helper
        private static bool EnumTryParse(Type enumType, string s, out object value)
        {
            value = null;
            if (string.IsNullOrEmpty(s)) return false;
            try
            {
                value = Enum.Parse(enumType, s, ignoreCase: true);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
