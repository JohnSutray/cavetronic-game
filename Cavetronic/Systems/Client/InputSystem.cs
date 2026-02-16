using Arch.Core;
using Raylib_cs;

namespace Cavetronic.Systems.Client;

public class InputSystem(GameWorld gameWorld, float tickRate = 60f) : EcsSystem(gameWorld) {
  private readonly QueryDescription _playersQuery = new QueryDescription().WithAll<ControlOwner>();
  private readonly List<Entity> _players = new();
  private readonly float _tickInterval = 1f / tickRate;
  private float _accumulator;

  public override void Tick(float dt) {
    _accumulator += dt;

    if (_accumulator < _tickInterval) {
      return;
    }

    _accumulator -= _tickInterval;

    _players.Clear();

    GameWorld.Ecs.Query(in _playersQuery, (Entity entity) => {
      _players.Add(entity);
    });

    foreach (var entity in _players) {
      SyncKeyToMarker<Action1>(entity, Raylib.IsKeyDown(KeyboardKey.Space));
      SyncKeyToMarker<Action2>(entity, Raylib.IsKeyDown(KeyboardKey.E));
      SyncKeyToMarker<MoveLeft>(entity, Raylib.IsKeyDown(KeyboardKey.A));
      SyncKeyToMarker<MoveRight>(entity, Raylib.IsKeyDown(KeyboardKey.D));
    }
  }

  private void SyncKeyToMarker<T>(Entity entity, bool active) where T : struct {
    if (active && !GameWorld.Ecs.Has<ControlSubjectInput<T>>(entity)) {
      GameWorld.Ecs.Add(entity, new ControlSubjectInput<T> { Active = true });
    } else if (!active && GameWorld.Ecs.Has<ControlSubjectInput<T>>(entity)) {
      GameWorld.Ecs.Remove<ControlSubjectInput<T>>(entity);
    }
  }
}
