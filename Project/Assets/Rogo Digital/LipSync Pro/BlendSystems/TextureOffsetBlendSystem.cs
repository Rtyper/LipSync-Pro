using UnityEngine;
using System.Collections.Generic;

namespace RogoDigital.Lipsync
{
	[RequireComponent(typeof(TextureOffsetManager))]
	public class TextureOffsetBlendSystem : BlendSystem
	{

		[SerializeField]
		private TextureOffsetManager manager;

		private Dictionary<int, int> groupLookup;
		private Dictionary<string, int> reverseGroupLookup;

		// Do any setup necessary here. BlendSystems run in edit mode as well as play mode, so this will also be called when Unity starts or your scripts recompile.
		// Make sure you call base.OnEnable() here for expected behaviour.
		public override void OnEnable ()
		{
			// Sets info about this blend system for use in the editor.
			blendableDisplayName = "Texture Setting";
			blendableDisplayNamePlural = "Texture Settings";
			noBlendablesMessage = "No Texture Settings available. Add Texture Settings to the attached Texture Offset Manager to use them.";
			notReadyMessage = "No renderers set up";

#if LS_EXPERIMENTAL_FEATURES
			allowResyncing = true;
#endif

			if (manager == null)
			{
				if (gameObject.GetComponents<TextureOffsetBlendSystem>().Length > 1)
				{
					manager = gameObject.AddComponent<TextureOffsetManager>();
				}
				else
				{
					manager = gameObject.GetComponent<TextureOffsetManager>();
				}
				manager.blendSystem = this;
			}
			else if (manager.blendSystem == null)
			{
				manager.blendSystem = this;
			}

			CacheGroups();

			base.OnEnable();
		}

		private void CacheGroups ()
		{
			groupLookup = new Dictionary<int, int>();
			reverseGroupLookup = new Dictionary<string, int>();

			int count = 0;
			for (int i = 0; i < manager.materialGroups.Length; i++)
			{
				for (int a = 0; a < manager.materialGroups[i].textureSettings.Length; a++)
				{
					groupLookup.Add(count, i);
					reverseGroupLookup.Add(i.ToString() + a, count);
					Debug.LogFormat("Cached {0} at index {1} as being in group {2} with a sub-index of {3}", manager.materialGroups[i].textureSettings[a].displayName, count, i, a);
					count++;
				}
			}
		}

		// This method is used for setting the value of a blendable. The blendable argument is a zero-based index for identifying a blendable.
		// It will never be higher than the number of blendables returned by GetBlendables().
		public override void SetBlendableValue (int blendable, float value)
		{
			// These two lines are important to avoid errors if the method is called before the system is setup.
			if (!isReady)
				return;

			if (manager.materialGroups.Length == 0)
				return;

			if (blendableCount != groupLookup.Count)
				CacheGroups();

			int groupNumber = groupLookup[blendable];
			TextureOffsetManager.MaterialTextureGroup group = manager.materialGroups[groupNumber];
			if (group == null)
				return;

			SetInternalValue(blendable, value);

			int highest = 0;
			float highestWeight = 0;
			for (int s = 0; s < group.textureSettings.Length; s++)
			{
				float sWeight = GetBlendableValue(reverseGroupLookup[groupNumber.ToString() + s]);
				if (sWeight > highestWeight)
				{
					highestWeight = sWeight;
					highest = s;
				}
			}

			if (!group.material)
				return;

			if (highestWeight == 0)
			{
				if (!group.defaultTexture)
					return;
				group.material.SetTexture(group.texturePropertyName, group.defaultTexture);
				group.material.SetTextureOffset(group.texturePropertyName, group.defaultTextureOffset);
				group.material.SetTextureScale(group.texturePropertyName, group.defaultTextureScale);
			}
			else if (group != null)
			{
				if (!group.textureSettings[highest].texture)
					return;
				group.material.SetTexture(group.texturePropertyName, group.textureSettings[highest].texture);
				group.material.SetTextureOffset(group.texturePropertyName, group.textureSettings[highest].textureOffset);
				group.material.SetTextureScale(group.texturePropertyName, group.textureSettings[highest].textureScale);
			}

		}

		// This method is used to populate the blendables dropdown in the LipSync editor.
		// The array of strings it returns should be easily readable, and can be categorised using "/".
		public override string[] GetBlendables ()
		{
			// These two lines are important to avoid errors if the method is called before the system is setup.
			if (!isReady)
				return null;

			ClearBlendables();
			List<string> blendShapes = new List<string>();

			if (manager == null)
			{
				manager = GetComponent<TextureOffsetManager>();
			}

			int count = 0;
			for (int a = 0; a < manager.materialGroups.Length; a++)
			{
				if (manager.materialGroups[a] != null)
				{
					for (int s = 0; s < manager.materialGroups[a].textureSettings.Length; s++)
					{
						if (manager.materialGroups[a].textureSettings[s] != null)
						{
							blendShapes.Add(manager.materialGroups[a].displayName + "/" + manager.materialGroups[a].textureSettings[s].displayName + " (" + count + ")");
							AddBlendable(a, 0);
							count++;
						}
					}
				}
			}
			return blendShapes.ToArray();
		}

		// This method is called whenever a public, non-static variable from this class (not the base BlendSystem class) is changed in the LipSync editor.
		// It should check that any essential variables have valid values, and set isReady to true only if they do.
		public override void OnVariableChanged ()
		{
			isReady = true;
		}

	}
}