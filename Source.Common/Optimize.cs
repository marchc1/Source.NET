namespace Source.Common;
public struct OptimizedModelFileHeader
{
	public const int OPTIMIZED_MODEL_FILE_VERSION = 7;

	public int Version;
	public int VertcacheSize;
	public ushort MaxBonesPerStrip;
	public ushort MaxBonesPerTri;
	public int MaxBonesPerVert;
	public int CheckSum;
	public int NumLODs;
	public int MaterialReplacementListOffset;
	public int NumBodyPArts;
	public int BodyPartOffset;
}
