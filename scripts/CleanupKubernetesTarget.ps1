Param (
    $svcName = $env:k8ServiceName
)
$env:KUBECONFIG="kubeconfig.centralus.json"

$kube = $env:AGENT_WORKFOLDER + "\kubectl.exe"

iex "$kube delete service $svcName"
iex "$kube delete deployment $svcName"
