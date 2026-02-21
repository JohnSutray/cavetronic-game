using Arch.Core;

namespace Cavetronic.Systems;

// Удаляет ребро по ПКМ-клику, если под курсором нет вершины.
// Удаляет все треугольники, содержащие это ребро, и осиротевшие вершины.
public class BlueprintEdgeDeleteSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private readonly QueryDescription _blueprintQuery =
    new QueryDescription().WithAll<
      Blueprint,
      BlueprintMesh,
      ControlSubjectInput<CursorRightClickAction>
    >();

  public override void Tick(float dt) {
    GameWorld.Ecs.Query(in _blueprintQuery, (
      ref BlueprintMesh mesh,
      ref ControlSubjectInput<CursorRightClickAction> rclick
    ) => {
      if (!rclick.Active) {
        return;
      }

      // Приоритет у вершины — если она заховерена, её обработает BlueprintVertexDeleteSystem
      if (mesh.HoveredVertexId != 0) {
        return;
      }

      if (mesh.HoveredEdgeA == 0) {
        return;
      }

      TryDeleteEdge(ref mesh, mesh.HoveredEdgeA, mesh.HoveredEdgeB);
    });
  }

  private void TryDeleteEdge(ref BlueprintMesh mesh, int edgeA, int edgeB) {
    // Ищем треугольники, содержащие оба конца ребра
    var removeSet = new HashSet<int>(); // стартовые индексы троек

    for (var i = 0; i < mesh.Triangles.Length; i += 3) {
      var hasA = mesh.Triangles[i] == edgeA || mesh.Triangles[i + 1] == edgeA || mesh.Triangles[i + 2] == edgeA;
      var hasB = mesh.Triangles[i] == edgeB || mesh.Triangles[i + 1] == edgeB || mesh.Triangles[i + 2] == edgeB;

      if (hasA && hasB) {
        removeSet.Add(i);
      }
    }

    if (removeSet.Count == 0) {
      return;
    }

    // Вершины, которые останутся после удаления
    var remaining = new HashSet<int>();

    for (var i = 0; i < mesh.Triangles.Length; i += 3) {
      if (removeSet.Contains(i)) {
        continue;
      }

      remaining.Add(mesh.Triangles[i]);
      remaining.Add(mesh.Triangles[i + 1]);
      remaining.Add(mesh.Triangles[i + 2]);
    }

    // Не удаляем, если меш деградирует до менее 3 вершин
    if (remaining.Count < 3) {
      return;
    }

    // Вершины из удаляемых треугольников, которые не попали в remaining
    var candidates = new HashSet<int>();

    for (var i = 0; i < mesh.Triangles.Length; i += 3) {
      if (!removeSet.Contains(i)) {
        continue;
      }

      candidates.Add(mesh.Triangles[i]);
      candidates.Add(mesh.Triangles[i + 1]);
      candidates.Add(mesh.Triangles[i + 2]);
    }

    var orphaned = candidates.Except(remaining).ToList();

    // Строим новый массив треугольников
    var newTriangles = new List<int>();

    for (var i = 0; i < mesh.Triangles.Length; i += 3) {
      if (removeSet.Contains(i)) {
        continue;
      }

      newTriangles.Add(mesh.Triangles[i]);
      newTriangles.Add(mesh.Triangles[i + 1]);
      newTriangles.Add(mesh.Triangles[i + 2]);
    }

    mesh.Triangles = newTriangles.ToArray();

    // Сбрасываем выделение и ховер на осиротевших вершинах
    foreach (var id in orphaned) {
      if (mesh.SelectedId1 == id) mesh.SelectedId1 = 0;
      if (mesh.SelectedId2 == id) mesh.SelectedId2 = 0;
      if (mesh.HoveredVertexId == id) mesh.HoveredVertexId = 0;
    }

    mesh.HoveredEdgeA = 0;
    mesh.HoveredEdgeB = 0;

    // Уничтожаем осиротевшие ECS-сущности
    foreach (var id in orphaned) {
      if (GameWorld.TryGetEntity(id, out var entity)) {
        GameWorld.PendingDestroy.Add((entity, id));
      }
    }
  }
}
