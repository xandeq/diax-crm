# -*- coding: utf-8 -*-
"""Template de email v2 + banco de copy por nicho (SCRP-01, Phase 10).

build_html_v2 e o template REALMENTE usado em producao (daily_waves.py:331, toda campanha).
SEGMENTS (4 nichos) e WAVES (8 nichos) alimentam 12 dos 20 nichos de daily_waves.ROTATION via
_harvest() -> COPY_BANK. Os outros 8 sao definidos inline em daily_waves.py.

Extraido VERBATIM de fire_w1_2707.py (build_html_v2, SEGMENTS) e fire_w2w3_2707.py (WAVES).
Mudanca de forma de dict aqui NAO da ImportError — quebra em silencio dias depois, quando o
cursor da rotacao chegar no nicho afetado. Validar sempre com waves_smoke.py.
"""
from waves_core import DIR, qp


def build_html_v2(hook, subhead, img_main, intro, bullets, wa):
    """v2: header dark + hook + subhead + intro -> IMAGEM PRINCIPAL NO MEIO -> entregas -> credibilidade -> CTA -> footer."""
    bl = "".join(
        '<tr><td style="padding:0 0 12px 0;">'
        '<table role="presentation" cellpadding="0" cellspacing="0" border="0"><tr>'
        '<td valign="top" style="padding:2px 10px 0 0;"><span style="display:inline-block;width:20px;height:20px;border-radius:10px;background-color:#e0f2fe;color:#0369a1;font-family:Arial,Helvetica,sans-serif;font-size:13px;font-weight:bold;text-align:center;line-height:20px;">&#10003;</span></td>'
        '<td style="font-family:Arial,Helvetica,sans-serif;font-size:16px;line-height:24px;color:#0f172a;">' + b + '</td>'
        '</tr></table></td></tr>' for b in bullets)
    return ('<!DOCTYPE html><html lang="pt-BR"><head><meta charset="utf-8"/><meta name="viewport" content="width=device-width, initial-scale=1.0"/></head>'
    '<body style="margin:0;padding:0;background-color:#f1f5f9;">'
    '<div style="display:none;max-height:0;overflow:hidden;mso-hide:all;">' + subhead + '</div>'
    '<table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color:#f1f5f9;"><tr><td align="center" style="padding:28px 12px;">'
    '<table role="presentation" width="600" cellpadding="0" cellspacing="0" border="0" style="width:600px;max-width:600px;background-color:#ffffff;border-radius:14px;overflow:hidden;">'
    # header
    '<tr><td align="left" style="background-color:#0f172a;padding:20px 32px;"><img src="https://www.alexandrequeiroz.com.br/images/logo.png" alt="Alexandre Queiroz" width="170" style="display:block;border:0;height:auto;width:170px;"/></td></tr>'
    '<tr><td style="height:4px;background:#57b3df;line-height:4px;font-size:4px;">&nbsp;</td></tr>'
    # hook + subhead
    '<tr><td style="padding:36px 32px 8px 32px;"><h1 style="margin:0;font-family:Arial,Helvetica,sans-serif;font-size:27px;line-height:35px;color:#0f172a;font-weight:bold;">' + hook + '</h1></td></tr>'
    '<tr><td style="padding:0 32px 18px 32px;"><p style="margin:0;font-family:Arial,Helvetica,sans-serif;font-size:16px;line-height:24px;color:#64748b;">' + subhead + '</p></td></tr>'
    # intro
    '<tr><td style="padding:0 32px 22px 32px;"><p style="margin:0;font-family:Arial,Helvetica,sans-serif;font-size:16px;line-height:26px;color:#334155;">' + intro + '</p></td></tr>'
    # IMAGEM PRINCIPAL — NO MEIO DO EMAIL
    '<tr><td style="padding:0 32px 26px 32px;font-size:0;line-height:0;"><img src="' + img_main + '" alt="" width="536" style="display:block;width:100%;max-width:536px;height:auto;border:0;border-radius:12px;"/></td></tr>'
    # entregas
    '<tr><td style="padding:0 32px 4px 32px;"><p style="margin:0 0 14px 0;font-family:Arial,Helvetica,sans-serif;font-size:16px;line-height:24px;color:#0f172a;font-weight:bold;">Para a {{empresa}}, eu entrego:</p>'
    '<table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0">' + bl + '</table></td></tr>'
    # credibilidade
    '<tr><td style="padding:18px 32px 0 32px;"><table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0"><tr><td style="background-color:#f8fafc;border-left:4px solid #57b3df;border-radius:8px;padding:14px 18px;font-family:Arial,Helvetica,sans-serif;font-size:14px;line-height:22px;color:#475569;">Engenheiro de software s&ecirc;nior &mdash; <strong>15+ anos</strong> e <strong>+200 projetos</strong> para empresas de Vit&oacute;ria, Vila Velha, Serra, Cariacica e interior do ES &mdash; de multinacionais a neg&oacute;cios locais.</td></tr></table></td></tr>'
    # CTA
    '<tr><td align="center" style="padding:28px 32px 8px 32px;"><table role="presentation" cellpadding="0" cellspacing="0" border="0"><tr><td align="center" bgcolor="#25D366" style="border-radius:10px;"><a href="https://wa.me/5527999840101?text=' + qp(wa) + '&amp;utm_source=email&amp;utm_medium=campanha&amp;utm_campaign=aq_wave_2707" target="_blank" style="display:inline-block;padding:16px 34px;font-family:Arial,Helvetica,sans-serif;font-size:17px;line-height:21px;font-weight:bold;color:#ffffff;text-decoration:none;border-radius:10px;">Quero um diagn&oacute;stico gratuito no WhatsApp &#8250;</a></td></tr></table>'
    '<p style="margin:10px 0 0 0;font-family:Arial,Helvetica,sans-serif;font-size:13px;color:#64748b;">Sem compromisso &middot; resposta no mesmo dia &uacute;til.</p></td></tr>'
    # assinatura
    '<tr><td align="center" style="padding:26px 32px 24px 32px;"><p style="margin:0;font-family:Georgia,serif;font-size:18px;line-height:26px;color:#0f172a;font-style:italic;">&ldquo;Tecnologia que faz o seu neg&oacute;cio vender mais.&rdquo;</p></td></tr>'
    # footer
    '<tr><td style="background-color:#0f172a;padding:24px 32px;" align="center"><img src="https://www.alexandrequeiroz.com.br/images/logo.png" alt="AQ" width="150" style="display:block;border:0;height:auto;width:150px;margin:0 auto 12px auto;"/>'
    '<p style="margin:0 0 4px 0;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:20px;color:#cbd5e1;">Sites &middot; Aplicativos &middot; Sistemas sob demanda &middot; Landing pages</p>'
    '<p style="margin:0 0 4px 0;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:20px;color:#cbd5e1;">WhatsApp (27) 99984-0101 &middot; contato@alexandrequeiroz.com.br</p>'
    '<p style="margin:0 0 10px 0;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:20px;color:#57b3df;">www.alexandrequeiroz.com.br</p>'
    '<p style="margin:0;font-family:Arial,Helvetica,sans-serif;font-size:11px;line-height:17px;color:#64748b;">Vit&oacute;ria, ES &middot; <a href="{{unsubscribe_url}}" target="_blank" style="color:#64748b;text-decoration:underline;">descadastrar</a></p></td></tr>'
    '</table></td></tr></table></body></html>')

SEGMENTS = [
    dict(seg='logistica', search_terms=['logistica', 'transporte', 'transportes', 'armazem'],
        service='Sistema de Gestao',
        subject='{{empresa}}: quantos fretes se perdem na planilha todo mês?',
        hook='Fretes, rotas e custos num painel só — sem planilha, sem retrabalho.',
        subhead='Sistema sob medida para a operação logística, do jeito que ela funciona hoje.',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo sistemas sob medida. Operação logística controlada em planilha perde frete, atrasa cobrança e esconde custo — um sistema feito para o seu fluxo devolve o controle no mesmo dia.',
        bullets=['Painel de fretes e cargas em tempo real',
                 'Custos por rota, veículo e cliente sem digitação dupla',
                 'Portal do cliente para rastrear entregas sozinho',
                 'Alertas de atraso e cobrança automática'],
        wa='Ola! Quero um diagnostico rapido de um sistema para minha operacao logistica',
        img='A Brazilian logistics manager monitoring a freight management dashboard on a large screen in a modern warehouse office, trucks visible through the window, realistic editorial photo'),
    dict(seg='arquitetura', search_terms=['arquitetura', 'arquiteto', 'arquitetos'],
        service='Site Portfolio',
        subject='O portfólio da {{empresa}} aparece no Google — ou só no Instagram?',
        hook='Quem procura arquiteto no Google precisa encontrar o seu portfólio.',
        subhead='Site profissional que transforma projeto entregue em cliente novo.',
        intro='Olá! Sou o Alexandre Queiroz e crio sites profissionais. Instagram mostra o projeto para quem já te segue — o site coloca seu portfólio na frente de quem está pesquisando arquiteto agora, com a sofisticação que o seu trabalho pede.',
        bullets=['Portfólio em alta resolução que valoriza cada projeto',
                 'Aparecer no Google de quem busca arquiteto na sua cidade',
                 'Página de serviços que filtra o cliente certo',
                 'Contato direto no WhatsApp a partir de qualquer projeto'],
        wa='Ola! Quero um diagnostico rapido de um site portfolio para meu escritorio de arquitetura',
        img='A Brazilian architect presenting an elegant portfolio website on a large monitor in a modern architecture studio with scale models and blueprints, realistic editorial photo'),
    dict(seg='estudio', search_terms=['yoga', 'pilates', 'estudio'],
        service='App de Agendamento',
        subject='Seus alunos agendando sozinhos — sem WhatsApp o dia inteiro',
        hook='Um app com a sua marca: aula marcada, lembrete enviado, falta reduzida.',
        subhead='Agendamento, pacotes e fidelização direto no celular do aluno.',
        intro='Olá! Sou o Alexandre Queiroz e desenvolvo aplicativos. Estúdio que agenda por WhatsApp perde hora respondendo e perde aluno quando esquece — com app próprio, o aluno marca, remarca e recebe lembrete sozinho.',
        bullets=['Agendamento de aulas 24h pelo app, sem mensagens',
                 'Lembretes automáticos que reduzem falta',
                 'Controle de pacotes, créditos e vencimentos',
                 'Publicado na App Store e Google Play com a sua marca'],
        wa='Ola! Quero um diagnostico rapido de um app de agendamento para meu estudio',
        img='A Brazilian yoga studio owner showing a class booking app on a smartphone to a student at a bright modern studio reception, mats and plants in background, realistic editorial photo'),
    dict(seg='medico', search_terms=['medico', 'consultorio', 'cardiologia', 'dermatologia'],
        service='Site com Agendamento',
        subject='Pacientes agendando na {{empresa}} — até fora do horário da recepção',
        hook='Consultório que agenda online atende quem procura médico à noite.',
        subhead='Site profissional com agendamento que trabalha quando a recepção não está.',
        intro='Olá! Sou o Alexandre Queiroz e crio sites para a área da saúde. Boa parte dos pacientes procura médico fora do horário comercial — site com agendamento online captura essa consulta que hoje vai para outro consultório.',
        bullets=['Agendamento online integrado à agenda do consultório',
                 'Aparecer no Google de quem busca a sua especialidade',
                 'Página que transmite confiança: equipe, estrutura, convênios',
                 'Lembretes que reduzem falta de paciente'],
        wa='Ola! Quero um diagnostico rapido de um site com agendamento para meu consultorio',
        img='A Brazilian doctor in a modern medical office reviewing an online appointment website on a laptop, clean bright clinic interior, realistic editorial photo'),
]

WAVES = [
 dict(wave=2, send_api='2026-07-27T13:30:00Z', send_sql='2026-07-27 13:30:00',
      file=DIR+'previews/wave-2026-07-27-w2.json', segments=[
  dict(seg='engenharia', search_terms=['engenharia', 'engenheiro'], service='Sistema sob medida',
    subject='{{empresa}}: obra controlada em planilha ainda fecha a conta?',
    hook='Medições, custos e cronograma da obra num sistema só.',
    subhead='Software sob medida para engenharia — do orçamento à medição final.',
    intro='Olá! Sou o Alexandre Queiroz e desenvolvo sistemas sob medida. Obra gerida em planilha espalha versão, atrasa medição e esconde estouro de custo — um sistema no seu fluxo fecha o ciclo no mesmo lugar.',
    bullets=['Cronograma e medições por obra em tempo real','Custos previstos × realizados sem digitação dupla','Diário de obra e fotos no celular da equipe','Relatórios prontos para o cliente e para o banco'],
    wa='Ola! Quero um diagnostico rapido de um sistema para minha empresa de engenharia',
    img='A Brazilian civil engineer reviewing a construction management dashboard on a tablet at a construction site office, cranes in background, realistic editorial photo'),
  dict(seg='advocacia', search_terms=['advocacia', 'advogados', 'juridico'], service='Site Profissional',
    subject='Quem procura advogado no Google encontra a {{empresa}}?',
    hook='Autoridade se constrói antes da primeira reunião.',
    subhead='Site profissional que posiciona a banca em quem pesquisa a sua área.',
    intro='Olá! Sou o Alexandre Queiroz e crio sites profissionais. Cliente empresarial pesquisa a banca antes de ligar — um site sóbrio, rápido e bem posicionado no Google transmite o peso que a {{empresa}} tem no balcão.',
    bullets=['Áreas de atuação e equipe com autoridade','Artigos que posicionam a banca no Google','Agendamento de consulta pelo WhatsApp','Design sóbrio, rápido e impecável no celular'],
    wa='Ola! Quero um diagnostico rapido de um site para meu escritorio de advocacia',
    img='A Brazilian lawyer in a modern law office reviewing an elegant law firm website on a large monitor, bookshelves and city view, realistic editorial photo'),
  dict(seg='escola', search_terms=['escola', 'colegio', 'ensino'], service='App Escola-Familia',
    subject='Recados da {{empresa}} chegando lidos — não perdidos no grupo',
    hook='Agenda, recados e autorizações no celular dos pais. Lidos.',
    subhead='App próprio da escola: comunicação que a família realmente vê.',
    intro='Olá! Sou o Alexandre Queiroz e desenvolvo aplicativos. Grupo de WhatsApp engole recado importante — com app próprio, a escola envia agenda, autorização e boleto com confirmação de leitura.',
    bullets=['Recados com confirmação de leitura por turma','Agenda, eventos e autorizações digitais','Fotos do dia com privacidade controlada','Publicado nas lojas com a marca da escola'],
    wa='Ola! Quero um diagnostico rapido de um app para minha escola',
    img='A Brazilian school coordinator showing a school communication app on a smartphone to parents at a bright modern school entrance, realistic editorial photo'),
  dict(seg='marketing', search_terms=['marketing', 'publicidade', 'agencia'], service='Dev White-label',
    subject='{{empresa}}, e se a sua agência entregasse sites e apps sem contratar dev?',
    hook='Seu cliente pede site, app, sistema — eu entrego no seu nome.',
    subhead='Desenvolvimento white-label para agências: você vende, eu construo.',
    intro='Olá! Sou o Alexandre Queiroz, engenheiro de software. Agência perde receita quando o cliente pede tecnologia e ela só entrega mídia — em parceria white-label, você fecha o projeto e eu desenvolvo com o seu branding.',
    bullets=['Sites, landing pages, apps e sistemas no seu nome','Prazo e escopo fechados antes de você propor ao cliente','Você mantém o relacionamento; eu fico invisível','Margem sua em cada projeto entregue'],
    wa='Ola! Tenho uma agencia e quero saber mais sobre desenvolvimento white-label',
    img='A Brazilian marketing agency team presenting a website project to a client on a big screen in a creative modern office, realistic editorial photo'),
 ]),
 dict(wave=3, send_api='2026-07-27T14:30:00Z', send_sql='2026-07-27 14:30:00',
      file=DIR+'previews/wave-2026-07-27-w3.json', segments=[
  dict(seg='moveis', search_terms=['moveis', 'moveis planejados', 'marcenaria'], service='Site Catalogo',
    subject='O showroom da {{empresa}} aberto 24h — no Google',
    hook='Quem pesquisa móveis planejados precisa ver o seu projeto primeiro.',
    subhead='Site catálogo que transforma ambiente entregue em orçamento novo.',
    intro='Olá! Sou o Alexandre Queiroz e crio sites profissionais. Móvel planejado se vende pelo projeto bonito — um site catálogo mostra seus ambientes para quem está pesquisando agora e leva direto pro orçamento no WhatsApp.',
    bullets=['Catálogo de ambientes em alta resolução','Aparecer no Google de quem busca planejados na cidade','Orçamento direto no WhatsApp a partir de cada projeto','Rápido e bonito no celular, com a sua marca'],
    wa='Ola! Quero um diagnostico rapido de um site catalogo para minha loja de moveis',
    img='A Brazilian custom furniture showroom owner showing a furniture catalog website on a tablet, elegant planned kitchen displays behind, realistic editorial photo'),
  dict(seg='academia', search_terms=['academia', 'crossfit', 'fitness'], service='App de Treino',
    subject='Alunos da {{empresa}} com treino no bolso — e matrícula em dia',
    hook='App próprio: treino, check-in e cobrança sem planilha.',
    subhead='Retenção começa no celular do aluno.',
    intro='Olá! Sou o Alexandre Queiroz e desenvolvo aplicativos. Aluno que sente acompanhamento fica — com app da academia, ele recebe treino, marca aula e paga sem você correr atrás.',
    bullets=['Fichas de treino atualizadas pelo professor','Check-in e agendamento de aulas pelo app','Cobrança recorrente com aviso de vencimento','Publicado nas lojas com a marca da academia'],
    wa='Ola! Quero um diagnostico rapido de um app para minha academia',
    img='A Brazilian gym owner showing a workout app on a smartphone to a member in a modern gym, equipment in background, realistic editorial photo'),
  dict(seg='otica', search_terms=['otica', 'oticas'], service='Site com Catalogo',
    subject='{{empresa}}: quem busca óculos na sua cidade te encontra online?',
    hook='Vitrine no Google: armações, lentes e agendamento de exame.',
    subhead='Site que coloca a ótica na frente de quem está pronto pra comprar.',
    intro='Olá! Sou o Alexandre Queiroz e crio sites profissionais. Óculos começa com pesquisa no celular — um site com catálogo e agendamento de exame captura esse cliente antes do concorrente.',
    bullets=['Catálogo de armações e marcas em destaque','Agendamento de exame de vista online','Aparecer no Google de quem busca ótica na cidade','WhatsApp direto para orçamento de lentes'],
    wa='Ola! Quero um diagnostico rapido de um site para minha otica',
    img='A Brazilian optician showing eyewear frames and a store website on a tablet in a modern optical store with elegant glasses displays, realistic editorial photo'),
  dict(seg='pousada', search_terms=['pousada', 'hospedagem'], service='Site com Reservas',
    subject='Reserva direta na {{empresa}} — sem pagar comissão de plataforma',
    hook='Cada reserva direta é comissão que fica com você.',
    subhead='Site com motor de reservas próprio para a sua pousada.',
    intro='Olá! Sou o Alexandre Queiroz e crio sites para hospedagem. Plataforma cobra comissão em toda reserva — um site bonito com reserva direta paga o investimento em poucas diárias.',
    bullets=['Motor de reservas direto no site, sem comissão','Fotos que vendem a experiência da pousada','Aparecer no Google de quem busca hospedagem na região','Integração com WhatsApp para dúvidas rápidas'],
    wa='Ola! Quero um diagnostico rapido de um site com reservas para minha pousada',
    img='A Brazilian guesthouse owner at a charming pousada reception showing a booking website on a laptop, tropical garden visible, realistic editorial photo'),
 ]),
]
