using UnityEngine;

namespace RogoDigital.Lipsync {
	[System.Serializable]
	public class GestureMarker : System.Object {
		[SerializeField]
		public string gesture;
		[SerializeField]
		public float time;

		public GestureMarker (string gesture, float time) {
			this.gesture = gesture;
			this.time = time;
		}

		public GestureMarker CreateCopy () {
			GestureMarker m = new GestureMarker(gesture, time);
			return m;
		}
	}
}
