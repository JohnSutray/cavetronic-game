using Arch.Core;
using nkast.Aether.Physics2D.Common;
using EcsWorld = Arch.Core.World;
using PhysicsWorld = nkast.Aether.Physics2D.Dynamics.World;

namespace Cavetronic;

public class GameWorld {
  public EcsWorld Ecs { get; } = EcsWorld.Create();
  public PhysicsWorld Physics { get; } = new(new Vector2(0, 9.8f));

  public Dictionary<int, Entity> EntityIndex { get; } = new();
  public List<(Entity Entity, int StableId)> PendingDestroy { get; } = new();

  private int _nextStableId = 2;

  public int NextStableId() => _nextStableId++;

  public Entity RegisterEntity(int stableId, Entity entity) {
    EntityIndex[stableId] = entity;
    return entity;
  }

  public void UnregisterEntity(int stableId) {
    EntityIndex.Remove(stableId);
  }

  public bool TryGetEntity(int stableId, out Entity entity) {
    return EntityIndex.TryGetValue(stableId, out entity);
  }
}
