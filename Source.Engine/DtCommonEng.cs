
using Source.Common;
using Source.Common.Client;
using Source.Common.Commands;
using Source.Common.Server;
using Source.Engine.Client;

namespace Source.Engine;

public class DtCommonEng(Host Host, Sys Sys, IServerGameDLL serverGameDLL, IBaseClientDLL clientDLL, ICommandLine CommandLine)
{
	public readonly LinkedList<RecvTable> RecvTables = [];
	public readonly LinkedList<RecvDecoder> RecvDecoders = [];
	public readonly LinkedList<ClientSendTable> ClientSendTables = [];
	public int PropsDecoded = 0;

	internal void CreateClientTablesFromServerTables() {
		if (serverGameDLL == null)
			Sys.Error("DataTable_CreateClientTablesFromServerTables:  No serverGameDLL loaded!");

		ServerClass? classes = serverGameDLL!.GetAllServerClasses();
		ServerClass? cur;

		HashSet<SendTable> visited = [];

		for (cur = classes; cur != null; cur = cur.Next)
			MaybeCreateReceiveTable(visited, cur.Table, true);

		for (cur = classes; cur != null; cur = cur.Next)
			MaybeCreateReceiveTable_R(visited, cur.Table);
	}

	private void MaybeCreateReceiveTable_R(HashSet<SendTable> visited, SendTable table) {
		MaybeCreateReceiveTable(visited, table, false);

		for (int i = 0; i < (table.Props?.Length ?? 0); i++) {
			SendProp prop = table.Props![i];
			if (prop.Type == SendPropType.DataTable)
				MaybeCreateReceiveTable_R(visited, prop.GetDataTable()!);
		}
	}

	private void MaybeCreateReceiveTable(HashSet<SendTable> visited, SendTable table, bool needDecoder) {
		if (visited.Contains(table))
			return;

		visited.Add(table);

		SetupReceiveTableFromSendTable(table, needDecoder);
	}

	public RecvTable? FindRecvTable(string? name) {
		return RecvTables.FirstOrDefault(x => x.GetName().Equals(name, StringComparison.OrdinalIgnoreCase));
	}

	private bool SetupReceiveTableFromSendTable(SendTable sendTable, bool needsDecoder) {
		ClientSendTable clientSendTable = new ClientSendTable();
		SendTable table = clientSendTable.SendTable;
		ClientSendTables.AddLast(clientSendTable);

		table.NetTableName = sendTable.NetTableName;

		if (needsDecoder) {
			RecvDecoder decoder = new RecvDecoder();
			RecvDecoders.AddLast(decoder);

			RecvTable? recvTable = FindRecvTable(table.NetTableName);
			if (recvTable == null) {
				recvTable = FindRenamedTable(table.NetTableName);
				if (recvTable == null) {
					Warning($"No matching RecvTable for SendTable '{table.NetTableName}'.\n");
					return false;
				}
			}

			recvTable.Decoder = decoder;
			decoder.Table = recvTable;

			decoder.ClientSendTable = clientSendTable;
			decoder.Precalc.SendTable = clientSendTable.GetSendTable()!;
			clientSendTable.GetSendTable()!.Precalc = decoder.Precalc;

			recvTable.SetupArrayProps_R();
		}

		table.Props = (sendTable.Props != null) ? new SendProp[sendTable.Props.Length] : null;
		clientSendTable.Props.EnsureCount(table.Props?.Length ?? 0);

		for (int iProp = 0; iProp < (table.Props?.Length ?? 0); iProp++) {
			// Fill the same types
			table.Props![iProp] = new SendProp();
		}

		for (int iProp = 0; iProp < (table.Props?.Length ?? 0); iProp++) {
			ClientSendProp clientProp = clientSendTable.Props[iProp];
			SendProp prop = table!.Props![iProp];
			SendProp sendTableProp = sendTable!.Props![iProp];

			prop.Type = sendTableProp.Type;
			prop.FieldInfo = sendTableProp.FieldInfo;
			prop.NameOverride = sendTableProp.NameOverride;
			prop.SetFlags(sendTableProp.GetFlags());

			if (CommandLine.FindParm("-dti") != 0 && !sendTableProp.GetParentArrayPropName().IsEmpty) {
				prop.ParentArrayPropName = new(sendTableProp.GetParentArrayPropName());
			}

			if (prop.Type == SendPropType.DataTable) {
				ReadOnlySpan<char> dtName = sendTableProp.ExcludeDTName;

				if (sendTableProp.GetDataTable() != null)
					dtName = sendTableProp.GetDataTable()!.NetTableName;

				Assert(!dtName.IsEmpty);

				clientProp.SetTableName(dtName);

				prop.SetDataTableProxyFn(sendTableProp.GetDataTableProxyFn());
				prop.SetOffset(sendTableProp.GetOffset());
			}
			else {
				if (prop.IsExcludeProp()) {
					prop.ExcludeDTName = new(sendTableProp.GetExcludeDTName());
				}
				else if (prop.GetPropType() == SendPropType.Array) {
					prop.SetNumElements(sendTableProp.GetNumElements());
				}
				else {
					prop.LowValue = sendTableProp.LowValue;
					prop.HighValue = sendTableProp.HighValue;
					prop.Bits = sendTableProp.Bits;
				}
			}
		}

		return true;
	}

	private RecvTable? FindRenamedTable(string? oldTableName) {
		RenamedRecvTableInfo cur = clientDLL.GetRenamedRecvTableInfos();

		while (cur != null && (cur.OldName != null && cur.NewName != null)) {
			if (cur.OldName.Equals(oldTableName, StringComparison.OrdinalIgnoreCase))
				return FindRecvTable(cur.NewName);

			cur = cur.Next;
		}

		return null;
	}

	internal void CreateClientClassInfosFromServerClasses(BaseClientState state) {
		ServerClass? classes = serverGameDLL.GetAllServerClasses();

		state.NumServerClasses = Constants.TEMP_TOTAL_SERVER_CLASSES;
		state.ServerClasses = new C_ServerClassInfo[state.NumServerClasses];

		for (ServerClass? svclass = classes; svclass != null; svclass = svclass.Next) {
			state.ServerClasses[svclass.ClassID] = new();
			state.ServerClasses[svclass.ClassID].ClassName = svclass.NetworkName;
			state.ServerClasses[svclass.ClassID].DatatableName = new(svclass.Table.GetName());
		}
	}
}
