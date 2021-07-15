using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace RogoDigital.Lipsync.AutoSync
{
    public class ASAzureSpeechRecognitionSettings : AutoSyncModuleSettings
    {
        public string subscriptionKey;
        public string regionName;

        private void OnEnable()
        {
            subscriptionKey = EditorPrefs.GetString("as_azurespeech_subscription_key");
            regionName = EditorPrefs.GetString("as_azurespeech_region_name");
        }
    }
}