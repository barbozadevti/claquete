using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace Claquete.Api.Dados;

public enum Provedor { Sqlite, SqlServer }

public sealed class OpcoesBanco
{
    public Provedor Provedor { get; set; } = Provedor.Sqlite;
    public string ConnectionString { get; set; } = "Data Source=claquete.db";
}

/// <summary>
/// Abre conexões e esconde as poucas diferenças de SQL entre SQLite e SQL Server.
/// </summary>
public sealed class Banco
{
    private readonly string _connectionString;

    public Banco(OpcoesBanco opcoes, string pastaDados)
    {
        Provedor = opcoes.Provedor;
        _connectionString = Provedor == Provedor.Sqlite
            ? PrepararSqlite(opcoes.ConnectionString, pastaDados)
            : opcoes.ConnectionString;
    }

    public Provedor Provedor { get; }

    public async Task<DbConnection> AbrirAsync()
    {
        if (Provedor == Provedor.SqlServer)
        {
            var sql = new SqlConnection(_connectionString);
            await sql.OpenAsync();
            return sql;
        }

        var sqlite = new SqliteConnection(_connectionString);
        await sqlite.OpenAsync();
        sqlite.CreateFunction("normalizar", (string? texto) => Texto.Normalizar(texto), isDeterministic: true);
        return sqlite;
    }

    /// <summary>Comando que devolve o Id gerado pelo último INSERT.</summary>
    public string UltimoId => Provedor == Provedor.Sqlite
        ? "SELECT last_insert_rowid();"
        : "SELECT CAST(SCOPE_IDENTITY() AS int);";

    /// <summary>Filtro LIKE que ignora maiúsculas e acentos ("genio" encontra "Gênio").</summary>
    public string Contem(string coluna, string parametro) => Provedor == Provedor.Sqlite
        ? $"normalizar({coluna}) LIKE '%' || normalizar({parametro}) || '%'"
        : $"{coluna} COLLATE Latin1_General_CI_AI LIKE '%' + {parametro} + '%'";

    private static string PrepararSqlite(string connectionString, string pastaDados)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString) { ForeignKeys = true };
        var arquivo = builder.DataSource;
        if (arquivo != ":memory:" && !Path.IsPathRooted(arquivo))
        {
            Directory.CreateDirectory(pastaDados);
            builder.DataSource = Path.Combine(pastaDados, arquivo);
        }
        return builder.ToString();
    }
}

/// <summary>
/// Cria e popula o banco SQLite na primeira execução. No SQL Server o banco é
/// criado pelos scripts de database/sqlserver.
/// </summary>
public static class InicializadorBanco
{
    public static async Task GarantirAsync(Banco banco)
    {
        if (banco.Provedor != Provedor.Sqlite)
            return;

        await using var conexao = await banco.AbrirAsync();
        await using var existe = conexao.CreateCommand();
        existe.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'Filmes'";
        if (Convert.ToInt32(await existe.ExecuteScalarAsync()) > 0)
            return;

        await using var transacao = await conexao.BeginTransactionAsync();
        foreach (var script in new[] { "01-esquema.sql", "02-carga.sql" })
        {
            await using var comando = conexao.CreateCommand();
            comando.Transaction = transacao;
            comando.CommandText = await File.ReadAllTextAsync(Scripts.Caminho("sqlite", script));
            await comando.ExecuteNonQueryAsync();
        }
        await transacao.CommitAsync();
    }
}

public static class Scripts
{
    public static string Caminho(params string[] partes) =>
        Path.Combine([AppContext.BaseDirectory, "database", .. partes]);
}

public static class Texto
{
    public static string? Normalizar(string? texto)
    {
        if (texto is null)
            return null;

        var decomposto = texto.Normalize(NormalizationForm.FormD);
        var resultado = new StringBuilder(decomposto.Length);
        foreach (var c in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                resultado.Append(char.ToLowerInvariant(c));
        }
        return resultado.ToString();
    }
}

public static class LeitorTabela
{
    /// <summary>Executa um SQL qualquer e devolve colunas + linhas, para o Laboratório SQL.</summary>
    public static async Task<(string[] Colunas, List<object?[]> Linhas)> LerAsync(DbConnection conexao, string sql)
    {
        await using var comando = conexao.CreateCommand();
        comando.CommandText = sql;
        await using var leitor = await comando.ExecuteReaderAsync(CommandBehavior.SingleResult);

        var colunas = Enumerable.Range(0, leitor.FieldCount).Select(leitor.GetName).ToArray();
        var linhas = new List<object?[]>();
        while (await leitor.ReadAsync())
        {
            var valores = new object?[leitor.FieldCount];
            for (var i = 0; i < leitor.FieldCount; i++)
                valores[i] = leitor.IsDBNull(i) ? null : leitor.GetValue(i);
            linhas.Add(valores);
        }
        return (colunas, linhas);
    }
}
