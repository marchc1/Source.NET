using Microsoft.Extensions.DependencyInjection;

using Source.Common.Client;
using Source.Common.Filesystem;
using Source.Common.Networking.DataTable;
using Source.Common.Server;
using Source.Engine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Server;

public static class GameInterface
{
	public static object? GetCommandClient(this Util Util) {
		int idx = Util.GetCommandClientIndex();
		if (idx > 0)
			return Util.PlayerByIndex(idx);

		return null;
	}

	public static int GetCommandClientIndex(this Util Util) {
		return 0;
	}

	public static object? PlayerByIndex(this Util Util, int idx) {
		return null;
	}
}

public class ServerGameDLL(IEngineServer engine, IFileSystem filesystem) : IServerGameDLL
{
	public static void DLLInit(IServiceCollection services) {
		
	}

	public void PostInit() {

	}

	public ServerClass? GetAllServerClasses()
	{
		return ServerClass.ServerClassHead;
	}
}
