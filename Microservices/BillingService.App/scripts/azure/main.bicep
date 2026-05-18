param location string = resourceGroup().location
param environmentName string = 'prod'
param aksNodeCount int = 3
param aksVmSize string = 'Standard_D4s_v5'
param sqlSkuName string = 'BC_Gen5_4'
param redisSkuName string = 'Premium'
param redisCapacity int = 2
@secure()
param sqlAdminPassword string
param sqlAdminUser string = 'sqladminuser'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'log-${environmentName}-billing'
  location: location
  properties: {
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-${environmentName}-billing'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

resource aks 'Microsoft.ContainerService/managedClusters@2024-10-01' = {
  name: 'aks-${environmentName}-billing'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    dnsPrefix: 'aks-${environmentName}-billing'
    agentPoolProfiles: [
      {
        name: 'apipool'
        mode: 'System'
        count: aksNodeCount
        vmSize: aksVmSize
        type: 'VirtualMachineScaleSets'
        osType: 'Linux'
        maxPods: 110
      }
    ]
    networkProfile: {
      networkPlugin: 'azure'
      networkPolicy: 'azure'
    }
    addonProfiles: {
      omsagent: {
        enabled: true
        config: {
          logAnalyticsWorkspaceResourceID: logAnalytics.id
        }
      }
    }
  }
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: 'sql-${environmentName}-billing'
  location: location
  properties: {
    administratorLogin: sqlAdminUser
    administratorLoginPassword: sqlAdminPassword
    publicNetworkAccess: 'Disabled'
  }
}

resource sqlDb 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  name: '${sqlServer.name}/BillingDb'
  location: location
  sku: {
    name: sqlSkuName
    tier: 'BusinessCritical'
  }
  properties: {
    zoneRedundant: true
    readScale: 'Enabled'
  }
}

resource redis 'Microsoft.Cache/Redis@2024-03-01' = {
  name: 'redis-${environmentName}-billing'
  location: location
  sku: {
    name: redisSkuName
    family: 'P'
    capacity: redisCapacity
  }
  properties: {
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
  }
}

resource serviceBus 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: 'sb-${environmentName}-billing'
  location: location
  sku: {
    name: 'Premium'
    tier: 'Premium'
    capacity: 1
  }
  properties: {
    zoneRedundant: true
  }
}

output aksName string = aks.name
output sqlServerName string = sqlServer.name
output sqlDatabaseName string = sqlDb.name
output redisName string = redis.name
output serviceBusNamespace string = serviceBus.name
output appInsightsConnectionString string = appInsights.properties.ConnectionString

