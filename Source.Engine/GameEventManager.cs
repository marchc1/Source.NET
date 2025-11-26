using Source.Common;
using Source.Common.Bitbuffers;
using Source.Common.Commands;
using Source.Common.Filesystem;
using Source.Common.Formats.Keyvalues;
using Source.Common.Networking;
using Source.Common.Utilities;
using Source.Engine.Client;
using Source.Engine.Server;


using System.Xml.Linq;

namespace Source.Engine;

public class GameEventManager(IFileSystem fileSystem) : IGameEventManager2
{
	ClientState? _cl; ClientState cl => _cl ??= Singleton<ClientState>();
	GameServer? _sv; GameServer sv => _sv ??= Singleton<GameServer>();
	public bool Init() {
		Reset();
		LoadEventsFromFile("resource/serverevents.res");
		return true;
	}

	bool AddListener(object listener, GameEventDescriptor descriptor, GameEventListenerType listenerType) {
		if (listener == null || descriptor == null)
			return false;

		GameEventCallback? callback = FindEventListener(listener);

		if (callback == null) {
			callback = new GameEventCallback();
			Listeners.Add(callback);

			callback.ListenerType = listenerType;
			callback.Callback = listener;
		}
		else {
			Assert(callback.ListenerType == listenerType);
			Assert(callback.Callback == listener);
		}

		if (descriptor.Listeners.Find(callback) == -1) {
			descriptor.Listeners.Add(callback);

			if (listenerType == GameEventListenerType.Clientside || listenerType == GameEventListenerType.ClientsideOld)
				ClientListenersChanged = true;
		}

		return true;
	}
	public bool AddListener(IGameEventListener2 listener, ReadOnlySpan<char> ev, bool serverSide) {
		if (ev.IsEmpty)
			return false;

		GameEventDescriptor? descriptor = GetEventDescriptor(ev);

		if (descriptor == null) {
			DevMsg($"GameEventManager.AddListener: event '{ev}' unknown. Check 'resource/serverevents.res'.\n");
			return false;
		}

		return AddListener(listener, descriptor, serverSide ? GameEventListenerType.Serverside : GameEventListenerType.Clientside);
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

	private GameEventDescriptor? GetEventDescriptor(IGameEvent? ev) {
		GameEvent? gameevent = (GameEvent?)ev;
		if (gameevent == null)
			return null;
		return gameevent.Descriptor;
	}

	private GameEventDescriptor? GetEventDescriptor(int eventid) {
		if (eventid < 0)
			return null;

		foreach (var descriptor in GameEvents)
			if (descriptor.EventID == eventid)
				return descriptor;

		return null;
	}

	private GameEventDescriptor? GetEventDescriptor(ReadOnlySpan<char> name) {
		if (name.IsEmpty)
			return null;

		foreach (var descriptor in GameEvents)
			if (name.Equals(((ReadOnlySpan<char>)descriptor.Name).SliceNullTerminatedString(), StringComparison.Ordinal))
				return descriptor;

		return null;
	}

	public IGameEvent DuplicateEvent(IGameEvent ev) {
		throw new NotImplementedException();
	}

	public bool FindListener(IGameEventListener2 listener, ReadOnlySpan<char> name) {
		throw new NotImplementedException();
	}

	public bool FireEvent(IGameEvent ev, bool serverOnly = false) {
		return FireEventIntern(ev, serverOnly, false);
	}

	public bool FireEventClientSide(IGameEvent ev) {
		return FireEventIntern(ev, false, true);
	}

	public void FreeEvent(IGameEvent ev) {
		// nothing to do here for now
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
	protected bool FireEventIntern(IGameEvent? ev, bool serverOnly, bool clientOnly) {
		if (ev == null)
			return false;

		GameEventDescriptor? descriptor = GetEventDescriptor(ev);
		if (descriptor == null) {
			DevMsg($"FireEvent: event '{ev.GetName()}' not registered.\n");
			FreeEvent(ev);
			return false;
		}

		if (net_showevents.GetInt() > 0) {
			if (clientOnly) {
				ConMsg($"Game event \"{descriptor.Name}\", Tick {cl.GetClientTickCount()}:\n");
				ConPrintEvent(ev);
			}
			else if (net_showevents.GetInt() > 1) {
				ConMsg($"Server event \"{descriptor.Name}\", Tick {sv.GetTick()}:\n");
				ConPrintEvent(ev);
			}
		}

		for (int i = 0; i < descriptor.Listeners.Count; i++) {
			GameEventCallback? listener = descriptor.Listeners[i];


			// don't trigger server listners for clientside only events
			if ((listener.ListenerType == GameEventListenerType.Serverside || listener.ListenerType == GameEventListenerType.ServersideOld) && clientOnly)
				continue;

			if ((listener.ListenerType == GameEventListenerType.Clientside || listener.ListenerType == GameEventListenerType.ClientsideOld) && !clientOnly)
				continue;

			if (listener.ListenerType == GameEventListenerType.Clientstub && (serverOnly || clientOnly))
				continue;

			if (listener.ListenerType == GameEventListenerType.ClientsideOld || listener.ListenerType == GameEventListenerType.ServersideOld) {
				IGameEventListener? callback = (IGameEventListener?)listener.Callback;
				GameEvent gameevent = (GameEvent)ev;

				callback!.FireGameEvent(gameevent.DataKeys!);
			}
			else {
				IGameEventListener2? callback = (IGameEventListener2?)listener.Callback;
				callback!.FireGameEvent(ev);
			}
		}

		FreeEvent(ev);

		return true;
	}

	private void ConPrintEvent(IGameEvent ev) {
		GameEventDescriptor? descriptor = GetEventDescriptor(ev);

		if (descriptor == null)
			return;

		KeyValues? key = descriptor.Keys?.GetFirstSubKey();

		while (key != null) {
			ReadOnlySpan<char> keyName = key.Name;

			GameEventType type = (GameEventType)key.GetInt();

			switch (type) {
				case GameEventType.Local: ConMsg($"- \"{keyName}\" = \"{ev.GetString(keyName)}\" (local)\n"); break;
				case GameEventType.String: ConMsg($"- \"{keyName}\" = \"{ev.GetString(keyName)}\"\n"); break;
				case GameEventType.Float: ConMsg($"- \"{keyName}\" = \"{ev.GetFloat(keyName)}\"\n"); break;
				default: ConMsg($"- \"{keyName}\" = \"{ev.GetInt(keyName)}\"\n"); break;
			}
			key = key.GetNextKey();
		}
	}

	protected GameEventCallback? FindEventListener(object? callback) {
		for (int i = 0; i < Listeners.Count; i++) {
			GameEventCallback listener = Listeners[i];
			if (listener.Callback == callback)
				return listener;
		}

		return null;
	}

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

	// TODO: Call this in server code
	public void ReloadEventDefinitions() {
		foreach(var fn in EventFileNames) {
			ReadOnlySpan<char> filename = EventFiles.String(fn);
			LoadEventsFromFile(filename);
		}

		int count = GameEvents.Count;
		for (int i = 0; i < count; i++) 
			GameEvents[i].EventID = i;
	}

	public bool ParseEventList(svc_GameEventList msg) {
		foreach (var descriptor in GameEvents)
			descriptor.EventID = -1;

		Span<char> name = stackalloc char[MAX_EVENT_NAME_LENGTH];
		for (int i = 0; i < msg.NumEvents; i++) {
			int id = (int)msg.DataIn.ReadUBitLong(MAX_EVENT_BITS);
			memreset(name);

			msg.DataIn.ReadString(name);

			GameEventDescriptor? descriptor = GetEventDescriptor(name);

			if (descriptor == null) {
				while (msg.DataIn.ReadUBitLong(3) != 0)
					msg.DataIn.ReadString(name);

				continue;
			}

			descriptor.Keys = new KeyValues("descriptor");
			GameEventType datatype = (GameEventType)msg.DataIn.ReadUBitLong(3);

			while (datatype != GameEventType.Local) {
				msg.DataIn.ReadString(name);
				descriptor.Keys.SetInt(name, (int)datatype);

				datatype = (GameEventType)msg.DataIn.ReadUBitLong(3);
			}

			descriptor.EventID = id;
		}

		ClientListenersChanged = true;

		return true;
	}

	protected readonly List<GameEventDescriptor> GameEvents = [];
	protected readonly List<GameEventCallback> Listeners = [];
	protected readonly UtlSymbolTable EventFiles = new();
	protected readonly List<UtlSymId_t> EventFileNames = [];

	protected bool ClientListenersChanged;
}
