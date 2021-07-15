using UnityEngine;

namespace RogoDigital.Lipsync {
	[System.Serializable]
	public class BoneShape : System.Object {
		[SerializeField]
		public Transform bone;
		[SerializeField]
		public Vector3 endPosition;
		[SerializeField]
		public Vector3 endRotation;
		[SerializeField]
		public Vector3 endScale = Vector3.one;

		[SerializeField]
		public bool lockPosition;
		[SerializeField]
		public bool lockRotation;
		[SerializeField]
		public bool lockScale;

		public Vector3 neutralPosition;
		public Vector3 neutralRotation;
		public Vector3 neutralScale = Vector3.one;

		public void SetNeutral () {
			if (bone != null) {
				neutralPosition = bone.localPosition;
				neutralRotation = bone.localEulerAngles;
				neutralScale = bone.localScale;
			}
		}

		public BoneShape (Transform bone, Vector3 endPosition, Vector3 endRotation, Vector3 endScale) {
			this.bone = bone;
			this.endPosition = endPosition;
			this.endRotation = endRotation;
			this.endScale = endScale;
		}

		public BoneShape (Transform bone, Vector3 endPosition, Vector3 endRotation) {
			this.bone = bone;
			this.endPosition = endPosition;
			this.endRotation = endRotation;
			this.endScale = bone.localScale;
		}

		public BoneShape () {
		}
	}
}