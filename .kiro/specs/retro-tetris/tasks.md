# Plano de Implementação: Retro Tetris

## Visão Geral

Implementação incremental do Retro Tetris em C# com .NET 10 e .NET MAUI, organizada em três projetos (`RetroTetris.Core`, `RetroTetris`, `RetroTetris.Tests`). Cada tarefa constrói sobre as anteriores, começando pelo núcleo puro de lógica e terminando com a camada de apresentação e integração final.

## Tarefas

- [x] 1. Configurar a solution e estrutura de projetos
  - Criar a solution `RetroTetris.sln` com os três projetos: `RetroTetris.Core` (class library .NET 10), `RetroTetris` (MAUI app) e `RetroTetris.Tests` (xUnit)
  - Adicionar referências entre projetos: `RetroTetris` → `RetroTetris.Core`; `RetroTetris.Tests` → `RetroTetris.Core`
  - Adicionar pacotes NuGet: `xunit`, `xunit.runner.visualstudio`, `FsCheck.Xunit 3.*` em `RetroTetris.Tests`; `Plugin.Maui.Audio` em `RetroTetris`
  - Criar a estrutura de pastas conforme o design: `Interfaces/`, `Engine/`, `Commands/`, `States/`, `Models/` em `RetroTetris.Core`; `Pages/`, `Presentation/`, `Services/`, `Resources/Audio/`, `Resources/Fonts/` em `RetroTetris`; `Arbitraries/`, `Unit/`, `Properties/`, `Integration/` em `RetroTetris.Tests`
  - Registrar a fonte `PressStart2P-Regular.ttf` no `MauiProgram.cs`
  - _Requisitos: 16.4_

- [x] 2. Implementar modelos de dados e formas dos tetrominós
  - [x] 2.1 Criar enums e tipos base em `RetroTetris.Core`
    - Implementar `TetrominoType` (I, O, T, S, Z, J, L) e `RotationState` (Spawn, Right, Two, Left) em `Tetromino.cs`
    - Implementar `GameSnapshot` e `HighScoreData` em `Models/`
    - _Requisitos: 3.1_

  - [x] 2.2 Implementar `TetrominoShapes` com todas as matrizes de rotação
    - Definir as coordenadas relativas (row, col) para cada um dos 7 tetrominós em cada um dos 4 estados de rotação como arrays estáticos em `TetrominoShapes.cs`
    - _Requisitos: 3.1, 4.5, 4.6_

  - [x] 2.3 Implementar `TetrominoColors` com a paleta retrô
    - Mapear cada `TetrominoType` para sua cor fixa (I=ciano, O=amarelo, T=roxo, S=verde, Z=vermelho, J=azul, L=laranja) em `TetrominoColors.cs`
    - _Requisitos: 3.2_

  - [x] 2.4 Implementar a classe `Tetromino`
    - Implementar `GetCells()` usando `TetrominoShapes`, `RotateClockwise()` e `RotateCounterClockwise()` retornando novas instâncias imutáveis, e `Tetromino.Create(type)` com posição inicial centralizada
    - _Requisitos: 3.1, 3.4, 4.5, 4.6_

  - [x] 2.5 Escrever testes unitários para `Tetromino` e `TetrominoShapes`
    - Verificar formas corretas de cada tetrominó em cada estado de rotação
    - Verificar que `RotateClockwise` seguido de `RotateCounterClockwise` retorna ao estado original
    - _Requisitos: 3.1, 4.5, 4.6_

- [x] 3. Implementar o `Board` e o `BagRandomizer`
  - [x] 3.1 Implementar `Board`
    - Implementar a grade `TetrominoType?[,]` de 10×22 (20 visíveis + 2 buffer) com `GetCell`, `IsOccupied`, `IsInBounds`, `CanPlace`, `LockPiece`, `FindCompleteLines`, `ClearLines` e `Reset`
    - _Requisitos: 2.1, 2.2, 5.3, 6.1, 6.2_

  - [x] 3.2 Escrever testes unitários para `Board`
    - Testar `CanPlace` com peças nos limites, fora dos limites e sobre células ocupadas
    - Testar `FindCompleteLines` com 0, 1, 2, 3 e 4 linhas completas
    - Testar `ClearLines` verificando que linhas acima descem corretamente
    - _Requisitos: 2.1, 6.1, 6.2_

  - [x] 3.3 Escrever property test — Propriedade 5: Eliminação preserva células não eliminadas
    - **Propriedade 5: Eliminação de linhas preserva células não eliminadas**
    - **Valida: Requisitos 6.1, 6.2**

  - [x] 3.4 Implementar `BagRandomizer`
    - Implementar o sistema "7-bag" com `Queue<TetrominoType>`, `Peek(index)`, `Dequeue()` e `Reset()`; preencher o bag com os 7 tipos embaralhados antes de repetir
    - _Requisitos: 3.5_

  - [x] 3.5 Escrever property test — Propriedade 7: Bag randomizer garante distribuição uniforme
    - **Propriedade 7: Bag randomizer garante distribuição uniforme**
    - **Valida: Requisito 3.5**

- [x] 4. Implementar `SRSRotationSystem`, `GhostCalculator` e `LockDelayController`
  - [x] 4.1 Implementar `SRSRotationSystem`
    - Embutir as tabelas de wall-kick do Tetris Guideline para J/L/S/T/Z (tabela compartilhada) e I (tabela própria); O não realiza wall-kick
    - Implementar `GetKickOffsets(type, from, to)` e `TryRotate(piece, board, clockwise)` testando cada offset em ordem
    - _Requisitos: 4.5, 4.6, 4.7_

  - [x] 4.2 Escrever testes unitários para `SRSRotationSystem`
    - Testar casos específicos de wall-kick documentados no Tetris Wiki (ex.: T-spin, I-piece nas bordas)
    - Testar que rotação falha quando todos os offsets estão bloqueados
    - _Requisitos: 4.5, 4.6, 4.7_

  - [x] 4.3 Escrever property test — Propriedade 2: Rotação SRS preserva integridade do board
    - **Propriedade 2: Rotação SRS preserva integridade do board**
    - **Valida: Requisitos 4.5, 4.6, 4.7**

  - [x] 4.4 Implementar `GhostCalculator`
    - Implementar `Calculate(activePiece, board)` movendo a peça para baixo até não poder mais, retornando a posição final
    - _Requisitos: 8.1, 8.3_

  - [x] 4.5 Escrever property test — Propriedade 4: Ghost piece coincide com destino do hard drop
    - **Propriedade 4: Ghost piece coincide com destino do hard drop**
    - **Valida: Requisitos 8.1, 8.3**

  - [x] 4.6 Implementar `LockDelayController`
    - Implementar `Start()`, `Reset()` (respeitando `MaxResets = 15`), `Cancel()` e `HasExpired(elapsed)` com `DelayMs = 500`
    - _Requisitos: 5.2, 5.3, 5.4_

  - [x] 4.7 Escrever testes unitários para `LockDelayController`
    - Testar expiração após 500ms, reset que reinicia o timer, bloqueio após 15 resets
    - _Requisitos: 5.2, 5.3, 5.4_

- [x] 5. Implementar `ScoreSystem` e `LevelSystem`
  - [x] 5.1 Implementar `ScoreSystem`
    - Implementar `AddLinesClear(linesCleared, level)` com a tabela de multiplicadores [100, 300, 500, 800] e `Reset()`
    - _Requisitos: 6.4, 6.5, 6.6, 6.7_

  - [x] 5.2 Escrever property test — Propriedade 6: Pontuação de linhas é calculada corretamente
    - **Propriedade 6: Pontuação de linhas é calculada corretamente**
    - **Valida: Requisitos 6.4, 6.5, 6.6, 6.7**

  - [x] 5.3 Implementar `LevelSystem`
    - Implementar `AddLines(count)` incrementando o nível a cada 10 linhas acumuladas (máximo 15), `GetDropIntervalMs()` com a fórmula `max(100, 1000 - (Level-1) × 90)` e `Reset()`
    - _Requisitos: 7.1, 7.2, 7.3, 7.5_

  - [x] 5.4 Escrever property test — Propriedade 8: Intervalo de queda segue a fórmula do nível
    - **Propriedade 8: Intervalo de queda segue a fórmula do nível**
    - **Valida: Requisitos 7.1, 7.3, 7.5**

  - [x] 5.5 Escrever property test — Propriedade 10: Nível incrementa a cada 10 linhas eliminadas
    - **Propriedade 10: Nível incrementa a cada 10 linhas eliminadas**
    - **Valida: Requisitos 7.2, 7.5**

- [x] 6. Checkpoint — Verificar núcleo de lógica pura
  - Garantir que todos os testes em `RetroTetris.Tests` passam. Perguntar ao usuário se há dúvidas antes de prosseguir.

- [x] 7. Implementar interfaces e Command Pattern
  - [x] 7.1 Definir interfaces do núcleo
    - Criar `IGameEngine.cs`, `IGameRenderer.cs`, `IInputHandler.cs`, `IAudioService.cs` e `IHighScoreRepository.cs` em `RetroTetris.Core/Interfaces/` conforme as assinaturas do design
    - _Requisitos: 1.1, 4.1–4.8, 5.1, 9.1, 11.2, 14.4_

  - [x] 7.2 Implementar `IGameCommand` e todos os comandos concretos
    - Criar `IGameCommand.cs` e os 8 comandos concretos em `RetroTetris.Core/Commands/`: `MoveLeftCommand`, `MoveRightCommand`, `SoftDropCommand`, `HardDropCommand`, `RotateClockwiseCommand`, `RotateCounterClockwiseCommand`, `HoldCommand`, `PauseCommand`
    - _Requisitos: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 9.1, 13.1_

  - [x] 7.3 Implementar `CommandQueue`
    - Implementar a fila thread-safe usando `ConcurrentQueue<IGameCommand>` com `Enqueue`, `TryDequeue` e `Clear`
    - _Requisitos: 4.1–4.8_

  - [x] 7.4 Escrever testes unitários para Command Pattern e `CommandQueue`
    - Verificar que cada `IGameCommand` chama o método correto do engine (usando mock/stub)
    - Testar thread-safety do `CommandQueue` com enqueue/dequeue concorrentes
    - _Requisitos: 4.1–4.8_

- [x] 8. Implementar State Machine e `GameEngine`
  - [x] 8.1 Implementar `IGameState` e os 4 estados concretos
    - Criar `IGameState.cs` com `Enter`, `Update` e `Exit` em `RetroTetris.Core/States/`
    - Implementar `StartScreenState`, `PlayingState`, `PausedState` e `GameOverState` com suas transições conforme o diagrama do design
    - _Requisitos: 1.4, 12.4, 13.1, 13.2, 13.4_

  - [x] 8.2 Escrever testes unitários para a State Machine
    - Testar todas as transições válidas (StartScreen→Playing, Playing→Paused, Playing→GameOver, Paused→Playing, GameOver→Playing)
    - Verificar que `Enter` e `Exit` são chamados corretamente em cada transição
    - Testar que comandos inválidos em estados incorretos são ignorados
    - _Requisitos: 1.4, 13.1, 13.4_

  - [x] 8.3 Implementar `GameEngine`
    - Implementar `IGameEngine` coordenando `Board`, `BagRandomizer`, `ScoreSystem`, `LevelSystem`, `LockDelayController`, `GhostCalculator` e `SRSRotationSystem`
    - Implementar `StartNewGame()`, `Update(delta)`, `TransitionTo(state)` e todos os métodos de ação do jogador (`MoveLeft`, `MoveRight`, `SoftDrop`, `HardDrop`, `RotateClockwise`, `RotateCounterClockwise`, `Hold`, `Pause`, `Resume`)
    - Disparar os eventos Observer (`LinesCleared`, `ScoreChanged`, `LevelChanged`, `PieceLocked`, `PieceSpawned`, `GameOverOccurred`) nos momentos corretos
    - _Requisitos: 2.3, 3.4, 4.1–4.8, 5.1–5.5, 6.1–6.8, 7.2, 9.1–9.4, 11.3, 12.4_

  - [x] 8.4 Escrever property test — Propriedade 1: Movimento lateral respeita limites do board
    - **Propriedade 1: Movimento lateral respeita limites do board**
    - **Valida: Requisitos 4.1, 4.2, 4.7**

  - [x] 8.5 Escrever property test — Propriedade 3: Hard drop posiciona peça na posição mais baixa possível
    - **Propriedade 3: Hard drop posiciona peça na posição mais baixa possível**
    - **Valida: Requisito 4.4**

  - [x] 8.6 Escrever property test — Propriedade 9: Hold é bloqueado após uso até fixação da peça
    - **Propriedade 9: Hold é bloqueado após uso até fixação da peça**
    - **Valida: Requisito 9.4**

- [x] 9. Implementar `GameLoop`
  - Implementar `GameLoop` com thread própria, `Stopwatch` para delta time real, as três fases (`ProcessInput`, `Update`, `Render`) e cap de ~60 fps via `Thread.Sleep(1)`
  - Implementar `Start()` e `Stop()` com controle do flag `volatile bool _running`
  - _Requisitos: 5.1, 7.3_

- [x] 10. Checkpoint — Verificar núcleo completo com todos os testes
  - Garantir que todos os testes em `RetroTetris.Tests` passam após a implementação do `GameEngine` e `GameLoop`. Perguntar ao usuário se há dúvidas antes de prosseguir para a camada MAUI.

- [x] 11. Implementar infraestrutura MAUI — `HighScoreRepository` e `AudioService`
  - [x] 11.1 Implementar `HighScoreRepository`
    - Implementar `IHighScoreRepository` usando `FileSystem.AppDataDirectory` do MAUI e `System.Text.Json` para serializar/deserializar `HighScoreData`
    - Tratar exceções de I/O: retornar 0 ao carregar em caso de erro; logar e não propagar ao salvar
    - _Requisitos: 11.2, 11.3_

  - [x] 11.2 Escrever testes de integração para `HighScoreRepository`
    - Testar leitura e escrita com arquivo real; testar comportamento quando o arquivo não existe
    - _Requisitos: 11.2, 11.3_

  - [x] 11.3 Implementar `AudioService`
    - Implementar `IAudioService` usando `Plugin.Maui.Audio` (`IAudioManager`) para reproduzir os 6 efeitos sonoros e a trilha de fundo em loop
    - Envolver todas as chamadas em `try/catch`; definir `_audioAvailable = false` na primeira falha
    - Implementar `SetMuted(bool)` para silenciar
    - _Requisitos: 14.4, 14.5_

- [x] 12. Implementar `LayoutManager` e `GameRenderer`
  - [x] 12.1 Implementar `LayoutManager`
    - Calcular dimensões e posições do board, painéis laterais (Hold, Next, Score, Level, High Score) e células em função do tamanho da `Window`
    - Suportar orientação retrato (painéis acima/abaixo) e paisagem (painéis laterais) conforme a proporção da janela
    - _Requisitos: 16.1, 16.2, 16.3_

  - [x] 12.2 Implementar `GameRenderer` — renderização do board e peças
    - Implementar `IDrawable.Draw(ICanvas, RectF)` desenhando o board com bordas retrô, células ocupadas com cor e sombreamento, células vazias diferenciadas
    - Renderizar a `ActivePiece` com cor sólida e borda, a `GhostPiece` com opacidade reduzida ou apenas contorno, e as linhas em flash durante a animação de clear (200ms)
    - _Requisitos: 2.4, 2.5, 3.2, 3.3, 6.3, 8.1, 8.2, 8.4, 14.1, 14.3_

  - [x] 12.3 Implementar `GameRenderer` — painéis laterais e textos
    - Renderizar os painéis de Hold, Next (3 peças), Score, High Score e Level usando a fonte `PressStart2P-Regular.ttf`
    - Renderizar a `StartScreen` com o título "Retro Tetris" e botão "Start"
    - Renderizar a tela de Game Over sobreposta ao board com "Game Over" em vermelho com efeito de piscar/glitch, Score final, High Score e botões "Try Again" e "Exit"
    - Renderizar a indicação visual de pausa com board oculto
    - _Requisitos: 1.1, 1.2, 9.5, 10.1, 10.2, 10.3, 11.1, 11.4, 12.1, 12.2, 12.3, 13.1, 13.3, 14.2_

- [x] 13. Implementar `InputHandler` e conectar à `GamePage`
  - [x] 13.1 Implementar `InputHandler` para teclado (Windows)
    - Capturar `KeyDown`/`KeyUp` via handler da `Window` nativa WinUI 3
    - Mapear as teclas do `GameKey` enum para os `IGameCommand` correspondentes e enfileirá-los na `CommandQueue`
    - Implementar DAS/ARR: após 170ms pressionado, repetir movimento lateral a cada 50ms via `Update(elapsed)`
    - _Requisitos: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.8, 9.1, 13.1_

  - [x] 13.2 Implementar `InputHandler` para toque
    - Adicionar `TapGestureRecognizer` e `PanGestureRecognizer` ao `GraphicsView`
    - Mapear swipe esquerda/direita/baixo/cima e tap esquerdo/direito para os comandos correspondentes
    - _Requisitos: 15.1, 15.2, 15.3, 15.4, 15.5, 15.6_

  - [x] 13.3 Configurar `GamePage` e `StartPage`
    - Criar `StartPage.xaml` com `GraphicsView` ou layout MAUI exibindo a tela inicial
    - Criar `GamePage.xaml` com `GraphicsView` ocupando toda a área disponível
    - Conectar `GameRenderer` como `IDrawable` do `GraphicsView`
    - Registrar o `InputHandler` para eventos de teclado e toque no `GamePage`
    - _Requisitos: 1.1, 1.3_

- [x] 14. Configurar injeção de dependências e integrar todos os componentes
  - Registrar todos os serviços no `MauiProgram.cs`: `IGameEngine` → `GameEngine`, `IHighScoreRepository` → `HighScoreRepository`, `IAudioService` → `AudioService`, `CommandQueue`, `GameLoop`, `GameRenderer`, `InputHandler`, `LayoutManager`
  - Inscrever `AudioService` e `HighScoreRepository` nos eventos Observer do `GameEngine` (`LinesCleared`, `PieceLocked`, `GameOverOccurred`, etc.)
  - Inscrever `GameRenderer` em `ScoreChanged`, `LevelChanged`, `PieceSpawned` para atualizar os painéis
  - Iniciar o `GameLoop` ao navegar para `GamePage` e pará-lo ao sair
  - _Requisitos: 1.3, 1.4, 5.1, 7.4, 11.3, 12.4, 12.5, 13.4_

- [x] 15. Implementar testes de integração do ciclo de jogo
  - [x] 15.1 Escrever testes de integração em `GameCycleTests.cs`
    - Testar ciclo completo: `StartNewGame()` → sequência de comandos de movimento → `LockPiece` → `LineCleared` → `GameOver`
    - Verificar que eventos Observer são disparados na ordem correta com os parâmetros corretos
    - _Requisitos: 5.1–5.5, 6.1–6.8, 7.1–7.5_

- [x] 16. Checkpoint final — Garantir que todos os testes passam
  - Garantir que todos os testes em `RetroTetris.Tests` passam. Perguntar ao usuário se há dúvidas antes de considerar a implementação concluída.

## Notas

- Tarefas marcadas com `*` são opcionais e podem ser puladas para um MVP mais rápido
- Cada tarefa referencia os requisitos específicos para rastreabilidade
- Os checkpoints garantem validação incremental antes de avançar para a próxima camada
- Os testes de propriedade (FsCheck) validam as 10 propriedades de correção definidas no design
- Os testes unitários cobrem casos concretos, bordas e condições de erro
- `RetroTetris.Tests` não referencia `RetroTetris` — todos os testes rodam sem instanciar a UI MAUI
