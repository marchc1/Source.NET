using Source.Common.MaterialSystem;

namespace Source.Common.Engine;

public interface IPooledVBAllocator
{
	bool Init(VertexFormat format, int numVerts);
	void Clear();
	IMesh GetSharedMesh();
	nint GetVertexBufferBase();
	int GetNumVertsAllocated();
	int Allocate(int numVerts);
	void Deallocate(int offset, int numVerts);
}
