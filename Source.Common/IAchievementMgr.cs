namespace Source.Common;

public interface IAchievement
{
	int GetAchievementID();
	ReadOnlySpan<char> GetName();
	AchievementFlags GetFlags();
	int GetGoal();
	int GetCount();
	bool IsAchieved();
	int GetPointValue();
	bool ShouldSaveWithGame();
	bool ShouldHideUntilAchieved();
	bool ShouldShowOnHUD();
	void SetShowOnHUD(bool show);
}

public interface IAchievementMgr
{
	IAchievement? GetAchievementByIndex(int index);
	IAchievement? GetAchievementByID(int id);
	int GetAchievementCount();
	void InitializeAchievements();
	void AwardAchievement(int achievementID);
	void OnMapEvent(ReadOnlySpan<char> eventName );
	void DownloadUserData();
	void EnsureGlobalStateLoaded();
	void SaveGlobalStateIfDirty(bool bAsync);
	bool HasAchieved( ReadOnlySpan<char> name );
	bool WereCheatsEverOn();
}

public enum AchievementFlags
{
	ListenKillEvents = 0x0001,
	ListenMapEvents = 0x0002,
	ListenComponentEvents = 0x0004,
	HasComponents = 0x0020,
	SaveWithGame = 0x0040,
	SaveGlobal = 0x0080,
	FilterAttackerIsPlayer = 0x0100,
	FilterVictimIsPlayerEnemy = 0x0200,
	FilterFullRoundOnly = 0x0400,

	LISTEN_PLAYER_KILL_ENEMY_EVENTS = ListenKillEvents | FilterAttackerIsPlayer | FilterVictimIsPlayerEnemy,
	LISTEN_KILL_ENEMY_EVENTS = ListenKillEvents | FilterVictimIsPlayerEnemy
}
