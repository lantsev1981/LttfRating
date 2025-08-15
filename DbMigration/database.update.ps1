param(
    [Boolean]$drop = $false,
    [string]$environment = "Development"  # Добавляем параметр для окружения
)

# Устанавливаем переменную окружения
$env:ASPNETCORE_ENVIRONMENT = $environment

Write-Host "Applying migrations for environment: $environment"

if ($drop) {
    Write-Host "Dropping database..."
    dotnet ef database drop --force --context AppDbContext
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Database drop failed"
        exit 1
    }
}

Write-Host "Applying migrations..."
dotnet ef database update --context AppDbContext
if ($LASTEXITCODE -ne 0) {
    Write-Error "Database update failed"
    exit 1
}

Write-Host "Migrations completed successfully"