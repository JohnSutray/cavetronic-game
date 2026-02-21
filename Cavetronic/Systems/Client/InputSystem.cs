using System.Numerics;
using Arch.Buffer;
using Arch.Core;
using Raylib_cs;

namespace Cavetronic.Systems.Client;

public class InputSystem(GameWorld gameWorld, CameraSystem? cameraSystem = null, float tickRate = 30f)
  : EcsSystem(gameWorld) {

  private readonly QueryDescription _playersQuery = new QueryDescription().WithAll<Player>();
  private readonly float _tickInterval = 1f / tickRate;
  private readonly CommandBuffer _buffer = new();
  private float _accumulator;

  // Mouse tracking — используется только когда cameraSystem задан
  private Vector2 _lmbLastWorldPos;
  private Vector2 _rmbLastScreenPos;
  private bool _rmbMoved;
  private bool _rmbClickLatched; // лататный клик: читаем каждый Raylib-фрейм, потребляем на каждом тике
  private const float RmbDragThresholdPx = 4f;

  public override void Tick(float dt) {
    // RMB-состояние читаем каждый Raylib-фрейм, чтобы не пропустить одноразовые события
    // (IsMouseButtonReleased = true только один фрейм; при tick rate < fps событие иначе теряется).
    if (cameraSystem != null) {
      var mouseScreen = Raylib.GetMousePosition();
      var rmbPressed = Raylib.IsMouseButtonPressed(MouseButton.Right);
      var rmbDown = Raylib.IsMouseButtonDown(MouseButton.Right);
      var rmbReleased = Raylib.IsMouseButtonReleased(MouseButton.Right);

      if (rmbPressed) {
        _rmbLastScreenPos = mouseScreen;
        _rmbMoved = false;
      }

      if (rmbDown && !rmbPressed) {
        if ((mouseScreen - _rmbLastScreenPos).Length() > RmbDragThresholdPx) {
          _rmbMoved = true;
        }
      }

      if (rmbReleased && !_rmbMoved) {
        _rmbClickLatched = true;
      }
    }

    _accumulator += dt;

    if (_accumulator < _tickInterval) {
      return;
    }

    _accumulator -= _tickInterval;

    // Keyboard state — читаем до query, чтобы не захватывать Raylib-вызовы в лямбду
    var spaceDown = Raylib.IsKeyDown(KeyboardKey.Space);
    var eDown = Raylib.IsKeyDown(KeyboardKey.E);
    var aDown = Raylib.IsKeyDown(KeyboardKey.A);
    var dDown = Raylib.IsKeyDown(KeyboardKey.D);

    // Mouse state
    var mouseWorld = Vector2.Zero;
    var lmbDown = false;
    var shiftDown = false;
    var lmbLastWorldPos = _lmbLastWorldPos;

    // Потребляем залатченный RMB-клик
    var rmbClick = _rmbClickLatched;
    _rmbClickLatched = false;

    if (cameraSystem != null) {
      var mouseScreen = Raylib.GetMousePosition();
      mouseWorld = Raylib.GetScreenToWorld2D(mouseScreen, cameraSystem.Camera);

      lmbDown = Raylib.IsMouseButtonDown(MouseButton.Left);
      var lmbPressed = Raylib.IsMouseButtonPressed(MouseButton.Left);
      shiftDown = Raylib.IsKeyDown(KeyboardKey.LeftShift)
        || Raylib.IsKeyDown(KeyboardKey.RightShift);

      if (lmbPressed) {
        _lmbLastWorldPos = mouseWorld;
        lmbLastWorldPos = mouseWorld;
      }
    }

    GameWorld.Ecs.Query(in _playersQuery, (Entity entity) => {
      SyncKeyToMarker<Action1>(_buffer, entity, spaceDown);
      SyncKeyToMarker<Action2>(_buffer, entity, eDown);
      SyncKeyToMarker<MoveLeft>(_buffer, entity, aDown);
      SyncKeyToMarker<MoveRight>(_buffer, entity, dDown);

      if (cameraSystem == null) {
        return;
      }

      SyncPayloadInput(_buffer, entity, new CursorInput {
        WorldX = mouseWorld.X,
        WorldY = mouseWorld.Y
      }, active: true);

      SyncPayloadInput<ShiftModifier>(_buffer, entity, default, shiftDown);

      SyncPayloadInput(_buffer, entity, new CursorLeftMoveAction {
        StartX = lmbLastWorldPos.X,
        StartY = lmbLastWorldPos.Y,
        EndX = mouseWorld.X,
        EndY = mouseWorld.Y
      }, lmbDown);

      SyncPayloadInput(_buffer, entity, new CursorRightClickAction {
        WorldX = mouseWorld.X,
        WorldY = mouseWorld.Y
      }, rmbClick);
    });

    _buffer.Playback(GameWorld.Ecs);

    // Обновляем _lmbLastWorldPos после query — в следующем тике Start = текущий End
    if (cameraSystem != null && lmbDown) {
      _lmbLastWorldPos = mouseWorld;
    }
  }

  private void SyncKeyToMarker<T>(CommandBuffer buffer, Entity entity, bool active) where T : struct {
    switch (active) {
      case true when !GameWorld.Ecs.Has<ControlSubjectInput<T>>(entity):
        buffer.Add(in entity, new ControlSubjectInput<T> { Active = true });
        break;
      case false when GameWorld.Ecs.Has<ControlSubjectInput<T>>(entity):
        buffer.Remove<ControlSubjectInput<T>>(in entity);
        break;
    }
  }

  private void SyncPayloadInput<T>(CommandBuffer buffer, Entity entity, T payload, bool active)
    where T : struct {
    if (active) {
      if (!GameWorld.Ecs.Has<ControlSubjectInput<T>>(entity)) {
        buffer.Add(in entity, new ControlSubjectInput<T> { Active = true, Payload = payload });
      }
      else {
        ref var input = ref GameWorld.Ecs.Get<ControlSubjectInput<T>>(entity);
        input.Active = true;
        input.Payload = payload;
      }
    }
    else if (GameWorld.Ecs.Has<ControlSubjectInput<T>>(entity)) {
      buffer.Remove<ControlSubjectInput<T>>(in entity);
    }
  }
}
