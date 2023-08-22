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

dotnetProjectArray=(
    "./test/Microsoft.SqlTools.ServiceLayer.IntegrationTests"
)

# Please update the framework vars when updating target framework for the projects
framework7="/bin/Debug/net7.0/"

requiredLocDirectories=(
    "pt-BR"
    "zh-Hans"
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
