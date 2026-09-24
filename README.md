<p align="center">
  <img src="assets/claquete.png" width="96" alt="Claquete">
</p>

<h1 align="center">Claquete</h1>

<p align="center">
  Acervo de filmes com catálogo, elenco, painel de estatísticas e um laboratório de consultas SQL executadas ao vivo.<br>
  <b>.NET 9 · Minimal API · Dapper · SQLite / SQL Server · HTML + CSS + JavaScript</b>
</p>

<p align="center">
  <a href="https://github.com/barbozadevti/claquete/actions/workflows/ci.yml"><img src="https://github.com/barbozadevti/claquete/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
</p>

<p align="center">
  <img src="docs/telas/catalogo.png" alt="Catálogo do Claquete" width="860">
</p>

---

## Sobre

O Claquete nasceu de um banco relacional de filmes (filmes, atores, gêneros e suas tabelas associativas) e virou um sistema completo:

- **Catálogo**: busca por nome que ignora acentos e maiúsculas ("genio" encontra *Gênio Indomável*), filtros por gênero, ano e duração, e ordenação.
- **Detalhe do filme**: gêneros, elenco com papéis, edição, exclusão e escalação de novos atores.
- **Elenco**: lista de atores e atrizes com filtro, filmografia de cada um e cadastro.
- **Painel**: indicadores, lançamentos por ano, filmes e duração média por década, filmes por gênero, recordes e lacunas do acervo.
- **Laboratório SQL**: as 12 consultas do projeto com realce de sintaxe e o resultado lido do banco na hora.

## Telas

| Painel | Laboratório SQL |
|---|---|
| ![Painel](docs/telas/painel.png) | ![Laboratório SQL](docs/telas/laboratorio.png) |

| Detalhe do filme |
|---|
| ![Detalhe do filme](docs/telas/filme.png) |

As decisões de produto (visão, personas, jornadas, sequenciador e MVP) estão no [Lean Inception](docs/lean-inception.md).

## Como executar

### Com um clique (Windows)
Dê dois cliques em **`Abrir Claquete.cmd`** (ou no atalho *Claquete* da Área de Trabalho). O servidor sobe e o navegador abre em `http://localhost:5190`.

### Pelo terminal
Pré-requisito: [.NET 9 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet run --project src/Claquete.Api
```

Na primeira execução, o banco SQLite é criado em `src/Claquete.Api/App_Data/claquete.db` e populado com o acervo original. Para voltar ao estado inicial, apague essa pasta.

### Testes

```bash
dotnet test
```

Rodam também no GitHub Actions a cada push. São 16 testes de integração: as 12 consultas conferidas contra os resultados esperados, mais busca, filtros, cadastro completo, validações e estatísticas.

Para rodar os mesmos testes contra o **SQL Server**, informe o servidor (sem `Database`); cada classe de teste cria um banco temporário e o apaga no fim:

```powershell
$env:CLAQUETE_SQLSERVER = "Server=localhost\sqlexpress;Trusted_Connection=True;TrustServerCertificate=True"
dotnet test
```

## Usando o SQL Server

1. Rode `database/sqlserver/01-criar-banco.sql` no SQL Server (cria o banco **Filmes** com os dados).
2. Opcional: rode `database/sqlserver/03-melhorias.sql` para trocar as chaves estrangeiras originais por versões com exclusão em cascata e criar índices.
3. Em `src/Claquete.Api/appsettings.json`, troque o provedor:

```json
"Banco": {
  "Provedor": "SqlServer",
  "ConnectionString": "Server=localhost;Database=Filmes;Trusted_Connection=True;TrustServerCertificate=True"
}
```

## As 12 consultas

Estão em [`database/sqlserver/02-consultas.sql`](database/sqlserver/02-consultas.sql) e são as mesmas exibidas no Laboratório SQL: o site lê o próprio arquivo, então os dois nunca ficam diferentes.

| # | Pergunta | Recursos de SQL |
|---|---|---|
| 1 | Nome e ano dos filmes | `SELECT` |
| 2 | Nome e ano, do mais antigo ao mais novo | `ORDER BY ASC` |
| 3 | *De Volta para o Futuro*: nome, ano e duração | `WHERE =` |
| 4 | Filmes lançados em 1997 | `WHERE =` |
| 5 | Filmes lançados após 2000 | `WHERE >` |
| 6 | Duração entre 100 e 150 minutos, crescente | `AND`, `ORDER BY` |
| 7 | Quantidade de filmes por ano, decrescente | `GROUP BY`, `COUNT` |
| 8 | Atores do gênero masculino | `WHERE` |
| 9 | Atrizes, ordenadas pelo primeiro nome | `WHERE`, `ORDER BY` |
| 10 | Filmes e seus gêneros | `INNER JOIN` ×2 |
| 11 | Filmes do gênero *Mistério* | `INNER JOIN` + `WHERE` |
| 12 | Filmes com elenco e papéis | `INNER JOIN` ×2 |

## API

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/filmes?busca=&genero=&anoDe=&anoAte=&duracaoDe=&duracaoAte=&ordem=nome\|ano\|duracao&direcao=asc\|desc` | Lista filmes com filtros |
| GET | `/api/filmes/{id}` | Detalhe com gêneros e elenco |
| POST / PUT / DELETE | `/api/filmes`, `/api/filmes/{id}` | Cadastra, edita e exclui |
| POST / DELETE | `/api/filmes/{id}/elenco`, `/api/filmes/{id}/elenco/{idElenco}` | Escala e remove elenco |
| GET / POST / PUT / DELETE | `/api/atores`, `/api/atores/{id}` | Atores e filmografia |
| GET | `/api/generos` | Gêneros com quantidade de filmes |
| GET | `/api/estatisticas` | Dados do painel |
| GET | `/api/consultas`, `/api/consultas/{n}` | Laboratório SQL |

Erros de validação voltam no padrão *ProblemDetails* (`400` com a lista de campos).

## Estrutura

```
├── .github/workflows/ci.yml    # build e testes a cada push
├── Abrir Claquete.cmd          # inicia o sistema com um clique
├── database/
│   ├── sqlserver/              # script do banco, as 12 consultas e melhorias
│   └── sqlite/                 # esquema com chaves estrangeiras + carga inicial
├── docs/
│   ├── lean-inception.md
│   ├── diagrama.png
│   └── telas/                  # capturas de tela
├── src/Claquete.Api/
│   ├── Dados/Banco.cs          # conexão e diferenças SQLite x SQL Server
│   ├── Endpoints/              # filmes, atores, painel e laboratório
│   ├── Modelos.cs              # DTOs e validações
│   └── wwwroot/                # frontend (HTML, CSS, JS)
└── tests/Claquete.Tests/       # testes de integração
```

## Modelo de dados

![Diagrama do banco de dados](docs/diagrama.png)

- **Filmes** (Id, Nome, Ano, Duracao)
- **Atores** (Id, PrimeiroNome, UltimoNome, Genero)
- **Generos** (Id, Genero)
- **ElencoFilme**: associação N:N entre filmes e atores, com o papel
- **FilmesGenero**: associação N:N entre filmes e gêneros
