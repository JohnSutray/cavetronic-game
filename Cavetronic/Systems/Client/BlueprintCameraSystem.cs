using System.Numerics;
using Arch.Core;
using Raylib_cs;

namespace Cavetronic.Systems.Client;

// Клиентская система: RMB drag → панорамирование камеры (Figma-стиль).
// Зум — уже обрабатывается в CameraSystem через колёсико мыши.
public class BlueprintCameraSystem(GameWorld gameWorld, CameraSystem cameraSystem)
  : EcsSystem(gameWorld) {

  private readonly QueryDescription _cameraTargetQuery =
    new QueryDescription().WithAll<CameraTarget, Position>();

  private bool _isDragging;
  private Vector2 _lastMousePos;

  public override void Tick(float dt) {
    var mousePos = Raylib.GetMousePosition();

    if (Raylib.IsMouseButtonPressed(MouseButton.Right)) {
      _isDragging = true;
      _lastMousePos = mousePos;
    }

    if (Raylib.IsMouseButtonReleased(MouseButton.Right)) {
      _isDragging = false;
    }

    if (!_isDragging) {
      return;
    }

    var delta = mousePos - _lastMousePos;
    _lastMousePos = mousePos;

    GameWorld.Ecs.Query(in _cameraTargetQuery, (ref Position pos) => {
      pos.X -= delta.X / cameraSystem.Camera.Zoom;
      pos.Y -= delta.Y / cameraSystem.Camera.Zoom;
    });
  }
}
