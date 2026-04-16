using CommunityToolkit.HighPerformance;

using Source;
using Source.Common.Commands;

using System.Numerics;
using System.Runtime.InteropServices;

namespace Game.Server.NavMesh;

static class NavFile
{
	// 1 = hiding spots as plain vector array
	// 2 = hiding spots as HidingSpot objects
	// 3 = Encounter spots use HidingSpot ID's instead of storing vector again
	// 4 = Includes size of source bsp file to verify nav data correlation
	// ---- Beta Release at V4 -----
	// 5 = Added Place info
	// ---- Conversion to Src ------
	// 6 = Added Ladder info
	// 7 = Areas store ladder ID's so ladders can have one-way connections
	// 8 = Added earliest occupy times (2 floats) to each area
	// 9 = Promoted CNavArea's attribute flags to a short
	// 10 - Added sub-version number to allow derived classes to have custom area data
	// 11 - Added light intensity to each area
	// 12 - Storing presence of unnamed areas in the PlaceDirectory
	// 13 - Widened NavArea attribute bits from unsigned short to int
	// 14 - Added a bool for if the nav needs analysis
	// 15 - removed approach areas
	// 16 - Added visibility data to the base mesh

	/// IMPORTANT: If this version changes, the swap function in makegamedata must be updated to match.
	public const int NavCurrentVersion = 16;

	public static void WarnIfMeshNeedsAnalysis(uint version) {
		if (version >= 14) {
			if (!NavMesh.Instance!.IsAnalyzed()) {
				Warning("The nav mesh needs a full nav_analyze\n");
				return;
			}
		}
	}
}

public class PlaceDirectory
{
	readonly List<NavPlace> Directory = [];
	bool HasUnnamedAreas;

	public PlaceDirectory() => Reset();

	public void Reset() {
		Directory.Clear();
		HasUnnamedAreas = false;
	}

	bool IsKnown(NavPlace place) => Directory.Contains(place);

	public ushort GetIndex(NavPlace place) {
		if (place == Nav.UndefinedPlace)
			return 0;

		int index = Directory.IndexOf(place);
		if (index < 0) {
			AssertMsg(false, "PlaceDirectory::GetIndex failure");
			return 0;
		}

		return (ushort)(index + 1);
	}

	public void AddPlace(NavPlace place) {
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

	public void Save(BinaryWriter buffer) {
		ushort count = (ushort)Directory.Count;
		buffer.Write(count);

		foreach (NavPlace place in Directory) {
			ReadOnlySpan<char> placeName = NavMesh.Instance!.PlaceToName(place);

			int byteCount = System.Text.Encoding.ASCII.GetByteCount(placeName);
			ushort len = (ushort)(byteCount + 1);

			buffer.Write(len);

			Span<byte> tmp = stackalloc byte[byteCount];
			System.Text.Encoding.ASCII.GetBytes(placeName, tmp);

			buffer.Write(tmp);
			buffer.Write((byte)0);
		}

		buffer.Write((byte)(HasUnnamedAreas ? 1 : 0));
	}

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
	public void Save(BinaryWriter buffer, uint version) {
		buffer.Write(ID);
		buffer.Write((int)AttributeFlags);

		buffer.Write(NWCorner.X);
		buffer.Write(NWCorner.Y);
		buffer.Write(NWCorner.Z);

		buffer.Write(SECorner.X);
		buffer.Write(SECorner.Y);
		buffer.Write(SECorner.Z);

		buffer.Write(NEZ);
		buffer.Write(SWZ);

		for (int d = 0; d < (int)NavDirType.NumDirections; d++) {
			uint c = (uint)Connect[d].Count;
			buffer.Write(c);

			for (int i = 0; i < Connect[d].Count; i++) {
				NavConnect connect = Connect[d][i];
				buffer.Write(connect.Area!.ID);
			}
		}

		byte count;
		if (HidingSpots.Count > 255) count = 255;
		else count = (byte)HidingSpots.Count;
		buffer.Write(count);

		uint saveCount = 0;
		for (int i = 0; i < HidingSpots.Count; i++) {
			HidingSpot spot = HidingSpots[i];
			spot.Save(buffer, version);
			if (++saveCount == count) break;
		}

		uint count2 = (uint)SpotEncounters.Count;
		buffer.Write(count2);

		for (int i = 0; i < SpotEncounters.Count; i++) {
			SpotEncounter e = SpotEncounters[i];

			if (e.From.Area != null) buffer.Write(e.From.Area.ID);
			else buffer.Write(0u);

			byte dir = (byte)e.FromDir;
			buffer.Write(dir);

			if (e.To.Area != null) buffer.Write(e.To.Area.ID);
			else buffer.Write(0u);

			dir = (byte)e.ToDir;
			buffer.Write(dir);

			byte spotCount;
			if (e.Spots.Count > 255) spotCount = 255;
			else spotCount = (byte)e.Spots.Count;
			buffer.Write(spotCount);

			saveCount = 0;
			for (int j = 0; j < e.Spots.Count; j++) {
				SpotOrder order = e.Spots[j];

				uint id;
				if (order.Spot != null) id = order.Spot.ID;
				else id = 0u;
				buffer.Write(id);

				byte t = (byte)(255 * order.T);
				buffer.Write(t);

				if (++saveCount == spotCount) break;
			}
		}

		ushort entry = NavMesh.placeDirectory.GetIndex(GetPlace());
		buffer.Write(entry);

		for (int i = 0; i < (int)NavLadder.LadderDirectionType.NumLadderDirections; i++) {
			uint count3 = (uint)Ladder[i].Count;
			buffer.Write(count3);

			for (int j = 0; j < Ladder[i].Count; j++) {
				NavLadderConnect ladder = Ladder[i][j];
				uint id = ladder.Ladder!.ID;
				buffer.Write(id);
			}
		}

		for (int i = 0; i < Nav.MAX_NAV_TEAMS; i++)
			buffer.Write(EarliestOccupyTime[i]);

		for (int i = 0; i < (int)NavCornerType.NumCorners; i++)
			buffer.Write(LightIntensity[i]);

		uint visibleAreaCount = (uint)PotentiallyVisibleAreas.Count;
		buffer.Write(visibleAreaCount);

		for (int i = 0; i < PotentiallyVisibleAreas.Count; i++) {
			AreaBindInfo v = PotentiallyVisibleAreas[i];

			uint id;
			if (v.Area != null) id = v.Area.ID;
			else id = 0u;

			buffer.Write(id);
			buffer.Write(v.Attributes);
		}

		uint inheritID;
		if (InheritVisibilityFrom.Area != null) inheritID = InheritVisibilityFrom.Area.ID;
		else inheritID = 0u;
		buffer.Write(inheritID);
	}

	public NavErrorType Load(BinaryReader fileBuffer, uint version, uint subVersion) {
		ID = fileBuffer.ReadUInt32();

		if (ID >= NextID)
			NextID = ID + 1;

		if (version <= 8)
			AttributeFlags = (NavAttributeType)fileBuffer.ReadByte();
		else if (version < 13)
			AttributeFlags = (NavAttributeType)fileBuffer.ReadUInt16();
		else
			AttributeFlags = (NavAttributeType)fileBuffer.ReadInt32();

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
				HidingSpot spot = NavMesh.CreateHidingSpot();
				spot.SetPosition(pos);
				spot.SetFlags(HidingSpotFlags.InCover);
				HidingSpots.Add(spot);
			}
		}
		else {
			for (int h = 0; h < hidingSpotCount; ++h) {
				HidingSpot spot = NavMesh.CreateHidingSpot();
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

		for (int i = 0; i < Nav.MAX_NAV_TEAMS; ++i)
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
				Ladder[dir][it] = connect;
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
				Connect[d][it] = connect;
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
				ComputePortal(e.To.Area, e.ToDir, ref e.Path.To, out float halfWidth);
				ComputePortal(e.From.Area, e.FromDir, ref e.Path.From, out halfWidth);

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
				e.Spots[sit] = order;
			}
			SpotEncounters[it] = e;
		}

		for (int it = 0; it < PotentiallyVisibleAreas.Count; ++it) {
			AreaBindInfo info = PotentiallyVisibleAreas[it];

			info.Area = NavMesh.Instance!.GetNavAreaByID(info.ID);
			if (info.Area == null)
				Warning("Invalid area in visible set for area #%d\n", GetID());
			PotentiallyVisibleAreas[it] = info;
		}

		InheritVisibilityFrom.Area = NavMesh.Instance!.GetNavAreaByID(InheritVisibilityFrom.ID);
		Assert(InheritVisibilityFrom.Area != this);

		PotentiallyVisibleAreas.RemoveAll(info => info.Area == null);

		ClearAllNavCostEntities();

		return error;
	}

	public void ComputeEarliestOccupyTimes() {
		for (int team = 0; team < Nav.MAX_NAV_TEAMS; ++team)
			EarliestOccupyTime[team] = 0.0f;
	}

	public virtual void CustomAnalysis(bool incremental) { }
}

public partial class NavMesh
{
	public static PlaceDirectory placeDirectory = new();
	static InlineArray256<char> Filename;
	static InlineArray256<char> BspFilename;

	public Span<char> GetFilename() {
		Span<char> gamePath = stackalloc char[256];
		engine.GetGameDir(gamePath);

		Span<char> path = stackalloc char[256];
		sprintf(path, "%smaps\\%s.nav").S(gamePath).S(gpGlobals.MapName);

		path.CopyTo(Filename);

		Span<char> filename = Filename;
		return filename.SliceNullTerminatedString();
	}

	ReadOnlySpan<char> GetBspFilename(ReadOnlySpan<char> navFilename) {
		sprintf(BspFilename, "maps\\%s.bsp").S(gpGlobals.MapName);
		ReadOnlySpan<char> filename = BspFilename;
		return filename.SliceNullTerminatedString();
	}

	public bool Save() {
		NavFile.WarnIfMeshNeedsAnalysis(NavFile.NavCurrentVersion);

		Span<char> filename = GetFilename();
		StrTools.FixSlashes(filename);

		ReadOnlySpan<char> bspFilename = GetBspFilename(filename);

		using MemoryStream memStream = new();
		using BinaryWriter buffer = new(memStream);

		uint magic = Nav.NavMagicNumber;
		buffer.Write(magic);
		buffer.Write(NavFile.NavCurrentVersion);
		buffer.Write(GetSubVersionNumber());

		long bspSize = filesystem.Size(bspFilename);
		DevMsg($"Size of bsp file '{bspFilename}' is {bspSize} bytes.\n");

		buffer.Write((uint)bspSize);
		buffer.Write((byte)(IsAnalyzed() ? 1 : 0));

		placeDirectory.Reset();

		foreach (NavArea area in NavArea.TheNavAreas) {
			NavPlace place = area.GetPlace();
			placeDirectory.AddPlace(place);
		}

		placeDirectory.Save(buffer);

		SaveCustomDataPreArea(buffer);

		uint count = (uint)NavArea.TheNavAreas.Count;
		buffer.Write(count);

		foreach (NavArea area in NavArea.TheNavAreas)
			area.Save(buffer, NavFile.NavCurrentVersion);

		uint ladderCount = (uint)GetLadders().Count;
		buffer.Write(ladderCount);

		foreach (NavLadder ladder in GetLadders())
			ladder.Save(buffer, NavFile.NavCurrentVersion);

		SaveCustomData(buffer);

		if (true /*!filesystem.WriteFile(filename, "MOD", memStream.ToArray(), 0)*/) {
			Warning($"Unable to save {memStream.Length} bytes to {filename}\n");
			return false;
		}

		long navSize = filesystem.Size(filename);
		DevMsg($"Size of nav file '{filename}' is {navSize} bytes.\n");

		return true;
	}

	List<NavPlace> GetPlacesFromNavFile(bool hasUnnamedPlaces) {
		throw new NotImplementedException();
	}

	private void GetNavSizeFromFile(out long size) { // HACK, we don't have a growable buffer
		Span<char> filename = stackalloc char[MAX_PATH];
		sprintf(filename, "maps/%s.nav").S(gpGlobals.MapName);

		size = filesystem.Size(filename, "MOD");
		if (size == -1) size = filesystem.Size(filename, "BSP");
		if (size == -1) size = filesystem.Size("maps\\embed.nav", "BSP");
	}

	NavErrorType GetNavDataFromFile(Span<byte> outBuffer, ref bool navDataFromBSP) {
		Span<char> filename = stackalloc char[MAX_PATH];
		sprintf(filename, "maps/%s.nav").S(gpGlobals.MapName);

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

	public NavErrorType Load() {
		Reset();
		placeDirectory.Reset();

		NavArea.NextID = 1;

		bool navIsInBsp = false;

		GetNavSizeFromFile(out long navSize);
		if (navSize <= 0)
			return NavErrorType.CantAccessFile;

		Span<byte> fileBuffer = new byte[(int)navSize];
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

			Span<char> bspFilename = stackalloc char[260];
			sprintf(bspFilename, "maps/%s.bsp").S(gpGlobals.MapName);

			long bspSize = filesystem.Size(bspFilename); // FIXME: Size doesn't work?

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
			bIsAnalyzed = buffer.ReadByte() != 0;
		else
			bIsAnalyzed = false;

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
			NavArea area = CreateArea();
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

		NavFile.WarnIfMeshNeedsAnalysis(version);

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

		List<OneWayLink> oneWayLinks = [];

		foreach (NavArea area in NavArea.TheNavAreas) {
			for (int d = 0; d < (int)NavDirType.NumDirections; ++d) {
				List<NavConnect> connectList = area.GetAdjacentAreas((NavDirType)d);

				foreach (NavConnect connect in connectList) {
					OneWayLink oneWayLink = new() {
						Area = area,
						DestArea = connect.Area!,
						BackD = (int)Nav.OppositeDirection((NavDirType)d)
					};

					List<NavConnect> backConnectList = oneWayLink.DestArea.GetAdjacentAreas((NavDirType)oneWayLink.BackD);
					bool isOneWay = true;

					foreach (NavConnect backConnect in backConnectList) {
						if (backConnect.Area.GetID() == oneWayLink.Area.GetID()) {
							isOneWay = false;
							break;
						}
					}

					if (isOneWay)
						oneWayLinks.Add(oneWayLink);
				}
			}
		}

		oneWayLinks.Sort(OneWayLink.Compare);

		foreach (OneWayLink link in oneWayLinks)
			link.DestArea.AddIncomingConnection(link.Area, (NavDirType)link.BackD);

		ValidateNavAreaConnections();

		for (int i = 0; i < AvoidanceObstacles.Count; ++i)
			AvoidanceObstacles[i].OnNavMeshLoaded();

		bIsLoaded = true;

		return NavErrorType.Ok;
	}
}