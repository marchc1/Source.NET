
using System.Numerics;

namespace Source.Common.Engine;

public ref struct SndInfo
{
	// Sound Guid
	public int Guid;
	public FileNameHandle_t FilenameHandle;      // filesystem filename handle - call IFilesystem to conver this to a string
	public int SoundSource;
	public int Channel;
	// If a sound is being played through a speaker entity (e.g., on a monitor,), this is the
	//  entity upon which to show the lips moving, if the sound has sentence data
	public int SpeakerEntity;
	public float Volume;
	public float LastSpatializedVolume;
	// Radius of this sound effect (spatialization is different within the radius)
	public float Radius;
	public int Pitch;
	public ref Vector3 Origin;
	public ref Vector3 Direction;

	// if true, assume sound source can move and update according to entity
	public bool UpdatePositions;
	// true if playing linked sentence
	public bool IsSentence;
	// if true, bypass all dsp processing for this sound (ie: music)	
	public bool DryMix;
	// true if sound is playing through in-game speaker entity.
	public bool Speaker;
	// true if sound is playing with special DSP effect
	public bool SpecialDSP;
	// for snd_show, networked sounds get colored differently than local sounds
	public bool FromServer;
}
