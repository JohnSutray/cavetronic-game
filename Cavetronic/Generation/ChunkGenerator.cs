using nkast.Aether.Physics2D.Common;
using nkast.Aether.Physics2D.Common.Decomposition;

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
      var convexData = EnsureConvexShards(shapedData);
      var noEnclosed = RemoveEnclosedShards(convexData, config.ShardEnclosedThreshold);
      var noRects = FilterRectangles(noEnclosed);
      var cleanData = FilterSmallShards(noRects, config.MinShardArea);
      shapedIslands.Add(cleanData);
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

  private static ShapedShardsData EnsureConvexShards(ShapedShardsData data) {
    var result = new List<ShapedShard>();

    foreach (var shard in data.Shards) {
      if (shard.Polygon.Count < 3) continue;

      if (IsConvex(shard.Polygon)) {
        result.Add(shard);
        continue;
      }

      // Декомпозиция невыпуклого полигона на выпуклые части
      var convexParts = DecomposeConvex(shard.Polygon);
      foreach (var part in convexParts) {
        var localCenter = CalculateCenter(part);
        var localPoly = part.Select(v => v - localCenter).ToList();
        // Position шарда в мировых координатах: shard.Position + смещение центра подчасти
        result.Add(new ShapedShard(shard.Position + localCenter, localPoly));
      }
    }

    return data with { Shards = result };
  }

  private static List<List<Vector2>> DecomposeConvex(List<Vector2> polygon) {
    var result = new List<List<Vector2>>();
    var queue = new Queue<List<Vector2>>();
    queue.Enqueue(polygon);
    var maxIterations = 200;

    while (queue.Count > 0 && maxIterations-- > 0) {
      var poly = queue.Dequeue();

      if (poly.Count <= 3 || IsConvex(poly)) {
        if (poly.Count >= 3) result.Add(poly);
        continue;
      }

      var split = TrySplitAtReflex(poly);
      if (split != null) {
        queue.Enqueue(split.Value.a);
        queue.Enqueue(split.Value.b);
        continue;
      }

      // Fallback: Earclip триангуляция
      try {
        var parts = Triangulate.ConvexPartition(
          new Vertices(poly),
          TriangulationAlgorithm.Earclip,
          discardAndFixInvalid: true,
          tolerance: 0.001f,
          skipSanityChecks: false
        );
        result.AddRange(parts.Select(v => v.ToList()));
      } catch {
        result.Add(poly);
      }
    }

    return result;
  }

  // Разрезает невыпуклый полигон на две части по диагонали из reflex-вершины
  private static (List<Vector2> a, List<Vector2> b)? TrySplitAtReflex(List<Vector2> polygon) {
    var n = polygon.Count;
    var windingSign = GetWindingSign(polygon);

    for (var i = 0; i < n; i++) {
      if (!IsReflex(polygon, i, windingSign)) continue;

      // Собираем кандидатов для диагонали, сортируем по расстоянию
      var candidates = new List<(int idx, float dist)>();
      for (var j = 0; j < n; j++) {
        if (j == i || j == (i - 1 + n) % n || j == (i + 1) % n) continue;
        var diff = polygon[i] - polygon[j];
        var d = diff.X * diff.X + diff.Y * diff.Y;
        candidates.Add((j, d));
      }
      candidates.Sort((a, b) => a.dist.CompareTo(b.dist));

      foreach (var (j, _) in candidates) {
        if (!IsValidDiagonal(polygon, i, j, windingSign)) continue;
        return SplitPolygon(polygon, i, j);
      }
    }

    return null;
  }

  private static int GetWindingSign(List<Vector2> polygon) {
    var area = 0f;
    for (var i = 0; i < polygon.Count; i++) {
      var a = polygon[i];
      var b = polygon[(i + 1) % polygon.Count];
      area += a.X * b.Y - b.X * a.Y;
    }
    return area > 0 ? 1 : -1;
  }

  private static bool IsReflex(List<Vector2> polygon, int i, int windingSign) {
    var n = polygon.Count;
    var prev = polygon[(i - 1 + n) % n];
    var curr = polygon[i];
    var next = polygon[(i + 1) % n];
    var cross = Cross2D(curr - prev, next - curr);
    if (MathF.Abs(cross) < 1e-6f) return false;
    return (cross > 0 ? 1 : -1) != windingSign;
  }

  private static bool IsValidDiagonal(
    List<Vector2> polygon,
    int i,
    int j,
    int windingSign
  ) {
    var n = polygon.Count;
    var a = polygon[i];
    var b = polygon[j];

    // Диагональ не должна пересекать рёбра полигона
    for (var k = 0; k < n; k++) {
      var kn = (k + 1) % n;
      if (k == i || kn == i || k == j || kn == j) continue;
      if (SegmentsIntersect(a, b, polygon[k], polygon[kn])) return false;
    }

    // Средняя точка диагонали должна быть внутри полигона
    var mid = (a + b) * 0.5f;
    return IsPointInPolygon(polygon, mid);
  }

  // Ray casting point-in-polygon test
  private static bool IsPointInPolygon(List<Vector2> polygon, Vector2 point) {
    var inside = false;
    var n = polygon.Count;
    for (int i = 0, j = n - 1; i < n; j = i++) {
      var pi = polygon[i];
      var pj = polygon[j];
      if ((pi.Y > point.Y) != (pj.Y > point.Y) &&
          point.X < (pj.X - pi.X) * (point.Y - pi.Y) / (pj.Y - pi.Y) + pi.X)
        inside = !inside;
    }
    return inside;
  }

  private static (List<Vector2> a, List<Vector2> b) SplitPolygon(
    List<Vector2> polygon,
    int i,
    int j
  ) {
    // Гарантируем i < j
    if (i > j) (i, j) = (j, i);

    var partA = new List<Vector2>();
    for (var k = i; k <= j; k++) partA.Add(polygon[k]);

    var partB = new List<Vector2>();
    for (var k = j; k < polygon.Count; k++) partB.Add(polygon[k]);
    for (var k = 0; k <= i; k++) partB.Add(polygon[k]);

    return (partA, partB);
  }

  private static bool SegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4) {
    var d1 = Cross2D(p4 - p3, p1 - p3);
    var d2 = Cross2D(p4 - p3, p2 - p3);
    var d3 = Cross2D(p2 - p1, p3 - p1);
    var d4 = Cross2D(p2 - p1, p4 - p1);

    if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
        ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
      return true;

    return false; // Коллинеарные/касающиеся случаи — не считаем пересечением
  }

  private static float Cross2D(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

  private static bool IsConvex(List<Vector2> polygon) {
    var n = polygon.Count;
    if (n < 3) return false;

    var sign = 0;
    for (var i = 0; i < n; i++) {
      var a = polygon[i];
      var b = polygon[(i + 1) % n];
      var c = polygon[(i + 2) % n];
      var cross = (b.X - a.X) * (c.Y - b.Y) - (b.Y - a.Y) * (c.X - b.X);

      if (MathF.Abs(cross) < 1e-6f) continue; // коллинеарные вершины — пропускаем

      var s = cross > 0 ? 1 : -1;
      if (sign == 0) sign = s;
      else if (s != sign) return false;
    }

    return true;
  }

  // Удаляет шарды, которые на threshold (напр. 80%) площади погружены внутрь большего шарда
  private static ShapedShardsData RemoveEnclosedShards(ShapedShardsData data, float threshold) {
    var shards = data.Shards;
    if (shards.Count <= 1) return data;

    // Предвычисляем мировые полигоны и площади
    var world = shards.Select(s => s.Polygon.Select(v => v + s.Position).ToList()).ToArray();
    var areas = shards.Select(s => PolygonArea(s.Polygon)).ToArray();
    var toRemove = new HashSet<int>();

    for (var i = 0; i < shards.Count; i++) {
      if (toRemove.Contains(i)) {
        continue;
      }
      
      for (var j = 0; j < shards.Count; j++) {
        if (i == j || toRemove.Contains(j)) continue;
        // Проверяем только меньший внутри большего
        if (areas[i] >= areas[j]) {
          continue;
        }

        var intersection = ClipConvexPolygons(world[i], world[j]);
        
        if (intersection.Count < 3) {
          continue;
        }

        var interArea = PolygonArea(intersection);
        
        if (interArea / areas[i] >= threshold) {
          toRemove.Add(i);
          break;
        }
      }
    }

    if (toRemove.Count == 0) return data;
    var result = shards.Where((_, idx) => !toRemove.Contains(idx)).ToList();
    return new ShapedShardsData(data.Island, result);
  }

  // Sutherland-Hodgman клиппинг: обрезает subject по clip (оба выпуклые)
  private static List<Vector2> ClipConvexPolygons(List<Vector2> subject, List<Vector2> clip) {
    var output = new List<Vector2>(subject);

    for (var i = 0; i < clip.Count && output.Count > 0; i++) {
      var edgeA = clip[i];
      var edgeB = clip[(i + 1) % clip.Count];
      var input = output;
      output = new List<Vector2>();

      for (var j = 0; j < input.Count; j++) {
        var curr = input[j];
        var prev = input[(j - 1 + input.Count) % input.Count];
        var currInside = Cross2D(edgeB - edgeA, curr - edgeA) >= 0;
        var prevInside = Cross2D(edgeB - edgeA, prev - edgeA) >= 0;

        if (currInside) {
          if (!prevInside) output.Add(LineIntersection(prev, curr, edgeA, edgeB));
          output.Add(curr);
        } else if (prevInside) {
          output.Add(LineIntersection(prev, curr, edgeA, edgeB));
        }
      }
    }

    return output;
  }

  private static Vector2 LineIntersection(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2) {
    var d1 = a2 - a1;
    var d2 = b2 - b1;
    var cross = d1.X * d2.Y - d1.Y * d2.X;
    if (MathF.Abs(cross) < 1e-10f) return (a1 + a2) * 0.5f;
    var t = ((b1.X - a1.X) * d2.Y - (b1.Y - a1.Y) * d2.X) / cross;
    return new Vector2(a1.X + t * d1.X, a1.Y + t * d1.Y);
  }

  private static ShapedShardsData FilterRectangles(ShapedShardsData data) {
    var result = data.Shards.Where(s => !IsRectangle(s.Polygon)).ToList();
    return new ShapedShardsData(data.Island, result);
  }

  private static bool IsRectangle(List<Vector2> polygon) {
    if (polygon.Count != 4) return false;

    for (var i = 0; i < 4; i++) {
      var a = polygon[i];
      var b = polygon[(i + 1) % 4];
      var c = polygon[(i + 2) % 4];
      var ab = b - a;
      var bc = c - b;
      var dot = ab.X * bc.X + ab.Y * bc.Y;
      var lenProduct = MathF.Sqrt((ab.X * ab.X + ab.Y * ab.Y) * (bc.X * bc.X + bc.Y * bc.Y));
      if (lenProduct < 1e-6f) return false;
      if (MathF.Abs(dot / lenProduct) > 0.1f) return false; // cos(angle) ≈ 0 → ~90°
    }

    return true;
  }

  private static ShapedShardsData FilterSmallShards(ShapedShardsData data, float minArea) {
    var result = data.Shards.Where(s => PolygonArea(s.Polygon) >= minArea).ToList();
    return new ShapedShardsData(data.Island, result);
  }

  private static float PolygonArea(List<Vector2> polygon) {
    var area = 0f;
    for (var i = 0; i < polygon.Count; i++) {
      var a = polygon[i];
      var b = polygon[(i + 1) % polygon.Count];
      area += a.X * b.Y - b.X * a.Y;
    }
    return MathF.Abs(area) * 0.5f;
  }

  private static Vector2 CalculateCenter(List<Vector2> vertices) {
    var sum = Vector2.Zero;
    foreach (var v in vertices) {
      sum += v;
    }
    return sum / vertices.Count;
  }
}
