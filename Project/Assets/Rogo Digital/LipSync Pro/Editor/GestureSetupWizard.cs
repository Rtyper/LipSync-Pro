using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

using RogoDigital.Lipsync;
using RogoDigital;

public class GestureSetupWizard : WizardWindow {
	private LipSync component;
	private AnimatorController controller;
	private LipSyncProject settings;

	// Step 1
	private int newLayerChoice = 0;
	private string newLayerName = "LipSync Gestures";
	private int layerSelected = 0;
	private bool additive = true;

	// Step 2
	private Vector2 scrollPosition;
	private float transitionTime = 0.2f;
	private bool allowGestureInterrupts = true;
	private string[] triggerNames;

	public override void OnWizardGUI () {

		switch (currentStep) {
			case 1:
				newLayerChoice = GUILayout.Toolbar(newLayerChoice, new string[] { "Create New Layer", "Use Existing Layer" });
				GUILayout.Space(10);
				if (newLayerChoice == 0) {
					GUILayout.BeginHorizontal(GUILayout.Height(25));
					newLayerName = EditorGUILayout.TextField("New Layer Name", newLayerName, GUILayout.Height(20));
					GUILayout.EndHorizontal();
					GUILayout.Space(5);
					additive = GUILayout.Toggle(additive, "Make Layer Additive");

					// Logic
					if (string.IsNullOrEmpty(newLayerName)) {
						canContinue = false;
					} else {
						canContinue = true;
					}
				} else {
					GUILayout.Label("Chose a Layer");
					GUILayout.Space(10);
					scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
					for (int a = 0; a < controller.layers.Length; a++) {
						GUILayout.BeginHorizontal(GUILayout.Height(25));
						GUILayout.Space(10);
						bool selected = EditorGUILayout.Toggle(layerSelected == a, EditorStyles.radioButton, GUILayout.Width(30));
						layerSelected = selected ? a : layerSelected;
						GUILayout.Space(5);
						GUILayout.Label(controller.layers[a].name);
						GUILayout.FlexibleSpace();
						GUILayout.EndHorizontal();

						canContinue = true;
					}
					EditorGUILayout.EndScrollView();
				}
				break;
			case 2:
				GUILayout.Label("Layer Settings");
				GUILayout.Space(5);
				transitionTime = EditorGUILayout.FloatField("Transition Time", transitionTime);
				allowGestureInterrupts = EditorGUILayout.Toggle(new GUIContent("Allow Gesture Interrupts", "Should hitting a new Gesture marker interrupt the previous one, or should it be queued?"), allowGestureInterrupts);
				GUILayout.Space(15);
				GUILayout.Label("Trigger Settings");
				GUILayout.Space(5);
				scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
				for (int a = 0; a < triggerNames.Length; a++) {
					GUILayout.BeginHorizontal(GUILayout.Height(25));
					GUILayout.Space(4);
					GUILayout.Label("Trigger for '" + settings.gestures[a] + "' is called: ");
					triggerNames[a] = GUILayout.TextField(triggerNames[a]);
					GUILayout.EndHorizontal();
				}
				EditorGUILayout.EndScrollView();
				break;
		}
	}

	public override void OnContinuePressed () {
		scrollPosition = Vector2.zero;

		switch (currentStep) {
			case 1:
				triggerNames = new string[settings.gestures.Count];
				for (int a = 0; a < settings.gestures.Count; a++) {
					triggerNames[a] = settings.gestures[a] + "_trigger";
				}

				break;
			case 2:
				if (newLayerChoice == 0) {
					for (int l = 0; l < controller.layers.Length; l++) {
						if (controller.layers[l].name == newLayerName) controller.RemoveLayer(l);
					}

					controller.AddLayer(newLayerName);
					layerSelected = controller.layers.Length - 1;
					if(additive) controller.layers[layerSelected].blendingMode = AnimatorLayerBlendingMode.Additive;
				}
				
				// Create Triggers
				for (int a = 0; a < settings.gestures.Count; a++) {
					for (int p = 0; p < controller.parameters.Length; p++) {
						if (controller.parameters[p].name == triggerNames[a]) controller.RemoveParameter(p);
					}

					controller.AddParameter(triggerNames[a], AnimatorControllerParameterType.Trigger);
				}

				AnimatorStateMachine sm = controller.layers[layerSelected].stateMachine;

				// Create States and transitions
				AnimatorState defaultState = null;
				defaultState = sm.AddState("None");
				if(newLayerChoice == 0) sm.defaultState = defaultState;

				for (int a = 0; a < settings.gestures.Count; a++) {
					AnimatorState newState = null;

					newState = sm.AddState(settings.gestures[a]);
					newState.motion = component.gestures[a].clip;
					AnimatorStateTransition transition = null;

					transition = defaultState.AddTransition(newState);
					transition.duration = transitionTime;
					transition.interruptionSource = allowGestureInterrupts ? TransitionInterruptionSource.SourceThenDestination : TransitionInterruptionSource.None;
					transition.AddCondition(AnimatorConditionMode.If, 0, triggerNames[a]);

					transition = newState.AddTransition(defaultState);
					transition.hasExitTime = true;
					transition.duration = transitionTime;
					transition.interruptionSource = TransitionInterruptionSource.Destination;

					component.gestures[a].triggerName = triggerNames[a];
				}
				component.gesturesLayer = layerSelected;

				break;
		}
	}

	public static void ShowWindow (LipSync component, AnimatorController controller) {
		GestureSetupWizard window = EditorWindow.GetWindow<GestureSetupWizard>(true);
		window.component = component;
		window.controller = controller;
		window.topMessage = "Setting up Gestures for " + controller.name + ".";
		window.totalSteps = 2;
		window.Focus();
		window.titleContent = new GUIContent("Gesture Setup Wizard");

		window.settings = LipSyncEditorExtensions.GetProjectFile();
	}
}
