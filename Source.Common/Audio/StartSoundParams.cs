using System.Numerics;

namespace Source.Common.Audio;
public enum SoundLevel
{
	LvlNone = 0,
	Lvl20dB = 20,   
	Lvl25dB = 25,   
	Lvl30dB = 30,   
	Lvl35dB = 35,
	Lvl40dB = 40,
	Lvl45dB = 45,   
	Lvl50dB = 50,   
	Lvl55dB = 55,   
	LvlIdle = 60,   
	Lvl60dB = 60,   
	Lvl65dB = 65,   
	LvlStatic = 66, 
	Lvl70dB = 70,   
	LvlNorm = 75,
	Lvl75dB = 75,   
	Lvl80dB = 80,  
	LvlTalking = 80,
	Lvl85dB = 85,  
	Lvl90dB = 90,  
	Lvl95dB = 95,
	Lvl100dB = 100,
	Lvl105dB = 105,
	Lvl110dB = 110,
	Lvl120dB = 120,
	Lvl130dB = 130,
	LvlGunfire = 140,  
	Lvl140dB = 140,
	Lvl150dB = 150,
	Lvl180dB = 180
}

public enum SoundFlags
{ 
	NoFlags = 0,                
	ChangeVolume = 1 << 0,      
	ChangePitch = 1 << 1,    
	Stop = 1 << 2,            
	Spawning = 1 << 3,        
	Delay = 1 << 4,           
	StopLooping = 1 << 5,    
	Speaker = 1 << 6,         
	ShouldPause = 1 << 7,     
	IgnorePhonemes = 1 << 8,
	IgnoreName = 1 << 9,     
	DoNotOverwriteExistingOnChannel = 1 << 10,
}

public enum SoundEntityChannel {
	Replace = -1,

	Auto = 0,
	Weapon = 1,
	Voice = 2,
	Item = 3,
	Body = 4,
	Stream = 5,   
	Static = 6,     
	Voice2 = 7,
	VoiceBase = 8, 

	UserBase = VoiceBase + 128     
}
