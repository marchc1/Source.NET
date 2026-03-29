using Game.Shared;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;


public static class ClientTraceFieldProps
{
	extension(ref Trace tr)
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool DidHitWorld() => tr.Ent == cl_entitylist.GetBaseEntity(0);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool DidHitNonWorldEntity() => tr.Ent != null && !tr.DidHitWorld();
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public int GetEntityIndex() => tr.Ent?.EntIndex() ?? -1;
	}
}
