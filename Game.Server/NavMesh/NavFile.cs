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

	public NavErrorType PostLoad() {
		NavErrorType error = NavErrorType.Ok;

		for (int dir = 0; dir < (int)NavLadder.LadderDirectionType.NumLadderDirections; ++dir) {
			for (int it = 0; it < Ladder[dir].Count; ++it) {
				NavLadderConnect connect = Ladder[dir][it];

				uint id = connect.ID;

				if (NavMesh.Instance!.GetLadders().Find(x => x == connect.Ladder) == null)
					connect.Ladder = NavMesh.Instance!.GetLadderByID(id);

				if (id != 0 && connect.Ladder == null) {
					Msg("NavArea::PostLoad: Corrupt navigation ladder data. Cannot connect Navigation Areas.\n");
					error = NavErrorType.CorruptData;
				}
			}
		}

		for (int d = 0; d < (int)NavDirType.NumDirections; d++) {
			for (int it = 0; it < Connect[d].Count; ++it) {
				NavConnect connect = Connect[d][it];

				uint id = connect.ID;
				connect.Area = NavMesh.Instance!.GetNavAreaByID(id);
				if (id != 0 && connect.Area == null) {
					Msg("NavArea::PostLoad: Corrupt navigation data. Cannot connect Navigation Areas.\n");
					error = NavErrorType.CorruptData;
				}
				connect.Length = (connect.Area!.GetCenter() - GetCenter()).Length();
			}
		}

		SpotEncounter e;
		for (int it = 0; it < SpotEncounters.Count; ++it) {
			e = SpotEncounters[it];

			e.From.Area = NavMesh.Instance!.GetNavAreaByID(e.From.ID);
			if (e.From.Area == null) {
				Msg("NavArea::PostLoad: Corrupt navigation data. Missing \"from\" Navigation Area for Encounter Spot.\n");
				error = NavErrorType.CorruptData;
			}

			e.To.Area = NavMesh.Instance!.GetNavAreaByID(e.To.ID);
			if (e.To.Area == null) {
				Msg("NavArea::PostLoad: Corrupt navigation data. Missing \"to\" Navigation Area for Encounter Spot.\n");
				error = NavErrorType.CorruptData;
			}

			if (e.From.Area != null && e.To.Area != null) {
				float halfWidth = 0;
				ComputePortal(e.To.Area, e.ToDir, e.Path.To, halfWidth);
				ComputePortal(e.From.Area, e.FromDir, e.Path.From, halfWidth);

				const float eyeHeight = Nav.HalfHumanHeight;
				e.Path.From.Z = e.From.Area.GetZ(e.Path.From) + eyeHeight;
				e.Path.To.Z = e.To.Area.GetZ(e.Path.To) + eyeHeight;
			}

			for (int sit = 0; sit < e.Spots.Count; ++sit) {
				SpotOrder order = e.Spots[sit];

				order.Spot = NavMesh.GetHidingSpotByID(order.ID);
				if (order.Spot == null) {
					Msg("NavArea::PostLoad: Corrupt navigation data. Missing Hiding Spot\n");
					error = NavErrorType.CorruptData;
				}
			}
		}

		for (int it = 0; it < PotentiallyVisibleAreas.Count; ++it) {
			AreaBindInfo info = PotentiallyVisibleAreas[it];

			info.Area = NavMesh.Instance!.GetNavAreaByID(info.ID);
			if (info.Area == null)
				Warning("Invalid area in visible set for area #%d\n", GetID());
		}

		InheritVisibilityFrom.Area = NavMesh.Instance!.GetNavAreaByID(InheritVisibilityFrom.ID);
		Assert(InheritVisibilityFrom.Area != this);

		PotentiallyVisibleAreas.RemoveAll(info => info.Area == null);

		ClearAllNavCostEntities();

#if DEBUG
		Console.WriteLine($"NavArea #{ID} (Connects: {Connect[0].Count} forward, {Connect[1].Count} left, {Connect[2].Count} back, {Connect[3].Count} right)");
#endif

		return error;
	}

	public void ComputeEarliestOccupyTimes() {
		for (int team = 0; team < MAX_NAV_TEAMS; ++team)
			EarliestOccupyTime[team] = 0.0f;
	}
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

	private void GetNavSizeFromFile(out long size) { // HACK, we don't have a growable buffer
		Span<char> filename = stackalloc char[MAX_PATH];  // FIXME: Awaiting singleplayer

		// sprintf(filename, "maps/%s.nav").S(gpGlobals.MapName);
		sprintf(filename, "maps/gm_flatgrass.nav");//.S(gpGlobals.MapName);

		size = filesystem.Size(filename, "MOD");
		if (size == -1) size = filesystem.Size(filename, "BSP");
		if (size == -1) size = filesystem.Size("maps\\embed.nav", "BSP");
	}

	NavErrorType GetNavDataFromFile(Span<byte> outBuffer, ref bool navDataFromBSP) {
		Span<char> filename = stackalloc char[MAX_PATH];  // FIXME: Awaiting singleplayer

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

		NavArea.NextID = 1;

		bool navIsInBsp = false;

		GetNavSizeFromFile(out long navSize);
		if (navSize <= 0)
			return NavErrorType.CantAccessFile;

		Span<byte> fileBuffer = stackalloc byte[(int)navSize];
		NavErrorType readResult = GetNavDataFromFile(fileBuffer, ref navIsInBsp);
		if (readResult != NavErrorType.Ok)
			return readResult;

		using var buffer = new BinaryReader(new MemoryStream(fileBuffer.ToArray(), false));

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

			Span<char> bspFilename = stackalloc char[260]; // FIXME: Awaiting singleplayer

			// sprintf(bspFilename, "maps/%s.bsp").S(gpGlobals.MapName);
			sprintf(bspFilename, "maps/gm_flatgrass.bsp");//.S(gpGlobals.MapName);

			long bspSize = filesystem.Size(bspFilename);

			if (bspSize != saveBspSize && !navIsInBsp) {
				if (engine.IsDedicatedServer())
					DevMsg("The Navigation Mesh was built using a different version of this map.\n");
				else
					DevWarning("The Navigation Mesh was built using a different version of this map.\n");

#if DEBUG
				Console.WriteLine($"BSP size: {bspSize:N0} bytes, expected {saveBspSize:N0} bytes");
#endif

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
			NavArea.TheNavAreas.Add(area);

			area.GetExtent(ref areaExtent);

			if (areaExtent.Lo.X < extent.Lo.X)
				extent.Lo.X = areaExtent.Lo.X;
			if (areaExtent.Lo.Y < extent.Lo.Y)
				extent.Lo.Y = areaExtent.Lo.Y;
			if (areaExtent.Hi.X > extent.Hi.X)
				extent.Hi.X = areaExtent.Hi.X;
			if (areaExtent.Hi.Y > extent.Hi.Y)
				extent.Hi.Y = areaExtent.Hi.Y;
		}

		AllocateGrid(extent.Lo.X, extent.Hi.X, extent.Lo.Y, extent.Hi.Y);

		foreach (NavArea area in NavArea.TheNavAreas)
			AddNavArea(area);

		if (version >= 6) {
			count = buffer.ReadUInt32();
			for (uint i = 0; i < count; ++i) {
				NavLadder ladder = new();
				ladder.Load(buffer, version);
				Ladders.Add(ladder);
			}
		}
		else
			BuildLadders();

		MarkStairAreas();

		NavErrorType loadResult = PostLoad(version);

		// WarnIfMeshNeedsAnalysis();

		return loadResult;
	}

	NavErrorType PostLoad(uint version) {
		foreach (NavArea area in NavArea.TheNavAreas)
			area.PostLoad();

		foreach (HidingSpot spot in HidingSpot.TheHidingSpots)
			spot.PostLoad();

		if (version < 8) {
			foreach (NavArea area in NavArea.TheNavAreas)
				area.ComputeEarliestOccupyTimes();
		}

		ComputeBattlefrontAreas();

		OneWayLink oneWayLink = new();
		List<OneWayLink> oneWayLinks = [];
		foreach (NavArea area in NavArea.TheNavAreas) {
			// todo
		}

		// todo

		ValidateNavAreaConnections();

		// for (int i = 0; i < AvoidanceObstacles.Count; ++i)
		// AvoidanceObstacles[i].OnNavMeshLoaded();

		IsLoaded = true;

		return NavErrorType.Ok;
	}

#if DEBUG
	static ConVar loadthenavrightnowplease = new("0", 0, "", callback: (_, in _) => {
		Console.WriteLine("Loading nav mesh...");
		Instance ??= new NavMesh();
		Instance?.Load();
		Instance?.Update();
	});
#endif
}