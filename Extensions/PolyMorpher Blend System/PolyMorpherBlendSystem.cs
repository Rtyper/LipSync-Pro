using RogoDigital.Lipsync;
using UnityEngine;

public class PolyMorpherBlendSystem : BlendSystem
{
	public PolyMorpher polyMorpherComponent;

	public override void OnEnable ()
	{
		// Sets info about this blend system for use in the editor.
		blendRangeHigh = 1;
		blendableDisplayName = "Shape";
		blendableDisplayNamePlural = "Shapes";
		noBlendablesMessage = "No Shapes defined on this PolyMorpher component.";
		notReadyMessage = "PolyMorpher component not defined or set up.";

		base.OnEnable();
	}

	public override void SetBlendableValue (int blendable, float value)
	{
		if(!isReady)
			return;

		polyMorpherComponent.SetShapeWeight(blendable, value);
		SetInternalValue(blendable , value);
		polyMorpherComponent.Morph();
	}

	public override string[] GetBlendables ()
	{
		// These two lines are important to avoid errors if the method is called before the system is setup.
		if(!isReady)
			return null;
		
		bool setInternal = false;
		string[] blendShapes = new string[polyMorpherComponent.m_vertexShapeKey.Count];
		if (blendableCount == 0) setInternal = true;

		for (int a = 0; a < blendShapes.Length; a++) {
			blendShapes[a] = polyMorpherComponent.m_vertexShapeKey[a].name + " (" + a.ToString() + ")";
			if (setInternal) AddBlendable(a, polyMorpherComponent.m_vertexShapeKey[a].weight);
		}

		return blendShapes;
	}

	public override void OnVariableChanged ()
	{
		isReady = false;
		if (polyMorpherComponent != null) {
			if (polyMorpherComponent.m_originalMesh != null) {
				isReady = true;
			}
		}
	}
}
