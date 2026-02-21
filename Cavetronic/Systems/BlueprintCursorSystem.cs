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

  public override void Tick(float dt) {
    GameWorld.Ecs.Query(in _blueprintQuery, (
      ref BlueprintMesh mesh,
      ref ControlSubjectInput<CursorInput> cursorInput
    ) => {
      if (!cursorInput.Active && !cursorInput.PreviouslyActive) {
        return;
      }

      var cx = cursorInput.Payload.WorldX;
      var cy = cursorInput.Payload.WorldY;

      // Vertex hover
      var bestVertexId = 0;
      var bestDist2 = VertexHoverRadius * VertexHoverRadius;

      GameWorld.Ecs.Query(in _verticesQuery, (
        ref StableId stableId,
        ref BlueprintVertex vertex
      ) => {
        var dx = vertex.X - cx;
        var dy = vertex.Y - cy;
        var dist2 = dx * dx + dy * dy;

        if (dist2 < bestDist2) {
          bestDist2 = dist2;
          bestVertexId = stableId.Id;
        }
      });

      mesh.HoveredVertexId = bestVertexId;

      // Edge hover — только когда ни одна вершина не заховерена
      var hoveredEdgeA = 0;
      var hoveredEdgeB = 0;

      if (bestVertexId == 0) {
        var bestEdgeDist = EdgeHoverRadius;

        foreach (var (a, b) in BlueprintGeometry.GetEdges(mesh.Triangles)) {
          var (ax, ay) = BlueprintGeometry.GetVertexPos(a, GameWorld);
          var (bx, by) = BlueprintGeometry.GetVertexPos(b, GameWorld);
          var dist = BlueprintGeometry.DistanceToSegment(cx, cy, ax, ay, bx, by);

          if (dist < bestEdgeDist) {
            bestEdgeDist = dist;
            hoveredEdgeA = a;
            hoveredEdgeB = b;
          }
        }
      }

      mesh.HoveredEdgeA = hoveredEdgeA;
      mesh.HoveredEdgeB = hoveredEdgeB;
    });
  }
}
