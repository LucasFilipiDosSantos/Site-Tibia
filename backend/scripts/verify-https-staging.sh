#!/bin/bash
#
# SEC-01 Runtime Verification Script
# Validates HTTPS redirect, HSTS header, and no insecure HTTP endpoints in staging
# Produces evidence artifacts for SEC-01 proof
#

set -euo pipefail

# Configuration
STAGING_HOST="${1:-staging.example.com}"
ARTIFACTS_DIR="artifacts/10-sec01-proof-$(date +%Y%m%d-%H%M%S)"
RESULT_FILE="$ARTIFACTS_DIR/result.json"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Initialize results
HTTP_REDIRECT=false
HSTS_PRESENT=false
NO_PUBLIC_HTTP=false

# Create artifacts directory
mkdir -p "$ARTIFACTS_DIR"

log_info "Starting SEC-01 verification for: $STAGING_HOST"
log_info "Artifacts directory: $ARTIFACTS_DIR"

# Test 1: HTTP to HTTPS redirect
echo "=== Test 1: HTTP Redirect ==="
HTTP_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" -I "http://$STAGING_HOST/" 2>/dev/null || echo "000")
HTTP_LOCATION=$(curl -s -D - "http://$STAGING_HOST/" -o /dev/null 2>/dev/null | grep -i "^Location:" || echo "")

echo "HTTP response code: $HTTP_RESPONSE"
echo "Location header: $HTTP_LOCATION"

# Save redirect response
cat > "$ARTIFACTS_DIR/redirect-response.txt" << EOF
HTTP Response Code: $HTTP_RESPONSE
Location Header: $HTTP_LOCATION
Timestamp: $(date -u +"%Y-%m-%dT%H:%M:%SZ")
EOF

if [ "$HTTP_RESPONSE" = "301" ] && echo "$HTTP_LOCATION" | grep -qi "https://"; then
    HTTP_REDIRECT=true
    log_info "HTTP -> HTTPS redirect: PASSED (301)"
else
    log_error "HTTP redirect FAILED: Expected 301 to HTTPS, got $HTTP_RESPONSE"
fi

# Test 2: HSTS header
echo "=== Test 2: HSTS Header ==="
HSTS_RESPONSE=$(curl -s -D - "https://$STAGING_HOST/" -o /dev/null 2>/dev/null)
HSTS_HEADER=$(echo "$HSTS_RESPONSE" | grep -i "^Strict-Transport-Security:" || echo "")

echo "HSTS Header: $HSTS_HEADER"

# Save HSTS headers
cat > "$ARTIFACTS_DIR/hsts-headers.txt" << EOF
$HSTS_RESPONSE

Timestamp: $(date -u +"%Y-%m-%dT%H:%M:%SZ")
EOF

if [ -n "$HSTS_HEADER" ]; then
    HSTS_PRESENT=true
    
    # Verify HSTS properties per D-12
    if echo "$HSTS_HEADER" | grep -q "max-age=31536000"; then
        log_info "HSTS max-age: PASSED (1 year)"
    else
        log_warn "HSTS max-age not set to 1 year"
    fi
    
    if echo "$HSTS_HEADER" | grep -qi "includeSubDomains"; then
        log_info "HSTS includeSubDomains: PASSED"
    else
        log_warn "HSTS includeSubDomains not found"
    fi
    
    if echo "$HSTS_HEADER" | grep -qi "preload"; then
        log_info "HSTS preload: PASSED"
    else
        log_warn "HSTS preload not found"
    fi
else
    log_error "HSTS header NOT FOUND"
fi

# Test 3: No public HTTP endpoints
echo "=== Test 3: No Public HTTP Endpoints ==="
HTTP_PROBE_RESULTS=""

# Probe common endpoints - all should redirect, not return 200
for endpoint in "/api/health" "/api/orders" "/api/products" "/api/auth/login"; do
    RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" "http://$STAGING_HOST$endpoint" 2>/dev/null || echo "000")
    HTTP_PROBE_RESULTS="$HTTP_PROBE_RESULTS$endpoint: $RESPONSE\n"
    
    if [ "$RESPONSE" = "200" ]; then
        log_error "SECURITY ISSUE: HTTP endpoint $endpoint returned 200!"
    else
        log_info "Endpoint $endpoint: $RESPONSE (should redirect)"
    fi
done

# Save HTTP probe results
cat > "$ARTIFACTS_DIR/http-probe.txt" << EOF
$HTTP_PROBE_RESULTS
Timestamp: $(date -u +"%Y-%m-%dT%H:%M:%SZ")
EOF

# If any endpoint returned 200 on HTTP, fail this test
if echo "$HTTP_PROBE_RESULTS" | grep -q ": 200"; then
    log_error "SECURITY ISSUE: Found HTTP endpoints returning 200!"
    NO_PUBLIC_HTTP=false
else
    NO_PUBLIC_HTTP=true
    log_info "No public HTTP endpoints: PASSED"
fi

# Generate environment info
cat > "$ARTIFACTS_DIR/environment.json" << EOF
{
    "host": "$STAGING_HOST",
    "verification_date": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")",
    "ip": "$(curl -s ifconfig.me 2>/dev/null || echo 'unknown')",
    "artifacts_version": "1.0"
}
EOF

# Generate result.json
cat > "$RESULT_FILE" << EOF
{
    "phase": "10",
    "plan": "02",
    "requirement": "SEC-01",
    "verification_date": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")",
    "host": "$STAGING_HOST",
    "checks": {
        "http_redirect": $HTTP_REDIRECT,
        "hsts_present": $HSTS_PRESENT,
        "no_public_http": $NO_PUBLIC_HTTP
    },
    "overall": "$([ "$HTTP_REDIRECT" = true ] && [ "$HSTS_PRESENT" = true ] && [ "$NO_PUBLIC_HTTP" = true ] && echo "PASSED" || echo "FAILED")"
}
EOF

log_info "Results saved to: $RESULT_FILE"

# Print summary
echo ""
echo "=== SEC-01 Verification Summary ==="
echo "HTTP Redirect: $([ "$HTTP_REDIRECT" = true ] && echo "PASSED" || echo "FAILED")"
echo "HSTS Present: $([ "$HSTS_PRESENT" = true ] && echo "PASSED" || echo "FAILED")"
echo "No Public HTTP: $([ "$NO_PUBLIC_HTTP" = true ] && echo "PASSED" || echo "FAILED")"
echo "Overall: $(cat "$RESULT_FILE" | grep -o '"overall": "[^"]*"' | cut -d'"' -f4)"
echo ""

# Exit with appropriate code
if [ "$HTTP_REDIRECT" = true ] && [ "$HSTS_PRESENT" = true ] && [ "$NO_PUBLIC_HTTP" = true ]; then
    log_info "SEC-01 verification PASSED"
    exit 0
else
    log_error "SEC-01 verification FAILED"
    exit 1
fi