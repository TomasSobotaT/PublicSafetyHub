[CmdletBinding()]
param(
    [string] $OpenApiUrl = 'https://localhost:7102/openapi/v1.json'
)

$ErrorActionPreference = 'Stop'
$projectPath = Join-Path $PSScriptRoot 'PublicSafetyHub.NSwagClient.csproj'

dotnet msbuild $projectPath -restore -t:GenerateApiClient "-p:OpenApiUrl=$OpenApiUrl" -nologo

if ($LASTEXITCODE -ne 0) {
    throw 'Client generation failed. Check that the API is running and its OpenAPI URL is accessible.'
}
