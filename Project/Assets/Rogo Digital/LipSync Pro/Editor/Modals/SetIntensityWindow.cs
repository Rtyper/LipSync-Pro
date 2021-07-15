using UnityEngine;
using UnityEditor;

using RogoDigital;

public class SetIntensityWindow : ModalWindow {
	private LipSyncClipSetup setup;

	private AnimationCurve remapCurve = new AnimationCurve();
	private bool advanced;

	void OnEnable () {
		remapCurve.AddKey(0, 0);
		remapCurve.AddKey(1, 1);
	}

	void OnGUI () {
		GUILayout.Space(10);
		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		EditorGUILayout.HelpBox("This window will be deprecated in favour of the 'Set Intensity From Volume' AutoSync module in a future version. It is still functional for now.", MessageType.Warning);
		GUILayout.Space(10);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		EditorGUILayout.HelpBox("Depending on your audio, you may want phoneme intensities to be influenced differently by audio volume. This curve can be used to remap audio volume (x) to phoneme intensity (y). It is set to linear by default.", MessageType.Info);
		GUILayout.Space(10);
		GUILayout.EndHorizontal();
		GUILayout.Space(15);
		remapCurve = EditorGUILayout.CurveField("Remap Curve", remapCurve, Color.yellow, new Rect(0, 0, 1, 1));
		GUILayout.FlexibleSpace();
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Accept", GUILayout.MinWidth(100), GUILayout.Height(20))) {
			Begin();
			Close();
		}
		GUILayout.Space(10);
		if (GUILayout.Button("Cancel", GUILayout.MinWidth(100), GUILayout.Height(20))) {
			Close();
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.Space(10);
	}

	void Begin () {
		for (int m = 0; m < setup.PhonemeData.Count; m++) {
			setup.PhonemeData[m].intensity = remapCurve.Evaluate(GetRMS(4096, Mathf.RoundToInt(setup.PhonemeData[m].time * setup.Clip.samples)));
		}

		setup.changed = true;
		setup.previewOutOfDate = true;
	}

	float GetRMS (int samples, int offset) {
		float[] sampleData = new float[samples];

		setup.Clip.GetData(sampleData, offset); // fill array with samples

		float sum = 0;
		for (int i = 0; i < samples; i++) {
			sum += sampleData[i] * sampleData[i]; // sum squared samples
		}

		return Mathf.Sqrt(sum / samples); // rms = square root of average
	}

	public static void CreateWindow (ModalParent parent, LipSyncClipSetup setup) {
		SetIntensityWindow window = CreateInstance<SetIntensityWindow>();

		window.position = new Rect(parent.center.x - 250, parent.center.y - 75, 500, 150);
		window.minSize = new Vector2(500, 150);
		window.titleContent = new GUIContent("Set Intensities");
		window.setup = setup;

		window.Show(parent);
	}
}
