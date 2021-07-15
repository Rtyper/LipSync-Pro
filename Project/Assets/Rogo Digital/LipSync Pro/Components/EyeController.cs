using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;
using System.Collections.Generic;
using RogoDigital.Lipsync;

namespace RogoDigital
{
	[AddComponentMenu("Rogo Digital/Eye Controller")]

	/// <summary>
	/// Provides automatic randomised blinking and looking for characters.
	/// Note: Component is due for a major overhaul next version. Expect better performance + cleaner code.
	/// </summary>
	public class EyeController : BlendSystemUser
	{

		/// <summary>
		/// Is blinking enabled.
		/// </summary>
		public bool blinkingEnabled = false;

		/// <summary>
		/// Whether to use legacy-style single blendable blinking, or the new blinking pose.
		/// </summary>
		public ControlMode blinkingControlMode = ControlMode.Classic;

		/// <summary>
		/// A generic pose for blinking. 
		/// </summary>
		public Shape blinkingShape;

		/// <summary>
		/// The left eye blink blendable index.
		/// </summary>
		[FormerlySerializedAs("leftEyeBlinkBlendshape")]
		public int leftEyeBlinkBlendable = 0;

		/// <summary>
		/// The right eye blink blendable index.
		/// </summary>
		[FormerlySerializedAs("rightEyeBlinkBlendshape")]
		public int rightEyeBlinkBlendable = 1;

		/// <summary>
		/// The minimum time between blinks.
		/// </summary>
		public float minimumBlinkGap = 1;

		/// <summary>
		/// The maximum time between blinks.
		/// </summary>
		public float maximumBlinkGap = 4;

		/// <summary>
		/// How long each blink takes.
		/// </summary>
		[FormerlySerializedAs("blinkSpeed")]
		public float blinkDuration = 0.14f;

		/// <summary>
		/// Keeps the eyes closed.
		/// </summary>
		public bool keepEyesClosed
		{
			get
			{
				return _keepEyesClosed;
			}
			set
			{
				if (value == true)
				{

					if (_keepEyesClosed != value)
						StartCoroutine(CloseEyes());
				}
				else
				{
					if (_keepEyesClosed != value)
						StartCoroutine(OpenEyes());
				}

				_keepEyesClosed = value;
			}
		}

		/// <summary>
		/// Is random looking enabled.
		/// </summary>
		public bool randomLookingEnabled = false;

		/// <summary>
		/// Whether to use legacy-style bone-based looking, or the new looking poses.
		/// </summary>
		public ControlMode lookingControlMode = ControlMode.Classic;

		/// <summary>
		/// A generic pose for looking up. 
		/// </summary>
		public Shape lookingUpShape;

		/// <summary>
		/// A generic pose for looking down. 
		/// </summary>
		public Shape lookingDownShape;

		/// <summary>
		/// A generic pose for looking left. 
		/// </summary>
		public Shape lookingLeftShape;

		/// <summary>
		/// A generic pose for looking right. 
		/// </summary>
		public Shape lookingRightShape;

		/// <summary>
		/// Transform for the left eye.
		/// </summary>
		public Transform LeftEyeLookAtBone
		{
			get
			{
				return _leftEyeLookAtBone;
			}
			set
			{
				if (_leftEyeLookAtBone == value)
					return;
				_leftEyeLookAtBone = value;
				if (Application.isPlaying)
					FixDummyHierarchy();
			}
		}
		[SerializeField, FormerlySerializedAs("leftEyeLookAtBone")]
		private Transform _leftEyeLookAtBone;

		/// <summary>
		/// Transform for the right eye.
		/// </summary>
		public Transform RightEyeLookAtBone
		{
			get
			{
				return _rightEyeLookAtBone;
			}
			set
			{
				if (_rightEyeLookAtBone == value)
					return;
				_rightEyeLookAtBone = value;
				if (Application.isPlaying)
					FixDummyHierarchy();
			}
		}
		[SerializeField, FormerlySerializedAs("rightEyeLookAtBone")]
		private Transform _rightEyeLookAtBone;

		/// <summary>
		/// The eye rotation range along the X axis.
		/// </summary>
		public Vector2 eyeRotationRangeX = new Vector2(-6.5f, 6.5f);

		/// <summary>
		/// The eye rotation range along the Y axis.
		/// </summary>
		public Vector2 eyeRotationRangeY = new Vector2(-17.2f, 17.2f);

		/// <summary>
		/// The eye look offset.
		/// </summary>
		public Vector3 eyeLookOffset;

		/// <summary>
		/// Vector3 describing the forward axis of the eye bones.
		/// </summary>
		public Axis eyeForwardAxis = Axis.Z_Positive;

		/// <summary>
		/// The eye turn speed.
		/// </summary>
		public float eyeTurnSpeed = 18;

		/// <summary>
		/// The minimum time between look direction changes.
		/// </summary>
		public float minimumChangeDirectionGap = 2;

		/// <summary>
		/// The maximum time between look direction changes.
		/// </summary>
		public float maximumChangeDirectionGap = 10;

		/// <summary>
		/// Is look targeting enabled.
		/// </summary>
		public bool targetEnabled = false;

		/// <summary>
		/// Should targets be found automatically.
		/// </summary>
		public bool autoTarget = false;

		/// <summary>
		/// Tag to use when looking for targets.
		/// </summary>
		public string autoTargetTag = "EyeControllerTarget";

		/// <summary>
		/// The maximum distance between a target and the character for it to be targeted.
		/// </summary>
		public float autoTargetDistance = 10;

		/// <summary>
		/// Transform to look at.
		/// </summary>
		public Transform viewTarget;

		/// <summary>
		/// The target weight.
		/// </summary>
		public float targetWeight = 1;

		/// <summary>
		/// Used for deciding if/when to repose boneshapes in LateUpdate.
		/// </summary>
		public bool boneUpdateAnimation = false;

		// Blinking
		private float blinkTimer;
		private bool blinking = false;

		// keepEyesClosed backing field
		private bool _keepEyesClosed = false;
		private bool _asyncBlending = false;

		// Shared Looking
		private Transform leftEyeDummy;
		private Transform rightEyeDummy;
		private Quaternion leftRotation;
		private Quaternion rightRotation;
		private Vector3[] axisOffsets = { new Vector3(0, -90, 0), new Vector3(0, 90, 0), new Vector3(90, 0, 0), new Vector3(-90, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 180, 0) };

		// Random Look
		private float lookTimer;
		private Quaternion randomAngle;
		private Vector2 randomBlend;

		// Look Target
		private Transform target;
		private Quaternion leftTargetAngle;
		private Quaternion rightTargetAngle;

		private Transform[] markedTargets;
		private Dictionary<Transform, BoneShapeInfo> boneShapes;

		void Start ()
		{
			// Get Starting Info
			randomAngle = Quaternion.identity;
			leftTargetAngle = Quaternion.identity;
			rightTargetAngle = Quaternion.identity;

			if (LeftEyeLookAtBone != null && RightEyeLookAtBone != null)
			{
				leftRotation = LeftEyeLookAtBone.rotation;
				rightRotation = RightEyeLookAtBone.rotation;
			}

			if (targetEnabled && autoTarget)
			{
				FindTargets();
			}

			// Create dummy eye transforms
			leftEyeDummy = new GameObject("Left Eye Dummy").transform;
			rightEyeDummy = new GameObject("Right Eye Dummy").transform;
			leftEyeDummy.gameObject.hideFlags = HideFlags.DontSave;
			rightEyeDummy.gameObject.hideFlags = HideFlags.DontSave;

			FixDummyHierarchy();

			// Populate BoneShapeInfo list
			boneShapes = new Dictionary<Transform, BoneShapeInfo>();

			if (blinkingControlMode == ControlMode.PoseBased)
			{
				foreach (BoneShape bone in blinkingShape.bones)
				{
					if (!boneShapes.ContainsKey(bone.bone))
						boneShapes.Add(bone.bone, new BoneShapeInfo(bone));
				}
			}

			if (lookingControlMode == ControlMode.PoseBased)
			{
				foreach (BoneShape bone in lookingUpShape.bones)
				{
					if (!boneShapes.ContainsKey(bone.bone))
						boneShapes.Add(bone.bone, new BoneShapeInfo(bone));
				}
				foreach (BoneShape bone in lookingDownShape.bones)
				{
					if (!boneShapes.ContainsKey(bone.bone))
						boneShapes.Add(bone.bone, new BoneShapeInfo(bone));
				}
				foreach (BoneShape bone in lookingLeftShape.bones)
				{
					if (!boneShapes.ContainsKey(bone.bone))
						boneShapes.Add(bone.bone, new BoneShapeInfo(bone));
				}
				foreach (BoneShape bone in lookingRightShape.bones)
				{
					if (!boneShapes.ContainsKey(bone.bone))
						boneShapes.Add(bone.bone, new BoneShapeInfo(bone));
				}
			}
		}

		void LateUpdate ()
		{
			if (!leftEyeDummy || !rightEyeDummy)
			{
				FixDummyHierarchy();
			}

			// Blinking
			if (blinkingEnabled && blendSystem != null && !keepEyesClosed && !_asyncBlending)
			{
				if (blendSystem.isReady)
				{
					if (blinking)
					{
						float halfBlinkSpeed = blinkDuration / 2;

						if (blinkTimer < halfBlinkSpeed)
						{
							if (blinkingControlMode == ControlMode.Classic)
							{
								blendSystem.SetBlendableValue(leftEyeBlinkBlendable, Mathf.Lerp(0, 100, blinkTimer / halfBlinkSpeed));
								blendSystem.SetBlendableValue(rightEyeBlinkBlendable, Mathf.Lerp(0, 100, blinkTimer / halfBlinkSpeed));

							}
							else if (blinkingControlMode == ControlMode.PoseBased)
							{
								for (int b = 0; b < blinkingShape.blendShapes.Count; b++)
								{
									blendSystem.SetBlendableValue(blinkingShape.blendShapes[b], Mathf.Lerp(0, blinkingShape.weights[b], blinkTimer / halfBlinkSpeed));
								}

								for (int b = 0; b < blinkingShape.bones.Count; b++)
								{
									if (boneUpdateAnimation)
									{
										Vector3 newPos = Vector3.Lerp(boneShapes[blinkingShape.bones[b].bone].storedPosition, blinkingShape.bones[b].endPosition, blinkTimer / halfBlinkSpeed) - blinkingShape.bones[b].neutralPosition;
										Vector3 newRot = Vector3.Lerp(boneShapes[blinkingShape.bones[b].bone].storedRotation.eulerAngles, blinkingShape.bones[b].endRotation, blinkTimer / halfBlinkSpeed) - blinkingShape.bones[b].neutralRotation;

										if (!blinkingShape.bones[b].lockPosition)
											blinkingShape.bones[b].bone.localPosition += newPos;
										if (!blinkingShape.bones[b].lockRotation)
											blinkingShape.bones[b].bone.localEulerAngles += newRot;
									}
									else
									{
										if (!blinkingShape.bones[b].lockPosition)
											blinkingShape.bones[b].bone.localPosition = Vector3.Lerp(boneShapes[blinkingShape.bones[b].bone].storedPosition, blinkingShape.bones[b].endPosition, blinkTimer / halfBlinkSpeed);
										if (!blinkingShape.bones[b].lockRotation)
											blinkingShape.bones[b].bone.localEulerAngles = Vector3.Lerp(boneShapes[blinkingShape.bones[b].bone].storedRotation.eulerAngles, blinkingShape.bones[b].endRotation, blinkTimer / halfBlinkSpeed);
									}
								}
							}
						}
						else
						{
							if (blinkingControlMode == ControlMode.Classic)
							{
								blendSystem.SetBlendableValue(leftEyeBlinkBlendable, Mathf.Lerp(100, 0, (blinkTimer - halfBlinkSpeed) / halfBlinkSpeed));
								blendSystem.SetBlendableValue(rightEyeBlinkBlendable, Mathf.Lerp(100, 0, (blinkTimer - halfBlinkSpeed) / halfBlinkSpeed));
							}
							else if (blinkingControlMode == ControlMode.PoseBased)
							{
								for (int b = 0; b < blinkingShape.blendShapes.Count; b++)
								{
									blendSystem.SetBlendableValue(blinkingShape.blendShapes[b], Mathf.Lerp(blinkingShape.weights[b], 0, (blinkTimer - halfBlinkSpeed) / halfBlinkSpeed));
								}

								for (int b = 0; b < blinkingShape.bones.Count; b++)
								{
									if (boneUpdateAnimation)
									{
										Vector3 newPos = Vector3.Lerp(blinkingShape.bones[b].endPosition, boneShapes[blinkingShape.bones[b].bone].storedPosition, (blinkTimer - halfBlinkSpeed) / halfBlinkSpeed) - blinkingShape.bones[b].neutralPosition;
										Vector3 newRot = Vector3.Lerp(blinkingShape.bones[b].endRotation, boneShapes[blinkingShape.bones[b].bone].storedRotation.eulerAngles, (blinkTimer - halfBlinkSpeed) / halfBlinkSpeed) - blinkingShape.bones[b].neutralRotation;

										if (!blinkingShape.bones[b].lockPosition)
											blinkingShape.bones[b].bone.localPosition += newPos;
										if (!blinkingShape.bones[b].lockRotation)
											blinkingShape.bones[b].bone.localEulerAngles += newRot;
									}
									else
									{
										if (!blinkingShape.bones[b].lockPosition)
											blinkingShape.bones[b].bone.localPosition = Vector3.Lerp(blinkingShape.bones[b].endPosition, boneShapes[blinkingShape.bones[b].bone].storedPosition, (blinkTimer - halfBlinkSpeed) / halfBlinkSpeed);
										if (!blinkingShape.bones[b].lockRotation)
											blinkingShape.bones[b].bone.localEulerAngles = Vector3.Lerp(blinkingShape.bones[b].endRotation, boneShapes[blinkingShape.bones[b].bone].storedRotation.eulerAngles, (blinkTimer - halfBlinkSpeed) / halfBlinkSpeed);
									}
								}
							}

							if (blinkTimer > blinkDuration)
							{
								blinking = false;
								blinkTimer = Random.Range(minimumBlinkGap, maximumBlinkGap);
							}
						}

						blinkTimer += Time.deltaTime;
					}
					else
					{
						if (blinkTimer <= 0)
						{
							blinking = true;
							blinkTimer = 0;
						}
						else
						{
							blinkTimer -= Time.deltaTime;
						}
					}
				}
			}

			// Look Target
			if (targetEnabled && lookingControlMode != ControlMode.PoseBased && leftEyeDummy != null && rightEyeDummy != null)
			{
				// Auto Target
				if (autoTarget)
				{
					try
					{
						float targetDistance = autoTargetDistance;
						target = null;
						for (int i = 0; i < markedTargets.Length; i++)
						{
							if (Vector3.Distance(transform.position, markedTargets[i].position) < targetDistance)
							{
								targetDistance = Vector3.Distance(transform.position, markedTargets[i].position);
								target = markedTargets[i];
							}
						}
					}
					catch (System.NullReferenceException)
					{
						FindTargets();
					}
				}
				else
				{
					target = viewTarget;
				}

				if (target != null)
				{
					Vector3 llta = leftEyeDummy.parent.InverseTransformEulerAngle((Quaternion.LookRotation(target.position - leftEyeDummy.position)).eulerAngles).ToNegativeEuler();
					Vector3 lrta = rightEyeDummy.parent.InverseTransformEulerAngle((Quaternion.LookRotation(target.position - rightEyeDummy.position)).eulerAngles).ToNegativeEuler();

					llta = new Vector3(Mathf.Clamp(llta.x, eyeRotationRangeX.x, eyeRotationRangeX.y), Mathf.Clamp(llta.y, eyeRotationRangeY.x, eyeRotationRangeY.y), 0) + eyeLookOffset;
					lrta = new Vector3(Mathf.Clamp(lrta.x, eyeRotationRangeX.x, eyeRotationRangeX.y), Mathf.Clamp(lrta.y, eyeRotationRangeY.x, eyeRotationRangeY.y), 0) + eyeLookOffset;

					leftTargetAngle = Quaternion.Euler(leftEyeDummy.parent.TransformEulerAngle(llta));
					rightTargetAngle = Quaternion.Euler(rightEyeDummy.parent.TransformEulerAngle(lrta));
				}
			}
			else
			{
				targetWeight = 0;
			}

			// Random Look
			if (randomLookingEnabled && ((leftEyeDummy != null && rightEyeDummy != null && lookingControlMode == ControlMode.Classic) || lookingControlMode == ControlMode.PoseBased))
			{
				if (lookTimer <= 0)
				{
					lookTimer = Random.Range(minimumChangeDirectionGap, maximumChangeDirectionGap);
					if (lookingControlMode == ControlMode.Classic)
					{
						randomAngle = Quaternion.Euler(Random.Range(eyeRotationRangeX.x, eyeRotationRangeX.y), Random.Range(eyeRotationRangeY.x, eyeRotationRangeY.y), 0) * Quaternion.Euler(eyeLookOffset);
					}
					else if (lookingControlMode == ControlMode.PoseBased)
					{
						randomBlend = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
					}
				}
				else
				{
					lookTimer -= Time.deltaTime;
				}
			}

			// Shared Looking
			if (leftEyeDummy != null && rightEyeDummy != null && (randomLookingEnabled || targetEnabled) || lookingControlMode == ControlMode.PoseBased)
			{
				if (lookingControlMode == ControlMode.Classic)
				{
					leftEyeDummy.rotation = leftRotation;
					rightEyeDummy.rotation = rightRotation;

					Quaternion leftAngle = Quaternion.Lerp(leftEyeDummy.parent.rotation * randomAngle, leftTargetAngle, targetWeight);
					Quaternion rightAngle = Quaternion.Lerp(rightEyeDummy.parent.rotation * randomAngle, rightTargetAngle, targetWeight);

					leftEyeDummy.rotation = Quaternion.Lerp(leftEyeDummy.rotation, leftAngle, Time.deltaTime * eyeTurnSpeed);
					rightEyeDummy.rotation = Quaternion.Lerp(rightEyeDummy.rotation, rightAngle, Time.deltaTime * eyeTurnSpeed);

					// Keep eye bones in place to override any animation keys
					LeftEyeLookAtBone.localPosition = RightEyeLookAtBone.localPosition = Vector3.zero;

					// Apply offsets
					LeftEyeLookAtBone.localEulerAngles = axisOffsets[(int)eyeForwardAxis];
					RightEyeLookAtBone.localEulerAngles = axisOffsets[(int)eyeForwardAxis];

					leftRotation = leftEyeDummy.rotation;
					rightRotation = rightEyeDummy.rotation;
				}
				else if (lookingControlMode == ControlMode.PoseBased)
				{
					if (randomBlend.y >= 0)
					{
						// Looking Up Range
						for (int b = 0; b < lookingUpShape.blendShapes.Count; b++)
						{
							if (blinkingControlMode == ControlMode.PoseBased)
							{
								if (!(blinkingShape.blendShapes.Contains(lookingUpShape.blendShapes[b]) && (blinking || keepEyesClosed)))
								{
									blendSystem.SetBlendableValue(lookingUpShape.blendShapes[b], Mathf.Lerp(blendSystem.GetBlendableValue(lookingUpShape.blendShapes[b]), Mathf.Lerp(0, lookingUpShape.weights[b], randomBlend.y), Time.deltaTime * eyeTurnSpeed));
								}
							}
							else
							{
								if (!((leftEyeBlinkBlendable == lookingUpShape.blendShapes[b] || rightEyeBlinkBlendable == lookingUpShape.blendShapes[b]) && (blinking || keepEyesClosed)))
								{
									blendSystem.SetBlendableValue(lookingUpShape.blendShapes[b], Mathf.Lerp(blendSystem.GetBlendableValue(lookingUpShape.blendShapes[b]), Mathf.Lerp(0, lookingUpShape.weights[b], randomBlend.y), Time.deltaTime * eyeTurnSpeed));
								}
							}
						}

						for (int b = 0; b < lookingUpShape.bones.Count; b++)
						{

							if (blinkingControlMode == ControlMode.PoseBased)
							{
								if (blinkingShape.HasBone(lookingUpShape.bones[b].bone) && (blinking || keepEyesClosed))
								{
									continue;
								}
							}

							Vector3 newPos = Vector3.Lerp(lookingUpShape.bones[b].neutralPosition, lookingUpShape.bones[b].endPosition, randomBlend.y);
							Vector3 newRot = Vector3LerpAngle(lookingUpShape.bones[b].neutralRotation, lookingUpShape.bones[b].endRotation, randomBlend.y);

							if (boneUpdateAnimation)
							{
								if (!lookingUpShape.bones[b].lockPosition)
									boneShapes[lookingUpShape.bones[b].bone].targetPosition = lookingUpShape.bones[b].bone.localPosition + (newPos - lookingUpShape.bones[b].neutralPosition);
								if (!lookingUpShape.bones[b].lockRotation)
									boneShapes[lookingUpShape.bones[b].bone].targetRotation = Quaternion.Euler(lookingUpShape.bones[b].bone.localEulerAngles + (newRot - lookingUpShape.bones[b].neutralRotation));
							}
							else
							{
								if (!lookingUpShape.bones[b].lockPosition)
									boneShapes[lookingUpShape.bones[b].bone].targetPosition = newPos;
								if (!lookingUpShape.bones[b].lockRotation)
									boneShapes[lookingUpShape.bones[b].bone].targetRotation = Quaternion.Euler(newRot);
							}

							if (!lookingUpShape.bones[b].lockPosition)
								boneShapes[lookingUpShape.bones[b].bone].storedPosition = Vector3.Lerp(boneShapes[lookingUpShape.bones[b].bone].storedPosition, boneShapes[lookingUpShape.bones[b].bone].targetPosition, Time.deltaTime * eyeTurnSpeed);
							if (!lookingUpShape.bones[b].lockRotation)
								boneShapes[lookingUpShape.bones[b].bone].storedRotation = Quaternion.Lerp(boneShapes[lookingUpShape.bones[b].bone].storedRotation, boneShapes[lookingUpShape.bones[b].bone].targetRotation, Time.deltaTime * eyeTurnSpeed);
						}
					}
					else
					{
						// Looking Down Range
						for (int b = 0; b < lookingDownShape.blendShapes.Count; b++)
						{
							if (blinkingControlMode == ControlMode.PoseBased)
							{
								if (!(blinkingShape.blendShapes.Contains(lookingDownShape.blendShapes[b]) && (blinking || keepEyesClosed)))
								{
									blendSystem.SetBlendableValue(lookingDownShape.blendShapes[b], Mathf.Lerp(blendSystem.GetBlendableValue(lookingDownShape.blendShapes[b]), Mathf.Lerp(0, lookingDownShape.weights[b], -randomBlend.y), Time.deltaTime * eyeTurnSpeed));
								}
							}
							else
							{
								if (!((leftEyeBlinkBlendable == lookingDownShape.blendShapes[b] || rightEyeBlinkBlendable == lookingDownShape.blendShapes[b]) && (blinking || keepEyesClosed)))
								{
									blendSystem.SetBlendableValue(lookingDownShape.blendShapes[b], Mathf.Lerp(blendSystem.GetBlendableValue(lookingDownShape.blendShapes[b]), Mathf.Lerp(0, lookingDownShape.weights[b], -randomBlend.y), Time.deltaTime * eyeTurnSpeed));
								}
							}
						}

						for (int b = 0; b < lookingDownShape.bones.Count; b++)
						{

							if (blinkingControlMode == ControlMode.PoseBased)
							{
								if (blinkingShape.HasBone(lookingDownShape.bones[b].bone) && (blinking || keepEyesClosed))
								{
									continue;
								}
							}

							Vector3 newPos = Vector3.Lerp(lookingDownShape.bones[b].neutralPosition, lookingDownShape.bones[b].endPosition, -randomBlend.y);
							Vector3 newRot = Vector3LerpAngle(lookingDownShape.bones[b].neutralRotation, lookingDownShape.bones[b].endRotation, -randomBlend.y);

							if (boneUpdateAnimation)
							{
								if (!lookingDownShape.bones[b].lockPosition)
									boneShapes[lookingDownShape.bones[b].bone].targetPosition = lookingDownShape.bones[b].bone.localPosition + (newPos - lookingDownShape.bones[b].neutralPosition);
								if (!lookingDownShape.bones[b].lockRotation)
									boneShapes[lookingDownShape.bones[b].bone].targetRotation = Quaternion.Euler(lookingDownShape.bones[b].bone.localEulerAngles + (newRot - lookingDownShape.bones[b].neutralRotation));
							}
							else
							{
								if (!lookingDownShape.bones[b].lockPosition)
									boneShapes[lookingDownShape.bones[b].bone].targetPosition = newPos;
								if (!lookingDownShape.bones[b].lockRotation)
									boneShapes[lookingDownShape.bones[b].bone].targetRotation = Quaternion.Euler(newRot);
							}

							if (!lookingDownShape.bones[b].lockPosition)
								boneShapes[lookingDownShape.bones[b].bone].storedPosition = Vector3.Lerp(boneShapes[lookingDownShape.bones[b].bone].storedPosition, boneShapes[lookingDownShape.bones[b].bone].targetPosition, Time.deltaTime * eyeTurnSpeed);
							if (!lookingDownShape.bones[b].lockRotation)
								boneShapes[lookingDownShape.bones[b].bone].storedRotation = Quaternion.Lerp(boneShapes[lookingDownShape.bones[b].bone].storedRotation, boneShapes[lookingDownShape.bones[b].bone].targetRotation, Time.deltaTime * eyeTurnSpeed);
						}
					}

					if (randomBlend.x >= 0)
					{
						// Looking Right Range
						for (int b = 0; b < lookingRightShape.blendShapes.Count; b++)
						{
							if (blinkingControlMode == ControlMode.PoseBased)
							{
								if (!(blinkingShape.blendShapes.Contains(lookingRightShape.blendShapes[b]) && (blinking || keepEyesClosed)))
								{
									blendSystem.SetBlendableValue(lookingRightShape.blendShapes[b], Mathf.Lerp(blendSystem.GetBlendableValue(lookingRightShape.blendShapes[b]), Mathf.Lerp(0, lookingRightShape.weights[b], randomBlend.x), Time.deltaTime * eyeTurnSpeed));
								}
							}
							else
							{
								if (!((leftEyeBlinkBlendable == lookingRightShape.blendShapes[b] || rightEyeBlinkBlendable == lookingRightShape.blendShapes[b]) && (blinking || keepEyesClosed)))
								{
									blendSystem.SetBlendableValue(lookingRightShape.blendShapes[b], Mathf.Lerp(blendSystem.GetBlendableValue(lookingRightShape.blendShapes[b]), Mathf.Lerp(0, lookingRightShape.weights[b], randomBlend.x), Time.deltaTime * eyeTurnSpeed));
								}
							}
						}

						for (int b = 0; b < lookingRightShape.bones.Count; b++)
						{

							if (blinkingControlMode == ControlMode.PoseBased)
							{
								if (blinkingShape.HasBone(lookingRightShape.bones[b].bone) && (blinking || keepEyesClosed))
								{
									continue;
								}
							}

							Vector3 newPos = Vector3.Lerp(lookingRightShape.bones[b].neutralPosition, lookingRightShape.bones[b].endPosition, randomBlend.x) - lookingRightShape.bones[b].neutralPosition;
							Vector3 newRot = Vector3LerpAngle(lookingRightShape.bones[b].neutralRotation, lookingRightShape.bones[b].endRotation, randomBlend.x) - lookingRightShape.bones[b].neutralRotation;

							if (!lookingRightShape.bones[b].lockPosition)
								boneShapes[lookingRightShape.bones[b].bone].targetPosition = lookingRightShape.bones[b].bone.localPosition + newPos;
							if (!lookingRightShape.bones[b].lockRotation)
								boneShapes[lookingRightShape.bones[b].bone].targetRotation = Quaternion.Euler(lookingRightShape.bones[b].bone.localEulerAngles + newRot);

							if (!lookingRightShape.bones[b].lockPosition)
								boneShapes[lookingRightShape.bones[b].bone].storedPosition = Vector3.Lerp(boneShapes[lookingRightShape.bones[b].bone].storedPosition, boneShapes[lookingRightShape.bones[b].bone].targetPosition, Time.deltaTime * eyeTurnSpeed);
							if (!lookingRightShape.bones[b].lockRotation)
								boneShapes[lookingRightShape.bones[b].bone].storedRotation = Quaternion.Lerp(boneShapes[lookingRightShape.bones[b].bone].storedRotation, boneShapes[lookingRightShape.bones[b].bone].targetRotation, Time.deltaTime * eyeTurnSpeed);
						}
					}
					else
					{
						// Looking Left Range
						for (int b = 0; b < lookingLeftShape.blendShapes.Count; b++)
						{
							if (blinkingControlMode == ControlMode.PoseBased)
							{
								if (!(blinkingShape.blendShapes.Contains(lookingLeftShape.blendShapes[b]) && (blinking || keepEyesClosed)))
								{
									blendSystem.SetBlendableValue(lookingLeftShape.blendShapes[b], Mathf.Lerp(blendSystem.GetBlendableValue(lookingRightShape.blendShapes[b]), Mathf.Lerp(0, lookingLeftShape.weights[b], -randomBlend.x), Time.deltaTime * eyeTurnSpeed));
								}
							}
							else
							{
								if (!((leftEyeBlinkBlendable == lookingLeftShape.blendShapes[b] || rightEyeBlinkBlendable == lookingLeftShape.blendShapes[b]) && (blinking || keepEyesClosed)))
								{
									blendSystem.SetBlendableValue(lookingLeftShape.blendShapes[b], Mathf.Lerp(blendSystem.GetBlendableValue(lookingRightShape.blendShapes[b]), Mathf.Lerp(0, lookingLeftShape.weights[b], -randomBlend.x), Time.deltaTime * eyeTurnSpeed));
								}
							}
						}

						for (int b = 0; b < lookingLeftShape.bones.Count; b++)
						{

							if (blinkingControlMode == ControlMode.PoseBased)
							{
								if (blinkingShape.HasBone(lookingLeftShape.bones[b].bone) && (blinking || keepEyesClosed))
								{
									continue;
								}
							}

							Vector3 newPos = Vector3.Lerp(lookingLeftShape.bones[b].neutralPosition, lookingLeftShape.bones[b].endPosition, -randomBlend.x) - lookingLeftShape.bones[b].neutralPosition;
							Vector3 newRot = Vector3LerpAngle(lookingLeftShape.bones[b].neutralRotation, lookingLeftShape.bones[b].endRotation, -randomBlend.x) - lookingLeftShape.bones[b].neutralRotation;

							if (!lookingLeftShape.bones[b].lockPosition)
								boneShapes[lookingLeftShape.bones[b].bone].targetPosition = lookingLeftShape.bones[b].bone.localPosition + newPos;
							if (!lookingLeftShape.bones[b].lockRotation)
								boneShapes[lookingLeftShape.bones[b].bone].targetRotation = Quaternion.Euler(lookingLeftShape.bones[b].bone.localEulerAngles + newRot);

							if (!lookingLeftShape.bones[b].lockPosition)
								boneShapes[lookingLeftShape.bones[b].bone].storedPosition = Vector3.Lerp(boneShapes[lookingLeftShape.bones[b].bone].storedPosition, boneShapes[lookingLeftShape.bones[b].bone].targetPosition, Time.deltaTime * eyeTurnSpeed);
							if (!lookingLeftShape.bones[b].lockRotation)
								boneShapes[lookingLeftShape.bones[b].bone].storedRotation = Quaternion.Lerp(boneShapes[lookingLeftShape.bones[b].bone].storedRotation, boneShapes[lookingLeftShape.bones[b].bone].targetRotation, Time.deltaTime * eyeTurnSpeed);
						}
					}
				}
			}
		}

		private IEnumerator CloseEyes ()
		{
			bool end = false;
			blinkTimer = 0;
			_asyncBlending = true;

			while (end == false)
			{
				if (blinkingControlMode == ControlMode.Classic)
				{
					blendSystem.SetBlendableValue(leftEyeBlinkBlendable, Mathf.Lerp(0, 100, blinkTimer / blinkDuration));
					blendSystem.SetBlendableValue(rightEyeBlinkBlendable, Mathf.Lerp(0, 100, blinkTimer / blinkDuration));
				}
				else
				{
					for (int b = 0; b < blinkingShape.blendShapes.Count; b++)
					{
						blendSystem.SetBlendableValue(blinkingShape.blendShapes[b], Mathf.Lerp(0, 100, blinkTimer / blinkDuration));
					}

					for (int b = 0; b < blinkingShape.bones.Count; b++)
					{
						if (boneUpdateAnimation)
						{
							Vector3 newPos = Vector3.Lerp(blinkingShape.bones[b].neutralPosition, blinkingShape.bones[b].endPosition, blinkTimer / blinkDuration) - blinkingShape.bones[b].neutralPosition;
							Vector3 newRot = Vector3.Lerp(blinkingShape.bones[b].neutralRotation, blinkingShape.bones[b].endRotation, blinkTimer / blinkDuration) - blinkingShape.bones[b].neutralRotation;

							if (!blinkingShape.bones[b].lockPosition)
								blinkingShape.bones[b].bone.localPosition += newPos;
							if (!blinkingShape.bones[b].lockRotation)
								blinkingShape.bones[b].bone.localEulerAngles += newRot;
						}
						else
						{
							if (!blinkingShape.bones[b].lockPosition)
								blinkingShape.bones[b].bone.localPosition = Vector3.Lerp(blinkingShape.bones[b].neutralPosition, blinkingShape.bones[b].endPosition, blinkTimer / blinkDuration);
							if (!blinkingShape.bones[b].lockRotation)
								blinkingShape.bones[b].bone.localEulerAngles = Vector3.Lerp(blinkingShape.bones[b].neutralRotation, blinkingShape.bones[b].endRotation, blinkTimer / blinkDuration);
						}
					}
				}

				if (blinkTimer > blinkDuration)
				{
					end = true;
					_asyncBlending = false;
				}

				blinkTimer += Time.deltaTime;
				yield return null;
			}
		}

		private IEnumerator OpenEyes ()
		{
			bool end = false;
			blinkTimer = 0;
			_asyncBlending = true;

			while (end == false)
			{
				if (blinkingControlMode == ControlMode.Classic)
				{
					blendSystem.SetBlendableValue(leftEyeBlinkBlendable, Mathf.Lerp(100, 0, blinkTimer / blinkDuration));
					blendSystem.SetBlendableValue(rightEyeBlinkBlendable, Mathf.Lerp(100, 0, blinkTimer / blinkDuration));
				}
				else
				{
					for (int b = 0; b < blinkingShape.blendShapes.Count; b++)
					{
						blendSystem.SetBlendableValue(blinkingShape.blendShapes[b], Mathf.Lerp(100, 0, blinkTimer / blinkDuration));
					}

					for (int b = 0; b < blinkingShape.bones.Count; b++)
					{
						if (boneUpdateAnimation)
						{
							Vector3 newPos = Vector3.Lerp(blinkingShape.bones[b].endPosition, blinkingShape.bones[b].neutralPosition, blinkTimer / blinkDuration) - blinkingShape.bones[b].neutralPosition;
							Vector3 newRot = Vector3.Lerp(blinkingShape.bones[b].endRotation, blinkingShape.bones[b].neutralRotation, blinkTimer / blinkDuration) - blinkingShape.bones[b].neutralRotation;

							if (!blinkingShape.bones[b].lockPosition)
								blinkingShape.bones[b].bone.localPosition += newPos;
							if (!blinkingShape.bones[b].lockRotation)
								blinkingShape.bones[b].bone.localEulerAngles += newRot;
						}
						else
						{
							if (!blinkingShape.bones[b].lockPosition)
								blinkingShape.bones[b].bone.localPosition = Vector3.Lerp(blinkingShape.bones[b].endPosition, blinkingShape.bones[b].neutralPosition, blinkTimer / blinkDuration);
							if (!blinkingShape.bones[b].lockRotation)
								blinkingShape.bones[b].bone.localEulerAngles = Vector3.Lerp(blinkingShape.bones[b].endRotation, blinkingShape.bones[b].neutralRotation, blinkTimer / blinkDuration);
						}
					}
				}

				if (blinkTimer > blinkDuration)
				{
					end = true;
					_asyncBlending = false;
				}

				blinkTimer += Time.deltaTime;
				yield return null;
			}
		}

		private void FixDummyHierarchy ()
		{
			if (LeftEyeLookAtBone == null || RightEyeLookAtBone == null)
				return;

			if (!leftEyeDummy || !rightEyeDummy)
			{
				leftEyeDummy = new GameObject("Left Eye Dummy").transform;
				leftEyeDummy.gameObject.hideFlags = HideFlags.DontSave;
			}

			if(!rightEyeDummy)
			{
				rightEyeDummy = new GameObject("Right Eye Dummy").transform;
				rightEyeDummy.gameObject.hideFlags = HideFlags.DontSave;
			}

			// Restore original hierarchy
			if (leftEyeDummy.childCount > 0)
			{
				for (int i = 0; i < leftEyeDummy.childCount; i++)
				{
					leftEyeDummy.GetChild(i).SetParent(leftEyeDummy.parent, true);

				}
			}

			if (rightEyeDummy.childCount > 0)
			{
				for (int i = 0; i < rightEyeDummy.childCount; i++)
				{
					rightEyeDummy.GetChild(i).SetParent(rightEyeDummy.parent, true);
				}
			}

			// Reparent new bones
			leftEyeDummy.SetParent(LeftEyeLookAtBone.parent, false);
			rightEyeDummy.SetParent(RightEyeLookAtBone.parent, false);
			leftEyeDummy.position = LeftEyeLookAtBone.position;
			leftEyeDummy.rotation = LeftEyeLookAtBone.rotation;
			rightEyeDummy.position = RightEyeLookAtBone.position;
			rightEyeDummy.rotation = RightEyeLookAtBone.rotation;
			LeftEyeLookAtBone.SetParent(leftEyeDummy, true);
			RightEyeLookAtBone.SetParent(rightEyeDummy, true);
		}

		/// <summary>
		/// Finds potential look targets using the autoTargetTag.
		/// </summary>
		public void FindTargets ()
		{
			GameObject[] gos = GameObject.FindGameObjectsWithTag(autoTargetTag);
			markedTargets = new Transform[gos.Length];

			for (int i = 0; i < markedTargets.Length; i++)
			{
				markedTargets[i] = gos[i].transform;
			}
		}

		public static Vector3 Vector3LerpAngle (Vector3 a, Vector3 b, float t)
		{
			return new Vector3(
				Mathf.LerpAngle(a.x, b.x, t),
				Mathf.LerpAngle(a.y, b.y, t),
				Mathf.LerpAngle(a.z, b.z, t)
				);
		}

		public void SetLookAtAmount (float amount)
		{
			targetWeight = amount;
		}

		public enum ControlMode
		{
			Classic,
			PoseBased
		}

		public enum Axis
		{
			X_Positive,
			X_Negative,
			Y_Positive,
			Y_Negative,
			Z_Positive,
			Z_Negative,
		}

		public class BoneShapeInfo
		{
			private Transform bone;

			private Vector3 m_storedPosition;
			private Quaternion m_storedRotation;

			public Vector3 storedPosition
			{
				get
				{
					return m_storedPosition;
				}

				set
				{
					m_storedPosition = value;
					bone.localPosition = value;
				}
			}
			public Quaternion storedRotation
			{
				get
				{
					return m_storedRotation;
				}

				set
				{
					m_storedRotation = value;
					bone.localRotation = value;
				}
			}

			public Vector3 targetPosition;
			public Quaternion targetRotation;

			public BoneShapeInfo (BoneShape boneShape)
			{
				bone = boneShape.bone;
				m_storedPosition = boneShape.neutralPosition;
				m_storedRotation = Quaternion.Euler(boneShape.neutralRotation);
			}
		}
	}
}