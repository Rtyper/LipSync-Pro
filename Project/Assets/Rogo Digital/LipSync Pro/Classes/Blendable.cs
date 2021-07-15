namespace RogoDigital.Lipsync {
	[System.Serializable]
	public class Blendable {
		public int number;
		public float currentWeight;

		public Blendable (int number, float currentWeight) {
			this.number = number;
			this.currentWeight = currentWeight;
		}
	}
}
