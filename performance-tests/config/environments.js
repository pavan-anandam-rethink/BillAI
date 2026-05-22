/**
 * Environment Configuration Module
 * ---------------------------------
 * Centralized environment settings for QA, STG, and PROD.
 * Selected via the ENV environment variable (e.g., k6 run -e ENV=qa ...).
 */

// Environment-specific configuration map
const environments = {
  qa: {
    baseUrl: 'https://qa01-web.rethinkbhbeta.com',
    apiBaseUrl: 'https://qa01-api.rethinkbhbeta.com',
    webAppUrl: 'https://qa01-web.rethinkbhbeta.com/Healthcare#/Main',
    authUrl: 'https://qa01-api.rethinkbhbeta.com/api/auth',
    name: 'QA',
  },
  stg: {
    baseUrl: 'https://stg-web.rethinkbhbeta.com',
    apiBaseUrl: 'https://stg-api.rethinkbhbeta.com',
    webAppUrl: 'https://stg-web.rethinkbhbeta.com/Healthcare#/Main',
    authUrl: 'https://stg-api.rethinkbhbeta.com/api/auth',
    name: 'STG',
  },
  prod: {
    baseUrl: 'https://web.rethinkbh.com',
    apiBaseUrl: 'https://api.rethinkbh.com',
    webAppUrl: 'https://web.rethinkbh.com/Healthcare#/Main',
    authUrl: 'https://api.rethinkbh.com/api/auth',
    name: 'PROD',
  },
};

/**
 * Resolves the current environment configuration.
 * Defaults to 'qa' if ENV is not specified.
 * @returns {Object} Environment configuration object
 */
export function getEnvironment() {
  const env = (__ENV.ENV || 'qa').toLowerCase();
  const config = environments[env];
  if (!config) {
    throw new Error(`Unknown environment: ${env}. Valid options: qa, stg, prod`);
  }
  return config;
}

export default environments;
