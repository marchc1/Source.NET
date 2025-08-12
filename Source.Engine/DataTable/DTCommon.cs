using Source.Common.Client;
using Source.Common.Networking.DataTable;
using Source.Common.Server;
using Source.Engine.Client;
using System.ComponentModel;
using static Source.Dbg;

namespace Source.Engine.DataTable;

public static class DTCommon
{
	private static RecvTable? FindRenamedTable(string pOldTableName)
	{
		return null; // RaphaelIT7: This is used by a SINGLE item in TF2, sooo we won't need it.
	}

	private static void SetupArrayProps_R(RecvTable pTable)
	{
		// If this table has already been initialized in here, then jump out.
		if (pTable.IsInitialized())
			return;

		pTable.SetInitialized(true);

		for (int i=0; i < pTable.GetNumProps(); i++)
		{
			RecvProp pProp = pTable.GetProp(i);
			if (pProp.GetType() == SendPropType.Array)
			{
				ErrorIfNot( i >= 1,
					"SetupArrayProps_R: array prop '{0}' is at index zero.", pProp.GetName()
				);

				// Get the property defining the elements in the array.
				RecvProp pArrayProp = pTable.GetProp(i-1);
				pArrayProp.SetInsideArray();
				pProp.SetArrayProp(pArrayProp);
			} else if (pProp.GetType() == SendPropType.DataTable) {
				// Recurse into children datatables.
				SetupArrayProps_R(pProp.GetDataTable());
			}
		}
	}

	private static bool SetupReceiveTableFromSendTable(SendTable sendTable, bool NeedsDecoder)
	{
		CClientSendTable pClientSendTable = new CClientSendTable();
		SendTable pTable = pClientSendTable.SendTable;
		DTRecv.g_ClientSendTables.AddLast( pClientSendTable );

		// Read the name.
		pTable.NetTableName = new(sendTable.NetTableName);

		// Create a decoder for it if necessary.
		if (NeedsDecoder)
		{
			// Make a decoder for it.
			RecvDecoder pDecoder = new RecvDecoder();
			DTRecv.g_RecvDecoders.AddLast( pDecoder );
		
			RecvTable? pRecvTable = DTRecv.FindRecvTable( pTable.NetTableName );
			if ( pRecvTable == null )
			{
				// Attempt to find a renamed version of the table.
				pRecvTable = FindRenamedTable( pTable.NetTableName );
				if ( pRecvTable == null )
				{
					Warning("DataTable warning: No matching RecvTable for SendTable '{0}'.\n", pTable.NetTableName);
					return false;
				}
			}

			pRecvTable.Decoder = pDecoder;
			pDecoder.RecvTable = pRecvTable;

			pDecoder.ClientSendTable = pClientSendTable;
			pDecoder.Precalc.Table = pClientSendTable.SendTable;
			pClientSendTable.SendTable.Precalc = pDecoder.Precalc;

			// Initialize array properties.
			SetupArrayProps_R( pRecvTable );
		}

		// Read the property list.
		pTable.Props = sendTable.Props;
		pTable.Props = pTable.Props != null ? new SendProp[pTable.TotalProps] : null;
		pClientSendTable.Props.EnsureCapacity(pTable.TotalProps);

		for ( int iProp=0; iProp < pTable.TotalProps; iProp++ )
		{
			CClientSendProp pClientProp = pClientSendTable.Props[iProp];
			SendProp pProp = pTable.Props[iProp];
			SendProp pSendTableProp = sendTable.Props[iProp];

			pProp.Type = (SendPropType)pSendTableProp.Type;
			pProp.VarName = new(pSendTableProp.GetName());
			pProp.SetFlags(pSendTableProp.GetFlags());

			if (pProp.Type == SendPropType.DataTable)
			{
				string pDTName = pSendTableProp.ExcludeDTName; // HACK
				if ( pSendTableProp.GetDataTable() != null )
					pDTName = pSendTableProp.GetDataTable().NetTableName;

				pClientProp.SetTableName(new(pDTName));
			
				// Normally we wouldn't care about this but we need to compare it against 
				// proxies in the server DLL in SendTable_BuildHierarchy.
				pProp.SetDataTableProxyFn(pSendTableProp.GetDataTableProxyFn());
				pProp.SetOffset(pSendTableProp.GetOffset());
			} else {
				if (pProp.IsExcludeProp())
				{
					pProp.ExcludeDTName = new(pSendTableProp.GetExcludeDTName());
				} else if (pProp.GetType() == SendPropType.Array) {
					pProp.SetNumElements( pSendTableProp.GetNumElements() );
				} else {
					pProp.LowValue = pSendTableProp.LowValue;
					pProp.HighValue = pSendTableProp.HighValue;
					pProp.NumBits = pSendTableProp.NumBits;
				}
			}
		}

		return true;
	}

	private static void MaybeCreateReceiveTable(List<SendTable> visited, SendTable table, bool NeedsDecoder)
	{
		if (visited.Contains(table))
			return;

		visited.Add(table);

		SetupReceiveTableFromSendTable(table, NeedsDecoder);
	}

	private static void MaybeCreateReceiveTable_R(List<SendTable> visited, SendTable Table)
	{
		MaybeCreateReceiveTable(visited, Table, false);

		// Make sure we send child send tables..
		for(int i=0; i < Table.TotalProps; ++i)
		{
			SendProp pProp = Table.Props[i];
			if(pProp.Type == SendPropType.DataTable)
			{
				MaybeCreateReceiveTable_R( visited, pProp.GetDataTable() );
			}
		}
	}

	public static void CreateClientTablesFromServerTables(ServerClass Classes)
	{
		ServerClass? Current;
		List<SendTable> visited = new();

		// First, we send all the leaf classes. These are the ones that will need decoders
		// on the client.
		for (Current=Classes; Current != null; Current=Current.Next)
			MaybeCreateReceiveTable(visited, Current.Table, true);

		// Now, we send their base classes. These don't need decoders on the client
		// because we will never send these SendTables by themselves.
		for (Current=Classes; Current != null; Current=Current.Next)
			MaybeCreateReceiveTable_R(visited, Current.Table);
	}

	public static void CreateClientClassInfosFromServerClasses(BaseClientState state, ServerClass Classes)
	{
		state.ServerClassInfo = new ServerClassInfo[state.ServerClasses];
		for (int i = 0; i < state.ServerClasses; i++)
			state.ServerClassInfo[i] = new ServerClassInfo();

		// Now fill in the entries
		int curID = 0;
		for (ServerClass? pClass=Classes; pClass != null; pClass=pClass.Next)
		{
			pClass.ClassID = curID++;
			
			state.ServerClassInfo[pClass.ClassID] = new ServerClassInfo();
			state.ServerClassInfo[pClass.ClassID].ClassName = new(pClass.NetworkName);
			state.ServerClassInfo[pClass.ClassID].DatatableName = new(pClass.Table.GetName());
		}
	}
}