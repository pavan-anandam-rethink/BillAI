export const environment = {
  production: false,
  clientApiBaseUrl: 'https://rta-billing-dev-app-bh-svc.agreeableforest-1d2b8eb2.eastus2.azurecontainerapps.io',
  claimApiBaseUrl: 'https://rta-billing-dev-app-claim-svc.agreeableforest-1d2b8eb2.eastus2.azurecontainerapps.io',
  authApiBaseUrl: 'https://rta-billing-dev-app-auth-svc.agreeableforest-1d2b8eb2.eastus2.azurecontainerapps.io',
  reportingApiBaseUrl:"https://rta-billing-dev-app-rpt-svc.agreeableforest-1d2b8eb2.eastus2.azurecontainerapps.io",
  //rethinkBHUrl: 'http://dev-billing-web.rethinkbhbeta.com',
  rethinkBHUrl: 'https://rta-bh-dev-billing-web.azurewebsites.net',
  token: 'eyJhbGciOiJodHRwOi8vd3d3LnczLm9yZy8yMDAxLzA0L3htbGRzaWctbW9yZSNobWFjLXNoYTI1NiIsInR5cCI6IkpXVCJ9.eyJBY2NvdW50SW5mb0lkIjoiMTg0MjEiLCJNZW1iZXJJZCI6IjEwNTgxNSIsIk1lbWJlck5hbWUiOiJIZWFsdGhjYXJlXzA4MDE1IiwiTWVtYmVyUm9sZSI6IlJvbGUgNGEiLCJQZXJtaXNzaW9ucyI6WyJiaWxsaW5ndmlldyIsImJpbGxpbmdlZGl0IiwiYmlsbGluZ2VkaXRhcHByb3ZlZGFwcG9pbnRtZW50cyIsImJpbGxpbmdhcHByb3ZlIiwiYmlsbGluZ3N1Ym1pdGNsYWltcyIsImJpbGxpbmdwb3N0cGF5bWVudHMiLCJiaWxsaW5ncmVvcGVuZW5jb3VudGVyIiwiYmlsbGluZ2Nsb3NlZW5jb3VudGVycyJdLCJleHAiOjE3OTc0ODc0MTAsImlzcyI6IlJldGhpbmsgQmlsbGluZyIsImF1ZCI6IlJldGhpbmsgQmlsbGluZyJ9.7mkzEbehKZU6PPiByMhgcTZUnhAonpU1E8XowDxZ_rc',
  cluster: "us2",
  key: "8a506dfbfde9176d6e7d",
  reasonCodesCacheTTL: 60 * 60 * 1000, // 1 hour cache in milliseconds
  personaBaseUrl: 'https://rethink-billing-payments-service-qa.azurewebsites.net/api/v1/PersonaPay',
  personaApiKey: '0017c0a2-8ddf-4486-94b7-178e457fa5aa'
};