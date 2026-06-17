# Assuntos do piloto — restaurante + criação de sites

Use UMA por vez (A/B). Variáveis renderizadas pelo DIAX EmailTemplateEngine.
Mantidas ≤ ~60 caracteres para não truncar no celular.

## 3 variações

1. **{{empresa}} aparece no Google de quem busca onde comer?**
   - Ângulo: dor de visibilidade / FOMO. Personalizada (empresa).

2. **Seu restaurante sem site perde reserva pro concorrente**
   - Ângulo: perda concreta. Genérica, forte, direta.

3. **Site profissional pro {{empresa}} — orçamento sem compromisso**
   - Ângulo: oferta + baixa fricção. Personalizada (empresa).

## Preheader (texto de prévia, já no template)
"Quem busca onde comer em {{cidade}} pesquisa no Google antes. O {{empresa}} aparece?"

## Observações
- Evitar palavras de spam-trigger em excesso (grátis, promoção, !!!, CAIXA ALTA).
- O otimizador de assunto por IA do DIAX (POST /api/v1/ai/email-subject-optimizer)
  pode gerar mais variações se quiser ampliar o A/B.
- Não inventar números/estatísticas no assunto ou corpo.
