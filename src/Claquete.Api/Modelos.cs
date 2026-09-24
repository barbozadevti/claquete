namespace Claquete.Api;

// ---- Filmes ----

public sealed class FilmeResumo
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public int? Ano { get; set; }
    public int? Duracao { get; set; }
    public IReadOnlyList<string> Generos { get; set; } = [];
    public int TamanhoElenco { get; set; }
}

public sealed class FilmeDetalhe
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public int? Ano { get; set; }
    public int? Duracao { get; set; }
    public IReadOnlyList<GeneroResumo> Generos { get; set; } = [];
    public IReadOnlyList<ElencoItem> Elenco { get; set; } = [];
}

public sealed class ElencoItem
{
    public int Id { get; set; }
    public int IdAtor { get; set; }
    public string PrimeiroNome { get; set; } = "";
    public string UltimoNome { get; set; } = "";
    public string? Papel { get; set; }
}

public sealed class FilmeEntrada
{
    public string? Nome { get; set; }
    public int? Ano { get; set; }
    public int? Duracao { get; set; }
    public int[] Generos { get; set; } = [];

    public Dictionary<string, string[]> Validar()
    {
        var erros = new Dictionary<string, string[]>();
        Nome = Nome?.Trim();
        if (string.IsNullOrEmpty(Nome))
            erros[nameof(Nome)] = ["Informe o nome do filme."];
        else if (Nome.Length > 50)
            erros[nameof(Nome)] = ["O nome pode ter no máximo 50 caracteres."];

        var anoMaximo = DateTime.Today.Year + 5;
        if (Ano is null || Ano < 1888 || Ano > anoMaximo)
            erros[nameof(Ano)] = [$"Informe um ano entre 1888 e {anoMaximo}."];

        if (Duracao is null || Duracao < 1 || Duracao > 1000)
            erros[nameof(Duracao)] = ["Informe a duração em minutos (1 a 1000)."];

        Generos = Generos.Distinct().ToArray();
        return erros;
    }
}

public sealed class ElencoEntrada
{
    public int IdAtor { get; set; }
    public string? Papel { get; set; }

    public Dictionary<string, string[]> Validar()
    {
        var erros = new Dictionary<string, string[]>();
        Papel = Papel?.Trim();
        if (string.IsNullOrEmpty(Papel))
            erros[nameof(Papel)] = ["Informe o papel (personagem)."];
        else if (Papel.Length > 30)
            erros[nameof(Papel)] = ["O papel pode ter no máximo 30 caracteres."];
        return erros;
    }
}

// ---- Atores ----

public sealed class AtorResumo
{
    public int Id { get; set; }
    public string PrimeiroNome { get; set; } = "";
    public string UltimoNome { get; set; } = "";
    public string? Genero { get; set; }
    public int Filmes { get; set; }
}

public sealed class AtorDetalhe
{
    public int Id { get; set; }
    public string PrimeiroNome { get; set; } = "";
    public string UltimoNome { get; set; } = "";
    public string? Genero { get; set; }
    public IReadOnlyList<Participacao> Filmografia { get; set; } = [];
}

public sealed class Participacao
{
    public int IdElenco { get; set; }
    public int IdFilme { get; set; }
    public string Nome { get; set; } = "";
    public int? Ano { get; set; }
    public string? Papel { get; set; }
}

public sealed class AtorEntrada
{
    public string? PrimeiroNome { get; set; }
    public string? UltimoNome { get; set; }
    public string? Genero { get; set; }

    public Dictionary<string, string[]> Validar()
    {
        var erros = new Dictionary<string, string[]>();
        PrimeiroNome = PrimeiroNome?.Trim();
        UltimoNome = UltimoNome?.Trim();
        Genero = Genero?.Trim().ToUpperInvariant();

        if (string.IsNullOrEmpty(PrimeiroNome) || PrimeiroNome.Length > 20)
            erros[nameof(PrimeiroNome)] = ["Informe o primeiro nome (até 20 caracteres)."];
        if (string.IsNullOrEmpty(UltimoNome) || UltimoNome.Length > 20)
            erros[nameof(UltimoNome)] = ["Informe o último nome (até 20 caracteres)."];
        if (Genero is not ("M" or "F"))
            erros[nameof(Genero)] = ["O gênero deve ser M ou F."];
        return erros;
    }
}

// ---- Gêneros ----

public sealed class GeneroResumo
{
    public int Id { get; set; }
    public string Genero { get; set; } = "";
}

public sealed class GeneroComContagem
{
    public int Id { get; set; }
    public string Genero { get; set; } = "";
    public int Filmes { get; set; }
}

// ---- Estatísticas ----

public sealed record Estatisticas(
    int TotalFilmes,
    int TotalAtores,
    int TotalGeneros,
    double DuracaoMedia,
    FilmeResumo? MaisLongo,
    FilmeResumo? MaisCurto,
    IReadOnlyList<ContagemPorAno> PorAno,
    IReadOnlyList<ContagemPorDecada> PorDecada,
    IReadOnlyList<ContagemPorGenero> PorGenero,
    int AtoresMasculinos,
    int AtoresFemininos,
    int FilmesSemGenero,
    int FilmesSemElenco);

public sealed class ContagemPorAno
{
    public int Ano { get; set; }
    public int Quantidade { get; set; }
}

public sealed class ContagemPorDecada
{
    public int Decada { get; set; }
    public int Quantidade { get; set; }
    public double DuracaoMedia { get; set; }
}

public sealed class ContagemPorGenero
{
    public string Genero { get; set; } = "";
    public int Quantidade { get; set; }
}

// ---- Laboratório SQL ----

public sealed record Consulta(int Numero, string Titulo, string Sql);

public sealed record ResultadoConsulta(int Numero, string Titulo, string Sql, string[] Colunas, IReadOnlyList<object?[]> Linhas);
