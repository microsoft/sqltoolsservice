$rnd = Get-Random -Minimum 1000000 -Maximum 9999999
$svcName = "mssql" + $rnd
$pwd = "Yukon" + $rnd
$env:KUBECONFIG = $env:AGENT_WORKFOLDER + "\kubeconfig.centralus.json"

$kube = $env:AGENT_WORKFOLDER + "\kubectl.exe"

iex "$kube run $svcName --image=sqltoolscontainers.azurecr.io/sql2017linux --port=1433 --env ACCEPT_EULA=Y --env SA_PASSWORD=$pwd"
iex "$kube expose deployment $svcName --type=LoadBalancer"

do
{
    $svc = iex "$kube describe service $svcName"
    Write-Host "Service Configuration: $svc"
    $endpoint = $svc -match "Endpoints:(.+):1433"
    Write-Host "Endpoint: $endpoint"
# the output of kubectl is an object array
} while ($endpoint -eq $null -or $endpoint.Length -eq 0)

$endpoint = $endpoint.Split(":")[1].Trim()
Write-Host "##vso[task.setvariable variable=k8EndPoint;]"$endpoint
Write-Host "##vso[task.setvariable variable=k8ServiceName;]"$svcName

$settingsOutput = "{ `"mssql.connections`":[ "
$settingsOutput = $settingsOutput + "{ `"server`":`"$svcName`", "
$settingsOutput = $settingsOutput +  "`"ServerType`":0, "
$settingsOutput = $settingsOutput +  "`"AuthenticationType`":1, "
$settingsOutput = $settingsOutput +  "`"User`":`"sa`", "
$settingsOutput = $settingsOutput +  "`"Password`":`"$pwd`", " 
$settingsOutput = $settingsOutput +  "`"ConnectTimeout`":30, "
$settingsOutput = $settingsOutput +  "`"VersionKey`":`"defaultSql2016`" }]}"

$settingsPath = $env:AGENT_WORKFOLDER + "_workconnectionsettings.json";
Set-Content -Path $settingsPath -Value $settingsOutput -Force

Write-Host "Saving settings to $settingsPath"
Write-Host $settingsOutput 

Write-Host "##vso[task.setvariable variable=SettingsFileName;]"$settingsPath 
