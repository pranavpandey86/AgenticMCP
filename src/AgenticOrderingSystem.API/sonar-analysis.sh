#!/bin/bash

# Local SonarQube Analysis Script for .NET Projects
# Make sure you have SonarQube scanner installed locally

echo "Starting SonarQube analysis..."

# Install dotnet-sonarscanner if not already installed
if ! command -v dotnet-sonarscanner &> /dev/null; then
    echo "Installing dotnet-sonarscanner..."
    dotnet tool install --global dotnet-sonarscanner
fi

# Set your SonarQube server URL and token
SONAR_HOST_URL="https://sonarcloud.io"  # Using SonarCloud
SONAR_TOKEN="${SONAR_TOKEN:-}"  # Use environment variable or pass as parameter

if [ -z "$SONAR_TOKEN" ]; then
    echo "Please set SONAR_TOKEN environment variable or edit this script"
    echo "For SonarCloud: Get token from https://sonarcloud.io/account/security/"
    echo "Usage: SONAR_TOKEN=your_token ./sonar-analysis.sh"
    exit 1
fi

# Navigate to project directory
cd "$(dirname "$0")"

# Clean previous builds
echo "Cleaning previous builds..."
dotnet clean

# Begin SonarQube analysis
echo "Beginning SonarQube analysis..."
dotnet-sonarscanner begin \
    /k:"pranavpandey86_AgenticMCP" \
    /o:"pranavpandey86" \
    /d:sonar.host.url="$SONAR_HOST_URL" \
    /d:sonar.token="$SONAR_TOKEN" \
    /d:sonar.cs.opencover.reportsPaths="coverage/*/coverage.opencover.xml" \
    /d:sonar.cs.vstest.reportsPaths="coverage/*.trx" \
    /d:sonar.exclusions="**/bin/**,**/obj/**,Tests/**,**/Tests/**" \
    /d:sonar.coverage.exclusions="Tests/**,**/Tests/**"

# Build the main project only
echo "Building main API project..."
dotnet build AgenticOrderingSystem.API.csproj --no-incremental

# Run tests with coverage (separate from main build)
echo "Running tests with coverage..."
dotnet test Tests/AgenticOrderingSystem.API.Tests.csproj \
    --collect:"XPlat Code Coverage" \
    --results-directory ./coverage \
    --logger trx \
    --no-build

# End SonarQube analysis
echo "Ending SonarQube analysis..."
dotnet-sonarscanner end /d:sonar.token="$SONAR_TOKEN"

echo "SonarQube analysis completed!"
echo "Check results at: $SONAR_HOST_URL/project/overview?id=pranavpandey86_AgenticMCP"
