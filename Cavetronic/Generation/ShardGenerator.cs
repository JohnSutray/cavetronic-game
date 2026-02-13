using nkast.Aether.Physics2D.Common;

namespace Cavetronic.Generation;

/// Разбивает остров на осколки через grid-based Voronoi (медовые соты)
public static class ShardGenerator {
  public static List<List<Vector2>> CreateShards(
    List<(int x, int y)> islandCells,
    float cellSize,
    int seed) {
    if (islandCells.Count < 3) return [];

    var random = new Random(seed);
    var cellSet = new HashSet<(int x, int y)>(islandCells);

    // Bounds острова в grid-координатах
    var minX = islandCells.Min(c => c.x);
    var maxX = islandCells.Max(c => c.x);
    var minY = islandCells.Min(c => c.y);
    var maxY = islandCells.Max(c => c.y);

    // Количество сайтов Вороного зависит от площади
    var area = islandCells.Count * cellSize * cellSize;
    var numSites = Math.Clamp((int)(area / 100f), 3, 20);

    // Генерируем случайные сайты (в grid-координатах с дробной частью)
    var sites = new List<(float x, float y)>();
    var attempts = 0;
    while (sites.Count < numSites && attempts < numSites * 100) {
      var x = minX + (float)random.NextDouble() * (maxX - minX + 1);
      var y = minY + (float)random.NextDouble() * (maxY - minY + 1);
      if (cellSet.Contains(((int)x, (int)y))) {
        sites.Add((x, y));
      }
      attempts++;
    }

    if (sites.Count < 2) {
      return [SimpleIslandTracer.ExtractContour(islandCells, cellSize)];
    }

    // Назначаем каждую клетку ближайшему сайту
    var groups = new Dictionary<int, List<(int x, int y)>>();
    for (var i = 0; i < sites.Count; i++) groups[i] = [];

    foreach (var cell in islandCells) {
      var cx = cell.x + 0.5f;
      var cy = cell.y + 0.5f;
      var nearest = 0;
      var nearestDist = float.MaxValue;

      for (var i = 0; i < sites.Count; i++) {
        var dx = cx - sites[i].x;
        var dy = cy - sites[i].y;
        var dist = dx * dx + dy * dy;
        if (dist < nearestDist) {
          nearestDist = dist;
          nearest = i;
        }
      }

      groups[nearest].Add(cell);
    }

    // Извлекаем контур каждой группы
    var shards = new List<List<Vector2>>();
    foreach (var group in groups.Values) {
      if (group.Count < 1) continue;
      var groupSet = new HashSet<(int x, int y)>(group);
      var contour = SimpleIslandTracer.ExtractContourFromSet(group, groupSet, cellSize);
      if (contour.Count >= 3) {
        shards.Add(contour);
      }
    }

    return shards.Count > 0 ? shards : [SimpleIslandTracer.ExtractContour(islandCells, cellSize)];
  }

}
