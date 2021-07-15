using UnityEngine;
using UnityEditor;

namespace RogoDigital {
	public class ModalParent : EditorWindow {
		public bool disabled = false;
		public ModalWindow currentModal = null;
		public Vector2 center {
			get {
				return new Vector2(position.x + (position.width / 2), position.y + (position.height / 2));
			}
		}

		public virtual void OnModalGUI () {
		}

		void OnGUI () {
			if (Event.current.type == EventType.Repaint) {
				EditorGUI.BeginDisabledGroup(currentModal != null || disabled);
				if (currentModal != null) {
					Event.current = null;
				}
			}
			OnModalGUI();
			if (Event.current.type == EventType.Repaint) EditorGUI.EndDisabledGroup();
		}

		void OnFocus () {
			if (currentModal != null) {
				EditorApplication.Beep();
				currentModal.Focus();
			}
		}
	}
}