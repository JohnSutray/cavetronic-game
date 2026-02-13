using Cavetronic.Generation;
using Cavetronic.Systems;
using Raylib_cs;

namespace Cavetronic;

public class Program {
  public static void Main() {
    var gameWorld = new GameWorld();

    // Создаем entity с камерой
    var cameraEntity = gameWorld.Ecs.Create(
      new Position { X = 10f, Y = 8f }, // Центр мира (chunk 0,0)
      new CameraTarget()
    );

    // Создаем системы в правильном порядке
    var cameraSystem = new CameraSystem(gameWorld);
    var systems = new EcsSystem[] {
      new PhysicsSystem(gameWorld),
      new TerrainSystem(gameWorld),
      cameraSystem,
      new CameraControlSystem(gameWorld, cameraSystem),
      new CameraStartSystem(gameWorld, cameraSystem),
      new DebugRenderSystem(gameWorld),
      new CameraEndSystem(gameWorld),
      new ScreenshotSystem(gameWorld),
    };

    foreach (var system in systems) {
      system.Init();
    }

    Raylib.InitWindow(2000, 1000, "Cavetronic");
    Raylib.SetTargetFPS(120);

    while (!Raylib.WindowShouldClose()) {
      var dt = Raylib.GetFrameTime();

      Raylib.BeginDrawing();
      Raylib.ClearBackground(Color.Black);

      foreach (var system in systems) {
        system.Tick(dt);
      }

      Raylib.DrawFPS(10, 10);
      Raylib.EndDrawing();
    }

    Raylib.CloseWindow();
  }
}