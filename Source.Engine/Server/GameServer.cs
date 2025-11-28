using Source.Common.Bitbuffers;
using Source.Common.Client;
using Source.Common.Engine;
using Source.Common.Networking;
using Source.Common.Server;
using Source.Common.Utilities;

namespace Source.Engine.Server;

/// <summary>
/// Base server, in SERVER. Often referred to by 'sv'
/// </summary>
public class GameServer : BaseServer
{
	public override void SetMaxClients(int number) {
		MaxClients = Math.Clamp(number, 1, MaxClientsLimit);
		Host.deathmatch.SetValue(MaxClients > 1);
	}

	public override void Init(bool dedicated) {

	}

	public override void Shutdown() {


	}

	public void SetQueryPortFromSteamServer() {
		// todo
	}
	internal bool IsLevelMainMenuBackground() {
		return LevelMainMenuBackground;
	}

	public bool LoadGame;           // handle connections specially

	public InlineArray64<char> Startspot;

	public int NumEdicts;
	public int MaxEdicts;
	public int FreeEdicts;
	public Edict[]? Edicts;
	IChangeInfoAccessor? edictchangeinfo;

	public int MaxClientsLimit;    // Max allowed on server.

	public bool AllowSignOnWrites;
	public bool DLLInitialized;    // Have we loaded the game dll.

	public bool LevelMainMenuBackground;  // true if the level running only as the background to the main menu

	public readonly List<EventInfo> TempEntities = [];     // temp entities

	public readonly bf_write FullSendTables = new();
	public readonly UtlMemory<byte> FullSendTablesBuffer = new();

	public bool LoadedPlugins;

	public void CreateEngineStringTables() {

	}

	public INetworkStringTable? GetModelPrecacheTable() => ModelPrecacheTable;
	public INetworkStringTable? GetGenericPrecacheTable() => GenericPrecacheTable;
	public INetworkStringTable? GetSoundPrecacheTable() => SoundPrecacheTable;
	public INetworkStringTable? GetDecalPrecacheTable() => DecalPrecacheTable;

	public INetworkStringTable? GetDynamicModelsTable() => DynamicModelsTable;


	public int PrecacheModel(ReadOnlySpan<char> name, int flags, Model? model = null) {
		if (ModelPrecacheTable == null)
			return -1;
		int idx = ModelPrecacheTable.AddString(true, name);
		if (idx == INetworkStringTable.INVALID_STRING_INDEX)
			return -1;
		throw new NotImplementedException();
	}
	public Model? GetModel(int index) {
		if (index <= 0 || ModelPrecacheTable == null)
			return null;
		if (index >= ModelPrecacheTable.GetNumStrings())
			return null;
		PrecacheItem slot = ModelPrecache![index];
		return slot.GetModel();
	}
	public int LookupModelIndex(ReadOnlySpan<char> name) {
		if (ModelPrecacheTable == null)
			return -1;
		int idx = ModelPrecacheTable.FindStringIndex(name);
		return idx == INetworkStringTable.INVALID_STRING_INDEX ? -1 : idx;
	}

	// Accessors to model precaching stuff
	public int PrecacheSound(ReadOnlySpan<char> name, int flags) {
		if (SoundPrecacheTable == null)
			return -1;
		int idx = SoundPrecacheTable.AddString(true, name);
		if (idx == INetworkStringTable.INVALID_STRING_INDEX)
			return -1;
		throw new NotImplementedException();
	}
	public ReadOnlySpan<char> GetSound(int index) {
		if (index <= 0 || SoundPrecacheTable == null)
			return null;
		if (index >= SoundPrecacheTable.GetNumStrings())
			return null;
		PrecacheItem slot = SoundPrecache![index];
		return slot.GetName();
	}
	public int LookupSoundIndex(ReadOnlySpan<char> name) {
		if (SoundPrecacheTable == null)
			return -1;
		int idx = SoundPrecacheTable.FindStringIndex(name);
		return idx == INetworkStringTable.INVALID_STRING_INDEX ? -1 : idx;
	}

	public int PrecacheGeneric(ReadOnlySpan<char> name, int flags) {
		if (GenericPrecacheTable == null)
			return -1;
		int idx = GenericPrecacheTable.AddString(true, name);
		if (idx == INetworkStringTable.INVALID_STRING_INDEX)
			return -1;
		throw new NotImplementedException();
	}
	public ReadOnlySpan<char> GetGeneric( int index ) {
		if (index <= 0 || GenericPrecacheTable == null)
			return null;
		if (index >= GenericPrecacheTable.GetNumStrings())
			return null;
		PrecacheItem slot = GenericPrecache![index];
		return slot.GetName();
	}
	public int LookupGenericIndex(ReadOnlySpan<char> name) {
		if (GenericPrecacheTable == null)
			return -1;
		int idx = GenericPrecacheTable.FindStringIndex(name);
		return idx == INetworkStringTable.INVALID_STRING_INDEX ? -1 : idx;
	}

	public int PrecacheDecal(ReadOnlySpan<char> name, int flags) {
		if (DecalPrecacheTable == null)
			return -1;
		int idx = DecalPrecacheTable.AddString(true, name);
		if (idx == INetworkStringTable.INVALID_STRING_INDEX)
			return -1;
		throw new NotImplementedException();
	}
	public int LookupDecalIndex(ReadOnlySpan<char> name) {
		if (DecalPrecacheTable == null)
			return -1;
		int idx = DecalPrecacheTable.FindStringIndex(name);
		return idx == INetworkStringTable.INVALID_STRING_INDEX ? -1 : idx;
 	}

	public void DumpPrecacheStats(INetworkStringTable? table) {

	}

	public bool IsHibernating() => Hibernating;
	public void UpdateHibernationState() {
		// todo
	}


	public PrecacheItem[]? ModelPrecache;
	public PrecacheItem[]? GenericPrecache;
	public PrecacheItem[]? SoundPrecache;
	public PrecacheItem[]? DecalPrecache;

	private void SetHibernating(bool hibernating) {
		// todo
	}

	internal void InitMaxClients() {
		// todo
	}

	INetworkStringTable? ModelPrecacheTable;
	INetworkStringTable? SoundPrecacheTable;
	INetworkStringTable? GenericPrecacheTable;
	INetworkStringTable? DecalPrecacheTable;

	INetworkStringTable? DynamicModelsTable;

	bool Hibernating;    // Are we hibernating.  Hibernation makes server process consume approx 0 CPU when no clients are connected
}
