#!/usr/bin/env bash

# This script creates the necessary directories required 
# for Linux dotnet builds to function correctly. The issue arises due to a mismatch
# in the casing of the localization directories between different frameworks used in this repo.
# Net 472 creates localization directories like zh-Hans, pt-BR, zh-Hant, while netcore 
# uses zh-hans, pt-br, zh-hant. This discrepancy causes build failures on Linux since the file system is 
# case-sensitive. Consequently, when attempting to build using the netcore framework, it tries to copy files
# from projects using the net472 framework (e.g., zh-Hant), resulting in failures as the localization directory 
# (present in netcore as zh-hant and not zh-Hant) cannot be found.


# To fix the issue, we need to make sure all the projects 
# and their referenced nuget packages follow the same letter casing for the 
# locailzation directories.

# The script need to run from the repo root

dotnetProjectArray=(
    "./src/Microsoft.Kusto.ServiceLayer"
    "./src/Microsoft.SqlTools.Credentials"
    "./src/Microsoft.SqlTools.Hosting"
    "./src/Microsoft.SqlTools.ResourceProvider"
    "./src/Microsoft.SqlTools.ResourceProvider.Core"
    "./src/Microsoft.SqlTools.ResourceProvider.DefaultImpl"
    "./src/Microsoft.SqlTools.ServiceLayer"
    "./src/Microsoft.SqlTools.Migration"
    "./src/Microsoft.SqlTools.SqlCore"
    "./test/Microsoft.Kusto.ServiceLayer.UnitTests"
    "./test/Microsoft.SqlTools.ManagedBatchParser.IntegrationTests"
    "./test/Microsoft.SqlTools.ServiceLayer.IntegrationTests"
    "./test/Microsoft.SqlTools.ServiceLayer.PerfTests"
    "./test/Microsoft.SqlTools.ServiceLayer.Test.Common"
    "./test/Microsoft.SqlTools.ServiceLayer.TestDriver"
    "./test/Microsoft.SqlTools.ServiceLayer.TestDriver.Tests"
    "./test/Microsoft.SqlTools.ServiceLayer.TestEnvConfig"
    "./test/Microsoft.SqlTools.ServiceLayer.UnitTests"
    "./test/Microsoft.SqlTools.Test.CompletionExtension"
)

# Please update the framework vars when updating target framework for the projects
framework7="/bin/Debug/net7.0/"

requiredLocDirectories=(
    "pt-br"
    "pt-BR"
    "zh-hans"
    "zh-Hans"
    "zh-hant"
    "zh-Hant"
)

for i in "${dotnetProjectArray[@]}"
do
   : 
   for k in "${requiredLocDirectories[@]}"
    do
        : 
        output=`mkdir -v -p $i$framework7$k`
        echo $output
    done
done
