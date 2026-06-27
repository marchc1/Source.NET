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

	public DetailModel() { }

	public bool InitCommon(int index, in Vector3 org, in QAngle angles) {
		MathLib.VectorCopy(org, out Origin);
		MathLib.VectorCopy(angles, out Angles);
		Alpha = 255;

		return true;
	}

	public bool Init(int index, in Vector3 org, in QAngle angles, Model? model, ColorRGBExp32 lighting, int lightstyle, byte lightstylecount, int orientation) => throw new NotImplementedException();

	public bool InitSprite(int index, bool flipped, in Vector3 org, in QAngle angles, ushort spriteIndex, ColorRGBExp32 lighting, int lightstyle, byte lightstylecount, int orientation, float scale, byte type, byte shapeAngle, byte shapeSize, byte swayAmount) => throw new NotImplementedException();

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
	public InlineArray4<nint> SpriteDefs;

	public void ReplicateFirstEntryToOthers() => throw new NotImplementedException();
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

	static readonly ConVar cl_detaildist = new("cl_detaildist", "1200", 0, "Distance at which detail props are no longer visible");
	static readonly ConVar cl_detailfade = new("cl_detailfade", "400", 0, "Distance across which detail props fade in");

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

	public void LevelInitPreEntity() => throw new NotImplementedException();
	public void LevelInitPostEntity() => throw new NotImplementedException();
	public void LevelShutdownPreClearSteamAPIContext() { }
	public void LevelShutdownPreEntity() => throw new NotImplementedException();
	public void LevelShutdownPostEntity() => throw new NotImplementedException();

	public void OnSave() { }
	public void OnRestore() { }
	public void SafeRemoveIfDesired() { }

	public IClientRenderable? GetDetailModel(int idx) => throw new NotImplementedException();

	public void BuildDetailObjectRenderLists(in Vector3 viewOrigin) => throw new NotImplementedException();

	public void RenderOpaqueDetailObjects(int leafCount, Span<LeafIndex_t> leafList) => throw new NotImplementedException();

	public void RenderTranslucentDetailObjects(in Vector3 viewOrigin, in Vector3 viewForward, in Vector3 viewRight, in Vector3 viewUp, int leafCount, Span<LeafIndex_t> leafList) => throw new NotImplementedException();

	public void RenderTranslucentDetailObjectsInLeaf(in Vector3 viewOrigin, in Vector3 viewForward, in Vector3 viewRight, in Vector3 viewUp, int leaf, Vector3? closestPoint) => throw new NotImplementedException();
	public void RenderFastTranslucentDetailObjectsInLeaf(in Vector3 viewOrigin, in Vector3 viewForward, in Vector3 viewRight, in Vector3 viewUp, int leaf, Vector3? closestPoint) => throw new NotImplementedException();

	public void BeginTranslucentDetailRendering() => throw new NotImplementedException();

	public bool EnumerateLeaf(int leaf, nint context) => throw new NotImplementedException();

	public ref DetailPropLightstylesLump DetailLighting(int i) => ref DetailLightingList.AsSpan()[i];
	public ref DetailPropSpriteDict DetailSpriteDict(int i) => ref DetailSpriteDictList.AsSpan()[i];

	int BuildOutSortedSprites(FastDetailLeafSpriteList data, in Vector3 viewOrigin, in Vector3 viewForward, in Vector3 viewRight, in Vector3 viewUp) => throw new NotImplementedException();

	void RenderFastSprites(in Vector3 viewOrigin, in Vector3 viewForward, in Vector3 viewRight, in Vector3 viewUp, int leafCount, ReadOnlySpan<LeafIndex_t> leafList) => throw new NotImplementedException();

	void UnserializeFastSprite(ref FastSpriteX4 spritex4, int subField, in DetailObjectLump lump, bool flipped, in Vector3 posOffset) => throw new NotImplementedException();

	void ScanForCounts(Stream buf, out int numOldStyleObjects, out int numFastSpritesToAllocate, out int maxOldInLeaf, out int maxFastInLeaf) => throw new NotImplementedException();

	void UnserializeModelDict(Stream buf) => throw new NotImplementedException();
	void UnserializeDetailSprites(Stream buf) => throw new NotImplementedException();
	void UnserializeModels(Stream buf) => throw new NotImplementedException();
	void UnserializeModelLighting(Stream buf) => throw new NotImplementedException();

	Vector3 GetSpriteMiddleBottomPosition(in DetailObjectLump lump) => throw new NotImplementedException();

	int CountSpritesInLeafList(int leafCount, ReadOnlySpan<LeafIndex_t> leafList) => throw new NotImplementedException();

	int CountSpriteQuadsInLeafList(int leafCount, ReadOnlySpan<LeafIndex_t> leafList) => throw new NotImplementedException();

	int CountFastSpritesInLeafList(int leafCount, ReadOnlySpan<LeafIndex_t> leafList, out int maxInLeaf) => throw new NotImplementedException();

	void FreeSortBuffers() => throw new NotImplementedException();

	static bool SortLessFunc(in SortInfo left, in SortInfo right) => throw new NotImplementedException();
	int SortSpritesBackToFront(int leaf, in Vector3 viewOrigin, in Vector3 viewForward, Span<SortInfo> sortInfo) => throw new NotImplementedException();

	IterationRetval EnumElement(int userId, nint context) => throw new NotImplementedException();
}
