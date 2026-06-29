using Source.Common.MaterialSystem;

namespace Source.ShaderAPI.Gl46;

public unsafe class BufferedMeshGl46 : MeshGl46
{
	MeshGl46? Mesh;
	ushort LastIndex;
	ushort ExtraIndices;
	bool IsFlushing;
	bool WasRendered = true;
	bool FlushNeeded;

	public void ResetRendered() {
		WasRendered = false;
	}
	public bool WasNotRendered() => !WasRendered;

	public void Flush() {
		if (Mesh != null && !IsFlushing && FlushNeeded) {
			IsFlushing = true;
			((IMesh)Mesh!)!.Draw();
			IsFlushing = false;
			FlushNeeded = false;
		}
	}

	public void SetMesh(MeshGl46? mesh) {
		if (Mesh != mesh) {
			ShaderAPI.FlushBufferedPrimitives();
			Mesh = mesh;
		}
	}

	public override void SetMaterial(IMaterialInternal matInternal) {
		Assert(Mesh != null);
		Mesh.SetMaterial(matInternal);
	}
	public override void SetVertexFormat(VertexFormat fmt) {
		Assert(Mesh != null);
		if (Mesh.NeedsVertexFormatReset(fmt)) {
			ShaderAPI.FlushBufferedPrimitives();
			Mesh.SetVertexFormat(fmt);
		}
	}

	public override MaterialPrimitiveType GetPrimitiveType() {
		return Mesh!.GetPrimitiveType();
	}
	public override void SetPrimitiveType(MaterialPrimitiveType type) {
		if (type != GetPrimitiveType()) {
			ShaderAPI.FlushBufferedPrimitives();
			Mesh!.SetPrimitiveType(type);
		}
	}

	public override void LockMesh(int vertexCount, int indexCount, ref MeshDesc desc) {
		ShaderUtil.SyncMatrices();

		Assert(Mesh != null);
		Assert(WasRendered);

		Mesh.PreLock();

		if (!Mesh.HasEnoughRoom(vertexCount, indexCount)) {
			// Msg("Not enough room! Flushing!\n");
			ShaderAPI.FlushBufferedPrimitives();
		}

		WasRendered = false;

		Mesh.LockMesh(vertexCount, indexCount, ref desc);
	}

	public override void UnlockMesh(int vertexCount, int indexCount, ref MeshDesc desc) {
		if (Mesh!.GetPrimitiveType() == MaterialPrimitiveType.TriangleStrip && desc.Index.IndexSize > 0) {
			if (ExtraIndices > 0)
				*(desc.Index.Indices - 1) = *desc.Index.Indices;

			LastIndex = desc.Index.Indices[indexCount - 1];
			indexCount += ExtraIndices;
		}

		Mesh.UnlockMesh(vertexCount, indexCount, ref desc);
	}

	public override void Draw(int firstIndex = -1, int indexCount = 0) {
		if (!ShaderUtil.OnDrawMesh(this, firstIndex, indexCount)) {
			WasRendered = true;
			MarkAsDrawn();
			return;
		}

		Assert(!IsFlushing && !WasRendered);
		Assert(firstIndex == -1 && indexCount == 0);

		WasRendered = true;
		FlushNeeded = true;
		// ShaderAPI.FlushBufferedPrimitives(); is sometimes called here... figure out why later?
	}
}
