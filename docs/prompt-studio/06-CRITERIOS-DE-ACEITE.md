# Prompt Studio — Critérios de aceite

Formato Dado/Quando/Então. Os critérios marcados **[auto]** viram teste automatizado (xUnit no backend, Playwright no frontend). O MVP só é "pronto" com todos os critérios do MVP verdes e a regressão (seção 9) verde.

## 1. Biblioteca, coleções, tags, favoritos (MVP)

- **AC-01 [auto]** Dado que não há prompts, quando abro `/prompts`, então vejo o estado vazio com as ações "Novo prompt" e "Importar".
- **AC-02 [auto]** Quando crio um prompt com título, conteúdo e 2 tags, então ele aparece no topo da lista "Todos" com `v1` e as 2 tags, sem recarregar a página.
- **AC-03 [auto]** Tags `" Cold Email "` e `"cold-email"` são normalizadas para a mesma tag `cold-email`.
- **AC-04 [auto]** Quando favorito com `F`, então a estrela muda em < 100 ms (otimista) e o prompt aparece em "Favoritos"; se a API falhar, o estado reverte e aparece um toast de erro.
- **AC-05 [auto]** Quando excluo uma coleção com 3 prompts, então os 3 continuam existindo, sem coleção.
- **AC-06 [auto]** O usuário A nunca lê, lista, exporta nem renderiza prompts do usuário B (teste de integração com 2 usuários em todas as rotas).
- **AC-07** Seleção múltipla de 5 prompts → "Adicionar tag" aplica a tag aos 5 numa ação.

## 2. Busca e command palette (MVP)

- **AC-10 [auto]** Com 2.000 prompts no índice, o filtro local responde em < 50 ms por tecla (benchmark no Playwright, mediana de 20 teclas).
- **AC-11 [auto]** A busca "rev cod" encontra "Revisor de código sênior" (tokens parciais, sem acento, ordem livre).
- **AC-12 [auto]** `GET /prompts?q=termo` encontra o termo presente só no conteúdo da versão atual e **não** encontra termo presente só em versão antiga.
- **AC-13 [auto]** `Ctrl+K` → digitar → `Ctrl+Enter` num prompt sem variáveis copia o texto para a área de transferência e registra `use` (`channel=palette`).
- **AC-14** Num prompt com variável obrigatória vazia, `Ctrl+Enter` na palette pede o valor inline antes de copiar.

## 3. Editor e variáveis (MVP)

- **AC-20 [auto]** Digitar `{{cliente}}` no editor faz a variável aparecer no painel em ≤ 300 ms (debounce), como `text` obrigatória.
- **AC-21 [auto]** O render no cliente e no servidor produz saída idêntica para todos os vetores da fixture `prompt-template-vectors.json` (incluindo escape `\{{`, variável repetida, espaços `{{ nome }}`, default, unicode).
- **AC-22 [auto]** `POST /render` com `strict=true` e variável obrigatória vazia → 400 com `missing: ["cliente"]`.
- **AC-23** Fechar a aba com alterações não salvas e reabrir o prompt restaura o rascunho local com o aviso "Rascunho recuperado".
- **AC-24** O preview markdown não executa HTML: o conteúdo `<img src=x onerror=alert(1)>` aparece como texto.
- **AC-25 [auto]** Conteúdo de 20 KB é salvo e lido sem truncamento (protege contra o default de 256 chars do DbContext).

## 4. Versionamento, histórico e diff (MVP)

- **AC-30 [auto]** `Ctrl+S` com conteúdo alterado cria `v(n+1)` com a nota informada; sem alteração (mesmo hash) não cria versão e mostra "Nada mudou".
- **AC-31 [auto]** Restaurar a v3 estando na v7 cria a v8 com conteúdo igual ao da v3; as v1–v7 continuam intactas.
- **AC-32 [auto]** Salvar com `baseVersionNumber=6` quando a atual é a v7 → 409 com a v7 no corpo; a UI abre o `ConflictDialog` com o diff.
- **AC-33** O diff entre 2 versões mostra adições e remoções por palavra, contagem `+x −y` e alterna lado a lado/inline com `D`.
- **AC-34 [auto]** Editar só metadados (título, tags, favorito) não cria versão.

## 5. Frameworks e templates (MVP)

- **AC-40** A galeria "Novo a partir de framework" lista os 20 frameworks com textos idênticos aos de `promptTypeOptions` (fonte única, sem cópia).
- **AC-41 [auto]** "Reescrever com framework COSTAR" retorna uma proposta sem salvar; só "Salvar como versão" cria a versão com `frameworkApplied=costar` e `source=generator`.
- **AC-42** Se o provider escolhido falhar com 402/429, a reescrita usa o fallback e a UI informa qual provider respondeu.

## 6. Importação / exportação (MVP)

- **AC-50 [auto]** Exportar um prompt como `.md` e importar o arquivo de volta (estratégia `new-version`) produz conteúdo, variáveis, tags e coleção idênticos (round-trip).
- **AC-51 [auto]** Importar um command do Claude Code sem bloco `diax` (`description` + `$ARGUMENTS`) cria um prompt com a variável `{{arguments}}`.
- **AC-52 [auto]** O import roda em `dryRun=true` por padrão e não grava nada; o relatório classifica create/newVersion/skip/error.
- **AC-53 [auto]** Um `.zip` com 1 arquivo de YAML inválido importa os demais e reporta o erro com nome de arquivo e linha.
- **AC-54 [auto]** `.zip` > 5 MB → 413; entrada com caminho `../../x.md` é tratada só como nome, sem escrever em disco.
- **AC-55** Export `flavor=claude-command` de um prompt com 2 variáveis gera `$1`/`$2` e `argument-hint`; o arquivo copiado para `~/.claude/commands/` funciona como `/slug` no Claude Code (teste manual documentado).

## 7. Migração dos prompts atuais (MVP)

- **AC-60 [auto]** No Gerador, "Salvar no Studio" cria o prompt com `content = generatedPrompt` e `framework = promptType`; clicar de novo no mesmo item não duplica (retorna o existente).
- **AC-61 [auto]** "Converter em prompt" num Snippet `markdown` cria o prompt; num Snippet `csharp` a ação não aparece; o Snippet original continua intacto.
- **AC-62** Os 5 arquivos de `prompts/*.txt` importados aparecem na coleção "Dev".

## 8. MCP / Claude Code (MVP)

- **AC-70 [auto]** `search_prompts { query: "revisor" }` retorna o slug e o título; `render_prompt` com variáveis devolve o texto renderizado e registra `use` com `channel=mcp`.
- **AC-71 [auto]** `render_prompt` sem a variável obrigatória devolve a lista do que falta (sem exceção no MCP).
- **AC-72 [auto]** `save_prompt` sem id cria (`source=mcp`); com slug existente cria nova versão.
- **AC-73** Um prompt com `exposeToMcp=true` aparece em `prompts/list` e, no Claude Code (após `/mcp` reconnect), como `/mcp__diax-crm__<slug>`; os argumentos correspondem às variáveis.
- **AC-74 [auto]** Um prompt com `exposeToMcp=false` não aparece em `prompts/list`, mas continua acessível via `get_prompt`.

## 9. Regressão e não funcionais (MVP)

- **AC-80 [auto]** Os testes existentes de `api-core` e o build do `crm-web` passam sem alteração.
- **AC-81 [auto]** Gerador: gerar prompt, histórico e detalhe do histórico funcionam como antes (Playwright smoke).
- **AC-82 [auto]** Snippets: criar, listar, ver link público e baixar anexo funcionam como antes.
- **AC-83** A migração `AddPromptLibrary` contém só `CreateTable`/`CreateIndex` (revisão do diff da migração no PR).
- **AC-84 [auto]** axe-core sem violações serious/critical em `/prompts` (lista, editor, palette aberta, diff).
- **AC-85** Tudo operável só por teclado (roteiro manual: criar, taguear, buscar, preencher variáveis, copiar, salvar versão, comparar, restaurar).
- **AC-86** Em 375 px de largura: buscar → abrir → preencher variáveis → copiar, sem scroll horizontal.
- **AC-87** O chunk do editor não entra no bundle das outras rotas (verificado no output do `next build`).

## 10. V2 (resumo)
- **AC-V2-01** Executar um prompt grava `PromptRun` com provider efetivo, latência e tokens; o fallback é sinalizado.
- **AC-V2-02** Comparar 2 versões × 1 modelo exibe as saídas lado a lado com o mesmo `batchId`.
- **AC-V2-03** Uma suíte com 5 casos e asserções `contains`/`regex`/`json_valid` mostra aprovados/reprovados por caso e a taxa por versão.
- **AC-V2-04** Nota 1–5 por execução; média por versão visível no histórico.
- **AC-V2-05** Git sync gera um commit no repositório configurado só com os arquivos alterados; o token nunca volta em nenhuma resposta da API.
- **AC-V2-06** O Gerador continua idêntico após a extração do `ITextLlmClient` (testes de caracterização verdes antes e depois).
