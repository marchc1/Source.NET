namespace Game.Shared;

using Source.Common.Bitbuffers;

using Source;
using Source.Common.Utilities;

public delegate void UserMessageHook(bf_read msg);

public class UserMessage
{
	public int Size = 0;
	public UtlSymbol Name = new();
	public List<UserMessageHook> ClientHooks = [];
}

[EngineComponent]
public partial class UserMessages
{
	private Dictionary<UtlSymId_t, int> nameToIntType = [];
	private Dictionary<int, UserMessage> intTypeToInstance = [];

	public int Count {
		get {
			int refCount = nameToIntType.Count;
			Dbg.Assert(refCount == intTypeToInstance.Count);
			return refCount;
		}
	}

	public int LookupUserMessage(ReadOnlySpan<char> name) {
		if (nameToIntType.TryGetValue(new UtlSymbol(name), out int idx))
			return idx;
		return -1;
	}

	public int GetUserMessageSize(int index) {
		if (index < 0 || index >= Count) 
			throw new Exception($"UserMessages.GetUserMessageSize({index}) out of range!!!");

		UserMessage e = intTypeToInstance[index];
		return e.Size;
	}

	public ReadOnlySpan<char> GetUserMessageName(int index) {
		if (index < 0 || index >= Count) 
			throw new Exception($"UserMessages.GetUserMessageSize({index}) out of range!!!");

		UserMessage e = intTypeToInstance[index];
		return e.Name.String();
	}

	public bool IsValidIndex(int index) {
		return intTypeToInstance.ContainsKey(index);
	}

	public void Register(ReadOnlySpan<char> name, int size) {
		if (nameToIntType.TryGetValue(new UtlSymbol(name), out int idx))
			throw new Exception($"UserMessages.Register '{name}' already registered");

		UserMessage entry = new UserMessage();
		entry.Size = size;
		entry.Name = new UtlSymbol(name);
		idx = Count;

		intTypeToInstance[idx] = entry;
		nameToIntType[entry.Name] = idx;
	}

	public void HookMessage(ReadOnlySpan<char> name, UserMessageHook hook) {
		Assert(!name.IsEmpty && hook != null);

		if (!nameToIntType.TryGetValue(new UtlSymbol(name), out int idx)) {
			Assert(false, $"UserMessages.HookMessage: no such message {name}");
			return;
		}

		UserMessage msg = intTypeToInstance[idx];
		msg.ClientHooks.Add(hook);
	}

	public bool DispatchUserMessage(int msgType, bf_read msgData) {
		if (msgType < 0 || msgType >= Count) {
			Assert(false, $"UserMessages.DispatchUserMessage: Bogus msg type {msgType} (max == {Count})");
			return false;
		}

		if (!intTypeToInstance.TryGetValue(msgType, out UserMessage? entry)) {
			Assert(false, $"UserMessages.DispatchUserMessage: Missing client entry for msg type {msgType}");
			return false;
		}

		if (entry.ClientHooks.Count == 0) {
			Warning($"UserMessages.DispatchUserMessage: missing client hook for {entry.Name.String()}\n");
			return false;
		}

		foreach (UserMessageHook hook in entry.ClientHooks) {
			bf_read msgCopy = msgData.Copy();
			hook(msgCopy);
		}

		return true;
	}
}
