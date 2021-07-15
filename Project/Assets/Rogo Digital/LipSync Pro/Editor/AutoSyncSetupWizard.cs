using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using RogoDigital.Lipsync.AutoSync;

namespace RogoDigital.Lipsync
{
	public class AutoSyncSetupWizard : WizardWindow
	{
		private string detectedOS, soXPath;
		private bool osLin, osMac, mfaFound;
		private List<System.Type> installedModules;
		private List<AutoSyncModuleInfoAttribute> moduleInfos;
		private List<AutoSyncModuleSettings> settingsObjects;
		private bool locked = false;

		private void DetectPaths ()
		{
			var dirGUIDS = AssetDatabase.FindAssets("AutoSync");
			if (dirGUIDS.Length == 0)
			{
				EditorUtility.DisplayDialog("Setup Error", "The 'AutoSync' folder could not be found in the project, please try re-downloading LipSync Pro from the Asset Store before trying setup again.", "Ok");
				Close();
			}
			string baseSearchPath = AssetDatabase.GUIDToAssetPath(dirGUIDS[0]);

			if (detectedOS.ToLowerInvariant().Contains("windows"))
			{
				string path = SearchForFile(baseSearchPath, "sox.exe");
				if (path == null)
				{
					soXPath = "";
				}
				else
				{
					soXPath = path;
				}
			}
			else if (detectedOS.ToLowerInvariant().Contains("mac"))
			{
				string path = SearchForFile(baseSearchPath, "sox");
				if (path == null)
				{
					soXPath = "";
				}
				else
				{
					soXPath = path;
				}
			}
			else
			{
				soXPath = "";
			}
		}

		new private void OnEnable ()
		{
			base.OnEnable();

			installedModules = AutoSyncUtility.GetModuleTypes();
			moduleInfos = new List<AutoSyncModuleInfoAttribute>(installedModules.Count);
			settingsObjects = new List<AutoSyncModuleSettings>(installedModules.Count);
			locked = false;
			canContinue = true;

			for (int i = 0; i < installedModules.Count; i++)
			{
				moduleInfos.Add(AutoSyncUtility.GetModuleInfo(installedModules[i]));

				if (moduleInfos[i].moduleSettingsType != null)
				{
					var settings = (AutoSyncModuleSettings)CreateInstance(moduleInfos[i].moduleSettingsType);
					settings.InitSetupWizardValues();
					settingsObjects.Add(settings);
				}
				else
				{
					settingsObjects.Add(null);
				}

				if (installedModules[i].Name == "ASMontrealPhonemeDetectionModule")
					mfaFound = true;
			}
		}

		public override void OnWizardGUI ()
		{
			EditorGUI.BeginDisabledGroup(locked);
			switch (currentStep)
			{
				case 1:
					EditorGUILayout.HelpBox("This wizard will guide you through setting up AutoSync for your current project.\nCheck the auto-detected settings below (based on your system) and press 'Continue' to apply them.", MessageType.Info);
					EditorGUILayout.Space();
					EditorGUILayout.LabelField("Operating System", detectedOS);
					EditorGUILayout.HelpBox("SoX Sound Exchange is a third-party library that AutoSync can use to convert audio files to the correct format to be processed, making it compatible with a wider range of files.", MessageType.Info);
					soXPath = LipSyncEditorExtensions.DrawPathField("SoX Sound Exchange Path", soXPath, "ls_debug", "Find SoX Sound Exchange");
					EditorGUILayout.Space();
					LipSyncEditorExtensions.BeginPaddedHorizontal(20);
					if(GUILayout.Button("Auto-Detect Path", GUILayout.MaxWidth(250), GUILayout.Height(20)))
					{
						DetectPaths();
					}
					LipSyncEditorExtensions.EndPaddedHorizontal(20);
					EditorGUILayout.Space();
					if (osLin)
					{
						EditorGUILayout.HelpBox("The Linux editor is not officially supported at this time, some AutoSync features may still work, but the PocketSphinx module and SoX audio conversion are not supported.", MessageType.Error);
						EditorGUILayout.Space();
					}
					else if (osMac)
					{
						EditorGUILayout.HelpBox("Note that the included PocketSphinx module is not supported on most versions of macOS. Follow the instructions on the next screen for more info.", MessageType.Warning);
						EditorGUILayout.Space();
					}

					break;
				case 2:
					GUILayout.Label("Installed Modules", EditorStyles.boldLabel);
					GUILayout.BeginHorizontal();
					GUILayout.Space(10);
					GUILayout.BeginVertical();
					if (moduleInfos != null)
					{
						for (int i = 0; i < moduleInfos.Count; i++)
						{
							GUILayout.Label(moduleInfos[i].displayName);
						}
					}
					GUILayout.EndVertical();
					GUILayout.EndHorizontal();
					EditorGUILayout.Space();
					if (mfaFound)
					{
						EditorGUILayout.HelpBox("It looks like all recommended modules are installed. Feel free to download others from the Extension Window.", MessageType.Info);
					}
					else
					{
						EditorGUILayout.HelpBox("In order to keep the initial download size down, only the legacy PocketSphinx module is included. For best results (and compatibility with macOS) we recommend downloading the Montreal Forced Aligner module, which makes use of both the audio clip and a text transcript to create much more accurate lip-sync animation.", MessageType.Info);
						if (GUILayout.Button("Start Downloading Montreal Forced Aligner", GUILayout.Height(25)))
						{
							locked = true;
							canContinue = false;

							if (osMac)
							{
								RDExtensionWindow.RequestInstall("AutoSync Montreal Forced Aligner Module (Mac)");
							}
							else if (!osLin)
							{
								RDExtensionWindow.RequestInstall("AutoSync Montreal Forced Aligner Module (Win)");
							}
						}
					}

					break;
				case 3:
					EditorGUILayout.HelpBox("This section contains setup for individual modules. You may want to run this wizard again later if you download additional modules that require setup, or you may configure them manually from the AutoSync settings page in the Clip Editor.", MessageType.Info);
					EditorGUILayout.Space();

					for (int i = 0; i < moduleInfos.Count; i++)
					{
						if (moduleInfos[i].moduleSettingsType != null)
						{
							GUILayout.Label(moduleInfos[i].displayName + " Setup", EditorStyles.boldLabel);
							settingsObjects[i].DrawSetupWizardSection();
							EditorGUILayout.Space();
						}
					}
					break;
			}
			EditorGUI.EndDisabledGroup();

			if (locked)
			{
				GUILayout.Space(10);
				if (GUILayout.Button("Continue Wizard Anyway", GUILayout.Height(30)))
				{
					locked = false;
					canContinue = true;
					RemoveNotification();
				}

				ShowNotification(new GUIContent("Please allow the download to finish + install before returning to this wizard."));
			}
		}

		public override void OnBackPressed ()
		{

		}

		public override void OnContinuePressed ()
		{
			switch (currentStep)
			{
				case 3:
					EditorPrefs.SetString("LipSync_SoXPath", soXPath);
					EditorPrefs.SetBool("LipSync_SoXAvailable", AutoSyncUtility.VerifyProgramAtPath(soXPath, "SoX"));
					for (int i = 0; i < settingsObjects.Count; i++)
					{
						if (settingsObjects[i])
						{
							settingsObjects[i].ApplySetupWizardValues();
						}
					}
					break;
			}
		}

		[MenuItem("Window/Rogo Digital/LipSync Pro/AutoSync Setup Wizard", priority = 10)]
		public static void ShowWindow ()
		{
			AutoSyncSetupWizard window = GetWindow<AutoSyncSetupWizard>(true);
			window.topMessage = "Easily configure AutoSync for your project.";
			window.totalSteps = 3;
			window.Focus();

			window.titleContent = new GUIContent("AutoSync Setup Wizard");
			window.canContinue = true;

			window.detectedOS = SystemInfo.operatingSystem;
			window.soXPath = EditorPrefs.GetString("LipSync_SoXPath");

			if (string.IsNullOrEmpty(window.soXPath))
			{
				window.DetectPaths();
			}

			if (window.detectedOS.ToLowerInvariant().Contains("windows"))
			{ }
			else if (window.detectedOS.ToLowerInvariant().Contains("mac"))
			{
				window.osMac = true;
			}
			else
			{
				window.osLin = true;
			}
		}

		public static string SearchForFile (string rootPath, string searchPattern)
		{
			string[] paths = Directory.GetFiles(rootPath, searchPattern, SearchOption.AllDirectories);
			if (paths.Length > 0)
			{
				return paths[0];
			}
			return null;
		}
	}
}