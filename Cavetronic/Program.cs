using Cavetronic.Systems;
using Cavetronic.Systems.Client;
using ImGuiNET;
using nkast.Aether.Physics2D.Common;
using Raylib_cs;
using rlImGui_cs;

namespace Cavetronic;

public class Program {
  public static void Main() {
    StartBlueprintEditorGameLoop();
  }

  static void StartBlueprintEditorGameLoop() {
    const int BlueprintEntityId = 100;
    const int V0Id = 200;
    const int V1Id = 201;
    const int V2Id = 202;

    var gameWorld = new GameWorld();

    // Игрок
    var playerEntity = gameWorld.Ecs.Create(
      new StableId { Id = StableId.LocalTestUser },
      new Player(),
      new ControlOwner { SubjectId = BlueprintEntityId }
    );
    gameWorld.RegisterEntity(StableId.LocalTestUser, playerEntity);

    // Начальный треугольник: три вершины
    var v0 = gameWorld.Ecs.Create(new StableId { Id = V0Id }, new BlueprintVertex { X = 0f, Y = -2f });
    var v1 = gameWorld.Ecs.Create(new StableId { Id = V1Id }, new BlueprintVertex { X = -2f, Y = 1f });
    var v2 = gameWorld.Ecs.Create(new StableId { Id = V2Id }, new BlueprintVertex { X = 2f, Y = 1f });
    gameWorld.RegisterEntity(V0Id, v0);
    gameWorld.RegisterEntity(V1Id, v1);
    gameWorld.RegisterEntity(V2Id, v2);

    // Blueprint-сущность: ControlSubject + BlueprintMesh на одной сущности,
    // чтобы запросы (Blueprint, BlueprintMesh, ControlSubjectInput<T>) совпадали.
    var blueprintEntity = gameWorld.Ecs.Create(
      new StableId { Id = BlueprintEntityId },
      new Blueprint(),
      new ControlSubject(),
      new BlueprintMesh { Triangles = [V0Id, V1Id, V2Id] }
    );
    gameWorld.RegisterEntity(BlueprintEntityId, blueprintEntity);

    // CameraTarget
    gameWorld.Ecs.Create(new CameraTarget(), new Position { X = 0f, Y = 0f });

    var cameraSystem = new CameraSystem(gameWorld);
    var systems = new EcsSystem[] {
      new BlueprintInputSystem(gameWorld, cameraSystem),
      new BlueprintInputSyncSystem(gameWorld),
      new ControlTransferSystem(gameWorld),
      new BlueprintCursorSystem(gameWorld),
      new BlueprintVertexSelectSystem(gameWorld),
      new BlueprintVertexMoveSystem(gameWorld),
      new BlueprintVertexDeleteSystem(gameWorld),
      cameraSystem,
      new BlueprintCameraSystem(gameWorld, cameraSystem),
      new CameraStartSystem(gameWorld, cameraSystem),
      new BlueprintRenderSystem(gameWorld),
      new CameraEndSystem(gameWorld),
    };

    foreach (var system in systems) {
      system.Init();
    }

    Raylib.InitWindow(1600, 900, "Cavetronic — Blueprint Editor");
    Raylib.SetTargetFPS(120);

    while (!Raylib.WindowShouldClose()) {
      gameWorld.Tick++;
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

  static void StartClaudeCodeCaveGeneration() {
    var gameWorld = new GameWorld();

    var caveGenerationSystem = new CaveGenerationSystem(gameWorld);

    caveGenerationSystem.Init();
  }
  
  private static ImGuiWindowFlags _flags = ImGuiWindowFlags.NoTitleBar | 
  ImGuiWindowFlags.NoResize | 
  ImGuiWindowFlags.NoMove | 
  ImGuiWindowFlags.NoBackground |
  ImGuiWindowFlags.NoScrollbar;

  private static string _inputValue = "";

  static void StartRealGameGameLoop() {
    var gameWorld = new GameWorld();

    var playerEntity = gameWorld.Ecs.Create(
      new StableId { Id = StableId.LocalTestUser },
      new Player(),
      new ControlOwner { SubjectId = StableId.DefaultSpawnerId }
    );
    gameWorld.RegisterEntity(StableId.LocalTestUser, playerEntity);
    gameWorld.Nicknames[StableId.LocalTestUser] = "Player 1";

    var spawnerEntity = gameWorld.Ecs.Create(
      new StableId { Id = StableId.DefaultSpawnerId },
      new Position { X = -10f, Y = -60f },
      new DroneHeadSpawner { ProductionTimer = 5f },
      new ControlSubject(),
      new ControlSubjectInputDescriptor<Action1>(),
      new ControlSubjectInputDescriptor<Action2>()
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
      new NicknameRenderSystem(gameWorld, cameraSystem),
    };

    foreach (var system in systems) {
      system.Init();
    }

    Raylib.InitWindow(2000, 1000, "Cavetronic");
    
    rlImGui.Setup();
    Raylib.SetTargetFPS(120);

    while (!Raylib.WindowShouldClose()) {
      gameWorld.Tick++;
      var dt = Raylib.GetFrameTime();

      Raylib.BeginDrawing();
      Raylib.ClearBackground(Color.Black);

      foreach (var system in systems) {
        system.Tick(dt);
      }
      
      rlImGui.Begin();

      ImGui.Begin("Overlay");
      
      ImGui.SetNextItemWidth(200); 
      ImGui.SetWindowFontScale(2);
      ImGui.InputText("##chat", ref _inputValue, 100);
      ImGui.Text("Roma");
      ImGui.InputText("##azazaza", ref _inputValue, 100);
      ImGui.Text("Andy");
      
      ImGui.End();
      
      rlImGui.End();

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
