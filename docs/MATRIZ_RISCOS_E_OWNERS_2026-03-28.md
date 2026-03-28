# Matriz de Riscos e Owners

| Prioridade | Risco | Impacto | Owner | Prazo | Ação |
|---|---|---|---|---|---|
| P0 | token em storage do browser | roubo de sessão via XSS | frontend + backend | 14 dias | migrar para cookie httpOnly ou reduzir exposição e endurecer CSP |
| P0 | token por query string | vazamento em logs, histórico e referer | backend | 7 dias | remover fluxo e trocar por token efêmero de download |
| P0 | fallback admin via config | acesso indevido em bootstrap | backend | 7 dias | limitar a bootstrap/development |
| P0 | autorização inconsistente | quebra de isolamento e privilégio | backend | 14 dias | matriz de permissão por controller/action |
| P0 | segredos fora de rotação formal | incidente operacional | infra | 7 dias | executar runbook e revogar antigos |
| P1 | migrations no startup | deploy instável e startup lento | backend + devops | 21 dias | migrar para etapa controlada de deploy |
| P1 | páginas gigantes no frontend | regressão alta e custo de mudança | frontend | 30 dias | modularizar páginas críticas |
| P1 | falta de filas para tarefas pesadas | degradação de API | backend | 30 dias | introduzir worker/queue |
| P1 | cobertura de testes insuficiente | regressões frequentes | engenharia | 21 dias | expandir smoke/regressão/integração |
| P2 | ausência de billing e limites | monetização fraca | produto + backend | 60 dias | implantar planos e enforcement |
