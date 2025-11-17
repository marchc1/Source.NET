using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.Common.MaterialSystem;

public unsafe struct VertexDesc
{
	public int PositionSize;
	public int BoneWeightSize;
	public int BoneMatrixIndexSize;
	public int NormalSize;
	public int ColorSize;
	public int SpecularSize;
	public int TangentSSize;
	public int TangentTSize;
	public int WrinkleSize;
	public int UserDataSize;
	public fixed int TexCoordSize[8];
	public int ActualVertexSize;

	public VertexCompressionType CompressionType;

	public int NumBoneWeights;

	public float* Position;
	public float* BoneWeight;
	public byte* BoneMatrixIndex;
	public float* Normal;
	public byte* Color;
	public byte* Specular;
	public float* TangentS;
	public float* TangentT;
	public float* Wrinkle;
	public float* UserData;
	public float* TexCoord0;
	public float* TexCoord1;
	public float* TexCoord2;
	public float* TexCoord3;
	public float* TexCoord4;
	public float* TexCoord5;
	public float* TexCoord6;
	public float* TexCoord7;
	public int FirstVertex;
	public uint OffsetVertex;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetTexCoord(int stage, float* val) {
		switch (stage) {
			case 0: TexCoord0 = val; return;
			case 1: TexCoord1 = val; return;
			case 2: TexCoord2 = val; return;
			case 3: TexCoord3 = val; return;
			case 4: TexCoord4 = val; return;
			case 5: TexCoord5 = val; return;
			case 6: TexCoord6 = val; return;
			case 7: TexCoord7 = val; return;
		}
	}
}

public unsafe struct IndexDesc
{
	public ushort* Indices;
	public uint OffsetIndex;
	public uint FirstIndex;
	public uint IndexSize;
}

public struct MeshDesc
{
	public VertexDesc Vertex;
	public IndexDesc Index;
}

public interface IVertexBuffer
{
	int VertexCount();
	VertexFormat GetVertexFormat();
	bool IsDynamic();
	void BeginCastBuffer(VertexFormat format);
	void EndCastBuffer();
	int GetRoomRemaining();
	bool Lock(int vertexCount, bool append, ref VertexDesc desc);
	bool Unlock(int vertexCount, ref VertexDesc desc);
}

public interface IIndexBuffer
{
	int IndexCount();
	MaterialIndexFormat IndexFormat();
	bool IsDynamic();
	void BeginCastBuffer(MaterialIndexFormat format);
	void EndCastBuffer();
	int Lock(bool readOnly, int firstIndex, int indexCount, ref IndexDesc desc);
	bool Unlock(int writtenIndexCount, ref IndexDesc desc);
}

public interface IMesh : IVertexBuffer, IIndexBuffer
{
	public const int VERTEX_MAX_TEXTURE_COORDINATES = 8;
	public const int BONE_MATRIX_INDEX_INVALID = 255;
	public const int INDEX_BUFFER_SIZE = 32768;
	public const int DYNAMIC_VERTEX_BUFFER_MEMORY = (1024 + 512) * 1024;
	public const int DYNAMIC_VERTEX_BUFFER_MEMORY_SMALL = 384 * 1024; // Only allocate this much during map transitions
	void SetPrimitiveType(MaterialPrimitiveType type);
	void Draw(int firstIndex = -1, int indexCount = 0);
	void SetColorMesh(IMesh colorMesh, int vertexOffset);
	void LockMesh(int vertexCount, int indexCount, ref MeshDesc desc);
	void ModifyBegin(int firstVertex, int vertexCount, int firstIndex, int indexCount, ref MeshDesc desc);
	void ModifyEnd(ref MeshDesc desc);
	void UnlockMesh(int vertexCount, int indexCount, ref MeshDesc desc);
	void MarkAsDrawn();
}

public unsafe struct VertexBuilder
{
	public const ulong INVALID_BUFFER_OFFSET = 0xFFFFFFFFUL;

	public VertexDesc Desc;
	public IVertexBuffer? VertexBuffer;

	// Used to make sure Begin/End calls and BeginModify/EndModify calls match.
	public bool Modify;

	// Max number of indices and vertices
	public int MaxVertexCount;

	// Number of indices and vertices
	public int VertexCount;

	// The current vertex and index
	public int CurrentVertex;

	// Optimization: Pointer to the current pos, norm, texcoord, and color
	public float* CurrPosition;
	public float* CurrNormal;
	public float* CurrTexCoord0;
	public float* CurrTexCoord1;
	public float* CurrTexCoord2;
	public float* CurrTexCoord3;
	public float* CurrTexCoord4;
	public float* CurrTexCoord5;
	public float* CurrTexCoord6;
	public float* CurrTexCoord7;
	public byte* CurrColor;

	// Total number of vertices appended
	public int TotalVertexCount;

	// First vertex buffer offset + index
	public uint BufferOffset;
	public uint BufferFirstVertex;

	// Debug checks to make sure we write userdata4/tangents AFTER normals
	public bool WrittenNormal;
	public bool WrittenUserData;

	public void AttachBegin(IMesh mesh, int maxVertexCount, ref VertexDesc desc) {
		VertexCompressionType compressionType = Desc.CompressionType;
		VertexBuffer = mesh;
		memcpy(ref Desc, ref desc);
		MaxVertexCount = maxVertexCount;
		VertexCount = 0;
		Modify = false;

		if (compressionType != VertexCompressionType.Invalid)
			Desc.CompressionType = compressionType;

		ValidateCompressionType();
		if (BufferOffset == INVALID_BUFFER_OFFSET) {
			TotalVertexCount = 0;
			BufferOffset = desc.OffsetVertex;
			BufferFirstVertex = (uint)desc.FirstVertex;
		}
	}

	private void ValidateCompressionType() {

	}

	public void Reset() {
		CurrentVertex = 0;

		CurrPosition = Desc.Position;
		CurrNormal = Desc.Normal;

		CurrTexCoord0 = Desc.TexCoord0;
		CurrTexCoord1 = Desc.TexCoord1;
		CurrTexCoord2 = Desc.TexCoord2;
		CurrTexCoord3 = Desc.TexCoord3;
		CurrTexCoord4 = Desc.TexCoord4;
		CurrTexCoord5 = Desc.TexCoord5;
		CurrTexCoord6 = Desc.TexCoord6;
		CurrTexCoord7 = Desc.TexCoord7;

		CurrColor = Desc.Color;
		WrittenNormal = false;
		WrittenUserData = false;

	}

	internal void Position3f(float x, float y, float z) {
		float* pDst = CurrPosition;
		*pDst++ = x;
		*pDst++ = y;
		*pDst = z;
	}

	public void Position3fv(ReadOnlySpan<float> v) {
		fixed (float* vptr = v) {
			float* vp = vptr;
			float* pDst = CurrPosition;
			*pDst++ = *vp++;
			*pDst++ = *vp++;
			*pDst = *vp;
		}
	}

	public void Normal3fv(ReadOnlySpan<float> n) {
		fixed (float* nptr = n) {
			float* np = nptr;
			float* pDst = CurrNormal;
			*pDst++ = *np++;
			*pDst++ = *np++;
			*pDst = *np;
		}
	}

	public void Normal3f(float x, float y, float z) {
		float* pDst = CurrNormal;
		*pDst++ = x;
		*pDst++ = y;
		*pDst = z;
	}

	public void Color3f(float r, float g, float b) {
		byte* pDst = CurrColor;
		*pDst++ = (byte)Math.Clamp(r * 255, 0, 255);
		*pDst++ = (byte)Math.Clamp(g * 255, 0, 255);
		*pDst++ = (byte)Math.Clamp(b * 255, 0, 255);
		*pDst = 255;
	}

	public void Color3fv(ReadOnlySpan<float> rgb) {
		fixed (float* cptr = rgb) {
			float* cp = cptr;
			byte* pDst = CurrColor;
			*pDst++ = (byte)Math.Clamp(*cp++, 0, 255);
			*pDst++ = (byte)Math.Clamp(*cp++, 0, 255);
			*pDst++ = (byte)Math.Clamp(*cp++, 0, 255);
			*pDst = 255;
		}
	}

	public void Color4f(float r, float g, float b, float a) {
		byte* pDst = CurrColor;
		*pDst++ = (byte)Math.Clamp(r * 255, 0, 255);
		*pDst++ = (byte)Math.Clamp(g * 255, 0, 255);
		*pDst++ = (byte)Math.Clamp(b * 255, 0, 255);
		*pDst = (byte)Math.Clamp(a * 255, 0, 255);
	}

	public void Color4fv(ReadOnlySpan<float> rgba) {
		fixed (float* cptr = rgba) {
			float* cp = cptr;
			byte* pDst = CurrColor;
			*pDst++ = (byte)Math.Clamp(*cp++, 0, 255);
			*pDst++ = (byte)Math.Clamp(*cp++, 0, 255);
			*pDst++ = (byte)Math.Clamp(*cp++, 0, 255);
			*pDst = (byte)Math.Clamp(*cp, 0, 255);
		}
	}


	internal void TexCoord2f(int stage, float s, float t) {
		float* pDst = stage switch {
			0 => CurrTexCoord0,
			1 => CurrTexCoord1,
			2 => CurrTexCoord2,
			3 => CurrTexCoord3,
			4 => CurrTexCoord4,
			5 => CurrTexCoord5,
			6 => CurrTexCoord6,
			7 => CurrTexCoord7,
			_ => null
		};
		if (pDst == null) return;
		*pDst++ = s;
		*pDst++ = t;
	}

	public void AdvanceVertex() {
		if (++CurrentVertex > VertexCount)
			VertexCount = CurrentVertex;

		// If this cast isn't done, it increments way too far
		CurrPosition = (float*)((byte*)CurrPosition + Desc.PositionSize);
		CurrNormal = (float*)((byte*)CurrNormal + Desc.NormalSize);
		CurrColor = CurrColor + Desc.ColorSize;
		
		CurrTexCoord0 = (float*)((byte*)CurrTexCoord0 + Desc.TexCoordSize[0]);
		CurrTexCoord1 = (float*)((byte*)CurrTexCoord1 + Desc.TexCoordSize[1]);
		CurrTexCoord2 = (float*)((byte*)CurrTexCoord2 + Desc.TexCoordSize[2]);
		CurrTexCoord3 = (float*)((byte*)CurrTexCoord3 + Desc.TexCoordSize[3]);
		CurrTexCoord4 = (float*)((byte*)CurrTexCoord4 + Desc.TexCoordSize[4]);
		CurrTexCoord5 = (float*)((byte*)CurrTexCoord5 + Desc.TexCoordSize[5]);
		CurrTexCoord6 = (float*)((byte*)CurrTexCoord6 + Desc.TexCoordSize[6]);
		CurrTexCoord7 = (float*)((byte*)CurrTexCoord7 + Desc.TexCoordSize[7]);
	}

	internal void AttachEnd() {
		VertexBuffer = null;
		MaxVertexCount = 0;

		CurrPosition = null;
		CurrNormal = null;
		CurrColor = null;

		memreset(ref Desc);
	}

	internal void Color3ubv(ReadOnlySpan<byte> rgb) {
		byte* pDst = CurrColor;
		*pDst++ = rgb[0];
		*pDst++ = rgb[1];
		*pDst = rgb[2];
	}

	internal void Color4ubv(ReadOnlySpan<byte> rgba) {
		byte* pDst = CurrColor;
		*pDst++ = rgba[0];
		*pDst++ = rgba[1];
		*pDst++ = rgba[2];
		*pDst = rgba[3];
	}

	internal unsafe void CompressedBoneWeight3fv(ReadOnlySpan<float> boneWeights) {
		float* pDestWeights = OffsetFloatPointer(Desc.BoneWeight, CurrentVertex, Desc.BoneWeightSize);
		// No compression yet
		pDestWeights[0] = boneWeights[0];
		pDestWeights[1] = boneWeights[1];
	}

	private float* OffsetFloatPointer(float* bufferPointer, int vertexCount, int vertexSize) {
		return (float*)((byte*)(bufferPointer) + (vertexCount * vertexSize));
	}

	internal void BoneWeight(int idx, float weight) {
		if(idx < Desc.NumBoneWeights) {
			float* boneWeight = OffsetFloatPointer(Desc.BoneWeight, CurrentVertex, Desc.BoneWeightSize);
			boneWeight[idx] = weight;
		}
	}

	internal void BoneMatrix(int idx, byte matrixIndex) {
		if (matrixIndex == IMesh.BONE_MATRIX_INDEX_INVALID)
			matrixIndex = 0;

		byte* boneMatrix = &Desc.BoneMatrixIndex[CurrentVertex * Desc.BoneMatrixIndexSize];
		boneMatrix[idx] = matrixIndex;
	}
}

public struct IndexBuilder
{
	public IndexDesc Desc;
	public IIndexBuffer? IndexBuffer;

	// Max number of indices
	public int MaxIndexCount;

	// Number of indices
	public int IndexCount;

	// Offset to add to each index as it's written into the buffer
	public int IndexOffset;

	// The current index
	public int CurrentIndex;

	// Total number of indices appended
	public int TotalIndexCount;

	// First index buffer offset + first index
	public uint BufferOffset;
	public uint BufferFirstIndex;

	// Used to make sure Begin/End calls and BeginModify/EndModify calls match.
	public bool Modify;

	// What the hell, this needs MeshDesc.... okay....
	public unsafe void AttachBegin(IMesh mesh, int maxIndexCount, ref MeshDesc desc) {
		IndexBuffer = mesh;
		IndexCount = 0;
		MaxIndexCount = maxIndexCount;

		Modify = false;

		IndexOffset = (int)desc.Vertex.FirstVertex;
		Desc.Indices = desc.Index.Indices;
		Desc.IndexSize = desc.Index.IndexSize;
		Reset();
	}

	public void Reset() {
		CurrentIndex = 0;
	}

	internal unsafe void GenerateIndices(MaterialPrimitiveType primitiveType, int indexCount) {
		if (Desc.IndexSize == 0)
			return;

		int maxIndices = MaxIndexCount - CurrentIndex;
		indexCount = Math.Min(maxIndices, indexCount);
		if (indexCount == 0)
			return;

		ushort* indices = &Desc.Indices[CurrentIndex];
		switch (primitiveType) {
			case MaterialPrimitiveType.InstancedQuads:
				Assert(false); // Shouldn't get here (this primtype is unindexed)
				break;
			case MaterialPrimitiveType.Quads:
				GenerateQuadIndexBuffer(indices, indexCount, IndexOffset);
				break;
			case MaterialPrimitiveType.Polygon:
				GeneratePolygonIndexBuffer(indices, indexCount, IndexOffset);
				break;
			case MaterialPrimitiveType.LineStrip:
				GenerateLineStripIndexBuffer(indices, indexCount, IndexOffset);
				break;
			case MaterialPrimitiveType.LineLoop:
				GenerateLineLoopIndexBuffer(indices, indexCount, IndexOffset);
				break;
			case MaterialPrimitiveType.Points:
				Assert(false); // Shouldn't get here (this primtype is unindexed)
				break;
			default:
				GenerateSequentialIndexBuffer(indices, indexCount, IndexOffset);
				break;
		}

		AdvanceIndices(indexCount);
	}

	private unsafe static void GenerateLineStripIndexBuffer(ushort* indices, int indexCount, int indexOffset) {
		throw new NotImplementedException();
	}

	private unsafe static void GenerateLineLoopIndexBuffer(ushort* indices, int indexCount, int indexOffset) {
		throw new NotImplementedException();
	}

	private unsafe static void GenerateSequentialIndexBuffer(ushort* indices, int indexCount, int indexOffset) {
		if (indices == null)
			return;

		for (int i = 0; i < indexCount; ++i)
			indices[i] = (ushort)(i + indexOffset);
	}

	private unsafe static void GeneratePolygonIndexBuffer(ushort* indices, int indexCount, int indexOffset) {
		if (indices == null)
			return;

		int i;
		int numPolygons = indexCount / 3;
		for (i = 0; i < numPolygons; ++i) {
			indices[0] = (ushort)(indexOffset);
			indices[1] = (ushort)(indexOffset + i + 1);
			indices[2] = (ushort)(indexOffset + i + 2);
			indices += 3;
		}
	}

	private unsafe static void GenerateQuadIndexBuffer(ushort* indices, int indexCount, int firstVertex) {
		if (indices == null)
			return;

		// Format the quad buffer
		int i;
		int numQuads = indexCount / 6;
		int baseVertex = firstVertex;
		for (i = 0; i < numQuads; ++i) {
			indices[0] = (ushort)(baseVertex);
			indices[1] = (ushort)(baseVertex + 1);
			indices[2] = (ushort)(baseVertex + 2);

			indices[3] = (ushort)(baseVertex);
			indices[4] = (ushort)(baseVertex + 2);
			indices[5] = (ushort)(baseVertex + 3);

			baseVertex += 4;
			indices += 6;
		}
	}

	public void AdvanceIndex() {
		CurrentIndex += (int)Desc.IndexSize;
		if (CurrentIndex > IndexCount)
			IndexCount = CurrentIndex;
	}

	public void AdvanceIndices(int indices) {
		CurrentIndex += (int)(indices * Desc.IndexSize);
		if (CurrentIndex > IndexCount)
			IndexCount = CurrentIndex;
	}

	internal void AttachEnd() {
		IndexBuffer = null;
		MaxIndexCount = 0;
		memreset(ref Desc);
	}

	public unsafe void Index(ushort index) {
		Desc.Indices[CurrentIndex] = (ushort)(IndexOffset + index);
	}

	public unsafe void FastIndex(ushort index) {
		Desc.Indices[CurrentIndex] = (ushort)(IndexOffset + index);
		CurrentIndex += (ushort)Desc.IndexSize;
		IndexCount = CurrentIndex;
	}

	public unsafe void FastTriangle(int startVert) {
		startVert += IndexOffset;
		if (CurrentIndex + 2 >= MaxIndexCount)
			throw new IndexOutOfRangeException();
		Desc.Indices[CurrentIndex] = (ushort)(startVert);
		Desc.Indices[CurrentIndex + 1] = (ushort)(startVert + 1);
		Desc.Indices[CurrentIndex + 2] = (ushort)(startVert + 2);
		AdvanceIndices(3);
	}

	public unsafe void FastQuad(int startVert) {
		startVert += IndexOffset;
		Desc.Indices[CurrentIndex + 0] = (ushort)startVert;
		Desc.Indices[CurrentIndex + 1] = (ushort)(startVert + 1);
		Desc.Indices[CurrentIndex + 2] = (ushort)(startVert + 2);
		Desc.Indices[CurrentIndex + 3] = (ushort)startVert;
		Desc.Indices[CurrentIndex + 4] = (ushort)(startVert + 2);
		Desc.Indices[CurrentIndex + 5] = (ushort)(startVert + 3);
		AdvanceIndices(6);
	}
}

public unsafe struct MeshBuilder : IDisposable
{
	public MeshDesc Desc;

	public MeshBuilder() { }
	public void Dispose() => Assert(Mesh == null);  // if this fires you did a Begin() without an End()


	// This must be called before Begin, if a vertex buffer with a compressed format is to be used
	public void SetCompressionType(VertexCompressionType compressionType) {
		// No op for now
	}

	// Locks the vertex buffer
	// (*cannot* use the Index() call below)
	public void Begin(IMesh pMesh, MaterialPrimitiveType type, int numPrimitives) {
		Assert(pMesh != null && Mesh == null);
		Assert(type != MaterialPrimitiveType.Heterogenous);

		Mesh = pMesh;
		GenerateIndices = true;
		Type = type;

		ComputeNumVertsAndIndices(out int maxVertexCount, out int maxIndexCount, type, numPrimitives);
		switch (type) {
			case MaterialPrimitiveType.InstancedQuads:
				Mesh.SetPrimitiveType(MaterialPrimitiveType.InstancedQuads);
				break;
			case MaterialPrimitiveType.Quads:
			case MaterialPrimitiveType.Polygon:
				Mesh.SetPrimitiveType(MaterialPrimitiveType.Triangles);
				break;
			case MaterialPrimitiveType.LineStrip:
			case MaterialPrimitiveType.LineLoop:
				Mesh.SetPrimitiveType(MaterialPrimitiveType.Lines);
				break;
			default:
				Mesh.SetPrimitiveType(type);
				break;
		}

		Mesh.LockMesh(maxVertexCount, maxIndexCount, ref Desc);
		IndexBuilder.AttachBegin(Mesh, maxIndexCount, ref Desc);
		VertexBuilder.AttachBegin(Mesh, maxVertexCount, ref Desc.Vertex);

		Reset();
	}

	// Locks the vertex buffer, can specify arbitrary index lists
	// (must use the Index() call below)
	public void Begin(IMesh pMesh, MaterialPrimitiveType type, int nVertexCount, int nIndexCount, ref int nFirstVertex) => throw new NotImplementedException();
	public void Begin(IMesh pMesh, MaterialPrimitiveType type, int nVertexCount, int nIndexCount) {
		Assert(pMesh != null && Mesh == null);
		Assert((type != MaterialPrimitiveType.Quads) && (type != MaterialPrimitiveType.InstancedQuads) && (type != MaterialPrimitiveType.Polygon) &&
			(type != MaterialPrimitiveType.LineStrip) && (type != MaterialPrimitiveType.LineLoop));

		Assert(type != MaterialPrimitiveType.Points);

		Mesh = pMesh;
		GenerateIndices = false;
		Type = type;

		Mesh.SetPrimitiveType(type);
		Mesh.LockMesh(nVertexCount, nIndexCount, ref Desc);

		IndexBuilder.AttachBegin(pMesh, nIndexCount, ref Desc);
		VertexBuilder.AttachBegin(pMesh, nVertexCount, ref Desc.Vertex);

		Reset();
	}

	// forward compat
	public void Begin(IVertexBuffer pVertexBuffer, MaterialPrimitiveType type, int numPrimitives) => throw new NotImplementedException();
	public void Begin(IVertexBuffer pVertexBuffer, IIndexBuffer pIndexBuffer, MaterialPrimitiveType type, int nVertexCount, int nIndexCount, ref int nFirstVertex) => throw new NotImplementedException();
	public void Begin(IVertexBuffer pVertexBuffer, IIndexBuffer pIndexBuffer, MaterialPrimitiveType type, int nVertexCount, int nIndexCount) => throw new NotImplementedException();

	// Use this when you're done writing
	// Set bDraw to true to call m_pMesh->Draw automatically.
	public void End(bool spewData = false, bool draw = false) {
		if (GenerateIndices) {
			int indexCount = IndicesFromVertices(Type, VertexBuilder.VertexCount);
			IndexBuilder.GenerateIndices(Type, indexCount);
		}

		if (spewData)
			Warning("Mesh spew not supported yet...\n");

		Mesh!.UnlockMesh(VertexBuilder.VertexCount, IndexBuilder.IndexCount, ref Desc);

		IndexBuilder.AttachEnd();
		VertexBuilder.AttachEnd();
		if (draw)
			Mesh.Draw();

		Mesh = null;
		memreset(ref Desc);
	}

	// Locks the vertex buffer to modify existing data
	// Passing nVertexCount == -1 says to lock all the vertices for modification.
	// Pass 0 for nIndexCount to not lock the index buffer.
	public void BeginModify(IMesh pMesh, int nFirstVertex = 0, int nVertexCount = -1, int nFirstIndex = 0, int nIndexCount = 0) => throw new NotImplementedException();
	public void EndModify(bool bSpewData = false) => throw new NotImplementedException();

	// A helper method since this seems to be done a whole bunch.
	public void DrawQuad(IMesh pMesh, ReadOnlySpan<float> v1, ReadOnlySpan<float> v2, ReadOnlySpan<float> v3, ReadOnlySpan<float> v4, ReadOnlySpan<byte> pColor, bool wireframe = false) => throw new NotImplementedException();

	// returns the number of indices and vertices
	public int VertexCount() => VertexBuilder.VertexCount;
	public int IndexCount() => IndexBuilder.IndexCount;

	// Resets the mesh builder so it points to the start of everything again
	public void Reset() {
		IndexBuilder.Reset();
		VertexBuilder.Reset();
	}

	// Returns the size of the vertex
	public int VertexSize() { return Desc.Vertex.ActualVertexSize; }

	// returns the data size of a given texture coordinate
	public int TextureCoordinateSize(int nTexCoordNumber) { return Desc.Vertex.TexCoordSize[nTexCoordNumber]; }

	// Selects the nth Vertex and Index 
	public void SelectVertex(int idx) => throw new NotImplementedException();
	public void SelectIndex(int idx) => throw new NotImplementedException();

	// Given an index, point to the associated vertex
	public void SelectVertexFromIndex(int idx) => throw new NotImplementedException();
	// Advances the current vertex and index by one
	public void AdvanceVertex() => VertexBuilder.AdvanceVertex();
	public void AdvanceVertices(int nVerts) => throw new NotImplementedException();
	public void AdvanceIndex() => IndexBuilder.AdvanceIndex();
	public void AdvanceIndices(int indices) => IndexBuilder.AdvanceIndices(indices);
	public int GetCurrentVertex() => VertexBuilder.CurrentVertex;
	public int GetCurrentIndex() => IndexBuilder.CurrentIndex;

	// Data retrieval...
	public ReadOnlySpan<float> Position() => throw new NotImplementedException();
	public ReadOnlySpan<float> Normal() => throw new NotImplementedException();
	public ReadOnlySpan<uint> Color() => throw new NotImplementedException();
	public ReadOnlySpan<byte> Specular() => throw new NotImplementedException();
	public ReadOnlySpan<float> TexCoord(int stage) => throw new NotImplementedException();
	public ReadOnlySpan<float> TangentS() => throw new NotImplementedException();
	public ReadOnlySpan<float> TangentT() => throw new NotImplementedException();
	public ReadOnlySpan<float> BoneWeight() => throw new NotImplementedException();
	public ReadOnlySpan<float> Wrinkle() => throw new NotImplementedException();

	public int NumBoneWeights() => throw new NotImplementedException();

	public ReadOnlySpan<byte> BoneMatrix() => throw new NotImplementedException();
	public ReadOnlySpan<ushort> Index() => throw new NotImplementedException();

	// position setting
	public void Position3f(float x, float y, float z) => VertexBuilder.Position3f(x, y, z);
	public void Position3fv(ReadOnlySpan<float> v) => VertexBuilder.Position3fv(v);
	public void Position3fv(in Vector3 vec) {
		fixed (Vector3* ptr = &vec)
			VertexBuilder.Position3fv(new(ptr, 3));
	}

	// normal setting
	public void Normal3f(float x, float y, float z) => VertexBuilder.Normal3f(x, y, z);
	public void Normal3fv(ReadOnlySpan<float> n) => VertexBuilder.Normal3fv(n);
	public void Normal3fv(in Vector3 vec) => VertexBuilder.Normal3f(vec.X, vec.Y, vec.Z);
	// What do these even do
	public void NormalDelta3fv(ReadOnlySpan<float> n) => throw new NotImplementedException();
	public void NormalDelta3f(float nx, float ny, float nz) => throw new NotImplementedException();

	// color setting
	public void Color3f(float r, float g, float b) => VertexBuilder.Color3f(r, g, b);
	public void Color3fv(ReadOnlySpan<float> rgb) => VertexBuilder.Color3fv(rgb);
	public void Color4f(float r, float g, float b, float a) => VertexBuilder.Color4f(r, g, b, a);
	public void Color4fv(ReadOnlySpan<float> rgba) => VertexBuilder.Color4fv(rgba);

	// Faster versions of color
	public void Color3ub(byte r, byte g, byte b) => VertexBuilder.Color3ubv([r, g, b]);
	public void Color3ubv(in Color rgb) {
		fixed (Color* ptr = &rgb)
			VertexBuilder.Color3ubv(new(ptr, 3));
	}
	public void Color4ub(byte r, byte g, byte b, byte a) => VertexBuilder.Color4ubv([r, g, b, a]);
	public unsafe void Color4ubv(ReadOnlySpan<byte> rgba) => VertexBuilder.Color4ubv(rgba);
	public unsafe void Color4ubv(in Color rgba) {
		fixed (Color* ptr = &rgba)
			VertexBuilder.Color4ubv(new(ptr, 4));
	}

	// specular color setting
	public void Specular3f(float r, float g, float b) => throw new NotImplementedException();
	public void Specular3fv(ReadOnlySpan<float> rgb) => throw new NotImplementedException();
	public void Specular4f(float r, float g, float b, float a) => throw new NotImplementedException();
	public void Specular4fv(ReadOnlySpan<float> rgba) => throw new NotImplementedException();

	// Faster version of specular
	public void Specular3ub(byte r, byte g, byte b) => throw new NotImplementedException();
	public void Specular3ubv(ReadOnlySpan<byte> c) => throw new NotImplementedException();
	public void Specular4ub(byte r, byte g, byte b, byte a) => throw new NotImplementedException();
	public void Specular4ubv(ReadOnlySpan<byte> c) => throw new NotImplementedException();

	// texture coordinate setting
	public void TexCoord1f(int stage, float s) => throw new NotImplementedException();
	public void TexCoord2f(int stage, float s, float t) => VertexBuilder.TexCoord2f(stage, s, t);
	public void TexCoord2fv(int stage, ReadOnlySpan<float> st) => VertexBuilder.TexCoord2f(stage, st[0], st[1]);
	public void TexCoord2fv(int stage, in Vector2 vec) => VertexBuilder.TexCoord2f(stage, vec.X, vec.Y);
	public void TexCoord3f(int stage, float s, float t, float u) => throw new NotImplementedException();
	public void TexCoord3fv(int stage, ReadOnlySpan<float> stu) => throw new NotImplementedException();
	public void TexCoord4f(int stage, float s, float t, float u, float w) => throw new NotImplementedException();
	public void TexCoord4fv(int stage, ReadOnlySpan<float> stuv) => throw new NotImplementedException();

	public void TexCoordSubRect2f(int stage, float s, float t, float offsetS, float offsetT, float scaleS, float scaleT) => throw new NotImplementedException();
	public void TexCoordSubRect2fv(int stage, ReadOnlySpan<float> st, ReadOnlySpan<float> offset, ReadOnlySpan<float> scale) => throw new NotImplementedException();

	// tangent space 
	public void TangentS3f(float sx, float sy, float sz) { /* TODO: add tangents to vertex elements + descriptor */ }
	public void TangentS3fv(ReadOnlySpan<float> s) { /* TODO: add tangents to vertex elements + descriptor */ }
	public void TangentS3fv(Vector3 vec) { /* TODO: add tangents to vertex elements + descriptor */ }

	public void TangentT3f(float tx, float ty, float tz) { /* TODO: add tangents to vertex elements + descriptor */ }
	public void TangentT3fv(ReadOnlySpan<float> t) { /* TODO: add tangents to vertex elements + descriptor */ }
	public void TangentT3fv(Vector3 vec) { /* TODO: add tangents to vertex elements + descriptor */ }

	// Wrinkle
	public void Wrinkle1f(float flWrinkle) => throw new NotImplementedException();

	// bone weights
	public void BoneWeight(int idx, float weight) => VertexBuilder.BoneWeight(idx, weight);
	// bone weights (templatized for code which needs to support compressed vertices)

	// bone matrix index
	public void BoneMatrix(int idx, int matrixIndex) => VertexBuilder.BoneMatrix(idx, (byte)matrixIndex);

	// Generic per-vertex data
	public void UserData(ReadOnlySpan<float> pData) => throw new NotImplementedException();

	// Used to define the indices (only used if you aren't using primitives)
	public void Index(ushort index) => IndexBuilder.Index(index);

	public void FastIndex(ushort index) => IndexBuilder.FastIndex(index);
	public void FastTriangle(int startVert) => IndexBuilder.FastTriangle(startVert);


	private void ComputeNumVertsAndIndices(out int maxVertices, out int maxIndices, MaterialPrimitiveType type, int primitiveCount) {
		switch (type) {
			case MaterialPrimitiveType.Points:
				maxVertices = maxIndices = primitiveCount;
				break;

			case MaterialPrimitiveType.Lines:
				maxVertices = maxIndices = primitiveCount * 2;
				break;

			case MaterialPrimitiveType.LineStrip:
				maxVertices = primitiveCount + 1;
				maxIndices = primitiveCount * 2;
				break;

			case MaterialPrimitiveType.LineLoop:
				maxVertices = primitiveCount;
				maxIndices = primitiveCount * 2;
				break;

			case MaterialPrimitiveType.Triangles:
				maxVertices = maxIndices = primitiveCount * 3;
				break;

			case MaterialPrimitiveType.TriangleStrip:
				maxVertices = maxIndices = primitiveCount + 2;
				break;

			case MaterialPrimitiveType.Quads:
				maxVertices = primitiveCount * 4;
				maxIndices = primitiveCount * 6;
				break;

			case MaterialPrimitiveType.InstancedQuads:
				maxVertices = primitiveCount;
				maxIndices = 0; // This primtype is unindexed
				break;

			case MaterialPrimitiveType.Polygon:
				maxVertices = primitiveCount;
				maxIndices = (primitiveCount - 2) * 3;
				break;

			default:
				maxVertices = 0;
				maxIndices = 0;
				Assert(false);
				break;
		}

		Assert(maxVertices <= 32768);
		Assert(maxIndices <= 32768);
	}
	private int IndicesFromVertices(MaterialPrimitiveType type, int vertexCount) {
		switch (type) {
			case MaterialPrimitiveType.Quads:
				Assert((vertexCount & 0x3) == 0);
				return (vertexCount * 6) / 4;
			case MaterialPrimitiveType.InstancedQuads:
				// This primtype is unindexed
				return 0;
			case MaterialPrimitiveType.Polygon:
				Assert(vertexCount >= 3);
				return (vertexCount - 2) * 3;
			case MaterialPrimitiveType.LineStrip:
				Assert(vertexCount >= 2);
				return (vertexCount - 1) * 2;
			case MaterialPrimitiveType.LineLoop:
				Assert(vertexCount >= 3);
				return vertexCount * 2;
			default:
				return vertexCount;
		}
	}

	public void CompressedBoneWeight3fv(ReadOnlySpan<float> boneWeights) {
		VertexBuilder.CompressedBoneWeight3fv(boneWeights);
	}

	// The mesh we're modifying
	IMesh? Mesh;

	MaterialPrimitiveType Type;

	// Generate indices?
	bool GenerateIndices;

	public IndexBuilder IndexBuilder;
	public VertexBuilder VertexBuilder;
}
