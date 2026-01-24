global using static Game.Shared.PredictionCopy;

using Source.Common;

using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Shared;

public enum PredictionCopyType
{
	Everything,
	NonNetworkedOnly,
	NetworkedOnly
}

public static class PredictionCopy
{
	static TypeDescription? FindFieldByName_R(ReadOnlySpan<char> fieldname, DataMap? dmap) {
		if (dmap == null)
			throw new NullReferenceException("Do not pass a null DataMap into FindFieldByName.");

		int c = dmap.DataNumFields;
		for (int i = 0; i < c; i++) {
			TypeDescription td = dmap.DataDesc[i];
			if (td.FieldType == FieldType.Void)
				continue;

			if (td.FieldType == FieldType.Embedded) {
				// TODO:  this will only find the first subclass with the variable of the specified name
				//  At some point we might want to support multiple levels of overriding automatically
				TypeDescription? ret = FindFieldByName_R(fieldname, td.TD!);
				if (ret != null)
					return ret;
			}

			if (0 == stricmp(td.FieldName, fieldname))
				return td;
		}

		if (dmap.BaseMap != null)
			return FindFieldByName_R(fieldname, dmap.BaseMap);

		return null;
	}
	public static TypeDescription? FindFieldByName(ReadOnlySpan<char> fieldname, DataMap? dmap) {
		return FindFieldByName_R(fieldname, dmap);
	}
}
