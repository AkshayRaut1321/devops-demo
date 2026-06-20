# ==============================
# FIXED VALUES (DO NOT CHANGE)
# ==============================
$RG="DevOpsDemoRG"
$AKS="DevOpsDemoAKSCluster"
$ACR="devopsdemoakshayacr"
$NAMESPACE="project-a"
$LOCATION="eastus"

# ==============================
# LOGIN & SUBSCRIPTION
# ==============================
az login | Out-Null

$SUB_ID=$(az account show --query id -o tsv)
Write-Host "Using Subscription: $SUB_ID"

# ==============================
# USER INPUT (SECURE)
# ==============================

$MongoConn = Read-Host "Enter MongoDB Connection String (mongodb+srv://...)"
$ElasticNode = Read-Host "Enter Elastic Node URL"
$ElasticUser = Read-Host "Enter Elastic Username (usually elastic)"
$ElasticPass = Read-Host "Enter Elastic Password" -AsSecureString
$ElasticCloudId = Read-Host "Enter Elastic CloudId"

# Convert SecureString to plain for kubectl
$ElasticPassPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($ElasticPass)
)

# ==============================
# CREATE ACR (if not exists)
# ==============================
az acr create `
  --resource-group $RG `
  --name $ACR `
  --sku Basic `
  --location $LOCATION `
  --output none 2>$null

# ==============================
# CREATE AKS (if not exists)
# ==============================
az aks create `
  --resource-group $RG `
  --name $AKS `
  --node-count 1 `
  --generate-ssh-keys `
  --location $LOCATION `
  --output none 2>$null

# Attach ACR to AKS
az aks update `
  --name $AKS `
  --resource-group $RG `
  --attach-acr $ACR `
  --output none

# ==============================
# CREATE GITHUB SERVICE PRINCIPAL
# ==============================
Write-Host "Creating GitHub Service Principal..."
az ad sp create-for-rbac `
  --name github-devops-sp `
  --role contributor `
  --scopes /subscriptions/$SUB_ID/resourceGroups/$RG `
  --sdk-auth

Write-Host ""
Write-Host "Copy the above JSON and store it in GitHub Secret: AZURE_CREDENTIALS"
Write-Host ""

# ==============================
# GET KUBECONFIG
# ==============================
az aks get-credentials -g $RG -n $AKS --overwrite-existing

# ==============================
# CREATE NAMESPACE
# ==============================
kubectl get namespace $NAMESPACE 2>$null `
  || kubectl create namespace $NAMESPACE

# ==============================
# CREATE SECRETS
# ==============================
kubectl create secret generic project-a-secrets `
  --namespace $NAMESPACE `
  --from-literal=MongoDbWebApi__ConnectionString="$MongoConn" `
  --from-literal=ElasticSearchWebApi__NodeUrl="$ElasticNode" `
  --from-literal=ElasticSearchWebApi__Username="$ElasticUser" `
  --from-literal=ElasticSearchWebApi__Password="$ElasticPassPlain" `
  --from-literal=ElasticSearchWebApi__CloudId="$ElasticCloudId" `
  --dry-run=client -o yaml | kubectl apply -f -

# ==============================
# CREATE CONFIGMAP
# ==============================
kubectl create configmap project-a-config `
  --namespace $NAMESPACE `
  --from-literal=ElasticSearchWebApi__IndexName="products_v1" `
  --from-literal=ElasticSearchWebApi__IndexAlias="products_current" `
  --from-literal=ElasticSearchWebApi__EnableChangeStreams="true" `
  --dry-run=client -o yaml | kubectl apply -f -

# ==============================
# GET OUTBOUND IP (Whitelist in MongoDB Atlas)
# ==============================
$OUTBOUND_ID=$(az aks show `
  --resource-group $RG `
  --name $AKS `
  --query "networkProfile.loadBalancerProfile.effectiveOutboundIPs[0].id" `
  -o tsv)

$PUBLIC_IP=$(az network public-ip show `
  --ids $OUTBOUND_ID `
  --query ipAddress `
  -o tsv)

Write-Host ""
Write-Host "===================================="
Write-Host "Whitelist this IP in MongoDB Atlas:"
Write-Host $PUBLIC_IP
Write-Host "===================================="