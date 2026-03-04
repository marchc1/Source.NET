using Source.Common;
using Source.Common.Bitbuffers;
using Source.Common.Engine;

using System.Diagnostics;

namespace Source.Engine;

[EngineComponent]
public class EngineSendTable(DtCommonEng DtCommonEng)
{
	readonly List<SendTable> SendTables = [];
	public CRC32_t SendTableCRC;
	internal bool Init(Span<SendTable> tables) {
		// Initialize them all.
		for (int i = 0; i < tables.Length; i++) {
			if (!InitTable(tables[i]))
				return false;
		}

		SendTables.EnsureCapacity(tables.Length);
		for (int i = 0; i < tables.Length; i++)
			SendTables.Add(tables[i]);

		SendTableCRC = ComputeCRC();

		return true;
	}

	private uint ComputeCRC() {
		// Totally lie for now
		return 0;
	}

	private bool InitTable(SendTable table) {
		if (table.Precalc != null)
			return true;

		SendTablePrecalc precalc = new();
		table.Precalc = precalc;

		precalc.SendTable = table;
		table.Precalc = precalc;

		CalcNextVectorElems(table);

		if (!precalc.SetupFlatPropertyArray())
			return false;

		Validate(precalc);
		return true;
	}

	private void Validate(SendTablePrecalc precalc) {
		SendTable table = precalc.SendTable;
		for (int i = 0; i < table.Props?.Length; i++) {
			SendProp prop = table.Props[i];

			if (prop.GetArrayProp() != null) {
				if (prop.GetArrayProp()!.GetPropType() == SendPropType.DataTable)
					Error($"Invalid property: {table.NetTableName}/{prop.GetName()} (array of datatables) [on prop {i} of {table.Props?.Length ?? 0} ({prop.GetArrayProp()!.GetName()})].");
			}
			else
				ErrorIfNot(prop.GetNumElements() == 1, $"Prop {table.NetTableName}/{prop.GetName()} has an invalid element count for a non-array.");

			if (prop.Bits == 1 && (prop.GetFlags() & PropFlags.Unsigned) == 0)
				DataTable_Warning($"SendTable prop {table.NetTableName}::{prop.GetName()} is a 1-bit signed property. Use PropFlags.Unsigned or the client will never receive a value.\n");
		}

		for (int i = 0; i < precalc.GetNumProps(); ++i) {
			SendProp prop = precalc.GetProp(i)!;
			if ((prop.GetFlags() & PropFlags.EncodedAgainstTickCount) != 0) {
				table.SetHasPropsEncodedAgainstTickcount(true);
				break;
			}
		}
	}

	private void DataTable_Warning(ReadOnlySpan<char> message) {
		Warning(message);
		Debug.Assert(false, new(message));
	}

	private void CalcNextVectorElems(SendTable table) {
		for (int i = 0; i < table.GetNumProps(); i++) {
			SendProp prop = table.GetProp(i);

			if (prop.GetPropType() == SendPropType.DataTable)
				CalcNextVectorElems(prop.GetDataTable()!);
			else if (prop.GetOffset() < 0) {
				prop.SetOffset(-prop.GetOffset());
				prop.SetFlags(prop.GetFlags() | PropFlags.IsAVectorElem);
			}
			else if (prop.FieldInfo is DynamicArrayIndexAccessor afii && afii.IsAVectorElement)
				prop.SetFlags(prop.GetFlags() | PropFlags.IsAVectorElem);
		}
	}

	public bool Encode(SendTable table, object data, bf_write dataOut, int objectId, SendProxyRecipients[] recipients, bool nonZeroOnly) {
		SendTablePrecalc precalc = table.Precalc!;
		ErrorIfNot(precalc != null, $"SendTable_Encode: Missing precalc for table {table.NetTableName}.");
		if (recipients.Length > 0)
			ErrorIfNot(recipients.Length >= precalc.GetNumDataTableProxies(), $"SendTable_Encode: recipients array too small (got {recipients.Length}, need {precalc.GetNumDataTableProxies()}).");

		EncodeInfo info = new(precalc, data, objectId, dataOut) {
			Recipients = recipients
		};
		info.Init();

		int numProps = precalc.GetNumProps();
		for (int i = 0; i < numProps; i++) {
			if (!info.IsPropProxyValid(i))
				continue;

			info.SeekToProp((uint)i);

			if (nonZeroOnly && IsPropZero(info, i))
				continue;

			EncodeProp(info, i);
		}

		return !dataOut.Overflowed;
	}


	bool IsPropZero(EncodeInfo info, int _) {
		SendProp p = info.GetCurProp()!;

		DVariant var = new();
		object baseData = info.GetCurStructBase()!;

		IFieldAccessor accessor = p.FieldInfo;
		p.GetProxyFn()(p, baseData, accessor, ref var, 0, info.GetObjectID());

		return PropTypeFns.g_PropTypeFns[(int)p.Type].IsZero(baseData, ref var, p);
	}

	public int GetNumFlatProps(SendTable table) {
		SendTablePrecalc precalc = table.Precalc!;
		ErrorIfNot(precalc != null, $"SendTable_GetNumFlatProps: missing pPrecalc.");
		return precalc.GetNumProps();
	}

	void EncodeProp(EncodeInfo info, int prop) {
		DVariant var = new();

		SendProp p = info.GetCurProp()!;
		object baseData = info.GetCurStructBase()!;

		IFieldAccessor accessor = p.FieldInfo;
		p.GetProxyFn()(p, baseData, accessor, ref var, 0, info.GetObjectID());

		info.DeltaBitsWriter.WritePropIndex(prop);

		PropTypeFns.g_PropTypeFns[(int)p.Type].Encode(baseData, ref var, p, info.DeltaBitsWriter.GetBitBuf(), info.GetObjectID());
	}

	public int CalcDelta(SendTable table, byte[]? fromState, int nFromBits, byte[] toState, int nToBits, Span<int> deltaProps, int maxDeltaProps, int objectId) {
		int nDeltaProps = 0;

		SendTablePrecalc precalc = table.Precalc!;

		bf_read toBits = new("CalcDelta/toBits", toState, BitBuffer.BitByte(nToBits), nToBits);
		DeltaBitsReader toBitsReader = new(toBits);
		uint iToProp = toBitsReader.ReadNextPropIndex();

		if (fromState != null) {
			bf_read fromBitsBuf = new("CalcDelta/fromBits", fromState, BitBuffer.BitByte(nFromBits), nFromBits);
			DeltaBitsReader fromBitsReader = new(fromBitsBuf);
			uint iFromProp = fromBitsReader.ReadNextPropIndex();

			for (; iToProp < Constants.MAX_DATATABLE_PROPS; iToProp = toBitsReader.ReadNextPropIndex()) {
				Assert((int)iToProp >= 0);

				// Skip any properties in the from state that aren't in the to state.
				while (iFromProp < iToProp) {
					fromBitsReader.SkipPropData(precalc.GetProp((int)iFromProp)!);
					iFromProp = fromBitsReader.ReadNextPropIndex();
				}

				if (iFromProp == iToProp) {
					// The property is in both states, so compare them and write the index
					// if the states are different.
					if (fromBitsReader.ComparePropData(ref toBitsReader, precalc.GetProp((int)iToProp)!) != 0) {
						deltaProps[nDeltaProps++] = (int)iToProp;
						if (nDeltaProps >= maxDeltaProps)
							break;
					}

					// Seek to the next property.
					iFromProp = fromBitsReader.ReadNextPropIndex();
				}
				else {
					// Only the 'to' state has this property, so just skip its data and register a change.
					toBitsReader.SkipPropData(precalc.GetProp((int)iToProp)!);
					deltaProps[nDeltaProps++] = (int)iToProp;
					if (nDeltaProps >= maxDeltaProps)
						break;
				}
			}

			Assert(iToProp == ~0u);

			fromBitsReader.ForceFinished();
		}
		else {
			for (; iToProp != unchecked((uint)-1); iToProp = toBitsReader.ReadNextPropIndex()) {
				Assert((int)iToProp >= 0 && iToProp < Constants.MAX_DATATABLE_PROPS);

				SendProp prop = precalc.GetProp((int)iToProp)!;
				if (!PropTypeFns.g_PropTypeFns[(int)prop.Type].IsEncodedZero(prop, toBits)) {
					deltaProps[nDeltaProps++] = (int)iToProp;
					if (nDeltaProps >= maxDeltaProps)
						break;
				}
			}
		}

		return nDeltaProps;
	}
}

class EncodeInfo(SendTablePrecalc precalc, object structData, int objectId, bf_write dataOut) : DatatableStack(precalc, structData, objectId)
{
	public DeltaBitsWriter DeltaBitsWriter = new(dataOut);
	public override void RecurseAndCallProxies(SendNode node, object instance) { }
}