<p align="center">
  <img src="Assets/Logos/Logo%20do%20Jogo.png" alt="Logo do jogo Onion" width="240">
</p>

# Onion — Atomicamente Instável

**Onion** é um prólogo jogável de ação e sobrevivência 2D, com visão superior, criado em Unity. O personagem começa dentro de casa, recebe equipamentos dos NPCs e entra em um mapa tomado por slimes. O objetivo é sobreviver ao tempo da dificuldade escolhida, explorando o mapa, lutando, coletando recursos e evoluindo o personagem.

## Começar uma partida

1. No menu, escolha **Novo Jogo**, a dificuldade, o nome do personagem e uma das **cinco vagas**.
2. Converse com a Madrasta na casa para receber a Espada de Ferro. O relógio ainda não avança dentro da casa.
3. Atravesse a porta para chegar ao mapa externo. Ali começam o tempo de sobrevivência, os inimigos e os itens que aparecem aleatoriamente.
4. Fale com o Velho para obter a Espada de Diamante, com o Louco do Arco para obter arco e flechas, e com a Mercadora para receber a armadura. Essas recompensas são dadas uma vez por tentativa; o Louco do Arco pode repor flechas até 99 depois de um minuto.
5. Nos modos Fácil, Médio e Difícil, sobreviva até o relógio terminar. No Insano, o tempo é ilimitado e o objetivo é durar o máximo possível.

| Modo | Tempo no mapa | Vida e dano dos inimigos | Intervalo de itens consumíveis |
| --- | ---: | ---: | ---: |
| Fácil | 3 minutos | Base atual | 25 s |
| Médio | 10 minutos | +5% sobre a base | 32,5 s |
| Difícil | 15 minutos | +10% sobre a base | 40 s |
| Insano | Sem limite | +15% sobre a base | 50 s |

Os slimes comuns já têm uma base de vida e dano 15% maior que a dos seus prefabs; os chefes usam três vezes a base dos respectivos prefabs. Conforme o tempo passa, mais slimes aparecem e cresce a chance de nascer uma variante forte. A cada dez níveis, os inimigos também melhoram dano, velocidade e perseguição. Um chefe surge perto do jogador a cada cinco níveis.

## Controles

| Entrada | Ação |
| --- | --- |
| **W, A, S, D** | Mover |
| **Mouse** | Mirar |
| **Espaço** | Dash, gastando 10 de estamina; em diálogo, avançar a fala |
| **Clique esquerdo** | Atacar com a espada equipada |
| **Clique direito** | Atirar com o arco equipado, gastando uma flecha |
| **E** | Abrir ou fechar o inventário |
| **B** | Abrir ou fechar a mochila equipada, na aba Inventário |
| **F** | Conversar com um NPC próximo |
| **Tab** | Alternar entre os dois slots de poder ocupados |
| **1 a 0** | Selecionar um dos dez espaços da barra rápida |
| **Duplo clique em item da barra rápida** | Usar um consumível |
| **Clique direito em consumível no inventário** | Usar uma unidade do item |
| **Esc** | Pausar ou retomar; também fecha diálogos |

A espada precisa estar no slot **Arma Primária**. Para disparar, equipe o arco em **Arma Secundária** e tenha flechas no inventário ou na barra rápida.

## Sistemas do prólogo

- **Combate:** a Espada de Ferro causa 5 de dano base; a de Diamante, 10, com chance de crítico de 15. O arco causa 7 de dano base. O dano das armas cresce com o nível. Slimes de Fogo e Ghost perseguem, atacam e alguns disparam projéteis com fogo ou veneno.
- **Progressão:** o personagem começa com 100 PV, 100 de estamina e 5 de defesa base. Cada nível acrescenta 25 ao máximo de vida e estamina, além de 1 à defesa base e ao dano das armas. Cada peça de armadura equipada ganha defesa adicional a cada cinco níveis.
- **Recursos:** itens coletados concedem XP. Pães e poções recuperam vida; poções de estamina recuperam estamina. A Poção de Queimadura protege do fogo por 20 segundos e a Maçã Envenenada protege do veneno por 20 segundos. Açúcar aumenta a velocidade temporariamente.
- **Inventário:** são 40 espaços principais, mais 10 ao equipar a mochila. Itens empilháveis chegam a 99 unidades por espaço; equipamentos ocupam um espaço por unidade. A barra rápida mantém dez espaços visíveis durante a partida.
- **Conquistas e histórico:** o jogo registra moedas, abates, recordes e conquistas entre tentativas. O Histórico pode ser aberto no menu inicial e na pausa.
- **Save:** existem cinco vagas. O progresso é salvo automaticamente quando muda e em intervalos durante a partida, além de ser gravado ao sair. Uma tentativa encerrada por morte ou vitória não pode ser continuada; a vaga pode ser usada para uma nova partida. O histórico global e as conquistas permanecem.

Os slots de **Poder 1** e **Poder 2** já distinguem itens de contato e absorvíveis. As habilidades ativas desses poderes e as ações do cenário que removeriam um poder absorvido ainda são ideias para uma continuação. A Mercadora entrega armadura; uma loja com preços e compra e venda ainda não foi implementada.

## Abrir no Unity e criar o EXE

1. Abra este projeto no **Unity 6000.6.3f1**.
2. Confira as cenas incluídas na build: `MainMenu`, `CasaInterior` e `SampleScene`, nessa ordem.
3. Escolha a plataforma **Windows 64-bit** e gere o executável em uma pasta de saída.
4. Para jogar ou entregar a build, mantenha **o EXE e toda a pasta de dados gerada ao lado dele**. Enviar só o EXE não inclui os recursos do jogo.

Os elementos de interface podem ser ajustados diretamente na Hierarchy, dentro de **Gameplay UI 1** em cada cena. Na cena externa, o componente **SurvivalSpawner** expõe no Inspector as frequências de nascimento e os multiplicadores de dificuldade. Os valores de itens ficam nos respectivos prefabs.

## Organização

| Pasta | Conteúdo |
| --- | --- |
| `Assets/Scenes/` | Menu inicial, interior da casa e mapa externo |
| `Assets/Scripts/` | Regras de combate, inimigos, itens, UI, NPCs e salvamento |
| `Assets/Prefabs/` | Modelos de itens, armas, inimigos e interface |
| `Assets/Sprites/` e `Assets/Logos/` | Arte do jogo, marca e ícone |
| `Documents/` | GDD, documento técnico e figuras publicados |
| `Packages/` e `ProjectSettings/` | Pacotes e configuração do Unity |

O projeto usa C#, Unity Input System, Universal Render Pipeline 2D e TextMesh Pro. Os saves ficam no diretório que o Unity fornece por `Application.persistentDataPath`.

## Documentação

- [GDD — Game Design Document, v1.1](Documents/v1.1%20-%20GDD%20-%20Game%20Design%20Document%20-%2008.10.2026.pdf)
- [Documento técnico, v1.1](Documents/v1.1%20-%20TD%20-%20Technical%20Document%20-%2008.10.2026.docx)

## Autoria

Projeto desenvolvido por **Igor Filipi**, **Heloisa Silva**, **Isaac Silva** e **Amanda Lima**. Os recursos de terceiros mantêm as condições de uso dos respectivos autores e pacotes.
