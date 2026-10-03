using Source.Common.Utilities;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Game.Shared;

public class ActivityList
{
	public Activity ActivityIndex;
	public ulong StringKey;
	public bool IsPrivate;

	static readonly List<ActivityList> g_ActivityList = [];
	// g_nActivityListVersion
	public static int Version = 1;
	public static Activity HighestActivity = 0;
	static readonly UtlSymbolTable ActivityStrings = new();
	static readonly Dictionary<UtlSymId_t, int> ActivityRemapDatabase = [];
	static readonly Dictionary<UtlSymId_t, int> SharedActivityRemapDatabase = [];

	public static void Init() {
		HighestActivity = 0;
	}

	public static void Free() {
		ActivityStrings.Clear();
		g_ActivityList.Clear();
		ActivityRemapDatabase.Clear();
		SharedActivityRemapDatabase.Clear();
		Version++;
	}

	public static ActivityList? ListFromString(ReadOnlySpan<char> str){
		ulong stringHash = ActivityStrings.AddString(str);
		if (!SharedActivityRemapDatabase.TryGetValue(stringHash, out int idx))
			return null;
		return g_ActivityList[idx];
	}

	public static int IndexForName(ReadOnlySpan<char> activityName) {
		ActivityList? list = ListFromString(activityName);

		if (list != null)
			return (int)list.ActivityIndex;

		return kActivityLookup_Missing;
	}

	public static string? NameForIndex(Activity activityIndex) {
		ActivityList? list = ListFromActivity(activityIndex);
		if (list != null)
			return ActivityStrings.String(list.StringKey);

		return null;
	}

	public static ActivityList? ListFromActivity(Activity activityIndex){
		foreach(var a in g_ActivityList)
			if (a.ActivityIndex == activityIndex) 
				return a;

		return null;
	}

	public static ActivityList AddActivityEntry(ReadOnlySpan<char> name, Activity activityIndex, bool isPrivate){
		ActivityList list = new();
		list.ActivityIndex = activityIndex;
		list.StringKey = ActivityStrings.AddString(name);
		list.IsPrivate = isPrivate;

		if (activityIndex > HighestActivity)
			HighestActivity = activityIndex;
		g_ActivityList.Add(list);
		return list;
	}


	static Activity lastActivityIndex = (Activity)(-1);
	public static Activity RegisterPrivateActivity(ReadOnlySpan<char> activityName) {
		ActivityList? list = ListFromString(activityName);
		if (list != null) {
			// this activity is already in the list. If the activity we collided with is also private, 
			// then the collision is OK. Otherwise, it's a bug.
			if (list.IsPrivate) 
				return list.ActivityIndex;
			else {
				// this private activity collides with a shared activity. That is not allowed.
				Warning("***\nShared<->Private Activity collision!\n***\n");
				Assert(0);
				return Activity.ACT_INVALID;
			}
		}

		list = AddActivityEntry(activityName, HighestActivity + 1, true);
		return list.ActivityIndex;
	}
	public static bool RegisterSharedActivity(ReadOnlySpan<char> activityName, Activity activityIndex) {
		Assert(activityIndex < Activity.LAST_SHARED_ACTIVITY && (activityIndex == lastActivityIndex + 1 || activityIndex == 0));
		lastActivityIndex = activityIndex;

		ActivityList? list = ListFromString(activityName);
		list ??= ListFromActivity(activityIndex);

		if (list != null) {
			Warning($"***\nShared activity collision! {activityName}<->{ActivityStrings.String(list.StringKey)}\n***\n");
			Assert(false);
			return false;
		}
		// ----------------------------------------------------------------
		AddActivityEntry(activityName, activityIndex, false);
		SharedActivityRemapDatabase[g_ActivityList[^1].StringKey] = g_ActivityList.Count - 1;
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] static void REGISTER_SHARED_ACTIVITY(Activity act) => RegisterSharedActivity(Enum.GetName<Activity>(act), act);

	public static void RegisterSharedActivities(){
		foreach (var value in Enum.GetValues<Activity>())
			REGISTER_SHARED_ACTIVITY(value);
	}
}
