using Source.Common.MaterialSystem;

namespace Source.MaterialSystem;
public interface ITextureInternal : ITexture
{
	public static string NormalizeTextureName(ReadOnlySpan<char> name) {

		return Path.ChangeExtension(new(name), null); // todo.
	}

	void Bind(Sampler sampler, int frame);
	int GetTextureHandle(int v);
	void OnRestore();
	void Precache();
	bool SetRenderTarget(int rt, ITexture? depthTexture = null);
}
