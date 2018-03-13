Param (
    $svcName = $env:k8ServiceName
)
$env:KUBECONFIG="kubeconfig.centralus.json"

$kube = $(System.DefaultWorkingDirectory) + "\kubectl.exe"

iex "$kube delete service $svcName"
iex "$kube delete deployment $svcName"
