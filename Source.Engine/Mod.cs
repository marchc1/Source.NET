using Microsoft.Extensions.DependencyInjection;

using Source.Common.Engine;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Engine;

public class Mod(bool serverOnly, IEngineAPI engineAPI)
{
	public bool IsServerOnly() => serverOnly;
	public ModResult Main() {
		ModResult res = ModResult.RunOK;
		IEngine eng = engineAPI.GetRequiredService<IEngine>();
		var host_parms = engineAPI.GetRequiredService<EngineParms>();
		SV SV = engineAPI.GetRequiredService<SV>();

		if (IsServerOnly()) {
			if (eng.Load(true, host_parms.BaseDir)) {
				dedicated.RunServer();
			}
		}
		else {
			eng.SetQuitting(IEngine.Quit.NotQuitting);

			if (eng.Load(false, host_parms.BaseDir)) {
#if !SWDS
				if (((IClientLauncherAPI)engineAPI).MainLoop())
					res = ModResult.RunRestart;

				eng.Unload();
#endif

				SV.ShutdownGameDLL();
			}
		}

		return res;
	}
}
