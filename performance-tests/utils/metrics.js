/**
 * Custom Metrics Module
 * ----------------------
 * Defines custom k6 metrics for granular performance measurement.
 * Includes transaction-level Trends, error Counters, and Rate metrics.
 */

import { Trend, Counter, Rate, Gauge } from 'k6/metrics';

// ---- Transaction Duration Trends ----
// Each Trend tracks response time distribution for a specific transaction group
export const transactionDuration = new Trend('transaction_duration', true);
export const launchDuration = new Trend('launch_duration', true);
export const loginDuration = new Trend('login_duration', true);
export const dashboardDuration = new Trend('dashboard_duration', true);
export const pendingReviewDuration = new Trend('pending_review_duration', true);
export const readyToBillDuration = new Trend('ready_to_bill_duration', true);
export const billedPendingDuration = new Trend('billed_pending_duration', true);
export const completedClaimsDuration = new Trend('completed_claims_duration', true);
export const rejectedClaimsDuration = new Trend('rejected_claims_duration', true);
export const deniedClaimsDuration = new Trend('denied_claims_duration', true);
export const paymentPostingDuration = new Trend('payment_posting_duration', true);
export const pendingCollectionDuration = new Trend('pending_collection_duration', true);

// ---- Error Tracking ----
// Counters for total and per-transaction error counts
export const errorCount = new Counter('error_count');
export const errorRate = new Rate('error_rate');
export const loginErrors = new Counter('login_errors');
export const apiErrors = new Counter('api_errors');

// ---- Throughput ----
// Counter for total successful transactions
export const successfulTransactions = new Counter('successful_transactions');
export const failedTransactions = new Counter('failed_transactions');

// ---- Session Metrics ----
// Track active sessions and authentication performance
export const activeUsers = new Gauge('active_users');
export const authDuration = new Trend('auth_duration', true);

/**
 * Records a transaction metric with the given name and duration.
 * Adds data to the global transactionDuration Trend with a tag.
 * @param {string} name - Transaction group name
 * @param {number} duration - Duration in milliseconds
 * @param {boolean} success - Whether the transaction succeeded
 */
export function recordTransaction(name, duration, success = true) {
  transactionDuration.add(duration, { transaction: name });
  if (success) {
    successfulTransactions.add(1);
  } else {
    failedTransactions.add(1);
    errorCount.add(1);
    errorRate.add(1);
  }
}

/**
 * Records a successful transaction.
 * @param {string} name - Transaction group name
 * @param {number} duration - Duration in milliseconds
 */
export function recordSuccess(name, duration) {
  recordTransaction(name, duration, true);
  errorRate.add(0);
}

/**
 * Records a failed transaction.
 * @param {string} name - Transaction group name
 * @param {number} duration - Duration in milliseconds
 */
export function recordFailure(name, duration) {
  recordTransaction(name, duration, false);
}

export default {
  transactionDuration,
  errorCount,
  errorRate,
  successfulTransactions,
  failedTransactions,
  recordTransaction,
  recordSuccess,
  recordFailure,
};
