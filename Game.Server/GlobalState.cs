global using static Game.Server.GlobalState;

using Game.Shared;

using Source.Common.Utilities;

using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Server;

public enum GlobalEState {
	Off,
	On,
	Dead
}

public struct GlobalEntity {
	public UtlSymbol Name;
	public UtlSymbol LevelName;
	public GlobalEState State;
	public int Counter;
}

public class GlobalState : AutoGameSystem
{
	static readonly GlobalState globalState = new("GlobalState");

	public GlobalState(ReadOnlySpan<char> name) : base(name) {

	}

	public static void ResetGlobalState() {
		globalState.Reset();
	}

	public void Reset() {
		List.Clear();
		NameList.RemoveAll();
	}

	public readonly UtlSymbolTable NameList = new();
	private bool DisableStateUpdates;
	private readonly List<GlobalEntity> List = [];
}
