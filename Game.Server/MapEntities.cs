global using static Game.Server.MapEntities;

using Source.Common;
using Source.Common.Engine;

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Game.Server;

public static class MapEntities
{
	static ref Edict? g_pForceAttachEdict => ref BaseEntity.g_pForceAttachEdict;

	public static BaseEntity? CreateEntityByName(ReadOnlySpan<char> className, int forceEdictIndex = -1) {
		if (forceEdictIndex != -1) {
			g_pForceAttachEdict = engine.CreateEdict(forceEdictIndex);
			if (g_pForceAttachEdict == null)
				Error($"CreateEntityByName( {className}, {forceEdictIndex} ) - CreateEdict failed.");
		}

		IServerNetworkable? network = EntityFactoryDictionary().Create(className);
		g_pForceAttachEdict = null;

		if (network == null)
			return null;

		BaseEntity? entity = (BaseEntity?)network.GetBaseEntity();
		Assert(entity);
		return entity;
	}

}
