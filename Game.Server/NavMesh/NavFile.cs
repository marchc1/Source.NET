using Source.Common.Commands;

namespace Game.Server.NavMesh;

static class NavFile
{
	/// IMPORTANT: If this version changes, the swap function in makegamedata must be updated to match.
	public const int NavCurrentVersion = 16;
}

class PlaceDirectory
{
	List<NavPlace> Directory = [];
	bool HasUnnamedAreas;

	public PlaceDirectory() => Reset();

	public void Reset() {
		Directory.Clear();
		HasUnnamedAreas = false;
	}

	bool IsKnown(NavPlace place) => Directory.Contains(place);

	ushort GetIndex(NavPlace place) { // todo IndexType
		if (place == Nav.UndefinedPlace)
			return 0;

		int index = Directory.IndexOf(place);
		if (index < 0) {
			AssertMsg(false, "PlaceDirectory::GetIndex failure");
			return 0;
		}

		return (ushort)(index + 1);
	}

	void AddPlace(NavPlace place) { }

	NavPlace IndexToPlace(ushort entry) {
		throw new NotImplementedException();
	}

	void Save(ReadOnlySpan<char> fileBuffer) { }

	public void Load(BinaryReader fileBuffer, uint version) {
		ushort count = fileBuffer.ReadUInt16();
		Directory.Clear();

		Span<char> placeName = stackalloc char[256];

		for (int i = 0; i < count; ++i) {
			ushort len = fileBuffer.ReadUInt16();
			len = Math.Min(len, (ushort)256);

			for (int j = 0; j < len; ++j)
				placeName[j] = (char)fileBuffer.ReadByte();

			NavPlace place = NavMesh.Instance!.NameToPlace(placeName[..len]);
			if (place == Nav.UndefinedPlace)
				DevWarning($"Warning: NavMesh place {placeName[..len]} is undefined?\n");

			AddPlace(place);
		}

		if (version > 11)
			HasUnnamedAreas = fileBuffer.ReadByte() != 0;
	}

	public List<NavPlace> GetPlaces() => Directory;
	bool HasUnnamedPlaces() => HasUnnamedAreas;
}

struct OneWayLink
{
	public NavArea DestArea;
	public NavArea Area;
	public int BackD;
	public static int Compare(OneWayLink lhs, OneWayLink rhs) {
		int result = lhs.DestArea.GetHashCode() - rhs.DestArea.GetHashCode();
		if (result != 0)
			return result;

		return lhs.BackD - rhs.BackD;
	}
}


public partial class NavArea
{
	void Save(ReadOnlySpan<char> fileBuffer, uint version) { }

	NavErrorType Load(ReadOnlySpan<char> fileBuffer, uint version, uint subVersion) {
		throw new NotImplementedException();
	}

	NavErrorType PostLoad() {
		throw new NotImplementedException();
	}

	void ComputeEarliestOccupyTimes() { }
}

public partial class NavMesh
{
	static PlaceDirectory placeDirectory = new();
	void ComputeBattlefrontAreas() { }

	ReadOnlySpan<char> GetFilename() {
		throw new NotImplementedException();
	}

	bool Save() {
		throw new NotImplementedException();
	}

	List<NavPlace> GetPlacesFromNavFile(bool hasUnnamedPlaces) {
		throw new NotImplementedException();
	}

	NavErrorType GetNavDataFromFile(Span<byte> outBuffer, ref bool navDataFromBSP) {
		Span<char> filename = stackalloc char[MAX_PATH];
		// sprintf(filename, "maps/%s.nav").S(gpGlobals.MapName);
		sprintf(filename, "maps/gm_flatgrass.nav");//.S(gpGlobals.MapName);

		// this ignores .nav files embedded in the .bsp ...
		if (!filesystem.ReadFile(filename, "MOD", outBuffer, 0)) {
			// ... and this looks for one if it's the only one around.
			if (!filesystem.ReadFile(filename, "BSP", outBuffer, 0)) {
				// Finally, check for the special embed name for in-BSP nav meshes only
				if (!filesystem.ReadFile("maps\\embed.nav", "BSP", outBuffer, 0))
					return NavErrorType.CantAccessFile;
			}

			navDataFromBSP = true;
		}

		return NavErrorType.Ok;
	}

	NavErrorType Load() {
		Reset();
		placeDirectory.Reset();

		// NavVectorNoEditAllocator.Reset();

		// GameRules.OnNavMeshLoad();

		NavArea.NextID = 1;

		bool navIsInBsp = false;

		Span<byte> fileBuffer = stackalloc byte[272 * 1024]; // FIXME: needs to grow if needed
		NavErrorType readResult = GetNavDataFromFile(fileBuffer, ref navIsInBsp);
		if (readResult != NavErrorType.Ok)
			return readResult;

		using var buffer = new BinaryReader(new MemoryStream(fileBuffer.ToArray()));

		uint magic = buffer.ReadUInt32();
		if (magic != Nav.NavMagicNumber)
			return NavErrorType.InvalidFile;

		uint version = buffer.ReadUInt32();
		if (version > NavFile.NavCurrentVersion)
			return NavErrorType.BadFileVersion;

		uint subVersion = 0;
		if (version >= 10) {
			subVersion = buffer.ReadUInt32();
			if (fileBuffer.Length < 12)
				return NavErrorType.InvalidFile;
		}

		if (version >= 4) {
			uint saveBspSize = buffer.ReadUInt32();

			Span<char> bspFilename = stackalloc char[260];
			sprintf(bspFilename, "maps/%s.bsp").S(gpGlobals.MapName);

			uint bspSize = (uint)filesystem.Size(bspFilename);

			if (bspSize != saveBspSize && !navIsInBsp) {
				if (/*engine.IsDedicatedServer()*/ false) // todo
					DevMsg("The Navigation Mesh was built using a different version of this map.\n");
				else
					DevWarning("The Navigation Mesh was built using a different version of this map.\n");

				IsOutOfDate = true;
			}
		}

		if (version >= 14)
			IsAnalyzed = buffer.ReadByte() != 0;
		else
			IsAnalyzed = false;

		if (version >= 5)
			placeDirectory.Load(buffer, version);

		ushort count = buffer.ReadUInt16();

		if (count == 0)
			return NavErrorType.InvalidFile;

		Extent extent = new();
		extent.Lo.X = 9999999999.9f;
		extent.Lo.Y = 9999999999.9f;
		extent.Hi.X = -9999999999.9f;
		extent.Hi.Y = -9999999999.9f;

		// Instance.PreLoadAreas(count);

		Extent areaExtent = new();
		for (int i = 0; i < count; ++i) {
			// NavArea area = Instance.CreateArea();
			// area.Load(buffer, version, subVersion);
		}

		AllocateGrid(extent.Lo.X, extent.Hi.X, extent.Lo.Y, extent.Hi.Y);

		// foreach

		if (version >= 6) {
			count = buffer.ReadUInt16();
			// ladders
		}
		else {
			// buildladders
		}

		// MarkStairAreas();

		NavErrorType loadResult = PostLoad(version);

		// WarnIfMeshNeedsAnalysis();

		return loadResult;
	}

	NavErrorType PostLoad(uint version) {
		throw new NotImplementedException();
	}

#if DEBUG
	static ConVar loadthenavrightnowplease = new("0", 0, "", callback: (_, in _) => {
		Console.WriteLine("Loading nav mesh...");
		Instance ??= new NavMesh();
		Instance?.Load();
	});
#endif
}