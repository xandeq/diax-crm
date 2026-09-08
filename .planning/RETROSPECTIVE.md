# Project Retrospective

*A living document updated after each milestone. Lessons feed forward into future planning.*

## Milestone: v1.3 — Pipeline de Aquisição

**Shipped:** 2026-09-08
**Phases:** 2 (7 e 8) | **Plans:** 11 | **Requisitos:** 6/6 validados

### What Was Built

- Qualidade de dado na entrada: checagem de MX com cache persistente por domínio, filtro de
  domínio-lixo, e degradação segura — falha de infraestrutura de DNS nunca rejeita um lead, só
  NXDOMAIN e resposta MX+A vazia rejeitam
- Rastreabilidade de descarte: os 4 motivos de rejeição (geo, e-mail-lixo, sem MX, duplicado)
  contados por rodada e consultáveis por período em `GET /customers/imports?from&to`
- Classificação site-próprio vs diretório de terceiro, persistida em `Customer.WebsiteKind` e usada
  como sinal de score
- Dedup do import por `Customer.ExternalId` com fallback e-mail→telefone, mais paridade de
  comportamento entre `source=Scraping` e `source=Import`
- Guarda de conformidade na troca de e-mail: o endereço antigo é checado contra a lista de supressão
  antes da troca, fechando um caminho que ressuscitaria contato descadastrado
- `lead_score` e segmento calculados no momento do import, com o bloco de fit recalibrado de teto 25
  para 45 para que um lead forte nasça Warm

### What Worked

- **Auditar as afirmações dos agentes contra o código, em vez de aceitar os SUMMARY.** O
  plan-checker e o verificador receberam listas explícitas de alegações para conferir com evidência
  `file:line`. Isso pegou coisas reais: um baseline de testes 29 unidades desatualizado que teria
  deixado passar regressão, e confirmou que os 12 testes de scoring pré-existentes seguiam intactos
  (verificado por `git diff --numstat`: 85 inserções, 0 remoções — não só "passaram").
- **Levar a decisão de escopo do IMPT-03 ao usuário em vez de escolher sozinho.** O requisito dizia
  "calcular score no import". Implementar ao pé da letra entregaria Cold constante, porque o teto do
  fit era 25 contra um limiar de 30. O CONTEXT.md mandava parar e perguntar, e a resposta mudou o
  trabalho: recalibrar em vez de só chamar a função.
- **Waves com arquivos disjuntos.** 08-01 e 08-02 rodaram em paralelo sem um único conflito de
  merge porque a divisão foi por arquivo, não por tema.
- **Migration única coordenada entre as duas fases** (decisão D-07): a coluna que a Phase 8 usaria
  viajou junto com as da Phase 7, evitando uma segunda ida ao banco de produção.

### What Was Inefficient

- **Squash-merge gerou conflito fantasma em todo PR depois do primeiro.** Cinco vezes seguidas o
  merge de `origin/main` acusou conflito com zero divergência real de conteúdo. O diagnóstico que
  resolve é `git diff origin/main <commit-anterior> -- <arquivos>`: retorno vazio prova que é ruído,
  e `--ours` é seguro. Custou tempo em toda iteração.
- **Duas sessões do Claude Code no mesmo working tree.** Os artefatos de planejamento da Phase 8
  acabaram dentro de um merge commit da outra sessão em vez de um commit próprio. Nada se perdeu,
  mas o histórico ficou menos legível.
- **Concorrência de `dotnet build` entre executores paralelos travou a DLL de teste uma vez.**
  Resolvido com retry, mas vale serializar a verificação final de suíte em vez de deixar cada agente
  rodar a sua.
- **Um one-liner de SUMMARY entrou no arquivo do milestone como "DESVIO em relação ao plano."** O
  extrator pega a primeira linha; quando o summary abre com o desvio, o arquivo histórico fica
  inútil. Corrigido à mão.

### Patterns Established

- **Baseline de testes por número absoluto nos critérios de aceitação.** Funciona, mas envelhece
  rápido quando outros PRs entram no meio. Reconferir o baseline imediatamente antes de executar,
  não confiar no que o plano diz.
- **Regra do `-c Release` em toda invocação de teste** (Smart App Control bloqueia DLL de Debug com
  `0x800711C7`) e **proibição de FluentAssertions** (o projeto não referencia; `Should()` não
  compila). Ambas entraram nos prompts de todo agente e nenhum violou.
- **Nunca `git add -A` neste repositório** — sessão concorrente, working tree sujo por padrão.
- **Log com prefixo literal greppável** (`ExternalIdConflict:`) quando a decisão é "observar em vez
  de persistir contador", como no D-03.

### Key Lessons

1. **Um requisito pode estar tecnicamente cumprido e ainda assim ser inútil.** "Calcular score no
   import" seria satisfeito por uma chamada de função que sempre devolve Cold. Vale checar a
   aritmética antes de aceitar que a implementação entrega o objetivo, não só a letra.
2. **Verificar afirmação com o comando certo.** Uma checagem anterior deste projeto procurou
   `ID Extrator:` nas notas dos clientes, achou zero e concluiu que o worker não tinha rodado — o
   worker tinha rodado, a busca é que estava errada. O hábito de auditar a evidência, e não o
   relato, veio disso.
3. **Deploy e "milestone completo" não são a mesma coisa.** No fechamento deste milestone as 14
   commits da Phase 8 estavam só na branch local. Criar a tag ali teria gravado um registro
   histórico falso. Conferir `git branch -r --contains` antes de declarar entrega.
4. **Uma chave de serviço "de proxy" que autentica como Admin não é chave de proxy.** Descoberto ao
   tentar usar o proxy de IA numa máquina de trabalho: a única credencial existente abria clientes,
   leads, usuários e logs de auditoria. Escopo de credencial merece a mesma atenção que escopo de
   requisito.

### Cost Observations

- Mix de modelos: planner em opus, executores/checker/verificador em sonnet
- 11 planos em 3 waves na Phase 8; wave 1 em paralelo real, waves 2 e 3 serializadas por dependência
- Notável: o custo do plan-checker se pagou num único achado (baseline desatualizado que teria
  deixado passar regressão de até 29 testes)

---

## Cross-Milestone Trends

### Process Evolution

| Milestone | Fases | Planos | Mudança de processo |
|-----------|-------|--------|---------------------|
| v1.2 (pausado) | 1 de 5 | 2/4 | Primeira tentativa de execução por waves; parou por sessão concorrente no mesmo repo |
| v1.3 | 2 | 11 | Auditoria explícita das alegações dos agentes contra `file:line`; decisão de escopo levada ao usuário em vez de assumida |

### Cumulative Quality

| Milestone | Suíte de testes | Verificação de fase |
|-----------|-----------------|---------------------|
| Início do v1.3 | 833 | — |
| Fim do v1.3 | 891 | 4/4 critérios em ambas as fases |

### Known Technical Debt

| Item | Origem | Impacto |
|------|--------|---------|
| Dry-run de `/customers/import` não replica a guarda in-batch de `ExternalId` | v1.3 Phase 8 | A prévia reporta sucesso onde o import real pularia a linha; a dedup real está correta |
| ~2300 clientes antigos sem `ExternalId` | v1.3 Phase 8 (backfill fora de escopo) | Continuam dependendo de dedup por e-mail; reavaliar se começar a falhar |
| v1.2 Agentes de IA pausado na Phase 2 (2/4 planos) | v1.2, desde 2026-05-29 | Migration `20260529134701_AddAgentFoundation` já aplicada em produção — atenção à ordem se retomado |
