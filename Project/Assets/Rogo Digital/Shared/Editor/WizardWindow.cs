using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;

namespace RogoDigital
{
	public class WizardWindow : EditorWindow
	{

		public int currentStep
		{
			get
			{
				return _currentStep;
			}

			set
			{
				_currentStep = value;
				progressBar.target = (float)_currentStep / (float)_totalSteps;
			}
		}

		public int totalSteps
		{
			get
			{
				return _totalSteps;
			}
			set
			{
				_totalSteps = value;
				progressBar.target = (float)_currentStep / (float)_totalSteps;
			}
		}

		private int _currentStep = 1;
		private int _totalSteps = 1;

		public bool canContinue = true;
		public string topMessage = "";

		private AnimFloat progressBar;
		private Texture2D white;

		public void OnEnable ()
		{
			progressBar = new AnimFloat(0, Repaint);
			progressBar.speed = 2;
			white = (Texture2D)EditorGUIUtility.Load("Rogo Digital/Shared/white.png");
		}

		void OnGUI ()
		{
			Rect topbar = EditorGUILayout.BeginHorizontal();
			GUI.Box(topbar, "", EditorStyles.toolbar);
			GUILayout.FlexibleSpace();
			GUILayout.Box(topMessage + " Step " + currentStep.ToString() + "/" + totalSteps.ToString(), EditorStyles.label);
			GUILayout.FlexibleSpace();
			GUILayout.Box("", EditorStyles.toolbar);
			EditorGUILayout.EndHorizontal();
			GUI.color = Color.grey;
			GUI.DrawTexture(new Rect(0, topbar.height, topbar.width, 3), white);
			GUI.color = new Color(1f, 0.77f, 0f);
			GUI.DrawTexture(new Rect(0, topbar.height, progressBar.value * topbar.width, 3), white);
			GUI.color = Color.white;
			GUILayout.Space(20);

			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(20);
			EditorGUILayout.BeginVertical();

			OnWizardGUI();

			EditorGUILayout.EndVertical();
			GUILayout.Space(20);
			EditorGUILayout.EndHorizontal();

			GUILayout.FlexibleSpace();
			Rect bottomBar = EditorGUILayout.BeginHorizontal(GUILayout.Height(50));
			EditorGUILayout.BeginVertical();
			GUILayout.FlexibleSpace();
			EditorGUILayout.BeginHorizontal();
			GUI.Box(bottomBar, "", EditorStyles.helpBox);
			GUILayout.FlexibleSpace();
			GUILayout.Space(20);
			if (GUILayout.Button((currentStep == 1) ? "Cancel" : "Back", GUILayout.Height(30), GUILayout.MaxWidth(200)))
			{
				Back();
			}
			GUILayout.Space(10);
			GUILayout.FlexibleSpace();
			GUILayout.Space(10);
			if (canContinue)
			{
				if (GUILayout.Button((currentStep == totalSteps) ? "Finish" : "Continue", GUILayout.Height(30), GUILayout.MaxWidth(200)))
				{
					Continue();
				}
			}
			else
			{
				GUI.color = Color.grey;
				GUILayout.Box("Continue", (GUIStyle)"button", GUILayout.Height(30), GUILayout.MaxWidth(200));
				GUI.color = Color.white;
			}
			GUILayout.Space(20);
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
		}

		protected void Continue ()
		{
			OnContinuePressed();
			GUI.FocusControl("");
			if (currentStep < totalSteps)
			{
				currentStep++;
			}
			else
			{
				Close();
			}
		}

		protected void Back ()
		{
			OnBackPressed();
			if (currentStep > 1)
			{
				currentStep--;
			}
			else
			{
				Close();
			}
		}

		public virtual void OnContinuePressed ()
		{
		}

		public virtual void OnBackPressed ()
		{
		}

		public virtual void OnWizardGUI ()
		{
		}
	}
}