using Microsoft.Data.SqlClient;
using System.Data;

namespace Demo.Api;

public sealed class SqlTestService
{
    private readonly string _cs;
    public SqlTestService(IConfiguration cfg)
        => _cs = cfg.GetConnectionString("Sql")
           ?? throw new InvalidOperationException("Missing ConnectionStrings:Sql");

    private async Task<string> EnsureDatabaseExistsAndGetCsAsync()
    {
        var builder = new SqlConnectionStringBuilder(_cs);
        var targetDb = string.IsNullOrWhiteSpace(builder.InitialCatalog) ? "DemoDb" : builder.InitialCatalog;

        var masterCs = new SqlConnectionStringBuilder(_cs) { InitialCatalog = "master" }.ConnectionString;
        await using (var master = new SqlConnection(masterCs))
        {
            await master.OpenAsync();

            using var create = master.CreateCommand();
            create.CommandText = "IF DB_ID(@db) IS NULL EXEC('CREATE DATABASE [' + @db + ']');";
            create.Parameters.Add(new SqlParameter("@db", SqlDbType.NVarChar, 128) { Value = targetDb });
            await create.ExecuteNonQueryAsync();
        }
        return new SqlConnectionStringBuilder(_cs) { InitialCatalog = targetDb }.ConnectionString;
    }

    public async Task EnsureSchemaAsync()
    {
        var csTarget = await EnsureDatabaseExistsAndGetCsAsync();

        await using var conn = new SqlConnection(csTarget);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'SqlDemo') EXEC('CREATE SCHEMA SqlDemo');

            IF OBJECT_ID('SqlDemo.Products') IS NULL
            BEGIN
                CREATE TABLE SqlDemo.Products(
                    Id INT IDENTITY PRIMARY KEY,
                    Sku NVARCHAR(64) NOT NULL UNIQUE,
                    Name NVARCHAR(128) NOT NULL
                );
                INSERT INTO SqlDemo.Products(Sku, Name) VALUES('DUP-001','Seed');
            END

            IF OBJECT_ID('SqlDemo.Parents') IS NULL
            BEGIN
                CREATE TABLE SqlDemo.Parents(Id INT IDENTITY PRIMARY KEY);
            END

            IF OBJECT_ID('SqlDemo.Children') IS NULL
            BEGIN
                CREATE TABLE SqlDemo.Children(
                    Id INT IDENTITY PRIMARY KEY,
                    ParentId INT NOT NULL,
                    CONSTRAINT FK_Children_Parents FOREIGN KEY (ParentId) REFERENCES SqlDemo.Parents(Id)
                );
            END

            IF OBJECT_ID('SqlDemo.LockA') IS NULL
            BEGIN
                CREATE TABLE SqlDemo.LockA(Id INT PRIMARY KEY, Payload INT NOT NULL);
                INSERT INTO SqlDemo.LockA(Id, Payload) VALUES(1,0);
            END

            IF OBJECT_ID('SqlDemo.LockB') IS NULL
            BEGIN
                CREATE TABLE SqlDemo.LockB(Id INT PRIMARY KEY, Payload INT NOT NULL);
                INSERT INTO SqlDemo.LockB(Id, Payload) VALUES(1,0);
            END
            ";
        await cmd.ExecuteNonQueryAsync();
    }

    // 2627 / 2601 – Unique violation
    public async Task TriggerDuplicateAsync()
    {
        await EnsureSchemaAsync();

        await using var conn = new SqlConnection(_cs);
        await conn.OpenAsync();

        var dup = conn.CreateCommand();
        dup.CommandText = "INSERT INTO SqlDemo.Products(Sku, Name) VALUES(@sku, @name)";
        dup.Parameters.Add(new SqlParameter("@sku", SqlDbType.NVarChar, 64) { Value = "DUP-001" });
        dup.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 128) { Value = "Duplicate Try" });

        await dup.ExecuteNonQueryAsync();
    }

    // 547 – FK violation
    public async Task TriggerConstraintAsync()
    {
        await EnsureSchemaAsync();

        await using var conn = new SqlConnection(_cs);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO SqlDemo.Children(ParentId) VALUES(@pid)";
        cmd.Parameters.Add(new SqlParameter("@pid", SqlDbType.Int) { Value = 999999 });
        await cmd.ExecuteNonQueryAsync();
    }

    // -2 – Timeout
    public async Task TriggerTimeoutAsync()
    {
        await using var conn = new SqlConnection(_cs);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 1;
        cmd.CommandText = "WAITFOR DELAY '00:00:05'; SELECT 1;";
        await cmd.ExecuteScalarAsync();
    }

    // 1205 – Deadlock
    public async Task TriggerDeadlockAsync()
    {
        await EnsureSchemaAsync();

        async Task TxA()
        {
            await using var c1 = new SqlConnection(_cs);
            await c1.OpenAsync();
            using var t1 = await c1.BeginTransactionAsync(IsolationLevel.ReadCommitted);

            var cmd1 = c1.CreateCommand();
            cmd1.Transaction = (SqlTransaction)t1;
            cmd1.CommandText = "UPDATE SqlDemo.LockA SET Payload = Payload + 1 WHERE Id = 1";
            await cmd1.ExecuteNonQueryAsync();

            await Task.Delay(500);

            var cmd2 = c1.CreateCommand();
            cmd2.Transaction = (SqlTransaction)t1;
            cmd2.CommandText = "UPDATE SqlDemo.LockB SET Payload = Payload + 1 WHERE Id = 1";
            await cmd2.ExecuteNonQueryAsync();

            await t1.CommitAsync();
        }

        async Task TxB()
        {
            await using var c2 = new SqlConnection(_cs);
            await c2.OpenAsync();
            using var t2 = await c2.BeginTransactionAsync(IsolationLevel.ReadCommitted);

            var cmd1 = c2.CreateCommand();
            cmd1.Transaction = (SqlTransaction)t2;
            cmd1.CommandText = "UPDATE SqlDemo.LockB SET Payload = Payload + 1 WHERE Id = 1";
            await cmd1.ExecuteNonQueryAsync();

            await Task.Delay(500);

            var cmd2 = c2.CreateCommand();
            cmd2.Transaction = (SqlTransaction)t2;
            cmd2.CommandText = "UPDATE SqlDemo.LockA SET Payload = Payload + 1 WHERE Id = 1";
            await cmd2.ExecuteNonQueryAsync();

            await t2.CommitAsync();
        }

        await Task.WhenAll(TxA(), TxB());
    }

    // 4060 – Cannot open database
    public async Task TriggerDbUnavailableAsync()
    {
        var builder = new SqlConnectionStringBuilder(_cs) { InitialCatalog = "Db_That_Does_Not_Exist_123" };
        await using var conn = new SqlConnection(builder.ConnectionString);
        await conn.OpenAsync();
    }
}