# ============================================================================
# BH Billing Application - k6 Performance Test Docker Image
# ============================================================================
# Multi-stage build for running k6 performance tests in containers.
#
# Build:
#   docker build -t bh-billing-perf-tests .
#
# Run:
#   docker run -e ENV=qa -e CONCURRENCY=300 bh-billing-perf-tests load
#   docker run -e ENV=qa bh-billing-perf-tests smoke
#   docker run -e ENV=stg -e CONCURRENCY=100 -v $(pwd)/results:/app/results bh-billing-perf-tests load
#
# With InfluxDB/Grafana:
#   docker run -e ENV=qa -e CONCURRENCY=300 \
#     -e K6_INFLUXDB_URL=http://influxdb:8086/k6 \
#     bh-billing-perf-tests load
# ============================================================================

FROM grafana/k6:latest

# Set working directory
WORKDIR /app

# Copy performance test framework files
COPY config/ ./config/
COPY data/ ./data/
COPY scenarios/ ./scenarios/
COPY services/ ./services/
COPY utils/ ./utils/
COPY scripts/ ./scripts/
COPY *.js ./

# Create output directories
RUN mkdir -p results reports

# Default environment variables
ENV ENV=qa
ENV CONCURRENCY=100

# Entry point: accepts test type as argument (smoke, load, stress, spike, soak)
ENTRYPOINT ["sh", "-c"]
CMD ["k6 run load-test.js -e ENV=${ENV} -e CONCURRENCY=${CONCURRENCY} --out json=results/results.json --out csv=results/metrics.csv"]
