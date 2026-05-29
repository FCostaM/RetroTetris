# Documento de Requisitos — Tela de Controles

## Introdução

Esta feature adiciona uma opção "Controls" ao RetroTetris, acessível tanto na tela inicial (Start_Screen) quanto durante a pausa do jogo (PausedState). Ao acionar essa opção, o jogador visualiza uma tela sobreposta com a lista completa de comandos disponíveis no jogo. O objetivo é permitir que jogadores novos ou ocasionais consultem os controles sem precisar sair da partida ou perder o progresso.

## Glossário

- **Controls_Screen**: Tela sobreposta (overlay) que exibe a lista de todos os comandos disponíveis no jogo.
- **Controls_Button**: Botão ou elemento interativo que aciona a exibição da Controls_Screen.
- **ControlsScreenState**: Estado da State Machine do jogo que representa a Controls_Screen ativa.
- **Previous_State**: O estado do jogo imediatamente anterior à abertura da Controls_Screen (StartScreenState ou PausedState).
- **Command_Entry**: Uma linha da lista de controles, composta por um rótulo de tecla/gesto e sua descrição de ação.

---

## Requisitos

### Requisito 1: Botão "Controls" na Tela Inicial

**User Story:** Como jogador, quero ver um botão "Controls" na tela inicial do jogo, para que eu possa consultar os comandos disponíveis antes de iniciar uma partida.

#### Critérios de Aceitação

1. WHEN a aplicação está no StartScreenState, THE Renderer SHALL exibir um Controls_Button visível na Start_Screen, abaixo do botão "Start".
2. THE Renderer SHALL exibir o Controls_Button com estilo visual consistente com os demais botões da Start_Screen, utilizando a fonte pixelada e o esquema de cores retrô.
3. WHEN o jogador clica ou toca no Controls_Button na Start_Screen, THE Game SHALL transicionar para o ControlsScreenState, armazenando StartScreenState como Previous_State.
4. WHERE o dispositivo suportar teclado, WHEN o jogador pressiona a tecla H na Start_Screen, THE Game SHALL transicionar para o ControlsScreenState, armazenando StartScreenState como Previous_State.

---

### Requisito 2: Botão "Controls" na Tela de Pausa

**User Story:** Como jogador, quero ver um botão "Controls" na tela de pausa, para que eu possa consultar os comandos sem perder o progresso da partida atual.

#### Critérios de Aceitação

1. WHEN o jogo está no PausedState, THE Renderer SHALL exibir um Controls_Button visível no overlay de pausa, abaixo da indicação "PAUSED".
2. THE Renderer SHALL exibir o Controls_Button no overlay de pausa com estilo visual consistente com os demais elementos do overlay, utilizando a fonte pixelada e o esquema de cores retrô.
3. WHEN o jogador clica ou toca no Controls_Button no overlay de pausa, THE Game SHALL transicionar para o ControlsScreenState, armazenando PausedState como Previous_State.
4. WHERE o dispositivo suportar teclado, WHEN o jogador pressiona a tecla H durante o PausedState, THE Game SHALL transicionar para o ControlsScreenState, armazenando PausedState como Previous_State.

---

### Requisito 3: Exibição da Tela de Controles

**User Story:** Como jogador, quero ver uma tela com todos os comandos do jogo listados de forma clara, para que eu possa aprender ou relembrar os controles rapidamente.

#### Critérios de Aceitação

1. WHEN o jogo está no ControlsScreenState, THE Renderer SHALL exibir a Controls_Screen como um overlay sobre o conteúdo anterior, cobrindo a área do Board.
2. THE Renderer SHALL exibir o título "CONTROLS" na Controls_Screen utilizando a fonte pixelada em estilo retrô.
3. THE Renderer SHALL exibir na Controls_Screen todos os Command_Entry da lista de controles de teclado, incluindo: Seta Esquerda (mover esquerda), Seta Direita (mover direita), Seta Baixo (soft drop), Espaço (hard drop), Seta Cima / Z (rotacionar horário), X (rotacionar anti-horário), C / Shift (hold), P / Escape (pausar/retomar) e H (controles).
4. WHERE o dispositivo suportar eventos de toque nativos do .NET MAUI, THE Renderer SHALL exibir na Controls_Screen os Command_Entry de gestos de toque, incluindo: Swipe Esquerda (mover esquerda), Swipe Direita (mover direita), Swipe Baixo (soft drop), Swipe Cima (hard drop), Tap Esquerda (rotacionar anti-horário) e Tap Direita (rotacionar horário).
5. THE Renderer SHALL exibir cada Command_Entry com o rótulo da tecla ou gesto alinhado à esquerda e a descrição da ação alinhada à direita ou centralizada, em fonte pixelada legível.
6. THE Renderer SHALL exibir um botão "BACK" ou indicação de tecla de retorno na Controls_Screen, para que o jogador saiba como fechar a tela.

---

### Requisito 4: Fechamento da Tela de Controles

**User Story:** Como jogador, quero fechar a tela de controles e retornar ao estado anterior do jogo, para que eu possa continuar de onde parei sem perder o progresso.

#### Critérios de Aceitação

1. WHEN o jogador clica ou toca no botão "BACK" na Controls_Screen, THE Game SHALL transicionar de volta para o Previous_State.
2. WHERE o dispositivo suportar teclado, WHEN o jogador pressiona Escape, P ou H durante o ControlsScreenState, THE Game SHALL transicionar de volta para o Previous_State.
3. WHEN o ControlsScreenState transiciona de volta para o PausedState como Previous_State, THE Game SHALL retomar o PausedState sem alterar o estado do Board, Score, Level, Hold_Piece ou Bag_Randomizer.
4. WHEN o ControlsScreenState transiciona de volta para o StartScreenState como Previous_State, THE Game SHALL retornar à Start_Screen sem iniciar uma nova partida.
5. WHILE o jogo está no ControlsScreenState com Previous_State igual a PausedState, THE Game_Loop SHALL permanecer suspenso, preservando o estado completo da partida.

---

### Requisito 5: Consistência Visual Retrô

**User Story:** Como jogador, quero que a tela de controles tenha visual consistente com o estilo retrô do jogo, para que a experiência seja coesa e imersiva.

#### Critérios de Aceitação

1. THE Renderer SHALL exibir a Controls_Screen com fundo escuro semitransparente sobreposto ao conteúdo anterior, consistente com o overlay de pausa e game over.
2. THE Renderer SHALL utilizar a fonte "PressStart2P" para todos os textos exibidos na Controls_Screen.
3. THE Renderer SHALL utilizar a paleta de cores retrô do jogo (fundo escuro, texto claro, destaques em ciano) na Controls_Screen.
4. THE Renderer SHALL exibir bordas decorativas em estilo retrô ao redor da Controls_Screen, consistentes com o estilo visual dos demais painéis do jogo.
