using System.Globalization;
using Claquete.Api.Dados;
using Dapper;

namespace Claquete.Api.Endpoints;

public static class PainelEndpoints
{
    public static void MapearPainel(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/generos", ListarGeneros).WithTags("Gêneros");
        app.MapGet("/api/estatisticas", ObterEstatisticas).WithTags("Painel");
        app.MapGet("/api/saude", (Banco banco) => Results.Ok(new { status = "ok", provedor = banco.Provedor.ToString() }))
            .WithTags("Painel");
    }

    private static async Task<IResult> ListarGeneros(Banco banco)
    {
        await using var conexao = await banco.AbrirAsync();
        var generos = await conexao.QueryAsync<GeneroComContagem>("""
            SELECT G.Id, G.Genero, COUNT(FG.Id) AS Filmes
            FROM Generos G
            LEFT JOIN FilmesGenero FG ON FG.IdGenero = G.Id
            GROUP BY G.Id, G.Genero
            """);

        // Ordena em português no C#: o SQLite compara bytes e colocaria "Ação" depois de "Aventura".
        var portugues = StringComparer.Create(new CultureInfo("pt-BR"), ignoreCase: true);
        return Results.Ok(generos.OrderBy(g => g.Genero, portugues));
    }

    private static async Task<IResult> ObterEstatisticas(Banco banco)
    {
        await using var conexao = await banco.AbrirAsync();

        var totais = await conexao.QuerySingleAsync<Totais>("""
            SELECT
                (SELECT COUNT(*) FROM Filmes) AS Filmes,
                (SELECT COUNT(*) FROM Atores) AS Atores,
                (SELECT COUNT(*) FROM Generos) AS Generos,
                (SELECT AVG(CAST(Duracao AS FLOAT)) FROM Filmes) AS DuracaoMedia,
                (SELECT COUNT(*) FROM Atores WHERE Genero = 'M') AS Masculinos,
                (SELECT COUNT(*) FROM Atores WHERE Genero = 'F') AS Femininos,
                (SELECT COUNT(*) FROM Filmes F WHERE NOT EXISTS (SELECT 1 FROM FilmesGenero FG WHERE FG.IdFilme = F.Id)) AS SemGenero,
                (SELECT COUNT(*) FROM Filmes F WHERE NOT EXISTS (SELECT 1 FROM ElencoFilme EF WHERE EF.IdFilme = F.Id)) AS SemElenco
            """);

        // Subconsulta com MAX/MIN em vez de TOP/LIMIT, para o mesmo SQL rodar no SQLite e no SQL Server.
        var maisLongo = await conexao.QueryFirstOrDefaultAsync<FilmeResumo>(
            "SELECT Id, Nome, Ano, Duracao FROM Filmes WHERE Duracao = (SELECT MAX(Duracao) FROM Filmes) ORDER BY Nome");
        var maisCurto = await conexao.QueryFirstOrDefaultAsync<FilmeResumo>(
            "SELECT Id, Nome, Ano, Duracao FROM Filmes WHERE Duracao = (SELECT MIN(Duracao) FROM Filmes) ORDER BY Nome");

        var porAno = await conexao.QueryAsync<ContagemPorAno>("""
            SELECT Ano, COUNT(*) AS Quantidade
            FROM Filmes
            GROUP BY Ano
            ORDER BY Ano
            """);

        var porDecada = await conexao.QueryAsync<ContagemPorDecada>("""
            SELECT (Ano / 10) * 10 AS Decada, COUNT(*) AS Quantidade, AVG(CAST(Duracao AS FLOAT)) AS DuracaoMedia
            FROM Filmes
            GROUP BY (Ano / 10) * 10
            ORDER BY Decada
            """);

        var porGenero = await conexao.QueryAsync<ContagemPorGenero>("""
            SELECT G.Genero, COUNT(FG.Id) AS Quantidade
            FROM Generos G
            INNER JOIN FilmesGenero FG ON FG.IdGenero = G.Id
            GROUP BY G.Genero
            ORDER BY Quantidade DESC, G.Genero
            """);

        return Results.Ok(new Estatisticas(
            totais.Filmes, totais.Atores, totais.Generos,
            Math.Round(totais.DuracaoMedia ?? 0, 1),
            maisLongo, maisCurto,
            porAno.ToList(), porDecada.ToList(), porGenero.ToList(),
            totais.Masculinos, totais.Femininos, totais.SemGenero, totais.SemElenco));
    }

    private sealed class Totais
    {
        public int Filmes { get; set; }
        public int Atores { get; set; }
        public int Generos { get; set; }
        public double? DuracaoMedia { get; set; }
        public int Masculinos { get; set; }
        public int Femininos { get; set; }
        public int SemGenero { get; set; }
        public int SemElenco { get; set; }
    }
}
