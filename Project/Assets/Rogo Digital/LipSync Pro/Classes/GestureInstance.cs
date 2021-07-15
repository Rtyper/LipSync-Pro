using UnityEngine;

[System.Serializable]
public class GestureInstance : System.Object {
	[SerializeField]
	public string gesture;
	[SerializeField]
	public AnimationClip clip;
	[SerializeField]
	public string triggerName;

	public GestureInstance (string gesture, AnimationClip clip, string triggerName) {
		this.gesture = gesture;
		this.clip = clip;
		this.triggerName = triggerName;
	}

	public bool IsValid (Animator animator) {
		for (int a = 0; a < animator.parameters.Length; a++) {
			if (animator.parameters[a].name == triggerName) {
				return true;
			}
		}
		return false;
	}
}
