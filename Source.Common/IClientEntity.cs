using Source.Common.Mathematics;

using System.Numerics;

namespace Source.Common;
public interface IClientEntity : IClientUnknown, IClientRenderable, IClientNetworkable, IClientThinkable, IDisposable
{
	ref readonly Vector3 GetAbsOrigin();
	ref readonly QAngle GetAbsAngles();
	bool GetSoundSpatialization(ref SpatializationInfo info);
}