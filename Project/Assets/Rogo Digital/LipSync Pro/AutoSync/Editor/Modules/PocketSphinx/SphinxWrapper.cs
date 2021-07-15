using UnityEngine;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Text;
using System;

namespace RogoDigital.Lipsync.AutoSync
{
	public class SphinxWrapper
	{
		static int resultCode = 0;
		public static string result;
		public static string error;
		public static bool dataReady = false;
		public static bool isFinished = false;

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
#if UNITY_EDITOR_64
		[DllImport("Assets/Rogo Digital/LipSync Pro/AutoSync/Editor/Modules/PocketSphinx/64bit/libpocketsphinx.3.dylib", EntryPoint = "ps_run")]
		public static extern int PSRun([MarshalAs(UnmanagedType.FunctionPtr)] MessageCallback msgCallbackPtr, [MarshalAs(UnmanagedType.FunctionPtr)] ResultCallback resCallbackPtr, int argsCount, string[] argsArray);
#endif
#else
		[DllImport("pocketsphinx", EntryPoint = "ps_run")]
		public static extern int PSRun ([MarshalAs(UnmanagedType.FunctionPtr)] MessageCallback msgCallbackPtr, [MarshalAs(UnmanagedType.FunctionPtr)] ResultCallback resCallbackPtr, int argsCount, string[] argsArray);
#endif

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void MessageCallback (string value);

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate void ResultCallback (string value);

		// Use this for initialization
		public static void Recognize (string[] args, bool multiThread = true)
		{
			dataReady = false;
			isFinished = false;
			if (SystemInfo.processorCount == 1)
				multiThread = false;

			RecognizeProcess(args, multiThread);
		}

		public static void PhonemesFromDictionary (string[] args, bool multiThread = true)
		{
			dataReady = false;
			isFinished = false;
			if (SystemInfo.processorCount == 1)
				multiThread = false;

			RecognizeProcess(args, multiThread, true, true);
		}

		public static string RecognizeProcess (string[] args, bool multiThread = true, bool flagOff = true, bool toPhone = false)
		{

			if (resultCode != -1)
			{
				string dictFile = null;
				result = null;
				error = null;
				resultCode = -1;
				bool isTime = false;

				int i = 0;
				foreach (string arg in args)
				{
					if (arg == "-dict")
						dictFile = args[i + 1];

					if (arg == "-time")
						isTime = true;
					i++;
				}

				MessageCallback msgCallback = (value) =>
				{
					if (value.Contains("ERROR") || value.Contains("FATAL"))
					{
						Debug.LogError("[AutoSync] " + value);
						error = "FAILED";
					}
				};

				ResultCallback resCallback = (value) =>
				{
					result = value;

					if (toPhone && dictFile != null)
					{
						string[] words = new string[0];

						if (isTime)
						{
							result = result.Replace("<s>", "SIL").Replace("</s>", "SIL").Replace("<sil>", "SIL");

							words = result.Split('\n');
							string[] timeMark = new string[words.Length];
							i = 0;
							foreach (string word in words)
							{
								int pos = word.IndexOf(" ");
								if (pos > 0)
								{
									words[i] = word.Substring(0, pos);
									timeMark[i] = word.Substring(pos, word.Length - pos);
								}
								i++;
							}
							string[] phonemes = ConvertToPhonemes(dictFile, words);
							for (i = 0; i < phonemes.Length; i++)
							{
								phonemes[i] = phonemes[i].TrimStart() + timeMark[i] + "\r\n";
								Debug.Log(phonemes[i]);
							}
							result = String.Join("", phonemes);
						}
						else
						{
							words = result.Split(' ');
							string[] phonemes = ConvertToPhonemes(dictFile, words);
							result = String.Join(" ", phonemes);
						}

						dataReady = true;
					}
					else
					{
						dataReady = true;
					}
				};

				int argsCount = args.Length;

				try
				{
					if (multiThread)
					{
						Thread thread = new Thread(new ThreadStart(() =>
						{
							resultCode = PSRun(msgCallback, resCallback, argsCount, args);
							isFinished = true;
						}));
						thread.Start();
					}
					else
					{
						resultCode = PSRun(msgCallback, resCallback, argsCount, args);
						isFinished = true;
					}
				}
				catch (Exception e)
				{
					error = "FAILED: " + e.Message;
					return error;

				}


			}
			else
			{
				Debug.Log("SphinxWrapper is busy. Please wait and try again.");
			}

			return result;
		}


		static void RemoveSIL ()
		{
			result = result.Replace("SIL ", "").Replace(" SIL", "");
		}


		static string[] ConvertToPhonemes (string dictFile, string[] words)
		{
			string line;
			string[] phonemes;
			phonemes = words;

			if (words.Length > 0)
			{
				StreamReader theReader = new StreamReader(dictFile, Encoding.Default);
				using (theReader)
				{
					line = theReader.ReadLine();
					if (line != null)
					{
						while (!theReader.EndOfStream)
						{
							int i = 0;
							foreach (string word in words)
							{
								if (word.Length + 1 <= line.Length)
								{
									if (line.IndexOf(word + " ") == 0)
									{
										phonemes[i] = line.Substring(word.Length + 1, line.Length - word.Length - 1);
									}
								}
								i++;
							}
							line = theReader.ReadLine();
						}

					}
					theReader.Close();
					return phonemes;
				}
			}
			return null;
		}
	}

	public class PluginPathClass
	{
		public string currentPath;
		public string dllPath;

		public PluginPathClass ()
		{
			currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
			dllPath = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "Assets" + Path.DirectorySeparatorChar + "Plugins";
			if (currentPath.Contains(dllPath) == false)
			{
				Environment.SetEnvironmentVariable("PATH", currentPath + Path.PathSeparator + dllPath, EnvironmentVariableTarget.Process);
			}
		}
	}
}