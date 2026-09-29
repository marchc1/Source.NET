using Source.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Shared;

public class EnvWindShared
{
	[NetworkName("m_flStartTime")]
	public float StartTime;
	[NetworkName("m_iWindSeed")]
	public int WindSeed;
	[NetworkName("m_iMinWind")]
	public int MinWind;
	[NetworkName("m_iMaxWind")]
	public int MaxWind;
	[NetworkName("m_iMinGust")]
	public int MinGust;
	[NetworkName("m_iMaxGust")]
	public int MaxGust;
	[NetworkName("m_flMinGustDelay")]
	public float MinGustDelay;
	[NetworkName("m_flMaxGustDelay")]
	public float MaxGustDelay;
	[NetworkName("m_flGustDuration")]
	public float GustDuration;
	[NetworkName("m_iGustDirChange")]
	public int GustDirChange;
	public int GustSound;
	public int WindDir;
	public float WindSpeed;
	[NetworkName("m_iInitialWindDir")]
	public int InitialWindDir;
	[NetworkName("m_flInitialWindSpeed")]
	public float InitialWindSpeed;
	[NetworkName("m_windRadius")]
	public int WindRadius;

	#if !CLIENT_DLL
		// todo: onguststart/ongustend
	#endif
}
