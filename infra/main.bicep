@description('Base name for Azure resources (letters and numbers).')
param baseName string

@description('Azure region for API App Service. Defaults to centralus.')
param location string = 'centralus'

@description('Region for Static Web App (Free tier: centralus, westus2, westeurope, eastasia).')
param staticWebAppLocation string = 'centralus'

@description('App Service plan SKU (B1 is a low-cost starting tier).')
param appServicePlanSku string = 'B1'

var appServicePlanName = 'asp-${baseName}'
var apiWebAppName = 'api-${baseName}'
var staticWebAppName = 'stapp-${baseName}'
var logAnalyticsWorkspaceName = 'law-${baseName}'
var appInsightsName = 'appi-${baseName}'
var sqliteConnectionString = 'Data Source=/home/data/karaokelist.db'

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: appServicePlanSku
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource apiWebApp 'Microsoft.Web/sites@2023-12-01' = {
  name: apiWebAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: false
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: sqliteConnectionString
        }
        {
          name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
          value: 'true'
        }
        {
          name: 'Jwt__Issuer'
          value: 'KaraokeList'
        }
        {
          name: 'Jwt__Audience'
          value: 'KaraokeList.Web'
        }
        {
          name: 'Security__Registration__RequireInviteCode'
          value: 'true'
        }
        {
          name: 'ApplicationInsights__ConnectionString'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
      ]
    }
  }
}

resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: staticWebAppName
  location: staticWebAppLocation
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
  }
}

output apiWebAppName string = apiWebApp.name
output apiWebAppDefaultHostName string = apiWebApp.properties.defaultHostName
output staticWebAppName string = staticWebApp.name
output staticWebAppDefaultHostName string = staticWebApp.properties.defaultHostname
@secure()
output staticWebAppDeploymentToken string = staticWebApp.listSecrets().properties.apiKey
output appInsightsName string = appInsights.name
output appInsightsConnectionString string = appInsights.properties.ConnectionString
output sqliteDataPath string = '/home/data/karaokelist.db'
