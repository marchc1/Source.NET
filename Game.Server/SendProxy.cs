global using static Game.Server.SendProxy;

using Game.Shared;

using Source;
using Source.Common;
namespace Game.Server;

public static class SendProxy
{
	public const int PREDICTABLE_ID_BITS = 31;

	public static SendProp SendPropPredictableId(IFieldAccessor field)
		=> SendPropInt(field, PREDICTABLE_ID_BITS, PropFlags.Unsigned, SendProxy_PredictableIdToInt);

	private static void SendProxy_PredictableIdToInt(SendProp prop, object instance, IFieldAccessor field, ref DVariant outData, int element, int objectID) {
		PredictableId? pId = field.GetValue<PredictableId?>(instance);
		if (pId != null)
			outData.Int = pId.GetRaw();
		else
			outData.Int = 0;
	}

	public static SendProp SendPropBool(IFieldAccessor field) {
		return SendPropInt(field, 1, PropFlags.Unsigned);
	}
	public static SendProp SendPropTime(IFieldAccessor field) {
		return SendPropFloat(field, -1, PropFlags.NoScale);
	}
	public static SendProp SendPropEHandle(IFieldAccessor field, PropFlags flags = 0, SendVarProxyFn? proxyFn = null) {
		return SendPropInt(field, Constants.NUM_NETWORKED_EHANDLE_BITS, PropFlags.Unsigned | flags, proxyFn ?? SendProxy_EHandleToInt);
	}
	public static SendProp SendPropIntWithMinusOneFlag(IFieldAccessor field, int bits, SendVarProxyFn? proxyFn = null)
		=> SendPropInt(field, bits, PropFlags.Unsigned, proxyFn ?? SendProxy_IntAddOne);

	public static void SendProxy_IntAddOne(SendProp prop, object instance, IFieldAccessor field, ref DVariant outData, int element, int objectID) {
		outData.Int = field.GetValue<int>(instance) + 1;
	}

	public static void SendProxy_Color32ToInt(SendProp prop, object instance, IFieldAccessor field, ref DVariant outData, int element, int objectID) {
		outData.Int = field.GetValue<Color>(instance).GetRawColor();
	}

	public static void SendProxy_ShortAddOne(SendProp prop, object instance, IFieldAccessor field, ref DVariant outData, int element, int objectID) {
		outData.Int = field.GetValue<short>(instance) + 1;
	}
	public static void SendProxy_EHandleToInt(SendProp prop, object instance, IFieldAccessor field, ref DVariant outData, int element, int objectID) {
		BaseHandle handle = field.GetValue<BaseHandle>(instance);
		if (handle.Get() != null) {
			int iSerialNum = handle.GetSerialNumber() & ((1 << Constants.NUM_NETWORKED_EHANDLE_SERIAL_NUMBER_BITS) - 1);
			outData.Int = handle.GetEntryIndex() | (iSerialNum << Constants.MAX_EDICT_BITS);
		}
		else
			outData.Int = Constants.INVALID_NETWORKED_EHANDLE_VALUE;
		field.SetValue<BaseHandle>(instance, handle);
	}
}
