#!/usr/bin/env bash

# This script creates the necessary directories that are required 
# for the the linux dotnet builds to work. This issue is caused due to a mismatch
# in the casing for the localization directories between all the projects and the 
# nuget packages they are using. This not an issue in the windows because the dirs 
# are case insensitive. 


# To fix the issue, we need to make sure all the projects 
# and their referenced nuget packages follow the same letter casing for the 
# locailzation directories.

# The script need to run from the repo root

# Dirs to create
dotnetProjectArray=(
    "./src/Microsoft.Kusto.ServiceLayer/bin/Debug/net7.0/"
    "./src/Microsoft.SqlTools.Credentials/bin/Debug/net7.0/"
    "./src/Microsoft.SqlTools.ResourceProvider/bin/Debug/net7.0/"
    "./src/Microsoft.SqlTools.ResourceProvider.Core/bin/Debug/net7.0/"
    "./src/Microsoft.SqlTools.ResourceProvider.DefaultImpl/bin/Debug/net7.0/"
    "./src/Microsoft.SqlTools.ServiceLayer/bin/Debug/net7.0/"
    "./src/Microsoft.SqlTools.Migration/bin/Debug/net7.0/"
    "./test/Microsoft.Kusto.ServiceLayer.UnitTests/bin/Debug/net7.0/"
    "./test/Microsoft.SqlTools.ManagedBatchParser.IntegrationTests/bin/Debug/net7.0/"
    "./test/Microsoft.SqlTools.ServiceLayer.IntegrationTests/bin/Debug/net7.0/"
    "./test/Microsoft.SqlTools.ServiceLayer.PerfTests/bin/Debug/net7.0/"
    "./test/Microsoft.SqlTools.ServiceLayer.Test.Common/bin/Debug/net7.0/"
    "./test/Microsoft.SqlTools.ServiceLayer.TestDriver/bin/Debug/net7.0/"
    "./test/Microsoft.SqlTools.ServiceLayer.TestDriver.Tests/bin/Debug/net7.0/"
    "./test/Microsoft.SqlTools.ServiceLayer.TestEnvConfig/bin/Debug/net7.0/"
    "./test/Microsoft.SqlTools.ServiceLayer.UnitTests/bin/Debug/net7.0/"
    "./test/Microsoft.SqlTools.Test.CompletionExtension/bin/Debug/net7.0/"
    "./src/Microsoft.SqlTools.Hosting/bin/Debug/netstandard2.0/"
    "./src/Microsoft.SqlTools.SqlCore/bin/Debug/netstandard2.0/"
)

requiredLocDirectories=(
    "cs"
    "cs-CZ"
    "de"
    "de-DE"
    "es"
    "es-ES"
    "fr"
    "fr-FR"
    "hu-HU"
    "it"
    "it-IT"
    "ja"
    "ja-JP"
    "ko"
    "ko-KR"
    "nl-NL"
    "pl"
    "pl-PL"
    "pt-br"
    "pt-PT"
    "ru"
    "ru-RU"
    "sv-SE"
    "tr"
    "tr-TR"
    "zh-HANS"
    "zh-hans"
    "zh-HANT"
    "zh-hant"
)

for i in "${dotnetProjectArray[@]}"
do
   : 
   for k in "${requiredLocDirectories[@]}"
    do
        : 
        output="mkdir -v -p $i$k"
        echo $output
    done
done
