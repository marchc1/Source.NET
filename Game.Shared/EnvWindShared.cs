using Source.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Shared;

public partial class EnvWindShared
{
	[NetworkName("m_flStartTime")]
	[NetworkVar] public partial float StartTime { get; set; }
	[NetworkName("m_iWindSeed")]
	[NetworkVar] public partial int WindSeed { get; set; }
	[NetworkName("m_iMinWind")]
	[NetworkVar] public partial int MinWind { get; set; }
	[NetworkName("m_iMaxWind")]
	[NetworkVar] public partial int MaxWind { get; set; }
	[NetworkName("m_iMinGust")]
	[NetworkVar] public partial int MinGust { get; set; }
	[NetworkName("m_iMaxGust")]
	[NetworkVar] public partial int MaxGust { get; set; }
	[NetworkName("m_flMinGustDelay")]
	[NetworkVar] public partial float MinGustDelay { get; set; }
	[NetworkName("m_flMaxGustDelay")]
	[NetworkVar] public partial float MaxGustDelay { get; set; }
	[NetworkName("m_flGustDuration")]
	[NetworkVar] public partial float GustDuration { get; set; }
	[NetworkName("m_iGustDirChange")]
	[NetworkVar] public partial int GustDirChange { get; set; }
	public int GustSound;
	public int WindDir;
	public float WindSpeed;
	[NetworkName("m_iInitialWindDir")]
	[NetworkVar] public partial int InitialWindDir { get; set; }
	[NetworkName("m_flInitialWindSpeed")]
	[NetworkVar] public partial float InitialWindSpeed { get; set; }
	[NetworkName("m_windRadius")]
	[NetworkVar] public partial int WindRadius { get; set; }

	#if !CLIENT_DLL
		// todo: onguststart/ongustend
	#endif
}
