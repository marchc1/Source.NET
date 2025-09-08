namespace Source;

public static class Constants
{
	public const int MAX_CMD_BUFFER = 4000;

	public const int MAX_EDICTS = 1 << MAX_EDICT_BITS;
	public const int MAX_EDICT_BITS = 13;
	public const int NUM_ENT_ENTRY_BITS = MAX_EDICT_BITS + 1;
	public const int NUM_ENT_ENTRIES = 1 << NUM_ENT_ENTRY_BITS;

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
	public const int SP_MODEL_INDEX_BITS = 13;

	public const int MAX_PACKEDENTITY_DATA = 16384;
	public const int MAX_PACKEDENTITY_PROPS = 4096;

	public const int ABSOLUTE_PLAYER_LIMIT = 255;
	public const int ABSOLUTE_PLAYER_LIMIT_DW = ((ABSOLUTE_PLAYER_LIMIT / 32) + 1);
	public const int MAX_PLAYERS = ABSOLUTE_PLAYER_LIMIT;
	public const int VOICE_MAX_PLAYERS = MAX_PLAYERS;
	public const int VOICE_MAX_PLAYERS_DW = (VOICE_MAX_PLAYERS / 32) + ((VOICE_MAX_PLAYERS & 31) != 0 ? 1 : 0);

	public const double MIN_FPS = 0.1;
	public const double MAX_FPS = 1000;

	public const double DEFAULT_TICK_INTERVAL = 0.015;
	public const double MINIMUM_TICK_INTERVAL = 0.001;
	public const double MAXIMUM_TICK_INTERVAL = 0.1;

	public const int NUM_ENT_ENTRY_BITS = MAX_EDICT_BITS + 1;
	public const int NUM_ENT_ENTRIES = 1 << NUM_ENT_ENTRY_BITS;
	public const int ENT_ENTRY_MASK = NUM_ENT_ENTRIES - 1;
	public const ulong INVALID_EHANDLE_INDEX = unchecked(0xFFFFFFFF);
	public const int NUM_SERIAL_NUM_BITS = 32 - NUM_ENT_ENTRY_BITS;

	public const int NUM_NETWORKED_EHANDLE_SERIAL_NUMBER_BITS = 10;
	public const int NUM_NETWORKED_EHANDLE_BITS = MAX_EDICT_BITS + NUM_NETWORKED_EHANDLE_SERIAL_NUMBER_BITS;
	public const int INVALID_NETWORKED_EHANDLE_VALUE = (1 << NUM_NETWORKED_EHANDLE_BITS) - 1;
}
