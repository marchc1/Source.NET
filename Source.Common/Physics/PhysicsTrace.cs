
namespace Source.Common.Physics;

public interface IPhysCollide {

}

public struct LeafMap {
	public object? Leaf;
	public ushort VertCount;

}

public struct CollideMap {
	public int LeafCount;
	public InlineArray1<LeafMap> LeafMap;
}

public class PhysCollide : IPhysCollide
{

}
