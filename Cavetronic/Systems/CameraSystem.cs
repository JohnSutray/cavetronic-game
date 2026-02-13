using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using Raylib_cs;

namespace Cavetronic.Systems;

public class CameraSystem : EcsSystem {
  public Camera2D Camera;
  private const float Scale = 50f; // 1 метр = 50 пикселей (базовый масштаб)
  private const float MinZoom = 0.1f; // Минимальный зум (далеко)
  private const float MaxZoom = 10f; // Максимальный зум (близко)

  private readonly QueryDescription _cameraTargetQuery = new QueryDescription().WithAll<CameraTarget, Position>();
  private float _userZoom = 1f; // Пользовательский зум (колесико мыши)

  public CameraSystem(GameWorld gameWorld) : base(gameWorld) {
    Camera = new Camera2D {
      Target = new Vector2(8f, 6f), // Координаты в метрах
      Offset = new Vector2(1000f, 500f), // Центр экрана (половина от 2000x1000)
      Rotation = 0f,
      Zoom = Scale * _userZoom // Scale встроен в Zoom
    };
  }

  public override void Tick(float dt) {
    // Находим entity с CameraTarget и обновляем камеру (в метрах)
    GameWorld.Ecs.Query(in _cameraTargetQuery, (ref Position pos) => {
      Camera.Target = new Vector2(pos.X, pos.Y);
    });

    // Управление зумом колесиком мыши
    float wheel = Raylib.GetMouseWheelMove();
    
    if (wheel != 0) {
      _userZoom += wheel * 0.1f;
      _userZoom = Math.Clamp(_userZoom, MinZoom, MaxZoom);
      Camera.Zoom = Scale * _userZoom;
    }
  }
}
