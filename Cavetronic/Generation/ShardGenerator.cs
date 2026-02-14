using nkast.Aether.Physics2D.Common;

namespace Cavetronic.Generation;

/// Разбивает остров на выпуклые осколки через диаграмму Вороного
public static class ShardGenerator {
  public static ShardsData CreateShards(IslandData island, int seed) {
    if (island.Cells.Count < 3) return new ShardsData(island, []);

    var random = new Random(seed);
    var cellSet = new HashSet<(int x, int y)>(island.Cells);

    var minGX = island.Cells.Min(c => c.x);
    var maxGX = island.Cells.Max(c => c.x);
    var minGY = island.Cells.Min(c => c.y);
    var maxGY = island.Cells.Max(c => c.y);

    var numSites = Math.Clamp(island.Cells.Count / 100, 3, 20);

    // Генерируем случайные сайты внутри острова
    var sites = new List<Vector2>();
    var attempts = 0;
    while (sites.Count < numSites && attempts < numSites * 100) {
      var gx = minGX + random.NextDouble() * (maxGX - minGX + 1);
      var gy = minGY + random.NextDouble() * (maxGY - minGY + 1);
      if (cellSet.Contains(((int)Math.Floor(gx), (int)Math.Floor(gy)))) {
        sites.Add(new Vector2((float)gx, (float)gy));
      }
      attempts++;
    }

    if (sites.Count < 2)
      return new ShardsData(island, [island.Contour]);

    // Lloyd's relaxation — делает ячейки более равномерными (как соты)
    for (var iter = 0; iter < 2; iter++) {
      var cells = ComputeVoronoiCells(sites);
      for (var i = 0; i < sites.Count; i++) {
        var clipped = SimpleClipToIsland(cells[i], cellSet);
        if (clipped.Count < 3) continue;
        var centroid = PolygonCentroid(clipped);
        if (IsInIsland(centroid, cellSet))
          sites[i] = centroid;
      }
    }

    // Контур острова уже есть в IslandData — переиспользуем
    var contour = island.Contour;

    // Финальное вычисление + обрезка по контуру острова с сохранением всех точек контура
    var finalCells = ComputeVoronoiCells(sites);
    var shards = new List<List<Vector2>>();
    for (var i = 0; i < sites.Count; i++) {
      var clipped = ClipCellWithContour(finalCells[i], contour, i, sites, cellSet);
      if (clipped.Count >= 3)
        shards.Add(clipped);
    }

    var result = shards.Count > 0 ? shards : [contour];
    return new ShardsData(island, result);
  }

  /// Вычисляет ячейки Вороного через пересечение полуплоскостей
  private static List<List<Vector2>> ComputeVoronoiCells(List<Vector2> sites) {
    var minX = sites.Min(s => s.X);
    var maxX = sites.Max(s => s.X);
    var minY = sites.Min(s => s.Y);
    var maxY = sites.Max(s => s.Y);
    var margin = MathF.Max(maxX - minX, maxY - minY) + 100f;

    var cells = new List<List<Vector2>>();
    for (var i = 0; i < sites.Count; i++) {
      var cell = new List<Vector2> {
        new(minX - margin, minY - margin),
        new(maxX + margin, minY - margin),
        new(maxX + margin, maxY + margin),
        new(minX - margin, maxY + margin)
      };

      for (var j = 0; j < sites.Count; j++) {
        if (i == j) continue;
        cell = ClipByBisector(cell, sites[i], sites[j]);
        if (cell.Count < 3) break;
      }
      cells.Add(cell);
    }
    return cells;
  }

  /// Обрезает полигон биссектрисой, оставляя половину ближе к site
  private static List<Vector2> ClipByBisector(List<Vector2> polygon, Vector2 site, Vector2 other) {
    if (polygon.Count < 3) return polygon;

    var mid = (site + other) * 0.5f;
    var normal = site - other;
    var result = new List<Vector2>();
    var n = polygon.Count;

    for (var i = 0; i < n; i++) {
      var curr = polygon[i];
      var next = polygon[(i + 1) % n];
      var currDist = Dot(curr - mid, normal);
      var nextDist = Dot(next - mid, normal);

      if (currDist >= 0) {
        result.Add(curr);
        if (nextDist < 0)
          result.Add(BisectorIntersect(curr, next, mid, normal));
      } else if (nextDist >= 0) {
        result.Add(BisectorIntersect(curr, next, mid, normal));
      }
    }
    return result;
  }

  /// Обрезка Voronoi-ячейки по контуру острова через Sutherland-Hodgman.
  /// Subject = contour (non-convex), Clip = Voronoi cell (convex) → результат гарантированно внутри обоих.
  private static List<Vector2> ClipCellWithContour(
    List<Vector2> cell,
    List<Vector2> contour,
    int siteIdx,
    List<Vector2> sites,
    HashSet<(int x, int y)> cellSet) {
    if (cell.Count < 3 || contour.Count < 3) return [];

    // S-H: clip contour (subject) по каждому ребру Voronoi cell (convex clip)
    var interior = sites[siteIdx];
    var clipped = new List<Vector2>(contour);

    for (var i = 0; i < cell.Count && clipped.Count >= 3; i++) {
      var edgeA = cell[i];
      var edgeB = cell[(i + 1) % cell.Count];
      clipped = ClipPolygonByEdge(clipped, edgeA, edgeB, interior);
    }

    // Граничная ячейка: S-H дал пересечение, но может содержать мостики через пустоту
    if (clipped.Count >= 3) {
      clipped = FixBridgeEdges(clipped, cellSet, contour);
      if (clipped.Count >= 3 && !HasLongBridgeEdge(clipped, cellSet))
        return clipped;
    }

    // Внутренняя ячейка: контур не пересекает ячейку, возвращаем если все рёбра внутри
    if (cell.All(v => IsInIsland(v, cellSet)) && !HasAnyBridgeEdge(cell, cellSet))
      return cell;

    return [];
  }

  /// S-H: обрезка полигона одним ребром, сохраняя сторону с interior
  private static List<Vector2> ClipPolygonByEdge(
    List<Vector2> polygon,
    Vector2 edgeA,
    Vector2 edgeB,
    Vector2 interior) {
    if (polygon.Count < 3) return polygon;

    var edgeDir = edgeB - edgeA;
    var interiorCross = edgeDir.X * (interior.Y - edgeA.Y) - edgeDir.Y * (interior.X - edgeA.X);
    var interiorSign = MathF.Sign(interiorCross);
    if (interiorSign == 0) return polygon;

    var result = new List<Vector2>();
    var n = polygon.Count;

    for (var i = 0; i < n; i++) {
      var curr = polygon[i];
      var next = polygon[(i + 1) % n];

      var currCross = edgeDir.X * (curr.Y - edgeA.Y) - edgeDir.Y * (curr.X - edgeA.X);
      var nextCross = edgeDir.X * (next.Y - edgeA.Y) - edgeDir.Y * (next.X - edgeA.X);

      var currInside = MathF.Sign(currCross) == interiorSign || MathF.Abs(currCross) < 1e-6f;
      var nextInside = MathF.Sign(nextCross) == interiorSign || MathF.Abs(nextCross) < 1e-6f;

      if (currInside) {
        result.Add(curr);
        if (!nextInside) {
          var pt = LineIntersect(curr, next, edgeA, edgeB);
          if (pt.HasValue) result.Add(pt.Value);
        }
      } else if (nextInside) {
        var pt = LineIntersect(curr, next, edgeA, edgeB);
        if (pt.HasValue) result.Add(pt.Value);
      }
    }

    return result;
  }

  private static Vector2? LineIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2) {
    var d1 = a2 - a1;
    var d2 = b2 - b1;
    var cross = d1.X * d2.Y - d1.Y * d2.X;
    if (MathF.Abs(cross) < 1e-10f) return null;
    var diff = b1 - a1;
    var t = (diff.X * d2.Y - diff.Y * d2.X) / cross;
    return a1 + d1 * t;
  }

  /// Простая обрезка для Lloyd's relaxation (без контура)
  private static List<Vector2> SimpleClipToIsland(
    List<Vector2> cell,
    HashSet<(int x, int y)> cellSet) {
    if (cell.Count < 3) return cell;
    if (cell.All(v => IsInIsland(v, cellSet))) return cell;

    var clipped = new List<Vector2>();
    var n = cell.Count;

    for (var i = 0; i < n; i++) {
      var curr = cell[i];
      var next = cell[(i + 1) % n];
      var currIn = IsInIsland(curr, cellSet);
      var nextIn = IsInIsland(next, cellSet);

      if (currIn) {
        clipped.Add(curr);
        if (!nextIn)
          clipped.Add(FindBoundary(curr, next, cellSet));
      } else if (nextIn) {
        clipped.Add(FindBoundary(next, curr, cellSet));
      }
    }
    return clipped;
  }

  /// Проверяет каждое ребро полигона и вставляет контурные вершины вместо мостиков через пустоту
  private static List<Vector2> FixBridgeEdges(
    List<Vector2> polygon,
    HashSet<(int x, int y)> cellSet,
    List<Vector2> contour) {
    var current = polygon;
    for (var iter = 0; iter < 3; iter++) {
      var result = new List<Vector2>();
      var n = current.Count;
      var hasBridges = false;

      for (var i = 0; i < n; i++) {
        var curr = current[i];
        var next = current[(i + 1) % n];
        result.Add(curr);

        if (EdgeCrossesEmpty(curr, next, cellSet)) {
          hasBridges = true;
          var path = FindContourPath(curr, next, contour);
          result.AddRange(path);
        }
      }

      if (!hasBridges) return current;
      current = result;
    }
    return current;
  }

  private static bool HasAnyBridgeEdge(List<Vector2> polygon, HashSet<(int x, int y)> cellSet) {
    for (var i = 0; i < polygon.Count; i++) {
      if (EdgeCrossesEmpty(polygon[i], polygon[(i + 1) % polygon.Count], cellSet))
        return true;
    }
    return false;
  }

  /// Проверяет наличие длинных мостиков (>2 подряд пустых сэмплов), игнорируя пограничные касания
  private static bool HasLongBridgeEdge(List<Vector2> polygon, HashSet<(int x, int y)> cellSet) {
    for (var i = 0; i < polygon.Count; i++) {
      if (EdgeHasLongBridge(polygon[i], polygon[(i + 1) % polygon.Count], cellSet))
        return true;
    }
    return false;
  }

  /// Проверяет серьёзные мостики: ≥2 клетки сплошной пустоты (не касаясь даже соседних solid клеток)
  private static bool EdgeHasLongBridge(Vector2 a, Vector2 b, HashSet<(int x, int y)> cellSet) {
    var dx = b.X - a.X;
    var dy = b.Y - a.Y;
    var dist = MathF.Sqrt(dx * dx + dy * dy);
    if (dist < 2f) return false;
    var steps = Math.Max((int)(dist * 4), 4);
    var emptyRun = 0;
    for (var s = 1; s < steps; s++) {
      var t = s / (float)steps;
      var p = a + (b - a) * t;
      if (!IsNearIsland(p, cellSet)) {
        emptyRun++;
        if (emptyRun >= 6) return true;
      } else {
        emptyRun = 0;
      }
    }
    return false;
  }

  /// Проверяет, пересекает ли ребро пустые клетки (сэмплируя точки каждые 0.25 клетки)
  private static bool EdgeCrossesEmpty(Vector2 a, Vector2 b, HashSet<(int x, int y)> cellSet) {
    var dx = b.X - a.X;
    var dy = b.Y - a.Y;
    var dist = MathF.Sqrt(dx * dx + dy * dy);
    var steps = Math.Max((int)(dist * 4), 4);
    for (var s = 1; s < steps; s++) {
      var t = s / (float)steps;
      var p = a + (b - a) * t;
      if (!IsInIsland(p, cellSet)) return true;
    }
    return false;
  }

  /// Находит путь по контуру от точки near 'from' до точки near 'to' (кратчайший обход),
  /// включая ближайшие контурные вершины для обоих концов
  private static List<Vector2> FindContourPath(Vector2 from, Vector2 to, List<Vector2> contour) {
    var fromIdx = NearestContourIdx(from, contour);
    var toIdx = NearestContourIdx(to, contour);
    if (fromIdx == toIdx) return [contour[fromIdx]];

    var n = contour.Count;

    // Обход вперёд (CW): fromIdx → ... → toIdx (включительно)
    var pathFwd = new List<Vector2> { contour[fromIdx] };
    for (var k = (fromIdx + 1) % n; k != toIdx; k = (k + 1) % n) {
      pathFwd.Add(contour[k]);
      if (pathFwd.Count > n) break;
    }
    pathFwd.Add(contour[toIdx]);

    // Обход назад (CCW): fromIdx → ... → toIdx (включительно)
    var pathBwd = new List<Vector2> { contour[fromIdx] };
    for (var k = (fromIdx - 1 + n) % n; k != toIdx; k = (k - 1 + n) % n) {
      pathBwd.Add(contour[k]);
      if (pathBwd.Count > n) break;
    }
    pathBwd.Add(contour[toIdx]);

    return pathFwd.Count <= pathBwd.Count ? pathFwd : pathBwd;
  }

  private static int NearestContourIdx(Vector2 point, List<Vector2> contour) {
    var best = 0;
    var bestDist = float.MaxValue;
    for (var i = 0; i < contour.Count; i++) {
      var dx = point.X - contour[i].X;
      var dy = point.Y - contour[i].Y;
      var d = dx * dx + dy * dy;
      if (d < bestDist) {
        bestDist = d;
        best = i;
      }
    }
    return best;
  }

  private static bool IsInIsland(Vector2 p, HashSet<(int x, int y)> cellSet) {
    return cellSet.Contains(((int)MathF.Floor(p.X), (int)MathF.Floor(p.Y)));
  }

  /// Ленивая проверка — true если хотя бы одна из 4 соседних клеток solid (для граничных рёбер)
  private static bool IsNearIsland(Vector2 p, HashSet<(int x, int y)> cellSet) {
    var ix = (int)MathF.Floor(p.X);
    var iy = (int)MathF.Floor(p.Y);
    return cellSet.Contains((ix, iy)) || cellSet.Contains((ix - 1, iy)) ||
           cellSet.Contains((ix, iy - 1)) || cellSet.Contains((ix - 1, iy - 1));
  }

  private static Vector2 FindBoundary(Vector2 inside, Vector2 outside, HashSet<(int x, int y)> cellSet) {
    for (var i = 0; i < 16; i++) {
      var mid = (inside + outside) * 0.5f;
      if (IsInIsland(mid, cellSet))
        inside = mid;
      else
        outside = mid;
    }
    return inside;
  }

  private static float Dot(Vector2 a, Vector2 b) => a.X * b.X + a.Y * b.Y;

  private static Vector2 BisectorIntersect(Vector2 a, Vector2 b, Vector2 planePoint, Vector2 planeNormal) {
    var d = b - a;
    var denom = Dot(d, planeNormal);
    if (MathF.Abs(denom) < 1e-10f) return a;
    var t = Dot(planePoint - a, planeNormal) / denom;
    return a + d * t;
  }

  private static Vector2 PolygonCentroid(List<Vector2> polygon) {
    float area = 0, cx = 0, cy = 0;
    var n = polygon.Count;
    for (var i = 0; i < n; i++) {
      var curr = polygon[i];
      var next = polygon[(i + 1) % n];
      var cross = curr.X * next.Y - next.X * curr.Y;
      area += cross;
      cx += (curr.X + next.X) * cross;
      cy += (curr.Y + next.Y) * cross;
    }
    area *= 0.5f;
    if (MathF.Abs(area) < 1e-10f) {
      var avg = Vector2.Zero;
      foreach (var v in polygon) avg += v;
      return avg / polygon.Count;
    }
    return new Vector2(cx / (6 * area), cy / (6 * area));
  }
}
