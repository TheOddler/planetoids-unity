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
using System.Linq;
using ScriptInspector;


public class FGTooltip : FGPopupWindow
{
	public string text { get; private set; }

	private FGTextEditor textEditor;

	public SyntaxToken tokenAtMouse;

	private SymbolDefinition[] overloads;
	private int currentOverload;

	private GUIStyle normalStyle;
	//private GUIStyle backgroundStyle;


	public static FGTooltip Create(FGTextEditor editor, Rect tokenRect, ParseTree.Leaf leaf, bool horizontal = false)
	{
		string tooltipText = null;
		var symbolDefinition = leaf.resolvedSymbol;
		SymbolDefinition[] overloads = null;
		int currentOverload = 0;
		if (symbolDefinition != null)
		{
			try
			{
				//Debug.Log("Creating tooltip: " + symbolDefinition.GetTooltipText());
				
				ConstructedTypeDefinition constructedType = symbolDefinition.parentSymbol as ConstructedTypeDefinition;

				if (symbolDefinition.kind == SymbolKind.MethodGroup)
				{
					var group = symbolDefinition as MethodGroupDefinition;
					if (group == null)
					{
						var constructedGroup = symbolDefinition as ConstructedSymbolDefinition;
						if (constructedGroup != null)
						{
							var genericGroup = constructedGroup.genericSymbol as MethodGroupDefinition; 
							if (constructedType != null)
								symbolDefinition = constructedType.GetConstructedMember(genericGroup.methods.FirstOrDefault());
						}
					}
					if (group != null && group.methods != null)
						symbolDefinition = group.methods.FirstOrDefault() ?? symbolDefinition;
					//else
					//	Debug.Log("Can't convert to MethodGroupDefinition. " + symbolDefinition.GetTooltipText());
				}
				
				tooltipText = symbolDefinition.GetTooltipText();
				//tooltipText += symbolDefinition.IsValid();

				var methodGroup = symbolDefinition;
				if (methodGroup.parentSymbol != null)
				{
					if (methodGroup.parentSymbol.kind == SymbolKind.MethodGroup)
					{
						methodGroup = methodGroup.parentSymbol;
					}
					else
					{
						if (constructedType != null)
						{
							var constructedMethod = methodGroup as ConstructedSymbolDefinition;
							methodGroup = constructedMethod != null ?
								constructedMethod.genericSymbol.parentSymbol as MethodGroupDefinition : null;
						}
					}
				}
				if (methodGroup != null && methodGroup.kind == SymbolKind.MethodGroup)
				{
					var group = methodGroup as MethodGroupDefinition;
					if (group != null && group.methods.Count > 1)
					{
						var methodOverloads = new MethodDefinition[group.methods.Count];
						group.methods.CopyTo(methodOverloads);
						if (constructedType != null)
						{
							overloads = new SymbolDefinition[methodOverloads.Length];
							for (int i = 0; i < overloads.Length; ++i)
								overloads[i] = constructedType.GetConstructedMember(methodOverloads[i]);
						}
						else
						{
							overloads = methodOverloads;
						}
						currentOverload = Mathf.Clamp(System.Array.IndexOf(overloads, symbolDefinition), 0, overloads.Length - 1);
					}
					//else if (group == null)
					//	Debug.Log("Can't convert to MethodGroupDefinition. " + symbolDefinition);
				}
				//else if (methodGroup != null)
				//	Debug.Log("symbolDefinition: " + symbolDefinition.GetType());
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
				tooltipText = e.ToString();
			}
		}
		if (leaf.syntaxError != null)
			tooltipText = leaf.syntaxError;
		else if (leaf.semanticError != null)
			tooltipText = leaf.semanticError + "\n\n" + tooltipText;
		
		if (string.IsNullOrEmpty(tooltipText))
			return null;
		
		Rect position = new Rect(tokenRect.x, tokenRect.yMax, 1f, 1f);
		
		var owner = EditorWindow.focusedWindow;

		FGTooltip window = CreateInstance<FGTooltip>();
		window.dropDownRect = tokenRect;
		window.horizontal = horizontal;
		window.hideFlags = HideFlags.HideAndDontSave;
		
		window.textEditor = editor;
		window.title = string.Empty;
		window.minSize = Vector2.one;
		window.owner = owner;
		window.tokenAtMouse = null;
		//window.symbolDefinition = symbolDefinition;
		window.text = tooltipText;
		window.overloads = overloads;
		window.currentOverload = currentOverload;

		//window.normalStyle = new GUIStyle(editor.styles.normalStyle);
		//window.normalStyle.wordWrap = true;
	//	window.normalStyle.fixedWidth = 300f;
	//	window.normalStyle.stretchHeight = true;
		window.normalStyle = editor.styles.tooltipTextStyle;
		window.normalStyle.font = EditorStyles.standardFont;
		//window.backgroundStyle = new GUIStyle(editor.styles.normalStyle);
		//if (window.backgroundStyle.normal.background)

		window.position = position;
		window.ShowPopup();
		//window.SetPosition(position);

		//if (window.owner != null)
		//	window.owner.Focus();
		return window;
	}
	
	public void Hide()
	{
		overloads = null;
		currentOverload = 0;
		text = null;
		textEditor.mouseHoverTime = 0f;
		textEditor.mouseHoverToken = null;
		textEditor.tokenTooltip = null;
		Close();
	}

	public void OnGUI()
	{
		if (Event.current.type == EventType.MouseMove ||
			Event.current.type == EventType.ScrollWheel ||
			Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
		{
			Hide();
			return;
		}
		if (this.text == null)
			return;

		var text = this.text;
		if (overloads != null && overloads.Length > 1)
		{
			if (horizontal)
				text = "\u25c0" + (currentOverload + 1) + " of " + overloads.Length + "\u25b6 " + text;
			else
				text = "\u25b2" + (currentOverload + 1) + " of " + overloads.Length + "\u25bc " + text;
		}

		wantsMouseMove = true;

		//if (focusedWindow == this && owner != null)
		//{
		//	owner.Focus();
		////	GUIUtility.ExitGUI();
		//}
		
		if (Event.current.type == EventType.layout)
		{
			var content = new GUIContent(text);
			normalStyle.fixedWidth = 0f;
			Vector2 size = normalStyle.font != null ? normalStyle.CalcSize(content) : Vector2.zero;
			if (size.x > 500f)
			{
				size.x = 500f;
				size.y = normalStyle.CalcHeight(content, size.x);
				normalStyle.fixedWidth = size.x;
				//size = normalStyle.CalcSize(content);
			}
			SetSize(size.x + 10f, size.y + 10f);
			return;
		}

		GUI.Box(new Rect(0f, 0f, position.width, position.height), GUIContent.none, textEditor.styles.tooltipFrameStyle);
		GUI.Box(new Rect(1f, 1f, position.width - 2, position.height - 2), GUIContent.none, textEditor.styles.tooltipBgStyle);

	//	if (Event.current.type == EventType.Repaint)
		{
			GUI.Label(new Rect(4f, 4f, position.width - 4f, position.height - 4), text, normalStyle);
		}
	}
	
	public void OnOwnerGUI()
	{
		if (Event.current.type == EventType.Layout)
		{
			var mousePos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
			//Debug.Log(dropDownRect + "\n" + mousePos);
			if (!dropDownRect.Contains(mousePos))
			{
				Hide();
				return;
			}
		}
		
		if (Event.current.type == EventType.ScrollWheel)
		{
			Hide();
			return;
		}
		
	    if (Event.current.type == EventType.KeyDown)
	    {
			if (!(Event.current.alt || Event.current.command || Event.current.control || Event.current.shift))
			{
				if (overloads != null && overloads.Length > 1)
				{
					if (Event.current.keyCode == KeyCode.UpArrow || Event.current.keyCode == KeyCode.DownArrow)
					{
						Event.current.Use();
						currentOverload = (currentOverload + overloads.Length + (Event.current.keyCode == KeyCode.DownArrow ? 1 : -1)) % overloads.Length;
						text = overloads[currentOverload].GetTooltipText();
						//text += overloads[currentOverload].IsValid();
						RepaintOnUpdate();
						return;
					}
				}

				if (Event.current.keyCode == KeyCode.Escape)
				{
					Event.current.Use();
				}
			}
			
			Hide();
		}
	}
	
	private void RepaintOnUpdate()
	{
		EditorApplication.update += DelayedRepaint;
	}
	
	private void DelayedRepaint()
	{
		EditorApplication.update -= DelayedRepaint;
		Repaint();
	}
}
