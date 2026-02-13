namespace Cavetronic.Generation;

public static class CellularAutomata {
  // Сглаживание + опционально заполнение изолированных пустот
  public static bool[,] Smooth(
    bool[,] grid,
    int iterations,
    int solidThreshold,
    bool fillIsolatedVoids = false
  ) {
    var width = grid.GetLength(0);
    var height = grid.GetLength(1);
    var result = (bool[,])grid.Clone();

    // Итерации сглаживания
    for (int iter = 0; iter < iterations; iter++) {
      var temp = new bool[width, height];

      for (int x = 0; x < width; x++) {
        for (int y = 0; y < height; y++) {
          var neighbors = CountSolidNeighbors(result, x, y);
          temp[x, y] = neighbors >= solidThreshold;
        }
      }

      result = temp;
    }

    // "Задушить" пустоты, которые не касаются границ чанка
    if (fillIsolatedVoids) {
      result = FillEnclosedVoids(result);
    }

    return result;
  }

  // "Задушить" изолированные пустоты - заполняем пустоты, которые НЕ касаются границ чанка
  private static bool[,] FillEnclosedVoids(bool[,] grid) {
    var width = grid.GetLength(0);
    var height = grid.GetLength(1);
    var result = (bool[,])grid.Clone();
    var visited = new bool[width, height];
    int totalVoids = 0;
    int filledVoids = 0;

    // Проход по всей матрице
    for (int x = 0; x < width; x++) {
      for (int y = 0; y < height; y++) {
        // Нашли непосещённую пустую клетку
        if (result[x, y] || visited[x, y]) {
          continue;
        }

        totalVoids++;

        // Ищем все связанные пустые клетки
        var voidCells = new List<(int x, int y)>();
        var queue = new Queue<(int x, int y)>();
        queue.Enqueue((x, y));
        visited[x, y] = true;
        bool touchesBorder = false;

        while (queue.Count > 0) {
          var (cx, cy) = queue.Dequeue();
          voidCells.Add((cx, cy));

          // Проверяем, касается ли границы чанка
          if (cx == 0 || cx == width - 1 || cy == 0 || cy == height - 1) {
            touchesBorder = true;
          }

          // Ищем соседей (4 направления)
          var neighbors = new[] { (cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1) };

          foreach (var (nx, ny) in neighbors) {
            if (
              nx >= 0
              && nx < width
              && ny >= 0
              && ny < height
              && !result[nx, ny]
              && !visited[nx, ny]
            ) {
              visited[nx, ny] = true;
              queue.Enqueue((nx, ny));
            }
          }
        }

        // Если пустота НЕ касается границ - это изолированная дырка, закрашиваем
        if (!touchesBorder) {
          foreach (var (vx, vy) in voidCells) {
            result[vx, vy] = true;
          }

          filledVoids++;
          Console.WriteLine($"    [CA] Filled isolated void: {voidCells.Count} cells");
        }
        else {
          Console.WriteLine($"    [CA] Kept border void: {voidCells.Count} cells");
        }
      }
    }

    Console.WriteLine($"  [CA] Total: {totalVoids} voids, filled {filledVoids}, kept {totalVoids - filledVoids}");

    return result;
  }

  private static List<(int x, int y)> FloodFillEmptyIsland(bool[,] grid, bool[,] visited, int startX, int startY) {
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
            !grid[nx, ny] && !visited[nx, ny]) {
          visited[nx, ny] = true;
          queue.Enqueue((nx, ny));
        }
      }
    }

    return cells;
  }

  private static int CountSolidNeighbors(bool[,] grid, int x, int y) {
    var width = grid.GetLength(0);
    var height = grid.GetLength(1);
    var count = 0;

    for (int dx = -1; dx <= 1; dx++) {
      for (int dy = -1; dy <= 1; dy++) {
        if (dx == 0 && dy == 0) continue; // Skip center cell

        var nx = x + dx;
        var ny = y + dy;

        // Treat out-of-bounds as solid (walls at chunk edges)
        if (nx < 0 || nx >= width || ny < 0 || ny >= height) {
          count++;
        }
        else if (grid[nx, ny]) {
          count++;
        }
      }
    }

    return count;
  }
}