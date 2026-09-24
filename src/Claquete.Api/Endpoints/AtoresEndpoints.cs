using System.Data.Common;
using Claquete.Api.Dados;
using Dapper;

namespace Claquete.Api.Endpoints;

public static class AtoresEndpoints
{
    public static void MapearAtores(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/atores").WithTags("Atores");

        grupo.MapGet("/", Listar);
        grupo.MapGet("/{id:int}", Obter);
        grupo.MapPost("/", Criar);
        grupo.MapPut("/{id:int}", Atualizar);
        grupo.MapDelete("/{id:int}", Excluir);
    }

    private static async Task<IResult> Listar(Banco banco, string? busca, string? genero)
    {
        var filtros = new List<string>();
        if (!string.IsNullOrWhiteSpace(busca))
            filtros.Add($"({banco.Contem("A.PrimeiroNome", "@busca")} OR {banco.Contem("A.UltimoNome", "@busca")})");
        if (!string.IsNullOrWhiteSpace(genero))
            filtros.Add("A.Genero = @genero");
        var where = filtros.Count > 0 ? "WHERE " + string.Join(" AND ", filtros) : "";

        await using var conexao = await banco.AbrirAsync();
        var atores = await conexao.QueryAsync<AtorResumo>($"""
            SELECT A.Id, A.PrimeiroNome, A.UltimoNome, A.Genero,
                   (SELECT COUNT(*) FROM ElencoFilme EF WHERE EF.IdAtor = A.Id) AS Filmes
            FROM Atores A
            {where}
            ORDER BY A.PrimeiroNome, A.UltimoNome
            """, new { busca = busca?.Trim(), genero = genero?.Trim().ToUpperInvariant() });

        return Results.Ok(atores);
    }

    private static async Task<IResult> Obter(Banco banco, int id)
    {
        await using var conexao = await banco.AbrirAsync();
        var ator = await BuscarDetalheAsync(conexao, id);
        return ator is null ? Results.NotFound() : Results.Ok(ator);
    }

    private static async Task<AtorDetalhe?> BuscarDetalheAsync(DbConnection conexao, int id)
    {
        var ator = await conexao.QuerySingleOrDefaultAsync<AtorDetalhe>(
            "SELECT Id, PrimeiroNome, UltimoNome, Genero FROM Atores WHERE Id = @id", new { id });
        if (ator is null)
            return null;

        ator.Filmografia = (await conexao.QueryAsync<Participacao>("""
            SELECT EF.Id AS IdElenco, F.Id AS IdFilme, F.Nome, F.Ano, EF.Papel
            FROM ElencoFilme EF
            INNER JOIN Filmes F ON F.Id = EF.IdFilme
            WHERE EF.IdAtor = @id
            ORDER BY F.Ano, F.Nome
            """, new { id })).ToList();

        return ator;
    }

    private static async Task<IResult> Criar(Banco banco, AtorEntrada entrada)
    {
        var erros = entrada.Validar();
        if (erros.Count > 0)
            return Results.ValidationProblem(erros);

        await using var conexao = await banco.AbrirAsync();
        var id = await conexao.ExecuteScalarAsync<int>(
            $"INSERT INTO Atores (PrimeiroNome, UltimoNome, Genero) VALUES (@PrimeiroNome, @UltimoNome, @Genero); {banco.UltimoId}",
            entrada);

        return Results.Created($"/api/atores/{id}", await BuscarDetalheAsync(conexao, id));
    }

    private static async Task<IResult> Atualizar(Banco banco, int id, AtorEntrada entrada)
    {
        var erros = entrada.Validar();
        if (erros.Count > 0)
            return Results.ValidationProblem(erros);

        await using var conexao = await banco.AbrirAsync();
        var alterados = await conexao.ExecuteAsync(
            "UPDATE Atores SET PrimeiroNome = @PrimeiroNome, UltimoNome = @UltimoNome, Genero = @Genero WHERE Id = @id",
            new { entrada.PrimeiroNome, entrada.UltimoNome, entrada.Genero, id });

        return alterados == 0 ? Results.NotFound() : Results.Ok(await BuscarDetalheAsync(conexao, id));
    }

    private static async Task<IResult> Excluir(Banco banco, int id)
    {
        await using var conexao = await banco.AbrirAsync();
        await using var transacao = await conexao.BeginTransactionAsync();

        await conexao.ExecuteAsync("DELETE FROM ElencoFilme WHERE IdAtor = @id", new { id }, transacao);
        var removidos = await conexao.ExecuteAsync("DELETE FROM Atores WHERE Id = @id", new { id }, transacao);
        if (removidos == 0)
            return Results.NotFound();

        await transacao.CommitAsync();
        return Results.NoContent();
    }
}
