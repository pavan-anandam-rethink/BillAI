# Database Dependency Map

## Primary contexts

- `BillingDbContext` (transactional billing domain)
- `ReportingDbContext` (reporting/materialized query paths)

## Core dependency categories

1. Claims and claim submissions
2. Payments and payment claim mappings
3. Patient invoices and collections
4. Attachments, notes, and audit trails
5. Billing/funder settings and lookup entities

## Stored procedure dependencies (identified)

- `GetClaimsByAccountInfoId`
- `GetClaimsCount`
- `GetClaimsPatientsFilters`
- `GetClaimsFundersFilters`
- `GetClaimsRPFilters`

## Modernization constraints

- No schema-breaking migrations during compatibility phase.
- Index-only optimization in early phases.
- Stored procedure signatures must remain backward compatible.
- Reporting result parity required before swapping read handlers.

