using UnityEditor;

namespace RogoDigital {
	public class ModalWindow : EditorWindow {
		public ModalParent parent;

		public void Show (ModalParent parent) {
			this.parent = parent;
			parent.currentModal = this;
			base.ShowUtility();
		}

		private void OnDestroy () {
			parent.currentModal = null;
			parent.Focus();
		}
	}
}
