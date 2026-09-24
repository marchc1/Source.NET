using Source;
#if CLIENT_DLL || GAME_DLL
using Source.Common;
using Source.Common.Commands;
using Source.Common.Formats.Keyvalues;

using System.Numerics;

using static Source.Engine.Client.Steam3ClientAccessor;

namespace Game.Shared;

public delegate BaseAchievement AchievementCreateFunc();

public class BaseAchievement : GameEventListener, IAchievement
{
	internal readonly static ConVar cc_achievement_debug = new("achievement_debug",
#if DEBUG
	"1"
#else
	"0"
#endif
, FCvar.Cheat | FCvar.Replicated, "Turn on achievement debug msgs.");

	public static readonly DataMap DataDesc = new(typeof(BaseAchievement), null, [
		Source.DEFINE<BaseAchievement>.FIELD(nameof(Count), FieldType.Integer),
	]);

	protected string? Name;
	protected string? Stat;
	protected int AchievementID;
	protected AchievementFlags Flags;
	protected int Goal;
	protected int ProgressMsgIncrement;
	protected int ProgressMsgMinimum;
	protected int PointValue;
	protected bool HideUntilAchieved;
	protected bool bStoreProgressInSteam;
	protected internal string? InflictorClassNameFilter;
	protected internal string? InflictorEntityNameFilter;
	protected internal string? VictimClassNameFilter;
	protected internal string? AttackerClassNameFilter;
	protected internal string? MapNameFilter;
	protected internal string? GameDirFilter;
	protected string[]? ComponentNames;
	protected int NumComponents;
	protected internal string? ComponentPrefix;
	protected internal int ComponentPrefixLen;
	protected bool Achieved;
	protected uint UnlockTime;
	protected int Count;
	protected int ProgressShown;
	protected ulong ComponentBits;
	protected internal AchievementMgr? AchievementMgr;
	protected bool ShowOnHUD;

	public BaseAchievement() {
		Flags = 0;
		Goal = 0;
		ProgressMsgIncrement = 0;
		ProgressMsgMinimum = 0;
		AchievementID = 0;
		PointValue = 0;
		HideUntilAchieved = false;
		bStoreProgressInSteam = false;
		VictimClassNameFilter = null;
		AttackerClassNameFilter = null;
		InflictorClassNameFilter = null;
		InflictorEntityNameFilter = null;
		MapNameFilter = null;
		GameDirFilter = null;
		ComponentNames = null;
		ComponentPrefix = null;
		NumComponents = 0;
		ComponentPrefixLen = 0;
		ComponentBits = 0;
		Count = 0;
		ProgressShown = 0;
		Achieved = false;
		UnlockTime = 0;
		AchievementMgr = null;
		ShowOnHUD = false;
		Name = null;
		Stat = null;
	}

	public virtual void Init() { }
	public virtual void ListenForEvents() { }

	public int GetAchievementID() => AchievementID;
	public void SetAchievementID(int achievementID) => AchievementID = achievementID;
	public void SetName(ReadOnlySpan<char> name) => Name = new(name);
	public ReadOnlySpan<char> GetName() => Name;
	public ReadOnlySpan<char> GetStat() => Stat ?? GetName();
	public AchievementFlags GetFlags() => Flags;
	public void SetGoal(int goal) => Goal = goal;
	public int GetGoal() => Goal;
	public bool HasComponents() => (Flags & AchievementFlags.HasComponents) > 0;
	public void SetPointValue(int pointValue) => PointValue = pointValue;
	public int GetPointValue() => PointValue;
	public bool ShouldHideUntilAchieved() => HideUntilAchieved;
	public void SetHideUntilAchieved(bool hide) => HideUntilAchieved = hide;
	public void SetStoreProgressInSteam(bool storeProgressInSteam) => bStoreProgressInSteam = storeProgressInSteam;
	public bool StoreProgressInSteam() => bStoreProgressInSteam;
	public virtual bool ShouldShowProgressNotification() => true;
	public virtual void OnPlayerStatsUpdate() { }

	public virtual bool ShouldSaveGlobal() => ((Flags & AchievementFlags.SaveGlobal) > 0 && GetCount() > 0) || IsAchieved() || ProgressShown > 0 || ShouldShowOnHUD();
	public void SetCount(int count) => Count = count;
	public int GetCount() => Count;
	public void SetProgressShown(int progressShown) => ProgressShown = progressShown;
	public int GetProgressShown() => ProgressShown;
	public virtual bool IsAchieved() => Achieved;
	public virtual bool LocalPlayerCanEarn() => true;
	public void SetAchieved(bool achieved) => Achieved = achieved;
	public virtual bool IsMetaAchievement() => false;
	public virtual bool AlwaysListen() => false;
	public virtual bool AlwaysEnabled() => false;

	public virtual void OnAchieved() { }
	public uint GetUnlockTime() => UnlockTime;
	public void SetUnlockTime(uint unlockTime) => UnlockTime = unlockTime;

	public ulong GetComponentBits() => ComponentBits;
	public virtual void PrintAdditionalStatus() { }
	public virtual void OnSteamUserStatsStored() { }
	public virtual void UpdateAchievement(int data) { }
	public bool ShouldShowOnHUD() => ShowOnHUD;

	public virtual void Think() { }

	public ReadOnlySpan<char> GetMapNameFilter() => MapNameFilter;
	public AchievementMgr? GetAchievementMgr() => AchievementMgr;

	protected void SetStat(ReadOnlySpan<char> statName) => Stat = new(statName);

	public void SetFlags(AchievementFlags flags) {
		Assert((flags & (AchievementFlags.SaveWithGame | AchievementFlags.SaveGlobal)) != 0);

		Flags = flags;
	}

	public override void FireGameEvent(IGameEvent ev) {
		if (!IsActive())
			return;

		if (MapNameFilter != null && 0 != strcmp(AchievementMgr!.GetMapName(), MapNameFilter))
			return;

		ReadOnlySpan<char> name = ev.GetName();
		if (0 == strcmp(name, "teamplay_round_win")) {
			if ((Flags & AchievementFlags.FilterFullRoundOnly) != 0 && false == ev.GetBool("full_round"))
				return;
		}

		FireGameEvent_Internal(ev);
	}

	protected virtual void FireGameEvent_Internal(IGameEvent ev) { }

	protected void SetVictimFilter(ReadOnlySpan<char> className) {
		VictimClassNameFilter = new(className);
	}

	protected void SetAttackerFilter(ReadOnlySpan<char> className) {
		AttackerClassNameFilter = new(className);
	}

	protected void SetInflictorFilter(ReadOnlySpan<char> className) {
		InflictorClassNameFilter = new(className);
	}

	protected void SetInflictorEntityNameFilter(ReadOnlySpan<char> entityName) {
		InflictorEntityNameFilter = new(entityName);
	}

	protected void SetMapNameFilter(ReadOnlySpan<char> mapName) {
		MapNameFilter = new(mapName);
	}

	public void SetGameDirFilter(ReadOnlySpan<char> gameDir) {
		GameDirFilter = new(gameDir);
	}

	protected void SetComponentPrefix(ReadOnlySpan<char> prefix) {
		ComponentPrefix = new(prefix);
		ComponentPrefixLen = (int)strlen(prefix);
	}

	public virtual void Event_EntityKilled(BaseEntity victim, BaseEntity attacker, BaseEntity inflictor, IGameEvent ev) {
		Assert((GetFlags() & AchievementFlags.ListenKillEvents) != 0);
		if ((GetFlags() & AchievementFlags.ListenKillEvents) == 0)
			return;

		IncrementCount();
	}

	protected internal void IncrementCount(int optIncrement = 0) {
		if (!IsAchieved() && LocalPlayerCanEarn()) {
			if (!AlwaysEnabled() && !AchievementMgr!.CheckAchievementsEnabled()) {
				Msg($"Achievements disabled, ignoring achievement progress for {Name}\n");
				return;
			}

			if (optIncrement > 0) {
				Count += optIncrement;
				if (Count > Goal)
					Count = Goal;
			}
			else
				Count++;

			if ((GetFlags() & AchievementFlags.SaveGlobal) != 0)
				AchievementMgr.SetDirty(true);

			if (cc_achievement_debug.GetInt() != 0)
				Msg($"Achievement count increased for {Name}: {Count}/{Goal}\n");

			if (StoreProgressInSteam() && Steam3Client().SteamUserStats() != null) {
				Span<char> progressName = stackalloc char[1024];
				sprintf(progressName, "%s_STAT").S(GetStat());
				bool ret = Steam3Client().SteamUserStats().SetStat(progressName.SliceNullTerminatedString(), Count);
				if (!ret)
					DevMsg($"ISteamUserStats::GetStat failed to set progress value in Steam for achievement {progressName.SliceNullTerminatedString()}\n");

				AchievementMgr.SetDirty(true);
			}

			if (Goal > 0) {
				if (Count >= Goal)
					AwardAchievement();
				else
					HandleProgressUpdate();
			}
		}
#if DEBUG
		else if (cc_achievement_debug.GetInt() != 0)
			Msg($"Achievement count not increased for {Name}: achieved={IsAchieved()} canEarn={LocalPlayerCanEarn()}\n");
#endif
	}

	public void SetShowOnHUD(bool show) {
		if (ShowOnHUD != show)
			AchievementMgr!.SetDirty(true);

		ShowOnHUD = show;
	}

	protected void HandleProgressUpdate() {
		if (ProgressMsgIncrement > 0 && Count >= ProgressMsgMinimum && 0 == Count % ProgressMsgIncrement) {
			int progress = Count / ProgressMsgIncrement;
			if (progress > ProgressShown) {
				ShowProgressNotification();
				ProgressShown = progress;
				AchievementMgr!.SetDirty(true);
			}
		}
	}

	protected internal virtual void CalcProgressMsgIncrement() {
		ProgressMsgIncrement = Goal / 4;
		if (0 != Goal % 4) {
			if (0 == Goal % 3)
				ProgressMsgIncrement = Goal / 3;
			else if (0 == Goal % 5)
				ProgressMsgIncrement = Goal / 5;
		}

		if (ProgressMsgIncrement < 5)
			ProgressMsgIncrement = 0;
	}

	protected void SetNextThink(float thinkTime) {
		AchievementMgr!.SetAchievementThink(this, thinkTime);
	}

	protected void ClearThink() {
		AchievementMgr!.SetAchievementThink(this, Game.Shared.AchievementMgr.THINK_CLEAR);
	}

	protected internal void EvaluateNewAchievement() {
		if (!IsAchieved() && Goal > 0 && Count >= Goal)
			AwardAchievement();
	}

	public void EvaluateIsAlreadyAchieved() {
		if (!IsAchieved() && Goal > 0 && Count >= Goal)
			Achieved = true;
	}

	public virtual void OnMapEvent(ReadOnlySpan<char> eventName) {
		Assert((Flags & AchievementFlags.ListenMapEvents) != 0);

		if (0 == stricmp(eventName, GetName()))
			IncrementCount();
	}

	protected void AwardAchievement() {
		Assert(!IsAchieved());
		if (IsAchieved())
			return;

		AchievementMgr!.AwardAchievement(AchievementID);
	}

	public void OnComponentEvent(ReadOnlySpan<char> componentName) {
		for (int i = 0; i < NumComponents; i++) {
			if (0 == strcmp(componentName, ComponentNames![i])) {
				EnsureComponentBitSetAndEvaluate(i);
				return;
			}
		}
	}

	public void EnsureComponentBitSetAndEvaluate(int bitNumber) {
		Assert(bitNumber < 64);

		if (IsAchieved())
			return;

		ulong bitMask = (ulong)1 << bitNumber;

		if (0 == (bitMask & ComponentBits)) {
			if (!AlwaysEnabled() && !AchievementMgr!.CheckAchievementsEnabled()) {
				Msg($"Achievements disabled, ignoring achievement component for {Name}\n");
				return;
			}

			SetComponentBits(ComponentBits | bitMask);
			if (Count != Goal) {
				AchievementMgr.SetDirty(true);

				if (cc_achievement_debug.GetInt() != 0)
					Msg($"Component {bitNumber} for achievement {Name} found\n");

				ShowProgressNotification();
			}
		}
		else {
			if (cc_achievement_debug.GetInt() != 0)
				Msg($"Component {bitNumber} for achievement {Name} found, but already had that component\n");
		}

		Assert(Count <= Goal);
		if (Count == Goal)
			AwardAchievement();
	}

	protected void ShowProgressNotification() {
		if (!ShouldShowProgressNotification())
			return;

		IGameEvent? ev = gameeventmanager!.CreateEvent("achievement_event");
		if (ev != null) {
			ev.SetString("achievement_name", GetName());
			ev.SetInt("cur_val", Count);
			ev.SetInt("max_val", Goal);
#if GAME_DLL
			gameeventmanager.FireEvent(ev);
#else
			gameeventmanager.FireEventClientSide(ev);
#endif
		}
	}

	public virtual void PreRestoreSavedGame() {
		if ((Flags & AchievementFlags.SaveWithGame) != 0)
			Count = 0;
	}

	public virtual void PostRestoreSavedGame() => EvaluateIsAlreadyAchieved();

	public void SetComponentBits(ulong componentBits) {
		Assert((Flags & AchievementFlags.HasComponents) != 0);
		ComponentBits = componentBits;

		Count = BitOperations.PopCount(componentBits);
	}

	public virtual bool ShouldSaveWithGame() => (Flags & AchievementFlags.SaveWithGame) > 0 && GetCount() > 0 && !IsAchieved();

	public virtual bool IsActive() {
		if (IsAchieved())
			return false;

		if (MapNameFilter != null && 0 != strcmp(AchievementMgr!.GetMapName(), MapNameFilter))
			return false;

		return true;
	}

	public virtual void GetSettings(KeyValues nodeOut) {
		nodeOut.SetInt("value", IsAchieved() ? 1 : 0);

		if (HasComponents())
			nodeOut.SetUint64("data", ComponentBits);
		else {
			if (!IsAchieved())
				nodeOut.SetInt("data", Count);
		}
		nodeOut.SetInt("hud", ShouldShowOnHUD() ? 1 : 0);
		nodeOut.SetInt("msg", ProgressShown);
	}

	public virtual void ApplySettings(KeyValues nodeIn) {
		if (nodeIn.GetInt("value") > 0) {
			Count = Goal;
			Achieved = true;
		}
		else if (!HasComponents())
			Count = nodeIn.GetInt("data");

		if (HasComponents()) {
			ulong componentBits = nodeIn.GetUint64("data");
			SetComponentBits(componentBits);
		}
		SetShowOnHUD(nodeIn.GetBool("hud", false));
		ProgressShown = nodeIn.GetInt("msg");
	}
}

public abstract class FailableAchievement : BaseAchievement
{
	public static readonly new DataMap DataDesc = new(typeof(FailableAchievement), BaseAchievement.DataDesc, [
		Source.DEFINE<FailableAchievement>.FIELD(nameof(Activated), FieldType.Boolean),
		Source.DEFINE<FailableAchievement>.FIELD(nameof(Failed), FieldType.Boolean),
	]);

	protected bool Activated;
	protected bool Failed;

	public FailableAchievement() : base() {
		Failed = false;
		Activated = false;
	}

	public override bool ShouldSaveWithGame() => (Flags & AchievementFlags.SaveWithGame) > 0 && (Activated || Failed);

	public override void PreRestoreSavedGame() {
		Failed = false;
		Activated = false;

		base.PreRestoreSavedGame();
	}

	public override void PostRestoreSavedGame() {
		if (!Failed && GetActivationEventName().IsEmpty)
			Activated = true;

		if (Activated)
			Activate();

		base.PostRestoreSavedGame();
	}

	public override bool IsAchieved() => !Failed && base.IsAchieved();
	public override bool IsActive() => Activated && !Failed && base.IsActive();
	public bool IsFailed() => Failed;

	public override void OnMapEvent(ReadOnlySpan<char> eventName) {
		if (!Activated && 0 == stricmp(eventName, GetActivationEventName()))
			OnActivationEvent();
		else if (Activated && 0 == stricmp(eventName, GetEvaluationEventName()))
			OnEvaluationEvent();
	}

	public virtual void OnActivationEvent() => Activate();
	public abstract ReadOnlySpan<char> GetActivationEventName();
	public abstract ReadOnlySpan<char> GetEvaluationEventName();

	protected void Activate() {
		Activated = true;
		ListenForEvents();
		if (cc_achievement_debug.GetInt() != 0)
			Msg($"Failable achievement {Name} now active\n");
	}

	public virtual void OnEvaluationEvent() {
		if (!Failed)
			IncrementCount();

		if (cc_achievement_debug.GetInt() != 0)
			Msg($"Failable achievement {Name} has been evaluated ({(Failed ? "FAILED" : "AWARDED")}), now inactive\n");
	}

	public void SetFailed() {
		if (!Failed) {
			Failed = true;

			if (cc_achievement_debug.GetInt() != 0)
				Msg($"Achievement failed: {Name} ({Name})\n");
		}
	}
}

public class MapAchievement : BaseAchievement
{
	public override void Init() {
		SetFlags(AchievementFlags.ListenMapEvents | AchievementFlags.SaveGlobal);
		SetGoal(1);
	}
}

public class Achievement_AchievedCount : BaseAchievement
{
	int NumRequired;
	int LowRange;
	int HighRange;

	public override void Init() {
		SetFlags(AchievementFlags.SaveGlobal);
		SetGoal(1);
		SetAchievementsRequired(0, 0, 0);
	}

	public override void OnSteamUserStatsStored() => Assert(false);

	public override bool IsMetaAchievement() => true;

	public int GetLowRange() => LowRange;
	public int GetHighRange() => HighRange;
	public int GetNumRequired() => NumRequired;

	protected void SetAchievementsRequired(int numRequired, int lowRange, int highRange) {
		NumRequired = numRequired;
		LowRange = lowRange;
		HighRange = highRange;
	}
}

public class BaseAchievementHelper
{
	public BaseAchievementHelper(AchievementCreateFunc createFunc) {
		Create = createFunc;
		Next = First;
		First = this;
	}
	public AchievementCreateFunc Create;
	public BaseAchievementHelper? Next;
	public static BaseAchievementHelper? First;

	public static BaseAchievementHelper DECLARE_ACHIEVEMENT_<T>(int achievementID, string achievementName, string? gameDirFilter, int pointValue, bool hidden) where T : BaseAchievement, new()
		=> DECLARE_ACHIEVEMENT_(static () => new T(), achievementID, achievementName, gameDirFilter, pointValue, hidden);

	public static BaseAchievementHelper DECLARE_ACHIEVEMENT_(AchievementCreateFunc create, int achievementID, string achievementName, string? gameDirFilter, int pointValue, bool hidden)
		=> new(() => {
			BaseAchievement achievement = create();
			achievement.SetAchievementID(achievementID);
			achievement.SetName(achievementName);
			achievement.SetPointValue(pointValue);
			achievement.SetHideUntilAchieved(hidden);
			if (gameDirFilter != null)
				achievement.SetGameDirFilter(gameDirFilter);
			return achievement;
		});

	public static BaseAchievementHelper DECLARE_ACHIEVEMENT<T>(int achievementID, string achievementName, int pointValue) where T : BaseAchievement, new()
		=> DECLARE_ACHIEVEMENT_<T>(achievementID, achievementName, null, pointValue, false);

	public static BaseAchievementHelper DECLARE_MAP_EVENT_ACHIEVEMENT_(int achievementID, string achievementName, string? gameDirFilter, int pointValue, bool hidden)
		=> DECLARE_ACHIEVEMENT_<MapAchievement>(achievementID, achievementName, gameDirFilter, pointValue, hidden);

	public static BaseAchievementHelper DECLARE_MAP_EVENT_ACHIEVEMENT(int achievementID, string achievementName, int pointValue)
		=> DECLARE_MAP_EVENT_ACHIEVEMENT_(achievementID, achievementName, null, pointValue, false);

	public static BaseAchievementHelper DECLARE_MAP_EVENT_ACHIEVEMENT_HIDDEN(int achievementID, string achievementName, int pointValue)
		=> DECLARE_MAP_EVENT_ACHIEVEMENT_(achievementID, achievementName, null, pointValue, true);
}
#endif
