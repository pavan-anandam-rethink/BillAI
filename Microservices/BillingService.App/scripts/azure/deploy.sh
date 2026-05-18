#!/usr/bin/env bash
set -euo pipefail

ENVIRONMENT="${1:-initial}"
RESOURCE_GROUP="${2:-rg-billingservice-${ENVIRONMENT}}"
LOCATION="${3:-eastus2}"

PARAM_FILE="scripts/azure/parameters.${ENVIRONMENT}.json"

if [[ ! -f "${PARAM_FILE}" ]]; then
  echo "Parameter file not found: ${PARAM_FILE}"
  exit 1
fi

az group create --name "${RESOURCE_GROUP}" --location "${LOCATION}"
az deployment group create \
  --resource-group "${RESOURCE_GROUP}" \
  --template-file scripts/azure/main.bicep \
  --parameters "@${PARAM_FILE}"

