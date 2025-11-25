using Source.Common.Client;
using Source.Common.Formats.BSP;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;
using Source.Common.Utilities;

using System.Numerics;

namespace Source.Common.Engine;

public struct ModelTexInfo
{
	public InlineArray2<Vector4> TextureVecsTexelsPerWorldUnits;
	public InlineArray2<Vector4> LightmapVecsLuxelsPerWorldUnits;
	public float LuxelsPerWorldUnit;
	public float WorldUnitsPerLuxel;
	public Surf Flags;
	public int TexData;
	public ushort TexInfoFlags;
	public IMaterial? Material;
}

public class WorldBrushData
{
	public int NumSubModels;
	public CollisionPlane[]? Planes;
	public int NumPlanes;
	public BSPLeaf[]? Leafs;
	public BSPLeafWaterData[]? LeafWaterData;
	public BSPVertex[]? Vertexes;
	public BSPOccluderData[]? Occluders;
	public BSPOccluderPolyData[]? OccluderPolys;
	public int[]? OccluderVertIndices;
	public ushort[]? VertNormalIndices;
	public int NumVertNormalIndices => VertNormalIndices?.Length ?? 0;
	public Vector3[]? VertNormals;
	public int NumVertNormals => VertNormals?.Length ?? 0;
	public BSPNode[]? Nodes;
	public BSPFace[]? Faces;
	public ushort[]? LeafMinDistToWater;
	public ModelTexInfo[]? TexInfo;
	public int NumTexInfo;
	public int NumTexData;
	public CollisionSurface[]? TexData;
	public ushort[]? VertIndices;
	// TODO: Displacement info
	public BSPWorldLight[]? WorldLights;
	public BSPMPrimitive[]? Primitives;
	public int NumPrimitives => Primitives?.Length ?? 0;
	public BSPMPrimVert[]? PrimVerts;
	public int NumPrimVerts => PrimVerts?.Length ?? 0;
	public ushort[]? PrimIndices;
	public int NumPrimIndices => PrimIndices?.Length ?? 0;
	public BSPArea[]? Areas;
	public BSPAreaPortal[]? AreaPortals;
	public Vector3[]? ClipPortalVerts;
	public BSPCubeMapSample[]? CubemapSamples;
	public int NumSurfaces;
	public BSPMSurface1[]? Surfaces1;
	public BSPMSurface2[]? Surfaces2;
	public BSPSurfaceLighting[]? SurfaceLighting;
	public ColorRGBExp32[]? LightData;
	public BSPMSurfaceNormal[]? SurfaceNormals;
	public object? DispInfos;
	public int NumDispInfos;
}

public struct BrushData
{
	public WorldBrushData? Shared;
	public int FirstModelSurface;
	public int NumModelSurfaces;
	public ushort RenderHandle;
	public ushort FirstNode;
}

public struct SpriteData
{
	public int NumFrames;
	public int Width;
	public int Height;
	public EngineSprite? Sprite;
}


public enum ModelFlag
{
	MaterialProxy = 0x0001,
	Translucent = 0x0002,
	VertexLit = 0x0004,
	TranslucentTwoPass = 0x0008,
	FramebufferTexture = 0x0010,
	HasDLight = 0x0020,
	UsesFBTexture = 0x0100,
	UsesBumpMapping = 0x0200,
	UsesEnvCubemap = 0x0400,
	AmbientBoost = 0x0800,
	DoNotCastShadows = 0x1000
}

/// <summary>
/// Analog of model_t
/// </summary>
public class Model
{
	public FileNameHandle_t FileNameHandle;
	public UtlSymbol StrName;
	public ModelLoaderFlags LoadFlags;
	public int ServerCount;
	public Memory<IMaterial> Materials;

	public ModelType Type;
	public ModelFlag Flags;

	public Vector3 Mins, Maxs;
	public float Radius;

	public BrushData Brush;
	public MDLHandle_t Studio;
	public SpriteData Sprite;
}
