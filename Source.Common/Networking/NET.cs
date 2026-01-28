using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Common.Networking;

public static class NET
{
	/// <summary>
	/// NOP command used for padding.
	/// </summary>
	public const int NOP = 0;
	/// <summary>
	/// Disconnect, last message in connection.
	/// </summary>
	public const int Disconnect = 1;
	/// <summary>
	/// File transmission message request/denial.
	/// </summary>
	public const int File = 2;
	/// <summary>
	/// Send the last world tick.
	/// </summary>
	public const int Tick = 3;
	/// <summary>
	/// A string command
	/// </summary>
	public const int StringCmd = 4;
	/// <summary>
	/// Sends one or more convar settings
	/// </summary>
	public const int SetConVar = 5;
	/// <summary>
	/// Signals current signon state.
	/// </summary>
	public const int SignOnState = 6;
}

public enum FragmentStream
{
	Normal,
	File,
	Max
}
