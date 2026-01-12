# Script para testar o endpoint /api/v1/financialaccounts
# Execute com a API rodando em http://localhost:5000

Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host "Testando endpoint GET /api/v1/financialaccounts" -ForegroundColor Cyan
Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host ""

try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/v1/financialaccounts" -Method GET -UseBasicParsing

    Write-Host "✅ SUCESSO!" -ForegroundColor Green
    Write-Host "Status Code: $($response.StatusCode)" -ForegroundColor Green
    Write-Host ""
    Write-Host "Response Body:" -ForegroundColor Yellow
    Write-Host $response.Content
    Write-Host ""

} catch {
    Write-Host "❌ ERRO!" -ForegroundColor Red
    Write-Host "Erro: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""

    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "Status Code: $statusCode" -ForegroundColor Red

        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            $reader.Close()

            Write-Host "Response Body:" -ForegroundColor Yellow
            Write-Host $responseBody
        } catch {
            Write-Host "Não foi possível ler o corpo da resposta" -ForegroundColor Yellow
        }
    }
}

Write-Host ""
Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host "Verifique os logs em: api-core\src\Diax.Api\logs\" -ForegroundColor Cyan
Write-Host "=====================================================================" -ForegroundColor Cyan
