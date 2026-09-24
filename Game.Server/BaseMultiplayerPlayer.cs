using Source.Common;
using Source.Common.Commands;
using Source.Common.Formats.Keyvalues;

namespace Game.Server;

// todo!! AI_ExpresserHost<BasePlayer>
public class BaseMultiplayerPlayer : BasePlayer
{
	public enum ChatIgnore
	{
		None = 0,
		All,
		Team
	}

	public int IgnoreGlobalChat;
	public float AreaCaptureScoreAccumulator;
	public float CapPointScoreRate;
	protected int CurrentConcept;
	// Multiplayer_Expresser? Expresser;
	TimeUnit_t ConnectionTime;
	float LastForcedChangeTeamTime;
	int BalanceScore;
	KeyValues? AchievementKV;
	// Dictionary<float, int> RateLimitLastCommandTimes;

	public BaseMultiplayerPlayer() {
		// CurrentConcept = MP_CONCEPT_NONE;
		LastForcedChangeTeamTime = -1;
		BalanceScore = 0;
		ConnectionTime = gpGlobals.CurTime;
		AchievementKV = new KeyValues("achievement_counts");
		AreaCaptureScoreAccumulator = 0.0f;
	}

	// public override void Spawn() => throw new NotImplementedException();

	// public override void PostConstructor(ReadOnlySpan<char> classname) => throw new NotImplementedException();
	// public virtual void ModifyOrAppendCriteria(AI_CriteriaSet criteriaSet) => throw new NotImplementedException();

	// public virtual bool SpeakIfAllowed(AIConcept_t concept, ReadOnlySpan<char> modifiers = default, Span<char> outResponseChosen = default, IRecipientFilter? filter = null) => throw new NotImplementedException();
	// public virtual IResponseSystem? GetResponseSystem() => throw new NotImplementedException();
	// public bool SpeakConcept(AI_Response response, int concept) => throw new NotImplementedException();
	public virtual bool SpeakConceptIfAllowed(int concept, ReadOnlySpan<char> modifiers = default, Span<char> outResponseChosen = default, IRecipientFilter? filter = null) => throw new NotImplementedException();

	public virtual bool CanHearAndReadChatFrom(BasePlayer player) => throw new NotImplementedException();
	// public virtual bool CanSpeak() => throw new NotImplementedException();
	public virtual bool CanBeAutobalanced() => throw new NotImplementedException();

	// public override void Precache() => throw new NotImplementedException();

	// public override bool ClientCommand(in TokenizedCommand args) => throw new NotImplementedException();

	public virtual bool CanSpeakVoiceCommand() => throw new NotImplementedException();
	public virtual bool ShouldShowVoiceSubtitleToEnemy() => throw new NotImplementedException();
	public virtual void NoteSpokeVoiceCommand(ReadOnlySpan<char> scenePlayed) => throw new NotImplementedException();

	public virtual void OnAchievementEarned(int achievement) => throw new NotImplementedException();

	// public virtual AI_Expresser? GetExpresser() => throw new NotImplementedException();
	// public virtual Multiplayer_Expresser? GetMultiplayerExpresser() => throw new NotImplementedException();

	public void SetLastForcedChangeTeamTimeToNow() => throw new NotImplementedException();
	public float GetLastForcedChangeTeamTime() => throw new NotImplementedException();

	public void SetTeamBalanceScore(int score) => throw new NotImplementedException();
	public int GetTeamBalanceScore() => throw new NotImplementedException();

	public virtual int CalculateTeamBalanceScore() => throw new NotImplementedException();

	public void AwardAchievement(int achievement, int count = 1) {
		Assert(achievement >= 0 && achievement < 0xFFFF);

		SingleUserRecipientFilter filter = new(this);

		UserMessageBegin(filter, "AchievementEvent");
		WRITE_SHORT((short)achievement);
		WRITE_SHORT((short)count);
		MessageEnd();
	}

	public int GetPerLifeCounterKV(ReadOnlySpan<char> name) => throw new NotImplementedException();
	public void SetPerLifeCounterKV(ReadOnlySpan<char> name, int value) => throw new NotImplementedException();
	public void ResetPerLifeCounters() => throw new NotImplementedException();

	public KeyValues? GetPerLifeCounterKeys() => throw new NotImplementedException();

	public void EscortScoringThink() => throw new NotImplementedException();
	public void StartScoringEscortPoints(float rate) => throw new NotImplementedException();
	public void StopScoringEscortPoints() => throw new NotImplementedException();

	public float GetConnectionTime() => throw new NotImplementedException();

	public bool ShouldRunRateLimitedCommand(in TokenizedCommand args) => throw new NotImplementedException();
	public bool ShouldRunRateLimitedCommand(ReadOnlySpan<char> command) => throw new NotImplementedException();

	// protected virtual AI_Expresser? CreateExpresser() => throw new NotImplementedException();
}
