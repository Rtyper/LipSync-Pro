using UnityEngine;
using System.Collections.Generic;

namespace RogoDigital.Lipsync
{
	[RequireComponent(typeof(SpriteManager))]
	public class SpriteBlendSystem : BlendSystem
	{

		[SerializeField]
		private SpriteManager manager;

		// Do any setup necessary here. BlendSystems run in edit mode as well as play mode, so this will also be called when Unity starts or your scripts recompile.
		// Make sure you call base.OnEnable() here for expected behaviour.
		public override void OnEnable ()
		{
			// Sets info about this blend system for use in the editor.
			blendableDisplayName = "Sprite";
			blendableDisplayNamePlural = "Sprites";
			noBlendablesMessage = "No Sprites or Layers available. Add Sprites and Layers to the attached SpriteManager to use them.";
			notReadyMessage = "No renderers set up";

#if LS_EXPERIMENTAL_FEATURES
			allowResyncing = true;
#endif

			if (manager == null)
			{
				if (gameObject.GetComponents<SpriteBlendSystem>().Length > 1)
				{
					manager = gameObject.AddComponent<SpriteManager>();
				}
				else
				{
					manager = gameObject.GetComponent<SpriteManager>();
				}
				manager.blendSystem = this;
			}
			else if (manager.blendSystem == null)
			{
				manager.blendSystem = this;
			}

			base.OnEnable();
		}

		// This method is used for setting the value of a blendable. The blendable argument is a zero-based index for identifying a blendable.
		// It will never be higher than the number of blendables returned by GetBlendables().
		public override void SetBlendableValue (int blendable, float value)
		{
			// These two lines are important to avoid errors if the method is called before the system is setup.
			if (!isReady)
				return;

			if (manager.groups.Count == 0 || manager.availableSprites.Count == 0)
				return;

			int groupnum = Mathf.FloorToInt(blendable / manager.availableSprites.Count);

			SpriteRenderer group = manager.groups[groupnum].spriteRenderer;
			if (group == null)
				return;

			SetInternalValue(blendable, value);

			int highest = 0;
			float highestWeight = 0;
			for (int s = groupnum * manager.availableSprites.Count; s < (groupnum + 1) * manager.availableSprites.Count; s++)
			{
				float sWeight = GetBlendableValue(s);
				if (sWeight > highestWeight)
				{
					highestWeight = sWeight;
					highest = s % manager.availableSprites.Count;
				}
			}

			if (highestWeight == 0)
			{
				group.sprite = manager.groups[groupnum].defaultSprite;
			}
			else if (group != null)
			{
				group.sprite = manager.availableSprites[highest];
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
				manager = GetComponent<SpriteManager>();

			for (int a = 0; a < manager.groups.Count; a++)
			{
				if (manager.groups[a] != null)
				{
					for (int s = 0; s < manager.availableSprites.Count; s++)
					{
						if (manager.availableSprites[s] != null)
						{
							blendShapes.Add(manager.groups[a].groupName + "/" + manager.availableSprites[s].name + "(" + ((a * manager.availableSprites.Count) + s).ToString() + ")");
							AddBlendable(a, 0);
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

		[BlendSystemButton("Show Help")]
		public void ShowHelp ()
		{
			Application.OpenURL("http://updates.rogodigital.com/AssetStore/LipSync/spriteblendsystem.pdf");
		}
	}
}