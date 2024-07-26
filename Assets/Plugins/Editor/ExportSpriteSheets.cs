// usage: right click on Atlas Texture / Atlas Sprite, Click "Export Sub-Sprites"

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEditor.U2D.Sprites;
using UnityEditor.U2D;

public class ExportSubSprites : Editor
{
	const string MENU_TITLE = "Assets/Export Sub-Sprites";

	[MenuItem(MENU_TITLE)]
	public static void DoExportSubSprites()
	{
		var folder = EditorUtility.OpenFolderPanel("Export Sub-Sprites into Which Folder?", "", "");

		int desWidth = 0;
		int desHeight = 0;

		foreach (var obj in Selection.objects)
		{
			if (obj is Texture2D)
			{
				var path = AssetDatabase.GetAssetPath(obj);
				Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
				foreach (var sprite in sprites)
				{
					var extracted = ExtractAndName(sprite, ref desWidth, ref desHeight);
					SaveSubSprite(extracted, folder);
				}
			}
			else
			{
				var sprite = obj as Sprite;
				if (sprite == null) continue;
				var extracted = ExtractAndName(sprite, ref desWidth, ref desHeight);
				SaveSubSprite(extracted, folder);
			}
		}
		AssetDatabase.Refresh();
		Debug.Log("Done Exporting Sub-Sprites!");
	}

	[MenuItem(MENU_TITLE, true)]
	private static bool CanExportSubSprites()
	{
		return Selection.activeObject is Sprite || Selection.activeObject is Texture2D;
	}

	// Determine whether point P in triangle ABC
	private static bool PointInTriangle(Vector2[] triangle, Vector2 P)
	{
		Debug.Assert(triangle.Length == 3);

		Vector2 v0 = triangle[2] - triangle[0];
		Vector2 v1 = triangle[1] - triangle[0];
		Vector2 v2 = P - triangle[0];

		float dot00 = Vector2.Dot(v0, v0);
		float dot01 = Vector2.Dot(v0, v1);
		float dot02 = Vector2.Dot(v0, v2);
		float dot11 = Vector2.Dot(v1, v1);
		float dot12 = Vector2.Dot(v1, v2);

		float inverDeno = 1 / (dot00 * dot11 - dot01 * dot01);

		float u = (dot11 * dot02 - dot01 * dot12) * inverDeno;
		if (u < 0 || u > 1) // if u out of range, return directly
		{
			return false;
		}

		float v = (dot00 * dot12 - dot01 * dot02) * inverDeno;
		if (v < 0 || v > 1) // if v out of range, return directly
		{
			return false;
		}

		return u + v <= 1;
	}

	// Since a sprite may exist anywhere on a tex2d, this will crop out the sprite's claimed region and return a new, cropped, tex2d.
	private static Texture2D ExtractAndName(Sprite sprite, ref int desWidth, ref int desHeight)
	{
		var texture = sprite.texture;

		var factory = new SpriteDataProviderFactories();
		factory.Init();
		var dataProvider = factory.GetSpriteEditorDataProviderFromObject(texture);
		dataProvider.InitSpriteEditorDataProvider();

		var guid = sprite.GetSpriteID();
		// Debug.Log("guid " + guid);

		var spriteRects = dataProvider.GetSpriteRects();
		// Loop over all Sprites and update the pivots
		Vector2 spritePivot = new Vector2(0.5f, 0.5f);

		if (desWidth <= 0 || desHeight <= 0)
		{
			var halfWidth = 0;
			var halfHeight = 0;
			foreach (var rect in spriteRects)
			{
				var pivot = rect.pivot;
				if (pivot.x < 0.5f) pivot.x = 1 - pivot.x;
				if (pivot.y < 0.5f) pivot.y = 1 - pivot.y;

				// Debug.Log("Sprite " + rect.name + " size " + rect.rect.width + " x " + rect.rect.height + " pivot " + pivot.x + " " + pivot.y);
				halfWidth = Mathf.Max(halfWidth, Mathf.RoundToInt(rect.rect.width * pivot.x));
				halfHeight = Mathf.Max(halfHeight, Mathf.RoundToInt(rect.rect.height * pivot.y));

				// for current sprite pivot
				if (rect.spriteID == guid)
				{
					spritePivot = rect.pivot;
				}
			}

			desWidth = halfWidth * 2;
			if (desWidth % 2 != 0) desWidth += 1;
			desHeight = halfHeight * 2;
			if (desHeight % 2 != 0) desHeight += 1;

			// Debug.Log("Desired size " + desWidth + " x " + desHeight);
		}
		else
		{
			foreach (var rect in spriteRects)
			{
				if (rect.spriteID == guid)
				{
					spritePivot = rect.pivot;
					break;
				}
			}
		}

		// custom outline seems use 0.5 instead of pivot
		Vector2 outlineCenter = new Vector2(sprite.rect.width * 0.5f, sprite.rect.height * 0.5f);

		/* Use the data provider */
		var outlineProvider = dataProvider.GetDataProvider<ISpriteOutlineDataProvider>();
		var outline = outlineProvider.GetOutlines(sprite.GetSpriteID());
		List<Vector2[]> normalizedOutline = new List<Vector2[]>();
		foreach (var o in outline)
		{
			List<Vector2> normalized = new List<Vector2>();
			foreach (var p in o)
			{
				normalized.Add(new Vector2(outlineCenter.x + p.x, outlineCenter.y + p.y));
			}
			normalizedOutline.Add(normalized.ToArray());
		}

		// Create a temporary RenderTexture of the same size as the texture
		RenderTexture tmp = RenderTexture.GetTemporary(
							texture.width,
							texture.height,
							0,
							RenderTextureFormat.Default,
							RenderTextureReadWrite.Linear);

		// Blit the pixels on texture to the RenderTexture
		Graphics.Blit(texture, tmp);

		// Backup the currently set RenderTexture
		RenderTexture previous = RenderTexture.active;

		// Set the current RenderTexture to the temporary one we created
		RenderTexture.active = tmp;

		// Create a new readable Texture2D to copy the pixels to it
		Texture2D cacheTexture2D = new Texture2D(texture.width, texture.height);

		// Copy the pixels from the RenderTexture to the new Texture
		cacheTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
		cacheTexture2D.Apply();

		// Reset the active RenderTexture
		RenderTexture.active = previous;

		// Release the temporary RenderTexture
		RenderTexture.ReleaseTemporary(tmp);

		// "cacheTexture2D" now has the same pixels from "texture" and it's re

		var output = new Texture2D(desWidth, desHeight, TextureFormat.RGBA32, false);
		output.SetPixels(Enumerable.Repeat(Color.clear, desWidth * desHeight).ToArray());
		var r = sprite.textureRect;

		var xOffset = Mathf.RoundToInt((desWidth / 2f) - (spritePivot.x * r.width));
		var yOffset = Mathf.RoundToInt((desHeight / 2f) - (spritePivot.y * r.height));

		var centerLeft = spritePivot.x * r.width + xOffset;
		var centerBottom = spritePivot.y * r.height + yOffset;

		// Debug.Log("Extracting " + sprite.name
		//     + " " + r.width + " x " + r.height
		//     + " left " + (spritePivot.x * r.width) + " bottom " + (spritePivot.y * r.height)
		//     + " to " + desWidth + " x " + desHeight + " at " + xOffset + " " + yOffset
		//     + " center " + centerLeft + " " + centerBottom);

		if (normalizedOutline.Count > 0)
		{
			for (int x = 0; x < r.width; x++)
			{
				for (int y = 0; y < r.height; y++)
				{
					var pixel = cacheTexture2D.GetPixel((int)r.x + x, (int)r.y + y);

					var p = new Vector2(x, y);

					var isInside = false;
					foreach (var o in normalizedOutline)
					{
						if (PointInTriangle(o, p))
						{
							isInside = true;
							break;
						}
					}

					if (isInside)
					{
						output.SetPixel(xOffset + x, yOffset + y, pixel);
					}
				}
			}
		}
		else
		{
			var pixels = cacheTexture2D.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height);
			output.SetPixels(xOffset, yOffset, (int)r.width, (int)r.height, pixels, 0);
		}

		output.Apply();
		output.name = sprite.name;
		return output;
	}

	private static void SaveSubSprite(Texture2D tex, string saveToDirectory)
	{
		if (!System.IO.Directory.Exists(saveToDirectory)) System.IO.Directory.CreateDirectory(saveToDirectory);
		System.IO.File.WriteAllBytes(System.IO.Path.Combine(saveToDirectory, tex.name + ".png"), tex.EncodeToPNG());
	}
}