using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace RogoDigital {
	public class RDEditorShortcut {
		public delegate void RDEditorShortcutActionDelegate ();

		public int action;
		public KeyCode key;
		public EventModifiers modifiers;

		public static void Serialize (string prefix, RDEditorShortcut[] shortcuts) {
			if (shortcuts.Length == 0) { Debug.LogError("Shortcuts list was empty."); return; }

			string info = shortcuts.Length.ToString() + "_";
			for (int a = 0; a < shortcuts.Length; a++) {
				info += (int)shortcuts[a].modifiers + "_" + (int)shortcuts[a].key + "_" + shortcuts[a].action + "_";
			}

			EditorPrefs.SetString(prefix + "_KeyboardShortcuts", info);
		}

		public static RDEditorShortcut[] Deserialize (string prefix, List<Action> actions) {
			return Deserialize(prefix, actions, null);
		}

		public static RDEditorShortcut[] Deserialize (string prefix, List<Action> actions, RDEditorShortcut[] defaults) {
			if (!EditorPrefs.HasKey(prefix + "_KeyboardShortcuts")) return defaults;

			string[] info = EditorPrefs.GetString(prefix + "_KeyboardShortcuts").Split('_');
			int count = int.Parse(info[0]);

			if (count < 3) return defaults;

			RDEditorShortcut[] shortcuts = new RDEditorShortcut[count];

			int infoCount = 1;
			for (int a = 0; a < count; a++) {
				RDEditorShortcut shortcut = new RDEditorShortcut();
				try {
					shortcut.modifiers = (EventModifiers)int.Parse(info[infoCount]);
					shortcut.key = (KeyCode)int.Parse(info[infoCount + 1]);
					shortcut.action = int.Parse(info[infoCount + 2]);
				} catch (System.Exception e) {
					Debug.Log(e.Message);
				}

				infoCount += 3;

				shortcuts[a] = shortcut;
			}

			return shortcuts;
		}

		public RDEditorShortcut () {
		}

		public RDEditorShortcut (int action, KeyCode key, EventModifiers modifier) {
			this.action = action;
			this.key = key;
			this.modifiers = modifier;
		}

		public struct Action {
			public string name;
			public RDEditorShortcutActionDelegate action;

			public Action (string name, RDEditorShortcutActionDelegate action) {
				this.name = name;
				this.action = action;
			}

			public static implicit operator string (Action action) {
				return action.name;
			}
		}
	}
}