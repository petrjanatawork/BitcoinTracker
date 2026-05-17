#!/bin/bash

# =============================================================================
# Docker Test Runner Script for Bitcoin Tracker
# 
# This script builds a Docker image with .NET 8 SDK and runs the unit tests
# inside a container. Useful when .NET is not installed locally.
# =============================================================================

set -e

# Variables
IMAGE_NAME="dotnet-test-runner"
CONTAINER_NAME="dotnet-test-container"
PROJECT_DIR=$(pwd)
TEST_PROJECT="BitcoinTracker.Tests/BitcoinTracker.Tests.csproj"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}=== Bitcoin Tracker Docker Test Runner ===${NC}"

# Step 1: Build the test runner image using the existing Dockerfile.test
# (no longer overwrites Dockerfile.test at runtime)
echo -e "${GREEN}Building test runner image from Dockerfile.test...${NC}"
docker build -t $IMAGE_NAME -f Dockerfile.test .

# Step 2: Run the tests inside the container with code coverage
echo -e "${GREEN}Running tests with code coverage...${NC}"
mkdir -p "$PROJECT_DIR/TestResults"
docker run --rm --name $CONTAINER_NAME \
    -v "$PROJECT_DIR/TestResults:/app/TestResults" \
    $IMAGE_NAME dotnet test "$TEST_PROJECT" \
    --collect:"XPlat Code Coverage" \
    --results-directory /app/TestResults \
    -v normal

# Step 3: Report coverage results location
if ls "$PROJECT_DIR/TestResults"/*/coverage.cobertura.xml 1>/dev/null 2>&1; then
    echo -e "${GREEN}Coverage report generated. Convert to HTML using:${NC}"
    echo -e "  reportgenerator -reports:TestResults/**/coverage.cobertura.xml -targetdir:coveragereport -reporttypes:Html"
fi

echo -e "${GREEN}=== Tests completed ===${NC}"
