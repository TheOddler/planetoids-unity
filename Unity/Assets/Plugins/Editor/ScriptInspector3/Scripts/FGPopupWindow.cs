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


namespace ScriptInspector
{

public class FGPopupWindow : EditorWindow
{
	protected EditorWindow owner;
	
	protected Rect dropDownRect;
	protected bool horizontal;
	private bool flipped;
	
	private static System.Type containerWindowType;
	private static System.Reflection.MethodInfo fitToScreenMethod;
	private static System.Reflection.FieldInfo parentField;
	private static System.Reflection.PropertyInfo windowProperty;
	private static Rect FitRectToScreen(Rect rc, EditorWindow window)
	{
		const System.Reflection.BindingFlags instanceFlags =
			System.Reflection.BindingFlags.Public |
			System.Reflection.BindingFlags.NonPublic |
			System.Reflection.BindingFlags.Instance;
		
		if (containerWindowType == null && parentField == null)
		{
			containerWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ContainerWindow");
			if (containerWindowType != null)
				fitToScreenMethod = containerWindowType.GetMethod("FitWindowRectToScreen", instanceFlags);
			
			parentField = typeof(EditorWindow).GetField("m_Parent", instanceFlags);
			if (parentField != null)
			{
				var viewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.View");
				windowProperty = viewType.GetProperty("window", instanceFlags);
			}
		}
		
		if (fitToScreenMethod == null || windowProperty == null)
			return rc;
		
		var hostView = parentField.GetValue(window);
		if (hostView == null)
			return rc;
		var container = windowProperty.GetValue(hostView, null);
		if (container == null)
			return rc;
		
		rc.height += 20f;
		rc = (Rect) fitToScreenMethod.Invoke(container, new object[] {rc, true, false});
		rc.height -= 20f;
		return rc;
	}
	
	protected void SetSize(float width, float height)
	{
		var x = horizontal ? (flipped ? dropDownRect.x - width : dropDownRect.xMax) : dropDownRect.x;
		var y = horizontal ? dropDownRect.y : (flipped ? dropDownRect.y - height : dropDownRect.yMax);
		var rc = new Rect(x, y, width, height);
		var fit = FitRectToScreen(rc, this);
		
		if (!flipped)
		{
			flipped = horizontal ? rc.x != fit.x : rc.y != fit.y;
			if (flipped)
			{
				x = horizontal ? dropDownRect.x - width : dropDownRect.x;
				y = horizontal ? dropDownRect.y : dropDownRect.y - height;
				rc = new Rect(x, y, width, height);
				fit = FitRectToScreen(rc, owner);
			}
		}
		
		minSize = Vector2.one;
		maxSize = new Vector2(4000f, 4000f);
		position = fit;
		maxSize = minSize = new Vector2(width, height);
	}
}

}
