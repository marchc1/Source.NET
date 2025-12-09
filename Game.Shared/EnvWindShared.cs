using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Shared;

public class EnvWindShared
{
	public float StartTime;
	public int WindSeed;
	public int MinWind;
	public int MaxWind;
	public int MinGust;
	public int MaxGust;
	public float MinGustDelay;
	public float MaxGustDelay;
	public float GustDuration;
	public int GustDirChange;
	public int GustSound;
	public int WindDir;
	public float WindSpeed;
	public int InitialWindDir;
	public float InitialWindSpeed;

	#if !CLIENT_DLL
		// todo: onguststart/ongustend
	#endif
}
