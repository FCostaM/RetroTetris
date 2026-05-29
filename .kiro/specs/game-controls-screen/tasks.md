# Plano de Implementação: Tela de Controles (game-controls-screen)

## Visão Geral

Implementação incremental da tela de controles do RetroTetris, seguindo a arquitetura existente: State Machine, Command Pattern e renderização via `IDrawable`. As tarefas cobrem Core → Presentation → Testes, garantindo que cada camada seja integrada antes de avançar.

## Tarefas

- [x] 1. Adicionar `GameKey.H` ao enum e criar `ControlsScreenState`
  - [x] 1.1 Adicionar `H` ao enum `GameKey` em `RetroTetris.Core/Interfaces/IInputHandler.cs`
    - Localizar o enum `GameKey` e inserir o valor `H` após os valores existentes
    - _Requisitos: 1.4, 2.4_

  - [x] 1.2 Criar `RetroTetris.Core/States/ControlsScreenState.cs`
    - Implementar a classe com propriedade `PreviousState { get; }` do tipo `IGameState`
    - Construtor recebe `IGameState previousState` e lança `ArgumentException` se `previousState is ControlsScreenState`
    - Implementar `Enter`, `Update` e `Exit` como no-op (o game loop permanece suspenso automaticamente)
    - _Requisitos: 1.3, 2.3, 4.3, 4.5_

  - [x] 1.3 Escrever testes unitários para `ControlsScreenState`
    - Testar que o construtor aceita `StartScreenState` e `PausedState` como `previousState`
    - Testar que o construtor lança `ArgumentException` ao receber outro `ControlsScreenState`
    - Testar que `PreviousState` retorna exatamente a instância passada ao construtor
    - Adicionar em `RetroTetris.Tests/Unit/StateMachineTests.cs` ou novo arquivo `ControlsScreenStateTests.cs`
    - _Requisitos: 4.3_

- [x] 2. Estender `IGameEngine` e implementar em `GameEngine`
  - [x] 2.1 Adicionar `ShowControls()` e `HideControls()` à interface `RetroTetris.Core/Interfaces/IGameEngine.cs`
    - Inserir as duas assinaturas após `Resume()` na seção de player actions
    - Atualizar o stub `TrackingEngine` em `RetroTetris.Tests/Unit/StateMachineTests.cs` para implementar os novos métodos (no-op)
    - _Requisitos: 1.3, 2.3, 4.1, 4.2_

  - [x] 2.2 Implementar `ShowControls()` e `HideControls()` em `RetroTetris.Core/Engine/GameEngine.cs`
    - `ShowControls()`: verifica `_currentState is StartScreenState or PausedState` e chama `TransitionTo(new ControlsScreenState(_currentState))`
    - `HideControls()`: verifica `_currentState is ControlsScreenState css` e chama `TransitionTo(css.PreviousState)`
    - _Requisitos: 1.3, 2.3, 4.1, 4.2, 4.3, 4.4_

  - [x] 2.3 Escrever testes unitários para `ShowControls()` e `HideControls()`
    - `ShowControls_FromStartScreen_TransitionsToControlsScreenState`
    - `ShowControls_FromPausedState_TransitionsToControlsScreenState`
    - `HideControls_FromControlsScreen_ReturnsToStartScreen`
    - `HideControls_FromControlsScreen_ReturnsToPausedState`
    - `ShowControls_InPlayingState_IsNoOp`
    - `ShowControls_InGameOverState_IsNoOp`
    - `ShowControls_InControlsScreenState_IsNoOp`
    - `HideControls_OutsideControlsScreenState_IsNoOp`
    - Adicionar em `RetroTetris.Tests/Unit/` (novo arquivo `ControlsEngineTests.cs`)
    - _Requisitos: 1.3, 2.3, 4.1, 4.2, 4.3, 4.4_

  - [x] 2.4 Escrever testes de propriedade para `ShowControls`/`HideControls` (Propriedades 1–6)
    - **Propriedade 1: ShowControls() preserva PreviousState correto**
      - Gera `StartScreenState` ou `PausedState` aleatoriamente; verifica que `CurrentState` é `ControlsScreenState` com `PreviousState` igual à instância de origem
      - **Valida: Requisitos 1.3, 2.3**
    - **Propriedade 2: HideControls() retorna exatamente ao PreviousState**
      - Cria `ControlsScreenState` com `PreviousState` aleatório; verifica `Assert.Same(previousState, engine.CurrentState)` após `HideControls()`
      - **Valida: Requisitos 4.1, 4.2**
    - **Propriedade 3: ShowControls + HideControls preserva estado de jogo completo**
      - Gera estado de jogo aleatório (board, score, level, hold piece); captura snapshot; executa `ShowControls()` + `HideControls()`; verifica que snapshot é idêntico
      - **Valida: Requisitos 4.3, 4.5**
    - **Propriedade 4: Update() é no-op em ControlsScreenState**
      - Gera N iterações e delta aleatórios; verifica que `Board`, `Score`, `Level`, `ActivePiece`, `HoldPiece` não mudam após N chamadas a `Update(delta)`
      - **Valida: Requisito 4.5**
    - **Propriedade 5: ShowControls() é no-op em estados inválidos**
      - Gera `PlayingState` ou `GameOverState`; verifica que `CurrentState` não muda após `ShowControls()`
      - **Valida: Requisitos 1.3, 2.3** (por contraposição)
    - **Propriedade 6: ControlsScreenState não pode ser aninhado**
      - Verifica que `new ControlsScreenState(controlsScreenState)` lança `ArgumentException`; verifica que `ShowControls()` em `ControlsScreenState` é no-op
      - **Valida: Requisito 4.3**
    - Criar `RetroTetris.Tests/Properties/ControlsScreenProperties.cs` usando `FsCheck.Xunit` com `[Property]`
    - _Requisitos: 1.3, 2.3, 4.1, 4.2, 4.3, 4.5_

- [x] 3. Checkpoint — Verificar Core
  - Garantir que todos os testes do projeto `RetroTetris.Tests` passam antes de avançar para a camada de Presentation. Perguntar ao usuário se houver dúvidas.

- [x] 4. Criar os comandos `ShowControlsCommand`, `HideControlsCommand` e `ToggleControlsCommand`
  - [x] 4.1 Criar `RetroTetris.Core/Commands/ShowControlsCommand.cs`
    - `Execute(IGameEngine engine)`: chama `engine.ShowControls()` apenas se `engine.CurrentState is StartScreenState or PausedState`
    - _Requisitos: 1.3, 2.3_

  - [x] 4.2 Criar `RetroTetris.Core/Commands/HideControlsCommand.cs`
    - `Execute(IGameEngine engine)`: chama `engine.HideControls()` apenas se `engine.CurrentState is ControlsScreenState`
    - _Requisitos: 4.1, 4.2_

  - [x] 4.3 Criar `RetroTetris.Core/Commands/ToggleControlsCommand.cs`
    - `Execute(IGameEngine engine)`: chama `engine.HideControls()` se `CurrentState is ControlsScreenState`; chama `engine.ShowControls()` se `CurrentState is StartScreenState or PausedState`; no-op em qualquer outro estado
    - Este comando será mapeado para `GameKey.H` no `InputHandler`
    - _Requisitos: 1.4, 2.4, 4.2_

  - [x] 4.4 Atualizar `RetroTetris.Core/Commands/TogglePauseCommand.cs`
    - Adicionar `else if (e.CurrentState is ControlsScreenState) e.HideControls();` ao método `Execute()`
    - Isso garante que Escape e P fecham a Controls_Screen quando ela está ativa
    - _Requisitos: 4.2_

  - [x] 4.5 Escrever testes unitários para os novos comandos
    - `ShowControlsCommand_FromStartScreen_CallsShowControls`
    - `ShowControlsCommand_FromPausedState_CallsShowControls`
    - `ShowControlsCommand_FromPlayingState_IsNoOp`
    - `ShowControlsCommand_FromControlsScreenState_IsNoOp`
    - `HideControlsCommand_FromControlsScreenState_CallsHideControls`
    - `HideControlsCommand_FromOtherState_IsNoOp`
    - `ToggleControlsCommand_FromStartScreen_CallsShowControls`
    - `ToggleControlsCommand_FromPausedState_CallsShowControls`
    - `ToggleControlsCommand_FromControlsScreenState_CallsHideControls`
    - `ToggleControlsCommand_FromPlayingState_IsNoOp`
    - `TogglePauseCommand_FromControlsScreenState_CallsHideControls`
    - Adicionar em `RetroTetris.Tests/Unit/CommandTests.cs`
    - _Requisitos: 1.3, 1.4, 2.3, 2.4, 4.1, 4.2_

- [x] 5. Atualizar `InputHandler` e `GamePage` (Windows keyboard)
  - [x] 5.1 Mapear `GameKey.H → new ToggleControlsCommand()` em `RetroTetris/Presentation/InputHandler.cs`
    - Adicionar a entrada `GameKey.H => new ToggleControlsCommand(),` ao switch em `MapKeyToCommand()`
    - _Requisitos: 1.4, 2.4, 4.2_

  - [x] 5.2 Mapear `VirtualKey.H → GameKey.H` em `RetroTetris/Pages/GamePage.xaml.cs`
    - Adicionar `VirtualKey.H => GameKey.H,` ao switch em `MapVirtualKey()` dentro do bloco `#if WINDOWS`
    - _Requisitos: 1.4, 2.4_

- [x] 6. Implementar renderização no `GameRenderer`
  - [x] 6.1 Adicionar campos de dados e atualizar `Draw()` em `RetroTetris/Presentation/GameRenderer.cs`
    - Declarar as constantes `_keyboardEntries` e `_touchEntries` como arrays estáticos de `(string key, string action)` com todas as entradas definidas no design
    - Atualizar o método `Draw()` para tratar `ControlsScreenState`:
      - Se `state is ControlsScreenState { PreviousState: StartScreenState }`: chamar `DrawStartScreen()` seguido de `DrawControlsOverlay()`
      - Se `state is ControlsScreenState { PreviousState: PausedState }`: renderizar o jogo normalmente, chamar `DrawPauseOverlay()` e depois `DrawControlsOverlay()`
    - _Requisitos: 3.1, 3.2, 5.1_

  - [x] 6.2 Implementar `DrawControlsButton()` e chamá-lo dentro de `DrawStartScreen()`
    - Botão posicionado em `btnY = bounds.Height * 0.68f` (abaixo do Start em `0.58f`)
    - Dimensões: `btnW = bounds.Width * 0.3f`, `btnH = bounds.Height * 0.08f`
    - Estilo: fundo `ButtonBg`, borda `#888888`, texto `"CONTROLS"` em `PixelFont`, cor `#888888`
    - _Requisitos: 1.1, 1.2, 5.2, 5.3, 5.4_

  - [x] 6.3 Implementar `DrawControlsButtonOnPause()` e chamá-lo dentro de `DrawPauseOverlay()`
    - Botão posicionado em `centerY = r.Y + r.Height * 0.62f` (abaixo de "PAUSED")
    - Dimensões: `btnW = r.Width * 0.7f`, `btnH = Math.Max(20, r.Height * 0.09f)`
    - Estilo consistente com os demais botões do overlay de pausa
    - _Requisitos: 2.1, 2.2, 5.2, 5.3, 5.4_

  - [x] 6.4 Implementar `DrawCommandEntries()`, `DrawControlsOverlay()` e `DrawBackButton()`
    - `DrawCommandEntries(ICanvas, RectF, (string key, string action)[], float startY)`: renderiza cada entrada com rótulo alinhado à esquerda (ciano `#00F0F0`) e descrição alinhada à direita (texto claro `#EEEEEE`), fonte `PixelFont`
    - `DrawControlsOverlay(ICanvas, RectF)`: fundo semitransparente `OverlayBg`, borda dupla ciano, título "CONTROLS", separadores, seção de teclado, separador, subtítulo "TOUCH", seção de toque, botão BACK
    - `DrawBackButton(ICanvas, RectF)`: botão posicionado em `centerY = boardRect.Y + boardRect.Height * 0.88f`, dimensões `btnW = boardRect.Width * 0.5f`
    - _Requisitos: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 5.1, 5.2, 5.3, 5.4_

- [x] 7. Implementar hit-testing e `OnTapped()` no `GamePage`
  - [x] 7.1 Adicionar `IsControlsButtonHit()`, `IsControlsButtonHitOnPause()` e `IsBackButtonHit()` em `RetroTetris/Pages/GamePage.xaml.cs`
    - `IsControlsButtonHit()`: espelha as coordenadas de `DrawControlsButton()` — `btnY = h * 0.68f`
    - `IsControlsButtonHitOnPause()`: espelha `DrawControlsButtonOnPause()` — `centerY = r.Y + r.Height * 0.62f`
    - `IsBackButtonHit()`: espelha `DrawBackButton()` — `centerY = r.Y + r.Height * 0.88f`
    - _Requisitos: 1.3, 2.3, 4.1_

  - [x] 7.2 Atualizar `OnTapped()` em `RetroTetris/Pages/GamePage.xaml.cs`
    - Adicionar bloco para `ControlsScreenState` no início do método (antes de `StartScreenState`): se `IsBackButtonHit()` → `_engine.HideControls()`; retornar
    - Adicionar `else if (IsControlsButtonHit(tapX, tapY)) _engine.ShowControls();` no bloco de `StartScreenState`
    - Adicionar bloco para `PausedState`: se `IsControlsButtonHitOnPause()` → `_engine.ShowControls()`; retornar (sem rotação durante pausa)
    - _Requisitos: 1.3, 2.3, 4.1_

- [x] 8. Checkpoint Final — Garantir que todos os testes passam
  - Garantir que todos os testes do projeto `RetroTetris.Tests` passam. Perguntar ao usuário se houver dúvidas.

- [x] 9. Testes de propriedade — Layout de Command_Entry (Propriedade 7)
  - [x] 9.1 Escrever teste de propriedade para layout de Command_Entry
    - **Propriedade 7: Rótulo de tecla sempre à esquerda da descrição da ação**
    - Gera dimensões de canvas aleatórias (`width` entre 200–800, `height` entre 400–1200)
    - Para cada entrada em `_keyboardEntries` e `_touchEntries`, verifica que a coordenada X do rótulo (`leftX = boardRect.X + boardRect.Width * 0.08f`) é estritamente menor que a coordenada X da descrição (`rightX = boardRect.X + boardRect.Width * 0.92f`)
    - Adicionar em `RetroTetris.Tests/Properties/ControlsScreenProperties.cs`
    - **Valida: Requisito 3.5**

## Notas

- Tarefas marcadas com `*` são opcionais e podem ser puladas para um MVP mais rápido
- Cada tarefa referencia requisitos específicos para rastreabilidade
- Os checkpoints garantem validação incremental antes de avançar entre camadas
- O projeto de testes usa **FsCheck** (já configurado em `RetroTetris.Tests.csproj`) com o atributo `[Property]` do `FsCheck.Xunit`
- As coordenadas de hit-testing no `GamePage` devem espelhar exatamente as coordenadas de renderização do `GameRenderer` para garantir consistência
- `TogglePauseCommand` (Escape/P) e `ToggleControlsCommand` (H) cobrem juntos todos os caminhos de fechamento da Controls_Screen via teclado (Requisito 4.2)
