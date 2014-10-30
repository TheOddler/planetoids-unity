using UnityEngine;
using UnityEditor;


namespace ScriptInspector
{
	
public static class SISettings
{
	public static bool highlightCurrentLine;
	public static bool frameCurrentLine;
	public static float lineHighlightOpacity;
	public static bool showLineNumbers;
	public static bool showLineNumbersText;
	public static bool trackChangesCode;
	public static bool trackChangesText;
	public static bool wordWrapCode;
	public static bool wordWrapText;
	public static string editorFont;
	public static int fontSizeDelta;
	public static string themeName;
	public static string themeNameText;
	
	static SISettings()
	{
		editorFont = EditorPrefs.GetString("ScriptInspectorFont", FGTextEditor.availableFonts[2]);
		fontSizeDelta = EditorPrefs.GetInt("ScriptInspectorFontSize", 0);
		themeName = EditorPrefs.GetString("ScriptInspectorTheme", EditorGUIUtility.isProSkin ? "Darcula" : Application.platform == RuntimePlatform.OSXEditor ? "Xcode" : "Visual Studio");
		themeNameText = EditorPrefs.GetString("ScriptInspectorThemeText", null);
		
		highlightCurrentLine = EditorPrefs.GetBool("FlipbookGames.ScriptInspector.HighlightCurrentLine", true);
		showLineNumbers = EditorPrefs.GetBool("FlipbookGames.ScriptInspector.LineNumbers", true);
		trackChangesCode = EditorPrefs.GetBool("FlipbookGames.ScriptInspector.TrackChanges", true);
		trackChangesText = EditorPrefs.GetBool("FlipbookGames.ScriptInspector.TrackChangesText", true);
		wordWrapCode = EditorPrefs.GetBool("FlipbookGames.ScriptInspector.WordWrapCode", false);
		wordWrapText = EditorPrefs.GetBool("FlipbookGames.ScriptInspector.WordWrapText", true);
	}
	
	public static void SaveSettings()
	{
		EditorPrefs.SetString("ScriptInspectorFont", editorFont);
		EditorPrefs.SetInt("ScriptInspectorFontSize", fontSizeDelta);
		EditorPrefs.SetString("ScriptInspectorTheme", themeName);
		EditorPrefs.SetString("ScriptInspectorThemeText", themeNameText);
		
		EditorPrefs.SetBool("FlipbookGames.ScriptInspector.HighlightCurrentLine", highlightCurrentLine);
		EditorPrefs.SetBool("FlipbookGames.ScriptInspector.LineNumbers", showLineNumbers);
		EditorPrefs.SetBool("FlipbookGames.ScriptInspector.TrackChanges", trackChangesCode);
		EditorPrefs.SetBool("FlipbookGames.ScriptInspector.TrackChangesText", trackChangesText);
		EditorPrefs.SetBool("FlipbookGames.ScriptInspector.WordWrapCode", wordWrapCode);
		EditorPrefs.SetBool("FlipbookGames.ScriptInspector.WordWrapText", wordWrapText);
	}
	
	static readonly GUIContent[] modeToggles = new GUIContent[]
	{
		new GUIContent("View"),
		new GUIContent("Editor"),
		new GUIContent("More"),
	};
	
	static int mode;
	
	static class Styles
	{
		public static GUIStyle largeButton = "LargeButton";
	}
	
	[PreferenceItem("Script Inspector")]
	static void SettingsGUI()
	{
		mode = GUILayout.Toolbar(mode, modeToggles, Styles.largeButton);
		EditorGUILayout.Space();
		switch (mode)
		{
		case 0:
			ViewSettings();
			break;
		case 1:
			EditorSettings();
			break;
		case 2:
			MoreSettings();
			break;
		}
	}
	
	static void ViewSettings()
	{
		EditorGUI.BeginChangeCheck();
		showLineNumbers = EditorGUILayout.Toggle("Show line numbers", showLineNumbers);
		if (EditorGUI.EndChangeCheck())
		{
			SaveSettings();
		}
	}
	
	static void EditorSettings()
	{
		global::ScriptInspector.FGTextEditor te; 
		//ScriptInspector.FGTextEditor te1;
		var global = 6;
		//global::
		//	global::ScriptInspector.FGTextEditor te;
	}
	
	static void MoreSettings()
	{
	}
}
	
}