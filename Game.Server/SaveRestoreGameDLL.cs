using Source;
using Source.Common;
using Source.Common.Mathematics;

using System.Numerics;

namespace Game.Server;

public static class SaveRestoreGameDLL
{
	public static bool ParseKeyvalue(object obj, TypeDescription[] fields, int numFields, ReadOnlySpan<char> keyName, ReadOnlySpan<char> value)
	{
		for (int i = 0; i < numFields; i++)
		{
			TypeDescription field = fields[i];

			if (field.FieldType == FieldType.Embedded && field.FieldSize == 1)
			{
				for (DataMap? dmap = field.TD; dmap != null; dmap = dmap.BaseMap)
				{
					object? embeddedObject = field.Accessor.GetValue<object?>(obj);
					if (embeddedObject != null && ParseKeyvalue(embeddedObject, dmap.DataDesc, dmap.DataNumFields, keyName, value))
						return true;
				}
			}

			if ((field.Flags & FieldTypeDescFlags.Key) != 0 && stricmp(field.ExternalName, keyName) == 0)
			{
				switch (field.FieldType)
				{
					case FieldType.ModelName:
					case FieldType.SoundName:
					case FieldType.String:
						field.Accessor.SetValue(obj, new string(value));
						return true;

					case FieldType.Time:
					case FieldType.Float:
						field.Accessor.SetValue(obj, strtof(value, out _));
						return true;

					case FieldType.Boolean:
						field.Accessor.SetValue(obj, atoi(value) != 0);
						return true;

					case FieldType.Character:
						field.Accessor.SetValue(obj, (sbyte)atoi(value));
						return true;

					case FieldType.Short:
						field.Accessor.SetValue(obj, (short)atoi(value));
						return true;

					case FieldType.Integer:
					case FieldType.Tick:
						field.Accessor.SetValue(obj, atoi(value));
						return true;

					case FieldType.PositionVector:
					case FieldType.Vector:
						Vector3 vec = default;
						UTIL_StringToVector(vec.Base(), value);
						field.Accessor.SetValue(obj, vec);
						return true;

					// case FieldType.VMatrix:
					// case FieldType.VMatrixWorldspace:
					// 	UTIL_StringToFloatArray(..., 16, value);
					// 	return true;

					// case FieldType.Matrix3x4Worldspace:
					// 	UTIL_StringToFloatArray(..., 12, value);
					// 	return true;

					case FieldType.Color32:
						Util.StringToColor32(out Color color, value);
						field.Accessor.SetValue(obj, color);
						return true;

					case FieldType.Custom:
						SaveRestoreFieldInfo fieldInfo = new(field.Accessor, obj, default);
						field.SaveRestoreOps!.Parse(in fieldInfo, value);
						return true;

					default:
					case FieldType.Interval:
					case FieldType.ClassPtr:
					case FieldType.EHandle:
						Warning("Bad field in entity!!\n");
						Assert(0);
						break;
				}
			}
		}

		return false;
	}
}
