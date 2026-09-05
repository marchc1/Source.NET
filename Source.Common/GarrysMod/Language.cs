using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Common.GarrysMod;

public interface Language
{
	void ChangeLanguage(ReadOnlySpan<char> unk1);
	void ChangeLanguage_Steam(ReadOnlySpan<char> unk1);
	void ReloadLanguage();
	void GetString(ReadOnlySpan<char> unk1, Span<char> unk2 /* unk3: likely unk2's size */);
	void UpdateSourceEngineLanguage();
}
