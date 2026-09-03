# -*- coding: utf-8 -*-
"""Copy v3 (skill cold-email): intro lidera com o mundo do leitor, não com o remetente.

Regras aplicadas: abrir pela situação concreta do nicho ("você/sua" > "eu"), ligar a
observação ao problema que o `service` do seg resolve, {{empresa}} no máximo 1x,
identidade do remetente só como cláusula final (ou ausente — o email já tem caixa de
credibilidade + assinatura). Sem jargão, sem urgência falsa, sem claims inventados.
Provas usadas: apenas "15+ anos" (template) e "negócios do ES / Grande Vitória".
Uso: COPY_BANK[seg]['intro'] = INTRO_V3[seg]; hook só onde HOOK_V3 tiver a chave."""

INTRO_V3 = {
    'logistica': (
        'Fim de mês na operação: fretes numa planilha, custos em outra, cobrança atrasando porque '
        'ninguém fechou a conferência. Quando o controle mora em arquivo solto, frete some e margem também. '
        'Um sistema feito para o fluxo da {{empresa}} junta carga, rota e custo num painel só — '
        'do jeito que a operação já roda hoje.'
    ),
    'arquitetura': (
        'Seu melhor projeto está no Instagram, onde só quem já te segue vê. Quem está pesquisando '
        '"arquiteto" no Google agora não chega lá — e é esse cliente que ainda não te conhece que fecha '
        'o próximo contrato. Um site portfólio coloca o trabalho da {{empresa}} na frente dessa busca, '
        'com a sofisticação que o projeto merece.'
    ),
    'estudio': (
        'Entre uma aula e outra, o celular do estúdio não para: aluno marcando, remarcando, perguntando '
        'se tem vaga. E quem esquece de responder no grupo acaba faltando. Com um app com a sua marca, '
        'o aluno agenda sozinho, recebe lembrete e controla o próprio pacote — sem o WhatsApp virar '
        'sua recepção.'
    ),
    'medico': (
        'Boa parte dos pacientes procura médico à noite, depois do expediente — quando a recepção já '
        'fechou. Sem agendamento online, essa consulta vai para o consultório que respondeu primeiro. '
        'Um site com agenda integrada marca o horário na hora e ainda posiciona a {{empresa}} no Google '
        'da sua especialidade.'
    ),
    'engenharia': (
        'Medição da obra numa planilha, custo em outra, cronograma no e-mail — e três versões do mesmo '
        'arquivo circulando na equipe. O estouro só aparece quando já aconteceu. Um sistema construído '
        'para o fluxo da {{empresa}} fecha orçamento, medição e diário de obra no mesmo lugar, com número '
        'confiável para o cliente e para o banco.'
    ),
    'advocacia': (
        'Antes de ligar, o cliente empresarial pesquisa a banca no Google: quem são os sócios, áreas de '
        'atuação, o que já escreveram. Se encontra pouco, liga para outro escritório. Um site sóbrio e '
        'bem posicionado transmite online o peso que a {{empresa}} tem no balcão — e chega antes da '
        'primeira reunião.'
    ),
    'escola': (
        'O recado da reunião de pais foi para o grupo às 15h. Às 17h já estava soterrado por dezenas de '
        'mensagens, e metade das famílias nem viu. Com um app próprio da escola, agenda, autorização e '
        'boleto chegam com confirmação de leitura — e a coordenação sabe exatamente quem recebeu.'
    ),
    'marketing': (
        'O cliente da agência aprova a campanha e pergunta: "e o site? e o app?" Sem dev na equipe, a '
        'resposta é indicar alguém — e a receita do projeto vai embora junto. Em parceria white-label, '
        'a {{empresa}} fecha o projeto e eu desenvolvo no seu nome, com escopo e prazo definidos antes '
        'da proposta.'
    ),
    'moveis': (
        'Quem vai reformar a cozinha começa pelo Google: "móveis planejados" mais a cidade, e abre os '
        'três primeiros. Se o seu ambiente mais bonito está guardado no celular do vendedor, esse cliente '
        'nem chega a pedir orçamento. Um site catálogo mostra os projetos da {{empresa}} na hora da '
        'pesquisa e leva direto pro WhatsApp.'
    ),
    'academia': (
        'Aluno que não sente acompanhamento some no segundo mês — e a cobrança da mensalidade vira '
        'corrida atrás de quem já desanimou. Com um app da academia, ele recebe a ficha do professor, '
        'faz check-in e paga pelo celular. Retenção começa no bolso do aluno, não na planilha da recepção.'
    ),
    'otica': (
        'Óculos novo começa com uma pesquisa no celular: "ótica perto de mim", modelo de armação, valor '
        'de lente. Quem aparece com catálogo e horário de exame leva o cliente; quem só tem a placa na '
        'rua, não. Um site com vitrine e agendamento de exame coloca a {{empresa}} nessa busca antes '
        'do concorrente.'
    ),
    'pousada': (
        'Cada reserva que entra pela plataforma deixa uma fatia da diária com ela — e o hóspede vira '
        'cliente da plataforma, não da pousada. Um site com motor de reservas próprio devolve essa '
        'margem e o contato direto com quem se hospeda: cada reserva direta é comissão que fica com '
        'a {{empresa}}.'
    ),
    'clinica': (
        'Paciente procura clínica quando dá tempo: à noite, no fim de semana, no intervalo do trabalho — '
        'fora do horário da recepção. Sem agendamento online, ele marca em quem respondeu primeiro. '
        'Um site com agenda integrada fecha essa consulta na hora e ainda posiciona a {{empresa}} no '
        'Google da especialidade.'
    ),
    'loja': (
        'Antes de sair de casa, o cliente já pesquisou o produto no celular e mandou mensagem para três '
        'lojas. Quem respondeu com foto, preço e link fechou a venda — mesmo com a porta fechada. Um '
        'catálogo online com pedido pelo WhatsApp deixa a {{empresa}} vendendo também fora do horário, '
        'sem virar loja virtual complicada.'
    ),
    'consultoria': (
        'A proposta foi enviada; antes de responder, o cliente digita o nome da {{empresa}} no Google. '
        'O que ele encontra decide se a próxima reunião acontece. Um site com método, casos e artigos '
        'faz esse trabalho por você — qualifica o cliente e sustenta o preço antes da primeira conversa.'
    ),
    'restaurante': (
        'Meio-dia, três pessoas decidindo onde almoçar pelo celular. Quem tem cardápio atualizado e '
        'botão de reserva ganha a mesa; quem só tem foto antiga no Instagram perde para o vizinho. '
        'Um site com cardápio, reserva e pedido no WhatsApp coloca a {{empresa}} na frente de quem está '
        'com fome agora.'
    ),
    'construtora': (
        'Antes de assinar, quem vai investir numa obra pesquisa a construtora: o que já entregou, o que '
        'está em andamento, quem está por trás. Se encontra só um perfil parado, a confiança cai antes '
        'da visita. Um site com portfólio de entregas e acompanhamento de obra transmite a solidez que '
        'a {{empresa}} tem no canteiro.'
    ),
    'informatica': (
        'Cliente pede um sistema, uma integração, um app — e a sua equipe está ocupada com suporte e '
        'infraestrutura. Recusar é entregar o cliente para outro fornecedor. Em parceria de '
        'desenvolvimento, a {{empresa}} mantém o relacionamento e a margem; eu entro na engenharia, '
        'com escopo e prazo fechados antes da proposta.'
    ),
    'turismo': (
        'Viajante abre três sites antes de fechar o pacote — compara destino, foto, preço e responde a '
        'quem passou mais confiança. Se a sua vitrine é um PDF no WhatsApp, você fica de fora dessa '
        'comparação. Um site com pacotes atualizados e cotação direta coloca a {{empresa}} na decisão '
        'final.'
    ),
    'geral_vitoria': (
        'Todo dia alguém na Grande Vitória digita no Google exatamente o que a {{empresa}} faz. Quem '
        'aparece com um site profissional e botão de WhatsApp leva o contato; quem só tem a página do '
        'Instagram fica de fora. Um site rápido, com a cara do seu negócio, coloca você nessa busca — '
        'é o que eu faço há 15 anos para negócios do ES.'
    ),
}

# Só onde o hook atual era pitch/hype ou centrado no remetente; demais segs mantêm o hook v2.
HOOK_V3 = {
    'turismo': 'Seus pacotes na comparação final do viajante — não só no PDF.',
    'informatica': 'Cliente pediu sistema? Você mantém o cliente e a margem, eu desenvolvo.',
    'loja': 'Sua loja vendendo pelo WhatsApp também com a porta fechada.',
}

SEGS_V3 = list(INTRO_V3)  # 20 segs — mesma chave usada em COPY_BANK / SHORT_SUBJ
