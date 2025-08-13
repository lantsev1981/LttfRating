param([Boolean]$drop = 0)

if ($drop) {
	dotnet ef database drop --force --context AppDbContext
}

dotnet ef database update --context AppDbContext