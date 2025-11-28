namespace Source;

public enum EntityFlags
{
	OnGround = 1 << 0,
	Ducking = 1 << 1,
	AnimDucking = 1 << 2,
	WaterJump = 1 << 3,
	OnTrain = 1 << 4,
	InRain = 1 << 5,
	Frozen = 1 << 6,
	AtControls = 1 << 7,
	Client = 1 << 8,
	FakeClient = 1 << 9,
	InWater = 1 << 10,
	Fly = 1 << 11,
	Swim = 1 << 12,
	Conveyor = 1 << 13,
	NPC = 1 << 14,
	GodMode = 1 << 15,
	NoTarget = 1 << 16,
	AimTarget = 1 << 17,
	PartialGround = 1 << 18,
	StaticProp = 1 << 19,
	Graphed = 1 << 20,
	Grenade = 1 << 21,
	StepMovement = 1 << 22,
	DontTouch = 1 << 23,
	BaseVelocity = 1 << 24,
	WorldBrush = 1 << 25,
	Object = 1 << 26,
	KillMe = 1 << 27,
	OnFire = 1 << 28,
	Dissolving = 1 << 29,
	TransRagdoll = 1 << 30,
	UnblockableByPlayer = 1 << 31
}

public enum MoveType
{
	None = 0,
	Isometric,
	Walk,
	Step,
	Fly,
	FlyGravity,
	VPhysics,
	Push,
	Noclip,
	Ladder,
	Observer,
	Custom,

	Last = Custom,
	MaxBits = 4
}

public enum MoveCollide
{
	Default = 0,
	FlyBounce,
	FlyCustom,
	FlySlide,
	Count,
	MaxBits = 3
}

public enum SolidType
{
	None = 0,
	BSP = 1,
	BBox = 2,
	OBB = 3,
	OBBYaw = 4,
	Custom = 5,
	VPhysics = 6,
	Last,
}

public enum SolidFlags
{
	CustomRayTest = 0x0001,
	CustomBoxTest = 0x0002,
	NotSolid = 0x0004,
	Trigger = 0x0008,
	NotStandable = 0x0010,
	VolumeContents = 0x0020,
	ForceWorldAligned = 0x0040,
	UseTriggerBounds = 0x0080,
	RootParentAligned = 0x0100,
	TriggerTouchDebris = 0x0200,

	MaxBits = 10
}

public enum LifeState
{
	Alive = 0,
	Dying = 1,
	Dead = 2,
	Respawnable = 3,
	DiscardBody = 4
}

public enum EntityEffects
{
	BoneMerge = 0x001,
	BrightLight = 0x002,
	DimLight = 0x004,
	NoInterp = 0x008,
	NoShadow = 0x010,
	NoDraw = 0x020,
	NoReceiveShadow = 0x040,
	BoneMergeFastCull = 0x080,
	ItemBlink = 0x100,
	ParentAnimates = 0x200,

	MaxBits = 16, // < gmod increased 10 -> 16
	ParityBits = 3,
	ParityMask = (1 << ParityBits) - 1,
	MuzzleflashBits = 2
}

public enum FixAngle
{
	None,
	Absolute,
	Relative
}

public enum BreakModel
{
	Glass = 0x01,
	Metal = 0x02,
	Flesh = 0x04,
	Wood = 0x08,
	Smoke = 0x10,
	Trans = 0x20,
	Concrete = 0x40
}

public enum BounceSound
{
	Glass = BreakModel.Glass,
	Metal = BreakModel.Metal,
	Flesh = BreakModel.Flesh,
	Wood = BreakModel.Wood,
	Shrap = 0x10,
	Shell = 0x20,
	Concrete = BreakModel.Concrete,
	Shotshell = 0x80
}

public enum RenderMode : byte
{
	Normal = 0,
	TransColor,
	TransTexture,
	Glow,
	TransAlpha,
	TransAdd,
	Environmental,
	TransAddFrameBlend,
	TransAlphaAdd,
	WorldGlow,
	None,
	Count,
}

public enum RenderFx
{
	None = 0,
	PulseSlow,
	PulseFast,
	PulseSlowWide,
	PulseFastWide,
	FadeSlow,
	FadeFast,
	SolidSlow,
	SolidFast,
	StrobeSlow,
	StrobeFast,
	StrobeFaster,
	FlickerSlow,
	FlickerFast,
	NoDissipation,
	Distort,
	Hologram,
	Explode,
	GlowShell,
	ClampMinScale,
	EnvRain,
	EnvSnow,
	Spotlight,
	Ragdoll,
	PulseFastWider,
	Max
}

public enum CollisionGroup
{
	None = 0,
	Debris,
	DebrisTrigger,
	InteractiveDebris,
	Interactive,
	Player,
	BreakableGlass,
	Vehicle,
	PlayerMovement,
	NPC,
	InVehicle,
	Weapon,
	VehicleClip,
	Projectile,
	DoorBlocker,
	PassableDoor,
	Dissolving,
	Pushaway,
	NPCActor,
	NPCScripted,
	LastSharedCollisionGroup
}

public static class Constants
{
	public const int MAX_CMD_BUFFER = 4000;

	public const int MAX_EDICTS = 1 << MAX_EDICT_BITS;
	public const int MAX_EDICT_BITS = 13;

	/// <summary>
	/// Most Source games have this at 20; Garry's Mod has it at 24
	/// </summary>
	public const int DELTASIZE_BITS = 24;

	public const int MAX_EVENT_BITS = 9;
	public const int MAX_EVENT_NUMBER = 1 << MAX_EVENT_BITS;

	public const int MAX_PLAYER_NAME_LENGTH = 32;

	public const int MAX_SERVER_CLASSES = 1 << MAX_SERVER_CLASS_BITS;
	public const int MAX_SERVER_CLASS_BITS = 13;

	public const int MAX_CUSTOM_FILES = 4;
	public const int MAX_CUSTOM_FILE_SIZE = 524288;

	public const int MAX_AREA_STATE_BYTES = 32;
	public const int MAX_AREA_PORTAL_STATE_BYTES = 24;

	public const int MAX_USER_MSG_DATA = 255;
	public const int MAX_ENTITY_MSG_DATA = 255;
	public const int MAX_DECAL_INDEX_BITS = 9;
	public const int SP_MODEL_INDEX_BITS = 14; // < gmod changed this from 13 -> 14

	public const int MAX_PACKEDENTITY_DATA = 16384;
	public const int MAX_PACKEDENTITY_PROPS = 4096;

	public const int ABSOLUTE_PLAYER_LIMIT = 128;
	public const int ABSOLUTE_PLAYER_LIMIT_DW = ((ABSOLUTE_PLAYER_LIMIT / 32) + 1);
	public const int MAX_PLAYERS = ABSOLUTE_PLAYER_LIMIT;
	public const int VOICE_MAX_PLAYERS = MAX_PLAYERS;
	public const int VOICE_MAX_PLAYERS_DW = (VOICE_MAX_PLAYERS / 32) + ((VOICE_MAX_PLAYERS & 31) != 0 ? 1 : 0);

	public const float HIGH_DEFAULT = -121121.121121f;

	public const double MIN_FPS = 0.1;
	public const double MAX_FPS = 100000;

	public const double DEFAULT_TICK_INTERVAL = 0.015;
	public const double MINIMUM_TICK_INTERVAL = 0.001;
	public const double MAXIMUM_TICK_INTERVAL = 0.1;

	public const int NUM_ENT_ENTRY_BITS = MAX_EDICT_BITS + 1;
	public const int NUM_ENT_ENTRIES = 1 << NUM_ENT_ENTRY_BITS;
	public const int ENT_ENTRY_MASK = NUM_ENT_ENTRIES - 1;
	public const uint INVALID_EHANDLE_INDEX = unchecked(0xFFFFFFFF);
	public const int NUM_SERIAL_NUM_BITS = 32 - NUM_ENT_ENTRY_BITS;

	public const int NUM_NETWORKED_EHANDLE_SERIAL_NUMBER_BITS = 10;
	public const int NUM_NETWORKED_EHANDLE_BITS = MAX_EDICT_BITS + NUM_NETWORKED_EHANDLE_SERIAL_NUMBER_BITS;
	public const int INVALID_NETWORKED_EHANDLE_VALUE = (1 << NUM_NETWORKED_EHANDLE_BITS) - 1;
	/// <summary>
	/// THIS IS TEMPORARY: We need to properly sync up with a client/server state before we can remove this!!!!!
	/// </summary>
	public const int TEMP_TOTAL_SERVER_CLASSES = 251;

	public const int MAX_DATATABLES = 1024;
	public const int MAX_DATATABLE_PROPS = 4096;
	public const int MAX_ARRAY_ELEMENTS = 2048;
	public const int DT_MAX_STRING_BITS = 9;
	public const int DT_MAX_STRING_BUFFERSIZE = 1 << DT_MAX_STRING_BITS;

	public const int DATATABLE_PROXY_INDEX_NOPROXY = 255;
	public const int DATATABLE_PROXY_INDEX_INVALID = 254;
	public const int MAX_EXCLUDE_PROPS = 512;

	public const int PROPINDEX_NUMBITS = 12;
	public const int MAX_TOTAL_SENDTABLE_PROPS = (1 << PROPINDEX_NUMBITS);

	public const int PLAYER_FLAG_BITS = 16; // gmod changed from 11 > 16
	public const int MAX_NETWORKID_LENGTH = 64;
}
