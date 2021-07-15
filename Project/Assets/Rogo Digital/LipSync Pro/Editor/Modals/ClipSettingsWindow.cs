using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using System;
using System.IO;
using RogoDigital.Lipsync.AutoSync;

namespace RogoDigital.Lipsync
{
	public class ClipSettingsWindow : ModalWindow
	{
		private LipSyncClipSetup setup;

		private float start;
		private float end;
		private float length;
		private string transcript;
		private Vector2 scroll;

		private bool adjustMarkers = true;
		private bool willTrim = false;
		private bool soXAvailable = false;

		private int durationMode = 0;
		private AnimBool adjustMarkersAnimBool;

		private void OnEnable ()
		{
			soXAvailable = AutoSyncConversionUtility.IsConversionAvailable;
		}

		private void OnGUI ()
		{
			GUILayout.Space(20);

			EditorGUI.BeginDisabledGroup(setup.Clip && !soXAvailable);
			LipSyncEditorExtensions.BeginPaddedHorizontal();
			durationMode = GUILayout.Toolbar(durationMode, new string[] { "Duration", "Start + End Times" });
			LipSyncEditorExtensions.EndPaddedHorizontal();
			GUILayout.Space(10);
			if (durationMode == 0)
			{
				willTrim = length != setup.FileLength;
				TimeSpan time = TimeSpan.FromSeconds(length);

				int minutes = time.Minutes;
				int seconds = time.Seconds;
				int milliseconds = time.Milliseconds;

				GUILayout.BeginHorizontal(GUILayout.MaxWidth(280));
				EditorGUI.BeginChangeCheck();
				GUILayout.Label("Duration");
				minutes = EditorGUILayout.IntField(minutes);
				GUILayout.Label("m", EditorStyles.miniLabel);
				seconds = EditorGUILayout.IntField(seconds);
				GUILayout.Label("s", EditorStyles.miniLabel);
				milliseconds = EditorGUILayout.IntField(milliseconds);
				GUILayout.Label("ms", EditorStyles.miniLabel);
				if (EditorGUI.EndChangeCheck())
				{
					float nl = (minutes * 60) + seconds + (milliseconds / 1000f);
					if (setup.Clip)
						nl = Mathf.Clamp(nl, 0, setup.Clip.length);
					length = nl;
				}
				GUILayout.EndHorizontal();
			}
			else
			{
				willTrim = start > 0 || end < setup.FileLength;
				TimeSpan startTime = TimeSpan.FromSeconds(start);
				TimeSpan endTime = TimeSpan.FromSeconds(end);

				int startMinutes = startTime.Minutes;
				int startSeconds = startTime.Seconds;
				int startMilliseconds = startTime.Milliseconds;
				int endMinutes = endTime.Minutes;
				int endSeconds = endTime.Seconds;
				int endMilliseconds = endTime.Milliseconds;

				GUILayout.BeginHorizontal(GUILayout.MaxWidth(280));
				EditorGUI.BeginChangeCheck();
				GUILayout.Label("Start Time");
				startMinutes = EditorGUILayout.IntField(startMinutes);
				GUILayout.Label("m", EditorStyles.miniLabel);
				startSeconds = EditorGUILayout.IntField(startSeconds);
				GUILayout.Label("s", EditorStyles.miniLabel);
				startMilliseconds = EditorGUILayout.IntField(startMilliseconds);
				GUILayout.Label("ms", EditorStyles.miniLabel);
				if (EditorGUI.EndChangeCheck())
				{
					float ns = (startMinutes * 60) + startSeconds + (startMilliseconds / 1000f);
					ns = Mathf.Clamp(ns, 0, end);
					start = ns;
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal(GUILayout.MaxWidth(280));
				EditorGUI.BeginChangeCheck();
				GUILayout.Label("End Time");
				endMinutes = EditorGUILayout.IntField(endMinutes);
				GUILayout.Label("m", EditorStyles.miniLabel);
				endSeconds = EditorGUILayout.IntField(endSeconds);
				GUILayout.Label("s", EditorStyles.miniLabel);
				endMilliseconds = EditorGUILayout.IntField(endMilliseconds);
				GUILayout.Label("ms", EditorStyles.miniLabel);
				if (EditorGUI.EndChangeCheck())
				{
					float ne = (endMinutes * 60) + endSeconds + (endMilliseconds / 1000f);
					if (setup.Clip)
						ne = Mathf.Clamp(ne, start, setup.Clip.length);
					end = ne;
				}
				GUILayout.EndHorizontal();
			}
			EditorGUI.EndDisabledGroup();
			adjustMarkersAnimBool.target = willTrim;
			if (EditorGUILayout.BeginFadeGroup(adjustMarkersAnimBool.faded))
			{
				adjustMarkers = EditorGUILayout.Toggle("Keep Marker Times", adjustMarkers);
			}
			EditorGUILayout.EndFadeGroup();

			if (setup.Clip && !soXAvailable)
				EditorGUILayout.HelpBox("Cannot Change duration as SoX is not available to trim the audio. Follow the included guide to set up SoX.", MessageType.Warning);

			GUILayout.Space(10);
			GUILayout.Label("Transcript");
			scroll = GUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
			transcript = GUILayout.TextArea(transcript, GUILayout.ExpandHeight(true));
			GUILayout.EndScrollView();
			GUILayout.FlexibleSpace();
			GUILayout.Space(20);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button(willTrim && setup.Clip ? "Trim & Save" : "Save", GUILayout.MinWidth(100), GUILayout.Height(20)))
			{
				setup.Transcript = transcript;
				if (willTrim)
				{
					if (durationMode == 0)
					{
						if (setup.Clip)
						{
							TrimClip(0, length);
						}
						else
						{
							if (adjustMarkers)
								AdjustMarkers(0, length);
						}

						setup.FileLength = length;
					}
					else
					{
						if (setup.Clip)
						{
							TrimClip(start, end - start);
						}
						else
						{
							if (adjustMarkers)
								AdjustMarkers(start, end - start);
						}

						setup.FileLength = end - start;
					}
				}

				setup.changed = true;
				setup.previewOutOfDate = true;
				Close();
			}
			GUILayout.Space(10);
			if (GUILayout.Button("Cancel", GUILayout.MinWidth(100), GUILayout.Height(20)))
			{
				Close();
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.Space(20);
		}

		void TrimClip (double newStartTime, double newLength)
		{
			if (soXAvailable)
			{
				// Paths
				string originalPathRelative = AssetDatabase.GetAssetPath(setup.Clip);
				string originalPathAbsolute = Application.dataPath + "/" + originalPathRelative.Substring("/Assets".Length);

				string newPathRelative = Path.GetDirectoryName(originalPathRelative) + "/" + Path.GetFileNameWithoutExtension(originalPathRelative) + "_Trimmed_" + newLength + Path.GetExtension(originalPathRelative);
				string newPathAbsolute = Application.dataPath + "/" + newPathRelative.Substring("/Assets".Length);

				string soXPath = EditorPrefs.GetString("LipSync_SoXPath");
				string soXArgs = "\"" + originalPathAbsolute + "\" \"" + newPathAbsolute + "\" trim " + newStartTime + " " + newLength;

				System.Diagnostics.Process process = new System.Diagnostics.Process();
				process.StartInfo.FileName = soXPath;
				process.StartInfo.Arguments = soXArgs;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.RedirectStandardError = true;

				process.ErrorDataReceived += (object e, System.Diagnostics.DataReceivedEventArgs outLine) =>
				{
					if (!string.IsNullOrEmpty(outLine.Data))
					{
						if (outLine.Data.Contains("FAIL"))
						{
							Debug.LogError("SoX Audio Trimming Failed: " + outLine.Data);
							process.Close();
						}
					}
				};

				process.Start();
				process.BeginErrorReadLine();
				process.WaitForExit(5000);

				AssetDatabase.Refresh();
				AudioClip newClip = AssetDatabase.LoadAssetAtPath<AudioClip>(newPathRelative);

				if (adjustMarkers)
					AdjustMarkers(newStartTime, newLength);

				setup.Clip = newClip;
				length = setup.Clip.length;
				setup.FixEmotionBlends();
			}
		}

		void AdjustMarkers (double newStartTime, double newLength)
		{
			// Times
			float newStartNormalised = 1 - ((setup.FileLength - (float)newStartTime) / setup.FileLength);
			float newEndNormalised = ((float)newStartTime + (float)newLength) / setup.FileLength;

			// Adjust Marker timings (go backwards so indices don't change)
			float multiplier = 1 / (newEndNormalised - newStartNormalised);
			for (int p = setup.PhonemeData.Count - 1; p >= 0; p--)
			{
				if (setup.PhonemeData[p].time < newStartNormalised || setup.PhonemeData[p].time > newEndNormalised)
				{
					setup.PhonemeData.RemoveAt(p);
				}
				else
				{
					setup.PhonemeData[p].time -= newStartNormalised;
					setup.PhonemeData[p].time *= multiplier;
				}
			}

			for (int g = setup.GestureData.Count - 1; g >= 0; g--)
			{
				if (setup.GestureData[g].time < newStartNormalised || setup.GestureData[g].time > newEndNormalised)
				{
					setup.GestureData.RemoveAt(g);
				}
				else
				{
					setup.GestureData[g].time -= newStartNormalised;
					setup.GestureData[g].time *= multiplier;
				}
			}

			for (int e = setup.EmotionData.Count - 1; e >= 0; e--)
			{
				if (setup.EmotionData[e].endTime < newStartNormalised || setup.EmotionData[e].startTime > newEndNormalised)
				{
					EmotionMarker em = setup.EmotionData[e];
					setup.EmotionData.Remove(em);
				}
				else
				{
					setup.EmotionData[e].startTime -= newStartNormalised;
					setup.EmotionData[e].startTime *= multiplier;
					setup.EmotionData[e].startTime = Mathf.Clamp01(setup.EmotionData[e].startTime);

					setup.EmotionData[e].endTime -= newStartNormalised;
					setup.EmotionData[e].endTime *= multiplier;
					setup.EmotionData[e].endTime = Mathf.Clamp01(setup.EmotionData[e].endTime);
				}
			}
		}

		public static ClipSettingsWindow CreateWindow (ModalParent parent, LipSyncClipSetup setup)
		{
			ClipSettingsWindow window = CreateInstance<ClipSettingsWindow>();

			window.length = setup.FileLength;
			window.transcript = setup.Transcript;
			window.end = window.length;

			window.position = new Rect(parent.center.x - 250, parent.center.y - 100, 500, 200);
			window.minSize = new Vector2(500, 200);
			window.titleContent = new GUIContent("Clip Settings");

			window.adjustMarkersAnimBool = new AnimBool(window.willTrim, window.Repaint);

			window.setup = setup;
			window.Show(parent);
			return window;
		}
	}
}