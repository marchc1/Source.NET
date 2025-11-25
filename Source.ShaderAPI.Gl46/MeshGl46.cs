using Source.Common.MaterialSystem;
using Source.Common.ShaderAPI;

using System.Runtime.InteropServices;

namespace Source.ShaderAPI.Gl46;

public struct PrimList
{
	public int FirstIndex;
	public int NumIndices;
}

public unsafe class MeshGl46 : IMesh
{
	public IShaderAPI ShaderAPI;
	public IShaderUtil ShaderUtil;
	public MeshMgr MeshMgr;
	public IShaderDevice ShaderDevice;

	protected VertexBufferGl46 VertexBuffer;
	protected IndexBufferGl46 IndexBuffer;

	protected VertexFormat LastVertexFormat;
	protected VertexFormat VertexFormat;
	protected IMaterialInternal Material;
	protected MaterialPrimitiveType Type = MaterialPrimitiveType.Triangles;
	protected bool IsDrawing;

	protected static PrimList* s_Prims;
	protected static int s_PrimsCount;
	protected static uint s_FirstVertex;
	protected static uint s_NumVertices;


	protected int Mode = ComputeMode(MaterialPrimitiveType.Triangles);
	public bool Locked;

	public VertexBufferGl46 GetVertexBuffer() => throw new Exception();
	public IndexBufferGl46 GetIndexBuffer() => throw new Exception();

	public virtual void BeginCastBuffer(VertexFormat format) {
		throw new NotImplementedException();
	}
	public void DrawMesh() {
		Assert(!IsDrawing);
		IsDrawing = true;

		ShaderAPI.DrawMesh(this);

		IsDrawing = false;
	}

	protected bool SetRenderState(int vertexOffsetInBytes, int firstIndex) => SetRenderState(vertexOffsetInBytes, firstIndex, VertexFormat.Invalid);
	protected virtual bool SetRenderState(int vertexOffsetInBytes, int firstIndex, VertexFormat vertexFormat) {
		if (ShaderDevice.IsDeactivated()) {
			ResetMeshRenderState();
			return false;
		}

		LastVertexFormat = vertexFormat;

		SetVertexStreamState(vertexOffsetInBytes);
		SetIndexStreamState(firstIndex);

		return true;
	}

	// What do these do...
	private void SetVertexStreamState(int vertexOffsetInBytes) {

	}

	private void SetIndexStreamState(int firstIndex) {

	}

	private void ResetMeshRenderState() {
		throw new NotImplementedException();
	}

	public virtual void BeginCastBuffer(MaterialIndexFormat format) {
		throw new NotImplementedException();
	}

	public virtual unsafe void Draw(int firstIndex = -1, int indexCount = 0) {
		Assert(VertexBuffer != null);
		if (VertexBuffer == null)
			return;

		if(!ShaderUtil.OnDrawMesh(this, firstIndex, indexCount)) {
			MarkAsDrawn();
			return;
		}

		PrimList* primList = stackalloc PrimList[1];
		if (firstIndex == -1 || indexCount == 0) {
			primList->FirstIndex = 0;
			primList->NumIndices = NumIndices;
		}
		else {
			primList->FirstIndex = firstIndex;
			primList->NumIndices = indexCount;
		}
		DrawInternal(primList, 1);
	}

	private unsafe void DrawInternal(PrimList* primList, int lists) {
		HandleLateCreation();

		int i;
		for (i = 0; i < lists; i++) {
			if (primList[i].NumIndices > 0)
				break;
		}

		if (i == lists)
			return;

		// Assert(ShaderAPI.IsInSelectionMode());

		if (!SetRenderState(0, 0))
			return;

		s_Prims = primList;
		s_PrimsCount = lists;

#if DEBUG
		for (i = 0; i < lists; ++i) 
			Assert(primList[i].NumIndices > 0);
#endif

		s_FirstVertex = 0;
		s_NumVertices = (uint)VertexBuffer.VertexCount;

		DrawMesh();

		// Source doesn't seem to reset these. I don't know why it doesn't, but I can't see a good reason not to,
		// and considering this is almost always going to be a stack-backed pointer, I see a very good reason to do so
		s_Prims = null;
		s_PrimsCount = 0;
	}

	public virtual void EndCastBuffer() {
		throw new NotImplementedException();
	}

	public virtual int GetRoomRemaining() {
		throw new NotImplementedException();
	}

	public virtual VertexFormat GetVertexFormat() {
		return VertexFormat;
	}

	public virtual int IndexCount() {
		throw new NotImplementedException();
	}

	public virtual MaterialIndexFormat IndexFormat() {
		throw new NotImplementedException();
	}

	public virtual bool IsDynamic() {
		throw new NotImplementedException();
	}

	public void HandleLateCreation() {
		VertexBuffer?.HandleLateCreation();
		IndexBuffer?.HandleLateCreation();
	}

	public virtual bool Lock(int vertexCount, bool append, ref VertexDesc desc) {
		if (VertexBuffer == null) {
			int size = MeshMgr.VertexFormatSize(VertexFormat);
			VertexBuffer = new VertexBufferGl46(VertexFormat, size, vertexCount, false);
		}

		byte* vertexMemory = VertexBuffer.Lock(vertexCount, out desc.FirstVertex);
		VertexBufferGl46.ComputeVertexDescription(vertexMemory, VertexFormat, ref desc);

		return true;
	}

	public virtual int Lock(bool readOnly, int firstIndex, int indexCount, ref IndexDesc desc) {
		if (ShaderDevice.IsDeactivated() || indexCount == 0) {
			desc.Indices = ScratchIndexBuffer;
			desc.IndexSize = 0;
			return 0;
		}

		IndexBuffer ??= new IndexBufferGl46(indexCount, false);

		desc.Indices = (ushort*)IndexBuffer.Lock(readOnly, indexCount, out int startIndex, firstIndex);
		if (desc.Indices == null) {
			desc.IndexSize = 0;
			Assert(false);
			Warning("Failed to lock index buffer...\n");
			return 0;
		}

		desc.IndexSize = 1;
		IsIBLocked = true;
		return startIndex;
	}
	bool IsIBLocked;
	static readonly ushort* ScratchIndexBuffer = (ushort*)NativeMemory.Alloc(6 * sizeof(ushort));
	public virtual void LockMesh(int vertexCount, int indexCount, ref MeshDesc desc) {
		ShaderUtil.SyncMatrices();

		Lock(vertexCount, false, ref desc.Vertex);
		if (Type != MaterialPrimitiveType.Points) 
			Lock(false, -1, indexCount, ref desc.Index);
		else {
			desc.Index.Indices = ScratchIndexBuffer;
			desc.Index.IndexSize = 0;
		}

		Locked = true;
	}

	public virtual void MarkAsDrawn() {}

	public virtual void ModifyBegin(int firstVertex, int vertexCount, int firstIndex, int indexCount, ref MeshDesc desc) {
		throw new NotImplementedException();
	}

	public virtual void ModifyEnd(ref MeshDesc desc) {
		throw new NotImplementedException();
	}

	IMesh? ColorMesh;
	int ColorMeshVertOffsetInBytes = 0;

	public virtual void SetColorMesh(IMesh colorMesh, int vertexOffset) {
		ColorMesh = colorMesh;
		ColorMeshVertOffsetInBytes = vertexOffset;
	}

	public virtual MaterialPrimitiveType GetPrimitiveType() {
		return Type;
	}

	public virtual void SetPrimitiveType(MaterialPrimitiveType type) {
		if (!ShaderUtil.OnSetPrimitiveType(this, type))
			return;

		Type = type;
		Mode = ComputeMode(type);
	}

	public virtual bool Unlock(int vertexCount, ref VertexDesc desc) {
		VertexBuffer.Unlock(vertexCount);
		return true;
	}

	public virtual bool Unlock(int indexCount, ref IndexDesc desc) {
		if (!IsIBLocked)
			return true;
		IndexBuffer.Unlock(indexCount);
		IsIBLocked = false;
		return true;
	}

	int NumVertices;
	int NumIndices;

	public virtual void UnlockMesh(int vertexCount, int indexCount, ref MeshDesc desc) {
		Assert(Locked);

		Unlock(vertexCount, ref desc.Vertex);
		if (Type != MaterialPrimitiveType.Points)
			Unlock(indexCount, ref desc.Index);

		NumVertices = vertexCount;
		NumIndices = indexCount;
		Locked = false;
	}

	public virtual int VertexCount() {
		return NumVertices;
	}

	public virtual void SetMaterial(IMaterialInternal matInternal) {
		Material = matInternal;
	}

	public virtual void SetVertexFormat(VertexFormat fmt) {
		VertexFormat = fmt;
	}

	public virtual unsafe void RenderPass() {
		HandleLateCreation();
		Assert(Type != MaterialPrimitiveType.Heterogenous);

		for (int iPrim = 0; iPrim < s_PrimsCount; iPrim++) {
			PrimList* pPrim = &s_Prims[iPrim];

			if (pPrim->NumIndices == 0)
				continue;

			if (Type == MaterialPrimitiveType.Points || Type == MaterialPrimitiveType.InstancedQuads) {
				throw new NotImplementedException();
			}
			else {
				int numPrimitives = NumPrimitives(s_NumVertices, pPrim->NumIndices);

				CheckIndices(pPrim, numPrimitives);
				uint vao = VertexBuffer!.VAO();
				uint ibo = IndexBuffer!.IBO();
				glVertexArrayElementBuffer(vao, ibo);
				glBindVertexArray(vao);
				glDrawElements(Mode, pPrim->NumIndices, GL_UNSIGNED_SHORT, (void*)(pPrim->FirstIndex * 2));
			}
		}
	}

	public static int ComputeMode(MaterialPrimitiveType type) {
		switch (type) {
			case MaterialPrimitiveType.Points: return GL_POINTS;
			case MaterialPrimitiveType.Lines: return GL_LINES;
			case MaterialPrimitiveType.Triangles: return GL_TRIANGLES;
			case MaterialPrimitiveType.TriangleStrip: return GL_TRIANGLE_STRIP;
			case MaterialPrimitiveType.Heterogenous: return -1;
			default: throw new Exception();
		}
	}

	private int NumPrimitives(uint vertexCount, int indexCount) {
		switch (Mode) {
			case GL_POINTS: return (int)vertexCount;
			case GL_LINES: return indexCount / 2;
			case GL_TRIANGLES: return indexCount / 3;
			case GL_TRIANGLE_STRIP: return indexCount - 2;
			default: Assert(0); return 0;
		}
	}

	private void CheckIndices(PrimList* pPrim, int numPrimitives) {
		int indexCount = 0;
		if (Mode == GL_TRIANGLES) {
			indexCount = numPrimitives * 3;
		}
		else if (Mode == GL_TRIANGLE_STRIP) {
			indexCount = numPrimitives + 2;
		}

		if (indexCount != 0) {
			// TODO: Should index buffer be global? Why the hell was it global before??
			Assert(pPrim->FirstIndex >= 0 && pPrim->FirstIndex < IndexBuffer.IndexCount);

			for (int j = 0; j < indexCount; j++) { // TODO
												   //uint index = IndexBuffer.GetShadowIndex(j + pPrim->FirstIndex);

				//if (index >= s_FirstVertex && index < s_FirstVertex + s_NumVertices) {
				//	continue;
				//}

				//Assert(false);
			}
		}
	}

	internal bool HasColorMesh() {
		return true;
	}

	internal bool HasFlexMesh() {
		return false;
	}

	public virtual bool NeedsVertexFormatReset(VertexFormat fmt) {
		return VertexFormat != fmt;
	}

	public virtual bool HasEnoughRoom(int vertexCount, int indexCount) => true;

	public virtual void PreLock() {
		throw new NotImplementedException();
	}

	public virtual void UseVertexBuffer(VertexBufferGl46 vertexBuffer) {
		VertexBuffer = vertexBuffer;
	}

	public virtual void UseIndexBuffer(IndexBufferGl46 indexBuffer) {
		IndexBuffer = indexBuffer;
	}
}
