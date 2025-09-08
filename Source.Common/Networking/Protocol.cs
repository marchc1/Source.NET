using System.Net.Mail;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Source.Common.Networking;

public static class C2S
{
	public const byte Connect = (byte)'k';
}
public static class S2C
{
	public const uint MagicVersion = 1515145523;

	public const byte Challenge = (byte)'A';
	public const byte Connection = (byte)'B';
	public const byte ConnectionRejected = (byte)'9';
}

public static class A2S
{
	/// <summary>
	/// Retrieve a challenge number from a server.
	/// </summary>
	public const byte GetChallenge = (byte)'q';
	/// <summary>
	/// Forgot exactly
	/// </summary>
	public const byte RCON = (byte)'r';
	/// <summary>
	/// Retrieve information about the server, such as map, maxplayers, etc
	/// </summary>
	public const byte Info = (byte)'T';
	/// <summary>
	/// Retrieves all players online. Contains their name (32-chars-max), score, and time connected.
	/// <br/><b>Note</b>: Names are blank until <see cref="SignOnState.Full"/>.
	/// </summary>
	public const byte Player = (byte)'U';
	/// <summary>
	/// Retrieves all convars on the server that contain <see cref="ConVarFlag.Replicated"/>.
	/// </summary>
	public const byte Rules = (byte)'V';
	public const string KeyString = "Source Engine Query";
}
public static class SVC
{
	/// <summary>
	/// print text to console
	/// </summary>
	public const byte Print = 7;
	/// <summary>
	/// first message from server about game, map etc
	/// </summary>
	public const byte ServerInfo = 8;
	/// <summary>
	/// sends a sendtable description for a game class
	/// </summary>
	public const byte SendTable = 9;
	/// <summary>
	/// Info about classes (first byte is a CLASSINFO_ define).							
	/// </summary>
	public const byte ClassInfo = 10;
	/// <summary>
	/// tells client if server paused or unpaused
	/// </summary>
	public const byte SetPause = 11;
	/// <summary>
	/// inits shared string tables
	/// </summary>
	public const byte CreateStringTable = 12;
	/// <summary>
	/// updates a string table
	/// </summary>
	public const byte UpdateStringTable = 13;
	/// <summary>
	/// inits used voice codecs 
	/// </summary>& quality
	public const byte VoiceInit = 14;
	/// <summary>
	/// Voicestream data from the server
	/// </summary>
	public const byte VoiceData = 15;
	/// <summary>
	/// starts playing sound
	/// </summary>
	public const byte Sounds = 17;
	/// <summary>
	/// sets entity as point of view
	/// </summary>
	public const byte SetView = 18;
	/// <summary>
	/// sets/corrects players viewangle
	/// </summary>
	public const byte FixAngle = 19;
	/// <summary>
	/// adjusts crosshair in auto aim mode to lock on traget
	/// </summary>
	public const byte CrosshairAngle = 20;
	/// <summary>
	/// add a static decal to the world BSP
	/// </summary>
	public const byte BSPDecal = 21;
	/// <summary>
	/// a game specific message 
	/// </summary>
	public const byte UserMessage = 23;
	/// <summary>
	/// a message for an entity
	/// </summary>
	public const byte EntityMessage = 24;
	/// <summary>
	/// global game event fired
	/// </summary>
	public const byte GameEvent = 25;
	/// <summary>
	/// non-delta compressed entities
	/// </summary>
	public const byte PacketEntities = 26;
	/// <summary>
	/// non-reliable event object
	/// </summary>
	public const byte TempEntities = 27;
	/// <summary>
	/// only sound indices for now
	/// </summary>
	public const byte Prefetch = 28;
	/// <summary>
	/// display a menu from a plugin
	/// </summary>
	public const byte Menu = 29;
	/// <summary>
	/// list of known games events and fields
	/// </summary>
	public const byte GameEventList = 30;
	/// <summary>
	/// Server wants to know the value of a cvar on the client
	/// </summary>
	public const byte GetCvarValue = 31;
	/// <summary>
	/// Server submits KeyValues command for the client
	/// </summary>
	public const byte CmdKeyValues = 32;
	public const byte GMod_ServerToClient = 33;

	/// <summary>
	/// last known server message
	/// </summary>
	public const byte LastMessage = GMod_ServerToClient;
}

public static class CLC
{
	public const int ClientInfoCRC = -180714362;

	// client info (table CRC etc)
	public const byte ClientInfo = 8;
	// [CUserCmd]
	public const byte Move = 9;
	// Voicestream data from a client
	public const byte VoiceData = 10;
	// client acknowledges a new baseline seqnr
	public const byte BaselineAck = 11;
	// client acknowledges a new baseline seqnr
	public const byte ListenEvents = 12;
	// client is responding to a svc_GetCvarValue message.
	public const byte RespondCvarValue = 13;
	// client is sending a file's CRC to the server to be verified.
	public const byte FileCRCCheck = 14;
	// client is sending a save replay request to the server.
	public const byte SaveReplay = 15;
	public const byte CmdKeyValues = 16;
	// client is sending a file's MD5 to the server to be verified.
	public const byte FileMD5Check = 17;
	public const byte GMod_ClientToServer = 18;
	/// <summary>
	/// last known client message
	/// </summary>
	public const byte LastMessage = GMod_ClientToServer;
}

public static class Protocol
{
	public static readonly int MAX_TABLES_BITS = (int)MathF.Log2(32);

	public const int NUM_NEW_COMMAND_BITS = 4;
	public const int MAX_NEW_COMMANDS = (1 << NUM_NEW_COMMAND_BITS) - 1;

	public const int NUM_BACKUP_COMMAND_BITS = 3;
	public const int MAX_BACKUP_COMMANDS = (1 << NUM_BACKUP_COMMAND_BITS) - 1;

	public const int CONNECTIONLESS_HEADER = unchecked((int)0xFFFFFFFF);
	public const int QUERY_HEADER = -1;
	public const int SPLITPACKET_HEADER = -2;
	public const int COMPRESSEDPACKET_HEADER = -3;

	public const int VERSION = 24;

	public const int PROTOCOL_AUTHCERTIFICATE = 0x01;
	public const int PROTOCOL_HASHEDCDKEY = 0x02;
	public const int PROTOCOL_STEAM = 0x03;

	public const int STEAM_KEYSIZE = 2048;

	public const int UDP_HEADER_SIZE = 20 + 8; // internet (20) + 8 (udp)
	public const int MAX_DATAGRAM_PAYLOAD = 4000; // = maximum unreliable payload size
	public const int MAX_PAYLOAD = 288000;  // largest message we can send in bytes
	public const int HEADER_BYTES = 9;
	public const int MIN_MESSAGE = 5;
	public const int MAX_MESSAGE = (MAX_PAYLOAD + HEADER_BYTES + (16 - 1)) / 16 * 16;

	public const int NETMSG_TYPE_BITS = 6;
	public const int NETMSG_LENGTH_BITS = 11;

	public const int MIN_ROUTABLE_PAYLOAD = 16;
	public const int MAX_ROUTABLE_PAYLOAD = 1260;
	public const int DEFAULT_UDP_BUFFERSIZE = 131072;
	public const int MIN_USER_MAXROUTABLE_SIZE = 576;
	public const int MAX_USER_MAXROUTABLE_SIZE = MAX_ROUTABLE_PAYLOAD;
	public const int SIZEOF_SPLITPACKET = 4 * 4; // 4x int32's. I don't like this, but sizeof on structs both 1. requires unsafe and 2. isn't constant (???)
	public const int MIN_SPLIT_SIZE = MIN_USER_MAXROUTABLE_SIZE - SIZEOF_SPLITPACKET;
	public const int MAX_SPLIT_SIZE = MAX_USER_MAXROUTABLE_SIZE - SIZEOF_SPLITPACKET;
	public const int MAX_SPLITPACKET_SPLITS = (int)(MAX_MESSAGE / (float)MIN_SPLIT_SIZE);
	public const float SPLIT_PACKET_STALE_TIME = 2.0f;
	public const float SPLIT_PACKET_TRACKING_MAX = 256; // most number of outstanding split packets to allow

	public const int FRAGMENT_BITS = 8;
	public const int FRAGMENT_SIZE = 1 << FRAGMENT_BITS;
	public const int MAX_FILE_SIZE_BITS = 26;
	public const int MAX_FILE_SIZE = (1 << MAX_FILE_SIZE_BITS) - 1;// maximum transferable size is	64MB

	public const string INSTANCE_BASELINE_TABLENAME = "instancebaseline";
	public const string LIGHT_STYLES_TABLENAME = "lightstyles";
	public const string USER_INFO_TABLENAME = "userinfo";
	public const string SERVER_STARTUP_DATA_TABLENAME = "server_query_info";
}

[Flags]
public enum FHDR
{
    ZERO = 0x0000,
    LEAVEPVS = 0x0001,
    DELETE = 0x0002,
    ENTERPVS = 0x0004,
};