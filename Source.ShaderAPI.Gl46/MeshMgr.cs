using Source.Common.MaterialSystem;
using Source.Common.ShaderAPI;

namespace Source.ShaderAPI.Gl46;

public enum DeviceState {
	OK,
	NeedsReset
}

public class MeshMgr : IMeshMgr
{
	internal IMaterialSystem MaterialSystem;

	internal void Flush() {
		if (IsPC()) {
			BufferedMesh.Flush();
		}
	}

	public IMesh GetDynamicMesh(IMaterial? material, VertexFormat vertexFormat, int hwSkinBoneCount, bool buffered, IMesh? vertexOverride, IMesh? indexOverride) {
		Assert(material == null || ((IMaterialInternal)material).IsRealTimeVersion());

		if (BufferedMode != buffered && BufferedMode) {
			BufferedMesh.SetMesh(null);
		}

		BufferedMode = buffered;

		IMaterialInternal matInternal = (IMaterialInternal)material!;
		MeshGl46 mesh = DynamicMesh;

		if (BufferedMode) {
			Assert(!BufferedMesh.WasNotRendered());
			BufferedMesh.SetMesh(mesh);
			mesh = BufferedMesh;
		}

		if (vertexOverride == null) {
			VertexFormat fmt = matInternal.GetVertexFormat();
			mesh.SetVertexFormat(fmt);
		}
		else {
			MeshGl46 vertexMesh = (MeshGl46)vertexOverride;
			mesh.SetVertexFormat(vertexMesh.GetVertexFormat());
		}

		mesh.SetMaterial(matInternal);
		if (mesh == DynamicMesh) {
			MeshGl46? baseVertex = (MeshGl46?)vertexOverride;
			if (baseVertex != null)
				DynamicMesh.OverrideVertexBuffer(baseVertex.GetVertexBuffer());
			MeshGl46? baseIndex = (MeshGl46?)vertexOverride;
			if (baseIndex != null)
				DynamicMesh.OverrideIndexBuffer(baseIndex.GetIndexBuffer());
		}

		return mesh;
	}

	internal ShaderAPIGl46 ShaderAPI;

	bool BufferedMode;
	bool UsingFatVertices;

	VertexFormat CurrentVertexFormat;
	int IndexBufferOffset;
	MaterialPrimitiveType PrimitiveTYpe;
	int FirstIndex;
	int NumIndices;

	internal void RenderPassWithVertexAndIndexBuffers() {
		throw new NotImplementedException();
	}

	// We can't rely on imported new () calls here because it makes the dependency injection
	// system crash and burn
	private TMesh InitMesh<TMesh>() where TMesh : MeshGl46, new() {
		TMesh ret = new TMesh();
		ret.ShaderAPI = MaterialSystem.GetRenderContext().GetShaderAPI();
		ret.ShaderUtil = MaterialSystem.GetShaderUtil();
		ret.MeshMgr = ShaderAPI.MeshMgr;
		ret.ShaderDevice = ret.ShaderAPI.GetShaderDevice();
		return ret;
	}


	List<VertexBufferGl46> DynamicVertexBuffers = [];
	IndexBufferGl46? DynamicIndexBuffer;

	BufferedMeshGl46 BufferedMesh;
	DynamicMeshGl46 DynamicMesh;

	internal void Init() {
		BufferedMesh = InitMesh<BufferedMeshGl46>();
		DynamicMesh = InitMesh<DynamicMeshGl46>();
		DynamicMesh.Init(0);
		CreateDynamicIndexBuffer();
		CreateZeroVertexBuffer();
		BufferedMode = true;
	}

	private void CreateDynamicIndexBuffer() {
		DestroyDynamicIndexBuffer();
		DynamicIndexBuffer = new IndexBufferGl46(IMesh.INDEX_BUFFER_SIZE, true);
	}
	private void DestroyDynamicIndexBuffer() {
		DynamicIndexBuffer?.Dispose();
		DynamicIndexBuffer = null;
	}

	private void CreateZeroVertexBuffer() {
		// Todo...
	}


	public const int VERTEX_BUFFER_SIZE = 32768;
	public const int MAX_QUAD_INDICES = 16384;

	internal VertexBufferGl46 FindOrCreateVertexBuffer(int dynamicBufferID, VertexFormat vertexFormat) {
		int vertexSize = VertexFormatSize(vertexFormat);

		while (DynamicVertexBuffers.Count <= dynamicBufferID) {
			int bufferMemory = ShaderAPI.GetCurrentDynamicVBSize();
			VertexBufferGl46 vertexBuffer = new VertexBufferGl46(true);
			vertexBuffer.VertexSize = 0;
			int initVertexSize = bufferMemory / VERTEX_BUFFER_SIZE, initVertexCount = VERTEX_BUFFER_SIZE;
			vertexBuffer.BufferSize = initVertexSize * initVertexCount;
			DynamicVertexBuffers.Add(vertexBuffer);
		}

		VertexBufferGl46 buffer = DynamicVertexBuffers[dynamicBufferID];

		if (buffer.VertexSize != vertexSize) {
			int bufferMemory = ShaderAPI.GetCurrentDynamicVBSize();
			buffer.VertexSize = vertexSize;
			buffer.ChangeConfiguration(vertexFormat, vertexSize, bufferMemory);
		}

		return DynamicVertexBuffers[dynamicBufferID];
	}

	internal unsafe int VertexFormatSize(VertexFormat vertexFormat) {
		MeshDesc desc = new();
		VertexBufferGl46.ComputeVertexDescription(null, vertexFormat, ref desc.Vertex);
		return desc.Vertex.ActualVertexSize;
	}

	internal IndexBufferGl46 GetDynamicIndexBuffer() {
		return DynamicIndexBuffer!;
	}

	internal void RestoreBuffers() {
		Init();
	}

	internal IMesh CreateStaticMesh(VertexFormat format, ReadOnlySpan<char> textureGroup, IMaterial? material) {
		MeshGl46 mesh = InitMesh<MeshGl46>();
		mesh.SetVertexFormat(format);
		if (material != null)
			mesh.SetMaterial((IMaterialInternal)material);
		return mesh;
	}

	internal int GetMaxIndicesToRender(IMaterial material) {
		return IMesh.INDEX_BUFFER_SIZE;
	}

	internal int GetMaxVerticesToRender(IMaterial material) {
		VertexFormat fmt = material.GetVertexFormat();
		int vertexSize = VertexFormatSize(fmt);
		if (vertexSize == 0) {
			Warning($"bad vertex size for material {material.GetName()}\n");
			return 0;
		}

		int nMaxVerts = ShaderAPI.GetCurrentDynamicVBSize() / vertexSize;
		return Math.Min(nMaxVerts, 32767); // TODO: We should look into increasing the vertex/index limits in-engine
	}
}
