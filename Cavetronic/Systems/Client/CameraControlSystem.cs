using System.Numerics;
using Arch.Core;
using Raylib_cs;

namespace Cavetronic.Systems.Client;

public class CameraControlSystem(GameWorld gameWorld, CameraSystem cameraSystem) : EcsSystem(gameWorld) {
  private readonly QueryDescription _cameraTargetQuery = new QueryDescription().WithAll<CameraTarget, Position>();

  private bool _isDragging;
  private Vector2 _lastMousePos;

  public override void Tick(float dt) {
    var mousePos = Raylib.GetMousePosition();

    // Начало перетаскивания (правая кнопка мыши)
    if (Raylib.IsMouseButtonPressed(MouseButton.Right)) {
      _isDragging = true;
      _lastMousePos = mousePos;
    }

    // Окончание перетаскивания
    if (Raylib.IsMouseButtonReleased(MouseButton.Right)) {
      _isDragging = false;
    }

    // Перетаскивание камеры
    if (_isDragging) {
      var delta = mousePos - _lastMousePos;
      _lastMousePos = mousePos;

      // Перемещаем entity с CameraTarget (инвертируем, как в Figma)
      GameWorld.Ecs.Query(in _cameraTargetQuery, (ref Position pos) => {
        // Делим на Zoom камеры (который уже включает Scale), инвертируем направление
        pos.X -= delta.X / cameraSystem.Camera.Zoom;
        pos.Y -= delta.Y / cameraSystem.Camera.Zoom;
      });
    }
  }
}