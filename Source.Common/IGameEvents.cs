using Source.Common.Bitbuffers;
using Source.Common.Formats.Keyvalues;

namespace Source.Common;

public interface IGameEvent
{
	ReadOnlySpan<char> GetName();
	bool IsReliable();
	bool IsLocal();
	bool IsEmpty(ReadOnlySpan<char> keyName = default);
	bool GetBool(ReadOnlySpan<char> keyName = default, bool defaultValue = false);
	int GetInt(ReadOnlySpan<char> keyName = default, int defaultValue = 0);
	float GetFloat(ReadOnlySpan<char> keyName = default, float defaultValue = 0.0f);
	ReadOnlySpan<char> GetString(ReadOnlySpan<char> keyName = default, ReadOnlySpan<char> defaultValue = default);
	void SetBool(ReadOnlySpan<char> keyName, bool value);
	void SetInt(ReadOnlySpan<char> keyName, int value);
	void SetFloat(ReadOnlySpan<char> keyName, float value);
	void SetString(ReadOnlySpan<char> keyName, ReadOnlySpan<char> value);
}

public interface IGameEventListener2
{
	void FireGameEvent(IGameEvent ev);
}

public interface IGameEventManager2
{
	int LoadEventsFromFile(ReadOnlySpan<char> filename);
	void Reset();
	bool AddListener(IGameEventListener2 listener, ReadOnlySpan<char> name, bool serverSide);
	bool FindListener(IGameEventListener2 listener, ReadOnlySpan<char> name);
	void RemoveListener(IGameEventListener2 listener);
	IGameEvent CreateEvent(ReadOnlySpan<char> name, bool force = false);
	bool FireEvent(IGameEvent ev, bool dontBroadcast = false);
	bool FireEventClientSide(IGameEvent ev);
	IGameEvent DuplicateEvent(IGameEvent ev);
	void FreeEvent(IGameEvent ev);
	bool SerializeEvent(IGameEvent ev, bf_write buf);
	IGameEvent UnserializeEvent(bf_read buf);
}

public interface IGameEventListener
{
	void FireGameEvent(KeyValues ev);
}
public interface IGameEventManager
{
	int LoadEventsFromFile(ReadOnlySpan<char> filename);
	void Reset();
	KeyValues GetEvent(ReadOnlySpan<char> name); // returns keys for event
	bool AddListener(IGameEventListener listener, ReadOnlySpan<char> ev, bool isServerSide);
	bool AddListener(IGameEventListener listener, bool isServerSide);
	void RemoveListener(IGameEventListener listener);
	bool FireEvent(KeyValues ev);
	bool FireEventServerOnly(KeyValues ev);
	bool FireEventClientOnly(KeyValues ev);
	bool SerializeKeyValues(KeyValues ev, bf_write buf, IGameEvent? eventtype = null);
	KeyValues UnserializeKeyValue(bf_read msg);
}
