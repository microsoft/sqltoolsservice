#!/usr/bin/env bash

# This script creates the necessary directories that are required 
# for the the linux dotnet builds to work
# The script has to be run by from the root of the repository

# Please update the framework vars when updating target framework for the projects
# All the dotnet projects in the STS folder

echo `pwd`

net6projectArray=(
    "./src/Microsoft.InsightsGenerator"
    "./src/Microsoft.Kusto.ServiceLayer"
    "./src/Microsoft.SqlTools.Credentials"
    "./src/Microsoft.SqlTools.Hosting"
    "./src/Microsoft.SqlTools.ResourceProvider"
    "./src/Microsoft.SqlTools.ResourceProvider.Core"
    "./src/Microsoft.SqlTools.ResourceProvider.DefaultImpl"
    "./src/Microsoft.SqlTools.ServiceLayer"
    "./test/Microsoft.InsightsGenerator.UnitTests"
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
    "pt-BR"
    "pt-PT"
    "ru"
    "ru-RU"
    "sv-SE"
    "tr"
    "tr-TR"
    "zh-Hans"
    "zh-HANS"
    "zh-Hant"
    "zh-HANT"
    "zh-hant"
    "zh-hans"

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