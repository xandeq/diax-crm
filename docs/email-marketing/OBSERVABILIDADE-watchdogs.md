# Observabilidade — Watchdogs WAHA/n8n local (OBSV-01)

Gerado em: 2026-09-19 (Phase 10, plano 10-05)

Escopo travado por `10-CONTEXT.md`: **alerta, não reparo**. Nada migra de máquina (D-04) — WAHA, n8n
e o Windows Task Scheduler continuam rodando no notebook do usuário. A entrega desta fase é o
usuário ficar sabendo quando algo cai, não a eliminação do ponto único de falha (isso ficou em
Deferred Ideas, migração para VPS).

---

## 1. O que é monitorado

| Alvo | Endpoint | Sucesso esperado | Observação |
|------|----------|-------------------|------------|
| n8n LOCAL | `http://localhost:5678/healthz` | HTTP 200 | Mesmo endpoint que `watchdog-n8n.ps1` já usava antes deste plano |
| WAHA local | `GET http://127.0.0.1:3000/api/sessions/default` (header `X-Api-Key`) | JSON com `status == "WORKING"` | Mesmo que `waha-watchdog.py` já usava (`api()`/`sessao()`) |

**`N8N_WEB_URL` (n8n cloud, EasyPanel) NÃO é alvo desta fase.** Aquele endpoint é de outro projeto e
já é coberto pelo `VPS-Watchdog` existente (`D:\claude-code\_scripts\vps-watchdog.py`). Confundir os
dois faria o runbook apontar pro lugar errado durante um incidente real.

## 2. De onde roda

Tudo roda no notebook do usuário, via Windows Task Scheduler — nenhuma infra nova, nenhuma migração
(D-04):

| Task | Estado (2026-09-19) | Execução | Intervalo |
|------|----------------------|----------|-----------|
| `n8n-watchdog` | Running | `wscript.exe` → `watchdog-n8n.ps1` | 5 min (já existia) |
| `waha-watchdog` | Ready (**registrada nesta fase — não existia antes**) | `pythonw.exe` direto (sem wrapper `.vbs`), padrão do `VPS-Watchdog` | 5 min |

A ausência da task `waha-watchdog` era a lacuna real encontrada na verificação ao vivo: o script
`waha-watchdog.py` existia no disco desde antes, mas nenhuma task do Windows o chamava — a metade
WAHA do OBSV-01 nunca teria disparado sem esse registro.

## 3. Frequência escolhida

**5 minutos**, para os dois watchdogs — confirmado pelo usuário no checkpoint deste plano
(`waha-watchdog` foi registrada com o mesmo intervalo que `n8n-watchdog` já usava, em vez de reduzir
para 15-30 min). Motivo: manter os dois watchdogs com a mesma cadência simplifica leitura dos logs e
correlação de horários entre os dois alertas.

## 4. O componente novo: `alerta_telegram.py`

**Localização:** `D:/claude-code/n8n-local/alerta_telegram.py` (229 linhas).

**`D:\claude-code\n8n-local` NÃO é um repositório git** (`git rev-parse` falha nessa pasta). Os três
arquivos de código deste plano (`alerta_telegram.py`, `waha-watchdog.py`, `watchdog-n8n.ps1`) **não
entram em nenhum commit** — isso é esperado, não um esquecimento. Só este runbook é versionado,
dentro do repositório `diax-crm`. Sessões futuras não devem procurar histórico git desses três
arquivos nem estranhar sua ausência em `git log`.

O que ele faz:
- Implementação única de alerta Telegram por **transição de estado** (up→down, down→up), stdlib-only
  (`json, os, sys, time, tempfile, urllib.request` — sem `requests`), compartilhada pelos dois
  watchdogs para não duplicar lógica.
- Lê `TELEGRAM_BOT_TOKEN`/`TELEGRAM_CHAT_ID` de `~/.claude/.secrets.env` com um parser mínimo
  (copiado do padrão já usado por `vps-watchdog.py`).
- Estado persistido em `D:/claude-code/n8n-local/logs/watchdog-estado.json`, com uma chave por alvo
  (`waha`, `n8n`) — os dois são independentes; mudar um não afeta o outro.
- Falha de rede no envio nunca propaga exceção para quem chamou (watchdog não pode morrer por causa
  do alerta) — só loga em `logs/alerta-telegram.log` e segue.
- Autoteste embutido: `python alerta_telegram.py --autoteste` roda 6 cenários contra um diretório de
  estado temporário (com o envio real substituído por stub), cobrindo primeira-leitura sem envio,
  10 chamadas repetidas em down = zero envios extras, alerta na virada (queda e volta, com duração
  formatada `XhYYmin`), e independência entre `waha`/`n8n`. Resultado confirmado nesta sessão:
  ```
  ok A-primeira-leitura
  ok B-sem-mudanca
  ok C-dez-chamadas-down-zero-envios
  ok D-alerta-volta
  ok E-alerta-queda
  ok F-independencia-waha-n8n
  ok F-estado-independente-preservado
  AUTOTESTE OK (6 cenarios)
  ```

## 5. Como os watchdogs existentes foram estendidos

Regra seguida nos dois: **só adições**, zero linha da lógica de auto-cura removida ou reescrita.

### `waha-watchdog.py` (134 linhas)
- Guarda de stdout para `pythonw.exe` (evita `AttributeError` no primeiro `print` quando a task
  agendada roda sem console).
- `from alerta_telegram import transicao`, dentro de `try/except ImportError` com fallback no-op —
  o watchdog nunca quebra por causa do alerta.
- `transicao('waha', ...)` chamado nos 4 pontos de saída de `checar()`:
  - Docker não sobe em 5 min → `False`
  - sessão `WORKING` direto → `True`
  - sessão recuperada via `/start` ou `/restart` → `True`
  - sessão não recuperou sozinha (pode exigir re-pareamento) → `False`
- O `sendText` antigo (aviso pelo próprio WhatsApp) **continua presente**, somado ao Telegram — não
  foi substituído.

### `watchdog-n8n.ps1` (30 linhas)
- Chama `alerta_telegram.py` via CLI (`--alvo n8n --estado up|down --detalhe ...`) nos dois caminhos:
  o `up` direto (`healthz` 200) e o pós-restart automático (recuperado ou não).
- **Desvio do plano, documentado:** o plano sugeria uma função PowerShell compartilhada
  (`function Alerta(...)`) chamando o CLI; na prática as duas chamadas foram inlined diretamente nos
  dois caminhos, para satisfazer o próprio critério de aceite do plano
  (`grep -c "alerta_telegram.py" watchdog-n8n.ps1 >= 2`) de forma inequívoca. Comportamento idêntico,
  ainda só-adições — nenhuma linha do restart automático foi tocada.
- `Stop-ScheduledTask`/`Start-ScheduledTask`/lógica de restart do n8n **não foram alteradas**.

## 6. Prova real de funcionamento (fault injection ao vivo)

Não foi preciso derrubar nada de propósito: o n8n local **já estava fora do ar** em 2026-09-19
(`Invoke-WebRequest http://localhost:5678/healthz` → "Unable to connect to the remote server"),
enquanto o WAHA estava `WORKING`. Isso deu um caso de teste real, gratuito, do exato cenário que esta
fase existe para matar.

Resultado confirmado pelo usuário no checkpoint:
- Chegaram **2 mensagens reais** no Telegram: `🔴 N8N FORA` e, na execução seguinte em que o restart
  automático conseguiu recuperar, `🟢 N8N VOLTOU` — ambas legíveis, no mesmo chat dos alertas do
  `VPS-Watchdog`, e claramente distinguíveis dele (o texto identifica o alvo e o host).
- **Anti-spam provado na prática:** rodar o mesmo watchdog repetidamente com o n8n ainda fora não
  gerou mensagem nova — só a transição de estado dispara envio, não o polling. Ver
  `logs/watchdog-n8n.log`: várias linhas de `sem-mudanca` entre a queda e a volta, sem envio repetido.
- `logs/watchdog-estado.json` contém as duas chaves (`waha` e `n8n`), cada uma com `vivo` e `desde`.
- Ambas as tasks (`waha-watchdog`, `n8n-watchdog`) confirmadas por `Get-ScheduledTask`.

## 7. Nuance de bootstrap — não é bug

**A primeiríssima execução de cada watchdog depois de implantado nunca dispara alerta**, mesmo que o
serviço já esteja fora do ar naquele instante. `transicao()` trata a primeira leitura (sem estado
prévio gravado) como `'primeira-leitura'`: ela só grava o estado inicial, não tem "antes" para
comparar contra. Só a **próxima mudança de estado** (a partir dali) dispara mensagem.

Na prática, isso apareceu no próprio log real desta sessão: a primeira chamada do `watchdog-n8n.ps1`
depois de estender o script registrou `primeira-leitura` (sem alerta), mesmo com o n8n já fora do ar
havia horas. O alerta só chegou na chamada seguinte, quando o estado gravado (`down`) foi comparado
contra a nova leitura.

**Por que isso importa:** se, no futuro, alguém reimplantar um dos watchdogs (apagar o
`watchdog-estado.json`, mover para outra máquina, etc.) enquanto o serviço monitorado já está caído,
a primeira checagem pós-deploy vai silenciar essa queda específica — por design, não por falha do
alerta. Não interpretar isso como o alerta "não funcionou".

## 8. O que fazer ao receber cada alerta

- **🔴 n8n FORA:** checar primeiro se a task `n8n-local` está `Disabled` (ver seção 9 — é a causa
  provável hoje). Se estiver habilitada e mesmo assim não subir, investigar `logs/watchdog-n8n.log`
  para ver quantas tentativas de restart já ocorreram.
- **🔴 waha FORA:** provável necessidade de re-parear a sessão do WhatsApp — rodar `parear.cmd`
  (mesmo fluxo manual já usado antes desta fase). Se o Docker não estiver subindo, checar Docker
  Desktop manualmente.
- **🟢 (qualquer alvo) VOLTOU:** apenas registro — a mensagem já inclui o tempo total de queda. Não
  requer ação, a não ser que a queda tenha sido anormalmente longa.

## 9. O que NÃO é alertado, e por quê (D-05)

Decisão explícita do usuário na discuss-phase, travada em `10-CONTEXT.md`:
- **Atraso do Task Scheduler** não é alertado — já é tolerado por `waves_lib.compute_wave_slots`.
- **"Rodou mas enviou 0 emails"** (circuit breaker aberto) não é alertado por este mecanismo — é um
  sinal de negócio, não de infraestrutura fora do ar, e fica fora de escopo desta fase.

## 10. Pendência registrada (fora de escopo desta fase)

**O n8n local está fora do ar e a task `n8n-local` está `Disabled`** (constatado em 2026-09-19,
antes e depois deste plano). Esta fase existe para **alertar** essa condição — e alertou, com sucesso
comprovado (seção 6) — não para **consertar** o n8n. Consertar/reabilitar `n8n-local` é decisão
explícita do usuário para tratar separadamente, fora da Phase 10. Não confundir o alerta 🔴 contínuo
de n8n nas próximas sessões com um bug deste plano — é o comportamento correto, documentando um
problema real e pré-existente que segue pendente.
