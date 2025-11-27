namespace Source.Engine;

using Source.Common.Bitbuffers;
using Source.Common.Engine;

using System;
using System.Diagnostics;

using static Source.Dbg;


public class NetworkStringTableItem
{
	public const int MAX_USERDATA_BITS = 19; // RaphaelIT7: Unlike all other source games, gmod has this at 19 bits! Rubat probably did this for the singleplayer client lua files workaround
	public const int MAX_USERDATA_SIZE = 1 << MAX_USERDATA_BITS;

	public class ItemChange
	{
		public int Tick { get; set; }
		public int Length { get; set; }
		public byte[] Data { get; set; } = Array.Empty<byte>();
	}

	public byte[]? UserData;
	public int UserDataLength;
	public int TickChanged;
	public int TickCreated;
	public List<ItemChange>? ChangeList;

	public NetworkStringTableItem() {
		UserData = null;
		UserDataLength = 0;
		TickChanged = -1;
		TickCreated = -1;
	}

	public void EnableChangeHistory() {
		ChangeList ??= new List<ItemChange>();
	}

	public void UpdateChangeList(int tick, int length, byte[] userData) {
		if (ChangeList == null)
			return;

		ChangeList.Add(new ItemChange {
			Tick = tick,
			Length = length,
			Data = userData[..length]
		});
	}

	public int RestoreTick(int tick) {
		if (ChangeList == null)
			return -1;

		foreach (var change in ChangeList) {
			if (change.Tick == tick) {
				UserData = change.Data[..change.Length];
				UserDataLength = change.Length;
				TickChanged = tick;
				return tick;
			}
		}

		return -1;
	}

	public bool SetUserData(int tick, int length, ReadOnlySpan<byte> userData) {
		if (length > MAX_USERDATA_SIZE)
			throw new ArgumentOutOfRangeException(nameof(length), "Length exceeds MAX_USERDATA_SIZE");

		if (userData.IsEmpty || length == 0) {
			UserData = null;
			UserDataLength = 0;
		}
		else {
			UserData = new byte[length];
			userData.ClampedCopyTo(UserData);
			UserDataLength = length;
		}

		TickChanged = tick;
		return true;
	}

	public byte[]? GetUserData(out int length) {
		length = UserDataLength;
		return UserData;
	}
}

public interface INetworkStringDict
{
	public int Count();
	public void Purge();
	public string String(int index);
	public bool IsValidIndex(int index);
	public int Insert(string pString);
	public int Find(string pString);
	public NetworkStringTableItem Element(int index);
};

public class NetworkStringDict : INetworkStringDict
{
	private readonly Dictionary<string, NetworkStringTableItem> Lookup = new(StringComparer.OrdinalIgnoreCase);
	private readonly List<string> Keys = new();

	public int Count() => Lookup.Count;

	public void Purge() {
		Lookup.Clear();
		Keys.Clear();
	}

	public string String(int index) {
		if (!IsValidIndex(index))
			throw new IndexOutOfRangeException();

		return Keys[index];
	}

	public bool IsValidIndex(int index) => index >= 0 && index < Keys.Count;

	public int Insert(string value) {
		if (!Lookup.ContainsKey(value)) {
			Lookup[value] = new NetworkStringTableItem();
			Keys.Add(value);
		}

		return Keys.IndexOf(value);
	}

	public int Find(string value) {
		if (value == null)
			return -1;

		return Keys.IndexOf(value);
	}

	public NetworkStringTableItem Element(int index) {
		if (!IsValidIndex(index))
			throw new IndexOutOfRangeException();

		var key = Keys[index];
		return Lookup[key];
	}
}

public class NetworkStringTable : INetworkStringTable
{
	private int TableID;
	private string TableName;
	private int MaxEntries;
	private int EntryBits;
	private int TickCount;
	private int LastChangedTick;

	private bool ChangeHistoryEnabled;
	private bool Locked;
	private bool AllowClientSideAddString;
	private bool UserDataFixedSize;
	private bool IsFilenames;

	private int UserDataSize;
	private int UserDataSizeBits;

	private object? CallbackObject = null;
	private StringChangedDelegate? ChangeFunc = null;
	private INetworkStringTable? MirrorTable = null;

	private INetworkStringDict Items;
	private INetworkStringDict? ItemsClientSide;

	private const int SUBSTRING_BITS = 5;
	private const int MAX_ENTRY_LENGTH = 1024;

	readonly Host Host = Singleton<Host>();

	public NetworkStringTable(int tableID, ReadOnlySpan<char> tableName, int maxEntries, int userdatafixedsize, int userdatanetworkbits, bool bIsFilenames) {
		AllowClientSideAddString = false;
		TableID = tableID;
		TableName = new(tableName);
		MaxEntries = maxEntries;
		EntryBits = (int)Math.Log2(maxEntries);
		UserDataFixedSize = userdatafixedsize != 0;
		UserDataSize = userdatafixedsize;
		UserDataSizeBits = userdatanetworkbits;

		if (UserDataSizeBits > NetworkStringTableItem.MAX_USERDATA_BITS)
			Host.Error($"String tables user data bits restricted to {NetworkStringTableItem.MAX_USERDATA_SIZE} bits, requested {UserDataSizeBits} is too large\n");

		if (UserDataSize > NetworkStringTableItem.MAX_USERDATA_SIZE)
			Host.Error("String tables user data size restricted to {NetworkStringTableItem.MAX_USERDATA_SIZE} bytes, requested {UserDataSizeBits} is too large\n");

		if ((1 << EntryBits) != maxEntries)
			Host.Error($"String tables must be powers of two in size!, {maxEntries} is not a power of 2\n");

		if (bIsFilenames) {
			IsFilenames = true;
			Items = new NetworkStringDict(); //new CNetworkStringFilenameDict;
		}
		else {
			IsFilenames = false;
			Items = new NetworkStringDict();
		}
	}

	public int GetTableId() {
		return TableID;
	}

	public ReadOnlySpan<char> GetTableName() {
		return TableName;
	}

	public int GetNumStrings() {
		return Items.Count();
	}

	public int GetMaxStrings() {
		return MaxEntries;
	}

	public int GetEntryBits() {
		return EntryBits;
	}

	public void SetTick(int iTick) {
		TickCount = iTick;
	}

	public bool ChangedSinceTick(int iTick) {
		return LastChangedTick > iTick;
	}

	public int AddString(bool isServer, ReadOnlySpan<char> value, int length, ReadOnlySpan<byte> userData = default) {
		if (Locked) 
			DevMsg($"Warning! CNetworkStringTable::AddString: adding '{value}' while locked.\n");

		string tempStrValueOhMyGodThisNeedsToUseROS = new(value);
		int i = Items.Find(tempStrValueOhMyGodThisNeedsToUseROS);
		if (!isServer && Items.IsValidIndex(i) && ItemsClientSide == null) {
			isServer = true;
		}

		bool bHasChanged = false;
		NetworkStringTableItem? item = null;
		if (!isServer && ItemsClientSide != null) {
			i = ItemsClientSide.Find(tempStrValueOhMyGodThisNeedsToUseROS);
			if (!ItemsClientSide.IsValidIndex(i)) {
				if (ItemsClientSide.Count() >= (uint)GetMaxStrings()) {
					ConMsg($"Warning:  Table {GetTableName()} is full, can't add {value}\n");
					return INetworkStringTable.INVALID_STRING_INDEX;
				}

				i = ItemsClientSide.Insert(tempStrValueOhMyGodThisNeedsToUseROS);
				item = ItemsClientSide.Element(i);
				item.TickChanged = TickCount;
				item.TickCreated = TickCount;

				if (ChangeHistoryEnabled) {
					item.EnableChangeHistory();
				}

				bHasChanged = true;
			}
			else {
				item = ItemsClientSide.Element(i);
				bHasChanged = false;
			}

			if (length > -1 && item.SetUserData(TickCount, length, userData)) {
				bHasChanged = true;
			}

			if (bHasChanged && !ChangeHistoryEnabled) {
				DataChanged(-i, item);
			}

			i = -i;
		}
		else {
			i = Items.Find(tempStrValueOhMyGodThisNeedsToUseROS);

			if (!Items.IsValidIndex(i)) {
				if (Items.Count() >= (uint)GetMaxStrings()) {
					ConMsg($"Warning:  Table {GetTableName()} is full, can't add {value}\n");
					return INetworkStringTable.INVALID_STRING_INDEX;
				}

				i = Items.Insert(tempStrValueOhMyGodThisNeedsToUseROS);
				item = Items.Element(i);
				item.TickChanged = TickCount;
				item.TickCreated = TickCount;

				if (ChangeHistoryEnabled) {
					item.EnableChangeHistory();
				}

				bHasChanged = true;
			}
			else {
				item = Items.Element(i);
				bHasChanged = false;
			}

			if (length > -1 && item.SetUserData(TickCount, length, userData)) {
				bHasChanged = true;
			}

			if (bHasChanged && !ChangeHistoryEnabled) {
				DataChanged(i, item);
			}
		}

		return i;
	}

	public ReadOnlySpan<char> GetString(int stringNumber) {
		INetworkStringDict dict = Items;
		if (ItemsClientSide != null && stringNumber < -1) {
			dict = ItemsClientSide;
			stringNumber = -stringNumber;
		}

		if (dict.IsValidIndex(stringNumber)) {
			return dict.String(stringNumber);
		}

		return null;
	}

	public void SetStringUserData(int stringNumber, int length, ReadOnlySpan<byte> userData) {
		if (Locked) 
			DevMsg($"Warning! CNetworkStringTable::SetStringUserData ({GetTableName()}): changing entry {stringNumber} while locked.\n");

		INetworkStringDict dict = Items;
		int saveStringNumber = stringNumber;
		if (ItemsClientSide != null && stringNumber < -1) {
			dict = ItemsClientSide;
			stringNumber = -stringNumber;
		}

		NetworkStringTableItem p = dict.Element(stringNumber);
		if (p.SetUserData(TickCount, length, userData)) {
			DataChanged(saveStringNumber, p);
		}
	}

	public byte[]? GetStringUserData(int stringNumber) {
		INetworkStringDict dict = Items;
		if (ItemsClientSide != null && stringNumber < -1) {
			dict = ItemsClientSide;
			stringNumber = -stringNumber;
		}

		NetworkStringTableItem p = dict.Element(stringNumber);
		return p.GetUserData(out int length)[..length];
	}

	public int FindStringIndex(ReadOnlySpan<char> value) {
		int i = Items.Find(new(value));
		if (Items.IsValidIndex(i))
			return i;

		return INetworkStringTable.INVALID_STRING_INDEX;
	}

	public void SetStringChangedCallback(object? context, StringChangedDelegate callback) {
		ChangeFunc = callback;
		CallbackObject = context;
	}

	public void EnableRollback() {
		// stringtable must be empty 
		if (Items.Count() == 0)
			ChangeHistoryEnabled = true;
	}

	private void DataChanged(int stringNumber, NetworkStringTableItem item) {
		LastChangedTick = TickCount;

		if (ChangeFunc != null) {
			int userDataSize;
			Span<byte> pUserData = item.GetUserData(out userDataSize);
			// Ignore it's yapping, when this is called GetString should always return a valid thing.
			ChangeFunc(CallbackObject, this, stringNumber, GetString(stringNumber), pUserData);
		}
	}

	public void SetAllowClientSideAddString(bool allowClientSideAddString) {
		if (allowClientSideAddString == AllowClientSideAddString)
			return;

		AllowClientSideAddString = allowClientSideAddString;
		if (ItemsClientSide != null) {
			ItemsClientSide = null;
		}

		if (AllowClientSideAddString) {
			ItemsClientSide = new NetworkStringDict();
			ItemsClientSide.Insert("___clientsideitemsplaceholder0___");
			ItemsClientSide.Insert("___clientsideitemsplaceholder1___");
		}
	}

	public void Dump() {
		ConMsg($"Table {GetTableName()}\n");
		ConMsg($"  {GetNumStrings()}/{GetMaxStrings()} items\n");
		for (int i = 0; i < GetNumStrings(); i++)
			ConMsg($"  {i} : {GetString(i)}\n");

		if (ItemsClientSide != null)
			for (int i = 0; i < ItemsClientSide.Count(); i++)
				ConMsg($"  (c){i} : {ItemsClientSide.String(i)}\n");

		ConMsg("\n");
	}

	bool IsUserDataFixedSize() {
		return UserDataFixedSize;
	}

	int GetUserDataSize() {
		return UserDataSize;
	}

	int GetUserDataSizeBits() {
		return UserDataSizeBits;
	}

	public void ParseUpdate(bf_read buf, int entries) {
		int lastEntry = -1;
		List<string> history = new();
		for (int i = 0; i < entries; i++) {
			int entryIndex = lastEntry + 1;
			if (buf.ReadOneBit() == 0) {
				entryIndex = (int)buf.ReadUBitLong(GetEntryBits());
			}

			lastEntry = entryIndex;
			if (entryIndex < 0 || entryIndex >= GetMaxStrings()) {
				Host.Error($"Server sent bogus string index {entryIndex} for table {GetTableName()}\n");
				continue;
			}

			string? pEntry = null;
			string? substr = null;
			if (buf.ReadOneBit() != 0) {
				bool isSubstring = buf.ReadOneBit() != 0;
				if (isSubstring) {
					uint index = buf.ReadUBitLong(5);
					uint bytesToCopy = buf.ReadUBitLong(SUBSTRING_BITS);
					if (index >= history.Count) {
						Host.Error($"Server sent bogus substring index {entryIndex} for table {GetTableName()}\n");
						continue;
					}

					string baseStr = history[(int)index];
					string prefix = baseStr.Substring(0, Math.Min((int)bytesToCopy, baseStr.Length));
					buf.ReadString(out substr, MAX_ENTRY_LENGTH);
					pEntry = prefix + substr;
				}
				else {
					buf.ReadString(out pEntry, MAX_ENTRY_LENGTH);
				}
			}

			byte[]? pUserData = null;
			int nBytes = 0;
			if (buf.ReadOneBit() != 0) {
				if (IsUserDataFixedSize()) {
					nBytes = GetUserDataSize();
					Debug.Assert(nBytes > 0);
					pUserData = new byte[GetUserDataSizeBits()];
					buf.ReadBits(pUserData, GetUserDataSizeBits());
				}
				else {
					nBytes = (int)buf.ReadUBitLong(NetworkStringTableItem.MAX_USERDATA_BITS);
					if (nBytes > NetworkStringTableItem.MAX_USERDATA_SIZE) {
						Host.Error($"NetworkStringTableClient.ParseUpdate: message too large ({nBytes} bytes).");
						continue;
					}

					pUserData = new byte[nBytes];
					buf.ReadBytes(pUserData);
				}
			}

			if (entryIndex < GetNumStrings()) {
				if (pUserData != null) {
					SetStringUserData(entryIndex, nBytes, pUserData);
				}

				if (pEntry != null) {
					Assert(pEntry == GetString(entryIndex));
				}

				pEntry = new(GetString(entryIndex));
			}
			else {
				if (pEntry == null) {
					Msg($"NetworkStringTable.ParseUpdate: NULL pEntry, table {GetTableName()}, index {entryIndex}\n");
					pEntry = "";
				}

				AddString(true, pEntry, nBytes, pUserData);
			}

			if (history.Count > 31) 
				history.RemoveAt(0);

			history.Add(pEntry ?? "");
		}
	}
}

public class NetworkStringTableContainer : INetworkStringTableContainer
{
	private bool AllowCreation;
	private int TickCount;
	private bool Locked;
	private bool EnableRollback;
	private List<NetworkStringTable> Tables = new List<NetworkStringTable>();

	public INetworkStringTable? CreateStringTable(ReadOnlySpan<char> tableName, int maxEntries, int userDataFixedSize, int userDataNetworkBits) {
		return CreateStringTableEx(tableName, maxEntries, userDataFixedSize, userDataNetworkBits, false);
	}

	readonly Host Host = Singleton<Host>();

	public INetworkStringTable? CreateStringTableEx(ReadOnlySpan<char> tableName, int maxEntries, int userDataFixedSize, int userDataNetworkBits, bool isFilenames) {
		if (!AllowCreation) {
			Host.Error($"Tried to create string table '{tableName}' at wrong time\n");
			return null;
		}

		NetworkStringTable? pTable = (NetworkStringTable?)FindTable(tableName);
		if (pTable != null) {
			Host.Error($"Tried to create string table '{tableName}' twice\n");
			return null;
		}

		if (Tables.Count() >= INetworkStringTable.MAX_TABLES) {
			Host.Error($"Only {INetworkStringTable.MAX_TABLES} string tables allowed, can't create '{tableName}'");
			return null;
		}

		int id = Tables.Count();
		pTable = new NetworkStringTable(id, tableName, maxEntries, userDataFixedSize, userDataNetworkBits, isFilenames);

		if (EnableRollback) {
			pTable.EnableRollback();
		}

		pTable.SetTick(TickCount);

		Tables.Add(pTable);

		return pTable;
	}

	public void RemoveAllTables() {
		Tables.Clear();
	}

	public INetworkStringTable? FindTable(ReadOnlySpan<char> tableName) {
		foreach (NetworkStringTable pTable in Tables) {
			if (pTable.GetTableName().Equals(tableName, StringComparison.OrdinalIgnoreCase))
				return pTable;
		}

		return null;
	}

	public INetworkStringTable? GetTable(int tableId) {
		return Tables[tableId];
	}

	public int GetNumTables() {
		return Tables.Count();
	}

	public void SetTick(long tick) { }

	public void SetAllowClientSideAddString(INetworkStringTable table, bool allowClientSideAddString) {
		foreach (NetworkStringTable pTable in Tables) {
			if (pTable == table) {
				pTable.SetAllowClientSideAddString(allowClientSideAddString);
				return;
			}
		}
	}

	public void Dump() {
		foreach (NetworkStringTable pTable in Tables) {
			pTable.Dump();
		}
	}

	public void SetAllowCreation(bool state) {
		AllowCreation = state;
	}
}
