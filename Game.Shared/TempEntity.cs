using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Shared;

public enum TempEntExplFlags
{
	None = 0x0,
	NoAdditive = 0x1,
	NoDLights = 0x2,
	NoSound = 0x4,
	NoParticles = 0x8,
	DrawAlpha = 0x10,
	Rotate = 0x20,
	NoFireball = 0x40,
	NoFireballSmoke = 0x80,
}

public enum TempEntType
{
	BeamPoints = 0,
	Sprite = 1,
	BeamDisk = 2,
	BeamCylinder = 3,
	BeamFollow = 4,
	BeamRing = 5,
	BeamSpline = 6,
	BeamRingPoint = 7,
	BeamLaser = 8,
	BeamTesla = 9,
}
