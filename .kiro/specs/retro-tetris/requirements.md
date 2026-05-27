# Documento de Requisitos

## Introdução

O Retro Tetris é uma implementação do jogo clássico Tetris como aplicação desktop, desenvolvida em .NET 10 com .NET MAUI. O jogo é executado em janela nativa do sistema operacional e reproduz fielmente a experiência do Tetris original, incluindo os 7 tetrominós padrão, mecânicas de rotação, eliminação de linhas e sistema de pontuação, com visual em estilo retrô que remete ao jogo clássico dos anos 80/90.

## Glossário

- **Game**: O sistema principal do jogo Retro Tetris.
- **Board**: O tabuleiro de jogo, grade de 10 colunas × 20 linhas onde as peças se movem e se acumulam.
- **Tetromino**: Peça do jogo composta por 4 blocos quadrados conectados. Existem 7 tipos: I, O, T, S, Z, J e L.
- **Active_Piece**: O tetrominó atualmente em queda controlado pelo jogador.
- **Ghost_Piece**: Projeção semitransparente que indica onde a Active_Piece irá pousar.
- **Next_Piece**: O próximo tetrominó a entrar no Board, exibido em painel separado.
- **Hold_Piece**: Tetrominó reservado pelo jogador para uso posterior.
- **Bag_Randomizer**: Algoritmo de geração de peças que garante distribuição uniforme dos 7 tetrominós (sistema "7-bag").
- **Lock_Delay**: Tempo de espera antes de uma peça ser fixada ao Board após tocar o chão ou outra peça.
- **Line_Clear**: Eliminação de uma ou mais linhas completas do Board.
- **Score**: Pontuação acumulada pelo jogador durante a partida.
- **Level**: Nível atual da partida, que determina a velocidade de queda das peças.
- **Renderer**: Componente responsável por renderizar o estado do jogo na tela, utilizando a API de desenho do .NET MAUI (GraphicsView/ICanvas).
- **Input_Handler**: Componente responsável por capturar e processar entradas de teclado e toque nativos do .NET MAUI.
- **Window**: Janela nativa do sistema operacional gerenciada pelo .NET MAUI, na qual o jogo é exibido e executado.
- **Game_Loop**: Ciclo principal de atualização e renderização do jogo.
- **SRS**: Super Rotation System — sistema de rotação e wall-kick padrão do Tetris moderno.
- **Start_Screen**: Tela inicial exibida ao abrir a aplicação, antes do início da partida, contendo o nome do jogo e o botão de início.

---

## Requisitos

### Requisito 1: Tela Inicial

**User Story:** Como jogador, quero ver uma tela inicial ao abrir o jogo com o nome e um botão de início, para que eu possa começar a partida de forma clara e intuitiva.

#### Critérios de Aceitação

1. WHEN a aplicação é iniciada, THE Renderer SHALL exibir a Start_Screen com o nome "Retro Tetris" em destaque e um botão "Start".
2. THE Renderer SHALL exibir o nome "Retro Tetris" na Start_Screen utilizando fonte pixelada ou monoespaçada em estilo retrô.
3. WHEN o jogador pressiona o botão "Start" na Start_Screen, THE Game SHALL iniciar uma nova partida e exibir o Board.
4. WHILE a Start_Screen está ativa, THE Game_Loop SHALL permanecer inativo.

---

### Requisito 2: Tabuleiro de Jogo

**User Story:** Como jogador, quero um tabuleiro de jogo com as dimensões corretas do Tetris clássico, para que a experiência seja fiel ao original.

#### Critérios de Aceitação

1. THE Board SHALL ter exatamente 10 colunas e 20 linhas visíveis.
2. THE Board SHALL manter uma área de buffer de 2 linhas acima da área visível para geração de peças.
3. WHEN uma Active_Piece ultrapassa o topo da área visível do Board, THE Game SHALL encerrar a partida atual.
4. THE Renderer SHALL exibir o Board com bordas visíveis em estilo retrô.
5. THE Renderer SHALL diferenciar visualmente células ocupadas de células vazias no Board.

---

### Requisito 3: Os Sete Tetrominós

**User Story:** Como jogador, quero que o jogo inclua todos os 7 tetrominós padrão do Tetris original, para que a experiência seja completa e autêntica.

#### Critérios de Aceitação

1. THE Game SHALL suportar os 7 tetrominós padrão: I, O, T, S, Z, J e L.
2. THE Renderer SHALL exibir cada tetrominó com uma cor distinta e fixa: I (ciano), O (amarelo), T (roxo), S (verde), Z (vermelho), J (azul), L (laranja).
3. THE Renderer SHALL exibir cada tetrominó com visual em estilo retrô, incluindo borda e sombreamento nos blocos.
4. WHEN um Tetromino é gerado, THE Game SHALL posicioná-lo centralizado horizontalmente no topo do Board.
5. THE Bag_Randomizer SHALL gerar os 7 tetrominós em ordem aleatória antes de repetir qualquer peça, garantindo que cada tipo apareça exatamente uma vez por ciclo de 7 peças.

---

### Requisito 4: Movimentação das Peças

**User Story:** Como jogador, quero mover e rotacionar as peças com o teclado, para que eu possa controlar o jogo com precisão.

#### Critérios de Aceitação

1. WHEN o jogador pressiona a tecla Seta Esquerda, THE Input_Handler SHALL mover a Active_Piece uma coluna para a esquerda, se a posição destino estiver livre no Board.
2. WHEN o jogador pressiona a tecla Seta Direita, THE Input_Handler SHALL mover a Active_Piece uma coluna para a direita, se a posição destino estiver livre no Board.
3. WHEN o jogador pressiona a tecla Seta Baixo (soft drop), THE Input_Handler SHALL mover a Active_Piece uma linha para baixo, se a posição destino estiver livre no Board.
4. WHEN o jogador pressiona a tecla Espaço (hard drop), THE Input_Handler SHALL mover a Active_Piece diretamente para a posição mais baixa disponível no Board e fixá-la imediatamente.
5. WHEN o jogador pressiona a tecla Seta Cima ou Z, THE Input_Handler SHALL rotacionar a Active_Piece 90 graus no sentido horário, aplicando as regras de wall-kick do SRS.
6. WHEN o jogador pressiona a tecla X, THE Input_Handler SHALL rotacionar a Active_Piece 90 graus no sentido anti-horário, aplicando as regras de wall-kick do SRS.
7. IF uma movimentação ou rotação resultaria em sobreposição com células ocupadas ou fora dos limites do Board, THEN THE Game SHALL ignorar o comando e manter a Active_Piece na posição atual.
8. WHILE o jogador mantém pressionada a tecla Seta Esquerda ou Seta Direita por mais de 170ms, THE Input_Handler SHALL repetir o movimento lateral a cada 50ms (Auto-Repeat Rate).

---

### Requisito 5: Queda Automática e Fixação de Peças

**User Story:** Como jogador, quero que as peças caiam automaticamente e se fixem ao tabuleiro, para que o jogo progrida com o tempo.

#### Critérios de Aceitação

1. THE Game_Loop SHALL mover a Active_Piece uma linha para baixo automaticamente em intervalos regulares definidos pelo Level atual.
2. WHEN a Active_Piece não pode mais se mover para baixo, THE Game SHALL iniciar o Lock_Delay de 500ms antes de fixar a peça.
3. WHEN o Lock_Delay expira sem que a Active_Piece tenha se movido, THE Game SHALL fixar a Active_Piece no Board na posição atual.
4. WHEN o jogador move ou rotaciona a Active_Piece durante o Lock_Delay, THE Game SHALL reiniciar o Lock_Delay, com no máximo 15 reinicializações por peça.
5. WHEN a Active_Piece é fixada no Board, THE Game SHALL gerar uma nova Active_Piece a partir do Bag_Randomizer.

---

### Requisito 6: Eliminação de Linhas

**User Story:** Como jogador, quero que linhas completas sejam eliminadas do tabuleiro, para que eu possa continuar jogando e acumular pontos.

#### Critérios de Aceitação

1. WHEN a Active_Piece é fixada no Board, THE Game SHALL verificar todas as linhas do Board em busca de linhas completas.
2. WHEN uma ou mais linhas completas são detectadas, THE Game SHALL removê-las do Board e deslocar todas as linhas acima para baixo.
3. THE Renderer SHALL exibir uma animação de flash nas linhas eliminadas antes de removê-las, com duração de 200ms.
4. WHEN 1 linha é eliminada, THE Score SHALL incrementar em 100 × Level pontos.
5. WHEN 2 linhas são eliminadas simultaneamente, THE Score SHALL incrementar em 300 × Level pontos.
6. WHEN 3 linhas são eliminadas simultaneamente, THE Score SHALL incrementar em 500 × Level pontos.
7. WHEN 4 linhas são eliminadas simultaneamente (Tetris), THE Score SHALL incrementar em 800 × Level pontos.
8. THE Game SHALL contabilizar o total de linhas eliminadas durante a partida para cálculo de Level.

---

### Requisito 7: Sistema de Níveis e Velocidade

**User Story:** Como jogador, quero que o jogo fique progressivamente mais rápido conforme avanço de nível, para que o desafio aumente ao longo da partida.

#### Critérios de Aceitação

1. THE Game SHALL iniciar no Level 1 com intervalo de queda automática de 1000ms por linha.
2. WHEN o total de linhas eliminadas atinge um múltiplo de 10, THE Game SHALL incrementar o Level em 1.
3. THE Game_Loop SHALL calcular o intervalo de queda automática em função do Level, reduzindo progressivamente conforme a fórmula: `intervalo(ms) = max(100, 1000 - (Level - 1) × 90)`.
4. THE Renderer SHALL exibir o Level atual em painel lateral visível durante a partida.
5. THE Game SHALL suportar até o Level 15 como nível máximo, mantendo o intervalo mínimo de 100ms a partir desse nível.

---

### Requisito 8: Ghost Piece

**User Story:** Como jogador, quero ver onde a peça atual vai pousar, para que eu possa planejar meus movimentos com mais precisão.

#### Critérios de Aceitação

1. THE Renderer SHALL exibir a Ghost_Piece como uma projeção da Active_Piece na posição mais baixa disponível diretamente abaixo dela.
2. THE Renderer SHALL diferenciar visualmente a Ghost_Piece da Active_Piece, usando a mesma cor da peça com opacidade reduzida ou contorno.
3. WHEN a Active_Piece se move ou rotaciona, THE Renderer SHALL atualizar a posição da Ghost_Piece imediatamente.
4. WHEN a Ghost_Piece ocupa a mesma posição que a Active_Piece, THE Renderer SHALL exibir apenas a Active_Piece.

---

### Requisito 9: Hold Piece

**User Story:** Como jogador, quero reservar uma peça para usar depois, para que eu possa gerenciar melhor as situações difíceis.

#### Critérios de Aceitação

1. WHEN o jogador pressiona a tecla C ou Shift, THE Input_Handler SHALL mover a Active_Piece para o Hold_Piece.
2. WHEN o Hold_Piece está vazio e o jogador aciona o hold, THE Game SHALL colocar a Active_Piece no Hold_Piece e gerar uma nova Active_Piece do Bag_Randomizer.
3. WHEN o Hold_Piece já contém uma peça e o jogador aciona o hold, THE Game SHALL trocar a Active_Piece com o Hold_Piece, reposicionando a peça trocada no topo central do Board.
4. THE Game SHALL permitir apenas uma troca de Hold_Piece por peça ativa, bloqueando novas trocas até que a Active_Piece atual seja fixada.
5. THE Renderer SHALL exibir o Hold_Piece em painel lateral visível durante a partida.

---

### Requisito 10: Próxima Peça

**User Story:** Como jogador, quero ver as próximas peças que vão entrar no jogo, para que eu possa planejar minha estratégia com antecedência.

#### Critérios de Aceitação

1. THE Renderer SHALL exibir as próximas 3 peças da fila do Bag_Randomizer em painel lateral visível durante a partida.
2. THE Renderer SHALL atualizar o painel de Next_Piece imediatamente após cada nova Active_Piece ser gerada.
3. THE Renderer SHALL exibir cada Next_Piece com sua cor correspondente e visual retrô.

---

### Requisito 11: Pontuação e Recordes

**User Story:** Como jogador, quero ver minha pontuação durante o jogo e meu recorde pessoal, para que eu possa acompanhar meu desempenho.

#### Critérios de Aceitação

1. THE Renderer SHALL exibir o Score atual em painel lateral visível e atualizado em tempo real durante a partida.
2. THE Game SHALL armazenar o High Score localmente em arquivo no sistema de arquivos do dispositivo, utilizando o FileSystem do .NET MAUI com serialização JSON.
3. WHEN a partida termina e o Score supera o High Score armazenado, THE Game SHALL atualizar o High Score armazenado com o novo valor.
4. THE Renderer SHALL exibir o High Score em painel lateral visível durante a partida.
5. WHEN o jogador inicia uma nova partida, THE Game SHALL resetar o Score para zero, mantendo o High Score armazenado.

---

### Requisito 12: Tela de Game Over

**User Story:** Como jogador, quero ver uma tela de game over ao perder com opções claras de reiniciar ou sair, para que eu possa decidir o que fazer após o fim da partida.

#### Critérios de Aceitação

1. WHEN a partida termina, THE Renderer SHALL exibir uma tela de Game Over sobreposta ao Board, mostrando o Score final e o High Score.
2. THE Renderer SHALL exibir a mensagem "Game Over" em fonte vermelha com efeito visual retrô, incluindo efeito de piscar (flicker) ou scanlines ou glitch retrô.
3. THE Renderer SHALL exibir na tela de Game Over dois botões: "Try Again" e "Exit".
4. WHEN o jogador pressiona o botão "Try Again" na tela de Game Over, THE Game SHALL resetar o Board, Score, Level, Hold_Piece e Bag_Randomizer para o estado inicial e iniciar uma nova partida.
5. WHEN o jogador pressiona o botão "Exit" na tela de Game Over, THE Game SHALL encerrar a aplicação.

---

### Requisito 13: Pausa

**User Story:** Como jogador, quero pausar o jogo a qualquer momento, para que eu possa interromper a partida sem perdê-la.

#### Critérios de Aceitação

1. WHEN o jogador pressiona a tecla Escape ou P durante a partida, THE Game SHALL pausar o Game_Loop e exibir uma indicação visual de pausa.
2. WHILE o jogo está pausado, THE Game_Loop SHALL suspender a queda automática e o Lock_Delay.
3. WHILE o jogo está pausado, THE Renderer SHALL ocultar o conteúdo do Board para evitar vantagem ao jogador.
4. WHEN o jogador pressiona Escape ou P novamente durante a pausa, THE Game SHALL retomar o Game_Loop do ponto em que foi pausado.

---

### Requisito 14: Visual Retrô

**User Story:** Como jogador, quero que o jogo tenha visual em estilo retrô semelhante ao Tetris clássico, para que a experiência seja nostálgica e imersiva.

#### Critérios de Aceitação

1. THE Renderer SHALL utilizar uma paleta de cores limitada e contrastante, com fundo escuro e blocos coloridos com bordas e sombreamento, remetendo ao estilo dos anos 80/90.
2. THE Renderer SHALL utilizar uma fonte monoespaçada ou pixelada para todos os textos exibidos na interface.
3. THE Renderer SHALL exibir o Board com bordas decorativas em estilo retrô.
4. THE Game SHALL exibir efeitos sonoros retrô para eventos de movimentação, fixação de peça, eliminação de linha e game over, utilizando biblioteca de áudio nativa compatível com .NET MAUI (Plugin.Maui.Audio).
5. WHERE o dispositivo suportar áudio, THE Game SHALL reproduzir uma trilha sonora de fundo em loop durante a partida, com opção de silenciar, utilizando Plugin.Maui.Audio.

---

### Requisito 15: Controles por Toque (Tablet e Dispositivos Híbridos)

**User Story:** Como jogador em tablet ou dispositivo híbrido, quero controlar o jogo por gestos de toque nativos, para que eu possa jogar sem teclado físico.

#### Critérios de Aceitação

1. WHERE o dispositivo suportar eventos de toque nativos do .NET MAUI, THE Input_Handler SHALL reconhecer swipe para a esquerda como movimento lateral esquerdo da Active_Piece.
2. WHERE o dispositivo suportar eventos de toque nativos do .NET MAUI, THE Input_Handler SHALL reconhecer swipe para a direita como movimento lateral direito da Active_Piece.
3. WHERE o dispositivo suportar eventos de toque nativos do .NET MAUI, THE Input_Handler SHALL reconhecer swipe para baixo como soft drop da Active_Piece.
4. WHERE o dispositivo suportar eventos de toque nativos do .NET MAUI, THE Input_Handler SHALL reconhecer tap na metade direita da tela como rotação horária da Active_Piece.
5. WHERE o dispositivo suportar eventos de toque nativos do .NET MAUI, THE Input_Handler SHALL reconhecer tap na metade esquerda da tela como rotação anti-horária da Active_Piece.
6. WHERE o dispositivo suportar eventos de toque nativos do .NET MAUI, THE Input_Handler SHALL reconhecer swipe rápido para cima como hard drop da Active_Piece.

---

### Requisito 16: Responsividade e Layout

**User Story:** Como jogador, quero que o jogo se adapte ao tamanho da janela do sistema operacional, para que eu possa jogar em diferentes resoluções.

#### Critérios de Aceitação

1. THE Renderer SHALL escalar o Board e os painéis laterais proporcionalmente ao tamanho da Window, mantendo a proporção correta do tabuleiro.
2. THE Renderer SHALL exibir o layout em orientação retrato quando a Window for mais alta do que larga, com os painéis de informação (Score, Level, Next, Hold) posicionados acima ou abaixo do Board.
3. THE Renderer SHALL exibir o layout em orientação paisagem quando a Window for mais larga do que alta, com os painéis de informação posicionados lateralmente ao Board.
4. THE Game SHALL funcionar corretamente em Windows como plataforma desktop principal.
5. WHERE a plataforma for macOS, THE Game SHALL funcionar corretamente como aplicação .NET MAUI nativa.
