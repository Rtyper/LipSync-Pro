using UnityEngine;
using System.Collections.Generic;

namespace RogoDigital.Lipsync
{
	[System.Serializable]
	public class TextureOffsetManager : MonoBehaviour
	{
		[SerializeField]
		public MaterialTextureGroup[] materialGroups = new MaterialTextureGroup[0];
		[HideInInspector]
		public TextureOffsetBlendSystem blendSystem;

		[System.Serializable]
		public class MaterialTextureGroup
		{
			[SerializeField]
			public string displayName;
			[Space, SerializeField]
			public Material material;
			[SerializeField]
			public string texturePropertyName;
			[Space, SerializeField]
			public Texture2D defaultTexture;
			[SerializeField]
			public Vector2 defaultTextureOffset;
			[SerializeField]
			public Vector2 defaultTextureScale = Vector2.one;
			[Space, SerializeField]
			public TextureSetting[] textureSettings;
		}

		[System.Serializable]
		public class TextureSetting
		{
			[SerializeField]
			public string displayName;
			[Space, SerializeField]
			public Texture2D texture;
			[SerializeField]
			public Vector2 textureOffset;
			[SerializeField]
			public Vector2 textureScale = Vector2.one;
		}
	}
}
