namespace Source.Common.Engine;

public delegate void StringChangedDelegate(
	object? context,
	INetworkStringTable stringTable,
	int stringNumber,
	string newString,
	ReadOnlySpan<byte> newData
);

public interface INetworkStringTable
{
	public const int INVALID_STRING_TABLE = -1;
	public const ushort INVALID_STRING_INDEX = ushort.MaxValue;
	public const uint MAX_TABLES = 32;

	public const string INSTANCE_BASELINE_TABLENAME = "instancebaseline";
	public const string LIGHT_STYLES_TABLENAME = "lightstyles";
	public const string USER_INFO_TABLENAME = "userinfo";
	public const string SERVER_STARTUP_DATA_TABLENAME = "server_query_info";

	public string GetTableName();
	public int GetTableId();
	public int GetNumStrings();
	public int GetMaxStrings();
	public int GetEntryBits();
	public void SetTick(int tick);
	public bool ChangedSinceTick(int tick);
	public int AddString(bool isServer, string value, int length = -1, byte[]? userData = null);
	public string? GetString(int stringNumber);
	public void SetStringUserData(int stringNumber, int length, byte[] userData);
	public byte[]? GetStringUserData(int stringNumber, out int length);
	public int FindStringIndex(string value);
	public void SetStringChangedCallback(object? context, StringChangedDelegate callback);
}

public interface INetworkStringTableContainer
{
#pragma warning disable CS8618
	public static INetworkStringTableContainer networkStringTableContainerClient;
	public static INetworkStringTableContainer networkStringTableContainerServer;
#pragma warning restore CS8618

	public INetworkStringTable? CreateStringTable(
		string tableName,
		int maxEntries,
		int userDataFixedSize = 0,
		int userDataNetworkBits = 0
	);
	public INetworkStringTable? CreateStringTableEx(
		string tableName,
		int maxEntries,
		int userDataFixedSize = 0,
		int userDataNetworkBits = 0,
		bool isFilenames = false
	);
	public void RemoveAllTables();
	public INetworkStringTable? FindTable(string tableName);
	public INetworkStringTable? GetTable(int tableId);
	public int GetNumTables();
	public void SetAllowClientSideAddString(INetworkStringTable table, bool allowClientSideAddString);
}