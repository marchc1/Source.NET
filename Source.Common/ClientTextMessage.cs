using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Common;

public struct ClientTextMessage
{
	public int Effect;
	public byte R1, G1, B1, A1;       
	public byte R2, G2, B2, A2;
	public float X;
	public float Y;
	public TimeUnit_t FadeIn;
	public TimeUnit_t FadeOut;
	public TimeUnit_t HoldTime;
	public TimeUnit_t FxTime;
	public string VGuiSchemeFontName; 
	public string Name;
	public string Message;
	public bool RoundedRectBackdropBox;
	public float BoxSize; 
	public Color BoxColor;
	public string clearMessage;
}
