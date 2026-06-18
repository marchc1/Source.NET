using Source.Common.Bitmap;
using Source.Common.MaterialSystem;

namespace Source.MaterialSystem;

public interface ITextureInternal : ITexture
{
	public static string NormalizeTextureName(ReadOnlySpan<char> name) {

		return Path.ChangeExtension(new(name), null); // todo.
	}

	void Bind(Sampler sampler, int frame);
	void UpdateSubTextureDirect(int x, int y, int w, int h, ImageFormat format, int strideBytes, Span<byte> bits);
	int GetTextureHandle(int v);
	void OnRestore();
	void Precache();
	bool SetRenderTarget(int rt, ITexture? depthTexture = null);
}
