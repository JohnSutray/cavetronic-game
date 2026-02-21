using Arch.Core;

namespace Cavetronic.Systems;

// Обрабатывает LMB press: выделение вершин и создание нового треугольника.
public class BlueprintVertexSelectSystem(GameWorld gameWorld) : EcsSystem(gameWorld) {
  private readonly QueryDescription _blueprintQuery =
    new QueryDescription().WithAll<
      Blueprint,
      BlueprintMesh,
      ControlSubjectInput<CursorLeftMoveAction>
    >();

  // Вынесено в поле, чтобы лямбда захватывала только `this` (без DisplayClass).
  private bool _hasShift;

  public override void Tick(float dt) {
    _hasShift = GameWorld.QueryHasBlueprintShift();

    GameWorld.Ecs.Query(in _blueprintQuery, (
      ref BlueprintMesh mesh,
      ref ControlSubjectInput<CursorLeftMoveAction> lmb
    ) => {
      // Только на первом фрейме нажатия
      if (lmb.PreviouslyActive || !lmb.Active) {
        return;
      }

      var clickX = lmb.Payload.EndX;
      var clickY = lmb.Payload.EndY;
      var shiftHeld = _hasShift;
      var hoveredId = mesh.HoveredVertexId;

      if (hoveredId != 0) {
        // Два выделены + клик на третью существующую → попытка сформировать треугольник из трёх вершин
        if (mesh.SelectedId1 != 0 && mesh.SelectedId2 != 0
          && hoveredId != mesh.SelectedId1 && hoveredId != mesh.SelectedId2) {
          TryCreateTriangleFromExisting(ref mesh, hoveredId);
        }
        else {
          HandleVertexClick(ref mesh, hoveredId, shiftHeld);
        }
      }
      else if (BlueprintGeometry.IsInsideMesh(clickX, clickY, mesh.Triangles, GameWorld)) {
        // Клик внутри меша — сбрасываем выделение
        mesh.SelectedId1 = 0;
        mesh.SelectedId2 = 0;
      }
      else if (mesh.SelectedId1 != 0 && mesh.SelectedId2 != 0) {
        // Клик в пустой области с двумя выделенными → попытка создать треугольник
        TryCreateTriangle(ref mesh, clickX, clickY);
      }
    });
  }

  private void HandleVertexClick(ref BlueprintMesh mesh, int vertexId, bool shift) {
    if (shift && mesh.SelectedId1 != 0) {
      // Shift+click: добавить в выделение если смежная и ещё нет двух
      if (mesh.SelectedId2 == 0
        && vertexId != mesh.SelectedId1
        && BlueprintGeometry.AreAdjacent(mesh.SelectedId1, vertexId, mesh.Triangles)) {
        mesh.SelectedId2 = vertexId;
      }
    }
    else {
      // Обычный click: выбрать только эту вершину
      mesh.SelectedId1 = vertexId;
      mesh.SelectedId2 = 0;
    }
  }

  private void TryCreateTriangle(ref BlueprintMesh mesh, float clickX, float clickY) {
    var v1Id = mesh.SelectedId1;
    var v2Id = mesh.SelectedId2;

    if (!BlueprintGeometry.IsValidNewVertex(clickX, clickY, v1Id, v2Id, mesh.Triangles, GameWorld)) {
      return;
    }

    var newStableId = GameWorld.NextStableId();
    var newEntity = GameWorld.Ecs.Create(
      new StableId { Id = newStableId },
      new BlueprintVertex { X = clickX, Y = clickY }
    );
    GameWorld.RegisterEntity(newStableId, newEntity);

    AppendTriangle(ref mesh, v1Id, v2Id, newStableId);
  }

  private void TryCreateTriangleFromExisting(ref BlueprintMesh mesh, int v3Id) {
    var v1Id = mesh.SelectedId1;
    var v2Id = mesh.SelectedId2;

    if (!BlueprintGeometry.IsValidTriangleFromExisting(v1Id, v2Id, v3Id, mesh.Triangles, GameWorld)) {
      return;
    }

    AppendTriangle(ref mesh, v1Id, v2Id, v3Id);
  }

  private static void AppendTriangle(ref BlueprintMesh mesh, int v1Id, int v2Id, int v3Id) {
    var oldLen = mesh.Triangles.Length;
    var newTriangles = new int[oldLen + 3];
    Array.Copy(mesh.Triangles, newTriangles, oldLen);
    newTriangles[oldLen] = v1Id;
    newTriangles[oldLen + 1] = v2Id;
    newTriangles[oldLen + 2] = v3Id;
    mesh.Triangles = newTriangles;

    mesh.SelectedId1 = 0;
    mesh.SelectedId2 = 0;
  }
}

// Хелпер для проверки Shift без query-параметра внутри другого query
file static class BlueprintQueryExtensions {
  // Проверяет, есть ли активный ShiftModifier на любой Blueprint-сущности.
  // Простой способ: Blueprint-сущность несёт ShiftModifier в ControlSubjectInput<ShiftModifier>.
  public static bool QueryHasBlueprintShift(this GameWorld world) {
    var found = false;
    var query = new Arch.Core.QueryDescription()
      .WithAll<Blueprint, ControlSubjectInput<ShiftModifier>>();

    world.Ecs.Query(in query, (ref ControlSubjectInput<ShiftModifier> shift) => {
      if (shift.Active || shift.PreviouslyActive) {
        found = true;
      }
    });

    return found;
  }
}
