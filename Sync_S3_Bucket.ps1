#Requires -Version 5.1

param(
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

$McExe  = "C:\Program Files\MinioClient\mc.exe"
$Alias  = "surimi"  #"myminio"

# ============================================================
# Configuration
# ============================================================

$Regions = @(
    "northwestern_med",
    "aegean_sea",
    "anchovy_bay"
)

$Mappings = @(
    @{
        Application = "cmsy"
        SourceRoot  = "C:\Users\Rik\source\repos\SURIMI-CMSY\R_files"
    },
    @{
        Application = "controller"
        SourceRoot  = "C:\Users\Rik\source\repos\SURIMI-controller\SURIMI-controller\Includes"
    },
    @{
        Application = "ecopath"
        SourceRoot  = "C:\Users\Rik\source\repos\SURIMI-Ecopath\SURIMI-Ecopath\Includes"
    },
    @{
        Application = "fisheries_authority"
        SourceRoot  = "C:\Users\Rik\source\repos\SURIMI-fisheries-authority\SURIMI-fisheries-authority\Includes"
    },
    @{
        Application = "market"
        SourceRoot  = "C:\Users\Rik\source\repos\SURIMI-market\config"
    }
)

# ============================================================
# Verify mc
# ============================================================

try {
    & $McExe --version | Out-Null
}
catch {
    throw "MinIO Client not found: $McExe"
}

# ============================================================
# Verify alias
# ============================================================

try {
    & $McExe ls $Alias | Out-Null
}
catch {
    throw "MinIO alias '$Alias' not found or not working."
}

# ============================================================
# Bucket
# ============================================================

$DefaultBucket = "surimi-bucket" #"oidc-rikkert"

$Bucket = Read-Host "Bucket name [$DefaultBucket]"

if ([string]::IsNullOrWhiteSpace($Bucket)) {
    $Bucket = $DefaultBucket
}

Write-Host ""
Write-Host "Checking bucket '$Bucket'..." -ForegroundColor Cyan

try {
    & $McExe ls "$Alias/$Bucket" | Out-Null
    Write-Host "Bucket exists." -ForegroundColor Green
}
catch {
    Write-Host "Bucket does not exist. Creating..." -ForegroundColor Yellow

    if (-not $DryRun) {
        & $McExe mb "$Alias/$Bucket"
    }
}

# ============================================================
# Statistics
# ============================================================

$SuccessCount = 0
$SkippedCount = 0
$FailedCount  = 0

# ============================================================
# Sync Function
# ============================================================

function Sync-Folder {

    param(
        [string]$LocalPath,
        [string]$TargetPath
    )

    if (-not (Test-Path $LocalPath)) {

        Write-Host "SKIPPED : $LocalPath" -ForegroundColor Yellow
        $script:SkippedCount++
        return
    }

    Write-Host ""
    Write-Host "SYNC" -ForegroundColor Cyan
    Write-Host "FROM : $LocalPath"
    Write-Host "TO   : $TargetPath"

    if ($DryRun) {

        Write-Host "DRY RUN" -ForegroundColor Yellow
        $script:SuccessCount++
        return
    }

    try {

        & $McExe mirror `
            --overwrite `
            "$LocalPath" `
            "$TargetPath"

        if ($LASTEXITCODE -ne 0) {
            throw "mc mirror failed"
        }

        Write-Host "SUCCESS" -ForegroundColor Green
        $script:SuccessCount++
    }
    catch {

        Write-Host "FAILED : $($_.Exception.Message)" -ForegroundColor Red
        $script:FailedCount++
    }
}

# ============================================================
# Uploads
# ============================================================

foreach ($mapping in $Mappings) {

    foreach ($region in $Regions) {

        $localFolder = Join-Path $mapping.SourceRoot $region

        $targetFolder = "$Alias/$Bucket/$($mapping.Application)/$region"

        Sync-Folder `
            -LocalPath $localFolder `
            -TargetPath $targetFolder
    }
}

# ============================================================
# Summary
# ============================================================

Write-Host ""
Write-Host "===================================="
Write-Host "Synchronization Complete"
Write-Host "===================================="
Write-Host ""

Write-Host "Successful : $SuccessCount"
Write-Host "Skipped    : $SkippedCount"
Write-Host "Failed     : $FailedCount"

if ($FailedCount -gt 0) {
    exit 1
}