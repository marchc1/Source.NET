using CommunityToolkit.HighPerformance;

using Source.Common.Hashing;

using System.Runtime.CompilerServices;
using System.Text;

namespace Game.Shared;

public struct BitFields(uint value = 0)
{
	private uint value = value;

	public bool Ack {
		get => (value & 0b1u) != 0;
		set {
			if (value)
				this.value |= 0b1u;
			else
				this.value &= ~0b1u;
		}
	}

	public uint Player {
		get => (value >> 1) & 0b1_1111u; // 5 bits
		set {
			this.value = (this.value & ~(0b1_1111u << 1)) | ((value & 0b1_1111u) << 1);
		}
	}

	public uint Command {
		get => (value >> 6) & 0b11_1111_1111u; // 10 bits
		set {
			this.value = (this.value & ~(0b11_1111_1111u << 6)) | ((value & 0b11_1111_1111u) << 6);
		}
	}

	public uint Hash {
		get => (value >> 16) & 0b1111_1111_1111u; // 12 bits
		set {
			this.value = (this.value & ~(0b1111_1111_1111u << 16)) | ((value & 0b1111_1111_1111u) << 16);
		}
	}

	public uint Instance {
		get => (value >> 28) & 0b1111u; // 4 bits
		set {
			this.value = (this.value & ~(0b1111u << 28)) | ((value & 0b1111u) << 28);
		}
	}

	public uint Raw {
		get => value;
		set => this.value = value;
	}

	public override string ToString() => $"Ack={Ack}, Player={Player}, Command={Command}, Hash={Hash}, Instance={Instance}, Raw=0x{value:X8}";
}

public class PredictableIdHelper
{
	const int MAX_ENTRIES = 256;

	readonly Entry[] entries = new Entry[256];
	public int CurrentCommand;
	public int Count;

	public struct Entry
	{
		public int Hash;
		public int Count;
	}

	public void Reset(int command) {
		CurrentCommand = command;
		Count = 0;
		memreset(entries.AsSpan());
	}

	public int AddEntry(int command, int hash) {
		if (command != CurrentCommand)
			Reset(command);

		ref Entry e = ref FindOrAddEntry(hash);
		if (Unsafe.IsNullRef(ref e))
			return 0;
		e.Count++;
		return e.Count - 1;
	}

	public ref Entry FindOrAddEntry(int hash) {
		Span<Entry> entries = this.entries.AsSpan();

		ref Entry e = ref Unsafe.NullRef<Entry>();

		for (int i = 0; i < Count; i++) {
			e = ref entries[i];
			if (e.Hash == hash)
				return ref e;
		}

		if (Count >= MAX_ENTRIES)
			return ref Unsafe.NullRef<Entry>();

		e = ref entries[Count++];
		e.Hash = hash;
		e.Count = 0;
		return ref e;
	}
}

public struct PredictableId
{
	static readonly PredictableIdHelper Helper = new();

	BitFields PredictableID;

	public static void ResetInstanceCounters() {
		Helper.Reset(-1);
	}
	static int ClassFileLineHash(ReadOnlySpan<char> classname, ReadOnlySpan<char> file, int line) {
		CRC32_t retval  = default;
		CRC32.Init(ref retval);

		Span<byte> classnameASCIIBuffer = stackalloc byte[Encoding.ASCII.GetByteCount(classname)];
		Span<byte> filenameASCIIBuffer = stackalloc byte[Encoding.ASCII.GetByteCount(file)];

		Encoding.ASCII.GetBytes(classname, classnameASCIIBuffer);
		Encoding.ASCII.GetBytes(file, filenameASCIIBuffer);

		Span<byte> tempbuffer = stackalloc byte[512];
		classnameASCIIBuffer.CopyTo(tempbuffer);
		CRC32.ProcessBuffer(ref retval, tempbuffer, classname.Length);

		filenameASCIIBuffer.CopyTo(tempbuffer);
		CRC32.ProcessBuffer(ref retval, tempbuffer, file.Length);

		new ReadOnlySpan<int>(in line).Cast<int, byte>().CopyTo(tempbuffer);
		CRC32.ProcessBuffer(ref retval, tempbuffer, sizeof(int));

		CRC32.Final(ref retval);

		return (int)retval;
	}

	static readonly char[] desc = new char[128];
	public ReadOnlySpan<char> Describe(){
		return sprintf(desc, "pl(%i) cmd(%i) hash(%i) inst(%i) ack(%s)").I(GetPlayer()).I(GetCommandNumber()).I(GetHash()).I(GetInstanceNumber()).S(GetAcknowledged() ? "true" : "false").ToSpan();
	}

	public bool IsActive() => PredictableID.Raw != 0;
	public void Init(int player, int command, ReadOnlySpan<char> classname = "", ReadOnlySpan<char> filename = "", int line = 0) {
		SetPlayer(player);
		SetCommandNumber(command);
		PredictableID.Hash = (uint)ClassFileLineHash(classname, filename, line);
		int instance = Helper.AddEntry(command, (int)PredictableID.Hash);
		SetInstanceNumber(instance);
	}
	public int GetPlayer() => (int)PredictableID.Player;
	public int GetHash() => (int)PredictableID.Hash;
	public int GetInstanceNumber() => (int)PredictableID.Instance;
	public int GetCommandNumber() => (int)PredictableID.Command;
	public void SetAcknowledged(bool ack) => PredictableID.Ack = ack;
	public bool GetAcknowledged() => PredictableID.Ack;
	public int GetRaw() => (int)PredictableID.Raw;
	public void SetRaw(int v) => PredictableID.Raw = (uint)v;
	private void SetCommandNumber(int commandNumber) => PredictableID.Command = (uint)commandNumber;
	private void SetPlayer(int playerIndex) => PredictableID.Player = (uint)playerIndex;
	private void SetInstanceNumber(int counter) => PredictableID.Instance = (uint)counter;


	public static bool operator ==(PredictableId self, PredictableId other) => self.GetRaw() == other.GetRaw();
	public static bool operator !=(PredictableId self, PredictableId other) => self.GetRaw() != other.GetRaw();
}
