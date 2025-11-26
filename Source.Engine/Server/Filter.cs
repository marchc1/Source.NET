using Source.Common.Commands;
using Source.Common.Server;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Engine.Server;

public struct IPFilter {
	public uint Mask;
	public uint Compare;
	public TimeUnit_t BanEndTime;
	public TimeUnit_t BanTime;
}

public struct UserFilter {
	public USERID UserID;
	public TimeUnit_t BanEndTime;
	public TimeUnit_t BanTime;
}

[EngineComponent]
public class Filter(Host Host)
{
	public const int MAX_IPFILTERS = 32768;
	public const int MAX_USERFILTERS	    = 32768;

	public const string BANNED_IP_FILENAME = "banned_ip.cfg";
	public const string BANNED_USER_FILENAME = "banned_user.cfg";
	public const string CONFIG_DIR = "cfg/";
	public const string STEAM_PREFIX = "STEAM_";
	
	static ConVar sv_filterban = new("sv_filterban", "1", 0, "Set packet filtering by IP mode");
	readonly List<IPFilter> IPFilters = [];
	readonly List<UserFilter> UserFilters = [];

	public bool IsUserBanned(USERID userid) {
		if (sv_filterban.GetInt() == 0)
			return false;

		bool negativeFilter = sv_filterban.GetInt() == 1;

		for (int i = UserFilters.Count - 1; i >= 0; i--) {
			if ((UserFilters[i].BanEndTime != 0.0) &&
				 (UserFilters[i].BanEndTime <= Host.RealTime)) {
				UserFilters.RemoveAt(i);
				continue;
			}

			if (Steam3Server().CompareUserID(userid, UserFilters[i].UserID)) 
				return negativeFilter;
		}

		return !negativeFilter;
	}
}
