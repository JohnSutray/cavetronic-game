namespace Cavetronic.Systems;

public abstract class EcsSystem {
  protected GameWorld GameWorld { get; }

  protected EcsSystem(GameWorld gameWorld) {
    GameWorld = gameWorld;
  }

  public virtual void Init() {
  }

  public virtual void Tick(float dt) {
  }
}