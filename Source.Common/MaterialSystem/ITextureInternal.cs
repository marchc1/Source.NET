using Source.Common.MaterialSystem;

using System.Numerics;

namespace Source.MaterialSystem;

public interface ITextureInternal : ITexture
{
	public static string NormalizeTextureName(ReadOnlySpan<char> name) {

		return Path.ChangeExtension(new(name.SliceNullTerminatedString()), null); // todo.
	}

	void Bind(Sampler sampler, int frame);
	int GetTextureHandle(int v);
	void OnRestore();
	void Precache();
	bool SetRenderTarget(int rt, ITexture? depthTexture = null);
	void GetReflectivity(out Vector3 reflectivity);
}
