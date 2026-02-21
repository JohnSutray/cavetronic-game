# Сессия: Blueprint Editor — баги, фичи, инпут-архитектура

---

## Что было сделано

### 1. Исправлен drag-snap (вершина прыгала к курсору при клике)

**Причина:** MoveSystem использовал delta-движение (`vertex.X += EndX - StartX`).
При тикрейте InputSystem 30 Гц и FPS 120 тот же delta применялся ~4 раза за тик → накопительный эффект.

**Решение:** Offset-based абсолютное позиционирование.
```csharp
// Захват один раз при начале нового drag'а
_dragOffsetX = vertex.X - lmb.Payload.EndX;
_dragOffsetY = vertex.Y - lmb.Payload.EndY;

// Применение каждый тик — идемпотентно
vertex.X = lmb.Payload.EndX + _dragOffsetX;
vertex.Y = lmb.Payload.EndY + _dragOffsetY;
```

Дополнительно: убрана побочная мутация `SelectedId1 = 0` из `GetDragTargetId`.

---

### 2. Исправлена валидация перемещения вершины

**IsVertexMoveValid теперь содержит 3 проверки:**

1. **Edge crossing** — движущиеся рёбра не пересекают статичные (было, слабее)
2. **Winding** — для каждого содержащего треугольника: `sign(Side_orig) == sign(Side_new)`.
   Ловит кейс "вершина проваливается через базовое ребро" — edge crossing тест его пропускает,
   т.к. смежные рёбра пропускаются из проверки.
3. **Not inside foreign triangle** — новая позиция не попадает внутрь чужого треугольника.

---

### 3. Исправлена валидация создания нового треугольника

**Проблема 1:** При однотреугольном меше можно было нарисовать треугольник, частично перекрывающий
существующий, если новая вершина не погружалась внутрь, но ребро цепляло.

**Исправление IsValidNewVertex:**
- Winding check: W должна быть на стороне, противоположной третьей вершине любого треугольника,
  разделяющего базовое ребро v1-v2 (`newSide * existingSide < 0`)
- Edge crossing: каждое из двух новых рёбер (W→v1, W→v2) проверяется только против рёбер,
  не делящих его собственный endpoint (не оба вместе, а по отдельности)

**Проблема 2:** Старый код пропускал рёбра, касающиеся v1 ИЛИ v2 — для обоих новых рёбер.
В однотреугольном меше с тремя рёбрами все три рёбра имеют хотя бы один конец в v1 или v2,
в результате ни одно ребро не проверялось.

---

### 4. Добавлено создание треугольника из трёх существующих вершин

**Поведение:** Если выделены 2 вершины и нажать LMB на третью уже существующую →
вместо выделения формируется треугольник из трёх существующих вершин.

**IsValidTriangleFromExisting** (отдельная функция):
- Не вызывает IsInsideMesh (существующая вершина по определению на меше → ложный reject)
- Проверяет: не дублирует треугольник, winding, edge crossings

```csharp
// В BlueprintVertexSelectSystem.Tick:
if (mesh.SelectedId1 != 0 && mesh.SelectedId2 != 0
  && hoveredId != mesh.SelectedId1 && hoveredId != mesh.SelectedId2) {
  TryCreateTriangleFromExisting(ref mesh, hoveredId);
}
```

---

### 5. Добавлен hover на рёбра + удаление ребра по RMB

**BlueprintCursorSystem** — edge hover (только когда HoveredVertexId == 0):
```csharp
foreach (var (a, b) in BlueprintGeometry.GetEdges(mesh.Triangles)) {
  var dist = BlueprintGeometry.DistanceToSegment(cx, cy, ax, ay, bx, by);
  if (dist < bestEdgeDist) { ... hoveredEdgeA = a; hoveredEdgeB = b; }
}
```
Порог EdgeHoverRadius = 0.2 ед. Приоритет: vertex hover (0.4 ед.) блокирует edge hover.

**BlueprintRenderSystem** — hover-цвет рёбра:
- ColorEdge = (0, 200, 255), толщина 0.08
- ColorEdgeHover = (255, 200, 60), толщина 0.18
- Проверка: `(a == hoveredEdgeA && b == hoveredEdgeB) || (a == hoveredEdgeB && b == hoveredEdgeA)`

**BlueprintEdgeDeleteSystem** (новый файл):
- Query: Blueprint + BlueprintMesh + ControlSubjectInput\<CursorRightClickAction\>
- Охранники: `!rclick.Active` → return; `HoveredVertexId != 0` → return; `HoveredEdgeA == 0` → return
- `TryDeleteEdge`: найти треугольники с обоими концами ребра → remaining set → reject если < 3 вершин →
  orphaned = в удалённых, но не в remaining → новый массив треугольников → PendingDestroy для orphaned

---

### 6. Исправлен RMB — события не пропадают при тикрейте < FPS

**Причина:** `IsMouseButtonReleased` — true ровно один Raylib-кадр. При 30 Гц тике и 120 FPS
вероятность попасть в нужный кадр ≈ 25%. Дополнительно: `_rmbMoved` не сбрасывался,
если тик пропустил кадр с press (оставался true от предыдущего drag'а).

**Решение в InputSystem:**
```csharp
public override void Tick(float dt) {
  // RMB tracking — каждый Raylib-кадр, ДО аккумулятора
  if (cameraSystem != null) {
    var rmbPressed = Raylib.IsMouseButtonPressed(MouseButton.Right);
    // ... rmbDown, rmbReleased ...
    if (rmbPressed) { _rmbLastScreenPos = mouseScreen; _rmbMoved = false; }
    if (rmbDown && !rmbPressed) { if (delta > RmbDragThresholdPx) _rmbMoved = true; }
    if (rmbReleased && !_rmbMoved) _rmbClickLatched = true;
  }

  _accumulator += dt;
  if (_accumulator < _tickInterval) return;
  _accumulator -= _tickInterval;

  // Потребляем залатченный клик
  var rmbClick = _rmbClickLatched;
  _rmbClickLatched = false;
  // ...
}
```

---

## Изменённые файлы

| Файл | Что изменилось |
|------|---------------|
| `Blueprint/BlueprintComponents.cs` | Добавлены `HoveredEdgeA`, `HoveredEdgeB` в `BlueprintMesh` |
| `Blueprint/BlueprintGeometry.cs` | Добавлены `DistanceToSegment`, `IsValidTriangleFromExisting`; переписаны `IsVertexMoveValid`, `IsValidNewVertex` |
| `Systems/BlueprintCursorSystem.cs` | Добавлен edge hover |
| `Systems/BlueprintVertexSelectSystem.cs` | TryCreateTriangleFromExisting, AppendTriangle |
| `Systems/BlueprintVertexMoveSystem.cs` | Полный переход на offset-based drag |
| `Systems/BlueprintEdgeDeleteSystem.cs` | **Новый файл** |
| `Systems/Client/BlueprintRenderSystem.cs` | ColorEdgeHover, LineThicknessHover, hover-рендер рёбер |
| `Systems/Client/InputSystem.cs` | RMB latching (`_rmbClickLatched`), вынос RMB tracking до аккумулятора |
| `Program.cs` | Добавлен `BlueprintEdgeDeleteSystem` перед `BlueprintVertexDeleteSystem` |

---

## Текущее состояние редактора

Все основные UX-функции реализованы и работают:
- [x] Hover на вершинах и рёбрах
- [x] Выделение 1 или 2 смежных вершин
- [x] Drag вершины с корректным offset (нет snap, нет накопления)
- [x] Создание нового треугольника (новая вершина в пустой области)
- [x] Создание треугольника из трёх существующих вершин
- [x] Удаление вершины по RMB
- [x] Удаление ребра по RMB
- [x] Полная валидация: edge crossing, winding, not-inside-foreign
- [x] RMB click vs drag корректно различается при любом FPS/тикрейте
- [x] Камера: панорамирование RMB drag

## Что ещё не реализовано (из оригинальной спецификации)

- [ ] LMB drag внутри меша → перемещение всего меша
- [ ] Ear-clipping ретриангуляция при удалении вершины (сейчас удаляются осиротевшие вершины)
- [ ] Scroll wheel зум
- [ ] Сохранение/загрузка префабов (JSON в `Prefabs/`)
- [ ] Сайдбар со списком префабов
