using Arch.Core;
using MemoryPack;

namespace Cavetronic.Serialization;

public class ComponentPacker<T> where T : struct {
  public void Pack(World world, MemoryStream stream) {
    var list = new List<(int NetId, T Component)>();
    var query = new QueryDescription().WithAll<StableId, T>();

    world.Query(in query, (ref StableId netId, ref T component) => { list.Add((netId.Id, component)); });

    var data = MemoryPackSerializer.Serialize(list);
    stream.Write(BitConverter.GetBytes(data.Length));
    stream.Write(data);
    Console.WriteLine($"  Packed {list.Count} entities with {typeof(T).Name}");
  }

  public void Unpack(World world, Dictionary<int, Entity> registry, byte[] buffer, ref int offset) {
    var length = BitConverter.ToInt32(buffer, offset);
    offset += 4;

    var list = MemoryPackSerializer.Deserialize<List<(int NetId, T Component)>>(
      buffer.AsSpan(offset, length));
    offset += length;

    if (list != null) {
      foreach (var (netId, component) in list) {
        if (!registry.TryGetValue(netId, out var entity)) {
          entity = world.Create(new StableId { Id = netId }, component);
          registry[netId] = entity;
        }
        else if (world.Has<T>(entity)) {
          world.Set(entity, component);
        }
        else {
          world.Add(entity, component);
        }
      }
    }

    Console.WriteLine($"  Unpacked {list?.Count ?? 0} entities with {typeof(T).Name}");
  }
}