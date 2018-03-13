$rnd = Get-Random -Minimum 1000000 -Maximum 9999999
$svcName = "mssql" + $rnd
$pwd = "Yukon" + $rnd
$env:KUBECONFIG="kubeconfig.centralus.json"

$kube = $(System.DefaultWorkingDirectory) + "\kubectl.exe"

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
Write-Host "##vso[task.setvariable variable=k8ServicePwd;]"$pwd
Write-Host "##vso[task.setvariable variable=k8ServiceName;]"$svcName
