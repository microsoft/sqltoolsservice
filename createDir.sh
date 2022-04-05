#!/usr/bin/env bash

pwd

projectArray=(
    "./src/Microsoft.InsightsGenerator"
    "./src/Microsoft.Kusto.ServiceLayer"
    "./src/Microsoft.SqlTools.Credentials"
    "./src/Microsoft.SqlTools.ManagedBatchParser"
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

requireDir=(
    "/bin/Debug/net6.0/cs"
    "/bin/Debug/net6.0/cs-CZ"
    "/bin/Debug/net6.0/de"
    "/bin/Debug/net6.0/de-DE"
    "/bin/Debug/net6.0/es"
    "/bin/Debug/net6.0/es-ES"
    "/bin/Debug/net6.0/fr"
    "/bin/Debug/net6.0/fr-FR"
    "/bin/Debug/net6.0/hu-HU"
    "/bin/Debug/net6.0/it"
    "/bin/Debug/net6.0/it-IT"
    "/bin/Debug/net6.0/ja"
    "/bin/Debug/net6.0/ja-JP"
    "/bin/Debug/net6.0/ko"
    "/bin/Debug/net6.0/ko-KR"
    "/bin/Debug/net6.0/nl-NL"
    "/bin/Debug/net6.0/pl"
    "/bin/Debug/net6.0/pl-PL"
    "/bin/Debug/net6.0/pt-br"
    "/bin/Debug/net6.0/pt-BR"
    "/bin/Debug/net6.0/pt-PT"
    "/bin/Debug/net6.0/ru"
    "/bin/Debug/net6.0/ru-RU"
    "/bin/Debug/net6.0/sv-SE"
    "/bin/Debug/net6.0/tr"
    "/bin/Debug/net6.0/tr-TR"
    "/bin/Debug/net6.0/zh-Hans"
    "/bin/Debug/net6.0/zh-HANS"
    "/bin/Debug/net6.0/zh-Hant"
    "/bin/Debug/net6.0/zh-HANT"
    "/bin/Debug/net6.0/zh-hant"
    "/bin/Debug/net6.0/zh-hans"

)

for i in "${projectArray[@]}"
do
   : 
   for k in "${requireDir[@]}"
    do
        : 
        echo Creating $i$k
        mkdir -p $i$k
    done
done
