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
3. A cena Jogo recebe um chao verde temporario, um jogador quadrado, limites, camera e os componentes de movimento/vida/energia.
4. A cena Menu recebe os botoes temporarios **Novo Jogo** e **Sair**.
5. Pressione Play na cena Menu e escolha **Novo Jogo**. Movimente com **WASD** ou as setas.

Se o comando nao aparecer, espere o Unity terminar de compilar os scripts e confira a janela Console.

O quadrado e somente um marcador temporario. Depois sera trocado pelos sprites do personagem.

## Abertura em casa

1. Escolha **Prototipo > Adicionar abertura em casa ao Jogo**.
2. O jogador comeca no ponto inicial da casa e encontra a madrasta perto dele. A conversa e opcional: aproxime-se e aperte **E**, ou simplesmente saia andando. Enquanto o dialogo estiver aberto, o movimento e os ataques ficam pausados. A tecla **E** que fecha a ultima fala nao inicia uma nova conversa no mesmo instante; para falar outra vez, interaja de novo.
3. A madrasta comenta que o dono do ferro-velho estava procurando ajuda. Isso apresenta a primeira missao sem obrigar o jogador a inicia-la naquele momento.

A casa e a personagem ainda sao representadas por marcadores provisórios. O comando prepara a interacao e o ponto inicial; os comodos, a porta e a arte serao ajustados quando formos organizar o mapa.

## Recursos do jogador

`PlayerVitals` comeca com 100 de vida e 100 de energia. A vida e a energia recuperam 1 ponto a cada 12 segundos enquanto estiverem abaixo do maximo. Um item pode chamar `RestoreHealth(50)` ou `RestoreEnergy(50)`. Um ataque especial pode tentar gastar energia com `TrySpendEnergy(custo)`; se faltar energia, o metodo retorna `false` e o ataque nao deve acontecer.

Os ataques atuais e a HUD usam esses metodos. Depois de adicionar o inventario, pao e queijo chamam a recuperacao desses recursos quando usados.

## Corrida, dash e defesa

- Segure **Shift** enquanto anda para correr. A velocidade provisoria e 1,6 vez a velocidade normal.
- Aperte **Espaco** para dar um dash curto. Ele segue a direcao do movimento; sem direcao pressionada, usa a ultima direcao em que o personagem olhou. O dash dura 0,16 segundo e tem recarga de 0,8 segundo. Nao gasta energia.
- Segure **Ctrl** para defender. A defesa reduz pela metade o dano recebido depois das armaduras e suspende seus ataques enquanto estiver ativa. Ela nao gasta energia.
- Conversas e inventario pausam o movimento e a defesa; o dash e cancelado se um deles abrir.

Os valores podem ser ajustados no Inspector dos componentes `PlayerMovement2D` e `PlayerVitals`. Por enquanto, a defesa reduz qualquer dano que passe por `PlayerVitals`, inclusive os projeteis do inimigo atual. Corrida, dash e defesa ainda usam o marcador visual atual; sprites e animacoes serao feitos na etapa de arte.

## Primeira missao: pecas do ferro-velho

1. Espere o Unity compilar e escolha **Prototipo > Adicionar primeira missao ao Jogo**.
2. O comando acrescenta o dono do ferro-velho, duas pecas de metal interativas, dialogo e o objetivo da missao na cena Jogo. Ele salva a cena sem trocar os objetos que ja estao nela.
3. Abra a cena Jogo, pressione Play e use **WASD** ou as setas para chegar perto de um objeto. Aperte **E** para conversar ou recolher as pecas.
4. Converse com o NPC para iniciar a tarefa; recolha as duas pecas e volte a falar com ele para concluir.

Os personagens e pecas ainda usam quadrados coloridos de placeholder. A primeira tarefa cobre diálogo e coleta; a colher, os poderes e a ligação narrativa do combate entram nas etapas seguintes.

## Combate temporario

1. Escolha **Prototipo > Adicionar combate de prototipo ao Jogo**.
2. O comando acrescenta um inimigo de teste que lanca projeteis de fogo, os ataques do jogador e a HUD de vida/energia.
3. Abra `Jogo` e pressione Play. Mire com o cursor; use o botao esquerdo para o ataque basico e o direito para o ataque pesado, que custa 25 de energia.
4. O inimigo tem 60 pontos de vida. Projeteis causam 8 de dano. A HUD mostra a vida e a energia atuais.

Este bloco serve para validar controles e recursos. O inimigo e os projeteis sao placeholders; os ataques de magnetismo e veneno sao demonstrados nas secoes seguintes.

## Colher e magnetismo

1. Escolha **Prototipo > Adicionar colher e poder de magnetismo ao Jogo**.
2. Na cena `Jogo`, aproxime-se do quadrado verde brilhante perto da praca e aperte **E** para pegar a colher. O poder de Contato de magnetismo sera equipado se o espaco de Contato estiver vazio.
3. Com a missao das pecas iniciada, recolha as duas pecas de metal. Depois de adicionar o inventario, elas ocupam espaco nele e tambem podem ser usadas pelo poder.
4. Mire com o cursor e aperte **Q**. O arremesso causa 30 de dano, consome 1 peca do inventario e 15 de energia.

O prototipo separa os espacos de Contato e Absorcao no componente `PlayerPowerLoadout2D`. A troca provisoria de Contato e feita com **Tab**. O bloco de inventario abaixo acrescenta a tela de 40 espacos e liga a coleta de metal ao ataque da colher.

## Emboscada e personagem misterioso

1. Depois de adicionar a primeira missao, o combate de prototipo e a colher, escolha **Prototipo > Adicionar sequencia narrativa da colher ao Jogo**.
2. A sequencia deixa o inimigo de fogo parado ate o jogador pegar a colher. Depois da fala da colher, o inimigo inicia a emboscada.
3. Ataque-o ate chegar a metade da vida. O jogador cai por um instante; o personagem misterioso aparece, derrota o inimigo em silencio e toma a colher. O ataque para nessa parte para a cena nao ser pulada mesmo que um golpe fosse derrubar o inimigo.
4. Volte ao dono do ferro-velho e entregue as duas pecas. O personagem misterioso aparece perto do jogador, explica a colher e o poder de Contato e o convida para ir ate a cidade proxima. Ao terminar a conversa, ele devolve a colher e fica disponivel para uma fala curta.
5. Depois do convite, caminhe ate o marcador do rato perto da borda do mapa. Ele so aparece quando voce se aproxima. Derrote-o e recolha o queijo; comer o queijo pelo inventario recupera 50 pontos de energia.

A devolucao da colher e uma decisao provisoria de implementacao para que o poder continue disponivel depois da cena. O personagem e a queda usam efeitos e quadrados temporarios; a animacao final sera feita na etapa de sprites.

Se a sequencia narrativa ja estava na cena antes de o rato ser ligado a ela, execute esse mesmo comando novamente. O Editor atualiza a referencia existente e deixa o rato oculto ate o encontro.

Para liberar o caminho apos o rato, escolha **Prototipo > Adicionar saida continua da cidade ao Jogo**. Isso abre a passagem leste, move o rato para o vao e acrescenta um trecho de chao e estrada provisoria sem mover o chao original do ponto inicial. Depois da conversa com o desconhecido, o objetivo aponta para a saida; derrotar o rato libera a passagem. Ao atravessar, o prologo fica marcado como concluido. Tudo continua na cena `Jogo`, sem loading ou teleporte. A estrada e um placeholder a ser redesenhado quando organizarmos o mapa. Se ja aplicou uma versao anterior desse comando, execute-o novamente para atualizar o chao.

## Poder de Absorcao: maca venenosa

1. Escolha **Prototipo > Adicionar maca e remocao de poder ao Jogo**.
2. A maca vermelha e um placeholder no canto sul do chao atual. Aproxime-se e aperte **E** para come-la.
3. Com o poder ativo, o botao direito lanca uma bolha verde. Cada acerto causa 6 de dano direto e mais 4 de dano por segundo durante tres segundos. O ataque custa 20 de energia e substitui o ataque pesado enquanto a maca estiver ativa.
4. Volte ao vaso azul perto do ponto inicial e aperte **E**. Ele remove o poder de veneno; o botao direito volta a ser o ataque pesado.

O estado dos poderes ainda existe apenas durante a partida atual. Salvamento persistente entre sessoes ainda nao foi implementado.

## Luva vermelha e troca de poderes de Contato

1. Escolha **Prototipo > Adicionar luva vermelha ao Jogo**.
2. A luva vermelha e um quadrado no lado sudoeste do mapa provisorio. Aperte **E** perto dela para adquiri-la.
3. Aperte **Tab** para alternar entre os poderes de Contato adquiridos e nenhum. A HUD mostra qual esta ativo. Se voce ja pegou a colher, um Tab troca da colher para a luva.
4. Com a luva ativa, os ataques basicos causam 50% mais dano. **F** usa o soco forte (15 de energia); **G** causa dano em area (25 de energia). O botao direito continua sendo o ataque pesado, tambem fortalecido.

Os quadrados nao representam ainda a casa vizinha ou o desenho final da luva. A implementacao demonstra os efeitos e a troca de equipamento no prototipo.

## Inventario, itens e rato mutante

1. Escolha **Prototipo > Adicionar inventario e itens ao Jogo**. O comando acrescenta o inventario de 40 espacos, a carteira de moedas, itens de exemplo e um rato mutante. Se esse bloco ja estiver na cena, o comando avisa e nao duplica os objetos. Com a sequencia narrativa da colher configurada, o rato fica oculto ate o jogador se aproximar depois do convite do desconhecido.
2. Aperte **I** para abrir ou fechar o inventario. Com ele aberto, o jogador para e nao ataca nem interage com o mundo. Aperte **Esc** para fechar.
3. Chegue perto de um item e aperte **E** para pega-lo. Os metais da primeira missao tambem entram no inventario e sao usados pelo poder de magnetismo. Se nao houver espaco, a peca permanece no chao e a missao nao avanca.
4. Clique em um espaco para seleciona-lo. Clique em **Usar** ou aperte **Enter** para consumir pao/queijo. Pao recupera 50 de vida e queijo recupera 50 de energia; o item nao e consumido se o recurso correspondente ja estiver cheio. Para equipamentos, use o botao **Equipar** ou **Enter**.
5. Abra o bau para receber 5 moedas. As moedas aparecem na carteira do inventario, separadas dos 40 espacos.
6. No fluxo do prologo, o rato aparece perto da saida depois do convite do desconhecido. Derrote-o para receber um queijo. Ele dispara bolas amarelas que causam dano como o ataque a distancia provisoriamente configurado.

O limite de empilhamento atual e 99 por espaco para gravetos, metal, pao, queijo e flechas; esse valor e ajustavel no Inspector do `PlayerInventory2D` e e provisório ate confirmacao das regras finais do GDD. A espada e as armaduras podem ser equipadas conforme a proxima secao. As posicoes e aparencias na cena sao placeholders.

## Experiencia e niveis

1. Escolha **Prototipo > Adicionar experiencia e niveis ao Jogo**. O comando liga a progressao ao jogador e a HUD existente, e prepara a recompensa de XP do rato.
2. A HUD mostra o nivel e a barra de experiencia. Ao subir de nivel, aparece por tres segundos o aviso **Novo nivel!**. O limite e nivel 200.
3. No balanceamento provisório, a primeira subida exige 100 XP e cada nivel seguinte exige 25 XP a mais que o anterior.
4. Coletar um item comum concede 5 XP; abrir o bau de moedas, 15; adquirir um poder, 10; cada peca de metal, 10; concluir a missao do ferro-velho, 50; receber a espada, 10; concluir a missao do arco, 40; derrotar o rato, 50.

Os valores e a curva podem ser ajustados no Inspector e revisados com o GDD. Os pontos de status e os efeitos de cada nivel ainda nao estao definidos; os bonus atuais de equipamento continuam funcionando separadamente. A progressao ainda nao e salva entre partidas.

## Espada e armaduras

1. Escolha **Prototipo > Adicionar equipamentos ao Jogo** depois de adicionar o inventario.
2. Conclua a tarefa das duas pecas e fale com o dono do ferro-velho novamente. Ele entrega uma espada de metal comum uma vez na partida atual, desde que haja espaco livre. Salvamento entre sessoes ainda nao foi implementado.
3. Recolha o peitoral em **X 8, Y 2**, o capacete em **X -9, Y 6.8** e as botas em **X -10, Y -6.5**. Abra o inventario com **I**, selecione uma dessas pecas e clique em **Equipar** ou aperte **Enter**.
4. Os botoes de equipamento no topo do inventario mostram o que esta vestido. Clique em um deles para devolver aquela peca ao inventario; precisa haver espaco livre.
5. A espada aumenta em 50% o dano dos ataques basicos e pesados. O peitoral reduz 3 de dano recebido, o capacete 2 e as botas 1. Os valores podem ser ajustados no Inspector de `PlayerEquipment2D`.

Esses numeros sao valores temporarios de balanceamento, pois ainda nao foram definidos nas regras confirmadas ate agora; devem ser revistos com o GDD. A espada usa os ataques e a animacao provisoria existentes; nao ha sprite ou animacao de espada nesta etapa.

## Missao secundaria do estranho e arco

1. Escolha **Prototipo > Adicionar missao secundaria do arco ao Jogo** depois de adicionar inventario e equipamentos.
2. Encontre o estranho perto da saida leste, em **X 10.4, Y 0.5**, e fale com ele usando **E**. O objetivo provisório e entregar 2 gravetos; se ja os tiver, fale com ele novamente para entregar.
3. A missao consome os dois gravetos e entrega um arco de madeira e 10 flechas. Se nao houver espaco no inventario para a recompensa, os gravetos ficam com o jogador e o NPC pede para liberar espaco.
4. Abra o inventario, selecione o arco e equipe-o no espaco de arma. Com o arco equipado, o botao esquerdo dispara uma flecha e consome uma municao. O dano provisório e 18 e o intervalo entre tiros e 0,45 segundo; o botao direito continua com o ataque pesado atual.

O objetivo de 2 gravetos foi escolhido provisoriamente para demonstrar a missao. O requisito pode ser alterado no componente `PrologueSideQuest2D`. O arco e as flechas ainda usam marcadores visuais, sem animacao de puxar corda.
