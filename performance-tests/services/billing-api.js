/**
 * Billing API Service Module
 * ---------------------------
 * API abstraction layer for BH Billing Application endpoints.
 * Provides methods for each billing module/transaction group.
 * All methods accept an auth token and return the API response.
 */

import { group } from 'k6';
import { getEnvironment } from '../config/environments.js';
import { get, post, getHeaders, validateResponse } from '../utils/http-client.js';
import { paginate } from '../utils/helpers.js';
import {
  dashboardDuration,
  pendingReviewDuration,
  readyToBillDuration,
  billedPendingDuration,
  completedClaimsDuration,
  rejectedClaimsDuration,
  deniedClaimsDuration,
  paymentPostingDuration,
  pendingCollectionDuration,
  recordSuccess,
  recordFailure,
} from '../utils/metrics.js';

/**
 * Fetches the main dashboard data.
 * Transaction Group: Dashboard
 * @param {string} token - Bearer token
 * @param {number} accountInfoId - Account identifier
 * @returns {Object} API response
 */
export function getDashboard(token, accountInfoId) {
  const env = getEnvironment();
  const params = {
    headers: getHeaders(token),
    tags: { name: 'API_Dashboard' },
  };

  return group('Dashboard', function () {
    const startTime = Date.now();
    const response = post(
      `${env.apiBaseUrl}/Claim/GetClaimHeadersAsync`,
      {
        accountInfoId: accountInfoId,
        ...paginate(),
      },
      params
    );
    const duration = Date.now() - startTime;

    // Record metrics and validate
    dashboardDuration.add(duration);
    const success = validateResponse(response, 'Dashboard');
    if (success) {
      recordSuccess('Dashboard', duration);
    } else {
      recordFailure('Dashboard', duration);
    }

    return response;
  });
}

/**
 * Fetches Pending Review claims.
 * Transaction Group: PendingReview
 * @param {string} token - Bearer token
 * @param {number} accountInfoId - Account identifier
 * @returns {Object} API response
 */
export function getPendingReview(token, accountInfoId) {
  const env = getEnvironment();
  const params = {
    headers: getHeaders(token),
    tags: { name: 'API_PendingReview' },
  };

  return group('PendingReview', function () {
    const startTime = Date.now();
    const response = post(
      `${env.apiBaseUrl}/Claim/GetPendingReviewClaimsAsync`,
      {
        accountInfoId: accountInfoId,
        claimStatusId: 1,
        ...paginate(),
      },
      params
    );
    const duration = Date.now() - startTime;

    pendingReviewDuration.add(duration);
    const success = validateResponse(response, 'PendingReview');
    if (success) {
      recordSuccess('PendingReview', duration);
    } else {
      recordFailure('PendingReview', duration);
    }

    return response;
  });
}

/**
 * Fetches Ready To Bill claims.
 * Transaction Group: ReadyToBill
 * @param {string} token - Bearer token
 * @param {number} accountInfoId - Account identifier
 * @returns {Object} API response
 */
export function getReadyToBill(token, accountInfoId) {
  const env = getEnvironment();
  const params = {
    headers: getHeaders(token),
    tags: { name: 'API_ReadyToBill' },
  };

  return group('ReadyToBill', function () {
    const startTime = Date.now();
    const response = post(
      `${env.apiBaseUrl}/Claim/GetReadyToBillClaimsAsync`,
      {
        accountInfoId: accountInfoId,
        claimStatusId: 2,
        ...paginate(),
      },
      params
    );
    const duration = Date.now() - startTime;

    readyToBillDuration.add(duration);
    const success = validateResponse(response, 'ReadyToBill');
    if (success) {
      recordSuccess('ReadyToBill', duration);
    } else {
      recordFailure('ReadyToBill', duration);
    }

    return response;
  });
}

/**
 * Fetches Billed Pending claims.
 * Transaction Group: BilledPending
 * @param {string} token - Bearer token
 * @param {number} accountInfoId - Account identifier
 * @returns {Object} API response
 */
export function getBilledPending(token, accountInfoId) {
  const env = getEnvironment();
  const params = {
    headers: getHeaders(token),
    tags: { name: 'API_BilledPending' },
  };

  return group('BilledPending', function () {
    const startTime = Date.now();
    const response = post(
      `${env.apiBaseUrl}/Claim/GetBilledPendingClaimsAsync`,
      {
        accountInfoId: accountInfoId,
        claimStatusId: 3,
        ...paginate(),
      },
      params
    );
    const duration = Date.now() - startTime;

    billedPendingDuration.add(duration);
    const success = validateResponse(response, 'BilledPending');
    if (success) {
      recordSuccess('BilledPending', duration);
    } else {
      recordFailure('BilledPending', duration);
    }

    return response;
  });
}

/**
 * Fetches Completed Claims.
 * Transaction Group: CompletedClaims
 * @param {string} token - Bearer token
 * @param {number} accountInfoId - Account identifier
 * @returns {Object} API response
 */
export function getCompletedClaims(token, accountInfoId) {
  const env = getEnvironment();
  const params = {
    headers: getHeaders(token),
    tags: { name: 'API_CompletedClaims' },
  };

  return group('CompletedClaims', function () {
    const startTime = Date.now();
    const response = post(
      `${env.apiBaseUrl}/Claim/GetCompletedClaimsAsync`,
      {
        accountInfoId: accountInfoId,
        claimStatusId: 4,
        ...paginate(),
      },
      params
    );
    const duration = Date.now() - startTime;

    completedClaimsDuration.add(duration);
    const success = validateResponse(response, 'CompletedClaims');
    if (success) {
      recordSuccess('CompletedClaims', duration);
    } else {
      recordFailure('CompletedClaims', duration);
    }

    return response;
  });
}

/**
 * Fetches Rejected Claims.
 * Transaction Group: RejectedClaims
 * @param {string} token - Bearer token
 * @param {number} accountInfoId - Account identifier
 * @returns {Object} API response
 */
export function getRejectedClaims(token, accountInfoId) {
  const env = getEnvironment();
  const params = {
    headers: getHeaders(token),
    tags: { name: 'API_RejectedClaims' },
  };

  return group('RejectedClaims', function () {
    const startTime = Date.now();
    const response = post(
      `${env.apiBaseUrl}/Claim/GetRejectedClaimsAsync`,
      {
        accountInfoId: accountInfoId,
        claimStatusId: 5,
        ...paginate(),
      },
      params
    );
    const duration = Date.now() - startTime;

    rejectedClaimsDuration.add(duration);
    const success = validateResponse(response, 'RejectedClaims');
    if (success) {
      recordSuccess('RejectedClaims', duration);
    } else {
      recordFailure('RejectedClaims', duration);
    }

    return response;
  });
}

/**
 * Fetches Denied Claims.
 * Transaction Group: DeniedClaims
 * @param {string} token - Bearer token
 * @param {number} accountInfoId - Account identifier
 * @returns {Object} API response
 */
export function getDeniedClaims(token, accountInfoId) {
  const env = getEnvironment();
  const params = {
    headers: getHeaders(token),
    tags: { name: 'API_DeniedClaims' },
  };

  return group('DeniedClaims', function () {
    const startTime = Date.now();
    const response = post(
      `${env.apiBaseUrl}/Claim/GetDeniedClaimsAsync`,
      {
        accountInfoId: accountInfoId,
        claimStatusId: 6,
        ...paginate(),
      },
      params
    );
    const duration = Date.now() - startTime;

    deniedClaimsDuration.add(duration);
    const success = validateResponse(response, 'DeniedClaims');
    if (success) {
      recordSuccess('DeniedClaims', duration);
    } else {
      recordFailure('DeniedClaims', duration);
    }

    return response;
  });
}

/**
 * Fetches Payment Posting data.
 * Transaction Group: PaymentPosting
 * @param {string} token - Bearer token
 * @param {number} accountInfoId - Account identifier
 * @returns {Object} API response
 */
export function getPaymentPosting(token, accountInfoId) {
  const env = getEnvironment();
  const params = {
    headers: getHeaders(token),
    tags: { name: 'API_PaymentPosting' },
  };

  return group('PaymentPosting', function () {
    const startTime = Date.now();
    const response = post(
      `${env.apiBaseUrl}/Claim/GetPaymentPostingClaimsAsync`,
      {
        accountInfoId: accountInfoId,
        claimStatusId: 7,
        ...paginate(),
      },
      params
    );
    const duration = Date.now() - startTime;

    paymentPostingDuration.add(duration);
    const success = validateResponse(response, 'PaymentPosting');
    if (success) {
      recordSuccess('PaymentPosting', duration);
    } else {
      recordFailure('PaymentPosting', duration);
    }

    return response;
  });
}

/**
 * Fetches Pending Collection data.
 * Transaction Group: PendingCollection
 * @param {string} token - Bearer token
 * @param {number} accountInfoId - Account identifier
 * @returns {Object} API response
 */
export function getPendingCollection(token, accountInfoId) {
  const env = getEnvironment();
  const params = {
    headers: getHeaders(token),
    tags: { name: 'API_PendingCollection' },
  };

  return group('PendingCollection', function () {
    const startTime = Date.now();
    const response = post(
      `${env.apiBaseUrl}/Claim/GetPendingCollectionClaimsAsync`,
      {
        accountInfoId: accountInfoId,
        claimStatusId: 8,
        ...paginate(),
      },
      params
    );
    const duration = Date.now() - startTime;

    pendingCollectionDuration.add(duration);
    const success = validateResponse(response, 'PendingCollection');
    if (success) {
      recordSuccess('PendingCollection', duration);
    } else {
      recordFailure('PendingCollection', duration);
    }

    return response;
  });
}

export default {
  getDashboard,
  getPendingReview,
  getReadyToBill,
  getBilledPending,
  getCompletedClaims,
  getRejectedClaims,
  getDeniedClaims,
  getPaymentPosting,
  getPendingCollection,
};
