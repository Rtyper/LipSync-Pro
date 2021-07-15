using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RogoDigital.Lipsync.AutoSync
{
	[CustomEditor(typeof(ASMontrealPhonemeDetectionSettings))]
	public class ASMontrealPhonemeDetectionSettingsEditor : Editor
	{
		public override void OnInspectorGUI ()
		{
			serializedObject.Update();
			LipSyncEditorExtensions.DrawVerifiedPathField(serializedObject.FindProperty("applicationPath"), "as_montrealfa_application_path", "as_montrealfa_application_path_verified", "Find Montreal Forced Aligner Application", Verify);
			serializedObject.ApplyModifiedProperties();
		}

		private bool Verify ()
		{
			return AutoSyncUtility.VerifyProgramAtPath(EditorPrefs.GetString("as_montrealfa_application_path"), "align");
		}
	}
}