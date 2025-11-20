using Source.Common;

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
}
