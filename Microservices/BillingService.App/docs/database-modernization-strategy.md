# Database Modernization Strategy (Schema-Compatible)

## Immediate actions (safe)

1. Add nonclustered indexes for claim, invoice, and payment query paths.
2. Ensure statistics update job frequency aligns with billing load.
3. Enable query store for regression detection.

## Near-term actions

1. Route reporting-heavy reads to read replicas.
2. Partition archival tables by billing period (monthly/quarterly) where table size warrants.
3. Introduce archived historical tables for old claim/payment records.

## TempDB and contention recommendations

- Multiple TempDB data files sized equally.
- Monitor PAGELATCH waits.
- Move expensive report joins to pre-aggregated materialized structures when parity is validated.

## Safety

- No contract-breaking schema changes in compatibility phase.
- All optimizations first validated in lower environment with parity checks.

