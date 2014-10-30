/* SCRIPT INSPECTOR 3
 * version 3.0 Beta 3, August 2014
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
using System.Collections.Generic;
using System.IO;


namespace ScriptInspector
{
	
public static class FGFindInFiles
{
	static List<string> assets;
	static string[] FindAllAssets(string searchPattern)
	{
		return Directory.GetFiles("Assets", searchPattern, SearchOption.AllDirectories);
	}
	
	//[UnityEditor.MenuItem("Test/Directories")]
	//static void TestDirectories()
	//{
	//	var symbol = ReflectedTypeReference.ForType(typeof(TangoColors)).definition;
	//	var candidates = FindDefinitionCandidates(symbol);
	//	foreach (var c in candidates)
	//	{
	//		var asset = AssetDatabase.LoadAssetAtPath(c, typeof(TextAsset)) as TextAsset;
	//		if (!asset)
	//			continue;
	//		var buffer = FGTextBufferManager.GetBuffer(asset);
	//		buffer.Initialize();
	//		while (buffer.IsLoading)
	//			buffer.ProgressiveLoadOnUpdate();
	//		buffer.ProgressiveLoadOnUpdate();
	//	}
	//	var ns = symbol.Assembly.FindNamespace(symbol.parentSymbol.FullName);
	//	Debug.Log(symbol.parentSymbol.FullName + ": " + ns);
	//	var tp = symbol.GetTypeParameters();
	//	var newSymbol = ns.FindName(symbol.name, tp != null ? tp.Count : 0, true);
	//	var scope = newSymbol.declarations[0].scope;
	//	while (scope.parentScope != null)
	//		scope = scope.parentScope;
	//	var cuScope = scope as CompilationUnitScope;
	//	var guid = AssetDatabase.AssetPathToGUID(cuScope.path);
	//	var line = newSymbol.declarations[0].parseTreeNode.GetFirstLeaf().line;
	//	FGCodeWindow.OpenAssetInTab(guid, line + 1);
	//}
	
	public static List<SymbolDeclaration> FindDeclarations(SymbolDefinition symbol)
	{
		var candidates = FindDefinitionCandidates(symbol);
		foreach (var c in candidates)
		{
			var asset = AssetDatabase.LoadAssetAtPath(c, typeof(TextAsset)) as TextAsset;
			if (!asset)
				continue;
			var buffer = FGTextBufferManager.GetBuffer(asset);
			buffer.Initialize();
			while (buffer.IsLoading)
				buffer.ProgressiveLoadOnUpdate();
			buffer.ProgressiveLoadOnUpdate();
		}
		
		var newSymbol = FindNewSymbol(symbol);
		return newSymbol == null ? null : newSymbol.declarations;
	}
	
	static SymbolDefinition FindNewSymbol(SymbolDefinition symbol)
	{
		if (symbol.kind == SymbolKind.Namespace)
			return symbol.Assembly.FindNamespace(symbol.FullName);
		
		if (symbol.parentSymbol == null)
			return symbol;
		
		var newParent = FindNewSymbol(symbol.parentSymbol);
		if (newParent == null)
			return null;
		
		var tp = symbol.GetTypeParameters();
		var numTypeParams = tp != null ? tp.Count : 0;
		var symbolIsType = symbol.kind == SymbolKind.Class ||
			symbol.kind == SymbolKind.Struct || symbol.kind == SymbolKind.Interface ||
			symbol.kind == SymbolKind.Delegate || symbol.kind == SymbolKind.Enum;
		var newSymbol = newParent.FindName(symbol.name, numTypeParams, symbolIsType);
		if (newSymbol == null)
		{
			if (newParent.kind == SymbolKind.MethodGroup)
			{
				var mg = newParent as MethodGroupDefinition;
				if (mg == null)
				{
					var generic = newParent.GetGenericSymbol();
					if (generic != null)
						mg = generic as MethodGroupDefinition;
				}
				//if (mg != null)
				//	newSymbol.PrintParameters(
			}
			Debug.Log(symbol.GetTooltipText() + " not found in " + newParent.GetTooltipText());
			return null;
		}
		return newSymbol;
	}
	
	static List<string> FindDefinitionCandidates(SymbolDefinition symbol)
	{
		var result = new List<string>();
		if (assets != null)
			assets.Clear();
		
		var symbolType = symbol;
		if (symbol.kind == SymbolKind.Namespace)
			return result;
		
		while (symbolType != null &&
			symbolType.kind != SymbolKind.Class && symbolType.kind != SymbolKind.Struct &&
			symbolType.kind != SymbolKind.Enum && symbolType.kind != SymbolKind.Delegate)
		{
			symbolType = symbolType.parentSymbol;
		}
		
		var assembly = symbolType.Assembly;
		var assemblyId = assembly.assemblyId;
		FindAllAssemblyScripts(assemblyId);
		//assets.RemoveAll(x => FGTextBufferManager.TryGetBuffer(x) != null);
		
		string keyword;
		string typeName = symbolType.name;
		switch (symbolType.kind)
		{
			case SymbolKind.Class: keyword = "class"; break;
			case SymbolKind.Struct: keyword = "struct"; break;
			case SymbolKind.Interface: keyword = "interface"; break;
			case SymbolKind.Enum: keyword = "enum"; break;
			case SymbolKind.Delegate: keyword = "delegate"; break;
			default: return result;
		}
		
		for (int i = assets.Count; i --> 0; )
			if (ContainsWordsSequence(assets[i], keyword, typeName))
				result.Add(assets[i]);
		
		return result;
	}
	
	static bool ContainsWordsSequence(string assetPath, params string[] words)
	{
		try
		{
			var lines = File.ReadAllLines(assetPath);
			var l = 0;
			var w = 0;
			var s = 0;
			while (l < lines.Length)
			{
				if (s >= lines[l].Length - words[0].Length)
				{
					s = 0;
					++l;
					continue;
				}
				
				s = lines[l].IndexOf(words[0], s);
				if (s < 0)
				{
					s = 0;
					++l;
					continue;
				}
				
				if (s > 0)
				{
					var c = lines[l][s - 1];
					if (c == '_' || char.IsLetterOrDigit(c))
					{
						s += words[0].Length;
						continue;
					}
				}
				
				s += words[0].Length;
				if (s < lines[l].Length)
				{
					var c = lines[l][s];
					s++;
					if (c != ' ' && c != '\t')
						continue;
				}
				else
				{
					s = 0;
					++l;
					if (l == lines.Length)
						break;
				}
				
				w = 1;
				while (w < words.Length)
				{
					// Skip additional whitespaces
					while (s < lines[l].Length)
					{
						var c = lines[l][s];
						if (c == ' ' || c == '\t')
							++s;
						else
							break;
					}
					
					if (s == lines[l].Length)
					{
						s = 0;
						++l;
						continue;
					}
					
					if (!lines[l].Substring(s).StartsWith(words[w]))
					{
						w = 0;
						break;
					}
					
					s += words[w].Length;
					if (s < lines[l].Length)
					{
						var c = lines[l][s];
						if (c == '_' || char.IsLetterOrDigit(c))
						{
							w = 0;
							break;
						}
					}
					
					++w;
				}
				
				if (w == words.Length)
				{
					return true;
				}
			}
		}
		catch (IOException e)
		{
			Debug.LogError(e);
		}
		return false;
	}
	
	static void FindAllAssemblyScripts(AssemblyDefinition.UnityAssembly assemblyId)
	{
		var editor = false;
		var firstPass = false;
		var pattern = "";
		
		switch (assemblyId)
		{
		case AssemblyDefinition.UnityAssembly.CSharpFirstPass:
		case AssemblyDefinition.UnityAssembly.UnityScriptFirstPass:
		case AssemblyDefinition.UnityAssembly.BooFirstPass:
		case AssemblyDefinition.UnityAssembly.CSharpEditorFirstPass:
		case AssemblyDefinition.UnityAssembly.UnityScriptEditorFirstPass:
		case AssemblyDefinition.UnityAssembly.BooEditorFirstPass:
			firstPass = true;
			break;
		}
		
		switch (assemblyId)
		{
		case AssemblyDefinition.UnityAssembly.CSharpFirstPass:
		case AssemblyDefinition.UnityAssembly.CSharpEditorFirstPass:
		case AssemblyDefinition.UnityAssembly.CSharp:
		case AssemblyDefinition.UnityAssembly.CSharpEditor:
			pattern = "*.cs";
			break;
		case AssemblyDefinition.UnityAssembly.UnityScriptFirstPass:
		case AssemblyDefinition.UnityAssembly.UnityScriptEditorFirstPass:
		case AssemblyDefinition.UnityAssembly.UnityScript:
		case AssemblyDefinition.UnityAssembly.UnityScriptEditor:
			pattern = "*.js";
			break;
		case AssemblyDefinition.UnityAssembly.BooFirstPass:
		case AssemblyDefinition.UnityAssembly.BooEditorFirstPass:
		case AssemblyDefinition.UnityAssembly.Boo:
		case AssemblyDefinition.UnityAssembly.BooEditor:
			pattern = "*.boo";
			break;
		}
		
		switch (assemblyId)
		{
		case AssemblyDefinition.UnityAssembly.CSharpEditorFirstPass:
		case AssemblyDefinition.UnityAssembly.UnityScriptEditorFirstPass:
		case AssemblyDefinition.UnityAssembly.BooEditorFirstPass:
		case AssemblyDefinition.UnityAssembly.CSharpEditor:
		case AssemblyDefinition.UnityAssembly.UnityScriptEditor:
		case AssemblyDefinition.UnityAssembly.BooEditor:
			editor = true;
			break;
		}
		
		var scripts = Directory.GetFiles("Assets", pattern, SearchOption.AllDirectories);
		var count = scripts.Length;
		
		if (assets == null)
			assets = new List<string>(count);
		
		for (var i = count; i --> 0; )
		{
			var path = scripts[i];
			scripts[i] = path = path.Replace('\\', '/');
			path = path.ToLowerInvariant();
			
			if (path.Contains("/.") || path.StartsWith("assets/webplayertemplates/"))
			{
				scripts[i] = scripts[--count];
				continue;
			}
			
			var isFirstPass = path.StartsWith("assets/standard assets/") ||
				path.StartsWith("assets/pro standard assets/") ||
				path.StartsWith("assets/plugins/");
			if (firstPass != isFirstPass)
			{
				scripts[i] = scripts[--count];
				continue;
			}
			
			var isEditor = false;
			if (path.StartsWith("assets/plugins/"))
				isEditor = path.StartsWith("assets/plugins/editor/");
			else
				isEditor = path.Contains("/editor/");
			if (editor != isEditor)
			{
				scripts[i] = scripts[--count];
				continue;
			}
			
			assets.Add(path);
		}
		//var joined = string.Join(", ", scripts, 0, count);
		//Debug.Log(joined);
	}
}
	
}