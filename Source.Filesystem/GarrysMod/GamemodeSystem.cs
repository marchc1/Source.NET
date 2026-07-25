using Source.Common.GarrysMod;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Filesystem.GarrysMod;

public class GamemodeSystem : Gamemode.System
{
	public ref IGamemodeSystem.Information Active() {
		throw new NotImplementedException();
	}

	public void Clear() {
		throw new NotImplementedException();
	}

	public ref IGamemodeSystem.Information FindByName(ReadOnlySpan<char> str) {
		throw new NotImplementedException();
	}

	public List<IGamemodeSystem.Information> GetList() {
		throw new NotImplementedException();
	}

	public bool IsServerBlacklisted(ReadOnlySpan<char> address, ReadOnlySpan<char> hostname, ReadOnlySpan<char> description, ReadOnlySpan<char> gm, ReadOnlySpan<char> map) {
		throw new NotImplementedException();
	}

	public void OnJoinServer(ReadOnlySpan<char> unk1) {
		throw new NotImplementedException();
	}

	public void OnLeaveServer() {
		throw new NotImplementedException();
	}

	public void Refresh() {
		throw new NotImplementedException();
	}

	public void SetActive(ReadOnlySpan<char> unk1) {
		throw new NotImplementedException();
	}
}
