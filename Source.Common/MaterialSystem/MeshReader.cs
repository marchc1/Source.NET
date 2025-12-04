using System.Numerics;

namespace Source.Common.MaterialSystem;

public class BaseMeshReader
{
	protected MeshDesc Desc;
	protected IMesh? Mesh;
	protected int MaxVertices = -1;
	protected int MaxIndices = -1;

	public void BeginRead(IMesh mesh, int firstVertex = 0, int numVertices = 0, int firstIndex = 0, int numIndices = 0) {
		if (numVertices < 0)
			numVertices = mesh.VertexCount();
		if (numIndices < 0)
			numIndices = mesh.IndexCount();

		Mesh = mesh;
		MaxVertices = numVertices;
		MaxIndices = numIndices;
		mesh.ModifyBegin(firstVertex, numVertices, firstIndex, numIndices, ref Desc);
	}

	public void EndRead() {
		Assert(Mesh != null);
		Mesh.ModifyEnd(ref Desc);
		Mesh = null;
	}

	public void BeginRead_Direct(in MeshDesc desc, int numVertices, int numIndices) {
		Desc = desc;

		MaxVertices = numVertices;
		MaxIndices = numIndices;
	}

	public void Reset() {

	}
}

public unsafe class MeshReader : BaseMeshReader
{
	public int NumIndices() => MaxIndices;
	public ushort Index(int index) {
		Assert((index >= 0) && (index < MaxIndices));
		return Desc.Index.Indices[index * Desc.Index.IndexSize];
	}
	public ref readonly Vector3 Position(int vertex) {
		throw new NotImplementedException();
	}
	public uint Color(int vertex) {
		throw new NotImplementedException();
	}
	public ReadOnlySpan<float> TexCoord(int vertex, int stage) {
		throw new NotImplementedException();
	}
	public void TexCoord2F(int vertex, int stage, out float s, out float t) {
		throw new NotImplementedException();
	}
	public ref readonly Vector2 TexCoord2F(int vertex, int stage) {
		throw new NotImplementedException();
	}
	public int NumBoneWeights(){
		throw new NotImplementedException();
	}
	public float Wrinkle(int vertex){
		throw new NotImplementedException();
	}
	public ref readonly Vector3 Normal(int vertex){
		throw new NotImplementedException();
	}
	public float BoneWeight(int vertex){
		throw new NotImplementedException();
	}
}
