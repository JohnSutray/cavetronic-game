using nkast.Aether.Physics2D.Dynamics;

namespace Cavetronic;

public partial struct StableId {
  public const int DefaultSpawnerId = 1;

  public int Id;
}

public partial struct Player {
}

[Flags]
public enum InputSignal : ulong {
  None    = 0,
  Up      = 1UL << 0,
  Down    = 1UL << 1,
  Left    = 1UL << 2,
  Right   = 1UL << 3,
  Action1 = 1UL << 4,    // Space
  Action2 = 1UL << 5,    // E
  Action3 = 1UL << 6,    // F
}

public struct ControlOwner {
  public int SubjectId;
  public ulong Input;
}

public struct ControlSubject {
  public ulong Input;
  public int TransferTargetId;
}

public partial struct DroneHead {
}

public partial struct DroneHeadSpawner {
  public float ProductionTimer;
  public bool ProductionReady;
  public int StageDroneId;
}

public partial struct Ghost {
}

public struct PhysicsBodyRef {
  public Body Body;
}

public partial struct Position {
  public float X;
  public float Y;
}

public partial struct Rotation {
  public float Angle;
}

public partial struct Collider {
  public float[] Vertices;
}

public partial struct CameraTarget {
  // Маркер для entity, за которым следит камера
}
