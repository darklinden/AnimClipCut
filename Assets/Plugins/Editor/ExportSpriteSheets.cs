using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ExportSubSprites {

	[MenuItem("Assets/Export Sub-Sprites", false, 80)]
	public static void DoExportSubSprites() {
		var folder = EditorUtility.OpenFolderPanel("Export subsprites into what folder?", "", "");
		foreach (var obj in Selection.objects) {
			var sprite = obj as Sprite;
			if (sprite == null)
				continue;
			var extracted = ExtractAndName(sprite);
			SaveSubSprite(extracted, folder);
		}
	}

	private static Texture2D CropTexture(Texture2D pSource, int left, int top, int width, int height) {
		if (left < 0) {
			width += left;
			left = 0;
		}
		if (top < 0) {
			height += top;
			top = 0;
		}
		if (left + width > pSource.width) {
			width = pSource.width - left;
		}
		if (top + height > pSource.height) {
			height = pSource.height - top;
		}

		if (width <= 0 || height <= 0) {
			return null;
		}

		Color[] aSourceColor = pSource.GetPixels(0);

		//*** Make New
		Texture2D oNewTex = new Texture2D(width, height, TextureFormat.RGBA32, false);

		//*** Make destination array
		int xLength = width * height;
		Color[] aColor = new Color[xLength];

		int i = 0;
		for (int y = 0; y < height; y++) {
			int sourceIndex = (y + top) * pSource.width + left;
			for (int x = 0; x < width; x++) {
				aColor[i++] = aSourceColor[sourceIndex++];
			}
		}

		//*** Set Pixels
		oNewTex.SetPixels(aColor);
		oNewTex.Apply();

		//*** Return
		return oNewTex;
	}

	// Since a sprite may exist anywhere on a tex2d, this will crop out the sprite's claimed region and return a new, cropped, tex2d.
	private static Texture2D ExtractAndName(Sprite sprite) {
		var r = sprite.textureRect;
		var output = CropTexture(sprite.texture, (int)r.x, (int)r.y, (int)r.width, (int)r.height);
		output.name = sprite.name;
		return output;
	}

	private static void SaveSubSprite(Texture2D tex, string saveToDirectory) {
		if (!System.IO.Directory.Exists(saveToDirectory))
			System.IO.Directory.CreateDirectory(saveToDirectory);
		System.IO.File.WriteAllBytes(System.IO.Path.Combine(saveToDirectory, tex.name + ".png"), tex.EncodeToPNG());
	}
}