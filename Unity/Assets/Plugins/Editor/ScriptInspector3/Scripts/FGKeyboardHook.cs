/* SCRIPT INSPECTOR 3
 * version 3.0 Beta 2, August 2014
 * Copyright © 2012-2014, Flipbook Games
 * 
 * Unity's legendary custom inspector for C#, UnityScript and Boo scripts,
 * now transformed into a powerful Script, Shader, and Text Editor!!!
 * 
 * Follow me on http://twitter.com/FlipbookGames
 * Like Flipbook Games on Facebook http://facebook.com/FlipbookGames
 * Join Unity forum discusion http://forum.unity3d.com/threads/138329
 * Contact info@flipbookgames.com for feedback, bug reports, or suggestions.
 * Visit http://flipbookgames.com/ for more info.
 */

using UnityEngine;
using UnityEditor;

using System;
using System.Runtime.InteropServices;


namespace ScriptInspector
{
	
	[InitializeOnLoad]
	internal static class FGKeyboardHook
	{
#if !UNITY_EDITOR_OSX
		const int WH_KEYBOARD = 2;
		
		const int VK_SHIFT = 0x10;
		const int VK_CONTROL = 0x11;
		
		static readonly KeyboardProc _proc = HookCallback;
		static IntPtr _hookID = IntPtr.Zero;
		
		delegate IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
		
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern IntPtr SetWindowsHookEx(int idHook, KeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
		
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool UnhookWindowsHookEx(IntPtr hhk);
		
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
		
		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
		static extern short GetKeyState(int keyCode);
		
		static FGKeyboardHook()
		{
			if (Application.platform == RuntimePlatform.WindowsEditor)
			{
				EditorApplication.update += SetHookOnFirstUpdate;
				EditorApplication.update += OnUpdate;
				AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
			}
		}
		
		static void SetHookOnFirstUpdate()
		{
			EditorApplication.update -= SetHookOnFirstUpdate;
			_hookID = SetHook(_proc);
		}
		
		static IntPtr SetHook(KeyboardProc proc)
		{
#pragma warning disable 618
			return SetWindowsHookEx(WH_KEYBOARD, proc, IntPtr.Zero, (uint) AppDomain.GetCurrentThreadId());
#pragma warning restore 618
		}
		
		static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
		{
			if (nCode >= 0)
			{
				if ((lParam.ToInt32() & 0xA0000000) == 0)
				{
					var wnd = EditorWindow.focusedWindow;
					if (wnd != null && FGTextBuffer.activeEditor != null &&
						FGTextBuffer.activeEditor.hasCodeViewFocus &&
						wnd == FGTextBuffer.activeEditor.OwnerWindow)
					{
						if ((GetKeyState(VK_CONTROL) & 0x8000) != 0)
						{
							var vkCode = wParam.ToInt32();
							if ((GetKeyState(VK_SHIFT) & 0x8000) == 0)
							{
								if (vkCode == 'S')
								{
								// Ctrl+S
									sendToWindow = wnd;
									delayedKeyEvent = Event.KeyboardEvent("^&s");
									return (IntPtr) 1;
								}
								if (vkCode == 'Z')
								{
								// Ctrl+Z
									sendToWindow = wnd;
									delayedKeyEvent = Event.KeyboardEvent("#^z");
									return (IntPtr) 1;
								}
							}
							else
							{
								if (vkCode == 'Z')
								{
								// Shift+Ctrl+Z
									sendToWindow = wnd;
									delayedKeyEvent = Event.KeyboardEvent("#^y");
									return (IntPtr) 1;
								}
							}
						}
					}
				}
			}
			return CallNextHookEx(_hookID, nCode, wParam, lParam);
		}
		
		static EditorWindow sendToWindow;
		static Event delayedKeyEvent;
		static void OnUpdate()
		{
			if (delayedKeyEvent != null)
			{
				sendToWindow.SendEvent(delayedKeyEvent);
				delayedKeyEvent = null;
			}
		}
		
		static void OnDomainUnload(object sender, EventArgs e)
		{
			if (_hookID != IntPtr.Zero)
				UnhookWindowsHookEx(_hookID);
			_hookID = IntPtr.Zero;
		}
#endif
	}
	
}
