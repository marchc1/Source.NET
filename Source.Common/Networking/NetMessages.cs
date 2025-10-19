namespace Source.Common.Networking;

using Source;
using System.Diagnostics;
using System.Numerics;

using static Dbg;
using static Protocol;
using static Net;
using static Constants;
using Source.Common.Bitbuffers;
using Source.Common.Hashing;
using Source.Common.Mathematics;

public static class NetMessageExtensions
{
	public static void WriteNetMessageType<T>(this bf_write buffer, T msg) where T : INetMessage => buffer.WriteUBitLong((uint)msg.GetMessageType(), NETMSG_TYPE_BITS);
	public static SignOnState ReadSignOnState(this bf_read buffer) => (SignOnState)buffer.ReadByte();
	public static void WriteSignOnState(this bf_write buffer, SignOnState state) => buffer.WriteByte((byte)state);
	public static void WriteZeros(this bf_write buffer, int amount) {
		for (int i = 0; i < amount; i++) {
			buffer.WriteBool(false);
		}
	}
	public static void WriteOnes(this bf_write buffer, int amount) {
		for (int i = 0; i < amount; i++) {
			buffer.WriteBool(true);
		}
	}
}

public struct cvar_s
{
	public string Name;
	public string Value;
}

public enum ServerOS : byte
{
	Win32 = (byte)'W',
	Linux = (byte)'L'
}

public class NET_Tick : NetMessage
{
	public NET_Tick() : base(Net.Tick) { reliable = false; }
	public NET_Tick(int tick, float frametime, float framedev) : base(Net.Tick) {
		reliable = false;
		Tick = tick;
		HostFrameTime = frametime;
		HostFrameDeviation = framedev;
	}

	public int Tick;
	public float HostFrameTime;
	public float HostFrameDeviation;
	public const float NET_TICK_SCALEUP = 100000.0f;
	public override bool ReadFromBuffer(bf_read buffer) {
		NetChannel netchan = GetNetChannel() ?? throw new Exception("No net channel found!");
		Tick = buffer.ReadLong();
		HostFrameTime = buffer.ReadUBitLong(16) / NET_TICK_SCALEUP;
		HostFrameDeviation = buffer.ReadUBitLong(16) / NET_TICK_SCALEUP;

		return !buffer.Overflowed;
	}

	public override bool WriteToBuffer(bf_write buffer) {
		buffer.WriteNetMessageType(this);
		buffer.WriteLong(Tick);
		buffer.WriteUBitLong((uint)Math.Clamp((int)(NET_TICK_SCALEUP * HostFrameTime), 0, 65535), 16);
		buffer.WriteUBitLong((uint)Math.Clamp((int)(NET_TICK_SCALEUP * HostFrameDeviation), 0, 65535), 16);
		return !buffer.Overflowed;
	}
}
public class NET_SetConVar : NetMessage
{
	public List<cvar_s> ConVars;
	public override NetChannelGroup GetGroup() => NetChannelGroup.StringCmd;
	public NET_SetConVar() : base(SetConVar) {
		ConVars = [];
	}


	public void AddCVar(string name, string value) {
		name = name.Length >= 260 ? name.Substring(0, 260) : name;
		value = value.Length >= 260 ? value.Substring(0, 260) : value;

		ConVars.Add(new() {
			Name = name,
			Value = value
		});
	}

	public override bool ReadFromBuffer(bf_read buffer) {
		int numvars = buffer.ReadByte();
		DevMsg($"{numvars} vars received\n");

		ConVars.Clear();

		for (int i = 0; i < numvars; i++) {
			cvar_s var = new();
			buffer.ReadString(out var.Name, 260);
			buffer.ReadString(out var.Value, 260);
			ConVars.Add(var);
		}

		return !buffer.Overflowed;
	}
	public override bool WriteToBuffer(bf_write buffer) {
		buffer.WriteNetMessageType(this);

		nint numvars = ConVars.Count;
		Debug.Assert(numvars <= byte.MaxValue);

		buffer.WriteByte((int)numvars);
		foreach (var cvar in ConVars) {
			buffer.WriteString(cvar.Name, limit: 260);
			buffer.WriteString(cvar.Value, limit: 260);
		}

		return !buffer.Overflowed;
	}

	public override string ToString() {
		string[] vars = new string[ConVars.Count];
		for (int i = 0; i < ConVars.Count; i++) {
			vars[i] = $"    {ConVars[i].Name}: {ConVars[i].Value}";
		}
		return $"NET_SetConVar: {ConVars.Count}, \n" + string.Join("\n", vars);
	}
}
public class NET_StringCmd : NetMessage
{
	public NET_StringCmd() : base(StringCmd) { }
	public override NetChannelGroup GetGroup() => NetChannelGroup.StringCmd;

	public string Command;

	public override bool ReadFromBuffer(bf_read buffer) => buffer.ReadString(out Command, 1024);
	public override bool WriteToBuffer(bf_write buffer) {
		if (string.IsNullOrEmpty(Command))
			return false; // don't write anything

		buffer.WriteNetMessageType(this);
		return buffer.WriteString(Command ?? " NET_StringCmd NULL");
	}

	public override string ToString() {
		return $"NET_StringCmd: \"{Command}\"";
	}
}
public class NET_SignonState : NetMessage
{
	public SignOnState SignOnState { get; set; }
	public int SpawnCount { get; set; }

	public NET_SignonState() : base(Net.SignOnState) {

	}
	public override NetChannelGroup GetGroup() => NetChannelGroup.SignOn;
	public NET_SignonState(SignOnState signOnState, int spawnCount) : base(Net.SignOnState) {
		SignOnState = signOnState;
		SpawnCount = spawnCount;
	}

	public override bool WriteToBuffer(bf_write buffer) {
		buffer.WriteNetMessageType(this);
		buffer.WriteSignOnState(SignOnState);
		buffer.WriteLong(SpawnCount);

		return !buffer.Overflowed;
	}

	public override bool ReadFromBuffer(bf_read buffer) {
		SignOnState = buffer.ReadSignOnState();
		SpawnCount = buffer.ReadLong();

		return !buffer.Overflowed;
	}

	public override string ToString() => $"{GetName()}: state {SignOnState}, count {SpawnCount}";
}
public class svc_Print : NetMessage
{
	public string? Text;
	public svc_Print() : base(SVC.Print) { }

	public override bool WriteToBuffer(bf_write buffer) {
		buffer.WriteNetMessageType(this);
		return buffer.WriteString(Text ?? "svc_print NULL");
	}

	public override bool ReadFromBuffer(bf_read buffer) {
		return buffer.ReadString(out Text, 2048);
	}

	public override string ToString() {
		return Text ?? "NULL";
	}
}
public class svc_ServerInfo : NetMessage
{
	public override NetChannelGroup GetGroup() => NetChannelGroup.SignOn;
	public svc_ServerInfo() : base(SVC.ServerInfo) { }

	public int Protocol;
	public int ServerCount;
	public bool IsDedicated;
	public bool IsHLTV;
	public ServerOS ServerOS;
	public uint CRC32;
	public MD5Value MapMD5 = new();
	public int MaxClients;
	public int MaxClasses;
	public int PlayerSlot;
	public double TickInterval;
	public string GameDirectory;
	public string MapName;
	public string SkyName;
	public string HostName;
	public string LoadingURL;
	public string Gamemode;

	public override bool WriteToBuffer(bf_write buffer) {
		buffer.WriteNetMessageType(this);

		buffer.WriteShort(Protocol);
		buffer.WriteLong(ServerCount);
		buffer.WriteOneBit(IsHLTV ? 1 : 0);
		buffer.WriteOneBit(IsDedicated ? 1 : 0);
		buffer.WriteLong(unchecked((int)0xffffffff));  // Used to be client.dll CRC.  This was far before signed binaries, VAC, and cross-platform play
		buffer.WriteWord(MaxClasses);
		buffer.WriteBytes(MapMD5.Bits, MD5Value.DIGEST_LENGTH);       // To prevent cheating with hacked maps
		buffer.WriteByte(PlayerSlot);
		buffer.WriteByte(MaxClients);
		buffer.WriteFloat((float)TickInterval);
		buffer.WriteChar((byte)ServerOS);
		buffer.WriteString(GameDirectory);
		buffer.WriteString(MapName);
		buffer.WriteString(SkyName);
		buffer.WriteString(HostName);

		return !buffer.Overflowed;
	}

	public override bool ReadFromBuffer(bf_read buffer) {
		Protocol = buffer.ReadShort();
		ServerCount = buffer.ReadLong();
		IsHLTV = buffer.ReadOneBit() != 0;
		IsDedicated = buffer.ReadOneBit() != 0;
		buffer.ReadLong();  // Legacy client CRC.
		MaxClasses = buffer.ReadWord();

		// Prevent cheating with hacked maps
		buffer.ReadBytes(MapMD5.Bits);

		PlayerSlot = buffer.ReadByte();
		MaxClients = buffer.ReadByte();
		TickInterval = buffer.ReadFloat();
		ServerOS = (ServerOS)buffer.ReadChar();

		GameDirectory = buffer.ReadString(260) ?? "";
		MapName = buffer.ReadString(260) ?? "";
		SkyName = buffer.ReadString(260) ?? "";
		HostName = buffer.ReadString(260) ?? "";
		LoadingURL = buffer.ReadString(260) ?? "";
		Gamemode = buffer.ReadString(260) ?? "";

		return !buffer.Overflowed;
	}
}
public class svc_ClassInfo : NetMessage
{
	public struct Class
	{
		public int ClassID;
		public string DataTableName;
		public string ClassName;
	}
	public override NetChannelGroup GetGroup() => NetChannelGroup.SignOn;
	public bool CreateOnClient;
	public List<Class> Classes = [];
	public int NumServerClasses = 0;
	public svc_ClassInfo() : base(SVC.ClassInfo) { }

	public override bool ReadFromBuffer(bf_read buffer) {
		Classes.Clear();

		int numServerClasses = buffer.ReadShort();
		int serverClassBits = (int)MathF.Log2(numServerClasses) + 1;

		NumServerClasses = numServerClasses;
		CreateOnClient = buffer.ReadBool();

		if (CreateOnClient)
			return !buffer.Overflowed;

		for (int i = 0; i < NumServerClasses; i++) {
			Class serverclass = new Class();

			serverclass.ClassID = (int)buffer.ReadUBitLong(serverClassBits);
			serverclass.ClassName = buffer.ReadString(256);
			serverclass.DataTableName = buffer.ReadString(256);

			Classes.Add(serverclass);
		}

		return !buffer.Overflowed;
	}

	public override bool WriteToBuffer(bf_write buffer) {
		throw new Exception();
	}
}
public class svc_CreateStringTable : NetMessage
{
	public svc_CreateStringTable() : base(SVC.CreateStringTable) { }
	public override NetChannelGroup GetGroup() => NetChannelGroup.SignOn;


	public string TableName;
	public int MaxEntries;
	public int NumEntries;
	public bool UserDataFixedSize;
	public int UserDataSize;
	public int UserDataSizeBits;
	public bool IsFilenames;
	public int Length;
	public bf_read DataIn;
	public bf_write DataOut;
	public bool DataCompressed;

	public override bool WriteToBuffer(bf_write buffer) {
		buffer.WriteNetMessageType(this);
		Length = DataOut.BitsWritten;

		buffer.WriteString(TableName);
		buffer.WriteWord(MaxEntries);
		int encodeBits = (int)MathF.Log2(MaxEntries);
		buffer.WriteUBitLong((uint)NumEntries, encodeBits + 1);
		buffer.WriteVarInt32((uint)Length);

		buffer.WriteBool(UserDataFixedSize);

		if (UserDataFixedSize) {
			buffer.WriteUBitLong((uint)UserDataSize, 12);
			buffer.WriteUBitLong((uint)UserDataSizeBits, 4);
		}

		buffer.WriteBool(DataCompressed);
		return buffer.WriteBits(DataOut.BaseArray, Length);
	}

	public override bool ReadFromBuffer(bf_read buffer) {
		TableName = buffer.ReadString(500) ?? throw new Exception();
		MaxEntries = buffer.ReadWord();
		int encodeBits = (int)MathF.Log2(MaxEntries);
		NumEntries = (int)buffer.ReadUBitLong(encodeBits + 1);
		// Protocol difference here we should account for later
		Length = (int)buffer.ReadVarInt32();
		UserDataFixedSize = buffer.ReadBool();
		if (UserDataFixedSize) {
			UserDataSize = (int)buffer.ReadUBitLong(12);
			UserDataSizeBits = (int)buffer.ReadUBitLong(4);
		}
		else {
			UserDataSize = 0;
			UserDataSizeBits = 0;
		}

		DataCompressed = buffer.ReadBool();

		DataIn = buffer.Copy();
		Msg($"svc_CreateStringTable: {TableName}, contains {NumEntries}/{MaxEntries}\n");
		return buffer.SeekRelative(Length);
	}
}
public class svc_UpdateStringTable : NetMessage
{
	public svc_UpdateStringTable() : base(SVC.UpdateStringTable) { }
	public override NetChannelGroup GetGroup() => NetChannelGroup.StringTable;

	public int TableID;
	public int ChangedEntries;
	public int Length;
	public bf_read DataIn;
	public bf_write DataOut;

	public override bool WriteToBuffer(bf_write buffer) {
		return false;
	}

	public override bool ReadFromBuffer(bf_read buffer) {
		TableID = (int)buffer.ReadUBitLong(MAX_TABLES_BITS);

		if (buffer.ReadBool() != false)
			ChangedEntries = buffer.ReadWord();
		else
			ChangedEntries = 1;

		Length = (int)buffer.ReadUBitLong(20);

		DataIn = buffer.Copy();
		return buffer.SeekRelative(Length);
	}

	public override string ToString() {
		return $"table {TableID}, changed {ChangedEntries}, bytes {Bits2Bytes(Length)}";
	}
}
public class svc_VoiceInit : NetMessage
{
	public svc_VoiceInit() : base(SVC.VoiceInit) { }
	public override NetChannelGroup GetGroup() => NetChannelGroup.SignOn;

	public string VoiceCodec;
	public int SampleRate;

	public override bool ReadFromBuffer(bf_read buffer) {
		VoiceCodec = buffer.ReadString(260);

		byte legacyQuality = buffer.ReadByte();
		if (legacyQuality == 255) {
			SampleRate = buffer.ReadShort();
		}
		else {

		}

		return !buffer.Overflowed;
	}

	public override bool WriteToBuffer(bf_write buffer) {
		throw new Exception();
	}
}
public class svc_Sounds : NetMessage
{
	public svc_Sounds() : base(SVC.Sounds) { }
	public override NetChannelGroup GetGroup() => NetChannelGroup.Sounds;
	public bool ReliableSound;
	public int NumSounds;
	public int Length;
	public bf_read DataIn;

	public override bool ReadFromBuffer(bf_read buffer) {
		ReliableSound = buffer.ReadOneBit() != 0;

		if (ReliableSound) {
			NumSounds = 1;
			Length = (int)buffer.ReadUBitLong(8);
		}
		else {
			NumSounds = (int)buffer.ReadUBitLong(8);
			Length = (int)buffer.ReadUBitLong(16);
		}
		DataIn = buffer;
		return buffer.SeekRelative(Length);
	}

	public override bool WriteToBuffer(bf_write buffer) {
		throw new Exception();
	}
	public override string ToString() {
		return $"number {NumSounds},{(ReliableSound ? " reliable" : " ")} bytes {Bits2Bytes(Length)}";
	}
}

public class svc_Prefetch : NetMessage {
	public svc_Prefetch() : base(SVC.Sounds) { }
	public override NetChannelGroup GetGroup() => NetChannelGroup.Sounds;

	public ushort Type;
	public ushort SoundIndex;
}
public class svc_BSPDecal : NetMessage
{
	public svc_BSPDecal() : base(SVC.BSPDecal) { }

	public Vector3 Pos;
	public int DecalTextureIndex;
	public int EntityIndex;
	public int ModelIndex;
	public bool LowPriority;

	public override bool ReadFromBuffer(bf_read buffer) {
		Pos = buffer.ReadBitVec3Coord();
		DecalTextureIndex = (int)buffer.ReadUBitLong(MAX_DECAL_INDEX_BITS);

		if (buffer.ReadBool()) {
			EntityIndex = (int)buffer.ReadUBitLong(MAX_EDICT_BITS);
			ModelIndex = (int)buffer.ReadUBitLong(SP_MODEL_INDEX_BITS);
		}
		else {
			EntityIndex = 0;
			ModelIndex = 0;
		}
		LowPriority = buffer.ReadBool();

		return !buffer.Overflowed;
	}

	public override bool WriteToBuffer(bf_write buffer) {
		throw new Exception();
	}
}
public class svc_GameEventList : NetMessage
{
	public svc_GameEventList() : base(SVC.GameEventList) { }

	public int NumEvents;
	public int Length;
	public bf_read DataIn;
	public bf_write DataOut;

	public override bool ReadFromBuffer(bf_read buffer) {
		NumEvents = (int)buffer.ReadUBitLong(MAX_EVENT_BITS);
		Length = (int)buffer.ReadUBitLong(20);
		DataIn = buffer.Copy();

		// temp
		byte[] data = new byte[Length];
		buffer.ReadBits(data, Length);

		return !buffer.Overflowed;
	}

	public override bool WriteToBuffer(bf_write buffer) {
		throw new Exception();
	}
}
public class svc_SetView : NetMessage
{
	public svc_SetView() : base(SVC.SetView) { }

	public int EntityIndex;

	public override bool ReadFromBuffer(bf_read buffer) {
		EntityIndex = (int)buffer.ReadUBitLong(MAX_EDICT_BITS);
		return !buffer.Overflowed;
	}
}
public class svc_FixAngle : NetMessage
{
	public bool Relative;
	public QAngle Angle;
	public svc_FixAngle() : base(SVC.FixAngle) { }

	public override bool ReadFromBuffer(bf_read buffer) {
		Relative = buffer.ReadBool();
		Angle = new();
		Angle.X = buffer.ReadBitAngle(16);
		Angle.Y = buffer.ReadBitAngle(16);
		Angle.Z = buffer.ReadBitAngle(16);

		return !buffer.Overflowed;
	}

	public override bool WriteToBuffer(bf_write buffer) {
		buffer.WriteNetMessageType(this);
		buffer.WriteBool(Relative);
		buffer.WriteBitAngle(Angle.X, 16);
		buffer.WriteBitAngle(Angle.Y, 16);
		buffer.WriteBitAngle(Angle.Z, 16);
		return !buffer.Overflowed;
	}
}
public class svc_UserMessage : NetMessage
{
	public svc_UserMessage() : base(SVC.UserMessage) { }
	public override NetChannelGroup GetGroup() => NetChannelGroup.UserMessage;

	public int MessageType;
	public int Length;
	public bf_read DataIn;
	public bf_write DataOut;

	public override bool ReadFromBuffer(bf_read buffer) {
		MessageType = buffer.ReadByte();
		Length = (int)buffer.ReadUBitLong(NETMSG_LENGTH_BITS);
		DataIn = buffer.Copy();

		return buffer.SeekRelative(Length);
	}

	public override bool WriteToBuffer(bf_write buffer) {
		Length = DataOut.BitsWritten;

		Debug.Assert(Length < 1 << NETMSG_LENGTH_BITS);
		if (Length >= 1 << NETMSG_LENGTH_BITS)
			return false;

		buffer.WriteNetMessageType(this);
		buffer.WriteByte(MessageType);
		buffer.WriteUBitLong((uint)Length, NETMSG_LENGTH_BITS);

		return buffer.WriteBits(DataOut.BaseArray, Length);
	}

	public override string ToString() {
		return $"SVC_UserMessage: type {MessageType}, bytes {Bits2Bytes(Length)}";
	}
}

public class svc_PacketEntities : NetMessage
{
	public svc_PacketEntities() : base(SVC.PacketEntities) { }
	public override NetChannelGroup GetGroup() => NetChannelGroup.Entities;
	public int MaxEntries;
	public int UpdatedEntries;
	public bool IsDelta;
	public bool UpdateBaseline;
	public int Baseline;
	public int DeltaFrom;
	public int Length;
	public bf_read DataIn;
	public bf_write DataOut;

	public override bool ReadFromBuffer(bf_read buffer) {
		MaxEntries = (int)buffer.ReadUBitLong(MAX_EDICT_BITS);
		IsDelta = buffer.ReadBool();

		if (IsDelta)
			DeltaFrom = buffer.ReadLong();
		else
			DeltaFrom = -1;

		Baseline = (int)buffer.ReadUBitLong(1);
		UpdatedEntries = (int)buffer.ReadUBitLong(MAX_EDICT_BITS);
		Length = (int)buffer.ReadUBitLong(DELTASIZE_BITS);
		UpdateBaseline = buffer.ReadBool();
		DataIn = buffer.Copy();

		return buffer.SeekRelative(Length);
	}

	public override bool WriteToBuffer(bf_write buffer) {
		return base.WriteToBuffer(buffer);
	}

	public override string ToString() {
		return $"SVC_PacketEntities: delta {DeltaFrom}, max {MaxEntries}, changed {UpdatedEntries}, changed {(UpdateBaseline ? "BL update" : "")}, bytes {Bits2Bytes(Length)}";
	}
}

public class svc_GMod_ServerToClient : NetMessage
{
	public svc_GMod_ServerToClient() : base(SVC.GMod_ServerToClient) { }

	public override bool ReadFromBuffer(bf_read buffer) {
		int bits = (int)buffer.ReadUBitLong(20);
		int type = buffer.ReadByte();

		if (bits < 1)
			return true;

		if (bits < 0) {
			Warning("Received invalid svc_GMod_ServerToClient\n");
			return true;
		}

		switch (type) {
			case 0: {
					int id = buffer.ReadWord();
					byte[] data = new byte[bits];
					int toRead = bits - 8 - 16;
					if (toRead > 0) {
						buffer.ReadBits(data, toRead);
					}

				}
				break;
			case 3: {

				}
				break;

			case 4: {
					int id = buffer.ReadWord();
					byte[] data = new byte[bits];
					int toRead = bits - 8 - 16;
					if (toRead > 0) {
						buffer.ReadBits(data, toRead);
					}
				}
				break;
			default:
				break;
		}

		//Console.WriteLine($"svc_GMod_ServerToClient: Type {type}, bits {bits}");

		return true;
	}
}
public class CLC_Move : NetMessage
{
	public CLC_Move() : base(CLC.Move) { reliable = false; }
	public override NetChannelGroup GetGroup() => NetChannelGroup.Move;

	public int BackupCommands;
	public int NewCommands;
	public int Length;
	public bf_read DataIn;
	public bf_write DataOut = new();

	public override bool WriteToBuffer(bf_write buffer) {
		buffer.WriteNetMessageType(this);
		Length = DataOut.BitsWritten;

		buffer.WriteUBitLong((uint)NewCommands, NUM_NEW_COMMAND_BITS);
		buffer.WriteUBitLong((uint)BackupCommands, NUM_BACKUP_COMMAND_BITS);

		buffer.WriteWord(Length);

		return buffer.WriteBits(DataOut.BaseArray, Length);
	}
}
public class CLC_ListenEvents : NetMessage
{
	public CLC_ListenEvents() : base(CLC.ListenEvents) {
		EventArray.EnsureCount(MAX_EVENT_NUMBER);
	}
	public override NetChannelGroup GetGroup() => NetChannelGroup.SignOn;
	public List<uint> EventArray = new List<uint>();

	public override bool WriteToBuffer(bf_write buffer) {
		buffer.WriteNetMessageType(this);
		int count = MAX_EVENT_NUMBER / 32;
		for (int i = 0; i < count; i++) {
			buffer.WriteUBitLong(EventArray[i], 32);
		}

		return !buffer.Overflowed;
	}
}
public class CLC_ClientInfo : NetMessage
{
	public CLC_ClientInfo() : base(CLC.ClientInfo) { }

	public int ServerCount;
	public int SendTableCRC;
	public bool IsHLTV;
	public ulong FriendsID;
	public string FriendsName;
	public uint[] CustomFiles = new uint[MAX_CUSTOM_FILES];

	// 01110100
	public override bool WriteToBuffer(bf_write buffer) {
		buffer.WriteNetMessageType(this);

		buffer.WriteLong(ServerCount);
		//buffer.WriteLong(SendTableCRC);
		// We have to do this. I don't know why.
		// There is some issue with sending it as a regular number that makes the bitbuffer flip a bit somewhere. I don't know why.
		foreach (int bit in new int[] { 0, 1, 1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1, 0, 1, 0, 1, 1, 1, 0, 0, 1, 0, 1, 0, 1, 1, 1, 1 }) {
			buffer.WriteOneBit(bit);
		}

		buffer.WriteBool(IsHLTV);
		buffer.WriteLong((int)FriendsID);
		buffer.WriteString("");
		for (int i = 0; i < MAX_CUSTOM_FILES; i++) {
			if (CustomFiles[i] != 0) {
				buffer.WriteBool(true);
				buffer.WriteUBitLong(CustomFiles[i], 32);
			}
			else buffer.WriteBool(false);
		}
		//buffer.WriteBool(false);

		return !buffer.Overflowed;
	}

	public override string ToString() {
		return $"ServerCount: {ServerCount}, SendTableCRC: {SendTableCRC}";
	}
}
public class CLC_GMod_ClientToServer : NetMessage
{

	public CLC_GMod_ClientToServer() : base(CLC.GMod_ClientToServer) { }

	public int Length;
	public bf_write DataOut;
	public bf_read DataIn;

	public override bool ReadFromBuffer(bf_read buffer) {
		return base.ReadFromBuffer(buffer);
	}

	public override bool WriteToBuffer(bf_write buffer) {
		buffer.WriteNetMessageType(this);
		buffer.WriteUBitLong(24, 20);
		buffer.WriteByte(4);
		buffer.WriteUBitLong(0, 16);

		return base.WriteToBuffer(buffer);
	}
}

public class clc_BaselineAck : NetMessage
{
	public clc_BaselineAck(int tick, int baseline) : base(CLC.BaselineAck) {
		BaselineTick = tick;
		BaselineNumber = baseline;
	}
	public override NetChannelGroup GetGroup() => NetChannelGroup.Entities;

	public int BaselineTick;
	public int BaselineNumber;

	public override bool WriteToBuffer(bf_write buffer) {
		buffer.WriteNetMessageType(this);
		buffer.WriteLong(BaselineTick);
		buffer.WriteUBitLong((uint)BaselineNumber, 1);

		return !buffer.Overflowed;
	}
}
public class svc_TempEntities : NetMessage
{
	public svc_TempEntities() : base(SVC.TempEntities) { }
	public override NetChannelGroup GetGroup() => NetChannelGroup.Events;
	public int NumEntries;
	public int Length;
	public bf_read DataIn;
	public bf_write DataOut;

	public override bool ReadFromBuffer(bf_read buffer) {
		NumEntries = (int)buffer.ReadUBitLong(EventInfo.EVENT_INDEX_BITS);
		Length = (int)buffer.ReadVarInt32();

		DataIn = buffer.Copy();
		return buffer.SeekRelative(Length);
	}

	public override bool WriteToBuffer(bf_write buffer) {
		return base.WriteToBuffer(buffer);
	}

	public override string ToString() {
		return $"svc_TempEntities: number {NumEntries}, bytes {Bits2Bytes(Length)}";
	}
}

public class EventInfo
{
	public const int EVENT_INDEX_BITS = 8;
	public const int EVENT_DATA_LEN_BITS = 11;
	public const int MAX_EVENT_DATA = 192;
}
