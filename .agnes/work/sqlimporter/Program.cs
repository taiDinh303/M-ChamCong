using System.Data;
using M.Repositories.Context;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

// ============================================================
//  import-to-somee: (1) tạo schema bằng EF migrations
//                     (2) copy data từ local (DESKTOP-DVT) -> Somee
//  IDEMPOTENT: chạy lại nhiều lần được (xóa table đích trước khi copy).
//  COLUMN-MAPPED: chỉ copy các cột có ở CẢ local lẫn Somee (fix lệch cột).
//  FK-TOLERANT: tắt FK khi import, bật lại (WITH CHECK) sau khi xong.
//
//  Cách chạy (từ project root):
//    dotnet run --project .agnes/work/sqlimporter -- "MẬT_KHẨU" 
//    dotnet run --project .agnes/work/sqlimporter -- --check-local
//    dotnet run --project .agnes/work/sqlimporter -- "MẬT_KHẨU" --diff Attendances
// ============================================================

// ---------- parse args ----------
string someePassword = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "PASTE_PASSWORD";
bool checkLocalOnly = args.Contains("--check-local");
int diffIdx = Array.IndexOf(args, "--diff");
string? diffTable = (diffIdx >= 0 && diffIdx + 1 < args.Length) ? args[diffIdx + 1] : null;

string someeCs = $"Data Source=monica_001.mssql.somee.com;Initial Catalog=monica_001;User ID=dinhvantai079_SQLLogin_1;Password={someePassword};Encrypt=False;TrustServerCertificate=True";
string localCs = "Data Source=DESKTOP-DVT;Initial Catalog=Monica_001;Integrated Security=True;Encrypt=False";

List<string> GetColumns(string cs, string table)
{
    var cols = new List<string>();
    using var conn = new SqlConnection(cs);
    conn.Open();
    using var cmd = new SqlCommand(
        "SELECT c.name FROM sys.columns c JOIN sys.tables t ON c.object_id=t.object_id WHERE t.name=@n ORDER BY c.column_id", conn);
    cmd.Parameters.AddWithValue("@n", table);
    using var rd = cmd.ExecuteReader();
    while (rd.Read()) cols.Add(rd.GetString(0));
    return cols;
}

// ---------- diff-only mode ----------
if (diffTable != null)
{
    var lc = GetColumns(localCs, diffTable);
    var sc_ = GetColumns(someeCs, diffTable);
    Console.WriteLine($"== Column diff for {diffTable} ==");
    Console.WriteLine($"LOCAL ({lc.Count}): {string.Join(", ", lc)}");
    Console.WriteLine($"SOMEE ({sc_.Count}): {string.Join(", ", sc_)}");
    var localOnly = lc.Where(c => !sc_.Contains(c)).ToList();
    var someeOnly = sc_.Where(c => !lc.Contains(c)).ToList();
    Console.WriteLine("LOCAL-only (will be skipped): " + string.Join(", ", localOnly));
    Console.WriteLine("SOMEE-only (will be null): " + string.Join(", ", someeOnly));
    Console.WriteLine("common=" + lc.Count(c => sc_.Contains(c)));
    return;
}

// ---------- check-local mode ----------
Console.WriteLine("[check-local] local row counts:");
using (var lc4 = new SqlConnection(localCs))
{
    lc4.Open();
    using var c = new SqlCommand(
        "SELECT t.name, p.rows FROM sys.tables t LEFT JOIN sys.partitions p ON t.object_id=p.object_id AND p.index_id IN (0,1) WHERE t.schema_id=SCHEMA_ID('dbo') ORDER BY p.rows DESC", lc4);
    using var rd = c.ExecuteReader();
    while (rd.Read())
        Console.WriteLine($"   {rd.GetString(0).PadRight(35)} {Convert.ToInt64(rd.GetValue(1))}");
}
if (checkLocalOnly) { Console.WriteLine("\nDONE check-local."); return; }

// ---------- 1. apply EF migrations (idempotent) ----------
Console.WriteLine("[1/4] Applying EF migrations to Somee...");
var opts = new DbContextOptionsBuilder<DatabaseContext>().UseSqlServer(someeCs).Options;
using (var ctx = new DatabaseContext(opts))
{
    await ctx.Database.MigrateAsync();
    Console.WriteLine("      Migrations applied. Pending: " + (await ctx.Database.GetPendingMigrationsAsync()).Count());
}

// ---------- 2. collect tables (present on BOTH sides) ----------
Console.WriteLine("[2/4] Collecting common tables...");
var localTables = new List<string>();
using (var lc2 = new SqlConnection(localCs))
{
    lc2.Open();
    using var c = new SqlCommand("SELECT name FROM sys.tables WHERE schema_id=SCHEMA_ID('dbo') AND name NOT IN ('__EFMigrationsHistory','sysdiagrams')", lc2);
    using var rd = c.ExecuteReader();
    while (rd.Read()) localTables.Add(rd.GetString(0));
}
var someeTables = new HashSet<string>();
using (var sc0 = new SqlConnection(someeCs))
{
    sc0.Open();
    using var c = new SqlCommand("SELECT name FROM sys.tables WHERE schema_id=SCHEMA_ID('dbo')", sc0);
    using var rd = c.ExecuteReader();
    while (rd.Read()) someeTables.Add(rd.GetString(0));
}
var tableList = localTables.Where(t => someeTables.Contains(t)).ToList();
foreach (var m in localTables.Where(t => !someeTables.Contains(t)))
    Console.WriteLine($"      [SKIP] {m}: not on Somee (local-only)");
Console.WriteLine($"      {tableList.Count} tables to copy.");

// ---------- 3. disable FKs on Somee ----------
var fks = new List<(string Table, string Name)>();
using (var sc = new SqlConnection(someeCs))
{
    sc.Open();
    using var c = new SqlCommand("SELECT t.name, fk.name FROM sys.foreign_keys fk JOIN sys.tables t ON t.object_id=fk.parent_object_id", sc);
    using var rd = c.ExecuteReader();
    while (rd.Read()) fks.Add((rd.GetString(0), rd.GetString(1)));
}
Console.WriteLine($"[3/4] Disabling {fks.Count} foreign keys...");
using (var sc = new SqlConnection(someeCs))
{
    sc.Open();
    foreach (var f in fks)
        new SqlCommand($"ALTER TABLE dbo.[{f.Table}] NOCHECK CONSTRAINT [{f.Name}]", sc).ExecuteNonQuery();
}

// ---------- 4. copy data (idempotent, column-mapped) ----------
Console.WriteLine("[4/4] Bulk-copying data local -> Somee...");
int copied = 0, empty = 0, failed = 0;
var errors = new List<string>();

foreach (var table in tableList)
{
    try
    {
        var localCols = GetColumns(localCs, table);
        var someeCols = GetColumns(someeCs, table);
        var common = someeCols.Where(c => localCols.Contains(c)).ToList();
        if (common.Count == 0)
        {
            Console.WriteLine($"      {table.PadRight(32)} [no common columns - skipped]");
            continue;
        }

        using var sc = new SqlConnection(someeCs);
        sc.Open();

        // idempotent: clear destination
        new SqlCommand($"DELETE FROM dbo.[{table}]", sc).ExecuteNonQuery();

        // identity insert (best effort)
        bool idOn = false;
        try { new SqlCommand($"SET IDENTITY_INSERT dbo.[{table}] ON", sc).ExecuteNonQuery(); idOn = true; } catch { }

        var selectCols = string.Join(",", common.Select(c => $"[{c}]"));
        int rowCount;
        using (var lc3 = new SqlConnection(localCs))
        {
            lc3.Open();
            using var src = new SqlCommand($"SELECT {selectCols} FROM dbo.[{table}]", lc3);
            using var rd = src.ExecuteReader();
            using var bc = new SqlBulkCopy(sc) { DestinationTableName = table, BatchSize = 2000 };
            bc.WriteToServer(rd);   // name-based mapping (only common cols present)
            rowCount = bc.RowsCopied;
        }

        if (idOn) new SqlCommand($"SET IDENTITY_INSERT dbo.[{table}] OFF", sc).ExecuteNonQuery();

        if (rowCount == 0) empty++; else copied++;
        Console.WriteLine($"      {table.PadRight(32)} {rowCount,6} rows  ({common.Count} cols)");
    }
    catch (Exception ex)
    {
        failed++;
        errors.Add($"{table}: {ex.Message}");
        Console.WriteLine($"      {table.PadRight(32)} FAILED: {ex.Message.Split('\n')[0]}");
    }
}

// ---------- 5. re-enable FKs ----------
Console.WriteLine("Re-enabling foreign keys...");
int fkFail = 0;
using (var sc = new SqlConnection(someeCs))
{
    sc.Open();
    foreach (var f in fks)
    {
        try
        {
            new SqlCommand($"ALTER TABLE dbo.[{f.Table}] WITH CHECK CHECK CONSTRAINT [{f.Name}]", sc).ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            fkFail++;
            Console.WriteLine($"  [FK WARN] {f.Table}.{f.Name}: {ex.Message.Split('\n')[0]}");
        }
    }
}

// ---------- verify ----------
Console.WriteLine("\nRow counts on Somee:");
using (var vc = new SqlConnection(someeCs))
{
    vc.Open();
    foreach (var table in tableList)
    {
        int n;
        using var c = new SqlCommand($"SELECT COUNT(*) FROM dbo.[{table}]", vc);
        try { n = Convert.ToInt32(c.ExecuteScalar()); } catch { n = -1; }
        Console.WriteLine($"   {table.PadRight(32)} {n}");
    }
}

Console.WriteLine($"\nDONE. copied={copied} empty={empty} failed={failed} fkIssues={fkFail}");
foreach (var e in errors) Console.WriteLine("  ERR: " + e);
