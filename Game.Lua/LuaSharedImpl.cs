using Source.Common.GarrysMod;
using Source.Common.GarrysMod.Lua;

using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Lua;

public class LuaSharedImpl : ILuaShared
{
	public void CloseLuaInterface(ILuaInterface unk1) {
		throw new NotImplementedException();
	}

	public ILuaInterface CreateLuaInterface(byte unk1, bool unk2) {
		throw new NotImplementedException();
	}

	public void DumpStats() {
		throw new NotImplementedException();
	}

	public void EmptyCache() {
		throw new NotImplementedException();
	}

	public void FindScripts(ReadOnlySpan<char> unk1, ReadOnlySpan<char> unk2, List<string> unk3) {
		throw new NotImplementedException();
	}

	public ref LuaFile? GetCache(ReadOnlySpan<char> unk1) {
		throw new NotImplementedException();
	}

	public ILuaInterface GetLuaInterface(byte unk1) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetStackTraces() {
		throw new NotImplementedException();
	}

	public void Init(IServiceProvider services, bool unk1, IGet unk2) {

	}

	public void InvalidateCache(ReadOnlySpan<char> unk1) {
		throw new NotImplementedException();
	}

	public ref LuaFile? LoadFile(ReadOnlySpan<char> path, ReadOnlySpan<char> pathId, bool fromDatatable, bool fromFile) {
		throw new NotImplementedException();
	}

	public void MountLua(ReadOnlySpan<char> unk1) {
		throw new NotImplementedException();
	}

	public void MountLuaAdd(ReadOnlySpan<char> unk1, ReadOnlySpan<char> unk2) {
		throw new NotImplementedException();
	}

	public void SetFileContents(ReadOnlySpan<char> unk1, ReadOnlySpan<char> unk2) {
		throw new NotImplementedException();
	}

	public void SetLuaFindHook(LuaClientDatatableHook unk1) {
		throw new NotImplementedException();
	}

	public void Shutdown() {
		throw new NotImplementedException();
	}

	public void UnMountLua(ReadOnlySpan<char> unk1) {
		throw new NotImplementedException();
	}
}
