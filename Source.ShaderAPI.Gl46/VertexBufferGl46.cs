using Source.Common.MaterialSystem;

using System.Runtime.InteropServices;

namespace Source.ShaderAPI.Gl46;

public unsafe class VertexBufferGl46 : IDisposable
{
	VertexFormat VertexBufferFormat;
	internal int Position;
	internal int VertexCount;
	internal int VertexSize;
	internal void* SysmemBuffer;
	internal int SysmemBufferStartBytes;
	internal int BufferSize;

	internal uint LockCount;
	internal bool Dynamic;
	internal bool Locked;
	internal bool Flush;
	internal bool ExternalMemory;
	internal bool SoftwareVertexProcessing;
	internal bool LateCreateShouldDiscard;

	int vao = -1;
	int vbo = -1;

	internal uint VAO() => vao > 0 ? (uint)vao : throw new NullReferenceException("Vertex Array Object was null");
	internal uint VBO() => vbo > 0 ? (uint)vbo : throw new NullReferenceException("Vertex Buffer Object was null");

	public VertexBufferGl46(bool dynamic) {
		Dynamic = dynamic;
	}

	public VertexBufferGl46(VertexFormat format, int vertexSize, int vertexCount, bool dynamic) {
		VertexBufferFormat = format;
		VertexSize = vertexSize;
		VertexCount = vertexCount;
		BufferSize = VertexSize * VertexCount;
		Dynamic = dynamic;
		Locked = false;
		Flush = true;
		ExternalMemory = false;
	}

	public void FlushASAP() => Flush = true;

	public enum OpenGL_ShaderInputAttribute
	{
		Position = 0,
		Normal = 1,
		Color = 2,
		Specular = 3,
		TangentS = 4,
		TangentT = 5,
		Wrinkle = 6,
		BoneIndex = 7,
		BoneWeights = 8,
		UserData = 9,
		TexCoord0 = 10,
		TexCoord1 = 11,
		TexCoord2 = 12,
		TexCoord3 = 13,
		TexCoord4 = 14,
		TexCoord5 = 15,
		TexCoord6 = 16,
		TexCoord7 = 17,
		Count
	}

	public static bool IsOn(OpenGL_ShaderInputAttribute shaderAttr, VertexFormat format, out int size, out VertexElement element) {
		switch (shaderAttr) {
			case OpenGL_ShaderInputAttribute.Position: size = 1; element = VertexElement.Position; return (format & VertexFormat.Position) != 0;
			case OpenGL_ShaderInputAttribute.Normal: size = 1; element = VertexElement.Normal; return (format & VertexFormat.Normal) != 0;
			case OpenGL_ShaderInputAttribute.Color: size = 1; element = VertexElement.Color; return (format & VertexFormat.Color) != 0;
			case OpenGL_ShaderInputAttribute.Specular: size = 1; element = VertexElement.Specular; return (format & VertexFormat.Specular) != 0;
			case OpenGL_ShaderInputAttribute.TangentS: size = 1; element = VertexElement.TangentS; return (format & VertexFormat.TangentS) != 0;
			case OpenGL_ShaderInputAttribute.TangentT: size = 1; element = VertexElement.TangentT; return (format & VertexFormat.TangentT) != 0;
			case OpenGL_ShaderInputAttribute.Wrinkle: size = 1; element = VertexElement.Wrinkle; return (format & VertexFormat.Wrinkle) != 0;
			case OpenGL_ShaderInputAttribute.BoneIndex: size = 1; element = VertexElement.BoneIndex; return (format & VertexFormat.BoneIndex) != 0;
			case OpenGL_ShaderInputAttribute.BoneWeights:
				int numBoneWeights = format.GetBoneWeightsSize();
				size = numBoneWeights;
				element = VertexElement.BoneWeights1 + (numBoneWeights - 1);
				return numBoneWeights > 0;
			case OpenGL_ShaderInputAttribute.UserData:
				int userDataSize = format.GetUserDataSize();
				size = userDataSize;

				element = VertexElement.UserData1 + (userDataSize - 1);
				return userDataSize > 0;
			case OpenGL_ShaderInputAttribute.TexCoord0:
			case OpenGL_ShaderInputAttribute.TexCoord1:
			case OpenGL_ShaderInputAttribute.TexCoord2:
			case OpenGL_ShaderInputAttribute.TexCoord3:
			case OpenGL_ShaderInputAttribute.TexCoord4:
			case OpenGL_ShaderInputAttribute.TexCoord5:
			case OpenGL_ShaderInputAttribute.TexCoord6:
			case OpenGL_ShaderInputAttribute.TexCoord7:
				int index = shaderAttr - OpenGL_ShaderInputAttribute.TexCoord0;
				int texCoordSize = format.GetTexCoordDimensionSize(index);
				size = texCoordSize;
				element = index switch {
					0 => texCoordSize switch { 0 => default, 1 => VertexElement.TexCoord1D_0, 2 => VertexElement.TexCoord2D_0, 3 => VertexElement.TexCoord3D_0, 4 => VertexElement.TexCoord4D_0, _ => throw new NotSupportedException() },
					1 => texCoordSize switch { 0 => default, 1 => VertexElement.TexCoord1D_1, 2 => VertexElement.TexCoord2D_1, 3 => VertexElement.TexCoord3D_1, 4 => VertexElement.TexCoord4D_1, _ => throw new NotSupportedException() },
					2 => texCoordSize switch { 0 => default, 1 => VertexElement.TexCoord1D_2, 2 => VertexElement.TexCoord2D_2, 3 => VertexElement.TexCoord3D_2, 4 => VertexElement.TexCoord4D_2, _ => throw new NotSupportedException() },
					3 => texCoordSize switch { 0 => default, 1 => VertexElement.TexCoord1D_3, 2 => VertexElement.TexCoord2D_3, 3 => VertexElement.TexCoord3D_3, 4 => VertexElement.TexCoord4D_3, _ => throw new NotSupportedException() },
					4 => texCoordSize switch { 0 => default, 1 => VertexElement.TexCoord1D_4, 2 => VertexElement.TexCoord2D_4, 3 => VertexElement.TexCoord3D_4, 4 => VertexElement.TexCoord4D_4, _ => throw new NotSupportedException() },
					5 => texCoordSize switch { 0 => default, 1 => VertexElement.TexCoord1D_5, 2 => VertexElement.TexCoord2D_5, 3 => VertexElement.TexCoord3D_5, 4 => VertexElement.TexCoord4D_5, _ => throw new NotSupportedException() },
					6 => texCoordSize switch { 0 => default, 1 => VertexElement.TexCoord1D_6, 2 => VertexElement.TexCoord2D_6, 3 => VertexElement.TexCoord3D_6, 4 => VertexElement.TexCoord4D_6, _ => throw new NotSupportedException() },
					7 => texCoordSize switch { 0 => default, 1 => VertexElement.TexCoord1D_7, 2 => VertexElement.TexCoord2D_7, 3 => VertexElement.TexCoord3D_7, 4 => VertexElement.TexCoord4D_7, _ => throw new NotSupportedException() },
					_ => throw new NotSupportedException()
				};
				return texCoordSize > 0;
			default: throw new NotSupportedException();
		}
	}

	public void RecomputeVAO() {
		// Unlike the VBO, we do not need to destroy everything when the state changes
		if (this.vao == -1) {
			this.vao = (int)glCreateVertexArray();
			glObjectLabel(GL_VERTEX_ARRAY, (uint)this.vao, "MaterialSystem VertexBuffer");
		}

		// But we need a VBO first
		if (vbo == -1)
			RecomputeVBO();

		uint vao = (uint)this.vao;
		int offset = 0;

		Span<uint> bindings = stackalloc uint[64];
		int bindingsPtr = 0;

		void ConfigureAttribute(Span<uint> bindings, OpenGL_ShaderInputAttribute attr, VertexElement element, int size) {
			element.GetInformation(out int count, out VertexAttributeType type);
			int elementSize = count * (int)type.SizeOf();

			glEnableVertexArrayAttrib(vao, (uint)attr);
			bool normalize = attr == OpenGL_ShaderInputAttribute.Color;
			glVertexArrayAttribFormat(vao, (uint)attr, count, (int)type, normalize, (uint)offset);
			bindings[bindingsPtr++] = (uint)attr;
			offset += elementSize;
		}

		// Rewrote this to process attributes in the same order as ComputeVertexDescription

		if ((VertexBufferFormat & VertexFormat.Position) != 0) {
			ConfigureAttribute(bindings, OpenGL_ShaderInputAttribute.Position, VertexElement.Position, 1);
		}
		else {
			glDisableVertexArrayAttrib(vao, (uint)OpenGL_ShaderInputAttribute.Position);
		}

		if ((VertexBufferFormat & VertexFormat.BoneIndex) != 0) {
			int numBoneWeights = VertexBufferFormat.GetBoneWeightsSize();
			if (numBoneWeights > 0) {
				VertexElement boneWeightElement = VertexElement.BoneWeights1 + (numBoneWeights - 1);
				ConfigureAttribute(bindings, OpenGL_ShaderInputAttribute.BoneWeights, boneWeightElement, numBoneWeights);
			}
			else {
				glDisableVertexArrayAttrib(vao, (uint)OpenGL_ShaderInputAttribute.BoneWeights);
			}

			ConfigureAttribute(bindings, OpenGL_ShaderInputAttribute.BoneIndex, VertexElement.BoneIndex, 1);
		}
		else {
			glDisableVertexArrayAttrib(vao, (uint)OpenGL_ShaderInputAttribute.BoneIndex);
			glDisableVertexArrayAttrib(vao, (uint)OpenGL_ShaderInputAttribute.BoneWeights);
		}

		if ((VertexBufferFormat & VertexFormat.Normal) != 0) {
			ConfigureAttribute(bindings, OpenGL_ShaderInputAttribute.Normal, VertexElement.Normal, 1);
		}
		else {
			glDisableVertexArrayAttrib(vao, (uint)OpenGL_ShaderInputAttribute.Normal);
		}

		if ((VertexBufferFormat & VertexFormat.Color) != 0) {
			ConfigureAttribute(bindings, OpenGL_ShaderInputAttribute.Color, VertexElement.Color, 1);
		}
		else {
			glDisableVertexArrayAttrib(vao, (uint)OpenGL_ShaderInputAttribute.Color);
		}

		if ((VertexBufferFormat & VertexFormat.Specular) != 0) {
			ConfigureAttribute(bindings, OpenGL_ShaderInputAttribute.Specular, VertexElement.Specular, 1);
		}
		else {
			glDisableVertexArrayAttrib(vao, (uint)OpenGL_ShaderInputAttribute.Specular);
		}

		Span<VertexElement> texCoordElements = [VertexElement.TexCoord1D_0, VertexElement.TexCoord2D_0, VertexElement.TexCoord3D_0, VertexElement.TexCoord4D_0];
		for (int i = 0; i < IMesh.VERTEX_MAX_TEXTURE_COORDINATES; i++) {
			int texCoordSize = VertexBufferFormat.GetTexCoordDimensionSize(i);
			if (texCoordSize > 0) {
				VertexElement element = (VertexElement)((int)texCoordElements[texCoordSize - 1] + i);
				ConfigureAttribute(bindings, (OpenGL_ShaderInputAttribute)((int)OpenGL_ShaderInputAttribute.TexCoord0 + i), element, texCoordSize);
			}
			else {
				glDisableVertexArrayAttrib(vao, (uint)((int)OpenGL_ShaderInputAttribute.TexCoord0 + i));
			}
		}

		if ((VertexBufferFormat & VertexFormat.TangentS) != 0) {
			ConfigureAttribute(bindings, OpenGL_ShaderInputAttribute.TangentS, VertexElement.TangentS, 1);
		}
		else {
			glDisableVertexArrayAttrib(vao, (uint)OpenGL_ShaderInputAttribute.TangentS);
		}

		if ((VertexBufferFormat & VertexFormat.TangentT) != 0) {
			ConfigureAttribute(bindings, OpenGL_ShaderInputAttribute.TangentT, VertexElement.TangentT, 1);
		}
		else {
			glDisableVertexArrayAttrib(vao, (uint)OpenGL_ShaderInputAttribute.TangentT);
		}

		int userDataSize = VertexBufferFormat.GetUserDataSize();
		if (userDataSize > 0) {
			VertexElement element = VertexElement.UserData1 + (userDataSize - 1);
			ConfigureAttribute(bindings, OpenGL_ShaderInputAttribute.UserData, element, userDataSize);
		}
		else {
			glDisableVertexArrayAttrib(vao, (uint)OpenGL_ShaderInputAttribute.UserData);
		}

		// todo
		glDisableVertexArrayAttrib(vao, (uint)OpenGL_ShaderInputAttribute.Wrinkle);

		int vertexSize = offset;
		glVertexArrayVertexBuffer(vao, 0, (uint)vbo, 0, vertexSize);

		Assert(bindingsPtr < bindings.Length);
		for (int i = 0; i < bindingsPtr; i++) 
			glVertexArrayAttribBinding(vao, bindings[i], 0);
	}

	public int NextLockOffset() {
		int nextOffset = VertexSize == 0 ? 0 : (Position + VertexSize - 1) / VertexSize;
		nextOffset *= VertexSize;
		return nextOffset;
	}

	internal void ChangeConfiguration(VertexFormat format, int vertexSize, int totalSize) {
		VertexBufferFormat = format;
		VertexSize = vertexSize;
		VertexCount = BufferSize / vertexSize;
		RecomputeVBO();
	}

	int lastBufferSize = -1;

	public unsafe void RecomputeVBO() {
		// Create the VBO if it doesn't exist
		if (vbo == -1)
			vbo = (int)glCreateBuffer();
		// Deallocate if Sysmembuffer != null and we cant fit in what we already allocated.
		if (BufferSize > lastBufferSize) {
			if (SysmemBuffer != null) {
				NativeMemory.Free(SysmemBuffer);
				SysmemBuffer = null;
			}
			lastBufferSize = BufferSize;
			SysmemBuffer = NativeMemory.AllocZeroed((nuint)BufferSize);
			glNamedBufferData((uint)vbo, BufferSize, null, Dynamic ? GL_DYNAMIC_DRAW : GL_STATIC_DRAW);
		}

		RecomputeVAO();
	}

	public byte* Lock(int numVerts, out int baseVertexIndex) {
		Assert(!Locked);

		if (numVerts > VertexCount) {
			baseVertexIndex = 0;
			return null;
		}
		if (Dynamic) {
			if (Flush || !HasEnoughRoom(numVerts)) {
				if (SysmemBuffer != null)
					LateCreateShouldDiscard = true;

				Flush = false;
				Position = 0;
			}
		}
		else {
			Position = 0;
		}
		baseVertexIndex = VertexSize == 0 ? 0 : (Position / VertexSize);
		if (SysmemBuffer == null) {
			RecomputeVBO();
		}
		Locked = true;
		return (byte*)((nint)SysmemBuffer + Position);
	}

	public void Unlock(int vertexCount) {
		if (!Locked)
			return;

		int lockOffset = NextLockOffset();
		int bufferSize = vertexCount * VertexSize;

		glNamedBufferSubData((uint)vbo, Position, bufferSize, (void*)((nint)SysmemBuffer + Position));
		Position = lockOffset + bufferSize;
		Locked = false;
	}

	internal bool HasEnoughRoom(int numVertices) {
		return NextLockOffset() + (numVertices * VertexSize) <= BufferSize;
	}

	unsafe static nint dummyData = (nint)NativeMemory.AlignedAlloc(512, 16);

	public static unsafe void ComputeVertexDescription(byte* vertexMemory, VertexFormat vertexFormat, ref VertexDesc desc) {
		desc.NumBoneWeights = vertexFormat.GetBoneWeightsSize();
		fixed (VertexDesc* descPtr = &desc) {
			nint offset = 0;
			nint baseptr = (nint)vertexMemory;
			int** vertexSizesToSet = stackalloc int*[64];
			int vertexSizesToSetPtr = 0;

			if ((vertexFormat & VertexFormat.Position) != 0) {
				descPtr->Position = (float*)(baseptr + offset);
				offset += VertexElement.Position.GetSize();
				vertexSizesToSet[vertexSizesToSetPtr++] = &descPtr->PositionSize;
			}
			else {
				descPtr->Position = (float*)dummyData;
				descPtr->PositionSize = 0;
			}

			if ((vertexFormat & VertexFormat.BoneIndex) != 0) {
				if (desc.NumBoneWeights > 0) {
					VertexElement boneWeightElement = VertexElement.BoneWeights1 + (desc.NumBoneWeights - 1);
					descPtr->BoneWeight = (float*)(baseptr + offset);
					offset += boneWeightElement.GetSize();
					vertexSizesToSet[vertexSizesToSetPtr++] = &descPtr->BoneWeightSize;
				}
				else {
					descPtr->BoneWeight = (float*)dummyData;
					descPtr->BoneWeightSize = 0;
				}

				descPtr->BoneMatrixIndex = (byte*)(baseptr + offset);
				offset += VertexElement.BoneIndex.GetSize();
				vertexSizesToSet[vertexSizesToSetPtr++] = &descPtr->BoneMatrixIndexSize;
			}
			else {
				descPtr->BoneMatrixIndex = (byte*)dummyData;
				descPtr->BoneMatrixIndexSize = 0;
			}

			if ((vertexFormat & VertexFormat.Normal) != 0) {
				descPtr->Normal = (float*)(baseptr + offset);
				offset += VertexElement.Normal.GetSize();
				vertexSizesToSet[vertexSizesToSetPtr++] = &descPtr->NormalSize;
			}
			else {
				descPtr->Normal = (float*)dummyData;
				descPtr->NormalSize = 0;
			}

			if ((vertexFormat & VertexFormat.Color) != 0) {
				descPtr->Color = (byte*)(baseptr + offset);
				offset += VertexElement.Color.GetSize();
				vertexSizesToSet[vertexSizesToSetPtr++] = &descPtr->ColorSize;
			}
			else {
				descPtr->Color = (byte*)dummyData;
				descPtr->ColorSize = 0;
			}

			if ((vertexFormat & VertexFormat.Specular) != 0) {
				descPtr->Specular = (byte*)(baseptr + offset);
				offset += VertexElement.Specular.GetSize();
				vertexSizesToSet[vertexSizesToSetPtr++] = &descPtr->SpecularSize;
			}
			else {
				descPtr->Specular = (byte*)dummyData;
				descPtr->SpecularSize = 0;
			}

			Span<VertexElement> texCoordElements = [VertexElement.TexCoord1D_0, VertexElement.TexCoord2D_0, VertexElement.TexCoord3D_0, VertexElement.TexCoord4D_0];
			for (int i = 0; i < IMesh.VERTEX_MAX_TEXTURE_COORDINATES; i++) {
				int size = (int)vertexFormat.GetTexCoordDimensionSize(i);
				if(size != 0) {
					desc.SetTexCoord(i, (float*)(baseptr + offset));
					offset += ((VertexElement)((int)texCoordElements[size - 1] + i)).GetSize();
					vertexSizesToSet[vertexSizesToSetPtr++] = &descPtr->TexCoordSize[i];
				}
				else {
					desc.SetTexCoord(i, (float*)dummyData);
					desc.TexCoordSize[i] = 0;
				}
			}

			if ((vertexFormat & VertexFormat.TangentS) != 0) {
				descPtr->TangentS = (float*)(baseptr + offset);
				offset += VertexElement.TangentS.GetSize();
				vertexSizesToSet[vertexSizesToSetPtr++] = &descPtr->TangentSSize;
			}
			else {
				descPtr->TangentS = (float*)dummyData;
				descPtr->TangentSSize = 0;
			}

			if ((vertexFormat & VertexFormat.TangentT) != 0) {
				descPtr->TangentT = (float*)(baseptr + offset);
				offset += VertexElement.TangentT.GetSize();
				vertexSizesToSet[vertexSizesToSetPtr++] = &descPtr->TangentTSize;
			}
			else {
				descPtr->TangentT = (float*)dummyData;
				descPtr->TangentTSize = 0;
			}

			int userDataSize = (int)vertexFormat.GetUserDataSize();
			if(userDataSize > 0) {
				desc.UserData = (float*)(baseptr + offset);
				offset += (VertexElement.UserData1 + (userDataSize - 1)).GetSize();
				vertexSizesToSet[vertexSizesToSetPtr++] = &descPtr->UserDataSize;
			}
			else {
				descPtr->UserData = (float*)dummyData;
				descPtr->UserDataSize = 0;
			}

			desc.ActualVertexSize = (int)offset;
			for (int i = 0; i < vertexSizesToSetPtr; i++) {
				*vertexSizesToSet[i] = (int)offset;
			}
		}
	}

	public void Dispose() {
		if (vbo != -1) {
			Assert(SysmemBuffer != null);
			fixed (int* ugh = &vbo)
				glDeleteBuffers(1, (uint*)ugh);
			vbo = -1;
			SysmemBuffer = null;
		}
	}

	internal void HandleLateCreation() {

	}
}
