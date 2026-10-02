namespace Source.Filesystem.GarrysMod;

public static class AddonFormat
{
	public const uint CompressionSignature = 3203386062;
	public const int TimestampOffset = 13;

	public struct FileEntry
	{
		public string Name;
		public long Size;
		public uint CRC;
		public uint FileNumber;
		public long Offset;
	}
}
