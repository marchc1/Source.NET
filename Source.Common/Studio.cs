using CommunityToolkit.HighPerformance;

using Source.Common.DataCache;
using Source.Common.Engine;
using Source.Common.Formats.BSP;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
namespace Source.Common;

using static Source.Common.Networking.svc_ClassInfo;
using static StudioDeps;

[EngineComponent]
public static class StudioDeps
{
	[Dependency] public static IModelInfo modelinfo { get; private set; } = null!;
}

public delegate T FactoryFn<T>(object caller, Memory<byte> data);
public static class Studio
{
	public static T UnmanagedFactoryFn<T>(object caller, Memory<byte> data) where T : unmanaged => data.Span.Cast<byte, T>()[0];

	// Some helper functions so I didn't have to write the same boilerplate over and over again for all of the class based views.
	// The way most of this file works differs from Source in that it instantiates class instances from binary data. That decision
	// was made because Source does most of this with pointers - and quite often stores those pointers, which we cant do with
	// reinterpreted struct references in any C#-safe way...
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ref T ProduceArrayIdx<T>(
		object caller,            // the caller (some factories need this)
		[NotNull] ref T[]? array, // the array to initialize if null
		int elements,             // how many elements in the array, NumLocalSeq for example
		int dataOffset,           // the offset into data, LocalSeqIndex for example
		int index,                // the array index
		int sizeOfOne,            // size of one component
		Memory<byte> data,        // the raw binary
		FactoryFn<T> factory      // how to create a new instance from the binary data, this also gets cached
	) {
		array ??= new T[elements];

		ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, elements);

		array[index] ??= factory(caller, data[(dataOffset + (index * sizeOfOne))..]);
		return ref array[index];
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T ProduceVaradicArrayIdx<T>(
		object caller,                 // the caller (some factories need this)
		[NotNull] ref List<T?>? array, // the array to initialize if null
		int dataOffset,                // the offset into data, LocalSeqIndex for example
		int index,                     // the array index
		int sizeOfOne,                 // size of one component
		Memory<byte> data,             // the raw binary
		FactoryFn<T> factory           // how to create a new instance from the binary data, this also gets cached
	) {
		array ??= [];

		ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
		array.EnsureCountDefault(index + 1);

		return array[index] ??= factory(caller, data[(dataOffset + (index * sizeOfOne))..]);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string ProduceASCIIString(ref string? cache, in Span<byte> data) {
		if (cache == null) {
			using ASCIIStringView view = new(data);
			cache = new(view);
		}

		return cache;
	}


	public const int STUDIO_VERSION = 48;

	public const int MAXSTUDIOTRIANGLES = 65536;
	public const int MAXSTUDIOVERTS = 65536;
	public const int MAXSTUDIOFLEXVERTS = 10000;

	public const int MAXSTUDIOSKINS = 32;
	public const int MAXSTUDIOBONES = 256;
	public const int MAXSTUDIOFLEXDESC = 1024;
	public const int MAXSTUDIOFLEXCTRL = 96;
	public const int MAXSTUDIOPOSEPARAM = 24;
	public const int MAXSTUDIOBONECTRLS = 4;
	public const int MAXSTUDIOANIMBLOCKS = 256;

	public const int MAX_NUM_LODS = 8;
	public const int MAX_NUM_BONES_PER_VERT = 3;

	public const int MAXSTUDIOBONEBITS = 7;

	public const int MODEL_VERTEX_FILE_ID = (('V' << 24) + ('S' << 16) + ('D' << 8) + 'I');
	public const int MODEL_VERTEX_FILE_VERSION = 4;
	public const int MODEL_VERTEX_FILE_THIN_ID = (('V' << 24) + ('C' << 16) + ('D' << 8) + 'I');

	public const int BONE_CALCULATE_MASK = 0x1F;
	public const int BONE_PHYSICALLY_SIMULATED = 0x01;
	public const int BONE_PHYSICS_PROCEDURAL = 0x02;
	public const int BONE_ALWAYS_PROCEDURAL = 0x04;
	public const int BONE_SCREEN_ALIGN_SPHERE = 0x08;
	public const int BONE_SCREEN_ALIGN_CYLINDER = 0x10;

	public const int BONE_USED_MASK = 0x0007FF00;
	public const int BONE_USED_BY_ANYTHING = 0x0007FF00;
	public const int BONE_USED_BY_HITBOX = 0x00000100;
	public const int BONE_USED_BY_ATTACHMENT = 0x00000200;
	public const int BONE_USED_BY_VERTEX_MASK = 0x0003FC00;
	public const int BONE_USED_BY_VERTEX_LOD0 = 0x00000400;
	public const int BONE_USED_BY_VERTEX_LOD1 = 0x00000800;
	public const int BONE_USED_BY_VERTEX_LOD2 = 0x00001000;
	public const int BONE_USED_BY_VERTEX_LOD3 = 0x00002000;
	public const int BONE_USED_BY_VERTEX_LOD4 = 0x00004000;
	public const int BONE_USED_BY_VERTEX_LOD5 = 0x00008000;
	public const int BONE_USED_BY_VERTEX_LOD6 = 0x00010000;
	public const int BONE_USED_BY_VERTEX_LOD7 = 0x00020000;
	public const int BONE_USED_BY_BONE_MERGE = 0x00040000;

	public const int BONE_TYPE_MASK = 0x00F00000;

	public const int BONE_FIXED_ALIGNMENT = 0x00100000;
	public const int BONE_HAS_SAVEFRAME_POS = 0x00200000;
	public const int BONE_HAS_SAVEFRAME_ROT = 0x00400000;

	public const int MAX_NUM_BONE_INDICES = 4;

	public const int USESHADOWLOD = -2;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static int BONE_USED_BY_VERTEX_AT_LOD(int lod) => BONE_USED_BY_VERTEX_LOD0 << lod;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public static int BONE_USED_BY_ANYTHING_AT_LOD(int lod) => ((BONE_USED_BY_ANYTHING & ~BONE_USED_BY_VERTEX_MASK) | BONE_USED_BY_VERTEX_AT_LOD(lod));
}

public enum StudioHdrFlags
{
	AutoGeneratedHitbox = 0x00000001,
	UsesEnvCubemap = 0x00000002,
	ForceOpaque = 0x00000004,
	TranslucentTwoPass = 0x00000008,
	StaticProp = 0x00000010,
	UsesFbTexture = 0x00000020,
	HasShadowLod = 0x00000040,
	UsesBumpmapping = 0x00000080,
	UseShadowLodMaterials = 0x00000100,
	Obsolete = 0x00000200,
	Unused = 0x00000400,
	NoForcedFade = 0x00000800,
	ForcePhonemeCrossfade = 0x00001000,
	ConstantDirectionalLightDot = 0x00002000,
	FlexesConverted = 0x00004000,
	BuiltInPreviewMode = 0x00008000,
	AmbientBoost = 0x00010000,
	DoNotCastShadows = 0x00020000,
	CastTextureShadows = 0x00040000,
	VertAnimFixedPointScale = 0x00200000
}


[InlineArray(Studio.MAX_NUM_LODS)] public struct InlineArrayMaxNumLODs<T> { T first; }
[InlineArray(Studio.MAX_NUM_BONES_PER_VERT)] public struct InlineArrayMaxNumBonesPerVert<T> { T first; }

public class VirtualGroup
{
	public object? Cache;
	public readonly List<int> BoneMap = [];
	public readonly List<int> MasterBone = [];
	public readonly List<int> MasterSeq = [];
	public readonly List<int> MasterAnim = [];
	public readonly List<int> MasterAttachment = [];
	public readonly List<int> MasterPose = [];
	public readonly List<int> MasterNode = [];
}

public class VirtualSequence
{
	public int Flags;
	public int Activity;
	public int Group;
	public int Index;
}

public class VirtualGeneric
{
	public int Group;
	public int Index;
}

public class VirtualModel
{
	public void AppendSequences(int group, StudioHeader studioHDR) {

	}
	public void AppendAnimations(int group, StudioHeader studioHDR) {

	}
	public void AppendAttachments(int group, StudioHeader studioHDR) {

	}
	public void AppendPoseParameters(int group, StudioHeader studioHDR) {

	}
	public void AppendBonemap(int group, StudioHeader studioHDR) {

	}
	public void AppendNodes(int group, StudioHeader studioHDR) {

	}
	public void AppendTransitions(int group, StudioHeader studioHDR) {

	}
	public void AppendIKLocks(int group, StudioHeader studioHDR) {

	}
	public void AppendModels(int group, StudioHeader studioHDR) {
		AppendSequences(group, studioHDR);
		AppendAnimations(group, studioHDR);
		AppendBonemap(group, studioHDR);
		AppendAttachments(group, studioHDR);
		AppendPoseParameters(group, studioHDR);
		AppendNodes(group, studioHDR);
		AppendIKLocks(group, studioHDR);
		// todo

		UpdateAutoplaySequences(studioHDR);
	}
	public void UpdateAutoplaySequences(StudioHeader studioHDR) {

	}

	public VirtualGroup AnimGroup(int animation) {
		throw new NotImplementedException();
	}
	public VirtualGroup SeqGroup(int sequence) {
		throw new NotImplementedException();
	}

	// TODO
	// public readonly Mutex Lock = new();

	public readonly List<VirtualSequence> Seq = [];
	public readonly List<VirtualGeneric> Anim = [];
	public readonly List<VirtualGeneric> Attachment = [];
	public readonly List<VirtualGeneric> Pose = [];
	public readonly List<VirtualGroup> Group = [];
	public readonly List<VirtualGeneric> Node = [];
	public readonly List<VirtualGeneric> IKLock = [];
	public readonly List<ushort> AutoplaySequences = [];
}

public enum StudioMeshGroupFlags
{
	IsFlexed = 0x1,
	IsHWSkinned = 0x2,
	IsDeltaFlexed = 0x4,
}

public class StudioMeshGroup
{
	public IMesh? Mesh;
	public int NumStrips;
	public StudioMeshGroupFlags Flags;
	public OptimizedModel.StripHeader[]? StripData;
	public ushort[]? GroupIndexToMeshIndex;
	public int NumVertices;
	public ushort[]? Indices;
	public bool MeshNeedsRestore;
	public int ColorMeshID;
	// IMorph?

	public ushort MeshIndex(int i) => GroupIndexToMeshIndex![Indices![i]];
}

public class StudioMeshData
{
	public int NumGroup;
	public StudioMeshGroup[]? MeshGroup;
}

public class StudioLODData
{
	public StudioMeshData[]? MeshData;
	public float SwitchPoint;
	public IMaterial[]? Materials;
	public int[]? MaterialFlags;
}

public class StudioHWData
{
	public int RootLOD;
	public int NumLODs;
	public StudioLODData[]? LODs;
	public int NumStudioMeshes;

	public int GetLODForMetric(float lodMetric) {
		if (NumLODs == 0)
			return 0;

		int numLODs = (LODs![NumLODs - 1].SwitchPoint < 0.0f) ? NumLODs - 1 : NumLODs;

		for (int i = RootLOD; i < numLODs - 1; i++) {
			if (LODs[i + 1].SwitchPoint > lodMetric)
				return i;
		}

		return numLODs - 1;
	}

	public float LODMetric(float unitSphereSize) => (unitSphereSize != 0.0f) ? (100.0f / unitSphereSize) : 0.0f;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct MStudioBoneWeight
{
	public InlineArrayMaxNumBonesPerVert<float> Weight;
	public InlineArrayMaxNumBonesPerVert<byte> Bone;
	public byte NumBones;
}

/// <summary>
/// mstudio_meshvertexdata_t
/// </summary>
public class MStudioMeshVertexData
{
	public Memory<byte> Data;
	public MStudioModelVertexData? ModelVertexData;
	public InlineArrayMaxNumLODs<int> NumLODVertexes;

	public MStudioMeshVertexData(Memory<byte> data) {
		Data = data;
		Span<byte> span = data.Span;
		for (int i = 0; i < Studio.MAX_NUM_LODS; i++) {
			NumLODVertexes[i] = span.Cast<byte, int>()[i + 1];
		}
	}

	public bool HasTangentData() => ModelVertexData!.HasTangentData();

	public ref Vector3 Position(int i) => ref ModelVertexData!.Position(i);
	public ref Vector3 Normal(int i) => ref ModelVertexData!.Normal(i);
	public ref Vector4 TangentS(int i) => ref ModelVertexData!.TangentS(i);
	public ref Vector2 TexCoord(int i) => ref ModelVertexData!.TexCoord(i);
	public ref MStudioBoneWeight BoneWeights(int i) => ref ModelVertexData!.BoneWeights(i);
	public ref MStudioVertex Vertex(int i) => ref ModelVertexData!.Vertex(i);
}


public class MStudioModelVertexData
{
	public object? VertexData;
	public object? TangentData;

	public object? GetVertexData() => VertexData;
	public T? GetVertexData<T>() => (T?)VertexData;
	public object? GetTangentData() => TangentData;
	public T? GetTangentData<T>() => (T?)TangentData;

	public ref Vector3 Position(int i) => ref Vertex(i).Position;
	public ref Vector3 Normal(int i) => ref Vertex(i).Normal;
	public ref Vector4 TangentS(int i) => ref ((Memory<Vector4>)GetTangentData()!).Span[i];
	public ref Vector2 TexCoord(int i) => ref Vertex(i).TexCoord;
	public ref MStudioBoneWeight BoneWeights(int i) => ref Vertex(i).BoneWeights;
	public ref MStudioVertex Vertex(int i) => ref ((Memory<MStudioVertex>)GetVertexData()!).Span[i];

	// todo: verify
	public bool HasTangentData() => TangentData != null;
}

public class MStudioMesh
{
	public const int SIZEOF = 116; // don't feel like typing this out right now
	public Memory<byte> Data;
	public readonly MStudioModel Model;
	public MStudioMesh(MStudioModel model, Memory<byte> data) {
		Data = data;
		Model = model;

		Material = data.Span[0..].Cast<byte, int>()[0];
		ModelIndex = data.Span[4..].Cast<byte, int>()[0];
		NumVertices = data.Span[8..].Cast<byte, int>()[0];
		VertexOffset = data.Span[12..].Cast<byte, int>()[0];

		NumFlexes = data.Span[16..].Cast<byte, int>()[0];
		FlexIndex = data.Span[20..].Cast<byte, int>()[0];
		MaterialType = data.Span[24..].Cast<byte, int>()[0];
		MaterialParam = data.Span[28..].Cast<byte, int>()[0];
		MeshID = data.Span[32..].Cast<byte, int>()[0];
		Center = data.Span[36..].Cast<byte, Vector3>()[0];
		VertexData = new(data[48..]);
	}

	public int Material;
	public int ModelIndex;
	public int NumVertices;
	public int VertexOffset;

	public int NumFlexes;
	public int FlexIndex;
	public int MaterialType;
	public int MaterialParam;
	public int MeshID;
	public Vector3 Center;
	public readonly MStudioMeshVertexData VertexData;

	public MStudioMeshVertexData? GetVertexData(IStudioDataCache dataCache, StudioHeader studioHdr) {
		this.Model.GetVertexData(dataCache, studioHdr);
		VertexData.ModelVertexData = this.Model.VertexData;
		if (VertexData.ModelVertexData.VertexData == null)
			return null;
		return VertexData;
	}
}
/// <summary>
/// analog of mstudiomodel_t
/// </summary>
public class MStudioModel
{
	Memory<byte> Data;
	[StructLayout(LayoutKind.Explicit)]
	internal struct __contents
	{
		[FieldOffset(0)] public InlineArray64<byte> Name;
		[FieldOffset(64)] public int Type;
		[FieldOffset(68)] public float BoundingRadius;
		[FieldOffset(72)] public int NumMeshes;
		[FieldOffset(76)] public int MeshIndex;
		[FieldOffset(80)] public int NumVertices;
		[FieldOffset(84)] public int VertexIndex;
		[FieldOffset(88)] public int TangentsIndex;
		[FieldOffset(92)] public int NumAttachments;
		[FieldOffset(96)] public int AttachmentIndex;
		[FieldOffset(100)] public int NumEyeballs;
		[FieldOffset(104)] public int EyeballIndex;

		// these are pointers which will instead be class instance refs OUTSIDE of the contents.
		// But we need to parse unused data anyway here for the pointers, so
		[FieldOffset(108)] InlineArray40<byte> unused;
	}
	__contents Contents;


	public ref int Type { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref Contents.Type; }
	public ref float BoundingRadius { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref Contents.BoundingRadius; }
	public ref int NumMeshes { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref Contents.NumMeshes; }
	public ref int MeshIndex { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref Contents.MeshIndex; }
	public ref int NumVertices { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref Contents.NumVertices; }
	public ref int VertexIndex { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref Contents.VertexIndex; }
	public ref int TangentsIndex { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref Contents.TangentsIndex; }
	public ref int NumAttachments { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref Contents.NumAttachments; }
	public ref int AttachmentIndex { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref Contents.AttachmentIndex; }
	public ref int NumEyeballs { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref Contents.NumEyeballs; }
	public ref int EyeballIndex { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref Contents.EyeballIndex; }

	public readonly MStudioModelVertexData VertexData = new();

	public MStudioModel(Memory<byte> data) {
		Data = data;
		Contents = data.Span.Cast<byte, __contents>()[0];
	}

	MStudioMesh[]? studioMeshCache;

	public MStudioMesh Mesh(int i) {
		if (studioMeshCache == null)
			studioMeshCache = new MStudioMesh[NumMeshes];

		ArgumentOutOfRangeException.ThrowIfLessThan(i, 0);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(i, NumMeshes);

		if (studioMeshCache[i] == null)
			return studioMeshCache[i] = new(this, Data[(MeshIndex + (i * MStudioMesh.SIZEOF))..]);
		return studioMeshCache[i];
	}

	string? nameCache;
	public string Name() => Studio.ProduceASCIIString(ref nameCache, Contents.Name);

	public MStudioModelVertexData? GetVertexData(IStudioDataCache dataCache, StudioHeader studioHdr) {
		VertexFileHeader? vertexHdr = CacheVertexData(dataCache, studioHdr);
		if (vertexHdr == null) {
			VertexData.VertexData = null;
			VertexData.TangentData = null;
			return null;
		}

		VertexData.VertexData = vertexHdr.GetVertexData();
		VertexData.TangentData = vertexHdr.GetTangentData();

		if (VertexData.VertexData == null)
			return null;

		return VertexData;
	}

	public VertexFileHeader? CacheVertexData(IStudioDataCache mdlCache, StudioHeader studioHdr) {
		return mdlCache.CacheVertexData(studioHdr);
	}
}

public class MStudioBodyParts(Memory<byte> Data)
{
	public const int SIZEOF = sizeof(int) * 4;
	public static MStudioBodyParts FACTORY(object caller, Memory<byte> data) => new(data);

	public int SzNameIndex => MemoryMarshal.Cast<byte, int>(Data.Span)[0];
	public int NumModels => MemoryMarshal.Cast<byte, int>(Data.Span)[1];
	public int Base => MemoryMarshal.Cast<byte, int>(Data.Span)[2];
	public int ModelIndex => MemoryMarshal.Cast<byte, int>(Data.Span)[3];


	string? nameCache;
	public string Name() => Studio.ProduceASCIIString(ref nameCache, Data.Span[SzNameIndex..]);

	MStudioModel[]? studioModelCache;

	public MStudioModel Model(int i) {
		if (studioModelCache == null)
			studioModelCache = new MStudioModel[NumModels];

		ArgumentOutOfRangeException.ThrowIfLessThan(i, 0);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(i, NumModels);

		if (studioModelCache[i] == null)
			return studioModelCache[i] = new(Data[(ModelIndex + (i * Unsafe.SizeOf<MStudioModel.__contents>()))..]);
		return studioModelCache[i];
	}
}

[StructLayout(LayoutKind.Explicit, Pack = 4)]
public struct MStudioVertex
{
	[FieldOffset(0)] public MStudioBoneWeight BoneWeights;
	[FieldOffset(16)] public Vector3 Position;
	[FieldOffset(28)] public Vector3 Normal;
	[FieldOffset(40)] public Vector2 TexCoord;
}

public class VertexFileHeader
{
	public Memory<byte> Data;

	public VertexFileHeader(byte[] data) {
		Data = data;
		using BinaryReader br = new(new MemoryStream(data), System.Text.Encoding.ASCII);
		ID = br.ReadInt32();
		Version = br.ReadInt32();
		Checksum = br.ReadInt32();
		NumLODs = br.ReadInt32();
		for (int i = 0; i < Studio.MAX_NUM_LODS; i++)
			NumLODVertices[i] = br.ReadInt32();

		NumFixups = br.ReadInt32();
		FixupTableStart = br.ReadInt32();
		VertexDataStart = br.ReadInt32();
		TangentDataStart = br.ReadInt32();
	}

	public int ID;
	public int Version;
	public int Checksum;
	public int NumLODs;
	public InlineArrayMaxNumLODs<int> NumLODVertices;
	public int NumFixups;
	public int FixupTableStart;
	public int VertexDataStart;
	public int TangentDataStart;

	public Memory<MStudioVertex> GetVertexData() {
		if (ID == Studio.MODEL_VERTEX_FILE_ID && VertexDataStart != 0)
			return Data[VertexDataStart..].Cast<byte, MStudioVertex>();
		else
			return null;
	}

	public Memory<Vector4> GetTangentData() {
		if (ID == Studio.MODEL_VERTEX_FILE_ID && TangentDataStart != 0)
			return Data[TangentDataStart..].Cast<byte, Vector4>();
		else
			return null;
	}
}

public class StudioHeader2
{
	public Memory<byte> Data;

	public StudioHeader2(Memory<byte> data) {
		Data = data;
		SpanBinaryReader br = new(data.Span);

		br.Read(out NumSrcBoneTransform);
		br.Read(out SrcBoneTransformIndex);
		br.Read(out IllumPositionAttachmentIndex);
		br.Read(out MaxEyeDeflection);
		br.Read(out LinearBoneIndex);
		br.Read(out SzNameIndex);
		br.Read(out BoneFlexDriverCount);
		br.Read(out BoneFlexDriverIndex);
	}

	public int NumSrcBoneTransform;
	public int SrcBoneTransformIndex;
	public int IllumPositionAttachmentIndex;
	public float MaxEyeDeflection;

	public int LinearBoneIndex;
	MStudioLinearBone? linearBones;
	public MStudioLinearBone LinearBones() => linearBones ??= new(Data[LinearBoneIndex..]);

	public int SzNameIndex;
	public int BoneFlexDriverCount;
	public int BoneFlexDriverIndex;
	public InlineArray56<int> Reserved;
}

public class MStudioTexture
{
	public const int SIZE_OF_ONE = 64; // msvc reports size 64, alignment 4
	public static MStudioTexture FACTORY(object caller, Memory<byte> data) => new(data);

	public Memory<byte> Data;

	public int SzNameIndex;
	public int Flags;
	public int Used;
	public int Unused;

	public IMaterial? Material;
	public object? ClientMaterial;

	public MStudioTexture(Memory<byte> data) {
		Data = data;
		SpanBinaryReader br = new(data.Span);

		SzNameIndex = br.Read<int>();
		Flags = br.Read<int>();
		Used = br.Read<int>();
		Unused = br.Read<int>();
	}

	public string? name;
	public string Name() => Studio.ProduceASCIIString(ref name, Data.Span[SzNameIndex..]);
}

public enum StudioAnimSeqFlags
{
	/// <summary>
	/// ending frame should be the same as the starting frame
	/// </summary>
	Looping = 0x0001,
	/// <summary>
	/// do not interpolate between previous animation and this one
	/// </summary>
	Snap = 0x0002,
	/// <summary>
	/// this sequence "adds" to the base sequences, not slerp blends
	/// </summary>
	Delta = 0x0004,
	/// <summary>
	/// temporary flag that forces the sequence to always play
	/// </summary>
	Autoplay = 0x0008,
	Post = 0x0010,
	/// <summary>
	/// this animation/sequence has no real animation data
	/// </summary>
	AllZeros = 0x0020,
	/// <summary>
	/// cycle index is taken from a pose parameter index
	/// </summary>
	CyclePose = 0x0080,
	/// <summary>
	/// cycle index is taken from a real-time clock, not the animations cycle index
	/// </summary>
	Realtime = 0x0100,
	/// <summary>
	/// sequence has a local context sequence
	/// </summary>
	Local = 0x0200,
	/// <summary>
	/// don't show in default selection views
	/// </summary>
	Hidden = 0x0400,
	/// <summary>
	/// a forward declared sequence (empty)
	/// </summary>
	Override = 0x0800,
	/// <summary>
	/// Has been updated at runtime to activity index
	/// </summary>
	Activity = 0x1000,
	/// <summary>
	/// Has been updated at runtime to event index
	/// </summary>
	Event = 0x2000,
	/// <summary>
	/// sequence blends in worldspace
	/// </summary>
	World = 0x4000
}

public enum StudioAnimFlags : byte
{
	/// <summary>
	/// Vector48
	/// </summary>
	RawPos = 0x01,
	/// <summary>
	/// Quaternion48
	/// </summary>
	RawRot = 0x02,
	/// <summary>
	/// mstudioanim_valueptr_t
	/// </summary>
	AnimPos = 0x04,
	/// <summary>
	/// mstudioanim_valueptr_t
	/// </summary>
	AnimRot = 0x08,
	Delta = 0x10,
	/// <summary>
	/// Quaternion64
	/// </summary>
	RawRot2 = 0x20,
}

public struct MStudioAnimValue
{
	public short Value;
	public readonly byte Valid => (byte)(Value & 0xFF);
	public readonly byte Total => (byte)((Value & 0xFF00) >> 8);
}

public class MStudioAnimValuePtr
{
	public const int SIZEOF = 6;

	public Memory<byte> Data;
	public InlineArray3<short> Offset;

	public MStudioAnimValuePtr(Memory<byte> data) {
		Data = data;
		SpanBinaryReader br = new(data.Span);
		Offset[0] = br.Read<short>();
		Offset[1] = br.Read<short>();
		Offset[2] = br.Read<short>();
	}

	public Span<MStudioAnimValue> Animvalue(int i) {
		if (Offset[i] > 0)
			return Data.Span[Offset[i]..].Cast<byte, MStudioAnimValue>();
		return null;
	}
}

// TODO: THESE ARE NOT EQUATABLE, PAY CLOSE ATTENTION IF SOURCE IS TRYING TO DO POINTER COMPARISON!!!!!!!!!!!!!!!!!!!!!!
// TODO: Should we use structs here instead and reinterpret casts? It's such a small data structure... and I worry heavily about
// the optimizations I tried to make to cope...
public class MStudioAnim
{
	public const int SIZEOF = 4; // Static offset from MSVC stats (mstudiobone_t size 4, alignment 2)
	public static MStudioAnim FACTORY(object caller, Memory<byte> data) => new(data);

	public Memory<byte> data;
	public byte Bone;
	public StudioAnimFlags Flags;
	public short NextOffset;

	public MStudioAnim(Memory<byte> data) {
		this.data = data;
		Bone = data.Span[0];
		Flags = (StudioAnimFlags)data.Span[1];
		NextOffset = data.Span[2..4].Cast<byte, short>()[0];
	}


	public Span<byte> Data() => data.Span[SIZEOF..];

	MStudioAnimValuePtr? rotV;
	MStudioAnimValuePtr? posV;

	public MStudioAnimValuePtr RotV() => rotV ??= new(data[SIZEOF..]);
	public MStudioAnimValuePtr PosV() => posV ??= new(data[(SIZEOF + ((Flags & StudioAnimFlags.AnimRot) != 0 ? MStudioAnimValuePtr.SIZEOF : 0))..]);

	public ref Quaternion48 Quat48() => ref Data().Cast<byte, Quaternion48>()[0];
	public ref Quaternion64 Quat64() => ref Data().Cast<byte, Quaternion64>()[0];
	public unsafe ref Vector48 Pos()
		=> ref Data()[(((Flags & StudioAnimFlags.RawRot) != 0 ? 1 : 0) * sizeof(Quaternion48) + ((Flags & StudioAnimFlags.RawRot2) != 0 ? 1 : 0) * sizeof(Quaternion64))..].Cast<byte, Vector48>()[0];


	MStudioAnim? next;
	public MStudioAnim? Next() {
		if (NextOffset != 0)
			return next ??= FACTORY(null!, data[NextOffset..]);
		else
			return null;
	}
}

public class MStudioLocalHierarchy
{
	public const int SIZEOF = 48; // Static offset from MSVC stats (mstudiobone_t size 48, alignment 4)
	public static MStudioLocalHierarchy FACTORY(object caller, Memory<byte> data) => new(data);

	public Memory<byte> Data;

	public int Bone;
	public int NewParent;
	public float Start;
	public float Peak;
	public float Tail;
	public float End;
	public int StartFrame; // iStart 
	public int LocalAnimIndex;

	public MStudioLocalHierarchy(Memory<byte> data) {
		Data = data;
		SpanBinaryReader br = new(data.Span);

		br.Read(out Bone);
		br.Read(out NewParent);
		br.Read(out Start);
		br.Read(out Peak);
		br.Read(out Tail);
		br.Read(out End);
		br.Read(out StartFrame);
		br.Read(out LocalAnimIndex);
		br.Advance<int>(4);
	}
}

public class MStudioAnimSections
{
	public const int SIZEOF = 8; // Static offset from MSVC stats (mstudiobone_t size 8, alignment 4)
	public static MStudioAnimSections FACTORY(object caller, Memory<byte> data) => new() {
		AnimBlock = data.Span.Cast<byte, int>()[0],
		AnimIndex = data.Span.Cast<byte, int>()[1]
	};
	public int AnimBlock;
	public int AnimIndex;
}

public class MStudioAnimDesc
{
	public const int SIZEOF = 100; // Static offset from MSVC stats (mstudiobone_t size 100, alignment 4)
	public static MStudioAnimDesc FACTORY(object caller, Memory<byte> data) => new((StudioHeader)caller ?? throw new NullReferenceException(), data);
	public readonly StudioHeader hdr;
	public StudioHeader Studiohdr() => hdr;
	public Memory<byte> Data;

	public int NameIndex;

	public float FPS;
	private int flags;
	public StudioAnimSeqFlags Flags => (StudioAnimSeqFlags)flags;

	public int NumFrames;

	public int NumMovements;
	public int MovementIndex;

	// UNUSED 6 INTEGERS

	int nAnimBlock;
	int nAnimIndex;

	List<List<MStudioAnim?>?> animCache = [];
	MStudioAnim CacheOffBlockIndex(int block, int index, Memory<byte> data) {
		animCache.EnsureCount(block + 1);
		var blockCache = animCache[block] ??= [];
		blockCache.EnsureCountDefault(index + 1);
		return blockCache[index] ??= MStudioAnim.FACTORY(null!, data[index..]);
	}

	public MStudioAnim? AnimBlock(int block, int index) {
		if (block == -1)
			return null;

		if (block == 0)
			return CacheOffBlockIndex(0, index, Data);

		Memory<byte> animBlock = Studiohdr().GetAnimBlock(block);
		if (!animBlock.IsEmpty)
			return CacheOffBlockIndex(0, index, animBlock);

		return null;
	}
	public MStudioAnim? Anim(ref int frame) => Anim(ref frame, out _);
	public MStudioAnim? Anim(ref int frame, out TimeUnit_t stall) {
		stall = default;

		MStudioAnim panim = null;

		int block = nAnimBlock;
		int index = nAnimIndex;
		int section = 0;

		if (SectionFrames != 0) {
			if (NumFrames > SectionFrames && frame == NumFrames - 1) {
				// last frame on long anims is stored separately
				frame = 0;
				section = (NumFrames / SectionFrames) + 1;
			}
			else {
				section = frame / SectionFrames;
				frame -= section * SectionFrames;
			}

			block = Section(section).AnimBlock;
			index = Section(section).AnimIndex;
		}

		if (block == -1) {
			// model needs to be recompiled
			return null;
		}

		panim = AnimBlock(block, index);

		// force a preload on the next block
		if (SectionFrames != 0) {
			int count = (NumFrames / SectionFrames) + 2;
			for (int i = section + 1; i < count; i++) {
				if (Section(i).AnimBlock != block) {
					AnimBlock(Section(i).AnimBlock, Section(i).AnimIndex);
					break;
				}
			}
		}

		if (panim == null) {
			// back up until a previously loaded block is found
			while (--section >= 0) {
				block = Section(section).AnimBlock;
				index = Section(section).AnimIndex;
				panim = AnimBlock(block, index);
				if (panim != null) {
					// set it to the last frame in the last valid section
					frame = SectionFrames - 1;
					break;
				}
			}
		}

		// try to guess a valid stall time interval (tuned for the X360)
		stall = 0.0;
		if (panim == null && section <= 0) {
			ZeroFrameStallTime = Platform.Time;
			stall = 1.0;
		}
		else if (panim != null && ZeroFrameStallTime != 0.0) {
			TimeUnit_t dt = Platform.Time - ZeroFrameStallTime;
			if (dt >= 0.0f)
				stall = MathLib.SimpleSpline(Math.Clamp((0.200f - dt) * 5.0f, 0.0f, 1.0f));

			if (stall == 0.0)
				ZeroFrameStallTime = 0.0;
		}

		return panim;
	}

	public int NumIKRules;
	public int IKRuleIndex;
	public int AnimBlockIKRuleIndex;

	public int NumLocalHierarchy;
	public int LocalHierarchyIndex;
	MStudioLocalHierarchy[]? localHierarchyCache;
	public MStudioLocalHierarchy Hierarchy(int i) => Studio.ProduceArrayIdx(this, ref localHierarchyCache, NumLocalHierarchy, LocalHierarchyIndex, i, MStudioLocalHierarchy.SIZEOF, Data, MStudioLocalHierarchy.FACTORY);

	public int SectionIndex;
	public int SectionFrames;
	MStudioAnimSections[]? sections;
	public MStudioAnimSections Section(int i) => Studio.ProduceArrayIdx(this, ref sections, SectionFrames, SectionIndex, i, MStudioAnimSections.SIZEOF, Data, MStudioAnimSections.FACTORY);


	public int ZeroFrameSpan;
	public int ZeroFrameCount;
	public int ZeroFrameIndex;
	public Span<byte> ZeroFrameData() {
		if (ZeroFrameIndex != 0) {
			return Data.Span[ZeroFrameIndex..];
		}

		return null;
	}

	public double ZeroFrameStallTime;

	public MStudioAnimDesc(StudioHeader hdr, Memory<byte> data) {
		this.hdr = hdr;
		Data = data;
		SpanBinaryReader br = new(Data.Span);

		br.Read<int>(); // index not needed
		br.Read(out NameIndex);
		br.Read(out FPS);
		br.Read(out flags);
		br.Read(out NumFrames);
		br.Read(out NumMovements);
		br.Read(out MovementIndex);
		br.Advance<int>(6);

		br.Read(out nAnimBlock);
		br.Read(out nAnimIndex);
		br.Read(out NumIKRules);
		br.Read(out IKRuleIndex);
		br.Read(out AnimBlockIKRuleIndex);
		br.Read(out NumLocalHierarchy);
		br.Read(out LocalHierarchyIndex);
		br.Read(out SectionIndex);
		br.Read(out SectionFrames);
		br.Read(out ZeroFrameSpan);
		br.Read(out ZeroFrameCount);
		br.Read(out ZeroFrameIndex);
	}
}

public class MStudioAnimBlock
{
	public const int SIZEOF = 8; // Static offset from MSVC stats (mstudiobone_t size 20, alignment 4)
	public static MStudioAnimBlock FACTORY(object caller, Memory<byte> data) => new(data);

	public Memory<byte> Data;
	public int DataStart;
	public int DataEnd;

	public MStudioAnimBlock(Memory<byte> data) {
		Data = data;
		SpanBinaryReader br = new(Data.Span);
		br.Read(out DataStart);
		br.Read(out DataEnd);
	}
}
public class MStudioPoseParamDesc
{
	public const int SIZEOF = 20; // Static offset from MSVC stats (mstudiobone_t size 20, alignment 4)
	public static MStudioPoseParamDesc FACTORY(object caller, Memory<byte> data) => new(data);

	public Memory<byte> Data;

	public int NameIndex;
	public string? nameCache;
	public string Name() => Studio.ProduceASCIIString(ref nameCache, Data.Span[NameIndex..]);

	public int Flags;
	public float Start;
	public float End;
	public float Loop;

	public MStudioPoseParamDesc(Memory<byte> data) {
		Data = data;
		SpanBinaryReader br = new(Data.Span);
		br.Read(out NameIndex);
		br.Read(out Flags);
		br.Read(out Start);
		br.Read(out End);
		br.Read(out Loop);
	}
}
public enum StudioAutolayerFlags
{
	Post = 0x0010,
	Spline = 0x0040,
	XFade = 0x0080,
	NoBlend = 0x0200,
	Local = 0x1000,
	Pose = 0x4000
}
public class MStudioAutoLayer
{
	public const int SIZEOF = 24; // Static offset from MSVC stats (mstudiobone_t size 24, alignment 4)
	public static MStudioAutoLayer FACTORY(object caller, Memory<byte> data) => new(data);
	public short Sequence;
	public short Pose;
	private int  flags;
	public float Start;
	public float Peak;
	public float Tail;
	public float End;
	public MStudioAutoLayer(Memory<byte> data) {
		SpanBinaryReader br = new(data.Span);
		br.Read(out Sequence);
		br.Read(out Pose);
		br.Read(out flags);
		br.Read(out Start);
		br.Read(out Peak);
		br.Read(out Tail);
		br.Read(out End);
	}
	public StudioAutolayerFlags Flags => (StudioAutolayerFlags)Flags;
}
public class MStudioSeqDesc
{
	public const int SIZEOF = 212; // Static offset from MSVC stats (mstudiobone_t size 212, alignment 4)
	public static MStudioSeqDesc FACTORY(object caller, Memory<byte> data) => new((StudioHeader)caller ?? throw new NullReferenceException(), data);

	public readonly StudioHeader hdr;
	public StudioHeader Studiohdr() => hdr;
	public Memory<byte> Data;

	public int LabelIndex;
	public string? labelCache;
	public string Label() => Studio.ProduceASCIIString(ref labelCache, Data.Span[LabelIndex..]);

	public int ActivityNameIndex;
	public string? activityNameCache;
	public string ActivityName() => Studio.ProduceASCIIString(ref activityNameCache, Data.Span[ActivityNameIndex..]);

	private int flags;
	public StudioAnimSeqFlags Flags => (StudioAnimSeqFlags)flags;

	public int Activity;
	public int ActWeight;

	public int NumEvents;
	public int EventIndex;
	// TODO: Events

	public Vector3 BBMin;
	public Vector3 BBMax;

	public int NumBlends;
	public int AnimIndexIndex;
	public int Anim(int x, int y) {
		if (x >= GroupSize[0]) x = GroupSize[0] - 1;
		if (y >= GroupSize[1]) y = GroupSize[1] - 1;

		int offset = y * GroupSize[0] + x;
		Span<short> blends = Data.Span[AnimIndexIndex..].Cast<byte, short>();
		return blends[offset];
	}

	public int MovementIndex;
	public InlineArray2<int> GroupSize;
	public InlineArray2<int> ParamIndex;
	public InlineArray2<float> ParamStart;
	public InlineArray2<float> ParamEnd;
	public int ParamParent;

	public float FadeInTime;
	public float FadeOutTime;

	public int LocalEntryNode;
	public int LocalExitNode;
	public int NodeFlags;

	public float EntryPhase;
	public float ExitPhase;

	public float LastFrame;

	public int NextSeq;
	public int Pose;

	public int NumIKRules;

	public int NumAutoLayers;
	public int AutoLayerIndex;
	MStudioAutoLayer[]? autolayerCache;
	public MStudioAutoLayer Autolayer(int i)
		=> Studio.ProduceArrayIdx(this, ref autolayerCache, NumAutoLayers, AutoLayerIndex, i, MStudioAutoLayer.SIZEOF, Data, MStudioAutoLayer.FACTORY);


	public int WeightListIndex;
	public ref float Boneweight(int i)
		=> ref Data.Span[WeightListIndex..].Cast<byte, float>()[i];
	public float Weight(int i) => Boneweight(i);

	public int PoseKeyIndex;
	public ref float PoseKeyRef(int param, int anim) => ref Data.Span[PoseKeyIndex..].Cast<byte, float>()[param * GroupSize[0] + anim];
	public float PoseKey(int param, int anim) => PoseKeyRef(param, anim);

	public int NumIKLocks;
	public int IKLockIndex;

	public int KeyValueIndex;
	public int KeyValueSize;

	public int CyclePoseIndex;

	public int ActivityModifierIndex;
	public int NumActivityModifiers;

	public MStudioSeqDesc() { hdr = null!; }
	public MStudioSeqDesc(StudioHeader hdr, Memory<byte> data) {
		this.hdr = hdr;
		Data = data;
		SpanBinaryReader br = new(Data.Span);

		br.Read<int>(); // index not needed
		br.Read(out LabelIndex);
		br.Read(out ActivityNameIndex);
		br.Read(out flags);
		br.Read(out Activity);
		br.Read(out ActWeight);
		br.Read(out NumEvents);
		br.Read(out EventIndex);
		br.Read(out BBMin);
		br.Read(out BBMax);
		br.Read(out NumBlends);
		br.Read(out AnimIndexIndex);
		br.Read(out MovementIndex);
		br.ReadInto<int>(GroupSize);
		br.ReadInto<int>(ParamIndex);
		br.ReadInto<float>(ParamStart);
		br.ReadInto<float>(ParamEnd);
		br.Read(out ParamParent);
		br.Read(out FadeInTime);
		br.Read(out FadeOutTime);
		br.Read(out LocalEntryNode);
		br.Read(out LocalExitNode);
		br.Read(out NodeFlags);
		br.Read(out EntryPhase);
		br.Read(out ExitPhase);
		br.Read(out LastFrame);
		br.Read(out NextSeq);
		br.Read(out Pose);
		br.Read(out NumIKRules);
		br.Read(out NumAutoLayers);
		br.Read(out AutoLayerIndex);
		br.Read(out WeightListIndex);
		br.Read(out PoseKeyIndex);
		br.Read(out NumIKLocks);
		br.Read(out IKLockIndex);
		br.Read(out KeyValueIndex);
		br.Read(out KeyValueSize);
		br.Read(out CyclePoseIndex);
		br.Read(out ActivityModifierIndex);
		br.Read(out NumActivityModifiers);
	}
}

/// <summary>
/// Analog of CStudioHdr
/// </summary>
public class StudioHdr
{
	public bool IsVirtual() => vModel != null;
	public bool IsValid() => studioHdr != null;
	public bool IsReadyForAccess() => studioHdr != null;

	public VirtualModel GetVirtualModel() => vModel!;
	public StudioHeader GetRenderHdr() => studioHdr!;

	private StudioHeader? studioHdr;
	private VirtualModel? vModel;
	public int NumBones() => studioHdr!.NumBones;
	public int BoneFlags(int i) => boneFlags[i];
	public int BoneParent(int i) => boneParent[i];
	public MStudioBone Bone(int i) => studioHdr!.Bone(i);
	/// <summary>
	/// Forces a preload of all bones into class views!
	/// </summary>
	/// <returns></returns>
	public Span<MStudioBone> Bones() => studioHdr.Bones();


	readonly List<StudioHeader> StudioHdrCache = [];

	public StudioHdrFlags Flags() => studioHdr!.Flags;

	public void Init(StudioHeader? studioHdr, IMDLCache mdlcache) {
		this.studioHdr = studioHdr;

		this.vModel = null;
		StudioHdrCache.Clear();

		if (this.studioHdr == null)
			return;

		boneFlags.EnsureCount(NumBones());
		boneParent.EnsureCount(NumBones());
		for (int i = 0; i < NumBones(); i++) {
			boneFlags[i] = Bone(i).Flags;
			boneParent[i] = Bone(i).Parent;
		}
	}

	readonly List<int> boneFlags = [];
	readonly List<int> boneParent = [];

	public bool SequencesAvailable() {
		if (studioHdr!.NumIncludeModels == 0) {
			return true;
		}

		if (vModel == null) {
			return (ResetVModel(studioHdr.GetVirtualModel()) != null);
		}
		else
			return true;
	}

	private VirtualModel? ResetVModel(VirtualModel? virtualModel) {
		if (virtualModel != null) {
			vModel = virtualModel;
			StudioHdrCache!.EnsureCountDefault(vModel.Group.Count());

			for (int i = 0; i < StudioHdrCache.Count; i++)
				StudioHdrCache[i] = null!;

			return vModel;
		}
		else {
			vModel = null;
			return null;
		}
	}

	public MStudioLinearBone? LinearBones() {
		return studioHdr!.LinearBones();
	}

	public int GetNumSeq() => vModel == null ? studioHdr!.NumLocalSeq : vModel.Seq.Count;

	public StudioHeader GroupStudioHdr(int i) {
		StudioHeader studioHdr = StudioHdrCache[i];
		return studioHdr; // todo: further validation needed
	}

	static readonly MStudioSeqDesc s_nil_seq = new();
	public MStudioSeqDesc Seqdesc(int i) {
		if (i < 0 || i >= GetNumSeq()) {
			if (GetNumSeq() <= 0)
				return s_nil_seq;

			i = 0;
		}

		if (vModel == null)
			return this.studioHdr!.LocalSeqdesc(i);

		StudioHeader studioHdr = GroupStudioHdr(vModel.Seq[i].Group);

		return studioHdr.LocalSeqdesc(vModel.Seq[i].Index);
	}

	public MStudioAnimDesc Animdesc(int i) {
		if (vModel == null)
			return this.studioHdr!.LocalAnimdesc(i);

		if (vModel.Pose[i].Group == 0)
			return this.studioHdr!.LocalAnimdesc(vModel.Pose[i].Index);

		StudioHeader studioHdr = GroupStudioHdr(vModel.Pose[i].Group);
		return studioHdr.LocalAnimdesc(vModel.Pose[i].Index);
	}

	public MStudioPoseParamDesc PoseParameter(int i) {
		if (vModel == null)
			return this.studioHdr!.LocalPoseParameter(i);

		if (vModel.Pose[i].Group == 0)
			return this.studioHdr!.LocalPoseParameter(vModel.Pose[i].Index);

		StudioHeader studioHdr = GroupStudioHdr(vModel.Pose[i].Group);
		return studioHdr.LocalPoseParameter(vModel.Pose[i].Index);
	}

	public int GetSharedPoseParameter(int sequence, int localPose) {
		if (vModel == null)
			return localPose;

		if (localPose == -1)
			return localPose;

		Assert(vModel != null);

		int group = vModel.Seq[sequence].Group;
		VirtualGroup? pGroup = vModel.Group.IsValidIndex(group) ? vModel.Group[group] : null;

		return pGroup != null ? pGroup.MasterPose[localPose] : localPose;
	}

	public int iRelativeAnim(int baseseq, int relanim) {
		if (vModel == null)
			return relanim;

		VirtualGroup group = vModel.Group[vModel.Seq[baseseq].Group];
		return group.MasterAnim[relanim];
	}

	public string Name() {
		return studioHdr!.GetName();
	}

	public int GetNumPoseParameters() {
		if (vModel == null) {
			if (studioHdr != null)
				return studioHdr.NumLocalPoseParameters;
			else
				return 0;
		}

		return vModel.Pose.Count;
	}

	internal int RelativeSeq(int baseseq, int relseq) {
		if (vModel == null)
			return relseq;

		VirtualGroup group = vModel.Group[vModel.Seq[baseseq].Group];
		return group.MasterSeq[relseq];
	}
}

public class MStudioBone
{
	public const int SIZEOF = 216; // Static offset from MSVC stats (mstudiobone_t size 216, alignment 4)

	Memory<byte> Data;
	public int NameIndex;
	public int Parent;
	public InlineArray6<int> BoneController;
	public Vector3 Position;
	public Quaternion Quat;
	public RadianEuler Rot;
	public Vector3 PosScale;
	public Vector3 RotScale;
	public Matrix3x4 PoseToBone;
	public Quaternion Alignment;
	public int Flags;
	public int ProcType;
	public int ProcIndex;
	public int PhysicsBone;
	public int SurfacePropIdx;
	public int Contents;
	public InlineArray8<int> Unused;

	string? nameCache;
	public string Name() => Studio.ProduceASCIIString(ref nameCache, Data.Span[NameIndex..]);

	public MStudioBone(Memory<byte> data) {
		Data = data;

		SpanBinaryReader br = new(data.Span);
		br.Read(out NameIndex);
		br.Read(out Parent);
		br.ReadInto<int>(BoneController);
		br.Read(out Position);
		br.Read(out Quat);
		br.Read(out Rot);
		br.Read(out PosScale);
		br.Read(out RotScale);
		br.Read(out PoseToBone);
		br.Read(out Alignment);
		br.Read(out Flags);
		br.Read(out ProcType);
		br.Read(out ProcIndex);
		br.Read(out PhysicsBone);
		br.Read(out SurfacePropIdx);
		br.Read(out Contents);
	}
}

public class MStudioLinearBone
{
	public const int SIZEOF = 64; // Static offset from MSVC stats (mstudiobonelinear_t size 64, alignment 4)
	private Memory<byte> Data;

	public int NumBones;
	public int FlagsIndex;
	public int ParentIndex;
	public int PosIndex;
	public int QuatIndex;
	public int RotIndex;
	public int PoseToBoneIndex;
	public int PosScaleIndex;
	public int RotScaleIndex;
	public int QAlignmentIndex;

	public MStudioLinearBone(Memory<byte> data) {
		Data = data;
		SpanBinaryReader br = new(data.Span);

		br.Read(out NumBones);
		br.Read(out FlagsIndex);
		br.Read(out ParentIndex);
		br.Read(out PosIndex);
		br.Read(out QuatIndex);
		br.Read(out RotIndex);
		br.Read(out PoseToBoneIndex);
		br.Read(out PosScaleIndex);
		br.Read(out RotScaleIndex);
		br.Read(out QAlignmentIndex);
	}

	public int Flags(int i) => Data.Span[FlagsIndex..].Cast<byte, int>()[i];
	public ref int RefFlags(int i) => ref Data.Span[FlagsIndex..].Cast<byte, int>()[i];
	public int Parent(int i) => Data.Span[ParentIndex..].Cast<byte, int>()[i];
	public Vector3 Pos(int i) => Data.Span[PosIndex..].Cast<byte, Vector3>()[i];
	public Quaternion Quat(int i) => Data.Span[QuatIndex..].Cast<byte, Quaternion>()[i];
	public RadianEuler Rot(int i) => Data.Span[RotIndex..].Cast<byte, RadianEuler>()[i];
	public Matrix3x4 PoseToBone(int i) => Data.Span[PoseToBoneIndex..].Cast<byte, Matrix3x4>()[i];
	public Vector3 PosScale(int i) => Data.Span[PosScaleIndex..].Cast<byte, Vector3>()[i];
	public Vector3 RotScale(int i) => Data.Span[RotScaleIndex..].Cast<byte, Vector3>()[i];
	public Quaternion Alignment(int i) => Data.Span[QAlignmentIndex..].Cast<byte, Quaternion>()[i];
}

/// <summary>
/// Analog of studiohdr_t
/// </summary>
public class StudioHeader
{
	private StudioHeader() { }
	public StudioHeader(Memory<byte> data) {
		Data = data;
	}
	public readonly Memory<byte> Data;

	public int ID;
	public int Version;
	public int Checksum;
	public InlineArray64<byte> Name;
	public string? nameCache;
	public string GetName() => Studio.ProduceASCIIString(ref nameCache, Name);
	public int Length;

	public Vector3 EyePosition;
	public Vector3 IllumPosition;
	public Vector3 HullMin;
	public Vector3 HullMax;
	public Vector3 ViewBoundingBoxMin;
	public Vector3 ViewBoundingBoxMax;
	public StudioHdrFlags Flags;

	public int NumBones;
	public int BoneIndex;
	MStudioBone[]? studioBoneCache;
	bool preloadedBones = false;

	public MStudioBone Bone(int i) {
		if (studioBoneCache == null)
			studioBoneCache = new MStudioBone[NumBones];

		return studioBoneCache[i] ??= new(Data[(BoneIndex + (i * MStudioBone.SIZEOF))..]);
	}

	public Span<MStudioBone> Bones() {
		if (preloadedBones)
			return studioBoneCache;
		else {
			// Load any bones that haven't been loaded yet.
			for (int i = 0; i < NumBones; i++)
				Bone(i);

			// Cache that all bones are good
			preloadedBones = true;
			return studioBoneCache;
		}
	}

	public int NumBoneControllers;
	public int BoneControllerIndex;

	public int NumHitboxSets;
	public int HitboxSetIndex;

	public int NumLocalAnim;
	public int LocalAnimIndex;
	MStudioAnimDesc[]? animDescs;
	internal MStudioAnimDesc LocalAnimdesc(int i) {
		if (i < 0 || i >= NumLocalAnim)
			i = 0;

		return Studio.ProduceArrayIdx(this, ref animDescs, NumLocalAnim, LocalAnimIndex, i, MStudioAnimDesc.SIZEOF, Data, MStudioAnimDesc.FACTORY);
	}

	public int NumLocalSeq;
	public int LocalSeqIndex;
	MStudioSeqDesc[]? seqDescs;
	internal MStudioSeqDesc LocalSeqdesc(int i) {
		if (i < 0 || i >= NumLocalSeq)
			i = 0;

		return Studio.ProduceArrayIdx(this, ref seqDescs, NumLocalSeq, LocalSeqIndex, i, MStudioSeqDesc.SIZEOF, Data, MStudioSeqDesc.FACTORY);
	}

	public int ActivityListVersion;
	public int EventsIndexed;

	public int NumTextures;
	public int TextureIndex;
	MStudioTexture[]? studioTextures;

	public MStudioTexture Texture(int i) {
		if (studioTextures == null)
			studioTextures = new MStudioTexture[NumTextures];

		return studioTextures[i] ??= new(Data[(TextureIndex + (MStudioTexture.SIZE_OF_ONE * i))..]);

		return Studio.ProduceArrayIdx(this, ref studioTextures, NumTextures, TextureIndex, i, MStudioTexture.SIZE_OF_ONE, Data, MStudioTexture.FACTORY);
	}

	public int NumCDTextures;
	public int CDTextureIndex;
	string[]? cdTextureCache;
	public ReadOnlySpan<char> CDTexture(int i) {
		if (cdTextureCache == null)
			cdTextureCache = new string[NumCDTextures];

		if (cdTextureCache[i] != null)
			return cdTextureCache[i];

		Span<byte> span = Data.Span;

		var offsetTable = MemoryMarshal.Cast<byte, int>(span[CDTextureIndex..]);
		int stringOffset = offsetTable[i];
		var strBytes = span[stringOffset..];

		using ASCIIStringView ascii = new(strBytes);
		cdTextureCache[i] = new(ascii);
		return cdTextureCache[i];
	}

	MStudioBodyParts[]? bodyPartCache;
	public MStudioBodyParts BodyPart(int i) {
		return Studio.ProduceArrayIdx(this, ref bodyPartCache, NumBodyParts, BodyPartIndex, i, MStudioBodyParts.SIZEOF, Data, MStudioBodyParts.FACTORY);
	}

	public int NumSkinRef;
	public int NumSkinFamilies;
	public int SkinIndex;

	public Span<short> SkinRef(int i) => Data.Span[SkinIndex..].Cast<byte, short>()[i..];

	public int NumBodyParts;
	public int BodyPartIndex;

	public int NumLocalAttachments;
	public int LocalAttachmentIndex;

	public int NumLocalNodes;
	public int LocalNodeIndex;
	public int LocalNodeNameIndex;

	public int NumFlexDesc;
	public int FlexDescIndex;

	public int NumFlexControllers;
	public int FlexControllerIndex;

	public int NumFlexRules;
	public int FlexRuleIndex;

	public int NumIKChains;
	public int IKChainIndex;

	public int NumMouths;
	public int MouthIndex;

	public int NumLocalPoseParameters;
	public int LocalPoseParamIndex;
	MStudioPoseParamDesc[]? poseParamDescCache;
	public MStudioPoseParamDesc LocalPoseParameter(int i)
		=> Studio.ProduceArrayIdx(this, ref poseParamDescCache, NumLocalPoseParameters, LocalPoseParamIndex, i, MStudioPoseParamDesc.SIZEOF, Data, MStudioPoseParamDesc.FACTORY);

	public int SurfacePropIndex;
	public int KeyValueIndex;
	public int KeyValueSize;

	public int NumLocalIKAutoplayLocks;
	public int LocalIKAutoplayLockIndex;

	public float Mass;
	public int Contents;

	public int NumIncludeModels;
	public int IncludeModelIndex;

	public MDLHandle_t VirtualModel;

	public int SzAnimBlockNameIndex;
	public string? animBlockNameCache;
	public string AnimBlockName() => Studio.ProduceASCIIString(ref animBlockNameCache, Data.Span[SzAnimBlockNameIndex..]);

	public int NumAnimBlocks;
	public int AnimBlockIndex;
	MStudioAnimBlock[]? animBlockCache;
	public MStudioAnimBlock AnimBlock(int i)
		=> Studio.ProduceArrayIdx(this, ref animBlockCache, NumAnimBlocks, AnimBlockIndex, i, MStudioAnimBlock.SIZEOF, Data, MStudioAnimBlock.FACTORY);

	public int AnimBlockModel;

	public Memory<byte> GetAnimBlock(int block) {
		return modelinfo.GetAnimBlock(this, block);
	}

	public int BoneTableByNameIndex;
	public int VertexBase;
	public int IndexBase;
	public byte ConstDirectionalLightDot;
	public byte RootLOD;
	public byte NumAllowedRootLODs;
	public int NumFlexControllerUI;
	public int FlexControllerUIIndex;
	public float VertAnimFixedPointScale;

	public int StudioHDR2Index;
	StudioHeader2? studioHdr2;
	public StudioHeader2 StudioHdr2() => studioHdr2 ??= new(Data[StudioHDR2Index..]);

	public MStudioLinearBone? LinearBones() => StudioHDR2Index != 0 ? StudioHdr2().LinearBones() : null;

	internal VirtualModel? GetVirtualModel() {
		if (NumIncludeModels == 0)
			return null;
		return modelinfo.GetVirtualModel(this);
	}
}
