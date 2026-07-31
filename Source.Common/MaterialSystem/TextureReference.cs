using Source.Common.Bitmap;
using Source.Common.Utilities;

namespace Source.Common.MaterialSystem;

public class TextureReference : Reference<ITexture>
{
	readonly IMaterialSystem materials = Singleton<IMaterialSystem>();

	public void Init(ReadOnlySpan<char> texture, ReadOnlySpan<char> textureGroupName, bool complain = true) {
		Shutdown();

		reference = materials.FindTexture(texture, textureGroupName, complain);
	}

	public void InitProceduralTexture(ReadOnlySpan<char> textureName, ReadOnlySpan<char> textureGroupName, int w, int h, ImageFormat format, TextureFlags flags) {
		Shutdown();

		reference = materials.CreateProceduralTexture(textureName, textureGroupName, w, h, format, flags);
	}

	public void InitRenderTarget(int w, int h, RenderTargetSizeMode sizeMod, ImageFormat format, MaterialRenderTargetDepth depth, bool hdr, ReadOnlySpan<char> optionalName = default) {
		Shutdown();

		TextureFlags textureFlags = TextureFlags.ClampS | TextureFlags.ClampT;
		if (depth == MaterialRenderTargetDepth.Only)
			textureFlags |= TextureFlags.PointSample;

		CreateRenderTargetFlags renderTargetFlags = hdr ? CreateRenderTargetFlags.HDR : 0;

		reference = materials.CreateNamedRenderTargetTextureEx(optionalName, w, h, sizeMod, format, depth, textureFlags, renderTargetFlags);

		Assert(reference != null);
	}

	public void Init(ITexture texture) {
		Shutdown();

		reference = texture;
		reference?.IncrementReferenceCount();
	}

	public void Shutdown(bool deleteIfUnreferenced = false) {
		if (reference != null && materials != null) {
			reference.DecrementReferenceCount();
			if (deleteIfUnreferenced)
				reference.DeleteIfUnreferenced();
			reference = null;
		}
	}
}
