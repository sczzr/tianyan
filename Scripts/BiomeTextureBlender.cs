using Godot;
using System;
using System.Collections.Generic;

namespace TianYanShop
{
	/// <summary>
	/// 生物群系纹理混合器 - 使用预处理方式生成混合纹理
	/// </summary>
	public static class BiomeTextureBlender
	{
		[Flags]
		public enum BlendDirection
		{
			None = 0,
			Top = 1,
			Right = 2,
			Bottom = 4,
			Left = 8
		}
		
		public static Image BlendBoundaryTile(
			Image centerImage,
			Dictionary<BlendDirection, (Image image, float strength)> neighborData,
			Vector2I atlasCoords,
			int tileSize)
		{
			var centerTile = ExtractTileFromAtlas(centerImage, atlasCoords, tileSize);
			var result = Image.CreateEmpty(tileSize, tileSize, false, Image.Format.Rgba8);
			
			for (int x = 0; x < tileSize; x++)
			{
				for (int y = 0; y < tileSize; y++)
				{
					Color finalColor = centerTile.GetPixel(x, y);
					
					if (neighborData.ContainsKey(BlendDirection.Top))
					{
						var (topImage, strength) = neighborData[BlendDirection.Top];
						float blendFactor = CalculateBlendFactor(y, tileSize, strength);
						
						if (blendFactor > 0.01f)
						{
							var topTile = ExtractTileFromAtlas(topImage, atlasCoords, tileSize);
							Color topColor = topTile.GetPixel(x, y);
							finalColor = finalColor.Lerp(topColor, blendFactor);
						}
					}
					
					if (neighborData.ContainsKey(BlendDirection.Bottom))
					{
						var (bottomImage, strength) = neighborData[BlendDirection.Bottom];
						float blendFactor = CalculateBlendFactor(tileSize - 1 - y, tileSize, strength);
						
						if (blendFactor > 0.01f)
						{
							var bottomTile = ExtractTileFromAtlas(bottomImage, atlasCoords, tileSize);
							Color bottomColor = bottomTile.GetPixel(x, y);
							finalColor = finalColor.Lerp(bottomColor, blendFactor);
						}
					}
					
					if (neighborData.ContainsKey(BlendDirection.Left))
					{
						var (leftImage, strength) = neighborData[BlendDirection.Left];
						float blendFactor = CalculateBlendFactor(x, tileSize, strength);
						
						if (blendFactor > 0.01f)
						{
							var leftTile = ExtractTileFromAtlas(leftImage, atlasCoords, tileSize);
							Color leftColor = leftTile.GetPixel(x, y);
							finalColor = finalColor.Lerp(leftColor, blendFactor);
						}
					}
					
					if (neighborData.ContainsKey(BlendDirection.Right))
					{
						var (rightImage, strength) = neighborData[BlendDirection.Right];
						float blendFactor = CalculateBlendFactor(tileSize - 1 - x, tileSize, strength);
						
						if (blendFactor > 0.01f)
						{
							var rightTile = ExtractTileFromAtlas(rightImage, atlasCoords, tileSize);
							Color rightColor = rightTile.GetPixel(x, y);
							finalColor = finalColor.Lerp(rightColor, blendFactor);
						}
					}
					
					result.SetPixel(x, y, finalColor);
				}
			}
			
			return result;
		}
		
		private static float CalculateBlendFactor(int distanceFromEdge, int tileSize, float blendStrength)
		{
			float normalizedDistance = (float)distanceFromEdge / tileSize;
			float smoothFactor = Mathf.SmoothStep(1.0f, 0.0f, normalizedDistance);
			return smoothFactor * blendStrength;
		}
		
		private static Image ExtractTileFromAtlas(Image atlasImage, Vector2I atlasCoords, int tileSize)
		{
			var tile = Image.CreateEmpty(tileSize, tileSize, false, Image.Format.Rgba8);
			
			int startX = atlasCoords.X * tileSize;
			int startY = atlasCoords.Y * tileSize;
			
			int atlasWidth = atlasImage.GetWidth();
			int atlasHeight = atlasImage.GetHeight();
			
			for (int x = 0; x < tileSize; x++)
			{
				for (int y = 0; y < tileSize; y++)
				{
					int atlasX = startX + x;
					int atlasY = startY + y;
					
					if (atlasX < atlasWidth && atlasY < atlasHeight)
					{
						Color pixel = atlasImage.GetPixel(atlasX, atlasY);
						tile.SetPixel(x, y, pixel);
					}
					else
					{
						tile.SetPixel(x, y, new Color(0, 0, 0, 0));
					}
				}
			}
			
			return tile;
		}
	}
}
