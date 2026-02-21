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

  private readonly HashSet<int> _removeSet = new();
  private readonly HashSet<int> _remaining = new();
  private readonly HashSet<int> _candidates = new();
  private readonly List<int> _orphaned = new();
  private readonly List<int> _newTriangles = new();

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
    _removeSet.Clear();

    for (var i = 0; i < mesh.Triangles.Length; i += 3) {
      var hasA = mesh.Triangles[i] == edgeA || mesh.Triangles[i + 1] == edgeA || mesh.Triangles[i + 2] == edgeA;
      var hasB = mesh.Triangles[i] == edgeB || mesh.Triangles[i + 1] == edgeB || mesh.Triangles[i + 2] == edgeB;

      if (hasA && hasB) {
        _removeSet.Add(i);
      }
    }

    if (_removeSet.Count == 0) {
      return;
    }

    // Вершины, которые останутся после удаления
    _remaining.Clear();

    for (var i = 0; i < mesh.Triangles.Length; i += 3) {
      if (_removeSet.Contains(i)) {
        continue;
      }

      _remaining.Add(mesh.Triangles[i]);
      _remaining.Add(mesh.Triangles[i + 1]);
      _remaining.Add(mesh.Triangles[i + 2]);
    }

    // Не удаляем, если меш деградирует до менее 3 вершин
    if (_remaining.Count < 3) {
      return;
    }

    // Вершины из удаляемых треугольников, которые не попали в _remaining
    _candidates.Clear();

    for (var i = 0; i < mesh.Triangles.Length; i += 3) {
      if (!_removeSet.Contains(i)) {
        continue;
      }

      _candidates.Add(mesh.Triangles[i]);
      _candidates.Add(mesh.Triangles[i + 1]);
      _candidates.Add(mesh.Triangles[i + 2]);
    }

    _orphaned.Clear();

    foreach (var id in _candidates) {
      if (!_remaining.Contains(id)) {
        _orphaned.Add(id);
      }
    }

    // Строим новый массив треугольников
    _newTriangles.Clear();

    for (var i = 0; i < mesh.Triangles.Length; i += 3) {
      if (_removeSet.Contains(i)) {
        continue;
      }

      _newTriangles.Add(mesh.Triangles[i]);
      _newTriangles.Add(mesh.Triangles[i + 1]);
      _newTriangles.Add(mesh.Triangles[i + 2]);
    }

    mesh.Triangles = _newTriangles.ToArray();

    // Сбрасываем выделение и ховер на осиротевших вершинах
    foreach (var id in _orphaned) {
      if (mesh.SelectedId1 == id) mesh.SelectedId1 = 0;
      if (mesh.SelectedId2 == id) mesh.SelectedId2 = 0;
      if (mesh.HoveredVertexId == id) mesh.HoveredVertexId = 0;
    }

    mesh.HoveredEdgeA = 0;
    mesh.HoveredEdgeB = 0;

    // Уничтожаем осиротевшие ECS-сущности
    foreach (var id in _orphaned) {
      if (GameWorld.TryGetEntity(id, out var entity)) {
        GameWorld.PendingDestroy.Add((entity, id));
      }
    }
  }
}
