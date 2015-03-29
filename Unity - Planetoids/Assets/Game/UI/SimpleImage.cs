using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


[ExecuteInEditMode]
public class SimpleImage : Graphic 
{
	
	protected override void OnFillVBO (List<UIVertex> vbo)
	{
		Vector2 corner1 = Vector2.zero;
		Vector2 corner2 = Vector2.one;
		
		corner1.x -= rectTransform.pivot.x;
		corner1.y -= rectTransform.pivot.y;
		corner2.x -= rectTransform.pivot.x;
		corner2.y -= rectTransform.pivot.y;
		
		corner1.x *= rectTransform.rect.width;
		corner1.y *= rectTransform.rect.height;
		corner2.x *= rectTransform.rect.width;
		corner2.y *= rectTransform.rect.height;
		
		vbo.Clear();
		
		UIVertex vert = UIVertex.simpleVert;
		
		vert.position = new Vector2(corner1.x, corner1.y);
		vert.color = color;
		vbo.Add(vert);
		
		vert.position = new Vector2(corner1.x, corner2.y);
		vert.color = color;
		vbo.Add(vert);
		
		vert.position = new Vector2(corner2.x, corner2.y);
		vert.color = color;
		vbo.Add(vert);
		
		vert.position = new Vector2(corner2.x, corner1.y);
		vert.color = color;
		vbo.Add(vert);
	}
}
