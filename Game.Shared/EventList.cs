using CommunityToolkit.HighPerformance;


namespace Game.Shared;

public struct EventListEntry
{
	public int EventIndex;
	public int Type;
	public ushort StringKey;
	public bool IsPrivate;
}

// TODO: finish me

public static class EventList
{
	public static readonly List<EventListEntry> g_EventList = [];
	public static int g_HighestEvent = 0;
	public static int g_EventListVersion = 1;

	public static void Init() {
		g_HighestEvent = 0;
	}

	public static void Free() {
		// g_EventStrings.Clear();
		g_EventList.Clear();

		++g_EventListVersion;
	}

	public static void RegisterSharedEvents(){

	}

	public static EventListEntry AddEventEntry(ReadOnlySpan<char> name, int eventIndex, bool isPrivate, int type) {
		int index = g_EventList.Count; g_EventList.Add(default);
		ref EventListEntry pList = ref g_EventList.AsSpan()[index];
		pList.EventIndex = eventIndex;
		pList.StringKey = 0; // g_EventStrings.AddString(name, index);
		pList.IsPrivate = isPrivate;
		pList.Type = type;

		// UNDONE: This implies that ALL shared activities are added before ANY custom activities
		// UNDONE: Segment these instead?  It's a 32-bit int, how many activities do we need?
		if (eventIndex > g_HighestEvent) {
			g_HighestEvent = eventIndex;
		}

		return pList;
	}
}
