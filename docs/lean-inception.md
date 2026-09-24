# Lean Inception — Claquete

> Registro das decisões de produto do Claquete, no formato Lean Inception (Paulo Caroli):
> visão → escopo → personas → jornadas → funcionalidades → sequenciamento → MVP.

---

## 1. Visão do produto

**Para** cinéfilos, curadores de acervo e estudantes de banco de dados
**cujo** problema é ter dados de filmes, elenco e gêneros espalhados e difíceis de explorar,
**o Claquete** é um acervo de filmes na web
**que** permite buscar, cadastrar e analisar filmes, atores e gêneros, e ainda mostra o SQL que responde cada pergunta.
**Diferente de** planilhas ou de consultas soltas no gerenciador do banco,
**o nosso produto** junta catálogo, painel de estatísticas e um laboratório de consultas SQL executadas ao vivo, num só lugar.

## 2. O produto É / NÃO É / FAZ / NÃO FAZ

| É | NÃO É |
|---|---|
| Um acervo de filmes com elenco e gêneros | Um serviço de streaming |
| Uma vitrine didática de SQL relacional (JOIN, GROUP BY, filtros) | Uma rede social de críticas |
| Um sistema web leve, que roda local com um clique | Um sistema com cadastro de usuários e login (nesta versão) |

| FAZ | NÃO FAZ |
|---|---|
| Busca por nome sem se importar com acentos e maiúsculas | Reproduz trailers ou vídeos |
| Filtra por gênero, ano e duração, com ordenação | Busca pôsteres ou notas em serviços externos |
| Cadastra, edita e exclui filmes e atores, e escala o elenco | Controla permissões por perfil |
| Mostra painel com indicadores e gráficos | Exporta relatórios em PDF/Excel (ainda) |
| Executa e exibe as 12 consultas SQL do projeto | Permite digitar SQL livre (por segurança) |

## 3. Objetivos do produto

1. **Explorar o acervo com rapidez**: encontrar qualquer filme em poucos segundos, por nome, época, duração ou gênero.
2. **Manter o acervo vivo**: cadastrar e corrigir filmes, atores e papéis sem mexer no banco manualmente.
3. **Gerar análises**: responder perguntas como "qual década lançou mais filmes?" ou "quais filmes estão sem elenco?".
4. **Ensinar SQL na prática**: mostrar a consulta e o resultado lado a lado, rodando no banco de verdade.

## 4. Personas

### Marina, a cinéfila curadora (34 anos)
- **Perfil:** organiza um cineclube e mantém uma lista de clássicos.
- **Comportamento:** pesquisa filmes por década e por gênero para montar as sessões.
- **Necessidades:** achar rápido "dramas dos anos 90 com menos de 2h30" e ver quem está no elenco.

### Lucas, o estudante de dados (22 anos)
- **Perfil:** estuda .NET e SQL Server e está montando o portfólio.
- **Comportamento:** aprende melhor vendo a consulta e o resultado juntos.
- **Necessidades:** entender JOINs e agrupamentos com um exemplo real e poder conferir se o resultado bate.

### Renata, a analista de acervo (41 anos)
- **Perfil:** responde pela qualidade dos dados do acervo.
- **Comportamento:** procura lacunas, como filmes sem gênero ou sem elenco.
- **Necessidades:** números consolidados e atalhos para corrigir o registro na hora.

## 5. Jornadas

**Marina monta a sessão do mês**
1. Abre o Claquete pelo atalho da Área de Trabalho.
2. No Catálogo, filtra *Drama* e *1990–1999* e ordena por *Mais curtos*.
3. Abre o filme escolhido e confere o elenco e os papéis.
4. Clica no ator para ver a filmografia e descobre outro filme para a sessão seguinte.

**Lucas estuda para a prova de SQL**
1. Entra no Laboratório SQL.
2. Seleciona a consulta 7 (quantidade por ano) e lê o SQL com realce de sintaxe.
3. Compara a tabela de resultado com o script `database/sqlserver/02-consultas.sql` no SSMS.
4. Vai ao Painel e vê o mesmo agrupamento em forma de gráfico.

**Renata corrige lacunas do acervo**
1. No Painel, vê que existem filmes sem gênero e filmes sem elenco.
2. No Catálogo, abre um filme sem gênero e clica em *Editar*.
3. Marca os gêneros e salva; escala o ator com o papel no detalhe do filme.
4. Volta ao Painel e confere que o indicador de lacunas diminuiu.

## 6. Brainstorming de funcionalidades

| # | Funcionalidade | Persona principal |
|---|---|---|
| F1 | Catálogo com busca sem acento/maiúsculas | Marina |
| F2 | Filtros por gênero, ano e duração + ordenação | Marina |
| F3 | Detalhe do filme com gêneros e elenco | Marina |
| F4 | Cadastro/edição/exclusão de filmes com gêneros | Renata |
| F5 | Escalar e remover elenco (ator + papel) | Renata |
| F6 | Lista de atores com filtro e filmografia | Marina |
| F7 | Cadastro/exclusão de atores | Renata |
| F8 | Painel: indicadores, lançamentos por ano, décadas, gêneros, recordes e lacunas | Renata |
| F9 | Laboratório SQL com as 12 consultas executadas ao vivo | Lucas |
| F10 | Banco local SQLite pronto no primeiro uso + suporte a SQL Server | Lucas |
| F11 | Atalho na Área de Trabalho que abre o sistema | Todos |
| F12 | Pôsteres e sinopses vindos de uma API externa (ex.: TMDB) | Marina |
| F13 | Avaliações e lista "quero assistir" | Marina |
| F14 | Login e perfis (leitor/editor) | Renata |
| F15 | Exportar resultados do laboratório em CSV | Lucas |
| F16 | Paginação e busca no servidor para acervos grandes | Renata |
| F17 | Publicação em nuvem com Docker | Todos |

## 7. Revisão técnica, de negócio e de UX

Escala: esforço E (1 baixo – 3 alto), valor de negócio $ (1–3), valor de UX ♥ (1–3).

| # | E | $ | ♥ | Observação |
|---|---|---|---|---|
| F1 | 1 | 3 | 3 | Função `normalizar` no SQLite / collation `CI_AI` no SQL Server |
| F2 | 1 | 3 | 3 | Ordenação por lista branca de colunas (evita SQL injection) |
| F3 | 1 | 3 | 3 | Dois JOINs sobre tabelas associativas |
| F4 | 2 | 3 | 2 | Transação para gravar filme + gêneros juntos |
| F5 | 1 | 2 | 2 | |
| F6 | 1 | 2 | 2 | |
| F7 | 1 | 2 | 1 | |
| F8 | 2 | 3 | 3 | GROUP BY por ano, década e gênero; MAX/MIN portáveis |
| F9 | 1 | 2 | 3 | SQL lido do próprio arquivo de consultas: fonte única |
| F10 | 2 | 3 | 2 | Scripts versionados em `database/` |
| F11 | 1 | 2 | 3 | |
| F12 | 3 | 2 | 3 | Depende de chave de API externa |
| F13 | 2 | 2 | 2 | Exige usuários (F14) |
| F14 | 3 | 2 | 1 | |
| F15 | 1 | 1 | 2 | |
| F16 | 2 | 2 | 1 | Só necessário com milhares de filmes |
| F17 | 2 | 2 | 1 | |

## 8. Sequenciador

| Onda | Funcionalidades | Resultado |
|---|---|---|
| **1: MVP** ✅ | F1, F2, F3, F9, F10 | Explorar o acervo e as 12 consultas |
| **2: Acervo vivo** ✅ | F4, F5, F6, F7 | Manter filmes, atores e elenco |
| **3: Análise** ✅ | F8, F11 | Painel e acesso com um clique |
| 4: Enriquecimento | F12, F15 | Pôsteres, sinopses e exportação |
| 5: Comunidade | F14, F13 | Perfis, avaliações e lista pessoal |
| 6: Escala | F16, F17 | Acervos grandes e publicação |

✅ = já implementado nesta versão.

## 9. Canvas do MVP

| Bloco | Conteúdo |
|---|---|
| **Proposta do MVP** | Explorar um acervo relacional de filmes e ver o SQL por trás de cada resposta. |
| **Personas segmentadas** | Marina (exploração) e Lucas (aprendizado de SQL). |
| **Jornadas atendidas** | "Marina monta a sessão do mês" e "Lucas estuda para a prova de SQL". |
| **Funcionalidades** | Catálogo com busca e filtros, detalhe com elenco, Laboratório SQL, banco pronto no primeiro uso. |
| **Resultado esperado** | Encontrar um filme por qualquer critério em menos de 10 segundos; as 12 consultas batendo com o resultado esperado. |
| **Métricas para validar** | 16 testes automatizados passando (as 12 consultas + catálogo); tempo de resposta da API abaixo de 100 ms no acervo atual. |
| **Custo e cronograma** | Uma pessoa desenvolvedora; stack gratuita (.NET 9, SQLite/SQL Server, HTML/CSS/JS sem framework). |
