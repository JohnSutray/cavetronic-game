using Arch.Core;

namespace Cavetronic.Systems;

// Обновляет HoveredVertexId и HoveredEdgeA/B в BlueprintMesh.
// Ребро ховерится только когда ни одна вершина не заховерена.
// Должна выполняться до систем выделения и удаления.
public class BlueprintCursorSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private const float VertexHoverRadius = 0.4f;
  private const float EdgeHoverRadius = 0.2f;

  private readonly QueryDescription _blueprintQuery =
    new QueryDescription().WithAll<Blueprint, BlueprintMesh, ControlSubjectInput<CursorInput>>();

  private readonly QueryDescription _verticesQuery =
    new QueryDescription().WithAll<StableId, BlueprintVertex>();

  private readonly List<(int A, int B)> _edges = new();
  private readonly HashSet<(int, int)> _edgeSeen = new();

  // Промежуточное состояние, вынесенное из lambda-тел в поля,
  // чтобы вложенные лямбды захватывали только `this` и не создавали DisplayClass.
  private float _cx;
  private float _cy;
  private int _bestVertexId;
  private float _bestDist2;
  private int _hoveredEdgeA;
  private int _hoveredEdgeB;
  private float _bestEdgeDist;

  public override void Tick(float dt) {
    GameWorld.Ecs.Query(in _blueprintQuery, (
      ref BlueprintMesh mesh,
      ref ControlSubjectInput<CursorInput> cursorInput
    ) => {
      if (!cursorInput.Active && !cursorInput.PreviouslyActive) {
        return;
      }

      _cx = cursorInput.Payload.WorldX;
      _cy = cursorInput.Payload.WorldY;

      // Vertex hover
      _bestVertexId = 0;
      _bestDist2 = VertexHoverRadius * VertexHoverRadius;

      GameWorld.Ecs.Query(in _verticesQuery, (
        ref StableId stableId,
        ref BlueprintVertex vertex
      ) => {
        var dx = vertex.X - _cx;
        var dy = vertex.Y - _cy;
        var dist2 = dx * dx + dy * dy;

        if (dist2 < _bestDist2) {
          _bestDist2 = dist2;
          _bestVertexId = stableId.Id;
        }
      });

      mesh.HoveredVertexId = _bestVertexId;

      // Edge hover — только когда ни одна вершина не заховерена
      _hoveredEdgeA = 0;
      _hoveredEdgeB = 0;

      if (_bestVertexId == 0) {
        _bestEdgeDist = EdgeHoverRadius;

        BlueprintGeometry.PopulateEdges(mesh.Triangles, _edges, _edgeSeen);

        foreach (var (a, b) in _edges) {
          var (ax, ay) = BlueprintGeometry.GetVertexPos(a, GameWorld);
          var (bx, by) = BlueprintGeometry.GetVertexPos(b, GameWorld);
          var dist = BlueprintGeometry.DistanceToSegment(_cx, _cy, ax, ay, bx, by);

          if (dist < _bestEdgeDist) {
            _bestEdgeDist = dist;
            _hoveredEdgeA = a;
            _hoveredEdgeB = b;
          }
        }
      }

      mesh.HoveredEdgeA = _hoveredEdgeA;
      mesh.HoveredEdgeB = _hoveredEdgeB;
    });
  }
}
