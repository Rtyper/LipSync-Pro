namespace RogoDigital.Lipsync.AutoSync
{
	[System.Flags]
	public enum ClipFeatures
	{
		None = 0,
		AudioClip = 16,
		Emotions = 4,
		Gestures = 8,
		Phonemes = 2,
		Transcript = 1,
	}
}