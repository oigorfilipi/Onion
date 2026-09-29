# Primeiros passos do prototipo

## Projeto

- Unity Editor: `6000.3.24f1`
- Template: 2D
- Cenas do prototipo: `Assets/Scenes/Menu.unity` e `Assets/Scenes/Jogo.unity`
- A `SampleScene.unity` veio com o template e nao faz parte do fluxo do prototipo.

## Abrir no Unity Hub

1. Abra o Unity Hub.
2. Escolha **Add** / **Adicionar projeto do disco**.
3. Selecione a pasta `ProjetoUnity`.
4. Abra o projeto com o Editor `6000.3.24f1` e espere a importacao inicial terminar.

## Cenas iniciais

1. No menu superior do Editor, escolha **Prototipo > Criar cenas iniciais**.
2. O comando cria `Assets/Scenes/Menu.unity` e `Assets/Scenes/Jogo.unity`, e adiciona as duas ao Build Settings.
3. A cena Jogo recebe um chao verde temporario, um jogador quadrado, limites, camera e os componentes de movimento, vida e estamina.
4. A cena Menu recebe os botoes temporarios **Novo Jogo** e **Sair**.
5. Pressione Play na cena Menu e escolha **Novo Jogo**. Movimente com **WASD** ou as setas.

Se o comando nao aparecer, espere o Unity terminar de compilar os scripts e confira a janela Console.

O quadrado e somente um marcador temporario. Depois sera trocado pelos sprites do personagem.

## Abertura em casa

1. Escolha **Prototipo > Adicionar abertura em casa ao Jogo**.
2. O jogador comeca no ponto inicial da casa e encontra a madrasta perto dele. A conversa e opcional: aproxime-se e aperte **F**, ou simplesmente saia andando. Enquanto o dialogo estiver aberto, o movimento e os ataques ficam pausados. Clique com o botao esquerdo para avancar as falas; depois do dialogo, aperte **F** para iniciar outra conversa.
3. A madrasta comenta que o dono do ferro-velho estava procurando ajuda. Isso apresenta a primeira missao sem obrigar o jogador a inicia-la naquele momento.

A casa e a personagem ainda sao representadas por marcadores provisórios. O comando prepara a interacao e o ponto inicial; os comodos, a porta e a arte serao ajustados quando formos organizar o mapa.

## Recursos do jogador

`PlayerVitals` comeca com 100 de vida e 100 de estamina. A vida e a estamina recuperam 1 ponto a cada 12 segundos enquanto estiverem abaixo do maximo. Um item pode chamar `RestoreHealth(50)` ou `RestoreEnergy(50)`. Um ataque especial pode tentar gastar estamina com `TrySpendEnergy(custo)`; se faltar estamina, o metodo retorna `false` e o ataque nao deve acontecer. Os nomes `Energy` ainda aparecem na API do codigo, mas representam a estamina do personagem.

Os ataques atuais e a HUD usam esses metodos. Depois de adicionar o inventario, pao e queijo chamam a recuperacao desses recursos quando usados.

## Controles atuais

- **WASD/setas:** mover; **Ctrl:** correr; **Espaco:** dash.
- **F:** iniciar dialogo, interagir ou recolher um item; **E:** abrir/fechar inventario.
- **B:** abrir o inventario diretamente quando a mochila estiver equipada.
- **I:** abrir o menu de gameplay no status do personagem; **Esc:** fechar o menu aberto ou abrir a pausa.
- **Clique esquerdo:** ataque leve ao soltar rapidamente; segurar por pelo menos 0,22 segundo carrega o ataque pesado.
- **Clique direito:** usar a arma secundaria ao soltar rapidamente; segurar por pelo menos 0,22 segundo bloqueia com espada ou escudo equipado.
- **Tab:** trocar as armas primaria e secundaria quando as duas posicoes estiverem ocupadas.
- **Z/X:** habilidades do poder de Contato (colher em Z; luva em Z e X); **C:** veneno da maca; **V:** espaco reservado para uma habilidade ainda nao implementada.
- **M/N/H/K:** abrir diretamente mapa, missoes, melhorias ou arvore de habilidades no menu de gameplay. Mapa, Skills, arvore e melhorias sao demonstracoes nesta etapa.

## Corrida, dash e defesa

- Segure **Ctrl** enquanto anda para correr. A velocidade provisoria e 1,6 vez a velocidade normal; correr nao consome estamina.
- Aperte **Espaco** para dar um dash curto. Ele segue a direcao do movimento; sem direcao pressionada, usa a ultima direcao em que o personagem olhou. O dash dura 0,16 segundo e tem recarga de 0,8 segundo. Nao gasta estamina.
- Segure o **botao direito do mouse** para bloquear. Com espada e sem escudo, cada golpe bloqueado consome 15 de estamina e reduz o dano em 15%. Com escudo, o bloqueio sempre usa o escudo: consome 10 de estamina e reduz o dano em 50%. Sem espada ou escudo, nao ha bloqueio.
- O menu de gameplay pausa o jogo. Conversas e inventario bloqueiam movimento e combate; o dash e cancelado se um deles abrir.

Os valores podem ser ajustados no Inspector dos componentes `PlayerMovement2D`, `PlayerVitals` e `PlayerEquipment2D`. O bloqueio e processado em cada chamada de dano feita por `PlayerVitals`. Corrida, dash e defesa ainda usam o marcador visual atual; sprites e animacoes serao feitos na etapa de arte.

## Primeira missao: pecas do ferro-velho

1. Espere o Unity compilar e escolha **Prototipo > Adicionar primeira missao ao Jogo**.
2. O comando acrescenta o dono do ferro-velho, duas pecas de metal interativas, dialogo e o objetivo da missao na cena Jogo. Ele salva a cena sem trocar os objetos que ja estao nela.
3. Abra a cena Jogo, pressione Play e use **WASD** ou as setas para chegar perto de um objeto. Aperte **F** para conversar ou recolher as pecas.
4. Converse com o NPC para iniciar a tarefa; recolha as duas pecas e volte a falar com ele para concluir.

Os personagens e pecas ainda usam quadrados coloridos de placeholder. A primeira tarefa cobre diálogo e coleta; a colher, os poderes e a ligação narrativa do combate entram nas etapas seguintes.

## Combate temporario

1. Escolha **Prototipo > Adicionar combate de prototipo ao Jogo**.
2. O comando acrescenta um inimigo de teste que lanca projeteis de fogo, os ataques do jogador e a HUD de vida/estamina.
3. Abra `Jogo` e pressione Play. Mire com o cursor; clique rapidamente com o botao esquerdo para o ataque leve ou segure-o para carregar o ataque pesado, que custa 25 de estamina. O botao direito fica reservado para a arma secundaria e o bloqueio.
4. O inimigo tem 60 pontos de vida. Projeteis causam 8 de dano. A HUD mostra a vida e a estamina atuais.

Este bloco serve para validar controles e recursos. O inimigo e os projeteis sao placeholders; os ataques de magnetismo e veneno sao demonstrados nas secoes seguintes.

## Colher e magnetismo

1. Escolha **Prototipo > Adicionar colher e poder de magnetismo ao Jogo**.
2. Na cena `Jogo`, aproxime-se do quadrado verde brilhante perto da praca e aperte **F** para pegar a colher. O poder de Contato de magnetismo sera equipado se o espaco de Contato estiver vazio.
3. Com a missao das pecas iniciada, recolha as duas pecas de metal. Depois de adicionar o inventario, elas ocupam espaco nele e tambem podem ser usadas pelo poder.
4. Mire com o cursor e aperte **Z**. O arremesso causa 30 de dano, consome 1 peca do inventario e 15 de estamina.

O prototipo separa os espacos de Contato e Absorcao no componente `PlayerPowerLoadout2D`. Depois de obter mais de um poder de Contato, troque-o pelo botao **Contato** no inventario. **Tab** agora troca as armas primaria e secundaria.

## Emboscada e personagem misterioso

1. Depois de adicionar a primeira missao, o combate de prototipo e a colher, escolha **Prototipo > Adicionar sequencia narrativa da colher ao Jogo**.
2. A sequencia deixa o inimigo de fogo parado ate o jogador pegar a colher. Depois da fala da colher, o inimigo inicia a emboscada.
3. Com o chefe configurado, ataque-o ate chegar a **300/500 de vida (60%)**. O jogador cai por um instante; o personagem misterioso aparece, derrota o inimigo em silencio e toma a colher. O dano para nesse ponto para a cena nao ser pulada mesmo que um golpe fosse derrubar o inimigo.
4. Volte ao dono do ferro-velho e entregue as duas pecas. O personagem misterioso aparece perto do jogador, explica a colher e o poder de Contato e o convida para ir ate a cidade proxima. Ao terminar a conversa, ele devolve a colher e fica disponivel para uma fala curta.
5. Depois do convite, caminhe ate o marcador do rato perto da borda do mapa. Ele so aparece quando voce se aproxima. Derrote-o e recolha o queijo; comer o queijo pelo inventario recupera 50 pontos de estamina.

A devolucao da colher e uma decisao provisoria de implementacao para que o poder continue disponivel depois da cena. O personagem e a queda usam efeitos e quadrados temporarios; a animacao final sera feita na etapa de sprites.

## Chefe de teste e queimadura

1. Depois de configurar a sequencia narrativa da colher, escolha **Prototipo > Configurar chefe do prologo**. Execute o comando novamente para atualizar o chefe com os ataques em padrao e a regeneracao lenta; ele atualiza o inimigo existente sem criar outro.
2. O inimigo comeca com **500 de vida** e uma barra de chefe no alto da tela, identificada como **Inimigo de fogo**. Ao chegar a **300 de vida (60%)**, o dano para e a sequencia da queda e do resgate continua. A investida fica visivel quando o quadrado do chefe aumenta antes do avanco.
3. O padrao alterna uma rajada de ate duas bolas de fogo, uma pausa, a janela da investida e um breve periodo de recuperacao. As bolas causam **12 de dano direto**, tem alcance maximo de **9 unidades**, miram com pequena previsao do movimento do jogador e param ao tocar um collider. Um acerto aplica queimadura de **2 de dano por segundo durante 3 segundos**; a HUD exibe **Queimando!**.
4. Quando o jogador esta ate **4,8 unidades** do chefe, a investida prepara por **0,35 segundo** e avanca. O soco causa **25 de dano**. A janela da investida e controlada pelo padrao, que alterna com os tiros e a recuperacao.
5. Se o chefe ficar sem receber dano por **6 segundos**, recupera **1 ponto de vida** e continua regenerando 1 ponto a cada **20 segundos** enquanto puder lutar. Essa taxa e mais lenta que a regeneracao do protagonista. A regeneracao para quando o chefe chega ao limite narrativo de 300 e a cena de resgate bloqueia o dano.
6. Esses valores sao de demonstracao e podem ser ajustados nos componentes `BossCombatPattern2D`, `EnemyDashPunch2D`, `EnemyHealth2D`, `PlayerVitals` e `EnemyFireShooter2D`.

Se a sequencia narrativa ja estava na cena antes de o rato ser ligado a ela, execute esse mesmo comando novamente. O Editor atualiza a referencia existente e deixa o rato oculto ate o encontro.

Para liberar o caminho apos o rato, escolha **Prototipo > Adicionar saida continua da cidade ao Jogo**. Isso abre a passagem leste, move o rato para o vao e acrescenta um trecho de chao e estrada provisoria sem mover o chao original do ponto inicial. Depois da conversa com o desconhecido, o objetivo aponta para a saida; derrotar o rato libera a passagem. Ao atravessar, o prologo fica marcado como concluido. Tudo continua na cena `Jogo`, sem loading ou teleporte. A estrada e um placeholder a ser redesenhado quando organizarmos o mapa. Se ja aplicou uma versao anterior desse comando, execute-o novamente para atualizar o chao.

## Poder de Absorcao: maca venenosa

1. Escolha **Prototipo > Adicionar maca e remocao de poder ao Jogo**.
2. A maca vermelha e um placeholder no canto sul do chao atual. Aproxime-se e aperte **F** para come-la.
3. Com o poder ativo, aperte **C** para lancar uma bolha verde. Cada acerto causa 6 de dano direto e mais 4 de dano por segundo durante tres segundos. O ataque custa 20 de estamina.
4. Volte ao vaso azul perto do ponto inicial e aperte **F**. Ele remove o poder de veneno.

O estado dos poderes ainda existe apenas durante a partida atual. Salvamento persistente entre sessoes ainda nao foi implementado.

## Luva vermelha e troca de poderes de Contato

1. Escolha **Prototipo > Adicionar luva vermelha ao Jogo**.
2. A luva vermelha e um quadrado no lado sudoeste do mapa provisorio. Aperte **F** perto dela para adquiri-la.
3. Para alternar entre poderes de Contato adquiridos, abra o inventario e clique no botao **Contato**. A HUD mostra qual esta ativo.
4. Com a luva ativa, os ataques basicos causam 50% mais dano. **Z** usa o soco forte (15 de estamina); **X** causa dano em area (25 de estamina). O ataque pesado carregado tambem recebe o bonus da luva.

Os quadrados nao representam ainda a casa vizinha ou o desenho final da luva. A implementacao demonstra os efeitos e a troca de equipamento no prototipo.

## Inventario, itens e rato mutante

1. Escolha **Prototipo > Adicionar inventario e itens ao Jogo**. O comando acrescenta o inventario de 40 espacos, a carteira de moedas, itens de exemplo e um rato mutante. Se esse bloco ja estiver na cena, o comando avisa e nao duplica os objetos. Com a sequencia narrativa da colher configurada, o rato fica oculto ate o jogador se aproximar depois do convite do desconhecido.
2. Aperte **E** para abrir ou fechar o inventario. Com ele aberto, o jogador para e nao ataca nem interage com o mundo. Aperte **Esc** para fechar.
3. Chegue perto de um item e aperte **F** para pega-lo. Os metais da primeira missao tambem entram no inventario e sao usados pelo poder de magnetismo. Se nao houver espaco, a peca permanece no chao e a missao nao avanca.
4. Clique em um espaco para seleciona-lo. Clique em **Usar** ou aperte **Enter** para consumir pao/queijo. Pao recupera 50 de vida e queijo recupera 50 de estamina; o item nao e consumido se o recurso correspondente ja estiver cheio. Para equipamentos, use o botao **Equipar** ou **Enter**.
5. Abra o bau para receber 5 moedas. As moedas aparecem na carteira do inventario, separadas dos espacos. Sem mochila, a tela exibe 40 espacos em quatro linhas de dez; com mochila, exibe 60 em seis linhas de dez.
6. No fluxo do prologo, o rato aparece perto da saida depois do convite do desconhecido. Derrote-o para receber um queijo. Ele dispara bolas amarelas que causam dano como o ataque a distancia provisoriamente configurado.

O limite de empilhamento atual e 99 por espaco para gravetos, metal, pao, queijo e flechas; esse valor e ajustavel no Inspector do `PlayerInventory2D` e e provisório ate confirmacao das regras finais do GDD. A espada e as armaduras podem ser equipadas conforme a proxima secao. As posicoes e aparencias na cena sao placeholders.

## Experiencia e niveis

1. Escolha **Prototipo > Adicionar experiencia e niveis ao Jogo**. O comando liga a progressao ao jogador e a HUD existente, e prepara a recompensa de XP do rato.
2. A HUD mostra o nivel e a barra de experiencia. Ao subir de nivel, aparece por tres segundos o aviso **Novo nivel!**. O limite e nivel 200.
3. No balanceamento provisório, a primeira subida exige 100 XP e cada nivel seguinte exige 25 XP a mais que o anterior.
4. Coletar um item comum concede 5 XP; abrir o bau de moedas, 15; adquirir um poder, 10; cada peca de metal, 10; concluir a missao do ferro-velho, 50; receber a espada, 10; concluir a missao do arco, 40; derrotar o rato, 50.

Os valores e a curva podem ser ajustados no Inspector e revisados com o GDD. Os pontos de status e os efeitos de cada nivel ainda nao estao definidos; os bonus atuais de equipamento continuam funcionando separadamente. A progressao ainda nao e salva entre partidas.

## Espada, armaduras, escudo e mochila

1. Escolha **Prototipo > Adicionar equipamentos ao Jogo** depois de adicionar o inventario. Se ja usou o comando antes, execute-o novamente: ele acrescenta escudo e mochila sem duplicar os itens existentes.
2. Conclua a tarefa das duas pecas e fale com o dono do ferro-velho novamente. Ele entrega uma espada de metal comum uma vez na partida atual, desde que haja espaco livre. Salvamento entre sessoes ainda nao foi implementado.
3. Recolha o peitoral em **X 8, Y 2**, o capacete em **X -9, Y 6.8**, as botas em **X -10, Y -6.5**, o escudo em **X 8, Y -4.5** e a mochila em **X -4, Y -6.5**. Abra o inventario com **E**, selecione uma peca e clique em **Equipar** ou aperte **Enter**.
4. Os oito espacos de equipamento ficam na area esquerda do inventario, em duas colunas alinhadas as quatro primeiras linhas: duas armas, escudo/capacete, peitoral/botas e Contato/mochila. Clique num equipamento vestido para devolve-lo ao inventario. Para retirar a mochila, esvazie os 20 espacos extras e deixe um espaco livre nos 40 principais.
5. A espada aumenta em 50% o dano dos ataques basicos e pesados. O peitoral reduz 3 de dano recebido, o capacete 2 e as botas 1. O escudo tem prioridade sobre o bloqueio com espada.
6. A mochila aumenta o inventario de 40 para 60 espacos. Aperte **B** para abrir o inventario quando ela estiver equipada.

Esses numeros sao valores temporarios de balanceamento, pois ainda nao foram definidos nas regras confirmadas ate agora; devem ser revistos com o GDD. A espada usa os ataques e a animacao provisoria existentes; nao ha sprite ou animacao de espada nesta etapa.

## Missao secundaria do estranho e arco

1. Escolha **Prototipo > Adicionar missao secundaria do arco ao Jogo** depois de adicionar inventario e equipamentos.
2. Encontre o estranho perto da saida leste, em **X 10.4, Y 0.5**, e fale com ele usando **F**. O objetivo provisório e entregar 2 gravetos; se ja os tiver, fale com ele novamente para entregar.
3. A missao consome os dois gravetos e entrega um arco de madeira e 10 flechas. Se nao houver espaco no inventario para a recompensa, os gravetos ficam com o jogador e o NPC pede para liberar espaco.
4. Abra o inventario, selecione o arco e equipe-o como arma primaria ou secundaria. Com o arco primario, um clique esquerdo dispara uma flecha e segurar o botao carrega um disparo mais forte; como arma secundaria, clique com o botao direito para disparar. Cada disparo consome uma municao.

O objetivo de 2 gravetos foi escolhido provisoriamente para demonstrar a missao. O requisito pode ser alterado no componente `PrologueSideQuest2D`. O arco e as flechas ainda usam marcadores visuais, sem animacao de puxar corda.

## IA de inimigos comuns

1. No Unity, aguarde a compilacao dos scripts e escolha **Prototipo > Adicionar inimigo de teste com IA**. O comando pode ser executado novamente sem criar outro inimigo; ele atualiza a demonstracao existente.
2. Abra a cena `Jogo` e procure o quadrado azul na estrada leste, em **X 21, Y -5**. Esse inimigo foi separado do rato mutante da missao, que continua fixo junto ao portao, e do chefe de fogo do prologo.
3. Aperte Play e aproxime-se a ate **7 unidades** para chamar a atencao do inimigo. Ele se aproxima, tenta manter distancia para atirar bolas de energia e usa um ataque corpo a corpo de **10 de dano** a cada **1,4 segundo** se voce chegar muito perto. O tiro causa **7 de dano**, tem alcance de **6,8 unidades** e o projetil para depois de **8 unidades**.
4. Afaste-se para alem de **8 unidades do ponto onde o inimigo surgiu**. Ele para a perseguicao, volta para a area inicial e patrulha em um raio de **1,4 unidade**. Ele nao segue o jogador pelo mapa inteiro.
5. Ao ficar com **25% de vida ou menos**, ele tenta se afastar. Se ele parar de receber dano, a vida comum volta completamente **10 segundos depois do ultimo golpe**. O chefe do prologo nao usa essa regeneracao automatica.

Os estilos **Ranged**, **Melee** e **Hybrid** podem ser escolhidos no componente `EnemyBehavior2D`; a demonstracao usa **Hybrid**. O campo `missionEnemy` permite que um inimigo de encontro roteirizado perceba o jogador dentro do limite de perseguicao. Nao aplique esse componente ao rato ou ao chefe nesta etapa.

## Mercearia e compra de pao

1. Aguarde a compilacao e escolha **Prototipo > Configurar compra de pao na mercearia**. O comando converte o antigo marcador de pao no vendedor existente; se o objeto ainda nao existir, cria um placeholder na posicao **X -8, Y 5.5**.
2. Aproxime-se e aperte **F**. Cada interacao compra **1 pao por 3 moedas**. O dialogo informa o resultado e o saldo, e o vendedor continua no mapa para novas compras.
3. A compra so acontece se houver pelo menos 3 moedas e espaco no inventario. O bau de exemplo entrega 5 moedas, entao permite comprar um pao e deixa 2 moedas.
4. Abra o inventario com **E**, selecione o pao e clique em **Usar** ou aperte **Enter**. Ele restaura ate **50 de vida** e so e consumido se a vida estiver abaixo do maximo.

O preco de 3 moedas e provisório para demonstrar a compra. O comando pode ser executado novamente sem criar outro vendedor.
