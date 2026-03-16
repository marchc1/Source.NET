using Source.Common.Engine;

using System.Reflection;
using System.Runtime.CompilerServices;

namespace Source.Common;

public static class ServerClassRetriever
{
	static readonly Dictionary<Type, ServerClass> ClassList = [];

	public static ServerClass GetOrError(Type t) {
		if (ClassList.TryGetValue(t, out ServerClass? c))
			return c;

		FieldInfo? field = t.GetField(nameof(ServerClass), BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
		if (field == null)
			throw new NullReferenceException(nameof(field));

		c = ClassList[t] = (ServerClass)field.GetValue(null)!;
		return c;
	}
}

public class ServerClass
{
	public static ServerClass? Head;

	public string NetworkName;
	public SendTable Table;
	public ServerClass? Next;
	public int ClassID;
	public int InstanceBaselineIndex = INetworkStringTable.INVALID_STRING_INDEX;
	public ServerClass(ReadOnlySpan<char> networkName, SendTable table, [CallerArgumentExpression(nameof(table))] string? nameOfTable = null) {
		NetworkName = new(networkName);
		Table = table;
		if (nameOfTable != null)
			table.NetTableName = nameOfTable;

		Next = Head;
		Head = this;

	}
}
