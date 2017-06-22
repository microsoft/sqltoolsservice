#!/usr/bin/env bash

WORKINGDIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )

dotnet restore $WORKINGDIR
dotnet restore $WORKINGDIR../../src/Microsoft.SqlTools.ServiceLayer/project.json
dotnet build $WORKINGDIR../../src/Microsoft.SqlTools.ServiceLayer\project.json
cd ..
dotnet restore 
dotnet build Microsoft.SqlTools.ServiceLayer.TestDriver/project.json
dotnet build Microsoft.SqlTools.ServiceLayer.Test.Common/project.json
dotnet build Microsoft.SqlTools.ServiceLayer.TestEnvConfig/project.json

cd Microsoft.SqlTools.ServiceLayer.TestEnvConfig

dotnet run "$1"