# Сессия: Оптимизация памяти и ImGui-рефакторинг

---

## Что было сделано

### 1. Диагностика роста памяти
- Пользователь сообщил о непрерывном росте памяти в процессе.
- Добавлен ImGui-оверлей (временно инлайном в `Program.cs`) с `Heap`, `Alloc`, `GC g0/g1/g2`, `ECS e:/arch:`.
- Выяснилось: `g0/g1/g2 = 0` при росте → GC не триггерился, просто накапливалось в ген0.
- Добавлен `GC.Collect(2, Forced, blocking:true)` каждые 0.5с → память перестала расти.
- **Вывод: утечек нет.** Была pre-GC аккумуляция коротких объектов. Gen2 коллект раз в секунду держит heap стабильным.

### 2. Устранение per-tick аллокаций (горячие пути)

**BlueprintGeometry.cs:**
- Добавлены 4 `private static readonly` буфера (`_cachedEdges`, `_cachedEdgeSeen`, `_movingEdgesBuf`, `_staticEdgesBuf`)
- Добавлен `PopulateEdges(triangles, list, seen)` — заполняет переданные коллекции без аллокации
- Добавлен `ClassifyEdge` — single-pass разделение рёбер на движущиеся/статичные без LINQ
- Убраны `GetEdges()` (возвращал `new List<>`) из `IsValidNewVertex`, `IsValidTriangleFromExisting`, `IsVertexMoveValid`

**Системы — устранение DisplayClass:**
- `BlueprintCursorSystem`: вынесены `_cx`, `_cy`, `_bestVertexId`, `_bestDist2`, `_hoveredEdgeA/B`, `_bestEdgeDist` в поля
- `BlueprintRenderSystem`: вынесены `_triangles`, `_selectedId1/2`, `_hoveredVertexId`, `_hoveredEdgeA/B`, `_cursorX/Y`, `_hasCursor` в поля; `DrawTrianglePreview(...)` стала безпараметровой
- `BlueprintVertexMoveSystem`: `var dragActiveThisTick` → `_dragActiveThisTick` поле
- `BlueprintVertexSelectSystem`: `var hasShift` → `_hasShift` поле
- `GhostControlSystem`: `var toDestroy = new List<>()` → `readonly List<> _toDestroy` поле с `.Clear()`
- `BlueprintEdgeDeleteSystem`, `BlueprintVertexDeleteSystem`: все временные коллекции вынесены в поля

### 3. ImGui-рефакторинг: инлайн → системы

**Новые файлы:**
- `Systems/Client/ImGuiBeginSystem.cs` — вызывает `rlImGui.Begin()`
- `Systems/Client/ImGuiEndSystem.cs` — вызывает `rlImGui.End()`
- `Systems/Client/MemoryStatsOverlaySystem.cs` — оверлей памяти/ECS с кнопкой "GC Gen2"

**Program.cs:**
- Убран весь инлайн ImGui-код из игрового цикла
- Три новые системы добавлены в конец массива после `CameraEndSystem`
- Цикл теперь: `BeginDrawing → systems.Tick → DrawFPS → EndDrawing`

**MemoryStatsOverlaySystem:**
- Обновляет строки раз в 1с
- `RefreshStats()` читает `GC.GetTotalMemory`, `GetTotalAllocatedBytes`, `CollectionCount(0/1/2)`, `Ecs.Size`, `Ecs.Archetypes.Count`
- Кнопка "GC Gen2" вызывает `GC.Collect(2, Forced, blocking:true)` немедленно по клику

---

## Паттерн ImGui в системах

Чтобы нарисовать UI в системе — встать между `ImGuiBeginSystem` и `ImGuiEndSystem`:

```csharp
// Program.cs — порядок систем:
new CameraEndSystem(gameWorld),
new ImGuiBeginSystem(gameWorld),
new MyUiSystem(gameWorld),   // ← сюда любая система, которая рисует ImGui
new MemoryStatsOverlaySystem(gameWorld),
new ImGuiEndSystem(gameWorld),
```

---

## Открытые задачи

- **InlineQuery / IForEach** — delegate при каждом `Ecs.Query(lambda)` всё ещё аллоцируется. Не критично при текущей нагрузке (gen2 раз в секунду держит heap стабильным), но является следующим шагом оптимизации при необходимости.
- **ControlInputSyncSystem** — вызывает `AdvanceInput×8 + CleanupInput×8` каждый Raylib-кадр (120fps). При наличии проблем с производительностью — ревизия этой системы.
