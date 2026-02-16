using Cavetronic.Systems;
using Cavetronic.Systems.Client;
using nkast.Aether.Physics2D.Common;
using Raylib_cs;

namespace Cavetronic;

public class Program {
  public static void Main() {
    StartRealGameGameLoop();
  }

  static void StartClaudeCodeCaveGeneration() {
    var gameWorld = new GameWorld();

    var caveGenerationSystem = new CaveGenerationSystem(gameWorld);

    caveGenerationSystem.Init();
  }

  static void StartRealGameGameLoop() {
    var gameWorld = new GameWorld();

    var playerEntity = gameWorld.Ecs.Create(
      new StableId { Id = 1000 },
      new Player(),
      new ControlOwner { SubjectId = StableId.DefaultSpawnerId }
    );
    gameWorld.RegisterEntity(1000, playerEntity);

    var spawnerEntity = gameWorld.Ecs.Create(
      new StableId { Id = StableId.DefaultSpawnerId },
      new Position { X = -10f, Y = -60f },
      new DroneHeadSpawner { ProductionTimer = 5f },
      new ControlSubject()
    );
    gameWorld.RegisterEntity(StableId.DefaultSpawnerId, spawnerEntity);

    var spawnerPosition = gameWorld.Ecs.Get<Position>(spawnerEntity);
    gameWorld.Physics.CreateBody(new Vector2(spawnerPosition.X, spawnerPosition.Y)).CreateCircle(1, 0, new Vector2(0, -2));

    // Создаем системы в правильном порядке
    var cameraSystem = new CameraSystem(gameWorld);
    var systems = new EcsSystem[] {
      new InputSystem(gameWorld),
      new ControlInputSyncSystem(gameWorld),
      new CaveGenerationSystem(gameWorld),
      new SpawnerProductionSystem(gameWorld),
      new SpawnerControlSystem(gameWorld),
      new DroneControlSystem(gameWorld),
      new GhostControlSystem(gameWorld),
      new DroneDeathTestSystem(gameWorld),
      new ControlTransferSystem(gameWorld),
      new PhysicsSystem(gameWorld),
      new PhysicsSyncSystem(gameWorld),
      cameraSystem,
      new CameraFollowSystem(gameWorld, cameraSystem),
      new CameraStartSystem(gameWorld, cameraSystem),
      new DebugCollidersRenderSystem(gameWorld),
      new CameraEndSystem(gameWorld),
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

  static void StartProceduralGenerationLookupGameLoop() {
    var gameWorld = new GameWorld();

    // Создаем entity с камерой
    gameWorld.Ecs.Create(
      new Position { X = 32f, Y = 32f }, // Центр мира (chunk 0,0)
      new CameraTarget()
    );

    // Создаем системы в правильном порядке
    var cameraSystem = new CameraSystem(gameWorld);
    var systems = new EcsSystem[] {
      new CaveGenerationSystem(gameWorld),
      new PhysicsSystem(gameWorld),
      cameraSystem,
      new CameraControlSystem(gameWorld, cameraSystem),
      new CameraStartSystem(gameWorld, cameraSystem),
      new DebugCollidersRenderSystem(gameWorld),
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
