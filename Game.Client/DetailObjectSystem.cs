using static Game.Client.DetailObjectSystemGlobals;

using CommunityToolkit.HighPerformance;

using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Formats.BSP;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;

using System.Numerics;

namespace Game.Client;

static class DetailObjectSystemGlobals
{
	public static ConVar cl_detaildist = new("cl_detaildist", "1200", 0, "Distance at which detail props are no longer visible");
	public static ConVar cl_detailfade = new("cl_detailfade", "400", 0, "Distance across which detail props fade in");
	public static ConVar cl_detail_max_sway = new("cl_detail_max_sway", "0", FCvar.Archive, "Amplitude of the detail prop sway");
	public static ConVar cl_detail_avoid_radius = new("cl_detail_avoid_radius", "0", FCvar.Archive, "radius around detail sprite to avoid players");
	public static ConVar cl_detail_avoid_force = new("cl_detail_avoid_force", "0", FCvar.Archive, "force with which to avoid players ( in units, percentage of the width of the detail sprite )");
	public static ConVar cl_detail_avoid_recover_speed = new("cl_detail_avoid_recover_speed", "0", FCvar.Archive, "how fast to recover position after avoiding players");
	public static ConVar cl_detail_multiplier = new("cl_detail_multiplier", "1", FCvar.Cheat, "extra details to create");
	public static ConVar cl_fastdetailsprites = new("cl_fastdetailsprites", "1", FCvar.Cheat, "whether to use new detail sprite system");
}

public interface IDetailObjectSystem : IGameSystem
{
	IClientRenderable? GetDetailModel(int idx);

	void BuildDetailObjectRenderLists(in Vector3 viewOrigin);

	void RenderOpaqueDetailObjects(int leafCount, Span<LeafIndex_t> leafList);

	void BeginTranslucentDetailRendering();

	void RenderTranslucentDetailObjects(in Vector3 viewOrigin, in Vector3 viewForward, in Vector3 viewRight, in Vector3 viewUp, int leafCount, Span<LeafIndex_t> leafList);

	void RenderTranslucentDetailObjectsInLeaf(in Vector3 viewOrigin, in Vector3 viewForward, in Vector3 viewRight, in Vector3 viewUp, int leaf, Vector3? closestPoint);
}


public struct DetailModelAdvInfo
{
	public InlineArray3<Vector3> AnglesForward;
	public InlineArray3<Vector3> AnglesRight;
	public InlineArray3<Vector3> AnglesUp;

	public Vector3 CurrentAvoid;

	public float SwayYaw;

	public float ShapeSize;

	public int ShapeAngle;
	public float SwayAmount;
}

public struct DetailObjectSystemPerLeafData()
{
	public ushort FirstDetailProp = 0;
	public ushort DetailPropCount = 0;
	public int DetailPropRenderFrame = -1;
}

public struct SprintInfo
{
	public ushort SpriteIndex;
	public Half Scale;
}

public class DetailModel : IClientUnknown, IClientRenderable
{
	public struct LightStyleInfo
	{
		public uint LightStyle;
		public uint LightStyleCount;
	}

	Vector3 Origin;
	QAngle Angles;

	ColorRGBExp32 Color;

	byte Orientation;
	byte Type;
	bool HasLightStyle;
	bool Flipped;

	byte Alpha;

	static readonly Dictionary<DetailModel, LightStyleInfo> LightStylesMap = [];

	Model? Model;
	SprintInfo SpriteInfo;

	DetailModelAdvInfo? AdvInfo;

	public DetailModel() { }

	public bool InitCommon(int index, in Vector3 org, in QAngle angles) {
		MathLib.VectorCopy(org, out Origin);
		MathLib.VectorCopy(angles, out Angles);
		Alpha = 255;

		return true;
	}

	public bool Init(int index, in Vector3 org, in QAngle angles, Model? model, ColorRGBExp32 lighting, int lightstyle, byte lightstylecount, int orientation) {
		Color = lighting;
		if (lightstylecount > 0) {
			HasLightStyle = true;
			if (lightstyle >= 0x1000000 || lightstylecount >= 100)
				Error("Light style overflow\n");
			LightStylesMap[this] = new LightStyleInfo { LightStyle = (uint)lightstyle, LightStyleCount = lightstylecount };
		}
		Orientation = (byte)orientation;
		Type = (byte)DetailPropType.Model;
		Model = model;
		return InitCommon(index, org, angles);
	}

	public bool InitSprite(int index, bool flipped, in Vector3 org, in QAngle angles, ushort spriteIndex, ColorRGBExp32 lighting, int lightstyle, byte lightstylecount, int orientation, float scale, byte type, byte shapeAngle, byte shapeSize, byte swayAmount) {
		Color = lighting;
		if (lightstylecount > 0) {
			HasLightStyle = true;
			if (lightstyle >= 0x1000000 || lightstylecount >= 100)
				Error("Light style overflow\n");
			LightStylesMap[this] = new LightStyleInfo { LightStyle = (uint)lightstyle, LightStyleCount = lightstylecount };
		}
		Orientation = (byte)orientation;
		SpriteInfo.SpriteIndex = spriteIndex;
		Type = type;
		SpriteInfo.Scale = (Half)scale;

		AdvInfo = null;
		Assert(type <= 3);
		if (type == (byte)DetailPropType.ShapeTri || type == (byte)DetailPropType.ShapeCross || swayAmount > 0) {
			Angles = angles;
			InitShapedSprite(shapeAngle, shapeSize, swayAmount);
		}

		Flipped = flipped;
		return InitCommon(index, org, angles);
	}

	public void SetAlpha(byte alpha) => Alpha = alpha;

	public IClientUnknown GetIClientUnknown() => this;
	public ICollideable? GetCollideable() => null;
	public IClientNetworkable? GetClientNetworkable() => null;
	public IClientRenderable? GetClientRenderable() => this;
	public IClientEntity? GetIClientEntity() => null;
	public IClientThinkable? GetClientThinkable() => null;

	public int GetBody() => 0;
	public ref readonly Vector3 GetRenderOrigin() => ref Origin;
	public ref readonly QAngle GetRenderAngles() => ref Angles;
	public ref readonly Matrix3x4 RenderableToWorldTransform() => throw new NotImplementedException();
	public bool ShouldDraw() => throw new NotImplementedException();
	public bool IsTwoPass() => false;
	public void OnThreadedDrawSetup() { }
	public bool IsTransparent() => throw new NotImplementedException();
	public Model? GetModel() => Model;
	public int DrawModel(StudioFlags flags) => throw new NotImplementedException();
	public void ComputeFxBlend() => throw new NotImplementedException();
	public int GetFxBlend() => throw new NotImplementedException();
	public bool SetupBones(Span<Matrix3x4> boneToWorldOut, int maxBones, int boneMask, TimeUnit_t currentTime) => throw new NotImplementedException();
	public void SetupWeights(Span<Matrix3x4> boneToWorld, Span<float> flexWeights, Span<float> flexDelayedWeights) => throw new NotImplementedException();
	public bool UsesFlexDelayedWeights() => false;
	public void DoAnimationEvents() => throw new NotImplementedException();
	public void GetRenderBounds(out Vector3 mins, out Vector3 maxs) => throw new NotImplementedException();
	public IPVSNotify? GetPVSNotifyInterface() => throw new NotImplementedException();
	public void GetRenderBoundsWorldspace(out Vector3 mins, out Vector3 maxs) => throw new NotImplementedException();
	public bool ShouldReceiveProjectedTextures(ShadowFlags flags) => throw new NotImplementedException();
	public bool GetShadowCastDistance(out float dist, ShadowType shadowType) { dist = 0; return false; }
	public bool GetShadowCastDirection(out Vector3 direction, ShadowType shadowType) { direction = default; return false; }
	public bool UsesPowerOfTwoFrameBufferTexture() => throw new NotImplementedException();
	public bool UsesFullFrameBufferTexture() => throw new NotImplementedException();
	public bool IgnoresZBuffer() => false;
	public bool LODTest() => true;
	public ClientShadowHandle_t GetShadowHandle() => throw new NotImplementedException();
	public ref ClientRenderHandle_t RenderHandle() => throw new NotImplementedException();
	public void GetShadowRenderBounds(out Vector3 mins, out Vector3 maxs, ShadowType shadowType) => throw new NotImplementedException();
	public bool IsShadowDirty() => false;
	public void MarkShadowDirty(bool dirty) { }
	public IClientRenderable? GetShadowParent() => null;
	public IClientRenderable? FirstShadowChild() => null;
	public IClientRenderable? NextShadowPeer() => null;
	public ShadowType ShadowCastType() => ShadowType.None;
	public void CreateModelInstance() { }
	public ModelInstanceHandle_t GetModelInstance() => MODEL_INSTANCE_INVALID;
	public int LookupAttachment(ReadOnlySpan<char> attachmentName) => -1;
	public bool GetAttachment(int number, out Matrix3x4 matrix) => throw new NotImplementedException();
	public bool GetAttachment(int number, out Vector3 origin, out QAngle angles) => throw new NotImplementedException();
	public Span<float> GetRenderClipPlane() => null;
	public int GetSkin() => 0;
	public void RecordToolMessage() { }

	public void GetColorModulation(Span<float> color) => throw new NotImplementedException();

	public void ComputeAngles() => throw new NotImplementedException();

	public void DrawSprite(ref MeshBuilder meshBuilder) => throw new NotImplementedException();

	public int QuadsToDraw() => QuadCount[Type];

	public void DrawTypeSprite(ref MeshBuilder meshBuilder) => throw new NotImplementedException();

	public void DrawTypeShapeCross(ref MeshBuilder meshBuilder) => throw new NotImplementedException();
	public void DrawTypeShapeTri(ref MeshBuilder meshBuilder) => throw new NotImplementedException();

	public void UpdatePlayerAvoid() => throw new NotImplementedException();

	public void InitShapedSprite(byte shapeAngle, byte shapeSize, byte swayAmount) => throw new NotImplementedException();
	public void InitShapeTri() => throw new NotImplementedException();
	public void InitShapeCross() => throw new NotImplementedException();

	public void DrawSwayingQuad(ref MeshBuilder meshBuilder, Vector3 vecOrigin, Vector3 vecSway, Vector2 texul, Vector2 texlr, Span<byte> color, Vector3 width, Vector3 height) => throw new NotImplementedException();

	public int GetDetailType() => Type;
	public byte GetAlpha() => Alpha;

	public bool IsDetailModelTranslucent() => throw new NotImplementedException();

	public void SetRefEHandle(in BaseHandle handle) => Assert(false);
	public ref readonly BaseHandle GetRefEHandle() => throw new NotImplementedException();

	static readonly int[] QuadCount = [
		0, //DETAIL_PROP_TYPE_MODEL
		1, //DETAIL_PROP_TYPE_SPRITE
		4, //DETAIL_PROP_TYPE_SHAPE_CROSS
		3 //DETAIL_PROP_TYPE_SHAPE_TRI
	];
}

public struct DetailPropSpriteDict
{
	public Vector2 UL;
	public Vector2 LR;
	public Vector2 TexUL;
	public Vector2 TexLR;
}

public struct FastSpriteX4
{
	public FourVectors Pos;
	public fltx4 HalfWidth;
	public fltx4 Height;
	public InlineArray4<InlineArray4<byte>> RGBColor;
	public InlineArray4<DetailPropSpriteDict> SpriteDefs;

	public void ReplicateFirstEntryToOthers() {
		HalfWidth = MathLib.ReplicateX4(MathLib.SubFloat(ref HalfWidth, 0));
		Height = MathLib.ReplicateX4(MathLib.SubFloat(ref Height, 0));

		for (int i = 1; i < 4; i++)
			for (int j = 0; j < 4; j++) {
				RGBColor[i][j] = RGBColor[0][j];
			}
		Pos.x = new Vector4(MathLib.SubFloat(ref Pos.x, 0));
		Pos.y = new Vector4(MathLib.SubFloat(ref Pos.y, 0));
		Pos.z = new Vector4(MathLib.SubFloat(ref Pos.z, 0));
	}
}

public struct FastSpriteQuadBuildoutBufferX4
{
	public InlineArray4<FourVectors> Coords;
	public InlineArray4<InlineArray4<byte>> RGBColor;
	public fltx4 Alpha;
	public InlineArray4<nint> SpriteDefs;
}

public struct FastSpriteQuadBuildoutBufferNonSIMDView
{
	public InlineArray4<float> X0, Y0, Z0;
	public InlineArray4<float> X1, Y1, Z1;
	public InlineArray4<float> X2, Y2, Z2;
	public InlineArray4<float> X3, Y3, Z3;

	public InlineArray4<InlineArray4<byte>> RGBColor;
	public InlineArray4<float> Alpha;
	public InlineArray4<nint> SpriteDefs;
}

public class FastDetailLeafSpriteList : ClientLeafSubSystemData
{
	internal int NumSprites;
	internal int NumSIMDSprites;
	internal FastSpriteX4[]? Sprites;
	internal int StartSIMDSprite;

	internal int NumPendingSprites;
	internal int StartSpriteIndex;

	public FastDetailLeafSpriteList() {
		NumPendingSprites = 0;
		StartSpriteIndex = 0;
	}
}

public class DetailObjectSystem : IDetailObjectSystem, ISpatialLeafEnumerator
{
	struct DetailModelDict
	{
		public Model? Model;
	}

	struct EnumContext
	{
		public Vector3 ViewOrigin;
		public int BuildWorldListNumber;
	}

	struct SortInfo
	{
		public int Index;
		public float Distance;
	}

	readonly List<DetailModelDict> DetailObjectDict = new(32);
	readonly List<DetailModel> DetailObjects = [];
	readonly List<DetailPropSpriteDict> DetailSpriteDictList = new(32);
	readonly List<DetailPropSpriteDict> DetailSpriteDictFlipped = new(32);
	readonly List<DetailPropLightstylesLump> DetailLightingList = [];
	FastSpriteX4[]? FastSpriteData;

	MaterialReference DetailSpriteMaterial = new();
	MaterialReference DetailWireframeMaterial = new();

	int SpriteCount;
	int FirstSprite;
	int SortedLeaf;
	int SortedFastLeaf;
	SortInfo[]? SortInfos;
	SortInfo[]? FastSortInfos;
	FastSpriteQuadBuildoutBufferX4[]? BuildoutBuffer;

	float DefaultFadeStart;
	float DefaultFadeEnd;

	float CurMaxSqDist;
	float CurFadeSqDist;
	float CurFalloffFactor;

	const string DETAIL_SPRITE_MATERIAL = "detail/detailsprites";

	static bool DetailObjectIsFastSprite(in DetailObjectLump lump) {
		return cl_fastdetailsprites.GetInt() != 0
			&& lump.Type == (byte)DetailPropType.Sprite
			&& lump.LightStyleCount == 0
			&& lump.Orientation == 2
			&& lump.ShapeAngle == 0
			&& lump.ShapeSize == 0
			&& lump.SwayAmount == 0;
	}

	static Vector3 RandomVector(float min, float max) => new(random.RandomFloat(min, max), random.RandomFloat(min, max), random.RandomFloat(min, max));

	static readonly DetailObjectSystem s_DetailObjectSystem = new();
	public static IDetailObjectSystem GetDetailObjectSystem() => s_DetailObjectSystem;

	public ReadOnlySpan<char> Name() => "DetailObjectSystem";

	public DetailObjectSystem() { }

	public bool IsPerFrame() => false;

	public bool Init() {
		DefaultFadeStart = cl_detailfade.GetFloat();
		DefaultFadeEnd = cl_detaildist.GetFloat();
		return true;
	}
	public void PostInit() { }
	public void Shutdown() { }

	public void LevelInitPreEntity() {
		DetailSpriteMaterial.Init("detail/detailsprites", MaterialDefines.TEXTURE_GROUP_OTHER);
		DetailWireframeMaterial.Init("debug/debugspritewireframe", MaterialDefines.TEXTURE_GROUP_OTHER);

		if (engine.GameLumpVersion((int)GameLump.DetailProps) < 4) {
			Warning("Map uses old detail prop file format.. ignoring detail props\n");
			return;
		}

		int size = engine.GameLumpSize((int)GameLump.DetailProps);
		byte[] fileMemory = new byte[size];
		if (engine.LoadGameLump((int)GameLump.DetailProps, fileMemory)) {
			using MemoryStream buf = new(fileMemory);
			UnserializeModelDict(buf);

			switch (engine.GameLumpVersion((int)GameLump.DetailProps)) {
				case 4:
					UnserializeDetailSprites(buf);
					UnserializeModels(buf);
					break;
			}
		}

		if (DetailObjects.Count != 0 || DetailSpriteDictList.Count != 0) {
			IMaterial pMat = DetailSpriteMaterial.Get()!;
			float ratio = (float)pMat.GetMappingWidth() / pMat.GetMappingHeight();
			if (ratio > 1.0) {
				Span<DetailPropSpriteDict> spriteDict = DetailSpriteDictList.AsSpan();
				Span<DetailPropSpriteDict> spriteDictFlipped = DetailSpriteDictFlipped.AsSpan();
				for (int i = 0; i < spriteDict.Length; i++) {
					spriteDict[i].TexUL.Y *= ratio;
					spriteDict[i].TexLR.Y *= ratio;
					spriteDictFlipped[i].TexUL.Y *= ratio;
					spriteDictFlipped[i].TexLR.Y *= ratio;
				}
			}
		}

		int detailPropLightingLump;
		if (Singleton<IMaterialSystemHardwareConfig>().GetHDRType() != HDRType.None)
			detailPropLightingLump = (int)GameLump.DetailPropLightingHDR;
		else
			detailPropLightingLump = (int)GameLump.DetailPropLighting;
		size = engine.GameLumpSize(detailPropLightingLump);

		fileMemory = new byte[size];
		if (engine.LoadGameLump(detailPropLightingLump, fileMemory)) {
			using MemoryStream buf = new(fileMemory);
			UnserializeModelLighting(buf);
		}
	}

	public void LevelInitPostEntity() {
		ReadOnlySpan<char> detailSpriteMaterial = DETAIL_SPRITE_MATERIAL;
		C_World? world = C_World.GetClientWorldEntity();
		if (world != null && !world.GetDetailSpriteMaterial().IsEmpty)
			detailSpriteMaterial = world.GetDetailSpriteMaterial();
		DetailSpriteMaterial.Init(detailSpriteMaterial, MaterialDefines.TEXTURE_GROUP_OTHER);

		if (C_EnvDetailController.GetDetailController() != null) {
			cl_detailfade.SetValue(Math.Min(DefaultFadeStart, C_EnvDetailController.GetDetailController()!.FadeStartDist));
			cl_detaildist.SetValue(Math.Min(DefaultFadeEnd, C_EnvDetailController.GetDetailController()!.FadeEndDist));
		}
		else {
			cl_detailfade.SetValue(DefaultFadeStart);
			cl_detaildist.SetValue(DefaultFadeEnd);
		}
	}
	public void LevelShutdownPreClearSteamAPIContext() { }
	public void LevelShutdownPreEntity() {
		DetailObjects.Clear();
		DetailObjectDict.Clear();
		DetailSpriteDictList.Clear();
		DetailSpriteDictFlipped.Clear();
		DetailLightingList.Clear();
		// DetailSpriteMaterial.Shutdown();
		FastSpriteData = null;
		FreeSortBuffers();
	}
	public void LevelShutdownPostEntity() {
		// DetailWireframeMaterial.Shutdown();
	}

	public void OnSave() { }
	public void OnRestore() { }
	public void SafeRemoveIfDesired() { }

	public IClientRenderable? GetDetailModel(int idx) {
		if (DetailObjects[idx].GetDetailType() != (int)DetailPropType.Model)
			return null;

		return DetailObjects[idx];
	}


	public void BuildDetailObjectRenderLists(in Vector3 viewOrigin) => throw new NotImplementedException();

	public void RenderOpaqueDetailObjects(int leafCount, Span<LeafIndex_t> leafList) => throw new NotImplementedException();

	public void RenderTranslucentDetailObjects(in Vector3 viewOrigin, in Vector3 viewForward, in Vector3 viewRight, in Vector3 viewUp, int leafCount, Span<LeafIndex_t> leafList) => throw new NotImplementedException();

	public void RenderTranslucentDetailObjectsInLeaf(in Vector3 viewOrigin, in Vector3 viewForward, in Vector3 viewRight, in Vector3 viewUp, int leaf, Vector3? closestPoint) => throw new NotImplementedException();
	public void RenderFastTranslucentDetailObjectsInLeaf(in Vector3 viewOrigin, in Vector3 viewForward, in Vector3 viewRight, in Vector3 viewUp, int leaf, Vector3? closestPoint) => throw new NotImplementedException();

	public void BeginTranslucentDetailRendering() {
		SortedLeaf = -1;
		SortedFastLeaf = -1;
		SpriteCount = FirstSprite = 0;
	}

	public bool EnumerateLeaf(int leaf, nint context) => throw new NotImplementedException();

	public ref DetailPropLightstylesLump DetailLighting(int i) => ref DetailLightingList.AsSpan()[i];
	public ref DetailPropSpriteDict DetailSpriteDict(int i) => ref DetailSpriteDictList.AsSpan()[i];

	int BuildOutSortedSprites(FastDetailLeafSpriteList data, in Vector3 viewOrigin, in Vector3 viewForward, in Vector3 viewRight, in Vector3 viewUp) => throw new NotImplementedException();

	void RenderFastSprites(in Vector3 viewOrigin, in Vector3 viewForward, in Vector3 viewRight, in Vector3 viewUp, int leafCount, ReadOnlySpan<LeafIndex_t> leafList) => throw new NotImplementedException();

	void UnserializeFastSprite(ref FastSpriteX4 spritex4, int subField, in DetailObjectLump lump, bool flipped, in Vector3 posOffset) {
		Vector3 pos = lump.Origin + posOffset;
		pos = GetSpriteMiddleBottomPosition(lump) + posOffset;

		MathLib.SubFloat(ref spritex4.Pos.x, subField) = pos.X;
		MathLib.SubFloat(ref spritex4.Pos.y, subField) = pos.Y;
		MathLib.SubFloat(ref spritex4.Pos.z, subField) = pos.Z;
		DetailPropSpriteDict sdef = DetailSpriteDictList[lump.DetailModel];

		MathLib.SubFloat(ref spritex4.HalfWidth, subField) = 0.5f * lump.Scale * (sdef.LR.X - sdef.UL.X);
		MathLib.SubFloat(ref spritex4.Height, subField) = lump.Scale * (sdef.LR.Y - sdef.UL.Y);
		if (!flipped)
			sdef = DetailSpriteDictFlipped[lump.DetailModel];

		ColorRGBExp32 rgbcolor = lump.Lighting;
		Span<float> color = [
			MathLib.TexLightToLinear(rgbcolor.R, rgbcolor.Exponent),
			MathLib.TexLightToLinear(rgbcolor.G, rgbcolor.Exponent),
			MathLib.TexLightToLinear(rgbcolor.B, rgbcolor.Exponent),
			255
		];
		engine.LinearToGamma(color, color);
		spritex4.RGBColor[subField][0] = (byte)(255.0f * color[0]);
		spritex4.RGBColor[subField][1] = (byte)(255.0f * color[1]);
		spritex4.RGBColor[subField][2] = (byte)(255.0f * color[2]);
		spritex4.RGBColor[subField][3] = 255;

		spritex4.SpriteDefs[subField] = sdef;
	}

	static void ScanForCounts(Stream buf, out int numOldStyleObjects, out int numFastSpritesToAllocate, out int maxNumOldSpritesInLeaf, out int maxNumFastSpritesInLeaf) {
		long oldpos = buf.Position;
		int count = 0;
		buf.ReadToStruct(ref count);

		int nOld = 0;
		int nFast = 0;
		int detailObjectLeaf = -1;

		int numOldInLeaf = 0;
		int numFastInLeaf = 0;
		int maxOld = 0;
		int maxFast = 0;
		while (--count >= 0) {
			DetailObjectLump lump = default;
			buf.ReadToStruct(ref lump);

			if (detailObjectLeaf != lump.Leaf) {
				nFast += (0 - nFast) & 3;
				maxFast = Math.Max(maxFast, numFastInLeaf);
				maxOld = Math.Max(maxOld, numOldInLeaf);
				numOldInLeaf = 0;
				numFastInLeaf = 0;
				detailObjectLeaf = lump.Leaf;
			}

			if (DetailObjectIsFastSprite(lump)) {
				nFast += cl_detail_multiplier.GetInt();
				numFastInLeaf += cl_detail_multiplier.GetInt();
			}
			else {
				nOld += cl_detail_multiplier.GetInt();
				numOldInLeaf += cl_detail_multiplier.GetInt();
			}
		}

		nFast += (0 - nFast) & 3;
		maxFast = Math.Max(maxFast, numFastInLeaf);
		maxOld = Math.Max(maxOld, numOldInLeaf);

		buf.Position = oldpos;
		numFastSpritesToAllocate = nFast;
		numOldStyleObjects = nOld;
		maxFast = (3 + maxFast) & ~3;
		maxNumOldSpritesInLeaf = maxOld;
		maxNumFastSpritesInLeaf = maxFast;
	}

	void UnserializeModelDict(Stream buf) {
		int count = 0;
		buf.ReadToStruct(ref count);
		DetailObjectDict.EnsureCapacity(count);
		while (--count >= 0) {
			DetailObjectDictLump lump = default;
			buf.ReadToStruct(ref lump);

			Span<byte> nameBytes = lump.Name;
			int len = nameBytes.IndexOf((byte)0);
			if (len < 0)
				len = nameBytes.Length;
			string name = System.Text.Encoding.ASCII.GetString(nameBytes[..len]);

			DetailModelDict dict = default;
			dict.Model = engine.LoadModel(name, true);

			if (modelinfo.IsModelVertexLit(dict.Model)) {
				Warning($"Detail prop model {name} is using vertex-lit materials!\nIt must use unlit materials!\n");
				dict.Model = engine.LoadModel("models/error.mdl");
			}

			DetailObjectDict.Add(dict);
		}

#if DEBUG
		DevMsg($"UnserializeModelDict: {DetailObjectDict.Count} detail prop models\n");
#endif
	}

	void UnserializeDetailSprites(Stream buf) {
		int count = 0;
		buf.ReadToStruct(ref count);
		DetailSpriteDictList.EnsureCapacity(count);
		DetailSpriteDictFlipped.EnsureCapacity(count);
		while (--count >= 0) {
			DetailPropSpriteDict dict = default;
			buf.ReadToStruct(ref dict);
			DetailSpriteDictList.Add(dict);

			DetailPropSpriteDict flipped = dict;
			(flipped.TexLR.X, flipped.TexUL.X) = (flipped.TexUL.X, flipped.TexLR.X);
			DetailSpriteDictFlipped.Add(flipped);
		}

#if DEBUG
		DevMsg($"UnserializeDetailSprites: {DetailSpriteDictList.Count} detail sprites\n");
#endif
	}

	void UnserializeModels(Stream buf) {
		int firstDetailObject = 0;
		int detailObjectCount = 0;
		int detailObjectLeaf = -1;

		ScanForCounts(buf, out int numOldStyleObjects, out int numFastSpritesToAllocate, out int maxOldInLeaf, out int maxFastInLeaf);

		FreeSortBuffers();

		if (maxOldInLeaf != 0)
			SortInfos = new SortInfo[3 + maxOldInLeaf];
		if (maxFastInLeaf != 0) {
			FastSortInfos = new SortInfo[3 + maxFastInLeaf];
			BuildoutBuffer = new FastSpriteQuadBuildoutBufferX4[1 + maxFastInLeaf / 4];
		}

		if (numFastSpritesToAllocate != 0) {
			Assert((numFastSpritesToAllocate & 3) == 0);
			Assert(FastSpriteData == null);
			FastSpriteData = new FastSpriteX4[numFastSpritesToAllocate >> 2];
		}

		DetailObjects.EnsureCapacity(numOldStyleObjects);

		int count = 0;
		buf.ReadToStruct(ref count);

		int curFastObject = 0;
		int numFastObjectsInCurLeaf = 0;
		int curFastSpriteOut = 0;

		bool flipped = true;
		while (--count >= 0) {
			flipped = !flipped;
			DetailObjectLump lump = default;
			buf.ReadToStruct(ref lump);

			if (detailObjectLeaf != lump.Leaf) {
				if (detailObjectLeaf != -1) {
					if (numFastObjectsInCurLeaf != 0) {
						FastDetailLeafSpriteList newList = new() {
							NumSprites = numFastObjectsInCurLeaf,
							NumSIMDSprites = (3 + numFastObjectsInCurLeaf) >> 2,
							Sprites = FastSpriteData,
							StartSIMDSprite = curFastSpriteOut
						};
						curFastSpriteOut += newList.NumSIMDSprites;
						clientLeafSystem.SetSubSystemDataInLeaf(detailObjectLeaf, ClientLeafSystem.CLSUBSYSTEM_DETAILOBJECTS, newList);
						curFastObject += (0 - curFastObject) & 3;
						numFastObjectsInCurLeaf = 0;
					}
					clientLeafSystem.SetDetailObjectsInLeaf(detailObjectLeaf, firstDetailObject, detailObjectCount);
				}

				detailObjectLeaf = lump.Leaf;
				firstDetailObject = DetailObjects.Count;
				detailObjectCount = 0;
			}

			if (DetailObjectIsFastSprite(lump)) {
				for (int i = 0; i < cl_detail_multiplier.GetInt(); i++) {
					int subField = curFastObject & 3;
					Vector3 pos = new(0, 0, 0);
					if (i != 0) {
						pos += RandomVector(-50, 50);
						pos.Z = 0;
					}
					UnserializeFastSprite(ref FastSpriteData![curFastObject >> 2], subField, lump, flipped, pos);
					if (subField == 0)
						FastSpriteData![curFastObject >> 2].ReplicateFirstEntryToOthers();
					curFastObject++;
					numFastObjectsInCurLeaf++;
				}
			}
			else {
				switch ((DetailPropType)lump.Type) {
					case DetailPropType.Model: {
							int newObj = DetailObjects.Count;
							DetailModel obj = new();
							DetailObjects.Add(obj);
							obj.Init(newObj, lump.Origin, lump.Angles, DetailObjectDict[lump.DetailModel].Model, lump.Lighting, (int)lump.LightStyles, lump.LightStyleCount, lump.Orientation);
							++detailObjectCount;
						}
						break;

					case DetailPropType.Sprite:
					case DetailPropType.ShapeCross:
					case DetailPropType.ShapeTri: {
							for (int i = 0; i < cl_detail_multiplier.GetInt(); i++) {
								Vector3 pos = lump.Origin;
								if (i != 0) {
									pos += RandomVector(-50, 50);
									pos.Z = lump.Origin.Z;
								}
								int newObj = DetailObjects.Count;
								DetailModel obj = new();
								DetailObjects.Add(obj);
								obj.InitSprite(newObj, flipped, pos, lump.Angles, lump.DetailModel, lump.Lighting, (int)lump.LightStyles, lump.LightStyleCount, lump.Orientation, lump.Scale, lump.Type, lump.ShapeAngle, lump.ShapeSize, lump.SwayAmount);
								++detailObjectCount;
							}
						}
						break;
				}
			}
		}

		if (detailObjectLeaf != -1) {
			if (numFastObjectsInCurLeaf != 0) {
				FastDetailLeafSpriteList newList = new() {
					NumSprites = numFastObjectsInCurLeaf,
					NumSIMDSprites = (3 + numFastObjectsInCurLeaf) >> 2,
					Sprites = FastSpriteData,
					StartSIMDSprite = curFastSpriteOut
				};
				curFastSpriteOut += newList.NumSIMDSprites;
				clientLeafSystem.SetSubSystemDataInLeaf(detailObjectLeaf, ClientLeafSystem.CLSUBSYSTEM_DETAILOBJECTS, newList);
			}
			clientLeafSystem.SetDetailObjectsInLeaf(detailObjectLeaf, firstDetailObject, detailObjectCount);
		}

#if DEBUG
		DevMsg($"UnserializeModels: {DetailObjects.Count} detail objects, {curFastObject} fast sprites\n");
#endif
	}

	void UnserializeModelLighting(Stream buf) {
		int count = 0;
		buf.ReadToStruct(ref count);
		DetailLightingList.EnsureCapacity(count);
		while (--count >= 0) {
			DetailPropLightstylesLump lump = default;
			buf.ReadToStruct(ref lump);
			DetailLightingList.Add(lump);
		}

#if DEBUG
		DevMsg($"UnserializeModelLighting: {DetailLightingList.Count} detail lighting entries\n");
#endif
	}

	Vector3 GetSpriteMiddleBottomPosition(in DetailObjectLump lump) {
		ref DetailPropSpriteDict dict = ref DetailSpriteDict(lump.DetailModel);

		MathLib.VectorSubtract(lump.Origin + new Vector3(0, -100, 0), lump.Origin, out Vector3 vecDir);
		vecDir.Z = 0.0f;
		MathLib.VectorAngles(vecDir, out QAngle angles);

		MathLib.AngleVectors(angles, out _, out Vector3 dx, out Vector3 dy);

		float scale = lump.Scale;
		MathLib.Vector2DMultiply(dict.UL, scale, out Vector2 ul);
		MathLib.Vector2DMultiply(dict.LR, scale, out Vector2 lr);

		MathLib.VectorMA(lump.Origin, ul.X, dx, out Vector3 vecOrigin);
		MathLib.VectorMA(vecOrigin, ul.Y, dy, out vecOrigin);
		dx *= lr.X - ul.X;
		dy *= lr.Y - ul.Y;

		return vecOrigin + dy + 0.5f * dx;
	}

	int CountSpritesInLeafList(int leafCount, ReadOnlySpan<LeafIndex_t> leafList) => throw new NotImplementedException();

	int CountSpriteQuadsInLeafList(int leafCount, ReadOnlySpan<LeafIndex_t> leafList) => throw new NotImplementedException();

	int CountFastSpritesInLeafList(int leafCount, ReadOnlySpan<LeafIndex_t> leafList, out int maxInLeaf) => throw new NotImplementedException();

	void FreeSortBuffers() {
		SortInfos = null;
		FastSortInfos = null;
		FastSortInfos = null;
		BuildoutBuffer = null;
	}

	static bool SortLessFunc(in SortInfo left, in SortInfo right) => throw new NotImplementedException();
	int SortSpritesBackToFront(int leaf, in Vector3 viewOrigin, in Vector3 viewForward, Span<SortInfo> sortInfo) => throw new NotImplementedException();

	IterationRetval EnumElement(int userId, nint context) => throw new NotImplementedException();
}
