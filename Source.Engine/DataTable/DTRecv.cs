using Source.Common.Bitbuffers;
using Source.Common.Networking.DataTable;
using Source.Engine.DataTable;

public static class DTRecv
{
	public static bool Decode(
		RecvTable table,
		byte[] structBase,
		bf_read input,
		int objectID,
		bool updateDTI)
	{
		var decoder = table.Decoder 
			?? throw new InvalidOperationException($"Missing decoder for {table.NetTableName}.");

		var stack = new ClientDatatableStack(decoder, structBase, objectID);
		stack.Init();

		int iStartBit = 0, nIndexBits = 0, iLastBit = input.BitsRead;
		var deltaReader = new DeltaBitsReader(input);

		uint propIndex;
		while ((propIndex = deltaReader.ReadNextPropIndex()) < NetConstants.MAX_DATATABLE_PROPS)
		{
			stack.SeekToProp((int)propIndex);

			var recvProp = decoder.GetProp((int)propIndex);
			var sendProp = decoder.GetSendProp((int)propIndex);

			var decodeInfo = new DecodeInfo
			{
				Struct = stack.GetCurrentStructBase(),
				Data = stack.GetCurrentStructBase() + recvProp.GetOffset(),
				RecvProp = stack.IsCurrentProxyValid() ? recvProp : null,
				Prop = sendProp,
				In = input,
				ObjectID = objectID
			};

			DTEncode.g_PropTypeFns[(int)sendProp.Type].Decode(decodeInfo);
			iLastBit = input.BitsRead;
		}

		return !input.Overflowed;
	}

	public static void DecodeZeros(RecvTable table, byte[] structBase, int objectID)
	{
		var decoder = table.Decoder
			?? throw new InvalidOperationException($"Missing decoder for {table.NetTableName}.");

		var stack = new ClientDatatableStack(decoder, structBase, objectID);
		stack.Init();

		for (int i = 0; i < decoder.GetNumProps(); i++)
		{
			stack.SeekToProp(i);
			var recvProp = decoder.GetProp(i);
			if (recvProp == null) continue;

			var decodeInfo = new DecodeInfo
			{
				Struct = stack.GetCurrentStructBase(),
				Data = stack.GetCurrentStructBase() + recvProp.GetOffset(),
				RecvProp = recvProp,
				Prop = decoder.GetSendProp(i),
				In = null,
				ObjectID = objectID
			};

			DTEncode.g_PropTypeFns[(int)recvProp.RecvType].DecodeZero(decodeInfo);
		}
	}

	public static int MergeDeltas(
		RecvTable table,
		bf_read oldState,
		bf_read newState,
		bf_write output,
		int objectID,
		out int[] changedProps,
		bool updateDTI)
	{
		if (table.Decoder == null)
			throw new InvalidOperationException($"Missing decoder for {table.NetTableName}.");

		var decoder = table.Decoder;
		var oldReader = oldState != null ? new DeltaBitsReader(oldState) : null;
		var newReader = new DeltaBitsReader(newState);
		var writer = new DeltaBitsWriter(output);

		uint oldProp = oldReader?.ReadNextPropIndex() ?? uint.MaxValue;
		uint newProp = newReader.ReadNextPropIndex();

		var changed = new List<int>();
		int iStartBit = 0, nIndexBits = 0, iLastBit = newState.BitsRead;

		while (true)
		{
			while (oldProp < newProp)
			{
				writer.WritePropIndex(oldProp);
				oldReader.CopyPropData(writer.GetBitBuf(), decoder.GetSendProp((int)oldProp));
				oldProp = oldReader.ReadNextPropIndex();
			}

			if (newProp >= NetConstants.MAX_DATATABLE_PROPS) break;

			if (oldProp == newProp)
			{
				oldReader.SkipPropData(decoder.GetSendProp((int)oldProp));
				oldProp = oldReader.ReadNextPropIndex();
			}

			writer.WritePropIndex(newProp);
			newReader.CopyPropData(writer.GetBitBuf(), decoder.GetSendProp((int)newProp));

			changed.Add((int)newProp);

			iLastBit = newState.BitsRead;
			newProp = newReader.ReadNextPropIndex();
		}

		if ((oldState?.Overflowed ?? false) || newState.Overflowed || output.Overflowed)
			throw new InvalidOperationException($"Overflow in merging deltas for table {table.NetTableName}.");

		changedProps = changed.ToArray();
		return changed.Count;
	}

	public static void CopyEncoding(
		RecvTable table,
		bf_read input,
		bf_write output,
		int objectID)
	{
		MergeDeltas(table, null, input, output, objectID, out _, false);
	}
}