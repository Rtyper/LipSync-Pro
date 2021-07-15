using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace RogoDigital.Lipsync
{

	[AddComponentMenu("Rogo Digital/LipSync Pro")]
	[DisallowMultipleComponent]
	[HelpURL("https://lipsync.rogodigital.com/documentation/lipsync.php")]
	public class LipSync : BlendSystemUser
	{
#pragma warning disable 618

		// Public Variables

		/// <summary>
		/// AudioSource used for playing dialogue
		/// </summary>
		public AudioSource audioSource;

		/// <summary>
		/// Allow bones to be used in phoneme shapes.
		/// </summary>
		public bool useBones = false;

		/// <summary>
		/// Used for deciding if/when to repose boneshapes in LateUpdate.
		/// </summary>
		public bool boneUpdateAnimation = false;

		/// <summary>
		/// All PhonemeShapes on this LipSync instance.
		/// PhonemeShapes are a list of blendables and
		/// weights associated with a particular phoneme.
		/// </summary>
		[SerializeField]
		public List<PhonemeShape> phonemes = new List<PhonemeShape>();

		/// <summary>
		/// All EmotionShapes on this LipSync instance.
		/// EmotionShapes are simply PhonemeShapes, but
		/// with a string identifier instead of a Phoneme.
		/// Emotions are set up in the Project Settings.
		/// </summary>
		[SerializeField]
		public List<EmotionShape> emotions = new List<EmotionShape>();

		/// <summary>
		/// If checked, the component will play defaultClip on awake.
		/// </summary>
		public bool playOnAwake = false;

		/// <summary>
		/// If checked, the clip will play again when it finishes.
		/// </summary>
		public bool loop = false;

		/// <summary>
		/// The clip to be played when playOnAwake is checked.
		/// </summary>
		public LipSyncData defaultClip = null;

		/// <summary>
		/// The delay between calling Play() and the clip playing.
		/// </summary>
		public float defaultDelay = 0f;

		/// <summary>
		/// If true, audio playback speed will match the timescale setting (allows slow or fast motion speech)
		/// </summary>
		public bool scaleAudioSpeed = true;

		[SerializeField]
		private AnimationTimingMode m_animationTimingMode = AnimationTimingMode.AudioPlayback;
		/// <summary>
		/// How animation playback is timed. AudioPlayback is linked to the audio position. FixedFrameRate assumes a constant speed (useful for offline rendering).
		/// </summary>
		public AnimationTimingMode animationTimingMode
		{
			get
			{
				return m_animationTimingMode;
			}
			set
			{
#if UNITY_WEBGL
				if(value == AnimationTimingMode.AudioPlayback) {
					Debug.LogError("AnimationTimingMode.AudioPlayback is not supported on WebGL. Falling back to AnimationTimingMode.CustomTimer");
					m_animationTimingMode = AnimationTimingMode.CustomTimer;
				} else {
					m_animationTimingMode = value;
				}
#endif
				m_animationTimingMode = value;
			}
		}

		/// <summary>
		/// The framerate used for fixed framerate rendering.
		/// </summary>
		public int frameRate = 30;

		/// <summary>
		/// If there are no phonemes within this many seconds
		/// of the previous one, a rest will be inserted.
		/// </summary>
		public float restTime = 0.2f;

		/// <summary>
		/// The time, in seconds, that a shape will be held for
		/// before blending to neutral when a rest is inserted.
		/// </summary>
		public float restHoldTime = 0.4f;

		/// <summary>
		/// The method used for generating curve tangents. Tight will ensure poses
		/// are matched exactly, but can make movement robotic, Loose will look
		/// more natural but can can cause poses to be over-emphasized.
		/// </summary>
		public CurveGenerationMode phonemeCurveGenerationMode = CurveGenerationMode.Loose;

		/// <summary>
		/// The method used for generating curve tangents. Tight will ensure poses
		/// are matched exactly, but can make movement robotic, Loose will look
		/// more natural but can can cause poses to be over-emphasized.
		/// </summary>
		public CurveGenerationMode emotionCurveGenerationMode = CurveGenerationMode.Tight;

		/// <summary>
		/// If true, any emotion marker that doesn't blend out before the end of the clip
		/// will stay active when the clip finishes.
		/// </summary>
		public bool keepEmotionWhenFinished = false;

		/// <summary>
		/// If true, will set the neutral position, rotation and scale for each bone (i.e. the transformation
		/// used when a bone isn't used in the current pose) to the position, rotation and scale the bone is in
		/// at the start of the scene.
		/// </summary>
		public bool setNeutralBonePosesOnStart = false;

		/// <summary>
		/// Whether or not there is currently a LipSync animation playing.
		/// </summary>
		public bool IsPlaying
		{
			get;
			private set;
		}

		/// <summary>
		/// Whether the currently playing animation is paused.
		/// </summary>
		public bool IsPaused
		{
			get;
			private set;
		}

		/// <summary>
		/// Whether the currently playing animation is transitioning back to neutral.
		/// </summary>
		public bool IsStopping
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the current playback time, in seconds.
		/// </summary>
		public float CurrentTime
		{
			get
			{
				if (!IsPlaying)
					return 0;

				switch (animationTimingMode)
				{
					case AnimationTimingMode.AudioPlayback:
						return audioSource.time;
					default:
						return customTimer;
				}
			}
		}

		/// <summary>
		/// The Animator component used for Gestures.
		/// </summary>
		public Animator gesturesAnimator;

		/// <summary>
		/// The Gestures layer.
		/// </summary>
		public int gesturesLayer;

		/// <summary>
		/// The animation clips used for gestures.
		/// </summary>
		public List<GestureInstance> gestures;

		/// <summary>
		/// Called when a clip finished playing.
		/// </summary>
		public UnityEvent onFinishedPlaying;

#if UNITY_EDITOR
		/// <summary>
		/// Used for updating Clip Editor previews. Only available in the editor.
		/// </summary>
		public BlendSystem.BlendSystemGenericDelegate onSettingsChanged;
#endif

		// Private Variables

		private AudioClip audioClip;
		private bool ready = false;
		private Dictionary<string, EmotionShape> emotionCache;
		private int currentFileID = 0;
		private LipSyncData lastClip;

		private float emotionBlendTime = 0;
		private float emotionTimer = 0;
		private bool changingEmotion = false;
		private int customEmotion = -1;
		private float customTimer = 0;
		private bool isDelaying = false;

		// Marker Data
		private List<PhonemeMarker> phonemeMarkers;
		private List<EmotionMarker> emotionMarkers;
		private List<GestureMarker> gestureMarkers;
		private float fileLength;

		private int nextGesture = 0;

		// Curves
		private List<int> indexBlendables;
		private List<AnimationCurve> animCurves;
		private List<Transform> bones;
		private List<TransformAnimationCurve> boneCurves;
		private List<Vector3> boneNeutralPositions;
		private List<Vector3> boneNeutralScales;
		private List<Quaternion> boneNeutralRotations;

		// Used by the editor
		public ResetDelegate reset;
		public float lastUsedVersion = 0;

		public delegate void ResetDelegate();

		void Reset()
		{
			CleanUpBlendSystems();
			if (reset != null)
				reset.Invoke();
		}

		void Awake()
		{

			// Get reference to attached AudioSource
			if (audioSource == null)
				audioSource = GetComponent<AudioSource>();

			// Ensure BlendSystem is set to allow animation
			if (audioSource == null)
			{
				Debug.LogError("[LipSync - " + gameObject.name + "] No AudioSource specified or found.");
				return;
			}
			else if (blendSystem == null)
			{
				Debug.LogError("[LipSync - " + gameObject.name + "] No BlendSystem set.");
				return;
			}
			else if (blendSystem.isReady == false)
			{
				Debug.LogError("[LipSync - " + gameObject.name + "] BlendSystem is not set up.");
				return;
			}
			else
			{
				ready = true;
			}

			// Check for old-style settings
			if (restTime < 0.1f)
			{
				Debug.LogWarning("[LipSync - " + gameObject.name + "] Rest Time and/or Hold Time are lower than recommended and may cause animation errors. From LipSync 0.6, Rest Time is recommended to be 0.2 and Hold Time is recommended to be 0.1");
			}

			// Cache Emotions for more performant cross-checking
			emotionCache = new Dictionary<string, EmotionShape>();
			foreach (EmotionShape emotionShape in emotions)
			{
				if (emotionCache.ContainsKey(emotionShape.emotion))
				{
					Debug.LogWarning("[LipSync - " + gameObject.name + "] Project Settings contains more than 1 emotion called \"" + emotionShape.emotion + "\". Duplicates will be ignored.");
				}
				else
				{
					if (setNeutralBonePosesOnStart)
					{
						foreach (BoneShape bone in emotionShape.bones)
						{
							bone.SetNeutral();
						}
					}

					emotionCache.Add(emotionShape.emotion, emotionShape);
				}
			}

			// Check validity of Gestures
			if (gesturesAnimator != null)
			{
				foreach (GestureInstance gesture in gestures)
				{
					if (!gesture.IsValid(gesturesAnimator))
					{
						Debug.LogWarning("[LipSync - " + gameObject.name + "] Animator does not contain a trigger called '" + gesture.triggerName + "'. This Gesture will be ignored.");
					}
				}
			}

			// Start Playing if playOnAwake is set
			if (playOnAwake && defaultClip != null)
				Play(defaultClip, defaultDelay);
		}


		void LateUpdate()
		{
			if ((IsPlaying && !IsPaused) || changingEmotion || IsStopping)
			{
				// Scale audio speed if set
				if (scaleAudioSpeed && !changingEmotion)
					audioSource.pitch = Time.timeScale;

				if (isDelaying)
				{
					customTimer -= Time.deltaTime;
					if (customTimer <= 0)
						isDelaying = false;
					return;
				}

				float normalisedTimer = 0;

				if (IsPlaying || IsStopping)
				{
					// Update timer based on animationTimingMode
					if (animationTimingMode == AnimationTimingMode.AudioPlayback && audioClip != null && IsPlaying)
					{
						// Use AudioSource playback only if an audioClip is present.
						normalisedTimer = audioSource.time / audioClip.length;
					}
					else if (animationTimingMode == AnimationTimingMode.CustomTimer || (animationTimingMode == AnimationTimingMode.AudioPlayback && audioClip == null) || IsStopping)
					{
						// Play at same rate, but don't tie to audioclip. Fallback for AnimationTimingMode.AudioPlayback when no clip is present.
						customTimer += Time.deltaTime;
						normalisedTimer = customTimer / (IsStopping ? restHoldTime : fileLength);
					}
					else if (animationTimingMode == AnimationTimingMode.FixedFrameRate)
					{
						// Play animation at a fixed framerate for offline rendering.
						customTimer += 1f / frameRate;
						normalisedTimer = customTimer / fileLength;
					}


					// Gesture cues
					if (gestures.Count > 0 && nextGesture < gestureMarkers.Count && gesturesAnimator != null && !IsStopping)
					{
						if (normalisedTimer >= gestureMarkers[nextGesture].time)
						{
							// Gesture Cue has been reached
							if (GetGesture(gestureMarkers[nextGesture].gesture) != null)
							{
								gesturesAnimator.SetTrigger(GetGesture(gestureMarkers[nextGesture].gesture).triggerName);
							}
							nextGesture++;
						}
					}
				}
				else
				{
					// Get normalised timer from custom timer
					emotionTimer += Time.deltaTime;
					normalisedTimer = emotionTimer / emotionBlendTime;
				}

				// Go through each animCurve and update blendables
				for (int curve = 0; curve < animCurves.Count; curve++)
				{
					blendSystem.SetBlendableValue(indexBlendables[curve], animCurves[curve].Evaluate(normalisedTimer));
				}

				// Do the same for bones
				if (useBones && boneCurves != null)
				{
					for (int curve = 0; curve < boneCurves.Count; curve++)
					{
						if (boneUpdateAnimation == false)
						{
							bones[curve].localPosition = boneCurves[curve].EvaluatePosition(normalisedTimer);
							bones[curve].localRotation = boneCurves[curve].EvaluateRotation(normalisedTimer);
							bones[curve].localScale = boneCurves[curve].EvaluateScale(normalisedTimer);
						}
						else
						{
							// Get transform relative to current animation frame
							Vector3 newPos = boneCurves[curve].EvaluatePosition(normalisedTimer) - boneNeutralPositions[curve];
							Vector3 newRot = boneCurves[curve].EvaluateRotation(normalisedTimer).eulerAngles - boneNeutralRotations[curve].eulerAngles;
							Vector3 newScale = boneCurves[curve].EvaluateScale(normalisedTimer) - boneNeutralScales[curve];

							bones[curve].localPosition += newPos;
							bones[curve].localEulerAngles += newRot;
							bones[curve].localScale += newScale;
						}
					}
				}

				if (changingEmotion && normalisedTimer > 1)
					changingEmotion = false;

				if ((normalisedTimer >= 0.98f) && !changingEmotion)
				{
					if (IsStopping)
					{
						IsStopping = false;
					}
					else
					{
						if (loop)
						{
							Stop(false);
							Play(lastClip);
						}
						else
						{
							Stop(false);
						}
					}
				}
			}
		}

		// Public Functions

		/// <summary>
		/// Sets the emotion.
		/// Only works when not playing an animation.
		/// </summary>
		/// <param name="emotion">Emotion.</param>
		/// <param name="blendTime">Blend time.</param>
		public void SetEmotion(string emotion, float blendTime)
		{
			if (!IsPlaying && ready && enabled)
			{
				EmotionShape emote = null;

				if (emotion == "")
				{
					emote = new EmotionShape("temp");
				}
				else
				{
					if (emotions.IndexOf(emotionCache[emotion]) == customEmotion)
						return;

					// Get Blendables
					emote = emotionCache[emotion];
				}

				// Init Curves
				animCurves = new List<AnimationCurve>();
				indexBlendables = new List<int>();

				if (useBones)
				{
					boneCurves = new List<TransformAnimationCurve>();
					bones = new List<Transform>();
				}

				for (int b = 0; b < emote.blendShapes.Count; b++)
				{
					indexBlendables.Add(emote.blendShapes[b]);
					animCurves.Add(new AnimationCurve());
				}

				if (useBones)
				{
					for (int b = 0; b < emote.bones.Count; b++)
					{
						bones.Add(emote.bones[b].bone);
						boneCurves.Add(new TransformAnimationCurve());
					}
				}

				if (customEmotion > -1)
				{
					// Add Previous Emotion blendables
					for (int b = 0; b < emotions[customEmotion].blendShapes.Count; b++)
					{
						if (!indexBlendables.Contains(emotions[customEmotion].blendShapes[b]))
						{
							indexBlendables.Add(emotions[customEmotion].blendShapes[b]);
							animCurves.Add(new AnimationCurve());
						}
					}

					if (useBones)
					{
						for (int b = 0; b < emotions[customEmotion].bones.Count; b++)
						{
							if (!bones.Contains(emotions[customEmotion].bones[b].bone))
							{
								bones.Add(emotions[customEmotion].bones[b].bone);
								boneCurves.Add(new TransformAnimationCurve());
							}
						}
					}
				}

				// Get Keys
				if (customEmotion > -1)
				{
					for (int b = 0; b < emotions[customEmotion].blendShapes.Count; b++)
					{
						int matchingCurve = indexBlendables.IndexOf(emotions[customEmotion].blendShapes[b]);
						animCurves[matchingCurve].AddKey(new Keyframe(0, blendSystem.GetBlendableValue(emotions[customEmotion].blendShapes[b]), 90, 0));
					}

					for (int b = 0; b < animCurves.Count; b++)
					{
						if (animCurves[b].keys.Length > 0)
						{
							if (emote.blendShapes.Contains(indexBlendables[b]))
							{
								animCurves[b].AddKey(new Keyframe(1, emote.weights[emote.blendShapes.IndexOf(indexBlendables[b])], 0, 90));
							}
							else
							{
								animCurves[b].AddKey(new Keyframe(1, 0, 0, 90));
							}
						}
						else
						{
							animCurves[b].AddKey(new Keyframe(0, blendSystem.GetBlendableValue(indexBlendables[b]), 90, 0));
							int match = emote.blendShapes.IndexOf(indexBlendables[b]);
							animCurves[b].AddKey(new Keyframe(1, emote.weights[match], 0, 90));
						}
					}

					if (useBones && boneCurves != null)
					{
						for (int b = 0; b < emotions[customEmotion].bones.Count; b++)
						{
							int matchingCurve = bones.IndexOf(emotions[customEmotion].bones[b].bone);
							boneCurves[matchingCurve].AddKey(0, emotions[customEmotion].bones[b].bone.localPosition, emotions[customEmotion].bones[b].bone.localRotation, emotions[customEmotion].bones[b].bone.localScale, 90, 0);
						}

						for (int b = 0; b < boneCurves.Count; b++)
						{
							if (boneCurves[b].length > 0)
							{
								if (emote.HasBone(bones[b]))
								{
									boneCurves[b].AddKey(1, emote.bones[emote.IndexOfBone(bones[b])].endPosition, Quaternion.Euler(emote.bones[emote.IndexOfBone(bones[b])].endRotation), emote.bones[emote.IndexOfBone(bones[b])].endScale, 0, 90);
								}
								else
								{
									boneCurves[b].AddKey(1, emotions[customEmotion].bones[emotions[customEmotion].IndexOfBone(bones[b])].neutralPosition, Quaternion.Euler(emotions[customEmotion].bones[emotions[customEmotion].IndexOfBone(bones[b])].neutralRotation), emotions[customEmotion].bones[emotions[customEmotion].IndexOfBone(bones[b])].neutralScale, 0, 90);
								}
							}
							else
							{
								boneCurves[b].AddKey(0, bones[b].localPosition, bones[b].localRotation, bones[b].localScale, 90, 0);
								int match = emote.IndexOfBone(bones[b]);
								boneCurves[b].AddKey(1, emote.bones[match].endPosition, Quaternion.Euler(emote.bones[match].endRotation), emote.bones[match].endScale, 0, 90);
							}
						}
					}
				}
				else
				{
					for (int b = 0; b < animCurves.Count; b++)
					{
						animCurves[b].AddKey(new Keyframe(0, blendSystem.GetBlendableValue(indexBlendables[b]), 90, 0));
						animCurves[b].AddKey(new Keyframe(1, emote.weights[b], 0, 90));
					}

					if (useBones && boneCurves != null)
					{
						for (int b = 0; b < boneCurves.Count; b++)
						{
							boneCurves[b].AddKey(0, bones[b].localPosition, bones[b].localRotation, bones[b].localScale, 90, 0);
							boneCurves[b].AddKey(1, emote.bones[b].endPosition, Quaternion.Euler(emote.bones[b].endRotation), emote.bones[b].endScale, 0, 90);
						}
					}
				}

				// Fix Quaternion rotations (Credit: Chris Lewis)
				foreach (TransformAnimationCurve curve in boneCurves)
				{
					curve.FixQuaternionContinuity();
				}

				emotionTimer = 0;
				emotionBlendTime = blendTime;
				customEmotion = emotions.IndexOf(emote);
				changingEmotion = true;
			}
		}

		/// <summary>
		/// Resets a custom set emotion back to neutral.
		/// </summary>
		/// <param name="blendTime"></param>
		public void ResetEmotion(float blendTime)
		{
			SetEmotion("", blendTime);
		}

		private void PlayPP(LipSyncData data, float delay, float time)
		{
			if (data.targetComponentID != GetInstanceID())
			{
				Debug.LogWarning("Playing pre-processed clip on a different character. The animation may look incorrect or not play at all. You can remove the pre-processed data by selecting the clip in the Project window.");
			}

			// Standard Data
			phonemeMarkers = new List<PhonemeMarker>(data.phonemeData);
			emotionMarkers = new List<EmotionMarker>(data.emotionData);
			gestureMarkers = new List<GestureMarker>(data.gestureData);
			gestureMarkers.Sort(SortTime);

			lastClip = data;
			currentFileID = data.GetInstanceID();
			audioClip = data.clip;
			fileLength = data.length;
			if (audioSource)
				audioSource.clip = audioClip;

			// Processed Data
			animCurves = new List<AnimationCurve>(data.animCurves);
			indexBlendables = data.indexBlendables;
			bones = data.bones;
			boneCurves = new List<TransformAnimationCurve>(data.boneCurves);
			boneNeutralPositions = data.boneNeutralPositions;
			boneNeutralRotations = data.boneNeutralRotations;
			boneNeutralScales = data.boneNeutralScales;

			if (gesturesAnimator != null && gestures != null)
			{
				if (gestures.Count > 0)
				{
					gesturesAnimator.SetLayerWeight(gesturesLayer, 1);
				}
			}
			else
			{
				if (data.gestureData.Length > 0)
				{
					Debug.Log("[LipSync - " + gameObject.name + "] Animator or Gestures are not set up. Gestures from this clip won't be played.");
				}
			}

			IsPlaying = true;
			IsPaused = false;
			nextGesture = 0;
			IsStopping = false;

			if (audioClip && delay > 0)
			{
				isDelaying = true;
				customTimer = time + delay;
			}
			else
			{
				isDelaying = false;
				customTimer = time;
			}

			// Play audio
			if (audioClip && audioSource)
				audioSource.PlayDelayed(time + delay);
		}

		/// <summary>
		/// Loads a LipSyncData file if necessary and
		/// then plays it on the current LipSync component.
		/// </summary>
		public void Play(LipSyncData dataFile, float delay)
		{
			if (ready && enabled)
			{
				if (dataFile.isPreprocessed)
				{
					PlayPP(dataFile, delay, 0);
					return;
				}

				// Load File if not already loaded
				bool loadSuccessful = true;
				if (dataFile.GetInstanceID() != currentFileID || customEmotion > -1)
				{
					loadSuccessful = LoadData(dataFile);
				}
				if (!loadSuccessful)
					return;

				ProcessData();

				if (gesturesAnimator != null && gestures != null)
				{
					if (gestures.Count > 0)
					{
						gesturesAnimator.SetLayerWeight(gesturesLayer, 1);
					}
				}
				else
				{
					if (dataFile.gestureData.Length > 0)
					{
						Debug.Log("[LipSync - " + gameObject.name + "] Animator or Gestures are not set up. Gestures from this clip won't be played.");
					}
				}

				// Set variables
				IsPlaying = true;
				IsPaused = false;
				nextGesture = 0;
				IsStopping = false;

				if (audioClip && delay > 0)
				{
					isDelaying = true;
					customTimer = delay;
				}
				else
				{
					isDelaying = false;
					customTimer = 0;
				}

				// Play audio
				if (audioClip && audioSource)
					audioSource.PlayDelayed(delay);
			}
		}

		/// <summary>
		/// Overload of Play with no delay specified. For compatibility with pre 0.4 scripts.
		/// </summary>
		public void Play(LipSyncData dataFile)
		{
			Play(dataFile, 0);
		}

		/// <summary>
		/// Loads an XML file and parses LipSync data from it,
		/// then plays it on the current LipSync component.
		/// </summary>
		public void Play(TextAsset xmlFile, AudioClip clip, float delay)
		{
			if (ready && enabled)
			{
				// Load File
				LoadXML(xmlFile, clip);

				if (gesturesAnimator != null)
					gesturesAnimator.SetLayerWeight(gesturesLayer, 1);

				// Set variables
				IsPlaying = true;
				IsPaused = false;
				nextGesture = 0;
				IsStopping = false;

				ProcessData();

				if (audioClip && delay > 0)
				{
					isDelaying = true;
					customTimer = delay;
				}
				else
				{
					isDelaying = false;
					customTimer = 0;
				}

				// Play audio
				audioSource.PlayDelayed(delay);
			}
		}

		/// <summary>
		/// Overload of Play with no delay specified. For compatibility with pre 0.4 scripts.
		/// </summary>
		public void Play(TextAsset xmlFile, AudioClip clip)
		{
			Play(xmlFile, clip, 0);
		}

		/// <summary>
		/// Loads a LipSyncData file if necessary and
		/// then plays it on the current LipSync component
		/// from a certain point in seconds.
		/// </summary>
		public void PlayFromTime(LipSyncData dataFile, float delay, float time)
		{
			if (ready && enabled)
			{
				if (dataFile.isPreprocessed)
				{
					PlayPP(dataFile, delay, time);
					return;
				}

				// Load File if not already loaded
				bool loadSuccessful = true;
				if (dataFile.GetInstanceID() != currentFileID || customEmotion > -1)
				{
					loadSuccessful = LoadData(dataFile);
				}
				if (!loadSuccessful)
					return;

				// Check that time is within range
				if (time >= fileLength)
				{
					Debug.LogError("[LipSync - " + gameObject.name + "] Couldn't play animation. Time parameter is greater than clip length.");
					return;
				}

				ProcessData();

				if (gesturesAnimator != null)
					gesturesAnimator.SetLayerWeight(gesturesLayer, 1);

				// Set variables
				IsPlaying = true;
				IsPaused = false;
				isDelaying = false;
				customTimer = 0;
				nextGesture = 0;
				IsStopping = false;

				// Play audio
				audioSource.Play();
				audioSource.time = time + delay;
			}
		}

		/// <summary>
		/// Overload of PlayFromTime with no delay specified.
		/// </summary>
		public void PlayFromTime(LipSyncData dataFile, float time)
		{
			PlayFromTime(dataFile, 0, time);
		}

		/// <summary>
		/// Loads an XML file and parses LipSync data from it,
		/// then plays it on the current LipSync component
		/// from a certain point in seconds.
		/// </summary>
		public void PlayFromTime(TextAsset xmlFile, AudioClip clip, float delay, float time)
		{
			if (ready && enabled)
			{
				// Load File
				LoadXML(xmlFile, clip);

				// Check that time is within range
				if (time >= fileLength)
				{
					Debug.LogError("[LipSync - " + gameObject.name + "] Couldn't play animation. Time parameter is greater than clip length.");
					return;
				}

				if (gesturesAnimator != null)
					gesturesAnimator.SetLayerWeight(gesturesLayer, 1);

				// Set variables
				IsPlaying = true;
				IsPaused = false;
				isDelaying = false;
				customTimer = 0;
				nextGesture = 0;
				IsStopping = false;

				ProcessData();

				// Play audio
				audioSource.Play();
				audioSource.time = time + delay;
			}
		}

		/// <summary>
		/// Overload of PlayFromTime with no delay specified.
		/// </summary>
		public void PlayFromTime(TextAsset xmlFile, AudioClip clip, float time)
		{
			PlayFromTime(xmlFile, clip, 0, time);
		}

		/// <summary>
		/// Pauses the currently playing animation.
		/// </summary>
		public void Pause()
		{
			if (IsPlaying && !IsPaused && enabled)
			{
				IsPaused = true;
				audioSource.Pause();
			}
		}

		/// <summary>
		/// Resumes the current animation after pausing.
		/// </summary>
		public void Resume()
		{
			if (IsPlaying && IsPaused && enabled)
			{
				IsPaused = false;
				audioSource.UnPause();
			}
		}

		/// <summary>
		/// Completely stops the current animation to be
		/// started again from the begining.
		/// </summary>
		public void Stop(bool stopAudio)
		{
			if (IsPlaying && enabled)
			{
				IsPlaying = false;
				IsPaused = false;
				isDelaying = false;
				IsStopping = true;
				customTimer = 0;

				// Blend out
				for (int c = 0; c < animCurves.Count; c++)
				{
					float finalValue = animCurves[c].Evaluate(1);
					float startingValue = blendSystem.GetBlendableValue(indexBlendables[c]);

					animCurves[c] = new AnimationCurve(new Keyframe[] { new Keyframe(0, startingValue), new Keyframe(1, finalValue) });
				}

				if (useBones)
				{
					for (int b = 0; b < boneCurves.Count; b++)
					{
						Vector3 finalPosition = boneCurves[b].EvaluatePosition(1);
						Vector3 finalScale = boneCurves[b].EvaluateScale(1);
						Quaternion finalRotation = boneCurves[b].EvaluateRotation(1);
						Vector3 startingPosition = bones[b].localPosition;
						Vector3 startingScale = bones[b].localScale;
						Quaternion startingRotation = bones[b].localRotation;

						boneCurves[b] = new TransformAnimationCurve();
						boneCurves[b].AddKey(0, startingPosition, startingRotation, startingScale);
						boneCurves[b].AddKey(1, finalPosition, finalRotation, finalScale);
					}
				}

				// Stop Audio
				if (stopAudio)
					audioSource.Stop();

				//Invoke Callback
				onFinishedPlaying.Invoke();
			}
		}

		/// <summary>
		/// Sets blendables to their state at a certain time in the animation.
		/// ProcessData must have already been called.
		/// </summary>
		/// <param name="time">Time.</param>
		public void PreviewAtTime(float time)
		{
			if (!IsPlaying && enabled && animCurves != null)
			{
				// Sanity check
				if (indexBlendables == null || animCurves == null)
				{
					// Data hasn't been loaded
					if (phonemeMarkers == null || emotionMarkers == null)
						return;

					// Otherwise, recreate animation data
					ProcessData();
				}
				else if (indexBlendables.Count != animCurves.Count)
				{
					// Data hasn't been loaded
					if (phonemeMarkers == null || emotionMarkers == null)
						return;

					// Otherwise, recreate animation data
					ProcessData();
				}

				// Go through each animCurve and update blendables
				for (int curve = 0; curve < animCurves.Count; curve++)
				{
					blendSystem.SetBlendableValue(indexBlendables[curve], animCurves[curve].Evaluate(time));
				}

				if (useBones && boneCurves != null)
				{
					for (int curve = 0; curve < boneCurves.Count; curve++)
					{
						if (bones[curve] != null)
							bones[curve].localPosition = boneCurves[curve].EvaluatePosition(time);
						if (bones[curve] != null)
							bones[curve].localRotation = boneCurves[curve].EvaluateRotation(time);
					}
				}
			}
		}

		public void DisplayEmotionPose(int emotion, float intensity)
		{
			if (useBones)
			{
				foreach (BoneShape boneshape in emotions[emotion].bones)
				{
					if (boneshape.bone != null)
					{
						boneshape.bone.localPosition = Vector3.Lerp(boneshape.neutralPosition, boneshape.endPosition, intensity);
						boneshape.bone.localEulerAngles = Quaternion.Slerp(Quaternion.Euler(boneshape.neutralRotation), Quaternion.Euler(boneshape.endRotation), intensity).eulerAngles;
						boneshape.bone.localScale = Vector3.Lerp(boneshape.neutralScale, boneshape.endScale, intensity);
					}
				}
			}

			for (int b = 0; b < emotions[emotion].blendShapes.Count; b++)
			{
				blendSystem.SetBlendableValue(emotions[emotion].blendShapes[b], emotions[emotion].weights[b] * intensity);
			}
		}

		public void ResetDisplayedEmotions()
		{
			foreach (EmotionShape shape in emotions)
			{
				if (useBones)
				{
					foreach (BoneShape boneshape in shape.bones)
					{
						if (boneshape.bone != null)
						{
							boneshape.bone.localPosition = boneshape.neutralPosition;
							boneshape.bone.localEulerAngles = boneshape.neutralRotation;
							boneshape.bone.localScale = boneshape.neutralScale;
						}
					}
				}

				foreach (int blendable in shape.blendShapes)
				{
					blendSystem.SetBlendableValue(blendable, 0);
				}
			}
		}

		public void PreviewAudioAtTime(float time, float length)
		{
			if (IsPlaying || !audioSource)
				return;

			if (!audioSource.isPlaying)
			{
				audioSource.PlayOneShot(audioClip);
				if (time <= 1)
					audioSource.time = time * audioClip.length;
				StartCoroutine(StopAudioSource(length));
			}
		}

		/// <summary>
		/// Loads raw data instead of using a serialised asset.
		/// Used for previewing animations in the editor.
		/// </summary>
		/// <param name="pData">Phoneme data.</param>
		/// <param name="eData">Emotion data.</param>
		/// <param name="clip">Audio Clip.</param>
		/// <param name="duration">File Duration.</param>
		public void TempLoad(List<PhonemeMarker> pData, List<EmotionMarker> eData, AudioClip clip, float duration)
		{
			TempLoad(pData.ToArray(), eData.ToArray(), clip, duration);
		}

		/// <summary>
		/// Loads raw data instead of using a serialised asset.
		/// Used for previewing animations in the editor.
		/// </summary>
		/// <param name="pData">Phoneme data.</param>
		/// <param name="eData">Emotion data.</param>
		/// <param name="clip">Audio Clip.</param>
		/// <param name="duration">File Duration.</param>
		public void TempLoad(PhonemeMarker[] pData, EmotionMarker[] eData, AudioClip clip, float duration)
		{
			if (enabled)
			{
				if (emotionCache == null)
				{
					// Cache Emotions for more performant cross-checking
					emotionCache = new Dictionary<string, EmotionShape>();
					foreach (EmotionShape emotionShape in emotions)
					{
						emotionCache.Add(emotionShape.emotion, emotionShape);
					}
				}

				// Clear/define marker lists, to overwrite any previous file
				phonemeMarkers = new List<PhonemeMarker>();
				emotionMarkers = new List<EmotionMarker>();

				// Copy data from file into new lists
				foreach (PhonemeMarker marker in pData)
				{
					phonemeMarkers.Add(marker);
				}
				foreach (EmotionMarker marker in eData)
				{
					emotionMarkers.Add(marker);
				}

				// Phonemes are stored out of sequence in the file, for depth sorting in the editor
				// Sort them by timestamp to make finding the current one faster
				phonemeMarkers.Sort(SortTime);

				audioClip = clip;
				fileLength = duration;
			}
		}

		/// <summary>
		/// Processes the data into readable animation curves.
		/// Do not call before loading data.
		/// </summary>
		public void ProcessData(bool emotionOnly = false)
		{
			if (enabled)
			{

				#region Setup/Definition

				boneNeutralPositions = null;
				boneNeutralRotations = null;
				boneNeutralScales = null;

				List<Transform> tempEmotionBones = null;
				List<TransformAnimationCurve> tempEmotionBoneCurves = null;

				List<Transform> tempBones = null;
				List<TransformAnimationCurve> tempBoneCurves = null;

				List<int> tempEmotionIndexBlendables = new List<int>();
				List<AnimationCurve> tempEmotionCurves = new List<AnimationCurve>();

				List<int> tempIndexBlendables = new List<int>();
				List<AnimationCurve> tempCurves = new List<AnimationCurve>();

				Dictionary<int, float> blendableNeutralValues = new Dictionary<int, float>();
				PhonemeShape restPhoneme = null;
				for (int i = 0; i < phonemes.Count; i++)
				{
					if (phonemes[i].phonemeName.ToLowerInvariant() == "rest")
						restPhoneme = phonemes[i];
				}

				indexBlendables = new List<int>();
				animCurves = new List<AnimationCurve>();

				phonemeMarkers.Sort(SortTime);

				if (useBones)
				{
					boneNeutralPositions = new List<Vector3>();
					boneNeutralRotations = new List<Quaternion>();
					boneNeutralScales = new List<Vector3>();

					bones = new List<Transform>();
					boneCurves = new List<TransformAnimationCurve>();

					tempBones = new List<Transform>();
					tempBoneCurves = new List<TransformAnimationCurve>();

					tempEmotionBones = new List<Transform>();
					tempEmotionBoneCurves = new List<TransformAnimationCurve>();
				}

				List<Shape> shapes = new List<Shape>();
				#endregion

				if (!emotionOnly)
				{
					#region Get Phoneme Info
					// Add phonemes used
					foreach (PhonemeMarker marker in phonemeMarkers)
					{
						if (shapes.Count == phonemes.Count)
						{
							break;
						}

						if (!shapes.Contains(phonemes[marker.phonemeNumber]))
						{
							shapes.Add(phonemes[marker.phonemeNumber]);

							foreach (int blendable in phonemes[marker.phonemeNumber].blendShapes)
							{
								if (!tempIndexBlendables.Contains(blendable))
								{
									AnimationCurve curve = new AnimationCurve();
									curve.postWrapMode = WrapMode.Once;
									tempCurves.Add(curve);
									tempIndexBlendables.Add(blendable);
								}

								if (!indexBlendables.Contains(blendable))
								{
									AnimationCurve curve = new AnimationCurve();
									curve.postWrapMode = WrapMode.Once;
									animCurves.Add(curve);
									indexBlendables.Add(blendable);
								}

								if (!blendableNeutralValues.ContainsKey(blendable))
								{
									blendableNeutralValues.Add(blendable, 0);
								}
							}

							if (useBones && boneCurves != null)
							{
								foreach (BoneShape boneShape in phonemes[marker.phonemeNumber].bones)
								{
									if (!tempBones.Contains(boneShape.bone))
									{
										TransformAnimationCurve curve = new TransformAnimationCurve();
										curve.postWrapMode = WrapMode.Once;
										tempBoneCurves.Add(curve);
										tempBones.Add(boneShape.bone);
									}

									if (!bones.Contains(boneShape.bone))
									{
										TransformAnimationCurve curve = new TransformAnimationCurve();
										curve.postWrapMode = WrapMode.Once;
										boneCurves.Add(curve);
										bones.Add(boneShape.bone);

										boneNeutralPositions.Add(boneShape.neutralPosition);
										boneNeutralRotations.Add(Quaternion.Euler(boneShape.neutralRotation.ToNegativeEuler()));
										boneNeutralScales.Add(boneShape.neutralScale);
									}
								}
							}
						}
					}
					#endregion
				}

				#region Get Emotion Info
				// Add emotions used
				foreach (EmotionMarker marker in emotionMarkers)
				{

					if (marker.isMixer)
					{
						for (int i = 0; i < marker.mixer.emotions.Count; i++)
						{
							if (!shapes.Contains(emotionCache[marker.mixer.emotions[i].emotion]))
							{
								if (emotionCache.ContainsKey(marker.mixer.emotions[i].emotion))
								{
									shapes.Add(emotionCache[marker.mixer.emotions[i].emotion]);

									foreach (int blendable in emotionCache[marker.mixer.emotions[i].emotion].blendShapes)
									{
										if (!tempEmotionIndexBlendables.Contains(blendable))
										{
											AnimationCurve curve = new AnimationCurve();
											curve.postWrapMode = WrapMode.Once;
											tempEmotionCurves.Add(curve);
											tempEmotionIndexBlendables.Add(blendable);
										}

										if (!indexBlendables.Contains(blendable))
										{
											AnimationCurve curve = new AnimationCurve();
											curve.postWrapMode = WrapMode.Once;
											animCurves.Add(curve);
											indexBlendables.Add(blendable);
										}

										if (!blendableNeutralValues.ContainsKey(blendable))
										{
											blendableNeutralValues.Add(blendable, 0);
										}
									}

									if (useBones && boneCurves != null)
									{
										foreach (BoneShape boneShape in emotionCache[marker.mixer.emotions[i].emotion].bones)
										{
											if (!tempEmotionBones.Contains(boneShape.bone))
											{
												TransformAnimationCurve curve = new TransformAnimationCurve();
												curve.postWrapMode = WrapMode.Once;
												tempEmotionBoneCurves.Add(curve);
												tempEmotionBones.Add(boneShape.bone);
											}

											if (!bones.Contains(boneShape.bone))
											{
												TransformAnimationCurve curve = new TransformAnimationCurve();
												curve.postWrapMode = WrapMode.Once;
												boneCurves.Add(curve);
												bones.Add(boneShape.bone);

												boneNeutralPositions.Add(boneShape.neutralPosition);
												boneNeutralRotations.Add(Quaternion.Euler(boneShape.neutralRotation.ToNegativeEuler()));
												boneNeutralScales.Add(boneShape.neutralScale);
											}
										}
									}
								}
							}
						}
					}
					else if (emotionCache.ContainsKey(marker.emotion))
					{
						if (emotionCache.ContainsKey(marker.emotion))
						{
							if (!shapes.Contains(emotionCache[marker.emotion]))
							{
								shapes.Add(emotionCache[marker.emotion]);

								foreach (int blendable in emotionCache[marker.emotion].blendShapes)
								{
									if (!tempEmotionIndexBlendables.Contains(blendable))
									{
										AnimationCurve curve = new AnimationCurve();
										curve.postWrapMode = WrapMode.Once;
										tempEmotionCurves.Add(curve);
										tempEmotionIndexBlendables.Add(blendable);
									}

									if (!indexBlendables.Contains(blendable))
									{
										AnimationCurve curve = new AnimationCurve();
										curve.postWrapMode = WrapMode.Once;
										animCurves.Add(curve);
										indexBlendables.Add(blendable);
									}

									if (!blendableNeutralValues.ContainsKey(blendable))
									{
										blendableNeutralValues.Add(blendable, 0);
									}
								}

								if (useBones && boneCurves != null)
								{
									foreach (BoneShape boneShape in emotionCache[marker.emotion].bones)
									{
										if (!tempEmotionBones.Contains(boneShape.bone))
										{
											TransformAnimationCurve curve = new TransformAnimationCurve();
											curve.postWrapMode = WrapMode.Once;
											tempEmotionBoneCurves.Add(curve);
											tempEmotionBones.Add(boneShape.bone);
										}

										if (!bones.Contains(boneShape.bone))
										{
											TransformAnimationCurve curve = new TransformAnimationCurve();
											curve.postWrapMode = WrapMode.Once;
											boneCurves.Add(curve);
											bones.Add(boneShape.bone);

											boneNeutralPositions.Add(boneShape.neutralPosition);
											boneNeutralRotations.Add(Quaternion.Euler(boneShape.neutralRotation.ToNegativeEuler()));
											boneNeutralScales.Add(boneShape.neutralScale);
										}
									}
								}
							}
						}
					}
					else
					{
						emotionMarkers.Remove(marker);
						break;
					}
				}
				#endregion

				if (!emotionOnly)
				{
					#region Extras (SetEmotion, Rest pose)
					// Add current set emotion if applicable
					if (customEmotion > -1)
					{
						if (!shapes.Contains(emotions[customEmotion]))
						{
							shapes.Add(emotions[customEmotion]);

							foreach (int blendable in emotions[customEmotion].blendShapes)
							{
								if (!tempEmotionIndexBlendables.Contains(blendable))
								{
									AnimationCurve curve = new AnimationCurve();
									curve.postWrapMode = WrapMode.Once;
									tempEmotionCurves.Add(curve);
									tempEmotionIndexBlendables.Add(blendable);
								}

								if (!indexBlendables.Contains(blendable))
								{
									AnimationCurve curve = new AnimationCurve();
									curve.postWrapMode = WrapMode.Once;
									animCurves.Add(curve);
									indexBlendables.Add(blendable);
								}

								if (!blendableNeutralValues.ContainsKey(blendable))
								{
									blendableNeutralValues.Add(blendable, 0);
								}
							}

							if (useBones && boneCurves != null)
							{
								foreach (BoneShape boneShape in emotions[customEmotion].bones)
								{
									if (!tempEmotionBones.Contains(boneShape.bone))
									{
										TransformAnimationCurve curve = new TransformAnimationCurve();
										curve.postWrapMode = WrapMode.Once;
										tempEmotionBoneCurves.Add(curve);
										tempEmotionBones.Add(boneShape.bone);
									}

									if (!bones.Contains(boneShape.bone))
									{
										TransformAnimationCurve curve = new TransformAnimationCurve();
										curve.postWrapMode = WrapMode.Once;
										boneCurves.Add(curve);
										bones.Add(boneShape.bone);

										boneNeutralPositions.Add(boneShape.neutralPosition);
										boneNeutralRotations.Add(Quaternion.Euler(boneShape.neutralRotation.ToNegativeEuler()));
										boneNeutralScales.Add(boneShape.neutralScale);
									}
								}
							}
						}
					}

					// Add any blendable not otherwise used, that appear in the rest phoneme
					if (restPhoneme != null)
					{
						foreach (int blendable in restPhoneme.blendShapes)
						{
							if (!tempIndexBlendables.Contains(blendable))
							{
								AnimationCurve curve = new AnimationCurve();
								curve.postWrapMode = WrapMode.Once;
								tempCurves.Add(curve);
								tempIndexBlendables.Add(blendable);
							}

							if (!indexBlendables.Contains(blendable))
							{
								AnimationCurve curve = new AnimationCurve();
								curve.postWrapMode = WrapMode.Once;
								animCurves.Add(curve);
								indexBlendables.Add(blendable);
							}

							if (!blendableNeutralValues.ContainsKey(blendable))
							{
								blendableNeutralValues.Add(blendable, 0);
							}
						}

						if (useBones && boneCurves != null)
						{
							foreach (BoneShape boneShape in restPhoneme.bones)
							{
								if (!tempBones.Contains(boneShape.bone))
								{
									TransformAnimationCurve curve = new TransformAnimationCurve();
									curve.postWrapMode = WrapMode.Once;
									tempBoneCurves.Add(curve);
									tempBones.Add(boneShape.bone);
								}

								if (!bones.Contains(boneShape.bone))
								{
									TransformAnimationCurve curve = new TransformAnimationCurve();
									curve.postWrapMode = WrapMode.Once;
									boneCurves.Add(curve);
									bones.Add(boneShape.bone);

									boneNeutralPositions.Add(boneShape.neutralPosition);
									boneNeutralRotations.Add(Quaternion.Euler(boneShape.neutralRotation.ToNegativeEuler()));
									boneNeutralScales.Add(boneShape.neutralScale);
								}
							}
						}
					}

					// Get neutral values
					for (int i = 0; i < indexBlendables.Count; i++)
					{
						if (restPhoneme != null)
						{
							if (restPhoneme.blendShapes.Contains(indexBlendables[i]))
							{
								blendableNeutralValues[indexBlendables[i]] = restPhoneme.weights[restPhoneme.blendShapes.IndexOf(indexBlendables[i])];
							}
							else
							{
								blendableNeutralValues[indexBlendables[i]] = 0;
							}
						}
						else
						{
							blendableNeutralValues[indexBlendables[i]] = 0;
						}
					}

					if (useBones && boneCurves != null)
					{
						for (int i = 0; i < bones.Count; i++)
						{
							if (restPhoneme != null)
							{
								if (restPhoneme.HasBone(bones[i]))
								{
									boneNeutralPositions[i] = restPhoneme.bones[restPhoneme.IndexOfBone(bones[i])].endPosition;
									boneNeutralRotations[i] = Quaternion.Euler(restPhoneme.bones[restPhoneme.IndexOfBone(bones[i])].endRotation);
									boneNeutralScales[i] = restPhoneme.bones[restPhoneme.IndexOfBone(bones[i])].endScale;
								}
							}
						}
					}
					#endregion

					#region Add Start & End Keys
					// Add neutral start and end keys, or get keys from current custom emotion
					for (int index = 0; index < tempCurves.Count; index++)
					{
						if (customEmotion == -1)
							tempCurves[index].AddKey(0, blendableNeutralValues[tempIndexBlendables[index]]);
						if (!keepEmotionWhenFinished)
							tempCurves[index].AddKey(1, blendableNeutralValues[tempIndexBlendables[index]]);
					}

					for (int index = 0; index < tempEmotionCurves.Count; index++)
					{
						if (customEmotion > -1)
						{
							if (emotions[customEmotion].blendShapes.Contains(tempEmotionIndexBlendables[index]))
							{
								tempEmotionCurves[index].AddKey(0, emotions[customEmotion].weights[emotions[customEmotion].blendShapes.IndexOf(tempEmotionIndexBlendables[index])]);
							}
							else
							{
								tempEmotionCurves[index].AddKey(0, blendableNeutralValues[tempEmotionIndexBlendables[index]]);
							}
						}
						else
						{
							tempEmotionCurves[index].AddKey(0, blendableNeutralValues[tempEmotionIndexBlendables[index]]);
						}

						// Only add end emotion key if keepEmotionWhenFinished is false
						if (!keepEmotionWhenFinished)
						{
							tempEmotionCurves[index].AddKey(1, blendableNeutralValues[tempEmotionIndexBlendables[index]]);
						}
						else if (customEmotion > -1)
						{
							if (emotions[customEmotion].blendShapes.Contains(tempEmotionIndexBlendables[index]))
							{
								tempEmotionCurves[index].AddKey(1, emotions[customEmotion].weights[emotions[customEmotion].blendShapes.IndexOf(tempEmotionIndexBlendables[index])]);
							}
						}
					}

					if (useBones && boneCurves != null)
					{
						for (int index = 0; index < tempBoneCurves.Count; index++)
						{
							if (customEmotion == -1)
								tempBoneCurves[index].AddKey(0, boneNeutralPositions[bones.IndexOf(tempBones[index])], boneNeutralRotations[bones.IndexOf(tempBones[index])], boneNeutralScales[bones.IndexOf(tempBones[index])], 0, 0);
							if (!keepEmotionWhenFinished)
								tempBoneCurves[index].AddKey(1, boneNeutralPositions[bones.IndexOf(tempBones[index])], boneNeutralRotations[bones.IndexOf(tempBones[index])], boneNeutralScales[bones.IndexOf(tempBones[index])], 0, 0);
						}

						for (int index = 0; index < tempEmotionBoneCurves.Count; index++)
						{
							if (customEmotion > -1)
							{
								if (emotions[customEmotion].HasBone(tempEmotionBones[index]))
								{
									tempEmotionBoneCurves[index].AddKey(
										0,
										emotions[customEmotion].bones[emotions[customEmotion].IndexOfBone(tempEmotionBones[index])].endPosition,
										Quaternion.Euler(emotions[customEmotion].bones[emotions[customEmotion].IndexOfBone(tempEmotionBones[index])].endRotation),
										emotions[customEmotion].bones[emotions[customEmotion].IndexOfBone(tempEmotionBones[index])].endScale,
										0,
										0
									);
								}
								else
								{
									tempEmotionBoneCurves[index].AddKey(0, boneNeutralPositions[bones.IndexOf(tempEmotionBones[index])], boneNeutralRotations[bones.IndexOf(tempEmotionBones[index])], boneNeutralScales[bones.IndexOf(tempEmotionBones[index])], 0, 0);
								}
							}
							else
							{
								tempEmotionBoneCurves[index].AddKey(0, boneNeutralPositions[bones.IndexOf(tempEmotionBones[index])], boneNeutralRotations[bones.IndexOf(tempEmotionBones[index])], boneNeutralScales[bones.IndexOf(tempEmotionBones[index])], 0, 0);
							}

							if (!keepEmotionWhenFinished)
							{
								tempEmotionBoneCurves[index].AddKey(1, boneNeutralPositions[bones.IndexOf(tempEmotionBones[index])], boneNeutralRotations[bones.IndexOf(tempEmotionBones[index])], boneNeutralScales[bones.IndexOf(tempEmotionBones[index])], 0, 0);
							}
							else if (customEmotion > -1)
							{
								if (emotions[customEmotion].HasBone(tempEmotionBones[index]))
								{
									tempEmotionBoneCurves[index].AddKey(
										1,
										emotions[customEmotion].bones[emotions[customEmotion].IndexOfBone(tempEmotionBones[index])].endPosition,
										Quaternion.Euler(emotions[customEmotion].bones[emotions[customEmotion].IndexOfBone(tempEmotionBones[index])].endRotation),
										emotions[customEmotion].bones[emotions[customEmotion].IndexOfBone(tempEmotionBones[index])].endScale,
										0,
										0
									);
								}
							}
						}
					}
					#endregion
				}

				#region Add Emotion Marker Keys
				// Get temp keys from emotion markers
				foreach (EmotionMarker marker in emotionMarkers)
				{
					EmotionShape shape = null;

					int variationPoints = marker.continuousVariation ? Mathf.Clamp(Mathf.FloorToInt((((marker.endTime - marker.startTime) - (marker.blendInTime - marker.blendOutTime)) * fileLength) / marker.variationFrequency), 2, 128) : 2;
					float[] intensityMod = new float[variationPoints];
					float[] blendableMod = new float[variationPoints];
					float[] bonePosMod = new float[variationPoints];
					float[] boneRotMod = new float[variationPoints];

					// Get Random Modifiers
					for (int i = 0; i < variationPoints; i++)
					{
						if (marker.continuousVariation)
						{
							intensityMod[i] = Random.Range(1 - (marker.intensityVariation / 2), 1 + (marker.intensityVariation / 2));
						}
						else
						{
							intensityMod[i] = 1;
							blendableMod[i] = 1;
							bonePosMod[i] = 1;
							boneRotMod[i] = 1;
						}
					}

					if (marker.isMixer)
					{
						shape = marker.mixer.GetShape(this);
					}
					else
					{
						shape = emotionCache[marker.emotion];
					}

					for (int index = 0; index < tempEmotionCurves.Count; index++)
					{
						if (shape.blendShapes.Contains(tempEmotionIndexBlendables[index]))
						{
							int b = shape.blendShapes.IndexOf(tempEmotionIndexBlendables[index]);

							float startWeight = blendableNeutralValues[tempEmotionIndexBlendables[index]];
							float endWeight = blendableNeutralValues[tempEmotionIndexBlendables[index]];

							if (marker.continuousVariation)
							{
								for (int i = 0; i < variationPoints; i++)
								{
									blendableMod[i] = Random.Range(1 - (marker.blendableVariation / 2), 1 + (marker.blendableVariation / 2));
								}
							}
							if (marker.blendFromMarker)
							{
								EmotionMarker prevMarker = emotionMarkers[emotionMarkers.IndexOf(marker) - 1];
								EmotionShape prevShape = null;

								if (prevMarker.isMixer)
								{
									prevShape = prevMarker.mixer.GetShape(this);
								}
								else
								{
									prevShape = emotionCache[prevMarker.emotion];
								}

								// Check if previous emotion used this blendable.
								if (prevShape.blendShapes.Contains(tempEmotionIndexBlendables[index]))
								{
									startWeight = prevShape.weights[prevShape.blendShapes.IndexOf(tempEmotionIndexBlendables[index])] * prevMarker.intensity;
								}
							}

							if (marker.blendToMarker)
							{
								EmotionMarker nextMarker = emotionMarkers[emotionMarkers.IndexOf(marker) + 1];
								EmotionShape nextShape = null;

								if (nextMarker.isMixer)
								{
									nextShape = nextMarker.mixer.GetShape(this);
								}
								else
								{
									nextShape = emotionCache[nextMarker.emotion];
								}

								// Check if next emotion uses this blendable.
								if (nextShape.blendShapes.Contains(tempEmotionIndexBlendables[index]))
								{
									endWeight = nextShape.weights[nextShape.blendShapes.IndexOf(tempEmotionIndexBlendables[index])] * nextMarker.intensity;
								}
							}

							if (emotionCurveGenerationMode == CurveGenerationMode.Tight)
							{
								tempEmotionCurves[index].AddKey(new Keyframe(marker.startTime, startWeight, 0, 0));
								for (int i = 0; i < variationPoints; i++)
								{
									float time = Mathf.Lerp(marker.startTime + marker.blendInTime, marker.endTime + marker.blendOutTime, (float)i / (float)(variationPoints - 1));
									tempEmotionCurves[index].AddKey(new Keyframe(time, shape.weights[b] * marker.intensity * intensityMod[i] * blendableMod[i], 0, 0));
								}
								tempEmotionCurves[index].AddKey(new Keyframe(marker.endTime, endWeight, 0, 0));
							}
							else if (emotionCurveGenerationMode == CurveGenerationMode.Loose)
							{
								tempEmotionCurves[index].AddKey(marker.startTime, startWeight);
								for (int i = 0; i < variationPoints; i++)
								{
									float time = Mathf.Lerp(marker.startTime + marker.blendInTime, marker.endTime + marker.blendOutTime, (float)i / (float)(variationPoints - 1));
									tempEmotionCurves[index].AddKey(time, shape.weights[b] * marker.intensity * intensityMod[i] * blendableMod[i]);
								}
								tempEmotionCurves[index].AddKey(marker.endTime, endWeight);
							}

						}
						else
						{
							if (emotionCurveGenerationMode == CurveGenerationMode.Tight)
							{
								tempEmotionCurves[index].AddKey(new Keyframe(marker.startTime + marker.blendInTime, 0, 0, 0));
								tempEmotionCurves[index].AddKey(new Keyframe(marker.endTime + marker.blendOutTime, 0, 0, 0));
							}
							else if (emotionCurveGenerationMode == CurveGenerationMode.Loose)
							{
								tempEmotionCurves[index].AddKey(marker.startTime + marker.blendInTime, 0);
								tempEmotionCurves[index].AddKey(marker.endTime + marker.blendOutTime, 0);
							}

							if (marker.blendToMarker)
							{
								EmotionMarker nextMarker = emotionMarkers[emotionMarkers.IndexOf(marker) + 1];
								EmotionShape nextShape = null;

								if (nextMarker.isMixer)
								{
									nextShape = nextMarker.mixer.GetShape(this);
								}
								else
								{
									nextShape = emotionCache[nextMarker.emotion];
								}

								// Check if next emotion uses this blendable.
								if (nextShape.blendShapes.Contains(tempEmotionIndexBlendables[index]))
								{
									if (emotionCurveGenerationMode == CurveGenerationMode.Tight)
									{
										tempEmotionCurves[index].AddKey(new Keyframe(marker.endTime, nextShape.weights[nextShape.blendShapes.IndexOf(tempEmotionIndexBlendables[index])] * nextMarker.intensity, 0, 0));
									}
									else if (emotionCurveGenerationMode == CurveGenerationMode.Loose)
									{
										tempEmotionCurves[index].AddKey(marker.endTime, nextShape.weights[nextShape.blendShapes.IndexOf(tempEmotionIndexBlendables[index])] * nextMarker.intensity);
									}
								}
							}
						}
					}

					if (useBones && boneCurves != null)
					{
						for (int index = 0; index < tempEmotionBoneCurves.Count; index++)
						{
							if (shape.HasBone(tempEmotionBones[index]))
							{
								int b = shape.IndexOfBone(tempEmotionBones[index]);

								if (marker.continuousVariation)
								{
									for (int i = 0; i < variationPoints; i++)
									{
										bonePosMod[i] = Random.Range(1 - (marker.bonePositionVariation / 2), 1 + (marker.bonePositionVariation / 2));
										boneRotMod[i] = Random.Range(1 - (marker.boneRotationVariation / 2), 1 + (marker.boneRotationVariation / 2));
									}
								}

								Vector3 startPosition = shape.bones[b].neutralPosition;
								Quaternion startRotation = Quaternion.Euler(shape.bones[b].neutralRotation);
								Vector3 startScale = shape.bones[b].neutralScale;
								Vector3 endPosition = shape.bones[b].neutralPosition;
								Quaternion endRotation = Quaternion.Euler(shape.bones[b].neutralRotation);
								Vector3 endScale = shape.bones[b].neutralScale;

								if (marker.blendFromMarker)
								{
									EmotionMarker prevMarker = emotionMarkers[emotionMarkers.IndexOf(marker) - 1];
									EmotionShape prevShape = null;

									if (prevMarker.isMixer)
									{
										prevShape = prevMarker.mixer.GetShape(this);
									}
									else
									{
										prevShape = emotionCache[prevMarker.emotion];
									}

									// Check if previous emotion used this blendable.
									if (prevShape.HasBone(tempEmotionBones[index]))
									{
										startPosition = Vector3.Lerp(startPosition, prevShape.bones[prevShape.IndexOfBone(tempEmotionBones[index])].endPosition, prevMarker.intensity);
										startRotation = Quaternion.Slerp(startRotation, Quaternion.Euler(prevShape.bones[prevShape.IndexOfBone(tempEmotionBones[index])].endRotation), prevMarker.intensity);
										startScale = Vector3.Lerp(startScale, prevShape.bones[prevShape.IndexOfBone(tempEmotionBones[index])].endScale, prevMarker.intensity);
									}
								}

								if (marker.blendToMarker)
								{
									EmotionMarker nextMarker = emotionMarkers[emotionMarkers.IndexOf(marker) + 1];
									EmotionShape nextShape = null;

									if (nextMarker.isMixer)
									{
										nextShape = nextMarker.mixer.GetShape(this);
									}
									else
									{
										nextShape = emotionCache[nextMarker.emotion];
									}

									// Check if next emotion uses this blendable.
									if (nextShape.HasBone(tempEmotionBones[index]))
									{
										endPosition = Vector3.Lerp(endPosition, nextShape.bones[nextShape.IndexOfBone(tempEmotionBones[index])].endPosition, nextMarker.intensity);
										endRotation = Quaternion.Slerp(endRotation, Quaternion.Euler(nextShape.bones[nextShape.IndexOfBone(tempEmotionBones[index])].endRotation), nextMarker.intensity);
										endScale = Vector3.Lerp(endScale, nextShape.bones[nextShape.IndexOfBone(tempEmotionBones[index])].endScale, nextMarker.intensity);
									}
								}

								tempEmotionBoneCurves[index].AddKey(marker.startTime, startPosition, startRotation, startScale, 0, 0);
								for (int i = 0; i < variationPoints; i++)
								{
									float time = Mathf.Lerp(marker.startTime + marker.blendInTime, marker.endTime + marker.blendOutTime, (float)i / (float)(variationPoints - 1));
									tempEmotionBoneCurves[index].AddKey(time, Vector3.Lerp(shape.bones[b].neutralPosition, shape.bones[b].endPosition * bonePosMod[i], marker.intensity * intensityMod[i]), Quaternion.Slerp(Quaternion.Euler(shape.bones[b].neutralRotation), Quaternion.Euler(shape.bones[b].endRotation * boneRotMod[i]), marker.intensity * intensityMod[i]), Vector3.Lerp(shape.bones[b].neutralScale, shape.bones[b].endScale, marker.intensity * intensityMod[i]), 0, 0);
								}
								tempEmotionBoneCurves[index].AddKey(marker.endTime, endPosition, endRotation, endScale, 0, 0);

							}
							else
							{
								tempEmotionBoneCurves[index].AddKey(marker.startTime + marker.blendInTime, boneNeutralPositions[bones.IndexOf(tempEmotionBones[index])], boneNeutralRotations[bones.IndexOf(tempEmotionBones[index])], boneNeutralScales[bones.IndexOf(tempEmotionBones[index])], 0, 0);
								tempEmotionBoneCurves[index].AddKey(marker.endTime + marker.blendOutTime, boneNeutralPositions[bones.IndexOf(tempEmotionBones[index])], boneNeutralRotations[bones.IndexOf(tempEmotionBones[index])], boneNeutralScales[bones.IndexOf(tempEmotionBones[index])], 0, 0);

								if (marker.blendToMarker)
								{
									EmotionMarker nextMarker = emotionMarkers[emotionMarkers.IndexOf(marker) + 1];
									EmotionShape nextShape = null;

									if (nextMarker.isMixer)
									{
										nextShape = nextMarker.mixer.GetShape(this);
									}
									else
									{
										nextShape = emotionCache[nextMarker.emotion];
									}

									// Check if next emotion uses this blendable.
									if (nextShape.HasBone(tempEmotionBones[index]))
									{
										BoneShape b = nextShape.bones[nextShape.IndexOfBone(tempEmotionBones[index])];
										tempEmotionBoneCurves[index].AddKey(marker.endTime, Vector3.Lerp(b.neutralPosition, b.endPosition, nextMarker.intensity), Quaternion.Slerp(Quaternion.Euler(b.neutralRotation), Quaternion.Euler(b.endRotation), nextMarker.intensity), Vector3.Lerp(b.neutralScale, b.endScale, nextMarker.intensity), 0, 0);
									}
								}
							}
						}
					}
				}
				#endregion

				if (!emotionOnly)
				{
					#region Add Phoneme Marker Keys
					// Get keys from phoneme track
					for (int m = 0; m < phonemeMarkers.Count; m++)
					{
						PhonemeMarker marker = phonemeMarkers[m];
						PhonemeShape shape = phonemes[marker.phonemeNumber];

						float intensityMod = 1;
						float blendableMod = 1;
						float bonePosMod = 1;
						float boneRotMod = 1;

						// Get Random Modifier
						if (marker.useRandomness)
						{
							intensityMod = Random.Range(1 - (marker.intensityRandomness / 2), 1 + (marker.intensityRandomness / 2));
						}

						bool addRest = false;

						// Check for rests
						if (!marker.sustain)
						{
							if (m + 1 < phonemeMarkers.Count)
							{
								if (phonemeMarkers[m + 1].time > marker.time + (restTime / fileLength) + (restHoldTime / fileLength))
								{
									addRest = true;
								}
							}
							else
							{
								// Last marker, add rest after hold time
								addRest = true;
							}
						}

						for (int index = 0; index < tempCurves.Count; index++)
						{
							if (shape.blendShapes.Contains(tempIndexBlendables[index]))
							{
								int b = shape.blendShapes.IndexOf(tempIndexBlendables[index]);

								// Get Random Other Modifiers
								if (marker.useRandomness)
								{
									blendableMod = Random.Range(1 - (marker.blendableRandomness / 2), 1 + (marker.blendableRandomness / 2));
								}

								if (phonemeCurveGenerationMode == CurveGenerationMode.Tight)
								{
									tempCurves[index].AddKey(new Keyframe(marker.time, shape.weights[b] * marker.intensity * intensityMod * blendableMod, 0, 0));

									//Check for pre-rest
									if (m == 0)
									{
										tempCurves[index].AddKey(new Keyframe(phonemeMarkers[m].time - (restHoldTime / fileLength), blendableNeutralValues[tempIndexBlendables[index]], 0, 0));
									}

									if (addRest)
									{
										// Add rest
										tempCurves[index].AddKey(new Keyframe(marker.time + (restHoldTime / fileLength), shape.weights[b] * marker.intensity * intensityMod * blendableMod, 0, 0));
										tempCurves[index].AddKey(new Keyframe(marker.time + ((restHoldTime / fileLength) * 2), blendableNeutralValues[tempIndexBlendables[index]], 0, 0));
										if (m + 1 < phonemeMarkers.Count)
										{
											tempCurves[index].AddKey(new Keyframe(phonemeMarkers[m + 1].time - (restHoldTime / fileLength), blendableNeutralValues[tempIndexBlendables[index]], 0, 0));
										}
									}
								}
								else if (phonemeCurveGenerationMode == CurveGenerationMode.Loose)
								{
									tempCurves[index].AddKey(marker.time, shape.weights[b] * marker.intensity);

									//Check for pre-rest
									if (m == 0)
									{
										tempCurves[index].AddKey(phonemeMarkers[m].time - (restHoldTime / fileLength), blendableNeutralValues[tempIndexBlendables[index]]);
									}

									if (addRest)
									{
										// Add rest
										tempCurves[index].AddKey(marker.time + (restHoldTime / fileLength), shape.weights[b] * marker.intensity * intensityMod * blendableMod);
										tempCurves[index].AddKey(marker.time + ((restHoldTime / fileLength) * 2), blendableNeutralValues[tempIndexBlendables[index]]);
										if (m + 1 < phonemeMarkers.Count)
										{
											tempCurves[index].AddKey(phonemeMarkers[m + 1].time - (restHoldTime / fileLength), blendableNeutralValues[tempIndexBlendables[index]]);
										}
									}
								}

							}
							else
							{
								// Blendable isn't in this marker
								if (phonemeCurveGenerationMode == CurveGenerationMode.Tight)
								{
									tempCurves[index].AddKey(new Keyframe(marker.time, blendableNeutralValues[tempIndexBlendables[index]], 0, 0));
								}
								else if (phonemeCurveGenerationMode == CurveGenerationMode.Loose)
								{
									tempCurves[index].AddKey(marker.time, blendableNeutralValues[tempIndexBlendables[index]]);
								}
								if (addRest)
								{
									if (m + 1 < phonemeMarkers.Count)
									{
										if (phonemeCurveGenerationMode == CurveGenerationMode.Tight)
										{
											tempCurves[index].AddKey(new Keyframe(phonemeMarkers[m + 1].time - (restHoldTime / fileLength), blendableNeutralValues[tempIndexBlendables[index]], 0, 0));
										}
										else if (phonemeCurveGenerationMode == CurveGenerationMode.Loose)
										{
											tempCurves[index].AddKey(phonemeMarkers[m + 1].time - (restHoldTime / fileLength), blendableNeutralValues[tempIndexBlendables[index]]);
										}
									}
								}
							}
						}

						if (useBones && boneCurves != null)
						{
							for (int index = 0; index < tempBoneCurves.Count; index++)
							{
								if (shape.HasBone(bones[index]))
								{
									int b = shape.IndexOfBone(bones[index]);

									// Get Random Other Modifiers
									if (marker.useRandomness)
									{
										bonePosMod = Random.Range(1 - (marker.bonePositionRandomness / 2), 1 + (marker.bonePositionRandomness / 2));
										boneRotMod = Random.Range(1 - (marker.boneRotationRandomness / 2), 1 + (marker.boneRotationRandomness / 2));
									}

									tempBoneCurves[index].AddKey(marker.time, Vector3.Lerp(shape.bones[b].neutralPosition, shape.bones[b].endPosition * bonePosMod, marker.intensity * intensityMod), Quaternion.Slerp(Quaternion.Euler(shape.bones[b].neutralRotation), Quaternion.Euler(shape.bones[b].endRotation * boneRotMod), marker.intensity), Vector3.Lerp(shape.bones[b].neutralScale, shape.bones[b].endScale, marker.intensity * intensityMod), 0, 0);

									//Check for pre-rest
									if (m == 0)
									{
										tempBoneCurves[index].AddKey(phonemeMarkers[m].time - (restHoldTime / fileLength), boneNeutralPositions[index], boneNeutralRotations[index], boneNeutralScales[index], 0, 0);
									}

									if (addRest)
									{
										// Add rest
										tempBoneCurves[index].AddKey(marker.time + (restHoldTime / fileLength), boneNeutralPositions[index], boneNeutralRotations[index], boneNeutralScales[index], 0, 0);
										if (m + 1 < phonemeMarkers.Count)
										{
											tempBoneCurves[index].AddKey(phonemeMarkers[m + 1].time - (restHoldTime / fileLength), boneNeutralPositions[index], boneNeutralRotations[index], boneNeutralScales[index], 0, 0);
										}
									}
								}
								else
								{
									// Blendable isn't in this marker, get value from matching emotion curve if available

									tempBoneCurves[index].AddKey(marker.time, boneNeutralPositions[index], boneNeutralRotations[index], boneNeutralScales[index], 0, 0);

									if (addRest)
									{
										if (m + 1 < phonemeMarkers.Count)
										{
											tempBoneCurves[index].AddKey(phonemeMarkers[m + 1].time - (restHoldTime / fileLength), boneNeutralPositions[index], boneNeutralRotations[index], boneNeutralScales[index], 0, 0);
										}
									}
								}
							}
						}
					}
					#endregion
				}

				#region Composite Animation
				// Merge curve sets
				for (int c = 0; c < animCurves.Count; c++)
				{
					if (tempIndexBlendables.Contains(indexBlendables[c]) && tempEmotionIndexBlendables.Contains(indexBlendables[c]))
					{
						int pIndex = tempIndexBlendables.IndexOf(indexBlendables[c]);
						int eIndex = tempEmotionIndexBlendables.IndexOf(indexBlendables[c]);

						for (int k = 0; k < tempCurves[pIndex].keys.Length; k++)
						{
							Keyframe key = tempCurves[pIndex].keys[k];
							animCurves[c].AddKey(key);
						}

						for (int k = 0; k < tempEmotionCurves[eIndex].keys.Length; k++)
						{
							Keyframe eKey = tempEmotionCurves[eIndex].keys[k];
							animCurves[c].AddKey(eKey);
						}

					}
					else if (tempIndexBlendables.Contains(indexBlendables[c]))
					{
						int pIndex = tempIndexBlendables.IndexOf(indexBlendables[c]);

						for (int k = 0; k < tempCurves[pIndex].keys.Length; k++)
						{
							Keyframe key = tempCurves[pIndex].keys[k];
							animCurves[c].AddKey(key);
						}

					}
					else
					{
						int eIndex = tempEmotionIndexBlendables.IndexOf(indexBlendables[c]);

						for (int k = 0; k < tempEmotionCurves[eIndex].keys.Length; k++)
						{
							Keyframe eKey = tempEmotionCurves[eIndex].keys[k];
							animCurves[c].AddKey(eKey);
						}
					}
				}

				if (useBones && boneCurves != null)
				{
					for (int c = 0; c < boneCurves.Count; c++)
					{
						if (tempBones.Contains(bones[c]) && tempEmotionBones.Contains(bones[c]))
						{
							int pIndex = tempBones.IndexOf(bones[c]);
							int eIndex = tempEmotionBones.IndexOf(bones[c]);

							foreach (TransformAnimationCurve.TransformKeyframe key in tempBoneCurves[pIndex].keys)
							{
								boneCurves[c].AddKey(key.time, key.position, key.rotation, key.scale, 0, 0);
							}

							foreach (TransformAnimationCurve.TransformKeyframe key in tempEmotionBoneCurves[eIndex].keys)
							{
								boneCurves[c].AddKey(key.time, key.position, key.rotation, key.scale, 0, 0);
							}

						}
						else if (tempBones.Contains(bones[c]))
						{
							int pIndex = tempBones.IndexOf(bones[c]);

							foreach (TransformAnimationCurve.TransformKeyframe key in tempBoneCurves[pIndex].keys)
							{
								boneCurves[c].AddKey(key.time, key.position, key.rotation, key.scale, 0, 0);
							}
						}
						else
						{
							int eIndex = tempEmotionBones.IndexOf(bones[c]);

							foreach (TransformAnimationCurve.TransformKeyframe key in tempEmotionBoneCurves[eIndex].keys)
							{
								boneCurves[c].AddKey(key.time, key.position, key.rotation, key.scale, 0, 0);
							}
						}
					}

					// Fix Quaternion rotations (Credit: Chris Lewis)
					foreach (TransformAnimationCurve curve in boneCurves)
					{
						curve.FixQuaternionContinuity();
					}
				}
				#endregion
			}

			if (!emotionOnly && customEmotion > -1)
			{
				ClearDataCache();
				customEmotion = -1;
			}
		}

		public void GetCurveDataOut(out List<int> indexBlendables, out List<AnimationCurve> animCurves, out List<Transform> bones, out List<TransformAnimationCurve> boneCurves, out List<Vector3> boneNeutralPositions, out List<Quaternion> boneNeutralRotations, out List<Vector3> boneNeutralScales)
		{
			indexBlendables = this.indexBlendables;
			animCurves = this.animCurves;
			bones = this.bones;
			boneCurves = this.boneCurves;
			boneNeutralPositions = this.boneNeutralPositions;
			boneNeutralRotations = this.boneNeutralRotations;
			boneNeutralScales = this.boneNeutralScales;
		}

		/// <summary>
		/// Clears the data cache, forcing the animation curves to be recalculated.
		/// </summary>
		public void ClearDataCache()
		{
			currentFileID = 0;
		}

		// -----------------
		// Private Functions
		// -----------------

		private EmotionMixer GetTransitionMixer(string oldEmotion, string newEmotion, float t)
		{
			var mixer = new EmotionMixer();

			mixer.mixingMode = EmotionMixer.MixingMode.Additive;
			mixer.emotions.Add(new EmotionMixer.EmotionComponent(oldEmotion, 1f - t));
			mixer.emotions.Add(new EmotionMixer.EmotionComponent(newEmotion, t));

			return mixer;
		}

		void FixEmotionBlends(ref List<EmotionMarker> data)
		{
			EmotionMarker[] markers = data.ToArray();
			FixEmotionBlends(ref markers);
			data.Clear();

			foreach (EmotionMarker marker in markers)
			{
				data.Add(marker);
			}
		}

		void FixEmotionBlends(ref EmotionMarker[] data)
		{

			foreach (EmotionMarker eMarker in data)
			{
				eMarker.blendFromMarker = false;
				eMarker.blendToMarker = false;
				if (!eMarker.customBlendIn)
					eMarker.blendInTime = 0;
				if (!eMarker.customBlendOut)
					eMarker.blendOutTime = 0;
				eMarker.invalid = false;
			}

			foreach (EmotionMarker eMarker in data)
			{
				foreach (EmotionMarker tMarker in data)
				{
					if (eMarker != tMarker)
					{
						if (eMarker.startTime > tMarker.startTime && eMarker.startTime < tMarker.endTime)
						{
							if (eMarker.customBlendIn)
							{
								eMarker.customBlendIn = false;
								FixEmotionBlends(ref data);
								return;
							}
							eMarker.blendFromMarker = true;

							if (eMarker.endTime > tMarker.startTime && eMarker.endTime < tMarker.endTime)
							{
								eMarker.invalid = true;
							}
							else
							{
								eMarker.blendInTime = tMarker.endTime - eMarker.startTime;
							}
						}

						if (eMarker.endTime > tMarker.startTime && eMarker.endTime < tMarker.endTime)
						{
							if (eMarker.customBlendOut)
							{
								eMarker.customBlendOut = false;
								FixEmotionBlends(ref data);
								return;
							}
							eMarker.blendToMarker = true;

							if (eMarker.startTime > tMarker.startTime && eMarker.startTime < tMarker.endTime)
							{
								eMarker.invalid = true;
							}
							else
							{
								eMarker.blendOutTime = tMarker.startTime - eMarker.endTime;
							}
						}
					}
				}
			}
		}

		private void LoadXML(TextAsset xmlFile, AudioClip linkedClip)
		{
#if UNITY_WP_8_1 || UNITY_WSA
			Debug.LogWarning("[LipSync - " + gameObject.name + "] XML loading is not supported on Windows Store platforms.");
#else
			XmlDocument document = new XmlDocument();
			document.LoadXml(xmlFile.text);

			// Clear/define marker lists, to overwrite any previous file
			phonemeMarkers = new List<PhonemeMarker>();
			emotionMarkers = new List<EmotionMarker>();
			gestureMarkers = new List<GestureMarker>();

			audioClip = linkedClip;
			audioSource.clip = audioClip;

			string version = ReadXML(document, "LipSyncData", "version");

			if (float.Parse(version) < 1.321f)
			{
				Debug.LogError("Cannot load pre-1.321 XML file. Run the converter from Window/Rogo Digital/LipSync Pro/Update XML files.");
				return;
			}

			try
			{
				fileLength = float.Parse(ReadXML(document, "LipSyncData", "length"));

				//Phonemes
				XmlNode phonemesNode = document.SelectSingleNode("//LipSyncData//phonemes");
				if (phonemesNode != null)
				{
					XmlNodeList phonemeNodes = phonemesNode.ChildNodes;

					for (int p = 0; p < phonemeNodes.Count; p++)
					{
						XmlNode node = phonemeNodes[p];

						if (node.LocalName == "marker")
						{
							int phoneme = int.Parse(node.Attributes["phonemeNumber"].Value);
							float time = float.Parse(node.Attributes["time"].Value) / fileLength;
							float intensity = float.Parse(node.Attributes["intensity"].Value);
							bool sustain = bool.Parse(node.Attributes["sustain"].Value);

							phonemeMarkers.Add(new PhonemeMarker(phoneme, time, intensity, sustain));
						}
					}
				}

				//Emotions
				XmlNode emotionsNode = document.SelectSingleNode("//LipSyncData//emotions");
				if (emotionsNode != null)
				{
					XmlNodeList emotionNodes = emotionsNode.ChildNodes;

					for (int p = 0; p < emotionNodes.Count; p++)
					{
						XmlNode node = emotionNodes[p];

						if (node.LocalName == "marker")
						{
							string emotion = node.Attributes["emotion"].Value;
							float startTime = float.Parse(node.Attributes["start"].Value) / fileLength;
							float endTime = float.Parse(node.Attributes["end"].Value) / fileLength;
							float blendInTime = float.Parse(node.Attributes["blendIn"].Value);
							float blendOutTime = float.Parse(node.Attributes["blendOut"].Value);
							bool blendTo = bool.Parse(node.Attributes["blendToMarker"].Value);
							bool blendFrom = bool.Parse(node.Attributes["blendFromMarker"].Value);
							bool customBlendIn = bool.Parse(node.Attributes["customBlendIn"].Value);
							bool customBlendOut = bool.Parse(node.Attributes["customBlendOut"].Value);
							float intensity = float.Parse(node.Attributes["intensity"].Value);

							emotionMarkers.Add(new EmotionMarker(emotion, startTime, endTime, blendInTime, blendOutTime, blendTo, blendFrom, customBlendIn, customBlendOut, intensity));
						}
					}
				}

				//Gestures
				XmlNode gesturesNode = document.SelectSingleNode("//LipSyncData//gestures");
				if (gesturesNode != null)
				{
					XmlNodeList gestureNodes = gesturesNode.ChildNodes;

					for (int p = 0; p < gestureNodes.Count; p++)
					{
						XmlNode node = gestureNodes[p];

						if (node.LocalName == "marker")
						{
							string gesture = node.Attributes["gesture"].Value;
							float time = float.Parse(node.Attributes["time"].Value) / fileLength;

							gestureMarkers.Add(new GestureMarker(gesture, time));
						}
					}
				}
			}
			catch
			{
				Debug.LogError("[LipSync - " + gameObject.name + "] Malformed XML file. See console for details. \nFor the sake of simplicity, LipSync Pro is unable to handle errors in XML files. The clip editor often can, however. Import this XML file into the clip editor and re-export to fix.");
			}

			phonemeMarkers.Sort(SortTime);
			gestureMarkers.Sort(SortTime);
#endif
		}

		private bool LoadData(LipSyncData dataFile)
		{
			// Check that the referenced file contains data
			if (dataFile.phonemeData.Length > 0 || dataFile.emotionData.Length > 0 || dataFile.gestureData.Length > 0)
			{
				// Store reference to the associated AudioClip.
				audioClip = dataFile.clip;
				fileLength = dataFile.length;

				// Update file to current format if needed
				bool updated = false;

				if (dataFile.version < 1)
				{
					// Pre 1.0 - update emotion blends to new format.
					updated = true;

					for (int e = 0; e < dataFile.emotionData.Length; e++)
					{
						if (dataFile.emotionData[e].blendFromMarker)
						{
							dataFile.emotionData[e].startTime -= dataFile.emotionData[e].blendInTime;
							dataFile.emotionData[e - 1].endTime += dataFile.emotionData[e].blendInTime;
						}
						else
						{
							dataFile.emotionData[e].customBlendIn = true;
						}

						if (dataFile.emotionData[e].blendToMarker)
						{
							dataFile.emotionData[e + 1].startTime -= dataFile.emotionData[e].blendOutTime;
							dataFile.emotionData[e].endTime += dataFile.emotionData[e].blendOutTime;
						}
						else
						{
							dataFile.emotionData[e].customBlendOut = true;
							dataFile.emotionData[e].blendOutTime = -dataFile.emotionData[e].blendOutTime;
						}
					}

					FixEmotionBlends(ref dataFile.emotionData);

					if (dataFile.length == 0)
					{
						fileLength = audioClip.length;
					}
				}

				if (dataFile.version < 1.3f)
				{
					// Pre 1.3 - update enum-based phoneme IDs
					updated = true;
					for (int p = 0; p < dataFile.phonemeData.Length; p++)
					{
						dataFile.phonemeData[p].phonemeNumber = (int)dataFile.phonemeData[p].phoneme;
					}
				}

				if (updated)
					Debug.LogWarning("[LipSync - " + gameObject.name + "] Loading data from an old format LipSyncData file. For better performance, open this clip in the Clip Editor and re-save to update.");

				// Clear/define marker lists, to overwrite any previous file
				phonemeMarkers = new List<PhonemeMarker>();
				emotionMarkers = new List<EmotionMarker>();
				gestureMarkers = new List<GestureMarker>();

				// Copy data from file into new lists
				foreach (PhonemeMarker marker in dataFile.phonemeData)
				{
					phonemeMarkers.Add(marker);
				}
				foreach (EmotionMarker marker in dataFile.emotionData)
				{
					emotionMarkers.Add(marker);
				}
				foreach (GestureMarker marker in dataFile.gestureData)
				{
					gestureMarkers.Add(marker);
				}

				// Phonemes are stored out of sequence in the file, for depth sorting in the editor
				// Sort them by timestamp to make finding the current one faster
				emotionMarkers.Sort(EmotionSort);
				phonemeMarkers.Sort(SortTime);
				gestureMarkers.Sort(SortTime);

				// Set current AudioClip in the AudioSource
				audioSource.clip = audioClip;

				// Save file InstanceID for later, to skip loading data that is already loaded
				currentFileID = dataFile.GetInstanceID();
				lastClip = dataFile;

				return true;
			}
			else
			{
				return false;
			}
		}

		private IEnumerator StopAudioSource(float delay)
		{
			yield return new WaitForSeconds(delay);
			audioSource.Stop();
		}

		GestureInstance GetGesture(string name)
		{
			for (int a = 0; a < gestures.Count; a++)
			{
				if (gestures[a].gesture == name)
					return gestures[a];
			}
			return null;
		}

		public LipSync()
		{
			// Constructor used to set version value on new component
			this.lastUsedVersion = 1.531f;
		}

		// Sort PhonemeMarker by timestamp
		public static int SortTime(PhonemeMarker a, PhonemeMarker b)
		{
			float sa = a.time;
			float sb = b.time;

			return sa.CompareTo(sb);
		}

		public static int SortTime(GestureMarker a, GestureMarker b)
		{
			float sa = a.time;
			float sb = b.time;

			return sa.CompareTo(sb);
		}

		static int EmotionSort(EmotionMarker a, EmotionMarker b)
		{
			return a.startTime.CompareTo(b.startTime);
		}

		public static string ReadXML(XmlDocument xml, string parentElement, string elementName)
		{
#if UNITY_WP_8_1 || UNITY_WSA
			return null;
#else
			XmlNode node = xml.SelectSingleNode("//" + parentElement + "//" + elementName);

			if (node == null)
			{
				return null;
			}

			return node.InnerText;
#endif
		}

		public enum AnimationTimingMode
		{
			AudioPlayback,
			CustomTimer,
			FixedFrameRate,
		}

		public enum CurveGenerationMode
		{
			Tight,
			Loose,
		}
	}

	// Old Phoneme List
	public enum Phoneme
	{
		AI,
		E,
		U,
		O,
		CDGKNRSThYZ,
		FV,
		L,
		MBP,
		WQ,
		Rest
	}
}