Param (
    $svcName = $env:k8ServiceName
)
$env:KUBECONFIG="kubeconfig.centralus.json"
.\kubectl.exe delete service $svcName
.\kubectl.exe delete deployment $svcName
