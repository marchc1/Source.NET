global using static Game.Client.IVModeManager;

using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Client;

public interface IVModeManager
{
	public static IVModeManager modemanager = null!;

	void Init();
	void LevelInit(ReadOnlySpan<char> newmap);
	void LevelShutdown();
}
