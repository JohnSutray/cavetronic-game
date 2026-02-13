using nkast.Aether.Physics2D.Common;

namespace Cavetronic.Generation;

public static class SimpleIslandTracer {
  // Извлекает острова из сетки - каждый остров это список вершин прямоугольного контура
  public static List<List<Vector2>> ExtractIslands(bool[,] grid, float cellSize) {
    var width = grid.GetLength(0);
    var height = grid.GetLength(1);
    var islands = new List<List<Vector2>>();
    var visited = new bool[width, height];

    // Находим все связные solid регионы через flood fill
    for (int x = 0; x < width; x++) {
      for (int y = 0; y < height; y++) {
        if (grid[x, y] && !visited[x, y]) {
          // Нашли новый остров
          var cells = FloodFill(grid, visited, x, y);
          if (cells.Count >= 1) {
            // Создаём прямоугольный контур вокруг острова
            var contour = CreateRectangularContour(cells, cellSize);
            islands.Add(contour);
          }
        }
      }
    }

    return islands;
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

      // Проверяем 4 соседа
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

  private static List<Vector2> CreateRectangularContour(List<(int x, int y)> cells, float cellSize) {
    // Создаём контур из граничных рёбер клеток
    var cellSet = new HashSet<(int x, int y)>(cells);
    var edges = new List<(Vector2 p1, Vector2 p2)>();

    // Для каждой клетки добавляем рёбра, которые граничат с empty
    foreach (var (x, y) in cells) {
      if (!cellSet.Contains((x - 1, y))) {
        // Left edge
        edges.Add((new Vector2(x * cellSize, y * cellSize),
                   new Vector2(x * cellSize, (y + 1) * cellSize)));
      }
      if (!cellSet.Contains((x + 1, y))) {
        // Right edge
        edges.Add((new Vector2((x + 1) * cellSize, (y + 1) * cellSize),
                   new Vector2((x + 1) * cellSize, y * cellSize)));
      }
      if (!cellSet.Contains((x, y - 1))) {
        // Top edge
        edges.Add((new Vector2((x + 1) * cellSize, y * cellSize),
                   new Vector2(x * cellSize, y * cellSize)));
      }
      if (!cellSet.Contains((x, y + 1))) {
        // Bottom edge
        edges.Add((new Vector2(x * cellSize, (y + 1) * cellSize),
                   new Vector2((x + 1) * cellSize, (y + 1) * cellSize)));
      }
    }

    if (edges.Count == 0) {
      // Fallback
      var minX = cells.Min(c => c.x);
      var maxX = cells.Max(c => c.x);
      var minY = cells.Min(c => c.y);
      var maxY = cells.Max(c => c.y);
      return new List<Vector2> {
        new Vector2(minX * cellSize, minY * cellSize),
        new Vector2((maxX + 1) * cellSize, minY * cellSize),
        new Vector2((maxX + 1) * cellSize, (maxY + 1) * cellSize),
        new Vector2(minX * cellSize, (maxY + 1) * cellSize)
      };
    }

    // Обходим рёбра в правильном порядке
    var contour = TraceEdgeLoop(edges);
    return SimplifyContour(contour);
  }

  private static List<Vector2> TraceEdgeLoop(List<(Vector2 p1, Vector2 p2)> edges) {
    var contour = new List<Vector2>();
    if (edges.Count == 0) return contour;

    var current = edges[0].p1;
    contour.Add(current);
    var used = new HashSet<int>();
    used.Add(0);
    current = edges[0].p2;

    while (used.Count < edges.Count) {
      contour.Add(current);

      // Ищем следующее ребро, которое начинается в current
      int nextIdx = -1;
      for (int i = 0; i < edges.Count; i++) {
        if (used.Contains(i)) continue;
        if (Distance(edges[i].p1, current) < 0.001f) {
          nextIdx = i;
          break;
        }
      }

      if (nextIdx == -1) break;

      used.Add(nextIdx);
      current = edges[nextIdx].p2;

      if (contour.Count > 10000) break; // Safety
    }

    return contour;
  }

  private static float Distance(Vector2 a, Vector2 b) {
    var dx = a.X - b.X;
    var dy = a.Y - b.Y;
    return MathF.Sqrt(dx * dx + dy * dy);
  }

  private static List<Vector2> SimplifyContour(List<Vector2> vertices) {
    if (vertices.Count < 3) return vertices;

    var simplified = new List<Vector2>();
    int n = vertices.Count;

    for (int i = 0; i < n; i++) {
      var prev = vertices[(i - 1 + n) % n];
      var curr = vertices[i];
      var next = vertices[(i + 1) % n];

      // Проверяем, лежит ли curr на прямой между prev и next
      if (!IsCollinear(prev, curr, next)) {
        simplified.Add(curr);
      }
    }

    return simplified.Count >= 3 ? simplified : vertices;
  }

  private static bool IsCollinear(Vector2 a, Vector2 b, Vector2 c) {
    // Вычисляем векторное произведение
    var cross = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
    return MathF.Abs(cross) < 0.001f;
  }
}
