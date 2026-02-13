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

    // Генерируем 3x3 chunk'ов
    for (int chunkX = -1; chunkX <= 1; chunkX++) {
      for (int chunkY = -1; chunkY <= 1; chunkY++) {
        GenerateChunk(chunkX, chunkY);
      }
    }

    // Сохраняем полную карту
    _visualizer.SaveFullMap();
  }

  private void GenerateChunk(int chunkX, int chunkY) {
    var gridSize = _config.ChunkSize;
    var startX = chunkX * gridSize;
    var startY = chunkY * gridSize;

    // 1. Генерация сырого шума (для визуализации)
    var rawNoise = ChunkVisualizer.GenerateRawNoise(_config, startX, startY, gridSize);

    // 2. Конвертация в boolean grid
    var grid = _noiseGenerator.GenerateGrid(
      startX,
      startY,
      gridSize,
      gridSize,
      _config.Threshold
    );

    // 3. Сглаживание через Cellular Automata
    var smoothedGrid = CellularAutomata.Smooth(
      grid,
      _config.SmoothIterations,
      _config.SolidNeighborThreshold,
      fillIsolatedVoids: true
    );

    // 4. Извлечение островов (контуры + клетки)
    var islands = SimpleIslandTracer.ExtractIslands(smoothedGrid, _config.CellSize);

    // 5. Конвертация в мировые координаты
    var offsetX = chunkX * gridSize * _config.CellSize;
    var offsetY = chunkY * gridSize * _config.CellSize;

    var worldContours = new List<List<Vector2>>();
    var allShards = new List<List<Vector2>>();
    var islandSeed = _config.Seed + chunkX * 1000 + chunkY;

    foreach (var island in islands) {
      if (island.Contour.Count >= 3) {
        // Смещаем контур в позицию chunk'а
        var worldContour = island.Contour.Select(
          p => p + new Vector2(offsetX, offsetY)
        ).ToList();
        worldContours.Add(worldContour);

        // 6. Разбиваем остров на осколки через grid-based Voronoi
        var shards = ShardGenerator.CreateShards(island.Cells, _config.CellSize, islandSeed++);

        // Смещаем шарды в мировые координаты
        var worldShards = shards.Select(s =>
          s.Select(p => p + new Vector2(offsetX, offsetY)).ToList()
        ).ToList();

        allShards.AddRange(worldShards);

        // 7. Создаём физические тела из осколков
        foreach (var shard in worldShards) {
          _bodyBuilder.CreateBodyFromShard(shard);
        }
      }
    }

    // 8. Добавляем в визуализатор
    _visualizer.AddChunk(chunkX, chunkY, rawNoise, grid, smoothedGrid, worldContours, allShards);
  }
}