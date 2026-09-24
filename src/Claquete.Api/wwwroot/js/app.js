"use strict";

const app = document.getElementById("app");

// ---------- Utilidades ----------

const esc = (valor) => String(valor ?? "").replace(/[&<>"']/g, (c) =>
  ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));

async function api(caminho, opcoes = {}) {
  const resposta = await fetch(caminho, {
    headers: opcoes.body ? { "Content-Type": "application/json" } : {},
    ...opcoes,
    body: opcoes.body ? JSON.stringify(opcoes.body) : undefined,
  });
  if (resposta.status === 204) return null;
  const dados = await resposta.json().catch(() => null);
  if (!resposta.ok) {
    const erro = new Error(dados?.title || `Erro ${resposta.status}`);
    erro.status = resposta.status;
    erro.campos = dados?.errors || {};
    throw erro;
  }
  return dados;
}

let temporizadorAviso;
function avisar(mensagem, tipo = "ok") {
  const aviso = document.getElementById("aviso");
  aviso.textContent = mensagem;
  aviso.className = `aviso visivel ${tipo}`;
  clearTimeout(temporizadorAviso);
  temporizadorAviso = setTimeout(() => aviso.classList.remove("visivel"), 3200);
}

function duracaoTexto(minutos) {
  if (!minutos) return "—";
  const h = Math.floor(minutos / 60);
  const m = minutos % 60;
  return h ? `${h}h${String(m).padStart(2, "0")}` : `${m}min`;
}

// Cartaz gerado a partir do nome: cada filme ganha sempre as mesmas cores.
function cartaz(nome) {
  let hash = 0;
  for (const c of nome) hash = (hash * 31 + c.codePointAt(0)) >>> 0;
  const matiz = hash % 360;
  const matiz2 = (matiz + 40 + (hash >> 8) % 80) % 360;
  const iniciais = nome.split(/\s+/).filter((p) => p.length > 2 || /^[A-Z0-9]/.test(p))
    .slice(0, 2).map((p) => p[0]).join("").toUpperCase();
  return `<div class="cartaz" style="background: linear-gradient(145deg, hsl(${matiz} 55% 38%), hsl(${matiz2} 60% 18%))">
    <span class="iniciais" aria-hidden="true">${esc(iniciais)}</span>
    <strong>${esc(nome)}</strong>
  </div>`;
}

function mostrarErrosDeCampo(formulario, erro) {
  formulario.querySelectorAll(".erro-campo").forEach((el) => (el.textContent = ""));
  for (const [campo, mensagens] of Object.entries(erro.campos || {})) {
    const alvo = formulario.querySelector(`[data-erro="${campo.toLowerCase()}"]`);
    if (alvo) alvo.textContent = mensagens.join(" ");
  }
}

function realcarSql(sql) {
  const palavras = /\b(SELECT|FROM|WHERE|ORDER BY|GROUP BY|INNER JOIN|LEFT JOIN|ON|AND|OR|AS|ASC|DESC|COUNT|USE|IN|NOT|EXISTS)\b/g;
  // Separa os textos entre aspas antes de escapar, para não colorir nada dentro deles.
  return sql.split(/('[^']*')/).map((parte, i) => i % 2
    ? `<span class="str">${esc(parte)}</span>`
    : esc(parte)
      .replace(/\b(\d+)\b/g, '<span class="num">$1</span>')
      .replace(palavras, '<span class="kw">$1</span>')).join("");
}

// ---------- Catálogo ----------

async function telaCatalogo(parametros) {
  const generos = await api("/api/generos");
  const filtro = Object.fromEntries(parametros);

  app.innerHTML = `
    <div class="cabecalho">
      <div>
        <h1>Catálogo</h1>
        <p>Explore o acervo por nome, gênero, época e duração.</p>
      </div>
      <a class="botao primario" href="#/filme/novo">+ Novo filme</a>
    </div>
    <form class="filtros" id="filtros">
      <label class="campo busca">Buscar pelo nome
        <input name="busca" type="search" placeholder="Ex.: genio, titanic, samurai…" value="${esc(filtro.busca)}">
      </label>
      <label class="campo">Gênero
        <select name="genero">
          <option value="">Todos</option>
          ${generos.map((g) => `<option value="${g.id}" ${String(g.id) === filtro.genero ? "selected" : ""}>${esc(g.genero)} (${g.filmes})</option>`).join("")}
        </select>
      </label>
      <label class="campo">Ano de <input name="anoDe" type="number" min="1888" value="${esc(filtro.anoDe)}"></label>
      <label class="campo">Ano até <input name="anoAte" type="number" min="1888" value="${esc(filtro.anoAte)}"></label>
      <label class="campo">Duração mín. (min) <input name="duracaoDe" type="number" min="1" value="${esc(filtro.duracaoDe)}"></label>
      <label class="campo">Duração máx. (min) <input name="duracaoAte" type="number" min="1" value="${esc(filtro.duracaoAte)}"></label>
      <label class="campo">Ordenar por
        <select name="ordenacao">
          ${[["nome:asc", "Nome (A–Z)"], ["ano:asc", "Mais antigos"], ["ano:desc", "Mais recentes"], ["duracao:desc", "Mais longos"], ["duracao:asc", "Mais curtos"]]
            .map(([v, t]) => `<option value="${v}" ${v === (filtro.ordenacao || "nome:asc") ? "selected" : ""}>${t}</option>`).join("")}
        </select>
      </label>
    </form>
    <p class="contador" id="contador"></p>
    <div class="grade" id="grade"><p class="carregando">Carregando filmes…</p></div>`;

  const formulario = document.getElementById("filtros");
  let espera;
  const aplicar = () => {
    const dados = new URLSearchParams();
    for (const [chave, valor] of new FormData(formulario)) if (valor) dados.set(chave, valor);
    history.replaceState(null, "", `#/?${dados}`);
    carregarFilmes(dados);
  };
  formulario.addEventListener("input", () => { clearTimeout(espera); espera = setTimeout(aplicar, 250); });
  formulario.addEventListener("submit", (e) => { e.preventDefault(); aplicar(); });
  await carregarFilmes(new URLSearchParams(filtro));
}

async function carregarFilmes(dados) {
  const consulta = new URLSearchParams(dados);
  const [ordem, direcao] = (consulta.get("ordenacao") || "nome:asc").split(":");
  consulta.delete("ordenacao");
  consulta.set("ordem", ordem);
  consulta.set("direcao", direcao);

  const filmes = await api(`/api/filmes?${consulta}`);
  const grade = document.getElementById("grade");
  if (!grade) return;
  document.getElementById("contador").textContent =
    filmes.length === 1 ? "1 filme encontrado" : `${filmes.length} filmes encontrados`;
  grade.innerHTML = filmes.length === 0
    ? `<p class="vazio">Nenhum filme nessa cena. Tente outros filtros.</p>`
    : filmes.map((f) => `
      <a class="cartao" href="#/filme/${f.id}">
        ${cartaz(f.nome)}
        <div class="info">
          <span class="meta">${esc(f.ano)} · ${duracaoTexto(f.duracao)}${f.tamanhoElenco ? ` · ${f.tamanhoElenco} no elenco` : ""}</span>
          <div class="etiquetas">${f.generos.map((g) => `<span class="etiqueta">${esc(g)}</span>`).join("")}</div>
        </div>
      </a>`).join("");
}

// ---------- Detalhe do filme ----------

async function telaFilme(id) {
  const [filme, atores] = await Promise.all([api(`/api/filmes/${id}`), api("/api/atores")]);

  app.innerHTML = `
    <a class="voltar" href="#/">← Voltar ao catálogo</a>
    <div class="detalhe">
      ${cartaz(filme.nome)}
      <div>
        <h1>${esc(filme.nome)}</h1>
        <div class="etiquetas">${filme.generos.map((g) => `<a class="etiqueta" href="#/?genero=${g.id}">${esc(g.genero)}</a>`).join("") || `<span class="meta">Sem gênero cadastrado</span>`}</div>
        <div class="fatos">
          <div>Ano<b>${esc(filme.ano)}</b></div>
          <div>Duração<b>${duracaoTexto(filme.duracao)}</b></div>
          <div>Minutos<b>${esc(filme.duracao)}</b></div>
        </div>
        <div class="acoes">
          <a class="botao" href="#/filme/${filme.id}/editar">Editar</a>
          <button class="botao perigo" id="excluir">Excluir filme</button>
        </div>

        <section class="bloco">
          <h2>Elenco</h2>
          ${filme.elenco.length === 0 ? `<p class="meta">Ninguém escalado ainda.</p>` : `
          <ul class="lista">
            ${filme.elenco.map((e) => `
              <li>
                <span><a href="#/ator/${e.idAtor}">${esc(e.primeiroNome)} ${esc(e.ultimoNome)}</a>
                  <span class="papel">como ${esc(e.papel)}</span></span>
                <button class="botao pequeno perigo" data-remover="${e.id}" aria-label="Remover ${esc(e.primeiroNome)} do elenco">Remover</button>
              </li>`).join("")}
          </ul>`}
          <form class="adicionar" id="escalar">
            <label class="campo">Ator ou atriz
              <select name="idAtor" required>
                ${atores.map((a) => `<option value="${a.id}">${esc(a.primeiroNome)} ${esc(a.ultimoNome)}</option>`).join("")}
              </select>
            </label>
            <label class="campo">Papel
              <input name="papel" maxlength="30" placeholder="Personagem" required>
              <span class="erro-campo" data-erro="papel"></span>
            </label>
            <button class="botao primario">Escalar</button>
          </form>
        </section>
      </div>
    </div>`;

  document.getElementById("excluir").addEventListener("click", async () => {
    if (!confirm(`Excluir "${filme.nome}"? O elenco e os gêneros ligados a ele também serão removidos.`)) return;
    await api(`/api/filmes/${filme.id}`, { method: "DELETE" });
    avisar("Filme excluído.");
    location.hash = "#/";
  });

  app.querySelectorAll("[data-remover]").forEach((botao) => botao.addEventListener("click", async () => {
    await api(`/api/filmes/${filme.id}/elenco/${botao.dataset.remover}`, { method: "DELETE" });
    avisar("Removido do elenco.");
    telaFilme(filme.id);
  }));

  const escalar = document.getElementById("escalar");
  escalar.addEventListener("submit", async (e) => {
    e.preventDefault();
    const dados = Object.fromEntries(new FormData(escalar));
    try {
      await api(`/api/filmes/${filme.id}/elenco`, { method: "POST", body: { idAtor: Number(dados.idAtor), papel: dados.papel } });
      avisar("Escalado com sucesso!");
      telaFilme(filme.id);
    } catch (erro) {
      mostrarErrosDeCampo(escalar, erro);
    }
  });
}

// ---------- Formulário de filme ----------

async function telaFormularioFilme(id) {
  const [generos, filme] = await Promise.all([
    api("/api/generos"),
    id ? api(`/api/filmes/${id}`) : Promise.resolve({ nome: "", ano: "", duracao: "", generos: [] }),
  ]);
  const selecionados = new Set(filme.generos.map((g) => g.id));

  app.innerHTML = `
    <a class="voltar" href="${id ? `#/filme/${id}` : "#/"}">← Voltar</a>
    <h1>${id ? "Editar filme" : "Novo filme"}</h1>
    <form class="formulario" id="formulario" novalidate>
      <label class="campo">Nome
        <input name="nome" maxlength="50" required value="${esc(filme.nome)}">
        <span class="erro-campo" data-erro="nome"></span>
      </label>
      <div class="linha">
        <label class="campo">Ano de lançamento
          <input name="ano" type="number" min="1888" required value="${esc(filme.ano)}">
          <span class="erro-campo" data-erro="ano"></span>
        </label>
        <label class="campo">Duração (minutos)
          <input name="duracao" type="number" min="1" max="1000" required value="${esc(filme.duracao)}">
          <span class="erro-campo" data-erro="duracao"></span>
        </label>
      </div>
      <fieldset class="campo" style="border:0;padding:0;margin:0">
        <legend style="margin-bottom:.4rem">Gêneros</legend>
        <div class="opcoes">
          ${generos.map((g) => `
            <label class="opcao"><input type="checkbox" name="generos" value="${g.id}" ${selecionados.has(g.id) ? "checked" : ""}><span>${esc(g.genero)}</span></label>`).join("")}
        </div>
        <span class="erro-campo" data-erro="generos"></span>
      </fieldset>
      <div class="acoes">
        <button class="botao primario" id="salvar">${id ? "Salvar alterações" : "Cadastrar filme"}</button>
        <a class="botao" href="${id ? `#/filme/${id}` : "#/"}">Cancelar</a>
      </div>
    </form>`;

  const formulario = document.getElementById("formulario");
  formulario.addEventListener("submit", async (e) => {
    e.preventDefault();
    const dados = new FormData(formulario);
    const corpo = {
      nome: dados.get("nome"),
      ano: dados.get("ano") ? Number(dados.get("ano")) : null,
      duracao: dados.get("duracao") ? Number(dados.get("duracao")) : null,
      generos: dados.getAll("generos").map(Number),
    };
    const botao = document.getElementById("salvar");
    botao.disabled = true;
    try {
      const salvo = await api(id ? `/api/filmes/${id}` : "/api/filmes", { method: id ? "PUT" : "POST", body: corpo });
      avisar(id ? "Alterações salvas." : "Filme cadastrado!");
      location.hash = `#/filme/${salvo.id}`;
    } catch (erro) {
      mostrarErrosDeCampo(formulario, erro);
      botao.disabled = false;
    }
  });
}

// ---------- Elenco (atores) ----------

async function telaAtores(parametros) {
  const genero = parametros.get("genero") || "";
  const busca = parametros.get("busca") || "";

  app.innerHTML = `
    <div class="cabecalho">
      <div>
        <h1>Elenco</h1>
        <p>Atores e atrizes do acervo e em quantos filmes aparecem.</p>
      </div>
    </div>
    <form class="filtros" id="filtros">
      <label class="campo">Buscar <input name="busca" type="search" placeholder="Nome ou sobrenome" value="${esc(busca)}"></label>
      <label class="campo">Gênero
        <select name="genero">
          <option value="">Todos</option>
          <option value="M" ${genero === "M" ? "selected" : ""}>Masculino</option>
          <option value="F" ${genero === "F" ? "selected" : ""}>Feminino</option>
        </select>
      </label>
    </form>
    <div id="tabela"><p class="carregando">Carregando elenco…</p></div>

    <section class="bloco">
      <h2>Cadastrar ator ou atriz</h2>
      <form class="adicionar quatro" id="novoAtor">
        <label class="campo">Primeiro nome <input name="primeiroNome" maxlength="20" required><span class="erro-campo" data-erro="primeironome"></span></label>
        <label class="campo">Último nome <input name="ultimoNome" maxlength="20" required><span class="erro-campo" data-erro="ultimonome"></span></label>
        <label class="campo">Gênero
          <select name="genero"><option value="F">Feminino</option><option value="M">Masculino</option></select>
          <span class="erro-campo" data-erro="genero"></span>
        </label>
        <button class="botao primario">Cadastrar</button>
      </form>
    </section>`;

  const filtros = document.getElementById("filtros");
  const carregar = async () => {
    const dados = new URLSearchParams();
    for (const [chave, valor] of new FormData(filtros)) if (valor) dados.set(chave, valor);
    history.replaceState(null, "", `#/atores?${dados}`);
    const atores = await api(`/api/atores?${dados}`);
    document.getElementById("tabela").innerHTML = atores.length === 0 ? `<p class="vazio">Ninguém encontrado.</p>` : `
      <div class="tabela-rolagem"><table>
        <thead><tr><th>Nome</th><th>Gênero</th><th class="numero">Filmes</th></tr></thead>
        <tbody>
          ${atores.map((a) => `<tr>
            <td><a href="#/ator/${a.id}">${esc(a.primeiroNome)} ${esc(a.ultimoNome)}</a></td>
            <td>${a.genero === "F" ? "Feminino" : "Masculino"}</td>
            <td class="numero">${a.filmes}</td>
          </tr>`).join("")}
        </tbody>
      </table></div>`;
  };
  let espera;
  filtros.addEventListener("input", () => { clearTimeout(espera); espera = setTimeout(carregar, 250); });
  filtros.addEventListener("submit", (e) => { e.preventDefault(); carregar(); });

  const novo = document.getElementById("novoAtor");
  novo.addEventListener("submit", async (e) => {
    e.preventDefault();
    try {
      const ator = await api("/api/atores", { method: "POST", body: Object.fromEntries(new FormData(novo)) });
      avisar(`${ator.primeiroNome} entrou para o elenco!`);
      location.hash = `#/ator/${ator.id}`;
    } catch (erro) {
      mostrarErrosDeCampo(novo, erro);
    }
  });

  await carregar();
}

async function telaAtor(id) {
  const ator = await api(`/api/atores/${id}`);
  app.innerHTML = `
    <a class="voltar" href="#/atores">← Voltar ao elenco</a>
    <div class="cabecalho">
      <div>
        <h1>${esc(ator.primeiroNome)} ${esc(ator.ultimoNome)}</h1>
        <p>${ator.genero === "F" ? "Atriz" : "Ator"} · ${ator.filmografia.length} ${ator.filmografia.length === 1 ? "filme" : "filmes"} no acervo</p>
      </div>
      <button class="botao perigo" id="excluir">Excluir</button>
    </div>
    <section class="bloco">
      <h2>Filmografia</h2>
      ${ator.filmografia.length === 0 ? `<p class="meta">Ainda não participou de nenhum filme do acervo. Escale pelo detalhe de um filme.</p>` : `
      <ul class="lista">
        ${ator.filmografia.map((p) => `
          <li><span><a href="#/filme/${p.idFilme}">${esc(p.nome)}</a> <span class="papel">como ${esc(p.papel)}</span></span>
          <span class="meta">${esc(p.ano)}</span></li>`).join("")}
      </ul>`}
    </section>`;

  document.getElementById("excluir").addEventListener("click", async () => {
    if (!confirm(`Excluir ${ator.primeiroNome} ${ator.ultimoNome}? As participações em filmes também serão removidas.`)) return;
    await api(`/api/atores/${ator.id}`, { method: "DELETE" });
    avisar("Removido do acervo.");
    location.hash = "#/atores";
  });
}

// ---------- Painel ----------

function barras(itens, rotulo, valor, formato = (v) => v) {
  const maximo = Math.max(...itens.map(valor), 1);
  return `<div class="barras">${itens.map((i) => `
    <div class="barra">
      <span>${esc(rotulo(i))}</span>
      <span class="trilho"><span class="preenchido" style="display:block;width:${(valor(i) / maximo) * 100}%"></span></span>
      <span class="valor">${formato(valor(i))}</span>
    </div>`).join("")}</div>`;
}

async function telaPainel() {
  const e = await api("/api/estatisticas");
  const maxAno = Math.max(...e.porAno.map((a) => a.quantidade), 1);

  app.innerHTML = `
    <div class="cabecalho">
      <div><h1>Painel</h1><p>O acervo em números, direto das consultas SQL.</p></div>
    </div>
    <div class="indicadores">
      <div class="indicador"><span>Filmes</span><b>${e.totalFilmes}</b></div>
      <div class="indicador"><span>Atores e atrizes</span><b>${e.totalAtores}</b><small>${e.atoresMasculinos} homens · ${e.atoresFemininos} mulheres</small></div>
      <div class="indicador"><span>Gêneros</span><b>${e.totalGeneros}</b></div>
      <div class="indicador"><span>Duração média</span><b>${duracaoTexto(Math.round(e.duracaoMedia))}</b><small>${e.duracaoMedia.toLocaleString("pt-BR")} minutos</small></div>
    </div>
    <div class="graficos">
      <section class="bloco">
        <h2>Lançamentos por ano</h2>
        <div class="linha-do-tempo">
          ${e.porAno.map((a) => `<div style="height:${(a.quantidade / maxAno) * 100}%" title="${a.ano}: ${a.quantidade} ${a.quantidade === 1 ? "filme" : "filmes"}"></div>`).join("")}
        </div>
        <div class="eixo"><span>${e.porAno[0]?.ano ?? ""}</span><span>${e.porAno.at(-1)?.ano ?? ""}</span></div>
      </section>
      <section class="bloco">
        <h2>Filmes por década</h2>
        ${barras(e.porDecada, (d) => `Anos ${String(d.decada).slice(2)}`, (d) => d.quantidade)}
      </section>
      <section class="bloco">
        <h2>Duração média por década</h2>
        ${barras(e.porDecada, (d) => `${d.decada}`, (d) => d.duracaoMedia, (v) => `${Math.round(v)}′`)}
      </section>
      <section class="bloco">
        <h2>Filmes por gênero</h2>
        ${barras(e.porGenero, (g) => g.genero, (g) => g.quantidade)}
      </section>
      <section class="bloco">
        <h2>Recordes e lacunas</h2>
        <ul class="lista">
          ${e.maisLongo ? `<li><span>Mais longo: <a href="#/filme/${e.maisLongo.id}">${esc(e.maisLongo.nome)}</a></span><span class="meta">${duracaoTexto(e.maisLongo.duracao)}</span></li>` : ""}
          ${e.maisCurto ? `<li><span>Mais curto: <a href="#/filme/${e.maisCurto.id}">${esc(e.maisCurto.nome)}</a></span><span class="meta">${duracaoTexto(e.maisCurto.duracao)}</span></li>` : ""}
          <li><span>Filmes sem gênero cadastrado</span><span class="meta">${e.filmesSemGenero}</span></li>
          <li><span>Filmes sem elenco cadastrado</span><span class="meta">${e.filmesSemElenco}</span></li>
        </ul>
      </section>
    </div>`;
}

// ---------- Laboratório SQL ----------

async function telaLaboratorio(parametros) {
  const consultas = await api("/api/consultas");
  const atual = Number(parametros.get("n")) || 1;

  app.innerHTML = `
    <div class="cabecalho">
      <div><h1>Laboratório SQL</h1><p>As 12 consultas do projeto, executadas ao vivo no banco.</p></div>
    </div>
    <div class="laboratorio">
      <ul class="consultas">
        ${consultas.map((c) => `<li><button data-n="${c.numero}"><span class="n">${c.numero}</span><span>${esc(c.titulo)}</span></button></li>`).join("")}
      </ul>
      <section id="resultado"></section>
    </div>`;

  const abrir = async (numero) => {
    app.querySelectorAll(".consultas button").forEach((b) => b.classList.toggle("ativo", Number(b.dataset.n) === numero));
    history.replaceState(null, "", `#/laboratorio?n=${numero}`);
    const alvo = document.getElementById("resultado");
    alvo.innerHTML = `<p class="carregando">Executando…</p>`;
    const r = await api(`/api/consultas/${numero}`);
    const numerica = r.colunas.map((_, i) => r.linhas.length > 0 && r.linhas.every((l) => typeof l[i] === "number"));
    alvo.innerHTML = `
      <h2>${r.numero}. ${esc(r.titulo)}</h2>
      <pre class="sql"><code>${realcarSql(r.sql)}</code></pre>
      <p class="contador">${r.linhas.length} ${r.linhas.length === 1 ? "linha" : "linhas"}</p>
      <div class="tabela-rolagem"><table>
        <thead><tr><th class="indice">#</th>${r.colunas.map((c, i) => `<th class="${numerica[i] ? "numero" : ""}">${esc(c)}</th>`).join("")}</tr></thead>
        <tbody>${r.linhas.map((l, n) => `<tr><td class="indice">${n + 1}</td>${l.map((v, i) => `<td class="${numerica[i] ? "numero" : ""}">${esc(v)}</td>`).join("")}</tr>`).join("")}</tbody>
      </table></div>`;
  };

  app.querySelectorAll(".consultas button").forEach((b) => b.addEventListener("click", () => abrir(Number(b.dataset.n))));
  await abrir(atual);
}

// ---------- Rotas ----------

const rotas = [
  [/^\/$/, (_, p) => telaCatalogo(p), "catalogo"],
  [/^\/filme\/novo$/, () => telaFormularioFilme(null), "catalogo"],
  [/^\/filme\/(\d+)\/editar$/, (m) => telaFormularioFilme(m[1]), "catalogo"],
  [/^\/filme\/(\d+)$/, (m) => telaFilme(m[1]), "catalogo"],
  [/^\/atores$/, (_, p) => telaAtores(p), "atores"],
  [/^\/ator\/(\d+)$/, (m) => telaAtor(m[1]), "atores"],
  [/^\/painel$/, () => telaPainel(), "painel"],
  [/^\/laboratorio$/, (_, p) => telaLaboratorio(p), "laboratorio"],
];

async function navegar() {
  const [caminho, busca = ""] = (location.hash.slice(1) || "/").split("?");
  const parametros = new URLSearchParams(busca);
  const rota = rotas.find(([padrao]) => padrao.test(caminho));

  document.querySelectorAll(".menu a").forEach((a) => a.classList.toggle("ativo", a.dataset.rota === rota?.[2]));
  app.innerHTML = `<p class="carregando">Rodando a cena…</p>`;
  window.scrollTo(0, 0);

  try {
    if (!rota) {
      app.innerHTML = `<p class="vazio">Essa cena não existe. <a href="#/">Voltar ao catálogo</a></p>`;
      return;
    }
    await rota[1](caminho.match(rota[0]), parametros);
  } catch (erro) {
    app.innerHTML = erro.status === 404
      ? `<p class="vazio">Não encontramos esse registro. <a href="#/">Voltar ao catálogo</a></p>`
      : `<p class="vazio">Algo deu errado: ${esc(erro.message)}</p>`;
  }
}

window.addEventListener("hashchange", navegar);
navegar();
api("/api/saude").then((s) => (document.getElementById("provedor").textContent = `banco: ${s.provedor}`)).catch(() => {});
