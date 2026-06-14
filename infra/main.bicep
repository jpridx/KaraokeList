@description('Base name for Azure resources (letters and numbers, globally unique for SQL server).')
param baseName string

@description('Azure region for SQL and API App Service. Defaults to centralus.')
param location string = 'centralus'

@description('Region for Static Web App (Free tier: centralus, westus2, westeurope, eastasia).')
param staticWebAppLocation string = 'centralus'

@description('SQL admin login name.')
param sqlAdminLogin string

@secure()
@description('SQL admin password.')
param sqlAdminPassword string

@description('App Service plan SKU (B1 is a low-cost starting tier).')
param appServicePlanSku string = 'B1'

var sqlServerName = 'sql-${baseName}'
var databaseName = 'KaraokeList'
var appServicePlanName = 'asp-${baseName}'
var apiWebAppName = 'api-${baseName}'
var staticWebAppName = 'stapp-${baseName}'

resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource firewallAzure 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource database 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: 'GP_S_Gen5'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 1
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 34359738368
    requestedBackupStorageRedundancy: 'Local'
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
          value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${databaseName};User ID=${sqlAdminLogin};Password=${sqlAdminPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;MultipleActiveResultSets=true'
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
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output databaseName string = database.name
