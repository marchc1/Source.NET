using Source.Common;
using Source.Common.Bitmap;
using Source.Common.MaterialSystem;
using Source.Common.MaterialSystem.TextureRegenerators;
using Source.Common.Utilities;
namespace Source.MaterialSystem;

public class TextureManager : ITextureManager
{
	public MaterialSystem MaterialSystem;
	public ITextureInternal CreateFileTexture(ReadOnlySpan<char> fileName, ReadOnlySpan<char> textureGroupName) {
		Texture tex = new(MaterialSystem);
		tex.InitFileTexture(fileName, textureGroupName);
		return tex;
	}


	private ITextureInternal? CreateRenderTarget(ReadOnlySpan<char> rtName, int w, int h, RenderTargetSizeMode sizeMode, ImageFormat format, RenderTargetType type, TextureFlags textureFlags, CreateRenderTargetFlags renderTargetFlags) {
		Texture tex = new(MaterialSystem);
		tex.InitRenderTarget(rtName, w, h, sizeMode, format, type, textureFlags, renderTargetFlags);
		return tex;
	}

	public ITextureInternal ErrorTexture() {
		return errorTexture;
	}

	private ITextureInternal errorTexture;
	private ITextureInternal whiteTexture;

	const int ERROR_TEXTURE_SIZE = 32;
	const int WHITE_TEXTURE_SIZE = 1;
	const int BLACK_TEXTURE_SIZE = 1;
	const int GREY_TEXTURE_SIZE = 1;

	public void Init() {
		Color color = new();
		Color color2 = new();


		errorTexture = CreateProceduralTexture("error", TEXTURE_GROUP_OTHER, ERROR_TEXTURE_SIZE, ERROR_TEXTURE_SIZE, 1, ImageFormat.RGB888, TextureFlags.NoMip | TextureFlags.SingleCopy)!;
		color.R = color.G = color.B = 0; color.A = 128;
		color2.R = color2.B = color2.A = 255; color2.G = 0;
		ErrorRegen = new CheckerboardTexture(4, color, color2);
		errorTexture.SetTextureRegenerator(ErrorRegen);

		whiteTexture = CreateProceduralTexture("white", TEXTURE_GROUP_OTHER, WHITE_TEXTURE_SIZE, WHITE_TEXTURE_SIZE, 1, ImageFormat.RGB888, TextureFlags.NoMip | TextureFlags.SingleCopy)!;
		color.R = color.G = color.B = color.A = 255;
		CreateSolidTexture(whiteTexture, color);


	}

	private void CreateCheckerboardTexture(ITexture errorTexture, int checkerSize, Color color1, Color color2)
		=> errorTexture.SetTextureRegenerator(new CheckerboardTexture(checkerSize, color1, color2));

	private void CreateSolidTexture(ITexture tex, Color color) 
		=> tex.SetTextureRegenerator(new SolidTexture(color));

	public ITextureInternal? CreateProceduralTexture(ReadOnlySpan<char> name, ReadOnlySpan<char> textureGroup, int w, int h, int d, ImageFormat imageFormat, TextureFlags flags, ITextureRegenerator? generator = null) {
		name = name.SliceNullTerminatedString();
		Texture newTexture = new(MaterialSystem);
		newTexture.InitProceduralTexture(name, textureGroup, w, h, d, imageFormat, flags, generator);
		if (newTexture == null)
			return null;

		TextureList[newTexture.GetName().Hash()] = newTexture;
		((ITextureInternal)newTexture).Download();

		return newTexture;
	}

	Dictionary<ulong, ITextureInternal> TextureList = [];

	public ITextureInternal? FindOrLoadTexture(ReadOnlySpan<char> textureName, ReadOnlySpan<char> textureGroupName, int additionalCreationFlags) {
		ITextureInternal? texture = FindTexture(textureName);
		if (texture == null) {
			texture = LoadTexture(textureName, textureGroupName, additionalCreationFlags);
			if (texture != null) 
				TextureList.Add(texture.GetName().Hash(), texture);
		}

		return texture;
	}

	public ITextureInternal? LoadTexture(ReadOnlySpan<char> textureName, ReadOnlySpan<char> textureGroupName, int additionalCreationFlags, bool download = true) {
		ITextureInternal? newTexture = CreateFileTexture(textureName, textureGroupName);
		if(newTexture != null) {
			if (download)
				newTexture.Download();
		}

		return newTexture;
	}

	public void RestoreTexture(ITextureInternal texture) {
		texture.OnRestore();
		texture.Download();
	}

	public ITextureInternal? FindTexture(ReadOnlySpan<char> textureName) {
		if (TextureList.TryGetValue(textureName.Hash(), out ITextureInternal? tex))
			return tex;

		return null;
	}

	internal void RestoreRenderTargets() {
		foreach (var tex in TextureList)
			if (tex.Value.IsRenderTarget())
				RestoreTexture(tex.Value);
	}

	internal void RestoreNonRenderTargetTextures() {
		foreach(var tex in TextureList) 
			if (!tex.Value.IsRenderTarget())
				RestoreTexture(tex.Value);
	}

	internal void AllocateStandardRenderTargets() {
		MaterialSystem.BeginRenderTargetAllocation();
		MaterialSystem.EndRenderTargetAllocation();
	}

	internal void FreeStandardRenderTargets() {

	}

	CheckerboardTexture ErrorRegen;

	internal void GenerateErrorTexture(Texture texture, IVTFTexture vtfTexture) {
		ErrorRegen.RegenerateTextureBits(texture, vtfTexture, default);
	}

	internal ITextureInternal? CreateRenderTargetTexture(ReadOnlySpan<char> rtName, int w, int h, RenderTargetSizeMode sizeMode, ImageFormat format, RenderTargetType type, TextureFlags textureFlags, CreateRenderTargetFlags renderTargetFlags) {
		ITextureInternal? texture;
		if(!rtName.IsEmpty) {
			texture = FindTexture(rtName);
			if(texture != null) {
				((Texture)texture)!.InitRenderTarget(texture.GetName(), w, h, sizeMode, format, type, textureFlags, renderTargetFlags);
				texture.Download();
				return texture;
			}
		}

		texture = CreateRenderTarget(rtName, w, h, sizeMode, format, type, textureFlags, renderTargetFlags);
		if (texture == null)
			return null;

		TextureList[new UtlSymbol(rtName)] = texture;
		texture.Download();
		return texture;
	}
}
