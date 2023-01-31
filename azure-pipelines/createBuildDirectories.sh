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

net6projectArray=(
    "./src/Microsoft.Kusto.ServiceLayer"
    "./src/Microsoft.SqlTools.Credentials"
    "./src/Microsoft.SqlTools.Hosting"
    "./src/Microsoft.SqlTools.ResourceProvider"
    "./src/Microsoft.SqlTools.ResourceProvider.Core"
    "./src/Microsoft.SqlTools.ResourceProvider.DefaultImpl"
    "./src/Microsoft.SqlTools.ServiceLayer"
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

netStandard2ProjectArray=(
    "./src/Microsoft.SqlTools.ManagedBatchParser"
)

# Please update the framework vars when updating target framework for the projects
framework6="/bin/Debug/net6.0/"
framework2="/bin/Debug/netstandard2.1/"

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

for i in "${net6projectArray[@]}"
do
   : 
   for k in "${requiredLocDirectories[@]}"
    do
        : 
        output=`mkdir -v -p $i$framework6$k`
        echo $output
    done
done

for i in "${netStandard2ProjectArray[@]}"
do
   : 
   for k in "${requiredLocDirectories[@]}"
    do
        : 
        output=`mkdir -v -p $i$framework2$k`
        echo $output
    done
done