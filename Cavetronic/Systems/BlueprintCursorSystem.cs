using Arch.Core;

namespace Cavetronic.Systems;

// Обновляет BlueprintMesh.HoveredVertexId на основе позиции курсора.
// Должна выполняться до систем выделения и удаления.
public class BlueprintCursorSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private const float HoverRadius = 0.4f;

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

      var bestId = 0;
      var bestDist = HoverRadius * HoverRadius;

      GameWorld.Ecs.Query(in _verticesQuery, (
        ref StableId stableId,
        ref BlueprintVertex vertex
      ) => {
        var dx = vertex.X - cx;
        var dy = vertex.Y - cy;
        var dist2 = dx * dx + dy * dy;

        if (dist2 < bestDist) {
          bestDist = dist2;
          bestId = stableId.Id;
        }
      });

      mesh.HoveredVertexId = bestId;
    });
  }
}
