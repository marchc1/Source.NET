using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Common.Utilities;

public delegate void OnEnterFn(ReadOnlySpan<char> text);

public class UserConsoleInput
{
	readonly char[] consoleText = new char[2048];
	int consoleTextLen;
	int cursorPosition;

	public event OnEnterFn? OnEnter;

	public void RunFrame() {
		while (!Console.IsInputRedirected && Console.KeyAvailable) {
			var key = Console.ReadKey(true);
			switch (key.Key) {
				case ConsoleKey.UpArrow:
					ReceiveUpArrow();
					break;
				case ConsoleKey.DownArrow:
					ReceiveDownArrow();
					break;
				case ConsoleKey.LeftArrow:
					ReceiveLeftArrow();
					break;
				case ConsoleKey.RightArrow:
					ReceiveRightArrow();
					break;
				case ConsoleKey.Enter:
					ReadOnlySpan<char> line = ReceiveNewLine();
					if (line.Length > 0)
						OnEnter?.Invoke(line);
					break;
				case ConsoleKey.Backspace:
					ReceiveBackspace();
					break;
				case ConsoleKey.Tab:
					ReceiveTab();
					break;
				default:
					char ch = key.KeyChar;
					if (ch >= ' ' && ch <= '~')
						ReceiveStandardChar(ch);
					break;
			}
		}
	}

	private void ReceiveUpArrow() {

	}

	private void ReceiveDownArrow() {

	}

	private void ReceiveLeftArrow() {
		if (cursorPosition <= 0)
			return;
		Console.Write('\b');
		cursorPosition--;
	}

	private void ReceiveRightArrow() {
		if (cursorPosition >= consoleTextLen)
			return;
		Console.Write(consoleText[cursorPosition]);
		cursorPosition++;
	}

	private void ReceiveTab() {

	}

	private void ReceiveBackspace() {
		int count;
		if (cursorPosition <= 0)
			return;
		consoleTextLen--;
		cursorPosition--;

		Console.Write('\b');
		for (count = cursorPosition; count < consoleTextLen; count++) {
			consoleText[count] = consoleText[count + 1];
			Console.Write(consoleText[count]);
		}

		Console.Write(' ');
		count = consoleTextLen;
		while (count >= cursorPosition) {
			Console.Write('\b');
			count--;
		}
	}

	private ReadOnlySpan<char> ReceiveNewLine() {
		Console.WriteLine();
		int len = 0;
		if (consoleTextLen > 0) {
			len = consoleTextLen;
			consoleTextLen = 0;
			cursorPosition = 0;
			return consoleText.AsSpan()[..len];
		}
		else
			return null;
	}

	private void ReceiveStandardChar(char ch) {
		int count;
		if (consoleTextLen >= (consoleText.Length - 2))
			return;

		count = consoleTextLen;
		while (count > cursorPosition) {
			consoleText[count] = consoleText[count - 1];
			count--;
		}

		consoleText[cursorPosition] = ch;

		Console.Write(new string(new ReadOnlySpan<char>(consoleText))[cursorPosition..(cursorPosition + (consoleTextLen - cursorPosition + 1))]);
		consoleTextLen++;
		cursorPosition++;
		count = consoleTextLen;
		while (count > cursorPosition) {
			Console.Write('\b');
			count--;
		}
	}
}
