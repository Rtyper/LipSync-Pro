using UnityEngine;
using System.Collections.Generic;

namespace RogoDigital.Lipsync
{
	[ExecuteInEditMode]
	public class BlendSystem : MonoBehaviour
	{

		// BlendSystem information
		[System.NonSerialized]
		public string blendableDisplayName = "Blendable";
		[System.NonSerialized]
		public string blendableDisplayNamePlural = "Blendables";
		[System.NonSerialized]
		public string noBlendablesMessage = "No Blendables found.";
		[System.NonSerialized]
		public string notReadyMessage = "Setup incomplete.";
		[System.NonSerialized]
		public float blendRangeLow = 0;
		[System.NonSerialized]
		public float blendRangeHigh = 100;
		[System.NonSerialized]
		public bool allowResyncing = false;

		/// <summary>
		/// Is the Blend System ready to use?
		/// </summary>
		public bool isReady = false;

		/// <summary>
		/// The components using this BlendSystem.
		/// </summary>
		public BlendSystemUser[] users = new BlendSystemUser[0];

		/// <summary>
		/// Gets the number of blendables associated with this Blend System.
		/// </summary>
		/// <value>The blendable count.</value>
		public int blendableCount
		{
			get
			{
				if (_blendables == null)
					_blendables = new List<Blendable>();
				return _blendables.Count;
			}
		}

		public BlendSystemGenericDelegate onBlendablesChanged;
		public delegate void BlendSystemGenericDelegate ();

		[SerializeField, HideInInspector]
		private List<Blendable> _blendables;

		public virtual void OnEnable ()
		{
			hideFlags = HideFlags.HideInInspector;

			OnVariableChanged();
			GetBlendables();
		}

		// When in editor mode, watch for components being removed without unregistering themselves
		// #if'd out in builds for performance, as components are able to unregister correctly in play mode.
#if UNITY_EDITOR
		void Update ()
		{
			for (int user = 0; user < users.Length; user++)
			{
				if (users[user] == null)
					Unregister(users[user]);
			}
		}
#endif
		/// <summary>
		/// Register a BlendSystemUser as using this Blend System
		/// </summary>
		/// <param name="user"></param>
		public void Register (BlendSystemUser user)
		{
			List<BlendSystemUser> newUsers = new List<BlendSystemUser>();

			for (int i = 0; i < users.Length; i++)
			{
				newUsers.Add(users[i]);
			}

			if (newUsers.Contains(user))
			{
				Debug.LogError("Could not register " + user.GetType().Name + " component to " + GetType().Name + ". BlendSystemUser is already registered.");
			}
			else
			{
				newUsers.Add(user);
				user.blendSystem = this;
			}

			users = newUsers.ToArray();
		}

		/// <summary>
		/// Unregister a BlendSystemUser
		/// </summary>
		/// <param name="user"></param>
		public void Unregister (BlendSystemUser user)
		{
			List<BlendSystemUser> newUsers = new List<BlendSystemUser>();

			for (int i = 0; i < users.Length; i++)
			{
				newUsers.Add(users[i]);
			}

			if (newUsers.Contains(user))
			{
				if (user != null)
					user.blendSystem = null;
				newUsers.Remove(user);
			}

			users = newUsers.ToArray();

			if (users.Length == 0)
			{
				OnBlendSystemRemoved();

				if (Application.isPlaying)
				{
					Destroy(this);
				}
				else
				{
					DestroyImmediate(this);
				}
			}
		}

		/// <summary>
		/// Sets the value of a blendable.
		/// </summary>
		/// <param name="blendable">Blendable.</param>
		/// <param name="value">Value.</param>
		public virtual void SetBlendableValue (int blendable, float value)
		{
		}

		/// <summary>
		/// Gets the value of a blendable.
		/// </summary>
		/// <returns>The blendable value.</returns>
		/// <param name="blendable">Blendable.</param>
		public float GetBlendableValue (int blendable)
		{
			if (_blendables == null)
				_blendables = new List<Blendable>();
			return _blendables[blendable].currentWeight;
		}

		/// <summary>
		/// Called when a BlendSystem variable is changed in a BlendSystemUser's editor.
		/// </summary>
		public virtual void OnVariableChanged ()
		{
		}

		/// <summary>
		/// Called just after a Blend System is added to the GameObject.
		/// </summary>
		public virtual void OnBlendSystemAdded ()
		{
		}

		/// <summary>
		/// Called just before a Blend System is removed from the GameObject.
		/// </summary>
		public virtual void OnBlendSystemRemoved ()
		{
		}

		/// <summary>
		/// Gets the blendables associated with this Blend System.
		/// </summary>
		/// <returns>The blendables.</returns>
		public virtual string[] GetBlendables ()
		{
			return null;
		}

		/// <summary>
		/// Called when a blendable is added to a pose on a BlendSystemUser using this blend system.
		/// </summary>
		/// <param name="blendable"></param>
		public virtual void OnBlendableAddedToPose (int blendable)
		{
		}

		/// <summary>
		/// Called when a blendable is removed from a pose on a BlendSystemUser using this blend system.
		/// </summary>
		/// <param name="blendable"></param>
		public virtual void OnBlendableRemovedFromPose (int blendable)
		{
		}

		// Internal blendable list methods
		public void AddBlendable (int blendable, float currentValue)
		{
			if (_blendables == null)
				_blendables = new List<Blendable>();
			_blendables.Insert(blendable, new Blendable(blendable, currentValue));
		}

		public void ClearBlendables ()
		{
			_blendables = new List<Blendable>();
		}

		public void SetInternalValue (int blendable, float value)
		{
			if (_blendables == null)
			{
				_blendables = new List<Blendable>();
				GetBlendables();
			}

			if (blendable >= _blendables.Count)
				GetBlendables();

			_blendables[blendable].currentWeight = value;
		}
	}
}
