# Projeto de jogo 2D

Protótipo de ação e RPG totalmente em 2D, com visão top-down, exploração em mundo aberto, combate e progressão de personagem. O projeto é desenvolvido com Unity 2D e C#.

## Conceito de jogo

Objetos podem conceder poderes ao jogador de duas formas:

- **Contato:** o objeto fica equipado e concede seus efeitos e habilidades enquanto estiver ativo. O jogador pode desequipá-lo para trocar de poder.
- **Absorção:** o jogador consome ou absorve o objeto e mantém o poder até realizar uma ação específica para removê-lo.

O jogador pode manter um poder de Contato e um poder de Absorção ao mesmo tempo. Cada poder pode oferecer até quatro habilidades ativas, associadas a espaços de habilidade.

## Sistemas e mecânicas

- **Exploração e missões:** mundo aberto, personagens interativos, objetivos principais e secundários, coleta de itens, moedas, compras e recompensas.
- **Combate:** ataques leves e pesados, habilidades de poderes, armas de combate próximo e à distância, escudo e bloqueio.
- **Armas e equipamentos:** arma primária, arma secundária, escudo, armaduras, objeto de Contato e mochila.
- **Bloqueio:** um escudo equipado reduz o dano recebido em 50% e consome 10 pontos de estamina por golpe bloqueado. Sem escudo, uma espada equipada permite bloquear com redução de 15% e custo de 15 pontos. Sem escudo ou espada equipada, o jogador recebe o dano normalmente.
- **Inventário:** 40 espaços, organizados em quatro linhas de dez; uma mochila pode acrescentar até 20 espaços.
- **Recursos:** vida e estamina se recuperam passivamente. Itens consumíveis recuperam recursos de forma imediata. Correr não consome estamina.
- **Progressão:** experiência, níveis de personagem, equipamentos e habilidades.
- **Inimigos:** a demonstração de IA usa detecção e perseguição com alcance limitado, retorno à área de origem, patrulha local, recuo com pouca vida e estilos de combate corpo a corpo, à distância ou híbrido. Inimigos comuns recuperam a vida após 10 segundos sem receber dano; chefes ficam fora dessa recuperação automática.
- **Chefes:** a demonstração tem barra de vida, sequência de ataques à distância e investida, queimadura e regeneração lenta; os valores são provisórios. A loja para compra de itens também está implementada como demonstração.

## Controles do protótipo

- **WASD/setas:** mover; **Ctrl:** correr; **Espaço:** dash.
- **F:** interagir e recolher itens; **E:** abrir/fechar o inventário.
- **B:** abrir o inventário diretamente quando a mochila estiver equipada.
- **I:** abrir o menu de gameplay; **Esc:** fechar um menu aberto ou pausar o jogo.
- **Clique esquerdo:** ataque leve; segurar o botão esquerdo carrega o ataque pesado.
- **Clique direito:** usar a arma secundária; segurar o botão direito bloqueia com escudo ou espada equipada.
- **Tab:** trocar as posições das armas primária e secundária.
- **Z/X/C/V:** espaços para habilidades; atualmente, a colher usa Z, a luva usa Z/X e a maçã usa C. V fica vazio até outro poder receber uma habilidade.
- **M/N/H/K:** abrir diretamente as abas de mapa, missões, melhorias e árvore de habilidades no menu de gameplay.

## Protótipo

O protótipo já reúne exemplos jogáveis de movimento, combate, poderes de Contato e Absorção, coleta e uso de itens, inventário, equipamentos, diálogos, missões e progressão. Algumas telas do menu de gameplay são demonstrações visuais e não representam sistemas completos.

As regras descritas neste README resumem o conceito do jogo. Nem todas as mecânicas planejadas estão implementadas no protótipo atual.

## Tecnologia

- Unity 2D
- C#
