<p align="center">
  <img src="Assets/Logos/Logo%20do%20Jogo.png" alt="Logo do jogo Onion" width="220">
</p>

# Onion — Atomicamente Instável

**Onion** é um jogo 2D de ação e sobrevivência com visão superior, feito em Unity. Este repositório reúne o **prólogo jogável**: o personagem começa dentro de casa, prepara seu equipamento e sai para um mapa onde precisa resistir a slimes, coletar suprimentos e sobreviver ao tempo do modo escolhido.

O mundo, os personagens e a ideia de instabilidade atômica ainda estão sendo desenvolvidos. A proposta de história e as missões futuras não são apresentadas aqui como conteúdo já concluído.

## Como funciona uma partida

1. No menu inicial, escolha **Novo Jogo**, uma dificuldade, o nome do personagem e uma das **cinco vagas de save**. O nome da vaga também é o nome mostrado para o jogador.
2. A partida começa na **casa interior**. É possível conversar com a Madrasta e receber uma Espada de Ferro na primeira conversa concluída.
3. Ao sair de casa, começa o desafio no **mapa externo**. O relógio só avança nesse mapa; permanecer na casa não aproxima a vitória.
4. Slimes surgem ao longo da partida. O jogador pode lutar com espada e arco, recolher alimentos e poções, organizar o inventário, equipar armadura e ganhar níveis.
5. Nos modos com limite de tempo, sobreviver até o contador terminar mostra a tela de **Vitória**. Se a vida chegar a zero, aparece **Derrotado**. Renascer inicia a tentativa do começo, mantendo o nome e a dificuldade escolhidos.

| Dificuldade | Objetivo | Relógio |
| --- | --- | --- |
| Fácil | Sobreviver por 3 minutos no mapa externo | Regressivo |
| Médio | Sobreviver por 10 minutos no mapa externo | Regressivo |
| Difícil | Sobreviver por 15 minutos no mapa externo | Regressivo |
| Insano | Resistir até morrer, sem vitória automática | Progressivo |

## Controles

| Entrada | Ação |
| --- | --- |
| **W, A, S, D** | Mover o personagem |
| **Mouse** | Apontar o personagem e as armas |
| **Espaço** | Dash; também avança uma fala quando um diálogo está aberto |
| **Clique esquerdo** | Atacar com a espada equipada |
| **Clique direito** | Atirar com o arco equipado, consumindo uma flecha |
| **E** | Abrir ou fechar o inventário |
| **B** | Alternar a janela da mochila, quando equipada e na aba Inventário |
| **Clique direito em um item de cura no inventário** | Usar uma unidade do item |
| **Tab** | Alternar a seleção entre os dois slots de poder, se ambos estiverem ocupados |
| **F** | Conversar com um NPC próximo e avançar o diálogo |
| **Esc** | Pausar ou retomar a partida; durante um diálogo, fechá-lo |

O ataque com espada exige uma arma primária equipada. O arco usa o botão direito durante a jogabilidade e precisa estar no slot de arma secundária com flechas disponíveis.

## Sistemas presentes no prólogo

### Combate, inimigos e progressão

- **Espadas:** a de Ferro causa 3 de dano base e a de Diamante, 5. Cada nível conquistado acrescenta 1 ao dano da espada.
- **Arco:** causa 4 de dano base por flecha e consome uma unidade de munição por disparo.
- **Slimes:** Fogo e Ghost patrulham, detectam o personagem, perseguem e atacam. Variantes geradas durante a partida podem ser mais fortes ou mais fracas.
- **Vida e defesa:** o personagem começa com 100 de vida e 5 de defesa base. A defesa reduz o dano recebido, com mínimo de 1 por golpe. Capacete, peitoral e botas podem acrescentar defesa.
- **Experiência:** matar slimes e coletar itens concede XP. A primeira subida exige 100 XP; a exigência cresce 50 a cada nível. XP excedente passa para o nível seguinte. Subir de nível também acrescenta 1 à defesa base.
- **Interface:** barras de vida, estamina e XP acompanham o personagem durante a jogabilidade; inimigos mostram a própria barra de vida.

### Inventário, mochila e cura

- O inventário principal tem **40 slots**. Uma mochila equipada oferece **mais 10 slots**.
- Itens comuns empilháveis, como alimentos e flechas, podem chegar a **99 unidades por slot**; equipamentos ocupam um slot por unidade.
- Pão francês recupera **20 PV**, pão de forma **35 PV**, poção de 50 PV recupera **50 PV** e poção de 100 PV recupera **100 PV**. A cura não é gasta se a vida já estiver completa.
- Itens de cura aparecem periodicamente em posições válidas do mapa externo.
- Há slots para arma primária, escudo, arma secundária, dois poderes, mochila, peitoral, capacete e botas.

### NPCs e recompensas

| NPC | Recompensa atual |
| --- | --- |
| Madrasta | Espada de Ferro, uma vez por save |
| Velho | Espada de Diamante, uma vez por save |
| Louco do Arco | Arco na primeira conversa; flechas repostas até 99 após o intervalo de um minuto |
| Mercadora | Interação de diálogo; loja ainda não implementada |

As falas definitivas dos NPCs ainda não foram escritas no projeto. A interface mostra um texto provisório quando uma lista de falas está vazia.

### Save, pausa e apresentação

O jogo oferece cinco vagas e **salvamento automático** quando o progresso muda, além de checkpoints periódicos no mapa externo. Continuar uma vaga restaura cena, posição, atributos, itens, equipamentos, dificuldade e tempo da tentativa. O menu de pausa interrompe a contagem do relógio. A tela inicial alterna imagens de fundo; as telas de pausa, vitória e derrota usam o jogo desfocado ao fundo.

Na abertura de uma build, a marca e a tipografia do projeto aparecem com o splash da Unity. O ícone do aplicativo usa a imagem em `Assets/Logos/Logo do Jogo.png`.

## Estado atual e próximos conteúdos

O repositório contém o prólogo e seus sistemas de sobrevivência, combate, inventário e save. Algumas estruturas foram preparadas para expansão:

- Os dois slots de **poder** distinguem itens de contato e itens absorvíveis. O bloqueio de retirada e a troca de seleção existem, mas as habilidades ativas e as ações do cenário para remover um poder absorvido ainda não estão implementadas.
- A **Mercadora** conversa, mas ainda não há moeda, preços ou compra e venda.
- Missões, história completa, diálogos finais, habilidades dos poderes e efeitos ativos do escudo são ideias para uma próxima etapa.

## Abrir e gerar uma build

1. Instale o **Unity Editor `6000.6.3f1`** pelo Unity Hub.
2. Abra a pasta raiz deste repositório como projeto Unity e aguarde a importação dos assets e dos pacotes.
3. Inicie pela cena `Assets/Scenes/MainMenu.unity`. As cenas do fluxo são `MainMenu`, `CasaInterior` e `SampleScene`, nessa ordem nas configurações de build.
4. Para gerar um executável, selecione **Windows** nos perfis de build da Unity e crie uma nova build. O repositório contém o **projeto-fonte**, não um `.exe` pronto.

## Organização do projeto

| Pasta | Conteúdo principal |
| --- | --- |
| `Assets/Scenes/` | Menu inicial, casa e mapa externo |
| `Assets/Scripts/` | Regras de player, combate, inimigos, itens, UI, NPCs e saves |
| `Assets/Prefabs/` | Modelos reutilizáveis de itens, armas, inimigos e interfaces |
| `Assets/Sprites/` | Arte, tilemaps, personagens, cenários e elementos de interface |
| `Assets/Logos/` | Marca da abertura e ícone do jogo |
| `Packages/` e `ProjectSettings/` | Dependências e configurações do Unity |

O projeto usa **C#**, **Unity Input System**, **Universal Render Pipeline 2D** e **TextMesh Pro**. Os saves são arquivos JSON no diretório que a Unity fornece por `Application.persistentDataPath`.

## Autoria e recursos

Projeto desenvolvido por **Igor Filipi**. O repositório contém recursos de terceiros; a autoria e as condições de uso desses recursos devem ser consultadas nos respectivos pacotes e arquivos de licença. A documentação de design para estudo é mantida localmente e não faz parte deste repositório.
