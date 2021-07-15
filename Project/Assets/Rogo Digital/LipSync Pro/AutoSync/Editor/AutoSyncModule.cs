using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RogoDigital.Lipsync.AutoSync
{
	[AutoSyncModuleInfo("An Unknown AutoSync Module.")]
	public abstract class AutoSyncModule : ScriptableObject
	{
		/// <summary>
		/// Returns a bitmask of compatibility criteria that must be met in order for this module to run.
		/// </summary>
		/// <returns></returns>
		public abstract ClipFeatures GetCompatibilityRequirements();

		/// <summary>
		/// Returns a bitmask of compatibility criteria that will be fulfilled by this module.
		/// </summary>
		/// <returns></returns>
		public abstract ClipFeatures GetOutputCompatibility();

		/// <summary>
		/// Begins processing the supplied inputClip using this module's settings, and will call the supplied callback when finished.
		/// </summary>
		/// <param name="inputClip"></param>
		/// <param name="callback"></param>
		public abstract void Process(LipSyncData inputClip, AutoSync.ASProcessDelegate callback);

		/// <summary>
		/// Begin processing this module, using the supplied templates for any new markers created. Override this if you want to provide support for templates in your module.
		/// </summary>
		/// <param name="inputClip"></param>
		/// <param name="callback"></param>
		/// <param name="phonemeTemplate"></param>
		/// <param name="emotionTemplate"></param>
		/// <param name="gestureTemplate"></param>
		public virtual void ProcessWithTemplates(LipSyncData inputClip, AutoSync.ASProcessDelegate callback, PhonemeMarker phonemeTemplate, EmotionMarker emotionTemplate)
		{
			Process(inputClip, callback);
		}
	}
}