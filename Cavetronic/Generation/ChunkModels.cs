using nkast.Aether.Physics2D.Common;

namespace Cavetronic.Generation;

/// Остров + его Voronoi-шарды (всё в абсолютных координатах)
public record ShardsData(
  IslandData Island,
  List<List<Vector2>> Shards
);

/// Один шард: позиция в мире + локальный полигон (пригоден для коллайдера)
public record ShapedShard(
  Vector2 Position,
  List<Vector2> Polygon
);

/// Остров + его shaped-шарды
public record ShapedShardsData(
  IslandData Island,
  List<ShapedShard> Shards
);

/// Результат генерации одного чанка
public record Chunk(List<ShapedShardsData> Islands);

/// Промежуточные данные генерации для визуализации/дебага
public record ChunkDebugData(
  float[,] RawNoise,
  bool[,] BoolGrid,
  bool[,] SmoothedGrid
);
