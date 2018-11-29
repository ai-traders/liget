# LiGet

## Migrations

Regenerate migrations with:

```
rm liget.db
dotnet ef migrations remove
dotnet ef migrations add Initial --context SqliteContext --output-dir Migrations/Sqlite
dotnet ef migrations add Initial --context SqlServerContext --output-dir Migrations/SqlServer

dotnet ef database update
```
