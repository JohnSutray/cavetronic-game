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
      if (cellSet.Contains(((int)gx, (int)gy))) {
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

  /// Обрезка ячейки по контуру с сохранением всех точек контура острова
  private static List<Vector2> ClipCellWithContour(
    List<Vector2> cell,
    List<Vector2> contour,
    int siteIdx,
    List<Vector2> sites,
    HashSet<(int x, int y)> cellSet) {
    if (cell.Count < 3 || contour.Count < 3) return cell;

    // Если все вершины ячейки внутри острова — внутренняя ячейка, контур не нужен
    if (cell.All(v => IsInIsland(v, cellSet))) return cell;

    var center = sites[siteIdx];
    var points = new List<Vector2>();

    // 1. Вершины Voronoi-ячейки, которые внутри острова
    foreach (var v in cell) {
      if (IsInIsland(v, cellSet))
        points.Add(v);
    }

    // 2. Вершины контура острова, ближайшие к этому сайту
    foreach (var v in contour) {
      if (NearestSiteIdx(v, sites) == siteIdx)
        points.Add(v);
    }

    // 3. Точки пересечения рёбер Voronoi-ячейки с рёбрами контура
    for (var i = 0; i < cell.Count; i++) {
      var a = cell[i];
      var b = cell[(i + 1) % cell.Count];
      for (var j = 0; j < contour.Count; j++) {
        var c = contour[j];
        var d = contour[(j + 1) % contour.Count];
        if (SegSegIntersect(a, b, c, d, out var pt))
          points.Add(pt);
      }
    }

    if (points.Count < 3) return [];

    // 4. Убираем дубликаты (по расстоянию)
    var unique = new List<Vector2> { points[0] };
    for (var i = 1; i < points.Count; i++) {
      var dominated = false;
      foreach (var u in unique) {
        var dx = u.X - points[i].X;
        var dy = u.Y - points[i].Y;
        if (dx * dx + dy * dy < 0.01f) {
          dominated = true;
          break;
        }
      }
      if (!dominated) unique.Add(points[i]);
    }

    if (unique.Count < 3) return [];

    // 5. Сортируем по углу вокруг сайта
    unique.Sort((a, b) => {
      var angleA = MathF.Atan2(a.Y - center.Y, a.X - center.X);
      var angleB = MathF.Atan2(b.Y - center.Y, b.X - center.X);
      return angleA.CompareTo(angleB);
    });

    return unique;
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

  private static int NearestSiteIdx(Vector2 point, List<Vector2> sites) {
    var best = 0;
    var bestDist = float.MaxValue;
    for (var i = 0; i < sites.Count; i++) {
      var dx = point.X - sites[i].X;
      var dy = point.Y - sites[i].Y;
      var d = dx * dx + dy * dy;
      if (d < bestDist) {
        bestDist = d;
        best = i;
      }
    }
    return best;
  }

  private static bool SegSegIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 point) {
    point = default;
    var d1 = a2 - a1;
    var d2 = b2 - b1;
    var cross = d1.X * d2.Y - d1.Y * d2.X;
    if (MathF.Abs(cross) < 1e-10f) return false;

    var diff = b1 - a1;
    var t = (diff.X * d2.Y - diff.Y * d2.X) / cross;
    var u = (diff.X * d1.Y - diff.Y * d1.X) / cross;

    if (t > 0.001f && t < 0.999f && u > 0.001f && u < 0.999f) {
      point = a1 + d1 * t;
      return true;
    }
    return false;
  }

  private static bool IsInIsland(Vector2 p, HashSet<(int x, int y)> cellSet) {
    return cellSet.Contains(((int)MathF.Floor(p.X), (int)MathF.Floor(p.Y)));
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
