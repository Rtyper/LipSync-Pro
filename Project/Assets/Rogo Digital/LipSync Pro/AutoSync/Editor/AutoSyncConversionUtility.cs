using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RogoDigital.Lipsync.AutoSync
{
	public static class AutoSyncConversionUtility
	{
		public static bool IsConversionAvailable
		{
			get
			{
				return EditorPrefs.GetBool("LipSync_SoXAvailable");
			}
		}

		public static bool StartConversion(string inputPath, string outputPath, AudioFormat outputFormat)
		{
			string args = string.Format("\"{0}\" -t {1} \"{2}\"", inputPath, GetAudioFormatArg(outputFormat), outputPath);
			return RunSoXProcess(outputPath, args);
		}

		public static bool StartConversion(string inputPath, string outputPath, AudioFormat outputFormat, int outputSampleRateHz, int outputChannelCount)
		{
			string args = string.Format("\"{0}\" -t {1} -r {2} -c {3} \"{4}\"", inputPath, GetAudioFormatArg(outputFormat), outputSampleRateHz, outputChannelCount, outputPath);
			return RunSoXProcess(outputPath, args);
		}

		public static bool StartConversion(string inputPath, string outputPath, AudioFormat outputFormat, int outputSampleRateHz, int outputSampleSizeBits, int outputChannelCount)
		{
			string args = string.Format("\"{0}\" -t {1} -r {2} -b {3} -c {4} \"{5}\"", inputPath, GetAudioFormatArg(outputFormat), outputSampleRateHz, outputSampleSizeBits, outputChannelCount, outputPath);
			return RunSoXProcess(outputPath, args);
		}

		public static bool StartConversion(string inputPath, string outputPath, AudioFormat outputFormat, int outputSampleRateHz, int outputSampleSizeBits, int outputChannelCount, EncodingType outputEncodingType, Endianness outputEndianness)
		{
			string args = string.Format("\"{0}\" -t {1} -r {2} -b {3} -c {4} -e {5} {6} \"{7}\"", inputPath, GetAudioFormatArg(outputFormat), outputSampleRateHz, outputSampleSizeBits, outputChannelCount, GetEncodingTypeArg(outputEncodingType), GetEndiannessArg(outputEndianness), outputPath);
			return RunSoXProcess(outputPath, args);
		}

		public static bool StartConversion(string inputPath, AudioFormat inputFormat, int inputSampleRateHz, int inputSampleSizeBits, int inputChannelCount, EncodingType inputEncodingType, Endianness inputEndianness,
											string outputPath, AudioFormat outputFormat, int outputSampleRateHz, int outputSampleSizeBits, int outputChannelCount, EncodingType outputEncodingType, Endianness outputEndianness)
		{
			string args = string.Format("-t {0} -r {1} -b {2} -c {3} - e {4} {5} \"{6}\" -t {7} -r {8} -b {9} -c {10} -e {11} {12} \"{13}\"", GetAudioFormatArg(inputFormat), inputSampleRateHz, inputSampleSizeBits, inputChannelCount, GetEncodingTypeArg(inputEncodingType), GetEndiannessArg(inputEndianness), inputPath, GetAudioFormatArg(outputFormat), outputSampleRateHz, outputSampleSizeBits, outputChannelCount, GetEncodingTypeArg(outputEncodingType), GetEndiannessArg(outputEndianness), outputPath);
			return RunSoXProcess(outputPath, args);
		}

		[System.Obsolete("Use AppendFiles instead.", true)]
		public static bool AppendFile(string input1Path, string input2Path, string outputPath)
		{
			string args = string.Format("\"{0}\" \"{1}\" \"{2}\"", input1Path, input2Path, outputPath);
			return RunSoXProcess(outputPath, args);
		}

		public static bool AppendFiles(string outputPath, params string[] inputPaths)
		{
			string args = "";

			for (int i = 0; i < inputPaths.Length; i++)
			{
				if(!string.IsNullOrEmpty(inputPaths[i]))
					args = string.Format("{0}\"{1}\" ", args, inputPaths[i]);
			}

			args = string.Format("{0}\"{1}\"", args, outputPath);

			return RunSoXProcess(outputPath, args);
		}

		private static bool RunSoXProcess(string outPath, string args)
		{
			string soXPath = EditorPrefs.GetString("LipSync_SoXPath");

			Directory.SetCurrentDirectory(Application.dataPath.Remove(Application.dataPath.Length - 6));
			soXPath = Path.GetFullPath(soXPath);

			System.Diagnostics.Process process = new System.Diagnostics.Process();
			process.StartInfo.FileName = soXPath;
			process.StartInfo.Arguments = args;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.RedirectStandardError = true;

			process.Start();
			process.WaitForExit(20000);

			string error = process.StandardError.ReadLine();
			if (!string.IsNullOrEmpty(error))
			{
				if (error.Contains("FAIL"))
				{
					Debug.Log(error);
					process.Close();
					return false;
				}
			}

			return true;
		}

		private static string GetEncodingTypeArg(EncodingType t)
		{
			switch (t)
			{
				default:
				case EncodingType.SignedInteger:
					return "signed";
				case EncodingType.UnsignedInteger:
					return "unsigned";
				case EncodingType.FloatingPoint:
					return "float";
				case EncodingType.ALaw:
					return "a-law";
				case EncodingType.MuLaw:
					return "mu-law";
				case EncodingType.OKI_ADPCM:
					return "oki";
				case EncodingType.IMA_ADPCM:
					return "ima";
				case EncodingType.MS_ADPCM:
					return "ms";
				case EncodingType.GSM:
					return "gsm";
			}
		}

		private static string GetAudioFormatArg(AudioFormat t)
		{
			switch (t)
			{
				default:
				case AudioFormat.WavPCM:
					return "wav";
				case AudioFormat.AIFF:
					return "aiff";
				case AudioFormat.FLAC:
					return "flac";
				case AudioFormat.MP2:
					return "mp2";
				case AudioFormat.MP3:
					return "mp3";
				case AudioFormat.OggVorbis:
					return "ogg";
				case AudioFormat.Raw:
					return "raw";
				case AudioFormat.VOC:
					return "voc";
				case AudioFormat.VOX:
					return "vox";
			}
		}

		private static string GetEndiannessArg(Endianness t)
		{
			switch (t)
			{
				default:
				case Endianness.BigEndian:
					return "-B";
				case Endianness.LittleEndian:
					return "-L";
				case Endianness.SwapEndianness:
					return "-x";
			}
		}

		public enum Endianness
		{
			BigEndian,
			LittleEndian,
			SwapEndianness,
		}

		public enum EncodingType
		{
			SignedInteger,
			UnsignedInteger,
			FloatingPoint,
			ALaw,
			MuLaw,
			OKI_ADPCM,
			IMA_ADPCM,
			MS_ADPCM,
			GSM,
		}

		public enum AudioFormat
		{
			AIFF,
			OggVorbis,
			MP2,
			MP3,
			WavPCM,
			FLAC,
			VOC,
			VOX,
			Raw,
		}
	}
}