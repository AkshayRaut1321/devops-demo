#!/usr/bin/env bash
set -euo pipefail

# ---------------------------
# Required environment variables (CI will set via GitHub Secrets)
# ---------------------------
: "${AZURE_CLIENT_ID:?}"
: "${AZURE_CLIENT_SECRET:?}"
: "${AZURE_TENANT_ID:?}"
: "${SUBSCRIPTION_ID:?}"
: "${ACR_NAME:?}"
: "${ACR_LOGIN_SERVER:?}"
: "${AKS_RG:?}"
: "${AKS_NAME:?}"
: "${IMAGE_NAME:?}"
: "${IMAGE_TAG:?}"

# ---------------------------
# Optional defaults
# ---------------------------
DOCKERFILE_PATH="${DOCKERFILE_PATH:-DevOpsDemo/Dockerfile}"  # where Dockerfile lives
BUILD_CONTEXT="${BUILD_CONTEXT:-DevOpsDemo}"                 # context = project folder
K8S_DIR="${K8S_DIR:-DevOpsDemo/k8s}"                         # folder with deployment.yaml
DEPLOYMENT_NAME="${DEPLOYMENT_NAME:-devopsdemo-deployment}"
CONTAINER_NAME="${CONTAINER_NAME:-devopsdemo}"

# ---------------------------
# Azure Login
# ---------------------------
echo "🔑 Logging into Azure..."
az login --service-principal \
  -u "$AZURE_CLIENT_ID" \
  -p "$AZURE_CLIENT_SECRET" \
  --tenant "$AZURE_TENANT_ID" >/dev/null

az account set --subscription "$SUBSCRIPTION_ID"

# ---------------------------
# Build & Push Docker Image
# ---------------------------
FULL_IMAGE="${ACR_LOGIN_SERVER}/${IMAGE_NAME}:${IMAGE_TAG}"

echo "🐳 Building Docker image: $FULL_IMAGE"
docker build -f "$DOCKERFILE_PATH" -t "$FULL_IMAGE" "$BUILD_CONTEXT"

echo "📤 Pushing image: $FULL_IMAGE"
az acr login --name "$ACR_NAME"
docker push "$FULL_IMAGE"

# ---------------------------
# Deploy to AKS
# ---------------------------
echo "☸️ Getting AKS credentials..."
az aks get-credentials -g "$AKS_RG" -n "$AKS_NAME" --overwrite-existing

echo "🧩 Updating Kubernetes manifests..."
tmpdir="$(mktemp -d)"
cp -r "$K8S_DIR"/. "$tmpdir"/

# Replace REPLACE_IMAGE_TAG in deployment.yaml with actual IMAGE_TAG
sed -i "s|REPLACE_IMAGE_TAG|${IMAGE_TAG}|g" "$tmpdir"/deployment.yaml

kubectl apply -f "$tmpdir"

echo "⏳ Waiting for rollout..."
kubectl rollout status deployment/"$DEPLOYMENT_NAME" --timeout=300s

echo "✅ Deployment successful. Current pods:"
kubectl get pods -l app="$CONTAINER_NAME" -o wide
