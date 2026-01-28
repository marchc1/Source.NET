using Source.Common;
using Source.Common.Engine;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Game.Server;

public class ServerNetworkProperty : IServerNetworkable, IEventRegisterCallback
{
	public int AreaNum() {
		throw new NotImplementedException();
	}

	public void FireEvent() {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetClassName() {
		throw new NotImplementedException();
	}

	public int EntIndex() => ENTINDEX(Pev);

	public Edict? GetEdict() {
		return Pev;
	}

	public IHandleEntity? GetEntityHandle() {
		return Outer;
	}

	public ServerClass GetServerClass() {
		throw new NotImplementedException();
	}

	public void Release() {
		throw new NotImplementedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public BaseEntity? GetBaseEntity() => Outer;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public BaseEntity? GetOuter() => Outer;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ref PVSInfo GetPVSInfo() => ref PVSInfo;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetNetworkParent(EHANDLE parent) => Parent.Index = parent.Index;

	public void AttachEdict(Edict? requiredEdict){
		if (requiredEdict == null)
			requiredEdict = engine.CreateEdict();

		Pev = requiredEdict;
		Pev.SetEdict(GetBaseEntity(), true);
	}


	private BaseEntity? Outer;
	private Edict Pev;
	private PVSInfo PVSInfo;
	private ServerClass? ServerClass;
	private readonly EHANDLE Parent = new();
	// event register later
	bool PendingStateChange;
}
