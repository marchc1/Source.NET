using System.Numerics;

namespace Source.Common;

public static class Studio
{
	public const int STUDIO_VERSION = 48;

	public const int MAXSTUDIOTRIANGLES = 65536;
	public const int MAXSTUDIOVERTS = 65536;
	public const int MAXSTUDIOFLEXVERTS = 10000;

	public const int MAXSTUDIOSKINS = 32;
	public const int MAXSTUDIOBONES = 128;
	public const int MAXSTUDIOFLEXDESC = 1024;
	public const int MAXSTUDIOFLEXCTRL = 96;
	public const int MAXSTUDIOPOSEPARAM = 24;
	public const int MAXSTUDIOBONECTRLS = 4;
	public const int MAXSTUDIOANIMBLOCKS = 256;

	public const int MAXSTUDIOBONEBITS = 7;
}

public class VirtualModel {
	// todo
}

public class StudioHDR2 {
	public Memory<byte> Data;

	public int NumSrcBoneTransform;
	public int SrcBoneTransformIndex;
	public int IllumPositionAttachmentIndex;
	public float MaxEyeDeflection;
	public int LinearBoneIndex;
	public int SzNameIndex;
	public int BoneFlexDriverCount;
	public int BoneFlexDriverIndex;
	public InlineArray56<int> Reserved;
}

public class StudioHDR {
	public Memory<byte> Data;

	public int ID;
	public int Version;
	public int Checksum;
	public InlineArray64<char> Name;
	public int Length;

	public Vector3 EyePosition;
	public Vector3 IllumPosition;
	public Vector3 HullMin;
	public Vector3 HullMax;
	public Vector3 ViewBoundingBoxMin;
	public Vector3 ViewBoundingBoxMax;
	public int Flags; // TODO: Enum this

	public int NumBones;
	public int BoneIndex;

	public int NumBoneControllers;
	public int BoneControllerIndex;

	public int NumHitboxSets;
	public int HitboxSetIndex;

	public int NumLocalAnim;
	public int LocalAnimIndex;

	public int NumLocalSeq;
	public int LocalSeqIndex;

	public int ActivityListVersion;
	public int EventsIndexed;

	public int NumTextures;
	public int TextureIndex;

	public int NumCDTextures;
	public int CDTextureIndex;

	public int NumSkinRef;
	public int NumSkinFamilies;
	public int SkinIndex;

	public int NumBodyParts;
	public int BodyPartIndex;

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

	public int SurfacePropIndex;
	public int KeyValueIndex;
	public int KeyValueSize;

	public int NumLocalIKAutoplayLocks;
	public int LocalIKAutoplayLockIndex;

	public int Mass;
	public int Contents;

	public int NumIncludeModels;
	public int IncludeModelIndex;
	
	public int VirtualModel;

	public int SzAnimBlockNameIndex;
	public int NumAnimBlocks;
	public int AnimBlockIndex;
	public int AnimBlockModel;
	
	public int BoneTableByNameIndex;
	public int VertexBase;
	public int IndexBase;
	public byte ConstDirectionalLightDot;
	public byte RootLOD;
	public byte NumALlowedRootLODs;
	byte _UNUSED1;
	public int StudioHDR2Index;
	byte _UNUSED2;
}
