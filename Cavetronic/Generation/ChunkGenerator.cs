using nkast.Aether.Physics2D.Common;

namespace Cavetronic.Generation;

public class ChunkGenerator(CaveGenerationConfig config, NoiseGenerator noiseGenerator) {
  public (Chunk chunk, ChunkDebugData debugData) GenerateChunk(int chunkX, int chunkY) {
    var gridSize = config.ChunkSize;
    var startX = chunkX * gridSize;
    var startY = chunkY * gridSize;

    // 1. Генерация сырого шума (для дебаг-визуализации)
    var rawNoise = ChunkVisualizer.GenerateRawNoise(config, startX, startY, gridSize);

    // 2. Конвертация в boolean grid
    var grid = noiseGenerator.GenerateGrid(startX, startY, gridSize, gridSize, config.Threshold);

    // 3. Сглаживание через Cellular Automata
    var smoothedGrid = CellularAutomata.Smooth(
      grid,
      config.SmoothIterations,
      config.SolidNeighborThreshold,
      fillIsolatedVoids: true
    );

    // 4. Извлечение островов (сразу в абсолютных координатах)
    var islands = SimpleIslandTracer.ExtractIslands(smoothedGrid, startX, startY);

    // 5. Генерация шардов → shaping → Chunk
    var islandSeed = config.Seed + chunkX * 1000 + chunkY;
    var shapedIslands = new List<ShapedShardsData>();

    foreach (var island in islands) {
      if (island.Contour.Count < 3) continue;
      var shardsData = ShardGenerator.CreateShards(island, islandSeed++);
      var shapedData = ShapeShards(shardsData);
      shapedIslands.Add(shapedData);
    }

    var chunk = new Chunk(shapedIslands);
    var debugData = new ChunkDebugData(rawNoise, grid, smoothedGrid);
    return (chunk, debugData);
  }

  private static ShapedShardsData ShapeShards(ShardsData data) {
    var shapedShards = new List<ShapedShard>();

    foreach (var shard in data.Shards) {
      if (shard.Count < 3) continue;
      var position = CalculateCenter(shard);
      var polygon = shard.Select(v => v - position).ToList();
      shapedShards.Add(new ShapedShard(position, polygon));
    }

    return new ShapedShardsData(data.Island, shapedShards);
  }

  private static Vector2 CalculateCenter(List<Vector2> vertices) {
    var sum = Vector2.Zero;
    foreach (var v in vertices) {
      sum += v;
    }
    return sum / vertices.Count;
  }
}
