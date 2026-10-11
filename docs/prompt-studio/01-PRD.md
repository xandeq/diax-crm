# Prompt Studio — PRD

> Status: proposta · Branch: `feat/prompt-studio` · Data: 2026-10-11
> Dono: Alexandre Queiroz (único usuário do DIAX CRM)

## 1. Problema

Os prompts que o Alexandre usa e reutiliza estão espalhados:

| Onde | O que tem | Limitação |
|---|---|---|
| Gerador de Prompts (`/utilities/prompt-generator`) | 20 frameworks (RTF, RISEN, COSTAR, CoT…) e histórico em `user_prompts` | O histórico é só leitura: sem título, tag, favorito, edição ou busca |
| Snippets (`/utilities/snippets`) | Título, conteúdo markdown, anexos, link público | Não dá pra editar (a API não tem PUT), sem tags, sem busca, sem variáveis |
| `diax-crm/prompts/*.txt` | 5 prompts de dev | Arquivos soltos, fora do CRM |
| Claude Code (`~/.claude/skills`, commands) | Prompts operacionais como slash commands | Não aparecem no CRM; sem histórico de versões visível |
| Cabeça / chats antigos | O resto | Perdido |

Consequência: reescrever o mesmo prompt várias vezes, não saber qual versão funcionou melhor e não conseguir usar o mesmo prompt no CRM, no ChatGPT e no Claude Code.

## 2. Objetivo

Uma **biblioteca central de prompts** dentro do DIAX CRM. Encontrar um prompt em menos de 3 segundos, preencher as variáveis, copiar ou usar direto no Claude Code via MCP. Cada mudança fica versionada.

### Métricas de sucesso (90 dias após o MVP)
- ≥ 50 prompts cadastrados; ≥ 70% com pelo menos 1 tag.
- Tempo mediano de "abrir o Studio → prompt copiado" ≤ 5 s (evento `prompt.used`).
- ≥ 30% dos usos vindos do Claude Code via MCP.
- Zero regressão no Gerador de Prompts e no Snippets (testes de caracterização verdes).

### Fora de escopo (decisão consciente)
- Multiusuário, times, permissões por prompt (o CRM é single-user).
- Tema claro. O CRM hoje é dark-only (`globals.css`); o tema claro é uma decisão global do produto, não deste módulo. Os tokens ficam preparados (ver 05-DESIGN-SYSTEM).
- Marketplace/compartilhamento público no MVP (V3).
- Execução de prompts em lote / chains (V3).

## 3. Persona e jobs-to-be-done

**Alexandre**: dev sênior, dono de agência e vários side projects (VNG, DIAX, IA Lá Fora). Usa Claude Code o dia todo, às vezes ChatGPT ou Gemini no navegador e no celular.

| # | Job | Frequência |
|---|---|---|
| J1 | "Preciso daquele prompt de revisão de código" → achar, copiar | diária |
| J2 | "Esse prompt funcionou, quero guardar" (vindo do Gerador, do chat ou do Claude Code) | diária |
| J3 | "Preencher o template de cold email pra esse lead" → variáveis | semanal |
| J4 | "Melhorei o prompt, mas a versão antiga era melhor?" → histórico e diff | semanal |
| J5 | "Testar o prompt em 2 modelos e comparar a saída" | semanal (V2) |
| J6 | "Usar o prompt da biblioteca direto no Claude Code" | diária |
| J7 | "Backup no Git / levar pra outro lugar" → export | mensal |

## 4. Escopo funcional

Prioridade: **P0** = MVP, **P1** = V2, **P2** = V3.

| Capacidade | Descrição | Prio |
|---|---|---|
| Biblioteca centralizada | Lista com preview, ordenação (atualizado, mais usado, A-Z), arquivados | P0 |
| Coleções (categorias) | Um nível, com ícone e cor; o prompt pertence a 0 ou 1 coleção | P0 |
| Tags | N por prompt, autocomplete, filtro combinável | P0 |
| Favoritos | Estrela, seção fixa no topo da sidebar | P0 |
| Busca | Título, descrição, tags e conteúdo; filtro instantâneo + busca no servidor | P0 |
| Command palette | `Ctrl/⌘+K`: buscar, abrir, copiar renderizado, criar | P0 |
| Editor Markdown | CodeMirror 6, destaque de `{{variáveis}}`, preview, autosave de rascunho | P0 |
| Variáveis | `{{nome}}` detectadas automaticamente, painel com tipo/default/obrigatória, render ao vivo | P0 |
| Templates | Prompt marcado `kind=template`, galeria "Novo a partir de template" | P0 |
| Frameworks | Os 20 frameworks atuais: rotular o prompt e "Reescrever com framework X" (reusa o Gerador) | P0 |
| Versionamento | Cada save com mudança de conteúdo = nova versão imutável, com nota | P0 |
| Histórico + diff | Linha do tempo de versões, diff lado a lado/inline, restaurar | P0 |
| Importação/exportação | `.md` com frontmatter (compatível com Claude Code), `.zip`, `.json`; dry-run antes de importar | P0 |
| Integração MCP | `diax-mcp`: tools `search/get/render/save_prompt` + capability `prompts` (slash commands no Claude Code) | P0 |
| Migração | Salvar no Studio a partir do Gerador/histórico, converter Snippet, importar os 5 `.txt` | P0 |
| Playground / testes | Rodar um prompt com variáveis em provider/modelo, guardar a execução | P1 |
| Comparação | Mesma entrada × 2 versões ou × 2 modelos, lado a lado | P1 |
| Casos de teste | Conjuntos de variáveis + asserções simples (contém / não contém / regex / JSON válido) | P1 |
| Avaliações | Nota manual 1–5 + nota por execução; média por versão | P1 |
| Git sync | Push one-way CRM → repositório GitHub (arquivos `.md`) | P1 |
| Avaliação automática | LLM-as-judge com rubrica | P2 |
| Git bidirecional | Pull de mudanças feitas no repositório | P2 |
| Analytics | Uso por prompt/canal, prompts mortos | P2 |
| Link público | Somente leitura, no padrão do Snippet público | P2 |

## 5. Fluxos principais (MVP)

1. **Copiar rápido (J1):** `Ctrl+K` → digitar "rev" → `Enter` abre / `Ctrl+Enter` copia o renderizado. Se houver variável obrigatória vazia, abre o painel de variáveis focado na primeira.
2. **Salvar do Gerador (J2):** no Gerador, botão "Salvar no Studio" → modal curto (título sugerido, tags, coleção) → cria o prompt v1 com `source=generator` e `framework` preenchido.
3. **Salvar do Claude Code (J2/J6):** "salva esse prompt na biblioteca com tag dev" → tool MCP `save_prompt` → v1 com `source=mcp`.
4. **Template com variáveis (J3):** abrir o template → painel à direita mostra os campos → preview renderizado ao vivo → "Copiar".
5. **Versões (J4):** editar → `Ctrl+S` → nota opcional → v(n+1). Aba "Histórico" → escolher 2 versões → diff → "Restaurar" cria v(n+2) igual à escolhida (nada é apagado).

## 6. Requisitos não funcionais

- **Performance:** lista de até 2.000 prompts com filtro local < 50 ms; API p95 < 300 ms (excluindo chamadas a LLM); editor carregado sob demanda (lazy).
- **Acessibilidade:** WCAG 2.2 AA, 100% operável por teclado, foco visível, `prefers-reduced-motion` respeitado.
- **Responsivo:** 3 painéis ≥ 1280 px; 2 painéis 768–1279 px; 1 painel com navegação em pilha < 768 px (uso no celular: buscar e copiar).
- **Confiabilidade:** nenhuma versão é perdida; conflito web × MCP detectado (409), nunca sobrescrito em silêncio.
- **Segurança:** dados por usuário (`IUserOwnedEntity`), preview markdown sem HTML cru, import com limites de tamanho e parser YAML tipado.
- **Compatibilidade:** zero mudança de schema ou contrato no Gerador, Snippets e `user_prompts`.

## 7. Dependências e premissas
- Infra existente: .NET 10 Clean Architecture, EF Core + SQL Server (SmarterASP), Next.js 16 em static export, shadcn/ui, `diax-mcp` (TypeScript, `@modelcontextprotocol/sdk` 1.29).
- Providers de IA já configurados no CRM (usados no "Reescrever com framework" e no Playground da V2).
- Premissa: volume < 10 mil prompts e < 100 mil versões, o que dispensa full-text search.
