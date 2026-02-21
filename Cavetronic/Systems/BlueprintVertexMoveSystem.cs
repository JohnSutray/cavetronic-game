using Arch.Core;

namespace Cavetronic.Systems;

// Перемещает выделенную вершину во время LMB drag.
// Использует абсолютное позиционирование с оффсетом, захваченным в начале драга,
// чтобы избежать накопительного эффекта при несовпадении тикрейтов InputSystem и игрового цикла.
public class BlueprintVertexMoveSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private readonly QueryDescription _blueprintQuery =
    new QueryDescription().WithAll<
      Blueprint,
      BlueprintMesh,
      ControlSubjectInput<CursorLeftMoveAction>
    >();

  // Оффсет между позицией вершины и курсором в момент начала драга
  private int _dragTargetId;
  private float _dragOffsetX;
  private float _dragOffsetY;

  // Вынесено в поле, чтобы лямбда захватывала только `this` (без DisplayClass).
  private bool _dragActiveThisTick;

  public override void Tick(float dt) {
    _dragActiveThisTick = false;

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

      // Захватываем оффсет один раз при начале нового драга
      if (_dragTargetId != dragId) {
        _dragTargetId = dragId;
        _dragOffsetX = vertex.X - lmb.Payload.EndX;
        _dragOffsetY = vertex.Y - lmb.Payload.EndY;
      }

      _dragActiveThisTick = true;

      var nx = lmb.Payload.EndX + _dragOffsetX;
      var ny = lmb.Payload.EndY + _dragOffsetY;

      if (!BlueprintGeometry.IsVertexMoveValid(dragId, nx, ny, mesh.Triangles, GameWorld)) {
        return;
      }

      vertex.X = nx;
      vertex.Y = ny;
    });

    if (!_dragActiveThisTick) {
      _dragTargetId = 0;
    }
  }

  private static int GetDragTargetId(ref BlueprintMesh mesh) {
    if (mesh.SelectedId2 != 0) {
      return mesh.SelectedId2;
    }

    return mesh.SelectedId1;
  }
}
