using Source.Common.Commands;
using Source.Common.Formats.Keyvalues;
using Source.Common.GarrysMod.Lua;

using System.Globalization;

namespace Game.Lua;

public class LuaConVarsImpl : ILuaConVars
{
	record struct ManagedCommand(ConCommandBase Command, bool IsConVar, bool Archive);

	readonly List<ManagedCommand> Managed = [];
	KeyValues? ClientCVars;
	KeyValues? ServerCVars;

	public void Cache(ReadOnlySpan<char> unk1, ReadOnlySpan<char> unk2) {
		throw new NotImplementedException();
	}

	public void ClearCache() {
		throw new NotImplementedException();
	}

	public ConCommand CreateConCommand(ReadOnlySpan<char> unk1, ReadOnlySpan<char> unk2, int unk3, FnCommandCallback unk4, FnCommandCompletionCallback unk5) {
		throw new NotImplementedException();
	}

	public ConVar CreateConVar(ReadOnlySpan<char> unk1, ReadOnlySpan<char> unk12, ReadOnlySpan<char> unk3, int unk4) {
		throw new NotImplementedException();
	}

	public void DestroyManaged() {
		SaveManaged();

		foreach (ManagedCommand managed in Managed)
			cvar.UnregisterConCommand(managed.Command);

		Managed.Clear();
	}

	void SaveManaged() {
		foreach (ManagedCommand managed in Managed) {
			if (!managed.IsConVar || !managed.Archive)
				continue;

			ConVar convar = (ConVar)managed.Command;
			if (convar.IsFlagSet(FCvar.LuaClient))
				ClientCVars!.SetString(convar.GetName(), GetSavedValue(convar));

			if (convar.IsFlagSet(FCvar.LuaServer))
				ServerCVars!.SetString(convar.GetName(), GetSavedValue(convar));
		}

		if (!ClientCVars!.IsEmpty())
			ClientCVars.WriteToFile(filesystem, "cfg/client.vdf", "MOD");

		if (!ServerCVars!.IsEmpty())
			ServerCVars.WriteToFile(filesystem, "cfg/server.vdf", "MOD");
	}

	static string GetSavedValue(ConVar convar) {
		if (convar.IsFlagSet(FCvar.NeverAsString))
			return ((float)convar.GetDouble()).ToString("F6", CultureInfo.InvariantCulture);

		return convar.GetString();
	}

	public void Init() {
		ClientCVars = new KeyValues("CVars");
		ServerCVars = new KeyValues("CVars");
		ClientCVars.LoadFromFile(filesystem, "cfg/client.vdf", "MOD");
		ServerCVars.LoadFromFile(filesystem, "cfg/server.vdf", "MOD");
	}
}
