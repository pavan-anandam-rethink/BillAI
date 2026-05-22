/**
 * Report Generator Utility
 * -------------------------
 * Provides functions for generating summary reports, tracking slow APIs,
 * and exporting metrics data for post-test analysis.
 * Used in teardown() for final report generation.
 */

/**
 * Generates a console summary report from k6 metrics data.
 * Logs key performance indicators to console output.
 * @param {Object} data - Data passed from setup() through VU execution
 */
export function generateConsoleSummary(data) {
  console.log('='.repeat(80));
  console.log('  BH BILLING APPLICATION - PERFORMANCE TEST SUMMARY');
  console.log('='.repeat(80));
  console.log(`  Environment: ${data.environment || 'QA'}`);
  console.log(`  Test Start:  ${data.testStartTime || 'N/A'}`);
  console.log(`  Concurrency: ${data.concurrency || 'N/A'} VUs`);
  console.log('='.repeat(80));
}

/**
 * Tracks and reports slow API responses.
 * APIs with response times above threshold are flagged.
 * @param {Array} slowApis - Array of { url, duration, status } objects
 * @param {number} threshold - Threshold in ms (default: 3000)
 */
export function reportSlowApis(slowApis, threshold = 3000) {
  if (!slowApis || slowApis.length === 0) {
    console.log('\n[SLOW API REPORT] No APIs exceeded the threshold.');
    return;
  }

  // Sort by duration descending
  const sorted = slowApis.sort((a, b) => b.duration - a.duration);
  const top10 = sorted.slice(0, 10);

  console.log('\n' + '='.repeat(80));
  console.log(`  TOP ${top10.length} SLOW TRANSACTIONS (Threshold: ${threshold}ms)`);
  console.log('='.repeat(80));
  console.log(
    `  ${'#'.padEnd(4)} ${'Transaction'.padEnd(30)} ${'Duration(ms)'.padEnd(15)} Status`
  );
  console.log('-'.repeat(80));

  top10.forEach((api, index) => {
    console.log(
      `  ${String(index + 1).padEnd(4)} ${(api.url || api.name || 'Unknown').substring(0, 28).padEnd(30)} ${String(Math.round(api.duration)).padEnd(15)} ${api.status || 'N/A'}`
    );
  });

  console.log('='.repeat(80));
}

/**
 * Generates a failure summary from collected error data.
 * @param {Array} failures - Array of { url, status, message, timestamp } objects
 */
export function reportFailures(failures) {
  if (!failures || failures.length === 0) {
    console.log('\n[FAILURE REPORT] No failures recorded.');
    return;
  }

  console.log('\n' + '='.repeat(80));
  console.log(`  FAILURE REPORT - Total Failures: ${failures.length}`);
  console.log('='.repeat(80));

  // Group failures by URL
  const grouped = {};
  failures.forEach((f) => {
    const key = f.url || 'unknown';
    if (!grouped[key]) {
      grouped[key] = { count: 0, statuses: new Set() };
    }
    grouped[key].count++;
    grouped[key].statuses.add(f.status);
  });

  Object.entries(grouped).forEach(([url, data]) => {
    console.log(
      `  ${url.substring(0, 50).padEnd(52)} Count: ${String(data.count).padEnd(6)} Statuses: [${[...data.statuses].join(', ')}]`
    );
  });

  console.log('='.repeat(80));
}

/**
 * Custom handleSummary function for k6 to generate multiple report formats.
 * Call this from the main test script's handleSummary export.
 * @param {Object} data - k6 summary data object
 * @param {string} testName - Name of the test for file naming
 * @returns {Object} k6 handleSummary output object
 */
export function generateReports(data, testName = 'load-test') {
  const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
  const fileName = `${testName}_${timestamp}`;

  return {
    // Console stdout summary
    stdout: generateTextSummary(data),
    // JSON results file
    [`results/${fileName}.json`]: JSON.stringify(data, null, 2),
    // HTML report
    [`reports/${fileName}.html`]: generateHtmlReport(data, testName),
  };
}

/**
 * Generates a text-based summary for console output.
 * @param {Object} data - k6 summary data
 * @returns {string} Formatted text summary
 */
function generateTextSummary(data) {
  let summary = '\n';
  summary += '='.repeat(80) + '\n';
  summary += '  BH BILLING APPLICATION - K6 PERFORMANCE TEST RESULTS\n';
  summary += '='.repeat(80) + '\n\n';

  // Extract key metrics
  if (data.metrics) {
    const metrics = data.metrics;

    if (metrics.http_req_duration) {
      const d = metrics.http_req_duration.values;
      summary += '  HTTP Request Duration:\n';
      summary += `    Avg:  ${(d.avg || 0).toFixed(2)}ms\n`;
      summary += `    Min:  ${(d.min || 0).toFixed(2)}ms\n`;
      summary += `    Max:  ${(d.max || 0).toFixed(2)}ms\n`;
      summary += `    P90:  ${(d['p(90)'] || 0).toFixed(2)}ms\n`;
      summary += `    P95:  ${(d['p(95)'] || 0).toFixed(2)}ms\n`;
      summary += `    P99:  ${(d['p(99)'] || 0).toFixed(2)}ms\n\n`;
    }

    if (metrics.http_reqs) {
      summary += `  Total HTTP Requests: ${metrics.http_reqs.values.count || 0}\n`;
      summary += `  Throughput: ${(metrics.http_reqs.values.rate || 0).toFixed(2)} req/s\n\n`;
    }

    if (metrics.http_req_failed) {
      const errorPct = ((metrics.http_req_failed.values.rate || 0) * 100).toFixed(2);
      summary += `  Error Rate: ${errorPct}%\n\n`;
    }
  }

  summary += '='.repeat(80) + '\n';
  return summary;
}

/**
 * Generates an HTML performance report.
 * Creates a self-contained HTML file with charts and metrics tables.
 * @param {Object} data - k6 summary data
 * @param {string} testName - Name of the test
 * @returns {string} HTML report content
 */
function generateHtmlReport(data, testName) {
  const metrics = data.metrics || {};
  const duration = metrics.http_req_duration ? metrics.http_req_duration.values : {};
  const reqs = metrics.http_reqs ? metrics.http_reqs.values : {};
  const failed = metrics.http_req_failed ? metrics.http_req_failed.values : {};

  return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>BH Billing - Performance Test Report: ${testName}</title>
  <style>
    * { margin: 0; padding: 0; box-sizing: border-box; }
    body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #f5f7fa; color: #333; }
    .header { background: linear-gradient(135deg, #1a237e, #0d47a1); color: white; padding: 30px 40px; }
    .header h1 { font-size: 24px; margin-bottom: 5px; }
    .header p { opacity: 0.8; font-size: 14px; }
    .container { max-width: 1200px; margin: 0 auto; padding: 20px; }
    .metrics-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 20px; margin: 20px 0; }
    .metric-card { background: white; border-radius: 8px; padding: 20px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
    .metric-card .label { font-size: 12px; text-transform: uppercase; color: #666; margin-bottom: 8px; }
    .metric-card .value { font-size: 28px; font-weight: bold; color: #1a237e; }
    .metric-card .unit { font-size: 14px; color: #999; }
    .metric-card.success .value { color: #2e7d32; }
    .metric-card.warning .value { color: #f57f17; }
    .metric-card.danger .value { color: #c62828; }
    table { width: 100%; border-collapse: collapse; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 4px rgba(0,0,0,0.1); margin: 20px 0; }
    th { background: #1a237e; color: white; padding: 12px 16px; text-align: left; font-size: 13px; }
    td { padding: 10px 16px; border-bottom: 1px solid #eee; font-size: 13px; }
    tr:hover { background: #f5f5f5; }
    .section-title { font-size: 18px; font-weight: 600; margin: 30px 0 10px; color: #1a237e; }
    .sla-pass { color: #2e7d32; font-weight: bold; }
    .sla-fail { color: #c62828; font-weight: bold; }
    .footer { text-align: center; padding: 20px; color: #999; font-size: 12px; }
  </style>
</head>
<body>
  <div class="header">
    <h1>BH Billing Application - Performance Test Report</h1>
    <p>Test: ${testName} | Generated: ${new Date().toISOString()} | Framework: k6</p>
  </div>
  <div class="container">
    <div class="metrics-grid">
      <div class="metric-card">
        <div class="label">Avg Response Time</div>
        <div class="value">${(duration.avg || 0).toFixed(0)}<span class="unit">ms</span></div>
      </div>
      <div class="metric-card ${(duration['p(95)'] || 0) < 3000 ? 'success' : 'danger'}">
        <div class="label">P95 Response Time</div>
        <div class="value">${(duration['p(95)'] || 0).toFixed(0)}<span class="unit">ms</span></div>
      </div>
      <div class="metric-card">
        <div class="label">Min / Max</div>
        <div class="value">${(duration.min || 0).toFixed(0)} / ${(duration.max || 0).toFixed(0)}<span class="unit">ms</span></div>
      </div>
      <div class="metric-card">
        <div class="label">Throughput</div>
        <div class="value">${(reqs.rate || 0).toFixed(1)}<span class="unit">req/s</span></div>
      </div>
      <div class="metric-card">
        <div class="label">Total Requests</div>
        <div class="value">${reqs.count || 0}</div>
      </div>
      <div class="metric-card ${(failed.rate || 0) < 0.01 ? 'success' : 'danger'}">
        <div class="label">Error Rate</div>
        <div class="value">${((failed.rate || 0) * 100).toFixed(2)}<span class="unit">%</span></div>
      </div>
    </div>

    <h2 class="section-title">SLA Compliance</h2>
    <table>
      <thead><tr><th>SLA Metric</th><th>Target</th><th>Actual</th><th>Status</th></tr></thead>
      <tbody>
        <tr>
          <td>P95 Response Time</td>
          <td>&lt; 3000ms</td>
          <td>${(duration['p(95)'] || 0).toFixed(2)}ms</td>
          <td class="${(duration['p(95)'] || 0) < 3000 ? 'sla-pass' : 'sla-fail'}">${(duration['p(95)'] || 0) < 3000 ? 'PASS' : 'FAIL'}</td>
        </tr>
        <tr>
          <td>Error Rate</td>
          <td>&lt; 1%</td>
          <td>${((failed.rate || 0) * 100).toFixed(2)}%</td>
          <td class="${(failed.rate || 0) < 0.01 ? 'sla-pass' : 'sla-fail'}">${(failed.rate || 0) < 0.01 ? 'PASS' : 'FAIL'}</td>
        </tr>
      </tbody>
    </table>

    <h2 class="section-title">Response Time Distribution</h2>
    <table>
      <thead><tr><th>Metric</th><th>Value (ms)</th></tr></thead>
      <tbody>
        <tr><td>Average</td><td>${(duration.avg || 0).toFixed(2)}</td></tr>
        <tr><td>Minimum</td><td>${(duration.min || 0).toFixed(2)}</td></tr>
        <tr><td>Maximum</td><td>${(duration.max || 0).toFixed(2)}</td></tr>
        <tr><td>Median (P50)</td><td>${(duration.med || 0).toFixed(2)}</td></tr>
        <tr><td>P90</td><td>${(duration['p(90)'] || 0).toFixed(2)}</td></tr>
        <tr><td>P95</td><td>${(duration['p(95)'] || 0).toFixed(2)}</td></tr>
        <tr><td>P99</td><td>${(duration['p(99)'] || 0).toFixed(2)}</td></tr>
      </tbody>
    </table>

    <h2 class="section-title">Transaction Groups</h2>
    <table>
      <thead><tr><th>Transaction</th><th>Avg (ms)</th><th>P95 (ms)</th><th>Min (ms)</th><th>Max (ms)</th><th>Count</th></tr></thead>
      <tbody>
        ${generateTransactionRows(metrics)}
      </tbody>
    </table>
  </div>
  <div class="footer">
    <p>Generated by BH Billing Performance Testing Framework | k6 v${data.state ? data.state.testRunDurationMs : 'N/A'}</p>
  </div>
</body>
</html>`;
}

/**
 * Generates HTML table rows for transaction-level metrics.
 * @param {Object} metrics - k6 metrics object
 * @returns {string} HTML table rows
 */
function generateTransactionRows(metrics) {
  const transactionNames = [
    'launch_duration', 'login_duration', 'dashboard_duration',
    'pending_review_duration', 'ready_to_bill_duration', 'billed_pending_duration',
    'completed_claims_duration', 'rejected_claims_duration', 'denied_claims_duration',
    'payment_posting_duration', 'pending_collection_duration',
  ];

  let rows = '';
  transactionNames.forEach((name) => {
    if (metrics[name]) {
      const v = metrics[name].values;
      const displayName = name.replace(/_duration/g, '').replace(/_/g, ' ').replace(/\b\w/g, (c) => c.toUpperCase());
      rows += `<tr>
        <td>${displayName}</td>
        <td>${(v.avg || 0).toFixed(2)}</td>
        <td>${(v['p(95)'] || 0).toFixed(2)}</td>
        <td>${(v.min || 0).toFixed(2)}</td>
        <td>${(v.max || 0).toFixed(2)}</td>
        <td>${v.count || 0}</td>
      </tr>`;
    }
  });

  return rows || '<tr><td colspan="6">No transaction data available</td></tr>';
}

export default { generateConsoleSummary, reportSlowApis, reportFailures, generateReports };
