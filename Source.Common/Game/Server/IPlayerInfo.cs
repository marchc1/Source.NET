using Source.Common.Engine;
using Source.Common.Mathematics;

using System.Numerics;

namespace Source.Common.Game.Server;

public struct BotCmd
{
	/// <summary>
	/// For matching server and client commands for debugging
	/// </summary>
	public int CommandNumber;
	/// <summary>
	/// the tick the client created this command
	/// </summary>
	public int TickCount;
	/// <summary>
	/// Player instantaneous view angles.
	/// </summary>
	public QAngle ViewAngles;
	/// <summary>
	/// forward velocity.
	/// </summary>
	public float ForwardMove;
	/// <summary>
	///  sideways velocity.
	/// </summary>
	public float SideMove;
	/// <summary>
	///  upward velocity.
	/// </summary>
	public float UpMove;
	/// <summary>
	/// Attack button states
	/// </summary>
	public int Buttons;
	/// <summary>
	/// Impulse command issued.
	/// </summary>
	public byte Impulse;
	/// <summary>
	/// Current weapon id
	/// </summary>
	public int WeaponSelect;
	public int WeaponSubtype;

	/// <summary>
	/// For shared random functions
	/// </summary>
	public int RandomSeed;      

	/// <summary>
	/// mouse accum in x from create move
	/// </summary>
	public short MouseDeltaX;
	/// <summary>
	/// mouse accum in y from create move
	/// </summary>
	public short MouseDeltaY;

	/// <summary>
	/// Client only, tracks whether we've predicted this command at least once
	/// </summary>
	public bool HasBeenPredicted;

	public BotCmd() {
		Reset();
	}

	public void Reset() {
		CommandNumber = 0;
		TickCount = 0;
		ViewAngles.Init();
		ForwardMove = 0.0f;
		SideMove = 0.0f;
		UpMove = 0.0f;
		Buttons = 0;
		Impulse = 0;
		WeaponSelect = 0;
		WeaponSubtype = 0;
		RandomSeed = 0;
		MouseDeltaX = 0;
		MouseDeltaY = 0;

		HasBeenPredicted = false;
	}
}

public interface IPlayerInfo
{
	ReadOnlySpan<char> GetName();
	int GetUserID();
	/// <summary>
	/// returns the string of their network (i.e Steam) ID
	/// </summary>
	/// <returns></returns>
	ReadOnlySpan<char> GetNetworkIDString();
	/// <summary>
	/// returns the team the player is on
	/// </summary>
	/// <returns></returns>
	int GetTeamIndex();
	/// <summary>
	/// changes the player to a new team (if the game dll logic allows it)
	/// </summary>
	/// <param name="iTeamNum"></param>
	void ChangeTeam(int iTeamNum);
	/// <summary>
	/// returns the number of kills this player has (exact meaning is mod dependent)
	/// </summary>
	/// <returns></returns>
	int GetFragCount();
	/// <summary>
	/// returns the number of deaths this player has (exact meaning is mod dependent)
	/// </summary>
	/// <returns></returns>
	int GetDeathCount();
	/// <summary>
	/// returns if this player slot is actually valid
	/// </summary>
	/// <returns></returns>
	bool IsConnected();
	/// <summary>
	/// returns the armor/health of the player (exact meaning is mod dependent)
	/// </summary>
	/// <returns></returns>
	int GetArmorValue();

	// various player flags
	bool IsHLTV();
	bool IsPlayer();
	bool IsFakeClient();
	bool IsDead();
	bool IsInAVehicle();
	bool IsObserver();

	Vector3 GetAbsOrigin();
	QAngle GetAbsAngles();
	Vector3 GetPlayerMins();
	Vector3 GetPlayerMaxs();
	/// <summary>
	/// the name of the weapon currently being carried
	/// </summary>
	/// <returns></returns>
	ReadOnlySpan<char> GetWeaponName();
	/// <summary>
	/// the name of the player model in use
	/// </summary>
	/// <returns></returns>
	ReadOnlySpan<char> GetModelName();
	/// <summary>
	/// current player health
	/// </summary>
	/// <returns></returns>
	int GetHealth();
	/// <summary>
	/// max health value
	/// </summary>
	/// <returns></returns>
	int GetMaxHealth();
	/// <summary>
	/// the last user input from this player
	/// </summary>
	/// <returns></returns>
	BotCmd GetLastUserCommand();

	bool IsReplay();
}


public interface IPlayerInfoManager
{
	IPlayerInfo GetPlayerInfo(Edict edict);
	GlobalVars GetGlobalVars();
}


public interface IBotController
{
	void SetAbsOrigin(in Vector3 vec);
	void SetAbsAngles(in QAngle ang);
	void SetLocalOrigin(in Vector3 origin);
	Vector3 GetLocalOrigin();
	void SetLocalAngles(in QAngle angles);
	QAngle GetLocalAngles();

	// strip them of weapons, etc
	void RemoveAllItems(bool removeSuit);
	// give them a weapon
	void SetActiveWeapon(ReadOnlySpan<char> weaponName);
	// check various effect flags
	bool IsEFlagSet(int eflagMask);
	// fire a  move command to the bot
	void RunPlayerMove(ref BotCmd ucmd);
}

public interface IBotManager
{
	IBotController GetBotController(Edict edict);
	Edict? CreateBot( ReadOnlySpan<char> botname );
}
