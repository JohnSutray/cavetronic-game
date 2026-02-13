using Cavetronic.Systems;
using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Dynamics;

namespace Cavetronic.Generation;

public class CaveGenerationSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private readonly CaveGenerationConfig _config = new();
  private NoiseGenerator _noiseGenerator = null!;
  private PhysicsBodyBuilder _bodyBuilder = null!;
  private FullMapVisualizer _visualizer = null!;

  public override void Init() {
    _noiseGenerator = new NoiseGenerator(_config.Seed, _config.Frequency, _config.Octaves);
    _bodyBuilder = new PhysicsBodyBuilder(GameWorld.Physics, _config);
    _visualizer = new FullMapVisualizer(_config);

    // Генерируем 3×3 chunk'ов
    for (int chunkX = -1; chunkX <= 1; chunkX++) {
      for (int chunkY = -1; chunkY <= 1; chunkY++) {
        GenerateChunk(chunkX, chunkY);
      }
    }

    // Сохраняем полную карту
    _visualizer.SaveFullMap();

    // Генерируем камни для тестирования
    // GenerateRocks(10);
  }

  private void GenerateChunk(int chunkX, int chunkY) {
    var gridSize = _config.ChunkSize;
    var startX = chunkX * gridSize;
    var startY = chunkY * gridSize;

    // 1. Генерация сырого шума (для визуализации)
    var rawNoise = ChunkVisualizer.GenerateRawNoise(_config, startX, startY, gridSize);

    // 2. Конвертация в boolean grid
    var grid = _noiseGenerator.GenerateGrid(startX, startY, gridSize, gridSize, _config.Threshold);

    // 3. Сглаживание через Cellular Automata (без заполнения пустот)
    var smoothedGrid = CellularAutomata.Smooth(grid, _config.SmoothIterations, _config.SolidNeighborThreshold, fillIsolatedVoids: false);

    // 4. Извлечение островов (простой алгоритм - прямоугольные контуры)
    var islandContours = SimpleIslandTracer.ExtractIslands(smoothedGrid, _config.CellSize);

    // 5. Создание физических тел + конвертация в мировые координаты
    var offsetX = chunkX * gridSize * _config.CellSize;
    var offsetY = chunkY * gridSize * _config.CellSize;

    var worldIslands = new List<List<Vector2>>();
    foreach (var island in islandContours) {
      if (island.Count >= 3) {
        // Смещаем контур в позицию chunk'а
        var worldIsland = island.Select(p => p + new Vector2(offsetX, offsetY)).ToList();
        worldIslands.Add(worldIsland);
        _bodyBuilder.CreateBodyFromRegion(worldIsland);
      }
    }

    // 6. Добавляем в визуализатор (с мировыми координатами контуров)
    _visualizer.AddChunk(chunkX, chunkY, rawNoise, grid, smoothedGrid, worldIslands);

    var solidCount = CountSolid(smoothedGrid);
    var total = gridSize * gridSize;
    var totalVertices = worldIslands.Sum(c => c.Count);
    Console.WriteLine($"Chunk ({chunkX},{chunkY}): {worldIslands.Count} islands, {totalVertices} vertices, {solidCount}/{total} solid ({100f * solidCount / total:F1}%)");
  }

  private static int CountSolid(bool[,] grid) {
    var count = 0;
    for (int x = 0; x < grid.GetLength(0); x++) {
      for (int y = 0; y < grid.GetLength(1); y++) {
        if (grid[x, y]) count++;
      }
    }
    return count;
  }

  private void GenerateRocks(int count) {
    var random = new Random(42);
    for (int i = 0; i < count; i++) {
      float x = 32f + (float)random.NextDouble() * 64f;
      float y = 32f + (float)random.NextDouble() * 64f;
      float radius = 0.15f + (float)random.NextDouble() * 0.2f;

      var rock = GameWorld.Physics.CreateBody(new Vector2(x, y), 0, BodyType.Dynamic);
      var fixture = rock.CreateCircle(radius, 1f);
      fixture.Restitution = 0.3f;
    }
  }
}
