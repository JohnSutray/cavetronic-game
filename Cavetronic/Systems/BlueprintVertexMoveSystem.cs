using Arch.Core;

namespace Cavetronic.Systems;

// Перемещает выделенную вершину во время LMB drag.
public class BlueprintVertexMoveSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private readonly QueryDescription _blueprintQuery =
    new QueryDescription().WithAll<
      Blueprint,
      BlueprintMesh,
      ControlSubjectInput<CursorLeftMoveAction>
    >();

  public override void Tick(float dt) {
    GameWorld.Ecs.Query(in _blueprintQuery, (
      ref BlueprintMesh mesh,
      ref ControlSubjectInput<CursorLeftMoveAction> lmb
    ) => {
      // Работаем только на hold-фреймах (не на press-фрейме)
      if (!lmb.PreviouslyActive || !lmb.Active) {
        return;
      }

      var dragId = GetDragTargetId(ref mesh);

      if (dragId == 0) {
        return;
      }

      if (!GameWorld.TryGetEntity(dragId, out var vertexEntity)) {
        return;
      }

      ref var vertex = ref GameWorld.Ecs.Get<BlueprintVertex>(vertexEntity);

      var nx = lmb.Payload.EndX;
      var ny = lmb.Payload.EndY;

      if (!BlueprintGeometry.IsVertexMoveValid(dragId, nx, ny, mesh.Triangles, GameWorld)) {
        return;
      }

      vertex.X = nx;
      vertex.Y = ny;
    });
  }

  // Возвращает StableId вершины, которую нужно тащить.
  // Если выделены две — тащим SelectedId2 (последнюю выделенную) и сбрасываем первую.
  private static int GetDragTargetId(ref BlueprintMesh mesh) {
    if (mesh.SelectedId2 != 0) {
      mesh.SelectedId1 = 0;
      return mesh.SelectedId2;
    }

    return mesh.SelectedId1;
  }
}
