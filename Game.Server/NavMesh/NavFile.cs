using Source.Common.Commands;

using System.Numerics;

namespace Game.Server.NavMesh;

static class NavFile
{
	/// IMPORTANT: If this version changes, the swap function in makegamedata must be updated to match.
	public const int NavCurrentVersion = 16;
}

public class PlaceDirectory
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

	void AddPlace(NavPlace place) {
		if (place == Nav.UndefinedPlace) {
			HasUnnamedAreas = true;
			return;
		}


		Assert(place < 1000);

		if (!IsKnown(place))
			Directory.Add(place);
	}

	public NavPlace IndexToPlace(ushort entry) {
		if (entry == 0)
			return Nav.UndefinedPlace;

		int index = entry - 1;
		if (index < 0 || index >= Directory.Count) {
			AssertMsg(false, "PlaceDirectory::IndexToPlace: Invalid entry");
			return Nav.UndefinedPlace;
		}

		return Directory[index];
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

	public NavErrorType Load(BinaryReader fileBuffer, uint version, uint subVersion) {
		ID = fileBuffer.ReadUInt32();

		if (ID >= NextID)
			NextID = ID + 1;

		if (version <= 8)
			AttributeFlags = fileBuffer.ReadByte();
		else if (version < 13)
			AttributeFlags = fileBuffer.ReadUInt16();
		else
			AttributeFlags = fileBuffer.ReadInt32();

		NWCorner = new Vector3(fileBuffer.ReadSingle(), fileBuffer.ReadSingle(), fileBuffer.ReadSingle());
		SECorner = new Vector3(fileBuffer.ReadSingle(), fileBuffer.ReadSingle(), fileBuffer.ReadSingle());

		Center.X = (NWCorner.X + SECorner.X) / 2.0f;
		Center.Y = (NWCorner.Y + SECorner.Y) / 2.0f;
		Center.Z = (NWCorner.Z + SECorner.Z) / 2.0f;

		if ((SECorner.X - NWCorner.X) > 0.0f && (SECorner.Y - NWCorner.Y) > 0.0f) {
			InvDXCorners = 1.0f / (SECorner.X - NWCorner.X);
			InvDYCorners = 1.0f / (SECorner.Y - NWCorner.Y);
		}
		else {
			InvDXCorners = InvDYCorners = 0;

			DevWarning($"Degenerate Navigation Area #{ID} at setpos {Center.X} {Center.Y} {Center.Z}\n");
		}

		NEZ = fileBuffer.ReadSingle();
		SWZ = fileBuffer.ReadSingle();

		CheckWaterLevel();

		for (int d = 0; d < (int)NavDirType.NumDirections; d++) {
			uint count = fileBuffer.ReadUInt32();

			for (uint i = 0; i < count; ++i) {
				NavConnect connect = new() {
					ID = fileBuffer.ReadUInt32()
				};

				if (connect.ID != ID)
					Connect[d].Add(connect);
			}
		}

		byte hidingSpotCount = fileBuffer.ReadByte();
		if (version == 1) {
			for (int h = 0; h < hidingSpotCount; ++h) {
				Vector3 pos = new Vector3(fileBuffer.ReadSingle(), fileBuffer.ReadSingle(), fileBuffer.ReadSingle());
				HidingSpot spot = NavMesh.Instance!.CreateHidingSpot();
				spot.SetPosition(pos);
				spot.SetFlags(HidingSpotFlags.InCover);
				HidingSpots.Add(spot);
			}
		}
		else {
			for (int h = 0; h < hidingSpotCount; ++h) {
				HidingSpot spot = NavMesh.Instance!.CreateHidingSpot();
				spot.Load(fileBuffer, version);
				HidingSpots.Add(spot);
			}
		}

		if (version < 15) {
			int nToEat = fileBuffer.ReadByte();

			for (int a = 0; a < nToEat; ++a) {
				fileBuffer.ReadUInt32();
				fileBuffer.ReadUInt32();
				fileBuffer.ReadByte();
				fileBuffer.ReadUInt32();
				fileBuffer.ReadByte();
			}
		}

		uint encounterCount = fileBuffer.ReadUInt32();
		if (version < 3) {
			for (uint e = 0; e < encounterCount; ++e) {
				SpotEncounter encounter = new();

				encounter.From.ID = fileBuffer.ReadUInt32();
				encounter.To.ID = fileBuffer.ReadUInt32();

				encounter.Path.From = new Vector3(fileBuffer.ReadSingle(), fileBuffer.ReadSingle(), fileBuffer.ReadSingle());
				encounter.Path.To = new Vector3(fileBuffer.ReadSingle(), fileBuffer.ReadSingle(), fileBuffer.ReadSingle());

				byte spotCount = fileBuffer.ReadByte();
				for (int s = 0; s < spotCount; ++s) {
					fileBuffer.ReadSingle();
					fileBuffer.ReadSingle();
					fileBuffer.ReadSingle();
					fileBuffer.ReadSingle();
				}
			}

			return NavErrorType.Ok;
		}

		for (uint e = 0; e < encounterCount; ++e) {
			SpotEncounter encounter = new();

			encounter.From.ID = fileBuffer.ReadUInt32();
			encounter.FromDir = (NavDirType)fileBuffer.ReadByte();
			encounter.To.ID = fileBuffer.ReadUInt32();
			encounter.ToDir = (NavDirType)fileBuffer.ReadByte();

			byte spotCount = fileBuffer.ReadByte();

			SpotOrder order = new();
			for (int s = 0; s < spotCount; ++s) {
				order.ID = fileBuffer.ReadUInt32();
				order.T = fileBuffer.ReadByte() / 255.0f;
				encounter.Spots.Add(order);
			}

			SpotEncounters.Add(encounter);
		}

		if (version < 5)
			return NavErrorType.Ok;

		ushort entry = fileBuffer.ReadUInt16();
		SetPlace(NavMesh.placeDirectory.IndexToPlace(entry));

		if (version < 7)
			return NavErrorType.Ok;

		for (int dir = 0; dir < (int)NavLadder.LadderDirectionType.NumLadderDirections; ++dir) {
			uint count = fileBuffer.ReadUInt32();
			for (uint i = 0; i < count; ++i) {
				NavLadderConnect connect = new() {
					ID = fileBuffer.ReadUInt32()
				};

				bool alreadyConnected = false;
				foreach (var ladder in Ladder[dir]) {
					if (ladder.ID == connect.ID) {
						alreadyConnected = true;
						break;
					}
				}

				if (!alreadyConnected)
					Ladder[dir].Add(connect);
			}
		}

		if (version < 8)
			return NavErrorType.Ok;

		for (int i = 0; i < MAX_NAV_TEAMS; ++i)
			EarliestOccupyTime[i] = fileBuffer.ReadSingle();

		if (version < 11)
			return NavErrorType.Ok;

		for (int i = 0; i < (int)NavCornerType.NumCorners; ++i)
			LightIntensity[i] = fileBuffer.ReadSingle();

		if (version < 16)
			return NavErrorType.Ok;

		uint visibleAreaCount = fileBuffer.ReadUInt32();
		for (uint j = 0; j < visibleAreaCount; ++j) {
			AreaBindInfo info = new() {
				ID = fileBuffer.ReadUInt32(),
				Attributes = fileBuffer.ReadByte()
			};

			PotentiallyVisibleAreas.Add(info);
		}

		InheritVisibilityFrom.ID = fileBuffer.ReadUInt32();

		return NavErrorType.Ok;
	}

	NavErrorType PostLoad() {
		throw new NotImplementedException();
	}

	void ComputeEarliestOccupyTimes() { }
}

public partial class NavMesh
{
	public static PlaceDirectory placeDirectory = new();
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
				if (engine.IsDedicatedServer())
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

		uint count = buffer.ReadUInt32();

		if (count == 0)
			return NavErrorType.InvalidFile;

		Extent extent = new();
		extent.Lo.X = 9999999999.9f;
		extent.Lo.Y = 9999999999.9f;
		extent.Hi.X = -9999999999.9f;
		extent.Hi.Y = -9999999999.9f;

		Extent areaExtent = new();
		for (int i = 0; i < count; ++i) {
			NavArea area = Instance!.CreateArea();
			area.Load(buffer, version, subVersion);
		}

		AllocateGrid(extent.Lo.X, extent.Hi.X, extent.Lo.Y, extent.Hi.Y);

		// foreach

		if (version >= 6) {
			count = buffer.ReadUInt32();
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