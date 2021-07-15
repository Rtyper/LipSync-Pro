using RogoDigital.Lipsync;

// This Blend System intentionally does nothing. It is intended as an alternative to the blendshape blend system for those using entirely bone-based rigs.

public class BonesOnlyBlendSystem : BlendSystem
{
	public override void OnVariableChanged ()
	{
		isReady = true;
	}
}
