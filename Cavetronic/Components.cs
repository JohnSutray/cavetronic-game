using nkast.Aether.Physics2D.Dynamics;

namespace Cavetronic;

public partial struct StableId {
  public const int DefaultSpawnerId = 1;
  public const int LocalTestUser = 1000;

  public int Id;
}

public partial struct Player {
}

public struct ControlOwner {
  public int SubjectId;
  public int ReassignedAtTick;
}

public struct ControlSubject {
  public int TransferTargetId;
}

public struct Action1;
public struct Action2;
public struct MoveLeft;
public struct MoveRight;

public struct ControlSubjectInputDescriptor<T> where T : struct {
  public int DescriptionKeyCode;
}

public struct ControlSubjectInput<T> where T : struct {
  public bool Active;
  public bool PreviouslyActive;
  public int OwnerId;
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
