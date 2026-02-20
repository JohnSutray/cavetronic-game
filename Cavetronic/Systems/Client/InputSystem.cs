using Arch.Buffer;
using Arch.Core;
using Raylib_cs;

namespace Cavetronic.Systems.Client;

public class InputSystem(GameWorld gameWorld, float tickRate = 60f) : EcsSystem(gameWorld) {
  private readonly QueryDescription _playersQuery = new QueryDescription().WithAll<ControlOwner>();
  private readonly float _tickInterval = 1f / tickRate;
  private readonly CommandBuffer _buffer = new();
  private float _accumulator;

  public override void Tick(float dt) {
    _accumulator += dt;
    
    if (_accumulator < _tickInterval) {
      return;
    }
    
    _accumulator -= _tickInterval;

    GameWorld.Ecs.Query(in _playersQuery, (Entity entity) => {
      SyncKeyToMarker<Action1>(_buffer, entity, Raylib.IsKeyDown(KeyboardKey.Space));
      SyncKeyToMarker<Action2>(_buffer, entity, Raylib.IsKeyDown(KeyboardKey.E));
      SyncKeyToMarker<MoveLeft>(_buffer, entity, Raylib.IsKeyDown(KeyboardKey.A));
      SyncKeyToMarker<MoveRight>(_buffer, entity, Raylib.IsKeyDown(KeyboardKey.D));
    });

    _buffer.Playback(GameWorld.Ecs);
  }

  private void SyncKeyToMarker<T>(CommandBuffer buffer, Entity entity, bool active) where T : struct {
    switch (active) {
      case true when !GameWorld.Ecs.Has<ControlSubjectInput<T>>(entity):
        buffer.Add(in entity, new ControlSubjectInput<T> { Active = true });
        break;
      case false when GameWorld.Ecs.Has<ControlSubjectInput<T>>(entity):
        buffer.Remove<ControlSubjectInput<T>>(in entity);
        break;
    }
  }
}
