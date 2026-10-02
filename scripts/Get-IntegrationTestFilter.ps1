# Copyright (c) Microsoft. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [ValidateSet('schema-compare-files', 'schema-compare', 'database', 'services', 'sql-projects')]
    [string] $Shard
)

# Split the expensive schema comparison fixture by operation. Unlisted and newly added
# schema comparison tests stay in the other schema shard.
$schemaFileOperations = @('GenerateScript', 'PublishChanges', 'OpenScmp', 'SaveScmp') | ForEach-Object {
    ".SchemaCompare.SchemaCompareServiceTests.SchemaCompare$_"
}
$databaseNamespaces = @('DacFx', 'DisasterRecovery', 'ObjectManagement', 'ObjectExplorer')

switch ($Shard) {
    'schema-compare-files' {
        ($schemaFileOperations | ForEach-Object { "FullyQualifiedName~$_" }) -join '|'
    }
    'schema-compare' {
        (@('FullyQualifiedName~.SchemaCompare.') +
            @($schemaFileOperations | ForEach-Object { "FullyQualifiedName!~$_" })) -join '&'
    }
    'database' {
        ($databaseNamespaces | ForEach-Object { "FullyQualifiedName~.$_." }) -join '|'
    }
    'services' {
        # The complement covers every remaining namespace, including future test suites.
        # SQL project tests run in a separate process on this shard to isolate service singletons.
        (@('FullyQualifiedName!~.SchemaCompare.', 'FullyQualifiedName!~.SqlProjects.') +
            @($databaseNamespaces | ForEach-Object { "FullyQualifiedName!~.$_." })) -join '&'
    }
    'sql-projects' {
        'FullyQualifiedName~.SqlProjects.'
    }
}
