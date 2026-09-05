using Source.MaterialSystem;

namespace Source.Common.MaterialSystem;

public interface ITextureManager
{
	void Init();
	ITextureInternal CreateFileTexture(ReadOnlySpan<char> fileName, ReadOnlySpan<char> textureGroupName);
	ITextureInternal ErrorTexture();
	ITextureInternal SignedNormalizationCubemap();
}