using Arch.Core;
using MemoryPack;

namespace Cavetronic.Serialization;

public class DeltaSerializer {
  private delegate void UnpackAction(World world, Dictionary<int, Entity> registry, byte[] buffer, ref int offset);

  private readonly List<Action<World, MemoryStream>> _packActions = new();
  private readonly List<UnpackAction> _unpackActions = new();

  public DeltaSerializer Add<T>() where T : struct {
    var packer = new ComponentPacker<T>();
    _packActions.Add(packer.Pack);
    _unpackActions.Add(packer.Unpack);
    return this;
  }

  public byte[] Pack(World world, uint frame) {
    using var stream = new MemoryStream();

    var frameData = MemoryPackSerializer.Serialize(frame);
    stream.Write(BitConverter.GetBytes(frameData.Length));
    stream.Write(frameData);

    foreach (var action in _packActions) {
      action(world, stream);
    }

    return stream.ToArray();
  }

  public uint Unpack(World world, Dictionary<int, Entity> registry, byte[] buffer) {
    int offset = 0;

    int frameLength = BitConverter.ToInt32(buffer, offset);
    offset += 4;
    uint frame = MemoryPackSerializer.Deserialize<uint>(buffer.AsSpan(offset, frameLength));
    offset += frameLength;

    foreach (var action in _unpackActions) {
      action(world, registry, buffer, ref offset);
    }

    return frame;
  }
}