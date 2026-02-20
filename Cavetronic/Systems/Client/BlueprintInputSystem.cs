using System.Numerics;
using Arch.Core;
using Raylib_cs;

namespace Cavetronic.Systems.Client;

// Клиентская система: читает состояние мыши и клавиатуры, записывает
// ControlSubjectInput<T> с Payload на сущность игрока.
// Запускается только в BlueprintEditor-петле.
public class BlueprintInputSystem(GameWorld gameWorld, CameraSystem cameraSystem)
  : EcsSystem(gameWorld) {
  private readonly QueryDescription _playersQuery = new QueryDescription().WithAll<Player>();

  private Vector2 _lmbLastWorldPos;
  private bool _lmbWasDown;

  private Vector2 _rmbLastScreenPos;
  private bool _rmbWasDown;
  private bool _rmbMoved;
  private const float RmbDragThresholdPx = 4f;

  public override void Tick(float dt) {
    var mouseScreen = Raylib.GetMousePosition();
    var mouseWorld = Raylib.GetScreenToWorld2D(mouseScreen, cameraSystem.Camera);

    var lmbDown = Raylib.IsMouseButtonDown(MouseButton.Left);
    var lmbPressed = Raylib.IsMouseButtonPressed(MouseButton.Left);
    var rmbDown = Raylib.IsMouseButtonDown(MouseButton.Right);
    var rmbPressed = Raylib.IsMouseButtonPressed(MouseButton.Right);
    var rmbReleased = Raylib.IsMouseButtonReleased(MouseButton.Right);
    var shiftDown = Raylib.IsKeyDown(KeyboardKey.LeftShift)
                    || Raylib.IsKeyDown(KeyboardKey.RightShift);
    var fPressed = Raylib.IsKeyPressed(KeyboardKey.F);

    if (lmbPressed) {
      _lmbLastWorldPos = mouseWorld;
    }

    if (rmbPressed) {
      _rmbLastScreenPos = mouseScreen;
      _rmbMoved = false;
    }

    if (rmbDown && !rmbPressed) {
      var screenDelta = mouseScreen - _rmbLastScreenPos;

      if (screenDelta.Length() > RmbDragThresholdPx) {
        _rmbMoved = true;
      }
    }

    GameWorld.Ecs.Query(in _playersQuery, (Entity entity) => {
      // CursorInput: каждый фрейм
      SetPayloadInput(
        entity,
        new CursorInput {
          WorldX = mouseWorld.X,
          WorldY = mouseWorld.Y
        },
        active: true
      );

      // ShiftModifier
      SetPayloadInput<ShiftModifier>(entity, default, shiftDown);

      // LMB drag
      if (lmbDown) {
        SetPayloadInput(entity, new CursorLeftMoveAction {
          StartX = _lmbLastWorldPos.X,
          StartY = _lmbLastWorldPos.Y,
          EndX = mouseWorld.X,
          EndY = mouseWorld.Y
        }, active: true);
      }
      else {
        RemoveInput<CursorLeftMoveAction>(entity);
      }

      // RMB click — эмитируется один фрейм при отпускании без drag
      if (rmbReleased && !_rmbMoved) {
        SetPayloadInput(entity, new CursorRightClickAction {
          WorldX = mouseWorld.X,
          WorldY = mouseWorld.Y
        }, active: true);
      }
      else if (!rmbDown) {
        RemoveInput<CursorRightClickAction>(entity);
      }

      // InputExclusive: F = взять эксклюзивный контроль
      if (fPressed) {
        var playerId = GameWorld.Ecs.Has<StableId>(entity)
          ? GameWorld.Ecs.Get<StableId>(entity).Id
          : StableId.LocalTestUser;

        SetPayloadInput(entity, new InputExclusive { OwnerStableId = playerId }, active: true);
      }
      else {
        RemoveInput<InputExclusive>(entity);
      }
    });

    if (lmbDown) {
      _lmbLastWorldPos = mouseWorld;
    }

    _lmbWasDown = lmbDown;
    _rmbWasDown = rmbDown;
  }

  private void SetPayloadInput<T>(Entity entity, T payload, bool active) where T : struct {
    if (!active) {
      RemoveInput<T>(entity);
      return;
    }

    if (!GameWorld.Ecs.Has<ControlSubjectInput<T>>(entity)) {
      GameWorld.Ecs.Add(entity, new ControlSubjectInput<T> {
        Active = true,
        Payload = payload
      });
    }
    else {
      ref var input = ref GameWorld.Ecs.Get<ControlSubjectInput<T>>(entity);
      input.Active = true;
      input.Payload = payload;
    }
  }

  private void RemoveInput<T>(Entity entity) where T : struct {
    if (GameWorld.Ecs.Has<ControlSubjectInput<T>>(entity)) {
      GameWorld.Ecs.Remove<ControlSubjectInput<T>>(entity);
    }
  }
}