using nkast.Aether.Physics2D.Common;

namespace Cavetronic.Generation;

public record IslandData(
  List<Vector2> Contour,
  List<(int x, int y)> Cells
);

public static class SimpleIslandTracer {
  /// Извлекает острова из сетки в абсолютных мировых координатах
  public static List<IslandData> ExtractIslands(bool[,] grid, int offsetX, int offsetY) {
    var width = grid.GetLength(0);
    var height = grid.GetLength(1);
    var islands = new List<IslandData>();
    var visited = new bool[width, height];

    for (int x = 0; x < width; x++) {
      for (int y = 0; y < height; y++) {
        if (grid[x, y] && !visited[x, y]) {
          var localCells = FloodFill(grid, visited, x, y);
          if (localCells.Count >= 1) {
            var cells = localCells.Select(c => (c.x + offsetX, c.y + offsetY)).ToList();
            var contour = ExtractContour(cells);
            islands.Add(new IslandData(contour, cells));
          }
        }
      }
    }

    return islands;
  }

  /// Извлекает контур из набора клеток через edge tracing
  public static List<Vector2> ExtractContour(List<(int x, int y)> cells) {
    var cellSet = new HashSet<(int x, int y)>(cells);
    return ExtractContourFromSet(cells, cellSet);
  }

  /// Извлекает контур из набора клеток (с предвычисленным HashSet)
  public static List<Vector2> ExtractContourFromSet(
    List<(int x, int y)> cells,
    HashSet<(int x, int y)> cellSet) {
    var edges = new List<(Vector2 p1, Vector2 p2)>();

    foreach (var (x, y) in cells) {
      if (!cellSet.Contains((x - 1, y)))
        edges.Add((new Vector2(x, y), new Vector2(x, y + 1)));
      if (!cellSet.Contains((x + 1, y)))
        edges.Add((new Vector2(x + 1, y + 1), new Vector2(x + 1, y)));
      if (!cellSet.Contains((x, y - 1)))
        edges.Add((new Vector2(x + 1, y), new Vector2(x, y)));
      if (!cellSet.Contains((x, y + 1)))
        edges.Add((new Vector2(x, y + 1), new Vector2(x + 1, y + 1)));
    }

    if (edges.Count == 0) {
      var minX = cells.Min(c => c.x);
      var maxX = cells.Max(c => c.x);
      var minY = cells.Min(c => c.y);
      var maxY = cells.Max(c => c.y);
      return [
        new Vector2(minX, minY),
        new Vector2(maxX + 1, minY),
        new Vector2(maxX + 1, maxY + 1),
        new Vector2(minX, maxY + 1)
      ];
    }

    var contour = TraceEdgeLoop(edges);
    return SimplifyContour(contour);
  }

  private static List<(int x, int y)> FloodFill(bool[,] grid, bool[,] visited, int startX, int startY) {
    var width = grid.GetLength(0);
    var height = grid.GetLength(1);
    var cells = new List<(int x, int y)>();
    var queue = new Queue<(int x, int y)>();

    queue.Enqueue((startX, startY));
    visited[startX, startY] = true;

    while (queue.Count > 0) {
      var (x, y) = queue.Dequeue();
      cells.Add((x, y));

      var neighbors = new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) };
      foreach (var (nx, ny) in neighbors) {
        if (nx >= 0 && nx < width && ny >= 0 && ny < height &&
            grid[nx, ny] && !visited[nx, ny]) {
          visited[nx, ny] = true;
          queue.Enqueue((nx, ny));
        }
      }
    }

    return cells;
  }

  private static List<Vector2> TraceEdgeLoop(List<(Vector2 p1, Vector2 p2)> edges) {
    var contour = new List<Vector2>();

    if (edges.Count == 0) {
      return contour;
    }

    // Используем словарь для O(1) поиска следующего ребра
    var edgeMap = new Dictionary<(int, int), List<int>>();
    for (var i = 0; i < edges.Count; i++) {
      var key = QuantizePoint(edges[i].p1);

      if (!edgeMap.TryGetValue(key, out var list)) {
        list = [];
        edgeMap[key] = list;
      }

      list.Add(i);
    }

    var current = edges[0].p1;
    contour.Add(current);
    var used = new HashSet<int> { 0 };
    current = edges[0].p2;

    while (used.Count < edges.Count) {
      contour.Add(current);

      var key = QuantizePoint(current);
      var nextIdx = -1;
      if (edgeMap.TryGetValue(key, out var candidates)) {
        foreach (var idx in candidates) {
          if (!used.Contains(idx)) {
            nextIdx = idx;
            break;
          }
        }
      }

      if (nextIdx == -1) break;
      used.Add(nextIdx);
      current = edges[nextIdx].p2;

      if (contour.Count > 10000) break;
    }

    return contour;
  }

  private static (int, int) QuantizePoint(Vector2 p) =>
    ((int)MathF.Round(p.X * 1000), (int)MathF.Round(p.Y * 1000));

  private static List<Vector2> SimplifyContour(List<Vector2> vertices) {
    if (vertices.Count < 3) return vertices;

    var simplified = new List<Vector2>();
    var n = vertices.Count;

    for (var i = 0; i < n; i++) {
      var prev = vertices[(i - 1 + n) % n];
      var curr = vertices[i];
      var next = vertices[(i + 1) % n];

      if (!IsCollinear(prev, curr, next)) {
        simplified.Add(curr);
      }
    }

    return simplified.Count >= 3 ? simplified : vertices;
  }

  private static bool IsCollinear(Vector2 a, Vector2 b, Vector2 c) {
    var cross = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
    return MathF.Abs(cross) < 0.001f;
  }
}
