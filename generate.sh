#!/bin/sh
set -e

API_URL="${API_URL:-http://api:8080}"

echo "Waiting for API to be ready..."
until wget -qO /dev/null "${API_URL}/health" 2>/dev/null; do
    sleep 2
done

echo "API is ready — starting traffic generation"

CITIES="London Paris Tokyo NewYork Sydney Berlin Mumbai Cairo SaoPaulo Toronto"

while true; do
    # All-cities forecast
    wget -qO /dev/null "${API_URL}/weatherforecast" 2>/dev/null || true
    sleep 0.5

    # Per-city forecasts
    for city in $CITIES; do
        wget -qO /dev/null "${API_URL}/weatherforecast/${city}" 2>/dev/null || true
        sleep 0.3
    done

    # Chain request — multi-hop distributed trace
    wget -qO /dev/null "${API_URL}/chain" 2>/dev/null || true
    sleep 1

    # Slow endpoint
    wget -qO /dev/null -T 10 "${API_URL}/slow" 2>/dev/null || true
    sleep 0.5

    # Error endpoint
    wget -qO /dev/null "${API_URL}/error" 2>/dev/null || true
    sleep 0.5

    echo "$(date): cycle done"
    sleep 2
done
