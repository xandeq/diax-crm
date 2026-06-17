# Plano de escala — Email marketing AQ (PREPARADO, NÃO ENVIAR)

**Status:** aguardando validação do piloto restaurante (retry dos 10 + métricas de open/click).
**Regra:** nada é enviado sem nova autorização explícita e sem o piloto atual estar saudável.

## Lição do piloto (ajuste técnico)
- Burst de 29 de uma vez gerou 10 com `provider=None` (atribuição sob rajada) → retry.
- **Ajuste:** enviar em **lotes ≤ 15–20** por vez, respeitando o limite horário (DIAX 150/h; cada provider ~25/h).
- Espaçar lotes para o round-robin de providers ter folga.

## Gate para escalar (todos verdadeiros)
- [ ] Os 10 em retry foram **enviados** (sentCount = 29) OU causa real resolvida
- [ ] Bounce rate < 5% · Unsub rate < 3% · Spam = 0 (após 24h)
- [ ] Open rate observável (sinal de inbox, não spam)
- [ ] Nenhum critério de parada ativo

## Próximos lotes sugeridos (conservador)
Ordem por valor x disponibilidade de leads no DIAX (a confirmar contagem antes):

| Lote | Nicho | Serviço | Volume sugerido |
|---|---|---|---|
| 2 | restaurante (restante, se houver) | Sites | ≤ 20 |
| 3 | pizzaria + cafeteria + lanchonete | Sites | ≤ 20 |
| 4 | clínica / dentista | Sites | ≤ 20 |
| 5 | advocacia / contabilidade | Sites | ≤ 20 |
| 6 | imobiliária | Sites | ≤ 20 |

- Depois de "Sites" validado por nicho, repetir a matriz com os outros 3 serviços
  (Apps, Software sob demanda, Landing pages), 1 serviço por vez por nicho.
- Cada novo nicho: filtrar no DIAX por busca de nome/termo, deduplicar (email/domínio/empresa/telefone),
  excluir opt-out/bounce/suppression, validar email — mesmo pipeline do piloto.

## Mecânica de envio por lote
1. Selecionar ≤20 IDs elegíveis (pipeline de dedup/validação).
2. Criar/atualizar campanha draft por nicho+serviço.
3. send-test interno → revisar.
4. queue dos ≤20 → monitorar 1–2 ciclos do worker.
5. Só avançar para o próximo lote se gates verdes.

## NÃO fazer
- Não automação recorrente. Não burlar rate limit. Não reenvio manual de falhas.
- Não escalar com piloto não validado.
