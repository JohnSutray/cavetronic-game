namespace Cavetronic;

public static class BlueprintGeometry {
  private const float Epsilon = 1e-6f;

  // ── Примитивы ──────────────────────────────────────────────────────────────

  private static float Cross(float ax, float ay, float bx, float by) =>
    ax * by - ay * bx;

  private static float Side(float px, float py, float ax, float ay, float bx, float by) =>
    Cross(bx - ax, by - ay, px - ax, py - ay);

  // Правильное пересечение отрезков (строгое — общие концы не считаются пересечением).
  public static bool SegmentsIntersect(
    float ax, float ay, float bx, float by,
    float cx, float cy, float dx, float dy
  ) {
    var d1 = Side(cx, cy, ax, ay, bx, by);
    var d2 = Side(dx, dy, ax, ay, bx, by);
    var d3 = Side(ax, ay, cx, cy, dx, dy);
    var d4 = Side(bx, by, cx, cy, dx, dy);

    return d1 * d2 < 0 && d3 * d4 < 0;
  }

  // Точка внутри треугольника (включая границу).
  public static bool PointInTriangle(
    float px, float py,
    float ax, float ay, float bx, float by, float cx, float cy
  ) {
    var d1 = Side(px, py, ax, ay, bx, by);
    var d2 = Side(px, py, bx, by, cx, cy);
    var d3 = Side(px, py, cx, cy, ax, ay);

    var hasNeg = d1 < 0 || d2 < 0 || d3 < 0;
    var hasPos = d1 > 0 || d2 > 0 || d3 > 0;

    return !(hasNeg && hasPos);
  }

  // ── Mesh-утилиты ───────────────────────────────────────────────────────────

  public static bool IsInsideMesh(float px, float py, int[] triangles, GameWorld world) {
    for (var i = 0; i < triangles.Length; i += 3) {
      var (ax, ay) = GetVertexPos(triangles[i], world);
      var (bx, by) = GetVertexPos(triangles[i + 1], world);
      var (cx, cy) = GetVertexPos(triangles[i + 2], world);

      if (PointInTriangle(px, py, ax, ay, bx, by, cx, cy)) {
        return true;
      }
    }

    return false;
  }

  public static bool AreAdjacent(int idA, int idB, int[] triangles) {
    for (var i = 0; i < triangles.Length; i += 3) {
      var hasA = triangles[i] == idA || triangles[i + 1] == idA || triangles[i + 2] == idA;
      var hasB = triangles[i] == idB || triangles[i + 1] == idB || triangles[i + 2] == idB;

      if (hasA && hasB) {
        return true;
      }
    }

    return false;
  }

  // Все рёбра меша без дублей (порядок вершин нормализован: меньший StableId первый).
  public static List<(int A, int B)> GetEdges(int[] triangles) {
    var seen = new HashSet<(int, int)>();
    var edges = new List<(int, int)>();

    for (var i = 0; i < triangles.Length; i += 3) {
      AddEdge(triangles[i], triangles[i + 1], seen, edges);
      AddEdge(triangles[i + 1], triangles[i + 2], seen, edges);
      AddEdge(triangles[i + 2], triangles[i], seen, edges);
    }

    return edges;
  }

  private static void AddEdge(int a, int b, HashSet<(int, int)> seen, List<(int, int)> edges) {
    var key = a < b ? (a, b) : (b, a);

    if (seen.Add(key)) {
      edges.Add(key);
    }
  }

  // Новая вершина в (wx, wy) валидна, если:
  // 1) она не внутри меша,
  // 2) W находится на противоположной стороне базового ребра v1-v2 от всех треугольников,
  //    которые уже это ребро используют (иначе новый треугольник перекроет существующий),
  // 3) новые рёбра W→v1 и W→v2 не пересекают рёбра меша (каждый проверяется
  //    только против рёбер, не делящих его endpoint).
  public static bool IsValidNewVertex(
    float wx, float wy,
    int v1Id, int v2Id,
    int[] triangles,
    GameWorld world
  ) {
    if (IsInsideMesh(wx, wy, triangles, world)) {
      return false;
    }

    var (v1x, v1y) = GetVertexPos(v1Id, world);
    var (v2x, v2y) = GetVertexPos(v2Id, world);

    // Проверка обмотки: W должна быть на стороне, противоположной третьей вершине
    // любого существующего треугольника, делящего ребро v1-v2.
    for (var i = 0; i < triangles.Length; i += 3) {
      var t0 = triangles[i];
      var t1 = triangles[i + 1];
      var t2 = triangles[i + 2];

      var hasV1 = t0 == v1Id || t1 == v1Id || t2 == v1Id;
      var hasV2 = t0 == v2Id || t1 == v2Id || t2 == v2Id;

      if (!hasV1 || !hasV2) {
        continue;
      }

      // Третья вершина этого треугольника (не v1 и не v2)
      var thirdId = t0 != v1Id && t0 != v2Id ? t0 : t1 != v1Id && t1 != v2Id ? t1 : t2;
      var (tx, ty) = GetVertexPos(thirdId, world);

      var existingSide = Side(tx, ty, v1x, v1y, v2x, v2y);
      var newSide = Side(wx, wy, v1x, v1y, v2x, v2y);

      // W должна быть строго с другой стороны от третьей вершины
      if (newSide * existingSide >= 0) {
        return false;
      }
    }

    // Проверка рёбер: W→v1 и W→v2 не должны пересекать рёбра меша.
    // Каждое новое ребро проверяется только против рёбер, не делящих его endpoint
    // (рёбра с общим endpoint не могут дать строгое пересечение).
    foreach (var (a, b) in GetEdges(triangles)) {
      // Базовое ребро v1-v2 пропускаем — оно станет общим ребром нового треугольника
      if ((a == v1Id || a == v2Id) && (b == v1Id || b == v2Id)) {
        continue;
      }

      var (ax, ay) = GetVertexPos(a, world);
      var (bx, by) = GetVertexPos(b, world);

      if (a != v1Id && b != v1Id && SegmentsIntersect(wx, wy, v1x, v1y, ax, ay, bx, by)) {
        return false;
      }

      if (a != v2Id && b != v2Id && SegmentsIntersect(wx, wy, v2x, v2y, ax, ay, bx, by)) {
        return false;
      }
    }

    return true;
  }

  // Возвращает true, если перемещение вершины vertexId в (nx, ny) не создаёт пересечений рёбер,
  // не инвертирует обмотку треугольников и не помещает вершину внутрь чужого треугольника.
  public static bool IsVertexMoveValid(
    int vertexId,
    float nx, float ny,
    int[] triangles,
    GameWorld world
  ) {
    var (origX, origY) = GetVertexPos(vertexId, world);
    var edges = GetEdges(triangles);

    var movingEdges = edges.Where(e => e.A == vertexId || e.B == vertexId).ToList();
    var staticEdges = edges.Where(e => e.A != vertexId && e.B != vertexId).ToList();

    // Проверка 1: движущиеся рёбра не пересекают статичные рёбра.
    foreach (var (mA, mB) in movingEdges) {
      var otherId = mA == vertexId ? mB : mA;
      var (ox, oy) = GetVertexPos(otherId, world);

      foreach (var (sA, sB) in staticEdges) {
        if (sA == otherId || sB == otherId) {
          continue;
        }

        var (sax, say) = GetVertexPos(sA, world);
        var (sbx, sby) = GetVertexPos(sB, world);

        if (SegmentsIntersect(nx, ny, ox, oy, sax, say, sbx, sby)) {
          return false;
        }
      }
    }

    // Проверка 2: обмотка каждого треугольника, содержащего вершину, должна сохраниться.
    // Это ловит случай, когда вершина "проваливается" через базовое ребро треугольника —
    // пересечение рёбер не возникает (смежные рёбра пропускаются), но треугольник инвертируется.
    for (var i = 0; i < triangles.Length; i += 3) {
      var t0 = triangles[i];
      var t1 = triangles[i + 1];
      var t2 = triangles[i + 2];

      if (t0 != vertexId && t1 != vertexId && t2 != vertexId) {
        continue;
      }

      int baseA, baseB;

      if (t0 == vertexId) { baseA = t1; baseB = t2; }
      else if (t1 == vertexId) { baseA = t0; baseB = t2; }
      else { baseA = t0; baseB = t1; }

      var (ax, ay) = GetVertexPos(baseA, world);
      var (bx, by) = GetVertexPos(baseB, world);

      var origSide = Side(origX, origY, ax, ay, bx, by);

      if (MathF.Abs(origSide) < Epsilon) {
        continue; // исходный треугольник вырожден — пропускаем
      }

      var newSide = Side(nx, ny, ax, ay, bx, by);

      // Другой знак или ноль (на ребре) означает инверсию или вырождение
      if (newSide * origSide <= 0) {
        return false;
      }
    }

    // Проверка 3: новая позиция вершины не должна находиться внутри чужого треугольника.
    for (var i = 0; i < triangles.Length; i += 3) {
      var t0 = triangles[i];
      var t1 = triangles[i + 1];
      var t2 = triangles[i + 2];

      if (t0 == vertexId || t1 == vertexId || t2 == vertexId) {
        continue;
      }

      var (ax, ay) = GetVertexPos(t0, world);
      var (bx, by) = GetVertexPos(t1, world);
      var (cx, cy) = GetVertexPos(t2, world);

      if (PointInTriangle(nx, ny, ax, ay, bx, by, cx, cy)) {
        return false;
      }
    }

    return true;
  }

  // ── Ear-clipping триангуляция ───────────────────────────────────────────────

  // Триангулирует полигон из vertex StableId-ов (порядок важен).
  // Возвращает список треугольников — каждый int[3] из StableId вершин.
  public static List<int[]> EarClipTriangulate(List<int> vertexIds, GameWorld world) {
    var result = new List<int[]>();

    if (vertexIds.Count < 3) {
      return result;
    }

    var remaining = new List<int>(vertexIds);
    var signedArea = ComputeSignedArea(remaining, world);

    if (MathF.Abs(signedArea) < Epsilon) {
      return result;
    }

    var isCcw = signedArea > 0;
    var maxIter = remaining.Count * remaining.Count + 10;

    for (var iter = 0; remaining.Count > 3 && iter < maxIter; iter++) {
      var earFound = false;

      for (var i = 0; i < remaining.Count; i++) {
        var prev = (i - 1 + remaining.Count) % remaining.Count;
        var next = (i + 1) % remaining.Count;

        if (!IsEar(remaining[prev], remaining[i], remaining[next], remaining, isCcw, world)) {
          continue;
        }

        result.Add([remaining[prev], remaining[i], remaining[next]]);
        remaining.RemoveAt(i);
        earFound = true;
        break;
      }

      if (!earFound) {
        break;
      }
    }

    if (remaining.Count == 3) {
      result.Add([remaining[0], remaining[1], remaining[2]]);
    }

    return result;
  }

  // Сортирует boundary-вершины по углу вокруг удалённой вершины (CCW).
  public static List<int> SortBoundaryAroundVertex(
    float centerX, float centerY,
    List<int> boundaryIds,
    GameWorld world
  ) {
    return boundaryIds
      .OrderBy(id => {
        var (bx, by) = GetVertexPos(id, world);
        return MathF.Atan2(by - centerY, bx - centerX);
      })
      .ToList();
  }

  private static float ComputeSignedArea(List<int> vertexIds, GameWorld world) {
    var area = 0f;
    var n = vertexIds.Count;

    for (var i = 0; i < n; i++) {
      var j = (i + 1) % n;
      var (xi, yi) = GetVertexPos(vertexIds[i], world);
      var (xj, yj) = GetVertexPos(vertexIds[j], world);
      area += xi * yj - xj * yi;
    }

    return area / 2f;
  }

  private static bool IsEar(
    int prevId, int currId, int nextId,
    List<int> remaining,
    bool isCcw,
    GameWorld world
  ) {
    var (px, py) = GetVertexPos(prevId, world);
    var (cx, cy) = GetVertexPos(currId, world);
    var (nx, ny) = GetVertexPos(nextId, world);

    var cross = Cross(cx - px, cy - py, nx - px, ny - py);

    if (isCcw && cross <= 0) {
      return false;
    }

    if (!isCcw && cross >= 0) {
      return false;
    }

    foreach (var otherId in remaining) {
      if (otherId == prevId || otherId == currId || otherId == nextId) {
        continue;
      }

      var (ox, oy) = GetVertexPos(otherId, world);

      if (PointInTriangle(ox, oy, px, py, cx, cy, nx, ny)) {
        return false;
      }
    }

    return true;
  }

  // ── Хелперы ────────────────────────────────────────────────────────────────

  public static (float X, float Y) GetVertexPos(int stableId, GameWorld world) {
    if (!world.TryGetEntity(stableId, out var entity)) {
      return (0f, 0f);
    }

    var v = world.Ecs.Get<BlueprintVertex>(entity);
    return (v.X, v.Y);
  }
}
