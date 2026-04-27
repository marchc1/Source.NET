global using static Game.Server.GlobalState;

using CommunityToolkit.HighPerformance;

using Game.Shared;

using Source;
using Source.Common.Utilities;

using System;
using System.Collections;
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

	public int GetIndex(ReadOnlySpan<char> str){
		UtlSymbol symName = new(NameList.Find(str));

		if (symName.IsValid()) {
			for (int i = List.Count - 1; i >= 0; --i) {
				if (List[i].Name == symName)
					return i;
			}
		}

		return -1;
	}

	public void EnableStateUpdates(bool enable) => DisableStateUpdates = !enable; 
	public void SetState(int globalIndex, GlobalEState state){
		if (DisableStateUpdates || !List.IsValidIndex(globalIndex))
			return;
		List.AsSpan()[globalIndex].State = state;
	}
	public GlobalEState GetState(int globalIndex) => List.IsValidIndex(globalIndex) ? List.AsSpan()[globalIndex].State : GlobalEState.Off;
	public void SetCounter(int globalIndex, int counter) {
		if (DisableStateUpdates || !List.IsValidIndex(globalIndex))
			return;
		List.AsSpan()[globalIndex].Counter = counter;
	}
	public int AddToCounter(int globalIndex, int delta) {
		if (DisableStateUpdates || !List.IsValidIndex(globalIndex))
			return 0;
		return List.AsSpan()[globalIndex].Counter += delta;
	}
	public int GetCounter(int globalIndex) {
		if (!List.IsValidIndex(globalIndex))
			return 0;
		return List.AsSpan()[globalIndex].Counter;
	}

	public void SetMap(int globalIndex, ReadOnlySpan<char> mapname) {
		if (!List.IsValidIndex(globalIndex))
			return;
		List.AsSpan()[globalIndex].LevelName = new(NameList.AddString(mapname));
	}
	public ReadOnlySpan<char> GetMap(int globalIndex) {
		if (!List.IsValidIndex(globalIndex))
			return null;
		return NameList.String(List.AsSpan()[globalIndex].LevelName);
	}
	public ReadOnlySpan<char> GetName(int globalIndex) {
		if (!List.IsValidIndex(globalIndex))
			return null;
		return NameList.String(List.AsSpan()[globalIndex].Name);
	}
	public nint AddEntity(ReadOnlySpan<char> globalname, ReadOnlySpan<char> mapname, GlobalEState state){
		GlobalEntity entity;
		entity.Name = new(NameList.AddString(globalname));
		entity.LevelName = new(NameList.AddString(mapname));
		entity.State = state;
		entity.Counter = 0;
		int index = GetIndex(NameList.String(entity.Name));
		if (index >= 0)
			return index;
		List.Add(entity);
		return List.Count - 1;
	}
	public nint GetNumGlobals() => List.Count;

	public static void GlobalEntity_SetState(int globalIndex, GlobalEState state) => globalState.SetState(globalIndex, state);
	public static void GlobalEntity_SetState(string globalName, GlobalEState state) => GlobalEntity_SetState(GlobalEntity_GetIndex(globalName), state);
	public static int GlobalEntity_GetIndex(string str) => GlobalEntity_GetIndex((ReadOnlySpan<char>)str);
	public static int GlobalEntity_GetIndex(ReadOnlySpan<char> str) => globalState.GetIndex(str);

	public readonly UtlSymbolTable NameList = new();
	private bool DisableStateUpdates;
	private readonly List<GlobalEntity> List = [];
}
