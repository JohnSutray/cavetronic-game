using Arch.Core;

namespace Cavetronic.Systems;

// Удаляет вершину по RMB-клику с автоматической перетриангуляцией дыры.
public class BlueprintVertexDeleteSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private readonly QueryDescription _blueprintQuery =
    new QueryDescription().WithAll<
      Blueprint,
      BlueprintMesh,
      ControlSubjectInput<CursorRightClickAction>
    >();

  private readonly QueryDescription _verticesQuery =
    new QueryDescription().WithAll<StableId, BlueprintVertex>();

  public override void Tick(float dt) {
    GameWorld.Ecs.Query(in _blueprintQuery, (
      ref BlueprintMesh mesh,
      ref ControlSubjectInput<CursorRightClickAction> rclick
    ) => {
      if (!rclick.Active && !rclick.PreviouslyActive) {
        return;
      }

      var targetId = mesh.HoveredVertexId;

      if (targetId == 0) {
        return;
      }

      TryDeleteVertex(ref mesh, targetId);
    });
  }

  private void TryDeleteVertex(ref BlueprintMesh mesh, int targetId) {
    var totalVertices = CountUniqueVertices(mesh.Triangles);

    if (totalVertices <= 3) {
      return; // Нельзя удалить вершину из одиночного треугольника
    }

    // Собираем вершины, смежные с удаляемой (из её треугольников)
    var adjacentIds = new HashSet<int>();
    var holeTriangleCount = 0;

    for (var i = 0; i < mesh.Triangles.Length; i += 3) {
      var v0 = mesh.Triangles[i];
      var v1 = mesh.Triangles[i + 1];
      var v2 = mesh.Triangles[i + 2];

      if (v0 != targetId && v1 != targetId && v2 != targetId) {
        continue;
      }

      holeTriangleCount++;

      if (v0 != targetId) adjacentIds.Add(v0);
      if (v1 != targetId) adjacentIds.Add(v1);
      if (v2 != targetId) adjacentIds.Add(v2);
    }

    // Удаляем треугольники с этой вершиной
    var newTriangles = new List<int>();

    for (var i = 0; i < mesh.Triangles.Length; i += 3) {
      if (mesh.Triangles[i] == targetId
        || mesh.Triangles[i + 1] == targetId
        || mesh.Triangles[i + 2] == targetId) {
        continue;
      }

      newTriangles.Add(mesh.Triangles[i]);
      newTriangles.Add(mesh.Triangles[i + 1]);
      newTriangles.Add(mesh.Triangles[i + 2]);
    }

    mesh.Triangles = newTriangles.ToArray();

    // Сбрасываем выделение если удалённая вершина была выделена
    if (mesh.SelectedId1 == targetId) mesh.SelectedId1 = 0;
    if (mesh.SelectedId2 == targetId) mesh.SelectedId2 = 0;
    if (mesh.HoveredVertexId == targetId) mesh.HoveredVertexId = 0;

    // Удаляем саму вершину
    if (GameWorld.TryGetEntity(targetId, out var targetEntity)) {
      var (vx, vy) = BlueprintGeometry.GetVertexPos(targetId, GameWorld);

      GameWorld.PendingDestroy.Add((targetEntity, targetId));

      // Перетриангулируем дыру
      if (adjacentIds.Count >= 3) {
        var boundary = BlueprintGeometry.SortBoundaryAroundVertex(
          vx, vy,
          adjacentIds.ToList(),
          GameWorld
        );

        var newTris = BlueprintGeometry.EarClipTriangulate(boundary, GameWorld);

        foreach (var tri in newTris) {
          newTriangles.Add(tri[0]);
          newTriangles.Add(tri[1]);
          newTriangles.Add(tri[2]);
        }

        mesh.Triangles = newTriangles.ToArray();
      }
    }

    // Удаляем осиротевшие вершины (не задействованные ни в одном треугольнике)
    var referencedIds = new HashSet<int>(mesh.Triangles);

    foreach (var adjId in adjacentIds) {
      if (referencedIds.Contains(adjId)) {
        continue;
      }

      if (GameWorld.TryGetEntity(adjId, out var orphanEntity)) {
        GameWorld.PendingDestroy.Add((orphanEntity, adjId));
      }
    }
  }

  private static int CountUniqueVertices(int[] triangles) {
    var ids = new HashSet<int>();

    foreach (var id in triangles) {
      ids.Add(id);
    }

    return ids.Count;
  }
}
