using Source.Common.Filesystem;
using Source.Common.Formats.Keyvalues;
using Source.Common.MaterialSystem;

using System.Drawing;
using System.Numerics;

using static Source.Common.Client.EngineSprite_Deps;
namespace Source.Common.Client;

public enum SpriteType
{
	VPParallelUpright,
	FacingUpright,
	VPParallel,
	Oriented,
	VPParallelOriented
}

[EngineComponent]
public static class EngineSprite_Deps
{
	[Dependency] public static IFileSystem fileSystem = null!;
	[Dependency] public static IMaterialSystem materialSystem = null!;
}
public class EngineSprite
{
	public int GetWidth() => Width;
	public int GetHeight() => Height;
	public int GetNumFrames() => NumFrames;
	public IMaterial? GetMaterial(RenderMode nRenderMode) => Material[(int)nRenderMode];
	static TokenCache spriteOrientationCache;
	static TokenCache spriteOriginCache;
	static TokenCache frameCache;
	public IMaterial? GetMaterial(RenderMode nRenderMode, int nFrame) {
		if (nRenderMode == RenderMode.None || nRenderMode == RenderMode.Environmental)
			return null;

		// todo: video

		IMaterial? pMaterial = Material[(int)nRenderMode];
		IMaterialVar? pFrameVar = pMaterial?.FindVarFast("$frame", ref frameCache);
		pFrameVar?.SetIntValue(nFrame);

		return pMaterial;
	}
	public void SetFrame(RenderMode nRenderMode, int nFrame) {

	}
	public bool Init(ReadOnlySpan<char> name) {
		for (int i = 0; i < (int)RenderMode.Count; ++i)
			Material[i] = null;

		Width = Height = NumFrames = 1;

		Span<char> pTemp = stackalloc char[MAX_PATH];
		Span<char> pMaterialName = stackalloc char[MAX_PATH];
		Span<char> pMaterialPath = stackalloc char[MAX_PATH];
		StrTools.StripExtension(name, pTemp);
		StrTools.StrLower(pTemp);
		StrTools.FixSlashes(pTemp);

		bool bIsUNC = pTemp[0] == '/' && pTemp[1] == '/' && pTemp[2] != '/';
		if (!bIsUNC) {
			"materials/".CopyTo(pMaterialName);
			StrTools.StrConcat(pMaterialName, pTemp, StrTools.COPY_ALL_CHARACTERS);
		}
		else 
			pTemp.CopyTo(pMaterialName);
		pMaterialName.CopyTo(pMaterialPath);
		StrTools.SetExtension(pMaterialPath, ".vmt");

		KeyValues kv = new KeyValues("vmt");
		if (!kv.LoadFromFile(fileSystem, pMaterialPath, "GAME")) {
			Warning($"Unable to load sprite material {pMaterialPath}!\n");
			return false;
		}

		for (RenderMode i = 0; i < RenderMode.Count; ++i) {
			if (i == RenderMode.None || i == RenderMode.Environmental) {
				Material[(int)i] = null;
				continue;
			}

			sprintf(pMaterialPath, "%s_rendermode_%d").S(pMaterialName).D((int)i);
			KeyValues pMaterialKV = kv.MakeCopy();
			pMaterialKV.SetInt("$spriteRenderMode", (int)i);
			Material[(int)i] = materialSystem.FindProceduralMaterial(pMaterialPath, TEXTURE_GROUP_CLIENT_EFFECTS, pMaterialKV);
			Material[(int)i]!.IncrementReferenceCount();
		}

		Width = (int)Material[0]!.GetMappingWidth();
		Height = (int)Material[0]!.GetMappingHeight();
		NumFrames = Material[0]!.GetNumAnimationFrames();

		for (RenderMode i = 0; i < RenderMode.Count; ++i) {
			if (i == RenderMode.None || i == RenderMode.Environmental)
				continue;

			if (Material[(int)i] == null)
				return false;
		}

		IMaterialVar? orientationVar = Material[0].FindVarFast("$spriteorientation", ref spriteOrientationCache);
		Orientation = orientationVar != null ? (SpriteType)orientationVar.GetIntValue() : SpriteType.VPParallelUpright;

		IMaterialVar? originVar = Material[0]!.FindVarFast("$spriteorigin", ref spriteOriginCache);
		Vector3 origin = default, originVarValue = default;
		if (originVar == null || (originVar.GetVarType() != MaterialVarType.Vector)) {
			origin[0] = -Width * 0.5f;
			origin[1] = Height * 0.5f;
		}
		else {
			originVar.GetVecValue(out originVarValue);
			origin[0] = -Width * originVarValue[0];
			origin[1] = Height * originVarValue[1];
		}

		Up = origin[1];
		Down = origin[1] - Height;
		Left = origin[0];
		Right = Width + origin[0];

		return true;
	}

	public void Shutdown() {

	}
	public void UnloadMaterial() {

	}
	public void SetColor(float r, float g, float b) {

	}
	public SpriteType GetOrientation() => Orientation;
	public void GetHUDSpriteColor(Span<float> color) {

	}
	public float GetUp() => Up;
	public float GetDown() => Down;
	public float GetLeft() => Left;
	public float GetRight() => Right;
	public void DrawFrame(RenderMode nRenderMode, int frame, int x, int y, ReadOnlySpan<Rectangle> prcSubRect ){
		throw new NotImplementedException();
	}
	public void DrawFrameOfSize(RenderMode nRenderMode, int frame, int x, int y, int iWidth, int iHeight, ReadOnlySpan<Rectangle> prcSubRect) {
		throw new NotImplementedException();
	}
	public bool IsVideo() => false;
	public void GetTexCoordRange(out float pMinU, out float pMinV, out float pMaxU, out float pMaxV) {
		throw new NotImplementedException();
	}

	// IVideoMaterial later
	private int Width;
	private int Height;
	private int NumFrames;
	private readonly IMaterial?[] Material = new IMaterial?[(int)RenderMode.Count];
	private SpriteType Orientation;
	private readonly float[] HudSpriteColor = new float[3];
	private float Up, Down, Left, Right;
}
