namespace Common;

public static class PgsqlHelper
{
    public static string CreateUser(string user, string password)
    {
        return @$"DO $$ BEGIN CREATE ROLE {user} NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT LOGIN PASSWORD '{password}'; EXCEPTION WHEN DUPLICATE_OBJECT THEN RAISE NOTICE 'not creating role because it already exists'; END $$; ";
    }

    public static string DropUser(string user)
    {
        return @$"DROP USER '{user}';";
    }

    public static string GrantPrivileges(string user, PgsqlGrant grant, params string[] tables)
    {
        return @$"GRANT {grant} ON {TablesString(tables)} TO {user};";
    }

    public static string TablesString(params string[] tables)
    {
        return string.Join(",", tables.Select(p => $"\"{p}\""));
    }
}