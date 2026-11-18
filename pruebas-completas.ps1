<#
.SYNOPSIS
    Corre todas las pruebas en orden (tests y linters, mutación y E2E) y deja un resumen.
    Termina con código distinto de 0 si algo falla.

.DESCRIPTION
    1  Tests y linters       : pruebas SQL, compilación con analizadores (advertencias = error), pruebas del backend,
                                linter del frontend y pruebas del frontend.
    2  Mutation testing      : Stryker.NET (backend) y StrykerJS (frontend). Falla bajo el umbral de cada config.
    3  Pruebas E2E           : Playwright contra la API y el frontend reales (levanta y apaga los servidores).


.EXAMPLE
    .\pruebas-completas.ps1                 # las tres capas
    .\pruebas-completas.ps1 -Capas 1        # solo tests y linters
    .\pruebas-completas.ps1 -Continuar      # no se detiene en la primera falla; resume todas
#>
param(
    [int[]]$Capas = @(1, 2, 3),
    [switch]$Continuar
)

$ErrorActionPreference = 'Continue'

# ---------------- Proyecto ----------------
$Nombre      = 'RendiGastos'
$Solucion    = Join-Path $PSScriptRoot 'backend'
$PruebasApi  = Join-Path $PSScriptRoot 'backend\tests\RendiGastos.Api.Tests'
$Frontend    = Join-Path $PSScriptRoot 'frontend'
$BaseDatos   = Join-Path $PSScriptRoot 'backend\database'
# ---------------------------------------------------------------------------

$resultados = New-Object System.Collections.Generic.List[object]
$huboFalla = $false

function Ejecutar([int]$capa, [string]$nombre, [string]$carpeta, [scriptblock]$accion, [string]$debeContener = '') {
    if ($Capas -notcontains $capa) { return }
    if ($huboFalla -and -not $Continuar) { $resultados.Add([pscustomobject]@{ Capa = $capa; Paso = $nombre; Estado = 'OMITIDO'; Segundos = 0 }); return }

    Write-Host "`n=== Capa $capa · $nombre ===" -ForegroundColor Cyan
    $reloj = [Diagnostics.Stopwatch]::StartNew()
    Push-Location $carpeta
    try {
        $global:LASTEXITCODE = 0
        $salida = & $accion 2>&1 | ForEach-Object { $_.ToString() } | Tee-Object -Variable lineas
        $texto = ($lineas -join "`n")
        $ok = ($global:LASTEXITCODE -eq 0)
        if ($ok -and $debeContener -ne '') { $ok = $texto -match $debeContener }
        $lineas | Select-Object -Last 12 | ForEach-Object { Write-Host "  $_" }
    } catch {
        $ok = $false
        Write-Host "  $_" -ForegroundColor Red
    } finally {
        Pop-Location
    }
    $reloj.Stop()
    if (-not $ok) { $script:huboFalla = $true }
    $resultados.Add([pscustomobject]@{ Capa = $capa; Paso = $nombre; Estado = $(if ($ok) { 'OK' } else { 'FALLÓ' }); Segundos = [math]::Round($reloj.Elapsed.TotalSeconds) })
}

# ---------------- 1. tests y linters ----------------
Ejecutar 1 'Procedimientos SQL'                 $BaseDatos  { powershell -NoProfile -ExecutionPolicy Bypass -File .\instalar.ps1 -ConPruebas } 'PASARON'
Ejecutar 1 'Compilación con analizadores'       $Solucion   { dotnet build -nologo -v q }
Ejecutar 1 'Pruebas del backend'                $Solucion   { dotnet test -nologo -v q --no-build }
Ejecutar 1 'Linter del frontend (ESLint)'       $Frontend   { npx ng lint }
Ejecutar 1 'Pruebas del frontend'               $Frontend   { npx ng test --watch=false --browsers=ChromeHeadless }

# ---------------- 2. mutation testing ----------------
Ejecutar 2 'Mutación del backend (Stryker.NET)' $Solucion   { dotnet tool restore | Out-Null; Set-Location $PruebasApi; dotnet dotnet-stryker --config-file stryker-config.json }
Ejecutar 2 'Mutación del frontend (StrykerJS)'  $Frontend   { npx stryker run }

# ---------------- 3. E2E ----------------
Ejecutar 3 'Pruebas E2E (Playwright)'           $Frontend   { npx playwright test }

# ---------------- Resumen ----------------
Write-Host "`n================ Resumen $Nombre ================" -ForegroundColor Cyan
$resultados | Format-Table -AutoSize | Out-String | Write-Host
if ($huboFalla) {
    Write-Host 'HAY PASOS QUE FALLARON: corregir y volver a correr hasta que todo quede en verde.' -ForegroundColor Red
    exit 1
}
Write-Host 'TODAS LAS CAPAS EN VERDE.' -ForegroundColor Green
exit 0
