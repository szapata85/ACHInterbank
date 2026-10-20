param(
    [string]$BaseUrl = "https://localhost:7269"
)

$ErrorActionPreference = "Stop"
$base = $BaseUrl.TrimEnd('/')
$openApiUrl = "$base/openapi/v1.json"
$scalarUrl = "$base/scalar/"

# Local development certificates may be self-signed. This handler is limited to this
# explicit smoke check and does not change the application TLS policy.
$handler = [System.Net.Http.HttpClientHandler]::new()
$handler.ServerCertificateCustomValidationCallback = { param($sender, $cert, $chain, $errors) $true }
$client = [System.Net.Http.HttpClient]::new($handler)
try {
    Write-Host "GET $openApiUrl"
    $openApi = $client.GetAsync($openApiUrl).GetAwaiter().GetResult()
    $body = $openApi.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    Write-Host "OpenAPI HTTP $([int]$openApi.StatusCode) $($openApi.StatusCode)"
    if (-not $openApi.IsSuccessStatusCode) {
        Write-Host $body
        $diagnosticUrl = "$base/openapi/_diagnostics"
        Write-Host "GET $diagnosticUrl"
        try {
            $diagnostic = $client.GetAsync($diagnosticUrl).GetAwaiter().GetResult()
            $diagnosticBody = $diagnostic.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            Write-Host "OpenAPI diagnostics HTTP $([int]$diagnostic.StatusCode) $($diagnostic.StatusCode)"
            Write-Host $diagnosticBody
        } catch {
            Write-Host "No fue posible consultar /openapi/_diagnostics: $($_.Exception.Message)"
        }
        # On Development, identify any response type whose JSON schema fails independently.
        $schemaDiagnosticUrl = "$base/openapi/_schema-diagnostics"
        Write-Host "GET $schemaDiagnosticUrl"
        try {
            $schemaResult = $client.GetAsync($schemaDiagnosticUrl).GetAwaiter().GetResult()
            $schemaBody = $schemaResult.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            Write-Host "Schema diagnostics HTTP $([int]$schemaResult.StatusCode) $($schemaResult.StatusCode)"
            Write-Host $schemaBody
        } catch {
            Write-Host "No fue posible consultar /openapi/_schema-diagnostics: $($_.Exception.Message)"
        }
        throw "OpenAPI no respondió 2xx. Revise también src/Cfa.ACHInterbank.Api/crash.log."
    }

    try {
        $json = $body | ConvertFrom-Json
    } catch {
        Write-Host $body
        throw "OpenAPI respondió 2xx pero el body no es JSON válido."
    }

    if (-not $json.openapi -or -not $json.paths) {
        throw "El documento no contiene las propiedades OpenAPI esperadas (openapi/paths)."
    }

    Write-Host "OpenAPI OK: version=$($json.openapi); paths=$($json.paths.PSObject.Properties.Count)"

    Write-Host "GET $scalarUrl"
    $scalar = $client.GetAsync($scalarUrl).GetAwaiter().GetResult()
    Write-Host "Scalar HTTP $([int]$scalar.StatusCode) $($scalar.StatusCode)"
    if (-not $scalar.IsSuccessStatusCode) {
        throw "Scalar no respondió 2xx."
    }

    Write-Host "Scalar/OpenAPI smoke check: OK"
}
finally {
    $client.Dispose()
    $handler.Dispose()
}
