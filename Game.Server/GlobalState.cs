global using static Game.Server.GlobalState;

using CommunityToolkit.HighPerformance;

using Game.Server;
using Game.Shared;

using Source;
using Source.Common.Utilities;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Game.Server;

public enum GlobalEState
{
	Off,
	On,
	Dead
}

public struct GlobalEntity
{
	public UtlSymbol Name;
	public UtlSymbol LevelName;
	public GlobalEState State;
	public int Counter;

	public static void SetState(int globalIndex, GlobalEState state) => GlobalState.globalState.SetState(globalIndex, state);
	public static void SetState(ReadOnlySpan<char> globalName, GlobalEState state) => GlobalState.globalState.SetState(GetIndex(globalName), state);
	public static void SetCounter(int globalIndex, int counter) => GlobalState.globalState.SetCounter(globalIndex, counter);
	public static void SetCounter(ReadOnlySpan<char> globalName, int counter) => GlobalState.globalState.SetCounter(GetIndex(globalName), counter);
	public static int AddToCounter(int globalIndex, int delta) => GlobalState.globalState.AddToCounter(globalIndex, delta);
	public static int AddToCounter(ReadOnlySpan<char> globalName, int delta) => GlobalState.globalState.AddToCounter(GetIndex(globalName), delta);
	public static void EnableStateUpdates(bool bEnable) => GlobalState.globalState.EnableStateUpdates(bEnable);
	public static void SetMap(int globalIndex, string mapname) => GlobalState.globalState.SetMap(globalIndex, mapname);
	public static void SetMap(ReadOnlySpan<char> globalName, string mapname) => GlobalState.globalState.SetMap(GetIndex(globalName), mapname);
	public static int Add(ReadOnlySpan<char> globalname, ReadOnlySpan<char> mapname, GlobalEState state) => (int)GlobalState.globalState.AddEntity(globalname, mapname, state);
	public static int GetIndex(ReadOnlySpan<char> globalname) => GlobalState.globalState.GetIndex(globalname);
	public static GlobalEState GetState(int globalIndex) => GlobalState.globalState.GetState(globalIndex);
	public static GlobalEState GetState(ReadOnlySpan<char> globalName) => GlobalState.globalState.GetState(GetIndex(globalName));
	public static int GetCounter(int globalIndex) => GlobalState.globalState.GetCounter(globalIndex);
	public static int GetCounter(ReadOnlySpan<char> globalName) => GlobalState.globalState.GetCounter(GetIndex(globalName));
	public static ReadOnlySpan<char> GetMap(int globalIndex) => GlobalState.globalState.GetMap(globalIndex);
	public static ReadOnlySpan<char> GetMap(ReadOnlySpan<char> globalName) => GlobalState.globalState.GetMap(GetIndex(globalName));
	public static ReadOnlySpan<char> GetName(int globalIndex) => GlobalState.globalState.GetName(globalIndex);
	public static ReadOnlySpan<char> GetName(ReadOnlySpan<char> globalName) => GlobalState.globalState.GetName(GetIndex(globalName));
	public static int GetNumGlobals() => (int)GlobalState.globalState.GetNumGlobals();

}

public class GlobalState : AutoGameSystem
{
	internal static readonly GlobalState globalState = new("GlobalState");
	public GlobalState(ReadOnlySpan<char> name) : base(name) {

	}

	public static void ResetGlobalState() {
		globalState.Reset();
	}

	public void Reset() {
		List.Clear();
		NameList.RemoveAll();
	}

	public int GetIndex(ReadOnlySpan<char> str) {
		UtlSymbol symName = NameList.Find(str);

		if (symName.IsValid()) {
			for (int i = List.Count - 1; i >= 0; --i) {
				if (List[i].Name == symName)
					return i;
			}
		}

		return -1;
	}

	public void EnableStateUpdates(bool enable) => DisableStateUpdates = !enable;
	public void SetState(int globalIndex, GlobalEState state) {
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
	public nint AddEntity(ReadOnlySpan<char> globalname, ReadOnlySpan<char> mapname, GlobalEState state) {
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

	public readonly UtlSymbolTable NameList = new();
	private bool DisableStateUpdates;
	private readonly List<GlobalEntity> List = [];
}
