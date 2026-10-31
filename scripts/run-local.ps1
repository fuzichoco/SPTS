param([int]$Port = 5223)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$projectDirectory = Join-Path $repoRoot 'SPTS'
$runtime = Join-Path $repoRoot '.local/dotnet/dotnet.exe'
if (-not (Test-Path -LiteralPath $runtime)) {
    throw 'Runtime cục bộ chưa có tại .local/dotnet. Cần chuẩn bị ASP.NET Core Runtime 8 trước.'
}
Push-Location $repoRoot
try {
    dotnet build SPTS.sln --no-restore --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Build thất bại. Kiểm tra lỗi ở trên; nếu thiếu gói, chạy dotnet restore SPTS.sln.' }
    sqllocaldb start SPTSDev
    if ($LASTEXITCODE -ne 0) { throw 'Không khởi động được LocalDB SPTSDev.' }
    $previousConnection = $env:ConnectionStrings__DefaultConnection
    $previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
    $previousUrls = $env:ASPNETCORE_URLS
    try {
        $env:ConnectionStrings__DefaultConnection = 'Server=(localdb)\SPTSDev;Database=SPTS_CatalogVerify;Trusted_Connection=True;TrustServerCertificate=True'
        $env:ASPNETCORE_ENVIRONMENT = 'Development'
        $env:ASPNETCORE_URLS = "http://localhost:$Port"
        Set-Location $projectDirectory
        Write-Host "Mở http://localhost:$Port/Account/Login — tài khoản mẫu admin / Admin@123"
        & $runtime bin/Debug/net8.0/SPTS.dll
    }
    finally {
        $env:ConnectionStrings__DefaultConnection = $previousConnection
        $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
        $env:ASPNETCORE_URLS = $previousUrls
    }
}
finally { Pop-Location }
