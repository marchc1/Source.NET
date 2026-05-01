using Source.Common.Audio;
using Source.Common.SoundEmitterSystem;
using Source.Common.Utilities;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.SoundEmitterSystem;

public class SoundEmitterSystemBase : ISoundEmitterSystemBase
{
	public bool AddSound(ReadOnlySpan<char> soundname, ReadOnlySpan<char> scriptfile, in SoundParametersInternal parms) {
		throw new NotImplementedException();
	}

	public void AddSoundOverrides(ReadOnlySpan<char> scriptfile, bool preload = false) {
		throw new NotImplementedException();
	}

	public UtlSymbol AddWaveName(ReadOnlySpan<char> name) {
		throw new NotImplementedException();
	}

	public int CheckForMissingWavFiles(bool verbose) {
		throw new NotImplementedException();
	}

	public void ClearSoundOverrides() {
		throw new NotImplementedException();
	}

	public void ExpandSoundNameMacros(in SoundParametersInternal parms, ReadOnlySpan<char> wavename) {
		throw new NotImplementedException();
	}

	public int FindSoundScript(ReadOnlySpan<char> name) {
		throw new NotImplementedException();
	}

	public int First() {
		throw new NotImplementedException();
	}

	public void Flush() {
		throw new NotImplementedException();
	}

	public void GenderExpandString(ReadOnlySpan<char> actormodel, ReadOnlySpan<char> inText, Span<char> outText) {
		throw new NotImplementedException();
	}

	public void GenderExpandString(Gender gender, ReadOnlySpan<char> inText, Span<char> outText) {
		throw new NotImplementedException();
	}

	public Gender GetActorGender(ReadOnlySpan<char> actormodel) {
		throw new NotImplementedException();
	}

	public uint GetManifestFileTimeChecksum() {
		throw new NotImplementedException();
	}

	public int GetNumSoundScripts() {
		throw new NotImplementedException();
	}

	public bool GetParametersForSound(ReadOnlySpan<char> soundname, SoundParameters parms, Gender gender, bool isbeingemitted = false) {
		throw new NotImplementedException();
	}

	public bool GetParametersForSoundEx(ReadOnlySpan<char> soundname, ref short handle, ref SoundParameters parms, Gender gender, bool isbeingemitted = false) {
		throw new NotImplementedException();
	}

	public int GetSoundCount() {
		throw new NotImplementedException();
	}

	public int GetSoundIndex(ReadOnlySpan<char> name) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetSoundName(int index) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetSoundScriptName(int index) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetSourceFileForSound(int index) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetWaveName(out UtlSymbol sym) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetWavFileForSound(ReadOnlySpan<char> soundname, ReadOnlySpan<char> actormodel) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetWavFileForSound(ReadOnlySpan<char> soundname, Gender gender) {
		throw new NotImplementedException();
	}

	public ref SoundParametersInternal InternalGetParametersForSound(int index) {
		throw new NotImplementedException();
	}

	public int InvalidIndex() {
		throw new NotImplementedException();
	}

	public bool IsSoundScriptDirty(int index) {
		throw new NotImplementedException();
	}

	public bool IsUsingGenderToken(ReadOnlySpan<char> soundname) {
		throw new NotImplementedException();
	}

	public bool IsValidIndex(int index) {
		throw new NotImplementedException();
	}

	public SoundLevel LookupSoundLevel(ReadOnlySpan<char> soundname) {
		// throw new NotImplementedException();
		Console.WriteLine($"LookupSoundLevel not implemented {soundname}");
		return SoundLevel.LvlNorm;
	}

	public SoundLevel LookupSoundLevelByHandle(ReadOnlySpan<char> soundname, ref short handle) {
		throw new NotImplementedException();
	}

	public bool ModInit() {
		throw new NotImplementedException();
	}

	public void ModShutdown() {
		throw new NotImplementedException();
	}

	public void MoveSound(ReadOnlySpan<char> soundname, ReadOnlySpan<char> newscript) {
		throw new NotImplementedException();
	}

	public int Next(int i) {
		throw new NotImplementedException();
	}

	public void RemoveSound(ReadOnlySpan<char> soundname) {
		throw new NotImplementedException();
	}

	public void RenameSound(ReadOnlySpan<char> soundname, ReadOnlySpan<char> newname) {
		throw new NotImplementedException();
	}

	public void SaveChangesToSoundScript(int scriptindex) {
		throw new NotImplementedException();
	}

	public void UpdateSoundParameters(ReadOnlySpan<char> soundname, in SoundParametersInternal parms) {
		throw new NotImplementedException();
	}
}
