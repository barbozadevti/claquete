using System.Text.RegularExpressions;
using Claquete.Api.Dados;

namespace Claquete.Api.Endpoints;

/// <summary>
/// Laboratório SQL: expõe as 12 consultas de database/sqlserver/02-consultas.sql,
/// lidas do próprio arquivo, para que o site e o script nunca fiquem diferentes.
/// </summary>
public static partial class LaboratorioEndpoints
{
    private static readonly Lazy<IReadOnlyList<Consulta>> Catalogo =
        new(() => LerCatalogo(File.ReadAllLines(Scripts.Caminho("sqlserver", "02-consultas.sql"))));

    public static void MapearLaboratorio(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/consultas").WithTags("Laboratório SQL");

        grupo.MapGet("/", () => Results.Ok(Catalogo.Value));
        grupo.MapGet("/{numero:int}", async (Banco banco, int numero) =>
        {
            var consulta = Catalogo.Value.FirstOrDefault(c => c.Numero == numero);
            if (consulta is null)
                return Results.NotFound();

            await using var conexao = await banco.AbrirAsync();
            var (colunas, linhas) = await LeitorTabela.LerAsync(conexao, consulta.Sql);
            return Results.Ok(new ResultadoConsulta(consulta.Numero, consulta.Titulo, consulta.Sql, colunas, linhas));
        });
    }

    public static IReadOnlyList<Consulta> LerCatalogo(IEnumerable<string> linhas)
    {
        var consultas = new List<Consulta>();
        int? numero = null;
        var titulo = "";
        var sql = new List<string>();

        void Fechar()
        {
            if (numero is not null && sql.Count > 0)
                consultas.Add(new Consulta(numero.Value, titulo, string.Join('\n', sql).Trim()));
            sql.Clear();
        }

        foreach (var linha in linhas)
        {
            var cabecalho = Cabecalho().Match(linha);
            if (cabecalho.Success)
            {
                Fechar();
                numero = int.Parse(cabecalho.Groups[1].Value);
                titulo = cabecalho.Groups[2].Value.Trim();
            }
            else if (numero is not null && !string.IsNullOrWhiteSpace(linha))
            {
                sql.Add(linha.TrimEnd());
            }
        }
        Fechar();
        return consultas;
    }

    [GeneratedRegex(@"^--\s*(\d+)\s*-\s*(.+)$")]
    private static partial Regex Cabecalho();
}
