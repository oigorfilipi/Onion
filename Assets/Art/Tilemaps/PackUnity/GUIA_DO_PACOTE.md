# Tile palettes do pacote

Os PNGs originais continuam em `Assets/Sprites/Tilemaps/Art`. Este comando cria Tiles e Palettes em `Assets/Art/Tilemaps/PackUnity` sem pintar o mapa.

## Palettes de uso comum

- `01_Terreno`: atlas de grama e chão.
- `02_Estradas`: peças de estrada.
- `03_Agua_e_Areia`: água, margens e areia.
- `04_Encostas_Simples`: peças simples de encosta.
- `05_Construcoes`: casas, poço e portão.
- `06_Vegetacao`: árvores e arbustos completos.
- `07_Rochas`: rochas completas.
- `08_Decoracao`: objetos e props completos.
- `09_Sombras`: sombras individuais e peças do atlas de sombra.
- `10_Animacoes`: flores e fogo montados a partir das animações do TSX.
- `11_Quadros_de_Porta`: quadros individuais dos sprites de porta; a interação de abrir/fechar ainda precisa de Animator/roteiro.

## Palettes avançadas

As palettes `12` a `15` contêm as peças 16×16 dos atlas originais de construções, decoração, rochas e vegetação. Elas são úteis para montar detalhes manualmente; para casas, árvores e props inteiros, prefira as palettes de uso comum.

O atlas `Tileset_RockSlope.png` permanece como uma única imagem de referência no Project: ele tem 4.096 células e depende das regras AutoMap do Tiled, então não foi fatiado nem convertido em Palette. A versão `Tileset_RockSlope_Simple` está pronta para pintar.

## Como pintar

1. Abra `Window > 2D > Tile Palette`.
2. Escolha uma Palette na lista e escolha como alvo a camada Tilemap correspondente na cena.
3. Para chão, use `Chao_Base`; estradas, água, encostas, sombras, objetos atrás do jogador, animações e objetos à frente têm camadas próprias na Grid da cidade inicial.
4. Use o pincel para pintar; o conta-gotas seleciona uma peça que já esteja na cena.

Os Tiles são visuais e não recebem colliders neste preparo. As colisões de árvores, casas, muros, portas e o sistema de telhado retrátil serão uma etapa separada. Para árvores e construções, use a camada atrás ou à frente do jogador conforme o trecho que deve ficar por cima.

O script leu os arquivos `.tsx` para respeitar dimensões, células e quadros de animação do pacote. Nenhum `.tmx` de exemplo é aplicado à cena.
