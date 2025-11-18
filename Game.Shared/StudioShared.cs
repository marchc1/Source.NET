#if CLIENT_DLL // TODO: We need more serverside stuff before we can add this || GAME_DLL
using Source.Common;
using Source.Engine;

namespace Game.Shared;

public static class StudioExts {
	public static VirtualModel? GetVirtualModel(this StudioHeader self) {
		if (self.NumIncludeModels == 0)
			return null;
		return modelinfo.GetVirtualModel(self);
	}
}
#endif
