param(
    [System.String]
    $TemplateNotebook = $(throw New-Object System.Exception "TemplateNotebook cannot be null"),

    [System.String]
    $DatabaseInstance,

    [System.String]
    $DatabaseServer,

    [System.String]
    $DatabaseUsername,

    [System.String]
    $DatabasePassword
)
<#
    Parses a DataTable returned by Invoke-SQL Command into a Jupyter Notebook output cell.
#>
function ParseTableToNotebookOutput {
    param (
        [System.Data.DataTable]
        $DataTable,

        [int]
        $CellExecutionCount
    )
    $TableHTMLText = "<table>"
    $TableSchemaFeilds = @()
    $TableHTMLText += "<tr>"
    foreach ($ColumnName in $DataTable.Columns) {
        $TableSchemaFeilds += @(@{name = $ColumnName.toString() })
        $TableHTMLText += "<th>" + $ColumnName.toString() + "</th>"
    }
    $TableHTMLText += "</tr>"
    $TableSchema = @{ }
    $TableSchema["fields"] = $TableSchemaFeilds

    $TableDataRows = @()
    foreach ($Row in $DataTable) {
        $TableDataRow = [ordered]@{ }
        $TableHTMLText += "<tr>"
        $i = 0
        foreach ($Cell in $Row.ItemArray) {
            $TableDataRow[$i.ToString()] = $Cell.toString()
            $TableHTMLText += "<td>" + $Cell.toString() + "</td>"
            $i++
        }
        $TableHTMLText += "</tr>"
        $TableDataRows += $TableDataRow
    }

    $TableDataResource = @{ }
    $TableDataResource["schema"] = $TableSchema
    $TableDataResource["data"] = $TableDataRows
    $TableData = @{ }
    $TableData["application/vnd.dataresource+json"] = $TableDataResource
    $TableData["text/html"] = $TableHTMLText
    $TableOutput = @{ }
    $TableOutput["output_type"] = "execute_result"
    $TableOutput["data"] = $TableData
    $TableOutput["metadata"] = @{ }
    $TableOutput["execution_count"] = $CellExecutionCount
    return $TableOutput
}


function ParseQueryErrorToNotebookOutput {
    param (
        $QueryError
    )
    <#
    Following the current syntax of errors in T-SQL notebooks from ADS
    #>
    $ErrorString = "Msg " + $QueryError.Exception.InnerException.Number +
    ", Level " + $QueryError.Exception.InnerException.Class +
    ", State " + $QueryError.Exception.InnerException.State +
    ", Line " + $QueryError.Exception.InnerException.LineNumber +
    "`r`n" + $QueryError.Exception.Message
    
    $ErrorOutput = @{ }
    $ErrorOutput["output_type"] = "error"
    $ErrorOutput["traceback"] = @()
    $ErrorOutput["evalue"] = $ErrorString
    return $ErrorOutput
}

function ParseStringToNotebookOutput {
    param (
        [System.String]
        $InputString
    )
    <#
    Parsing the string to notebook cell output. 
    It's the standard Jupyter Syntax
    #>
    $StringOutputData = @{ }
    $StringOutputData["text/html"] = $InputString
    $StringOutput = @{ }
    $StringOutput["output_type"] = "display_data"
    $StringOutput["data"] = $StringOutputData
    $StringOutput["metadata"] = @{ }
    return $StringOutput
}

<#
Start of script
#>
try {
    $TemplateNotebookJsonObject = ConvertFrom-Json -InputObject $TemplateNotebook
}
catch {
    Throw $_.Exception
}

<#
Setting params for Invoke-Sqlcmd
#>
$DatabaseQueryHashTable = @{ }
if ($DatabaseInstance) {
    $DatabaseQueryHashTable["ServerInstance"] = $DatabaseInstance
}
if ($DatabaseServer) {
    $DatabaseQueryHashTable["HostName"] = $DatabaseServer
}
if ($DatabasePassword) {
    $DatabaseQueryHashTable["Username"] = $DatabaseUsername
}
if ($DatabasePassword) {
    $DatabaseQueryHashTable["Password"] = $DatabasePassword
}
$DatabaseQueryHashTable["Verbose"] = $true
$DatabaseQueryHashTable["ErrorVariable"] = "SqlQueryError"
$DatabaseQueryHashTable["OutputAs"] = "DataTables"

$CellExcecutionCount = 1

foreach ($NotebookCell in $TemplateNotebookJsonObject.cells) {
    $NotebookCellOutputs = @()
    <#
    Ignoring Markdown or raw cells
    #>
    if ($NotebookCell.cell_type -eq "markdown" -or $NotebookCell.cell_type -eq "raw" -or $NotebookCell.source -eq "") {
        continue;
    }
    <#
    Getting the source T-SQL from the cell
    #>
    $DatabaseQueryHashTable["Query"] = $NotebookCell.source
    <#
    Executing the T-SQL Query and storing the result and the time taken to execute
    #>
    $SqlQueryExecutionTime = Measure-Command { $SqlQueryResult = @(Invoke-Sqlcmd @DatabaseQueryHashTable  4>&1) }
    <#
    Setting the the Notebook Cell Execution Count
    #>
    $NotebookCell.execution_count = $CellExcecutionCount++
    $NotebookCellTableOutputs = @()
    if ($SqlQueryResult) {
        foreach ($SQLQueryResultElement in $SqlQueryResult) {
            <#
            Iterating over the results by Invoke-Sqlcmd
            There are 2 types of errors:
                1. Verbose Output: Print Statements:
                These needs to be added to the beginning of the cell outputs
                2. Datatables from the database
                These needs to be added to the end of cell outputs
            #>
            switch ($SQLQueryResultElement.getType()) {
                System.Management.Automation.VerboseRecord {
                    <#
                    Adding the print statments to the cell outputs
                    #>
                    $NotebookCellOutputs += $(ParseStringToNotebookOutput($SQLQueryResultElement.Message))
                }
                System.Data.DataTable {
                    <#
                    Storing the print Tables into an array to be added later to the cell outpuyt
                    #>
                    $NotebookCellTableOutputs += $(ParseTableToNotebookOutput $SQLQueryResultElement  $CellExcecutionCount)
                }
                <#
                TODO Throw an appropriate error
                #>
                Default { }
            }
        }
    }
    if ($SqlQueryError) {
        <#
        Adding the parsed query error from Invoke-Sqlcmd
        #>
        $NotebookCellOutputs += $(ParseQueryErrorToNotebookOutput($SqlQueryError))
    }
    if ($SqlQueryExecutionTime) {
        <#
        Adding the parsed execution time from Measure-Command 
        #>
        $NotebookCellExcutionTimeString = "Total execution time: " + $SqlQueryExecutionTime.ToString("hh\:mm\:ss\.fff")
        $NotebookCellOutputs += $(ParseStringToNotebookOutput($NotebookCellExcutionTimeString))
    }
    <#
    Adding the data tables
    #>
    $NotebookCellOutputs += $NotebookCellTableOutputs
    $NotebookCell.outputs = $NotebookCellOutputs
    
}


return ($TemplateNotebookJsonObject | ConvertTo-Json -Depth 100)