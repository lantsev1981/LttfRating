param([string]$name='')

dotnet ef migrations add $name --context 'AppDbContext'