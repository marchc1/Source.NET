using Source.Common.Engine;

namespace Source.Common;

public interface IClientUnknown : IHandleEntity {
	ICollideable? GetCollideable();
	IClientNetworkable? GetClientNetworkable();
	IClientRenderable? GetClientRenderable();
	IClientEntity? GetIClientEntity();
	IClientThinkable? GetClientThinkable();
}
