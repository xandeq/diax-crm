# Prompt Studio — Design System e especificação de UX

Referências de UX: **Linear** (densidade, velocidade, teclado), **Notion** (organização e edição fluida), **Raycast** (command bar como entrada principal). O Studio usa o sistema visual real do CRM (`globals.css`, dark-first verde), sem inventar paleta nova (ADR-09).

Protótipo navegável: [`prototype/prompt-studio.html`](./prototype/prompt-studio.html).

## 1. Princípios

1. **Teclado primeiro, mouse sempre funciona.** Toda ação tem atalho; nenhuma depende só de hover.
2. **Conteúdo é o herói.** Cromado discreto, texto do prompt em mono legível, metadados recolhíveis.
3. **Nada se perde.** Autosave de rascunho local; cada save vira versão; restaurar nunca apaga.
4. **Velocidade percebida.** Filtro local instantâneo, otimista em favoritar/tag, skeleton só no primeiro load.
5. **Densidade calma.** Linhas de 36 px na lista, espaçamento em múltiplos de 4, um único acento (verde).

## 2. Tokens

O Studio só **referencia** os tokens existentes; os aliases `--ps-*` ficam em `components/prompt-studio/prompt-studio.css` (escopo `.prompt-studio`).

| Token Studio | Valor (fonte) | Uso |
|---|---|---|
| `--ps-bg` | `var(--diax-bg)` `#0F1A14` | fundo da área |
| `--ps-pane` | `var(--diax-bg-sidebar)` `#0B1510` | sidebar e painéis laterais |
| `--ps-surface` | `var(--diax-bg-card)` `rgba(255,255,255,.04)` | lista e cards |
| `--ps-surface-hover` | `var(--diax-bg-card-2)` `rgba(255,255,255,.07)` | hover/linha ativa |
| `--ps-border` / `--ps-border-strong` | `var(--diax-border)` / `var(--diax-border-2)` | divisórias |
| `--ps-text-1..4` | `var(--diax-text-1..4)` | hierarquia de texto (text-4 só decorativo ≥ 10 px caps) |
| `--ps-accent` | `var(--diax-green)` `#10B981` | foco, seleção, CTA, variável preenchida |
| `--ps-accent-dim` | `var(--diax-green-dim)` | fundo de seleção |
| `--ps-var` | `var(--chart-3)` âmbar `oklch(.769 .188 70)` | destaque de `{{variável}}` no editor |
| `--ps-var-missing` | `var(--chart-4)` `oklch(.704 .191 22)` | variável obrigatória vazia |
| `--ps-diff-add` / `--ps-diff-del` | verde 18% / vermelho 18% sobre `--ps-bg` | diff |
| `--ps-fav` | `#F5B83D` | estrela de favorito |
| `--ps-radius` | `var(--radius)` 10 px; inputs `--radius-md` 8 px | |
| `--ps-ease` | `var(--diax-transition-fast)` 150 ms `cubic-bezier(.16,1,.3,1)` | |

Cores de tag: 8 matizes derivados de `--chart-1..5` + `--accent-secondary` (`#6366F1`), escolhidas por `hash(nome) % 8`, sempre texto 85% + fundo 14% (contraste AA garantido no dark).

**Tema claro (preparado, fora do MVP):** como todos os componentes usam só `--ps-*`, um tema claro futuro redefine os aliases sob `.light .prompt-studio` sem tocar nos componentes. O protótipo já demonstra esse tema via `prefers-color-scheme`.

### Tipografia
- UI: `var(--font-sans)` (Plus Jakarta Sans). Título do prompt 20/28 600; seção 12/16 600 caps +0.04em; corpo 14/20; meta 12/16.
- Editor e preview de prompt: `var(--font-mono)` (JetBrains Mono) 13.5/22, `font-variant-ligatures: none` (prompts não são código, mas o mono ajuda a ver espaços e quebras).
- Números (versões, contadores): `tabular-nums`.

## 3. Layout

```
≥1280px  ┌──────────┬────────────────┬──────────────────────────────┬───────────┐
         │ Sidebar  │  Lista         │  Editor / Histórico / Testar │ Variáveis │
         │ 232px    │  340px (redim.)│  flex                        │ 300px     │
         └──────────┴────────────────┴──────────────────────────────┴───────────┘
768–1279 sidebar vira gaveta (☰ ou Ctrl+\), variáveis viram aba do editor
<768     pilha: Biblioteca → Prompt (voltar ←); ações principais em barra inferior fixa
         (Copiar · Variáveis · Mais); a busca fica no topo, sempre visível
```

O Studio vive dentro do `AppShell` existente (sidebar global do CRM). Em `/prompts` a sidebar global entra no modo compacto (só ícones) para liberar largura, comportamento opcional via preferência local.

## 4. Anatomia das telas

### 4.1 Sidebar (navegação do Studio)
- Botão **Novo prompt** (`C`) e campo de busca falso que abre a palette (`Ctrl+K`).
- Visões fixas: Todos · Favoritos · Recentes · Templates · Expostos no Claude Code · Arquivados (com contadores).
- **Coleções**: ícone + cor + contador; drag para reordenar; `+` inline para criar.
- **Tags**: as 12 mais usadas + "ver todas"; clicar filtra; `Shift+clique` soma filtros.

### 4.2 Lista
- Cabeçalho: título da visão, filtros ativos como chips removíveis, ordenação (Atualizado · Mais usado · A–Z).
- Linha (36 px compacta / 64 px confortável, alternável): estrela · título · tags (máx. 2 + "+n") · ícone de template/MCP · `v7` · tempo relativo. O modo confortável mostra o preview em 1 linha.
- Navegação `J/K` ou setas; `Enter` abre; `F` favorita; `E` arquiva; `Ctrl+C` copia o renderizado com defaults.
- Seleção múltipla (`X` ou `Shift+clique`) → barra de ações em lote: tag, coleção, exportar, arquivar.
- Vazio por visão: ilustração mínima (ícone Lucide 32 px), uma frase e a ação ("Nenhum favorito ainda. Pressione F em um prompt.").

### 4.3 Editor
- Cabeçalho: título editável inline, slug com ícone de copiar, chips de tags (adicionar com `#`), coleção, framework, kind, toggle "Expor no Claude Code".
- Abas: **Editor** · **Preview** · **Histórico** · **Testar** (V2). Atalhos `Ctrl+1..4`.
- CodeMirror: `{{var}}` com fundo âmbar 14% e sublinhado; autocomplete ao digitar `{{` (variáveis já declaradas); `Ctrl+S` salva versão (abre um popover pequeno de nota, `Enter` confirma, `Esc` salva sem nota); indicador "Rascunho não salvo" + autosave local a cada 2 s (localStorage com try/catch).
- Barra de status: `v7 · salvo há 3 min · 1.240 caracteres · ~310 tokens · 3 variáveis`.
- Ação **Reescrever com framework** (menu): escolhe framework e provider → mostra o diff proposta × atual → "Salvar como v8" ou "Descartar".

### 4.4 Painel de variáveis
- Um campo por variável na ordem de aparição; o tipo define o controle (input, textarea autoexpansível, select, number).
- Obrigatória vazia: borda `--ps-var-missing` + contador no topo ("2 faltando").
- Preview renderizado ao vivo logo abaixo, com as variáveis preenchidas destacadas em verde.
- Botões: **Copiar** (primário, `Ctrl+Enter`), Copiar como Markdown, Limpar. Os valores ficam lembrados por prompt (localStorage) para reuso.
- Engrenagem por variável → editar label, tipo, default, opções (grava em `variables` da próxima versão).

### 4.5 Histórico e diff
- Timeline vertical: `v7 · "Mais exemplos" · Manual · há 3 min`; badges de origem (Gerador, MCP, Import).
- Selecionar 2 versões (padrão: atual × anterior) → diff **lado a lado** (≥ 1024 px) ou **inline** (toggle `D`), por palavra, com contagem `+42 −13`.
- Ações: Restaurar esta versão (cria v8), Copiar, Abrir em modo leitura.

### 4.6 Command palette (Raycast-like)
> **Implementado diferente:** o `Ctrl/⌘+K` já é da paleta global do AppShell. O Studio registra entradas nela ("Ir para Prompt Studio", "Ir para Gerador de Prompts") e usa o filtro da lista como busca rápida (`/` foca, `Enter`/`↓` abre o 1º, `J/K` navega, `Ctrl+Enter` copia). Busca de prompts dentro da paleta global fica para V2.

**Especificação original:**
- `Ctrl/⌘+K` em qualquer lugar do Studio; também registrada na palette global do `AppShell` ("Prompts: …").
- Seções: Prompts (fuzzy no índice local) · Ações (Novo, Importar, Exportar…) · Coleções · Tags.
- Item de prompt: `Enter` abre · `Ctrl+Enter` copia renderizado (pede variáveis faltantes num passo inline) · `Ctrl+Shift+C` copia bruto · `Tab` mostra preview à direita.
- Resultado em < 16 ms por tecla (índice em memória, sem rede).

### 4.7 Importar / Exportar
- Import: dropzone (`react-dropzone`, já instalado) → **dry-run** obrigatório → tabela de prévia (criar / nova versão / pular / erro) com estratégia por conflito → Confirmar.
- Export: escolher escopo (seleção, coleção, tudo), formato (`.md`, `.zip`, `.json`) e sabor (DIAX ou "Claude Code command").

### 4.8 Galeria "Novo a partir de…"
- Em branco · Template meu · **Framework** (os 20 de `promptTypeOptions`, com "o que é", "quando usar" e exemplo, que já existem) · Importar arquivo · Do histórico do Gerador.

## 5. Atalhos

| Atalho | Ação |
|---|---|
| `Ctrl/⌘+K` | Command palette |
| `C` | Novo prompt |
| `/` | Focar busca da lista |
| `J`/`K` ou `↓`/`↑` | Navegar na lista |
| `Enter` | Abrir |
| `F` | Favoritar |
| `E` | Arquivar / desarquivar |
| `X` | Selecionar (lote) |
| `Ctrl+S` | Salvar nova versão |
| `Ctrl+Enter` | Copiar renderizado |
| `Ctrl+1..4` | Abas Editor/Preview/Histórico/Testar |
| `D` | Alternar diff lado a lado / inline |
| `Ctrl+\` | Mostrar/ocultar sidebar |
| `?` | Folha de atalhos |
| `Esc` | Fecha o popover/painel mais recente |

Atalhos de uma letra só valem com foco fora de campo de texto. Folha de atalhos acessível via `?` e no menu.

## 6. Estados e feedback
- **Carregando:** skeleton só no primeiro carregamento (lista de 8 linhas e editor com 6 barras); trocas seguintes instantâneas a partir do cache.
- **Otimista:** favoritar, tag, arquivar → atualiza na hora e reverte com toast em caso de erro.
- **Salvar:** toast discreto (Sonner, já instalado) "v8 salva" com ação "Ver diff".
- **Conflito (409):** modal "Esse prompt mudou em outro lugar (v8 pelo Claude Code)" → diff sua × atual → [Salvar a minha como v9] [Descartar a minha].
- **Erro de rede:** banner fino no topo da área + retry; o rascunho permanece local.
- **Copiado:** botão vira ✓ por 1,2 s + toast "Copiado (3 variáveis preenchidas)".

## 7. Movimento
- 150 ms `--diax-transition-fast` para hover, foco e seleção; 200 ms fade+translate 4 px para popover/palette; sem animação de layout (largura/altura).
- `prefers-reduced-motion: reduce` → transições ≤ 50 ms, sem translate.

## 8. Acessibilidade (WCAG 2.2 AA)
- Lista = `role="listbox"` com `aria-activedescendant`; palette = padrão combobox (Radix Dialog + listbox).
- Foco sempre visível: anel 2 px `--ps-accent` com offset 2 px; ordem de tab segue os painéis da esquerda para a direita.
- Contraste: texto ≥ 4.5:1 (text-1..3 já validados no `globals.css`); destaque de variável não depende só de cor (sublinhado + `aria-label` "variável obrigatória vazia").
- Editor CodeMirror com `aria-label`, e `Esc` libera o foco do editor (`Tab` não fica preso).
- Alvos de toque ≥ 44 px no mobile; live region para "Copiado" e "Versão salva".

## 9. Componentes: reuso × novos

| Necessidade | Reuso (`components/ui`) | Novo (`components/prompt-studio`) |
|---|---|---|
| Diálogos, confirmação | `dialog`, `confirm-dialog` | `ConflictDialog`, `ImportDialog` |
| Menus | `dropdown-menu` | — |
| Abas | `tabs` | — |
| Painel mobile | `sheet` | — |
| Inputs | `input`, `textarea`, `select`, `checkbox`, `label` | `TagInput` (chips + autocomplete) |
| Badges | `badge` | `TagChip` (cor por hash) |
| Scroll | `scroll-area` | — |
| Skeleton | `skeleton` | — |
| Toast | Sonner | — |
| Upload | `react-dropzone` | — |
| Markdown | `react-markdown` | `PromptPreview` (render + destaque) |
| — | — | `PromptStudioShell`, `StudioSidebar`, `PromptList`, `PromptEditor` (CodeMirror), `VariablesPanel`, `VersionTimeline`, `DiffView`, `CommandBar`, `FrameworkGallery`, `ShortcutSheet` |

Faltando no `components/ui` e útil para outros módulos: `tooltip`, `popover`, `toggle-group` (shadcn padrão, sobre Radix já presente).
