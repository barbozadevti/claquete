using System.Data.Common;
using Claquete.Api.Dados;
using Dapper;

namespace Claquete.Api.Endpoints;

public static class FilmesEndpoints
{
    private static readonly Dictionary<string, string> Ordenacoes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["nome"] = "F.Nome",
        ["ano"] = "F.Ano",
        ["duracao"] = "F.Duracao",
    };

    public static void MapearFilmes(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/filmes").WithTags("Filmes");

        grupo.MapGet("/", Listar);
        grupo.MapGet("/{id:int}", Obter);
        grupo.MapPost("/", Criar);
        grupo.MapPut("/{id:int}", Atualizar);
        grupo.MapDelete("/{id:int}", Excluir);
        grupo.MapPost("/{id:int}/elenco", AdicionarElenco);
        grupo.MapDelete("/{id:int}/elenco/{idElenco:int}", RemoverElenco);
    }

    private static async Task<IResult> Listar(
        Banco banco, string? busca, int? genero, int? anoDe, int? anoAte,
        int? duracaoDe, int? duracaoAte, string? ordem, string? direcao)
    {
        var filtros = new List<string>();
        if (!string.IsNullOrWhiteSpace(busca)) filtros.Add(banco.Contem("F.Nome", "@busca"));
        if (genero is not null) filtros.Add("EXISTS (SELECT 1 FROM FilmesGenero FG WHERE FG.IdFilme = F.Id AND FG.IdGenero = @genero)");
        if (anoDe is not null) filtros.Add("F.Ano >= @anoDe");
        if (anoAte is not null) filtros.Add("F.Ano <= @anoAte");
        if (duracaoDe is not null) filtros.Add("F.Duracao >= @duracaoDe");
        if (duracaoAte is not null) filtros.Add("F.Duracao <= @duracaoAte");

        var coluna = Ordenacoes.GetValueOrDefault(ordem ?? "", "F.Nome");
        var sentido = string.Equals(direcao, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
        var where = filtros.Count > 0 ? "WHERE " + string.Join(" AND ", filtros) : "";
        // Desempate por nome só quando a ordem principal é outra: o SQL Server recusa coluna repetida no ORDER BY.
        var desempate = coluna == "F.Nome" ? "" : ", F.Nome";

        var sql = $"""
            SELECT F.Id, F.Nome, F.Ano, F.Duracao,
                   (SELECT COUNT(*) FROM ElencoFilme EF WHERE EF.IdFilme = F.Id) AS TamanhoElenco
            FROM Filmes F
            {where}
            ORDER BY {coluna} {sentido}{desempate}
            """;

        await using var conexao = await banco.AbrirAsync();
        var filmes = (await conexao.QueryAsync<FilmeResumo>(sql,
            new { busca = busca?.Trim(), genero, anoDe, anoAte, duracaoDe, duracaoAte })).ToList();

        await PreencherGenerosAsync(conexao, filmes);
        return Results.Ok(filmes);
    }

    internal static async Task PreencherGenerosAsync(DbConnection conexao, List<FilmeResumo> filmes)
    {
        if (filmes.Count == 0)
            return;

        var generos = await conexao.QueryAsync<GeneroDoFilme>("""
            SELECT FG.IdFilme, G.Genero
            FROM FilmesGenero FG
            INNER JOIN Generos G ON G.Id = FG.IdGenero
            WHERE FG.IdFilme IN @ids
            ORDER BY G.Genero
            """, new { ids = filmes.Select(f => f.Id).ToArray() });

        var porFilme = generos.ToLookup(g => g.IdFilme, g => g.Genero);
        foreach (var filme in filmes)
            filme.Generos = porFilme[filme.Id].ToList();
    }

    private static async Task<IResult> Obter(Banco banco, int id)
    {
        await using var conexao = await banco.AbrirAsync();
        var filme = await BuscarDetalheAsync(conexao, id);
        return filme is null ? Results.NotFound() : Results.Ok(filme);
    }

    private static async Task<FilmeDetalhe?> BuscarDetalheAsync(DbConnection conexao, int id)
    {
        var filme = await conexao.QuerySingleOrDefaultAsync<FilmeDetalhe>(
            "SELECT Id, Nome, Ano, Duracao FROM Filmes WHERE Id = @id", new { id });
        if (filme is null)
            return null;

        filme.Generos = (await conexao.QueryAsync<GeneroResumo>("""
            SELECT G.Id, G.Genero
            FROM FilmesGenero FG
            INNER JOIN Generos G ON G.Id = FG.IdGenero
            WHERE FG.IdFilme = @id
            ORDER BY G.Genero
            """, new { id })).ToList();

        filme.Elenco = (await conexao.QueryAsync<ElencoItem>("""
            SELECT EF.Id, EF.IdAtor, A.PrimeiroNome, A.UltimoNome, EF.Papel
            FROM ElencoFilme EF
            INNER JOIN Atores A ON A.Id = EF.IdAtor
            WHERE EF.IdFilme = @id
            ORDER BY A.PrimeiroNome, A.UltimoNome
            """, new { id })).ToList();

        return filme;
    }

    private static async Task<IResult> Criar(Banco banco, FilmeEntrada entrada)
    {
        var erros = entrada.Validar();
        if (erros.Count > 0)
            return Results.ValidationProblem(erros);

        await using var conexao = await banco.AbrirAsync();
        if (!await GenerosExistemAsync(conexao, entrada.Generos))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["Generos"] = ["Algum gênero informado não existe."] });

        await using var transacao = await conexao.BeginTransactionAsync();
        var id = await conexao.ExecuteScalarAsync<int>(
            $"INSERT INTO Filmes (Nome, Ano, Duracao) VALUES (@Nome, @Ano, @Duracao); {banco.UltimoId}",
            entrada, transacao);
        await GravarGenerosAsync(conexao, transacao, id, entrada.Generos);
        await transacao.CommitAsync();

        return Results.Created($"/api/filmes/{id}", await BuscarDetalheAsync(conexao, id));
    }

    private static async Task<IResult> Atualizar(Banco banco, int id, FilmeEntrada entrada)
    {
        var erros = entrada.Validar();
        if (erros.Count > 0)
            return Results.ValidationProblem(erros);

        await using var conexao = await banco.AbrirAsync();
        if (!await GenerosExistemAsync(conexao, entrada.Generos))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["Generos"] = ["Algum gênero informado não existe."] });

        await using var transacao = await conexao.BeginTransactionAsync();
        var alterados = await conexao.ExecuteAsync(
            "UPDATE Filmes SET Nome = @Nome, Ano = @Ano, Duracao = @Duracao WHERE Id = @id",
            new { entrada.Nome, entrada.Ano, entrada.Duracao, id }, transacao);
        if (alterados == 0)
            return Results.NotFound();

        await conexao.ExecuteAsync("DELETE FROM FilmesGenero WHERE IdFilme = @id", new { id }, transacao);
        await GravarGenerosAsync(conexao, transacao, id, entrada.Generos);
        await transacao.CommitAsync();

        return Results.Ok(await BuscarDetalheAsync(conexao, id));
    }

    private static async Task<IResult> Excluir(Banco banco, int id)
    {
        await using var conexao = await banco.AbrirAsync();
        await using var transacao = await conexao.BeginTransactionAsync();

        // Remove os relacionamentos explicitamente: o script original do SQL Server não tem chaves estrangeiras com cascata.
        await conexao.ExecuteAsync("DELETE FROM ElencoFilme WHERE IdFilme = @id", new { id }, transacao);
        await conexao.ExecuteAsync("DELETE FROM FilmesGenero WHERE IdFilme = @id", new { id }, transacao);
        var removidos = await conexao.ExecuteAsync("DELETE FROM Filmes WHERE Id = @id", new { id }, transacao);
        if (removidos == 0)
            return Results.NotFound();

        await transacao.CommitAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> AdicionarElenco(Banco banco, int id, ElencoEntrada entrada)
    {
        var erros = entrada.Validar();
        if (erros.Count > 0)
            return Results.ValidationProblem(erros);

        await using var conexao = await banco.AbrirAsync();
        if (await conexao.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Filmes WHERE Id = @id", new { id }) == 0)
            return Results.NotFound();
        if (await conexao.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Atores WHERE Id = @IdAtor", entrada) == 0)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["IdAtor"] = ["Ator não encontrado."] });

        await conexao.ExecuteAsync(
            "INSERT INTO ElencoFilme (IdAtor, IdFilme, Papel) VALUES (@IdAtor, @id, @Papel)",
            new { entrada.IdAtor, id, entrada.Papel });

        return Results.Created($"/api/filmes/{id}", await BuscarDetalheAsync(conexao, id));
    }

    private static async Task<IResult> RemoverElenco(Banco banco, int id, int idElenco)
    {
        await using var conexao = await banco.AbrirAsync();
        var removidos = await conexao.ExecuteAsync(
            "DELETE FROM ElencoFilme WHERE Id = @idElenco AND IdFilme = @id", new { id, idElenco });
        return removidos == 0 ? Results.NotFound() : Results.NoContent();
    }

    private static async Task<bool> GenerosExistemAsync(DbConnection conexao, int[] generos)
    {
        if (generos.Length == 0)
            return true;
        var encontrados = await conexao.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Generos WHERE Id IN @generos", new { generos });
        return encontrados == generos.Length;
    }

    private static async Task GravarGenerosAsync(DbConnection conexao, DbTransaction transacao, int idFilme, int[] generos)
    {
        foreach (var idGenero in generos)
        {
            await conexao.ExecuteAsync(
                "INSERT INTO FilmesGenero (IdGenero, IdFilme) VALUES (@idGenero, @idFilme)",
                new { idGenero, idFilme }, transacao);
        }
    }

    private sealed class GeneroDoFilme
    {
        public int IdFilme { get; set; }
        public string Genero { get; set; } = "";
    }
}
