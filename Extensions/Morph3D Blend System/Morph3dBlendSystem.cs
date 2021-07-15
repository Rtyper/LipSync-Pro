using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UMA;
using UMA.PoseTools;

namespace RogoDigital.Lipsync.Extensions {
	public class UMABlendSystem : BlendSystem {

		/// <summary>
		/// The UMA character.
		/// </summary>
		public UMAAvatarBase avatar;

		private UMAExpressionPlayer expressionPlayer;

		// Do any setup necessary here. BlendSystems run in edit mode as well as play mode, so this will also be called when Unity starts or your scripts recompile.
		// Make sure you call base.OnEnable() here for expected behaviour.
		public override void OnEnable () {
			// Sets info about this blend system for use in the editor.
			blendableDisplayName = "Pose";
			blendableDisplayNamePlural = "Poses";
			notReadyMessage = "UMA Avatar not set. The UMA 2 BlendSystem requires an avatar.";

			blendRangeLow = -1;
			blendRangeHigh = 1;

			if(avatar != null && Application.isPlaying){
				avatar.CharacterCreated.AddListener(OnUMACreated);
			}

			base.OnEnable();
		}
			
		public override void SetBlendableValue (int blendable, float value)
		{
			if(expressionPlayer != null){
				SetInternalValue(blendable , value);
				float[] values = expressionPlayer.Values;
				values[blendable] = value;
				expressionPlayer.Values = values;
			}
		}

		public override string[] GetBlendables ()
		{
			bool setInternal = false;
			string[] shapes = new string[ExpressionPlayer.PoseCount];


			if(blendableCount == 0) setInternal = true;

			for(int a = 0 ; a < shapes.Length ; a++){
				shapes[a] = ExpressionPlayer.PoseNames[a] + " (" + a.ToString() + ")";
				if(setInternal) AddBlendable(a , 0);
			}

			return shapes;
		}

		public void OnUMACreated (UMAData data) {
			expressionPlayer = data.gameObject.GetComponent<UMAExpressionPlayer>();
			if(expressionPlayer == null){
				expressionPlayer = data.gameObject.AddComponent<UMAExpressionPlayer>();
			}
			UMAExpressionSet expressionSet = data.umaRecipe.raceData.expressionSet;
			expressionPlayer.expressionSet = expressionSet;
			expressionPlayer.umaData = data;
			expressionPlayer.Initialize();
		}

		public override void OnVariableChanged () {
			if(avatar == null){
				isReady = false;
			}else {
				isReady = true;
			}
		}
	}
}