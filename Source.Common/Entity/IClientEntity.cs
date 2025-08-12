using Source.Common.Mathematics;

namespace Source.Common.Entity;

public interface IClientEntity : IClientUnknown, IClientRenderable, IClientNetworkable, IClientThinkable
{
	// Delete yourself. - RaphaelIT7: This is more like it >:3 https://www.youtube.com/watch?v=ZmP4VwbbaCg
	new public void Release();
	// public Vector GetAbsOrigin();
	// public QAngle GetAbsAngles();
}