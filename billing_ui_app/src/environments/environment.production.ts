export const environment = {
  production: true,
  clientApiBaseUrl: 'https://rta-billing-prd-app-bh-svc.graysmoke-c0bdd691.eastus2.azurecontainerapps.io',
  claimApiBaseUrl: 'https://rta-billing-prd-app-claim-svc.graysmoke-c0bdd691.eastus2.azurecontainerapps.io',
  authApiBaseUrl: 'https://rta-billing-prd-app-auth-svc.graysmoke-c0bdd691.eastus2.azurecontainerapps.io',
  reportingApiBaseUrl: "https://rta-billing-prd-app-rpt-svc.graysmoke-c0bdd691.eastus2.azurecontainerapps.io",
  rethinkBHUrl: 'https://webapp.rethinkbehavioralhealth.com',
  token: '',
  cluster: "us2",
  key: "1e45bcf9c4a437f0c242",
  reasonCodesCacheTTL: 60 * 60 * 1000, // 1 hour cache in milliseconds
  personaBaseUrl: 'https://services.rethinkfirst.com/api/payment/api/v1/PersonaPay',
  personaApiKey: '3472f5c9-0357-470f-a7e2-6dfc24ac3d08'
};
