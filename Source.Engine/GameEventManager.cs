using Source.Common;
using Source.Common.Bitbuffers;
using Source.Common.Commands;
using Source.Common.Filesystem;
using Source.Common.Formats.Keyvalues;
using Source.Common.Networking;
using Source.Common.Utilities;

namespace Source.Engine;

public class GameEventManager(IFileSystem fileSystem) : IGameEventManager2
{
	public bool Init() {
		Reset();
		LoadEventsFromFile("resource/serverevents.res");
		return true;
	}

	public bool AddListener(IGameEventListener2 listener, ReadOnlySpan<char> name, bool serverSide) {
		throw new NotImplementedException();
	}

	public IGameEvent? CreateEvent(ReadOnlySpan<char> name, bool force = false) {
		if (name.IsEmpty)
			return null;

		GameEventDescriptor? descriptor = GetEventDescriptor(name);
		if (descriptor == null) {
			DevMsg($"CreateEvent: event '{name}' not registered.\n");
			return null;
		}

		if (descriptor.Listeners.Count == 0 && !force)
			return null;

		return new GameEvent(descriptor);
	}

	private GameEventDescriptor? GetEventDescriptor(ReadOnlySpan<char> name) {
		if (name.IsEmpty)
			return null;

		foreach (var descriptor in GameEvents)
			if (name.Equals(descriptor.Name, StringComparison.Ordinal))
				return descriptor;

		return null;
	}

	public IGameEvent DuplicateEvent(IGameEvent ev) {
		throw new NotImplementedException();
	}

	public bool FindListener(IGameEventListener2 listener, ReadOnlySpan<char> name) {
		throw new NotImplementedException();
	}

	public bool FireEvent(IGameEvent ev, bool dontBroadcast = false) {
		throw new NotImplementedException();
	}

	public bool FireEventClientSide(IGameEvent ev) {
		throw new NotImplementedException();
	}

	public void FreeEvent(IGameEvent ev) {
		throw new NotImplementedException();
	}

	public int LoadEventsFromFile(ReadOnlySpan<char> filename) {
		if (UTL_INVAL_SYMBOL == EventFiles.Find(filename)) {
			UtlSymId_t id = EventFiles.AddString(filename);
			EventFileNames.Add(id);
		}

		KeyValues key = new(filename);

		if (!key.LoadFromFile(fileSystem, filename, "GAME"))
			return 0;

		int count = 0;  // number new events

		KeyValues? subkey = key.GetFirstSubKey();

		while (subkey != null) {
			if (subkey.Type == KeyValues.Types.None) {
				RegisterEvent(subkey);
				count++;
			}

			subkey = subkey.GetNextKey();
		}

		if (net_showevents.GetBool())
			DevMsg($"Event System loaded {GameEvents.Count} events from file {filename}.\n");

		return GameEvents.Count;
	}
	static ConVar net_showevents = new("net_showevents", "0", FCvar.Cheat, "Dump game events to console (1=client only, 2=all).");


	public void RemoveListener(IGameEventListener2 listener) {
		throw new NotImplementedException();
	}

	public void Reset() {
		GameEvents.Clear();
		Listeners.Clear();
		EventFiles.Clear();
		EventFileNames.Clear();
		ClientListenersChanged = true;
	}

	public bool SerializeEvent(IGameEvent ev, bf_write buf) {
		throw new NotImplementedException();
	}

	public IGameEvent UnserializeEvent(bf_read buf) {
		throw new NotImplementedException();
	}

	static readonly string[] s_GameEventTypeMap = [
		"local",
		"string",
		"float",
		"long",
		"short",
		"byte",
		"bool"
	];


	protected bool RegisterEvent(KeyValues? ev) {
		if (ev == null)
			return false;

		if (GameEvents.Count == MAX_EVENT_NUMBER) {
			DevMsg($"GameEventManager: couldn't register event '{ev.Name}', limit reached ({MAX_EVENT_NUMBER}).\n");
			return false;
		}

		GameEventDescriptor? descriptor = GetEventDescriptor(ev.Name);

		if (descriptor == null) {
			int index = GameEvents.Count; GameEvents.Add(new());
			descriptor = GameEvents[index];

			AssertMsg(ev.Name.Length <= MAX_EVENT_NAME_LENGTH, $"Event named '{ev.Name}' exceeds maximum name length {MAX_EVENT_NAME_LENGTH}");
			strcpy(descriptor.Name, ev.Name);
		}

		descriptor.Keys = new KeyValues("descriptor");
		KeyValues? subkey = ev.GetFirstSubKey();

		while (subkey != null) {
			ReadOnlySpan<char> keyName = subkey.Name;
			ReadOnlySpan<char> type = subkey.GetString();

			if (streq("local", keyName))
				descriptor.Local = int.TryParse(type, out int i) ? i != 0 : false;
			else if (streq("reliable", keyName))
				descriptor.Reliable = int.TryParse(type, out int i) ? i != 0 : false;
			else {
				GameEventType i;

				for (i = GameEventType.Local; i <= GameEventType.Bool; i++) {
					if (streq(type, s_GameEventTypeMap[(int)i])) {
						descriptor.Keys.SetInt(keyName, (int)i);
						break;
					}
				}

				if (i > GameEventType.Bool) {
					descriptor.Keys.SetInt(keyName, 0);
					DevMsg($"GameEventManager: unknown type '{type}' for key '{subkey.Name}'.\n");
				}
			}

			subkey = subkey.GetNextKey();
		}

		return true;
	}
	protected void UnregisterEvent(int index) { throw new NotImplementedException(); }
	protected bool FireEventIntern(IGameEvent ev, bool serverSide, bool clientOnly) { throw new NotImplementedException(); }
	protected GameEventCallback? FindEventListener(object? listener) { throw new NotImplementedException(); }

	public bool HasClientListenersChanged(bool reset = true) {
		if (!ClientListenersChanged)
			return false;

		if (reset)
			ClientListenersChanged = false;

		return true;
	}

	internal void WriteListenEventList(CLC_ListenEvents msg) {
		msg.EventArray.ClearAll();

		foreach (var descriptor in GameEvents) {
			bool hasClientListener = false;

			foreach (var listener in descriptor.Listeners) {
				if (listener.ListenerType == GameEventListenerType.Clientside || listener.ListenerType == GameEventListenerType.ClientsideOld) {
					hasClientListener = true;
					break;
				}
			}

			if (!hasClientListener)
				continue;

			if (descriptor.EventID == -1) {
				DevMsg($"Warning! Client listens to event '{descriptor.Name}' unknown by server.\n");
				continue;
			}

			msg.EventArray.Set(descriptor.EventID);
		}
	}

	protected readonly List<GameEventDescriptor> GameEvents = [];
	protected readonly List<GameEventDescriptor> Listeners = [];
	protected readonly UtlSymbolTable EventFiles = new();
	protected readonly List<UtlSymId_t> EventFileNames = [];

	protected bool ClientListenersChanged;
}
