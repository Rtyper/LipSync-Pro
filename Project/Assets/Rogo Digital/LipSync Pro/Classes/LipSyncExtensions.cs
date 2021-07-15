using UnityEngine;
using System.Collections.Generic;
using System.Xml;
using RogoDigital.Lipsync;

namespace RogoDigital.Lipsync {
	public static class LipSyncExtensions {
		/// <summary>
		/// Finds a named child or grandchild of a Transform.
		/// </summary>
		/// <param name="aParent"></param>
		/// <param name="aName"></param>
		/// <returns></returns>
		public static Transform FindDeepChild (this Transform aParent, string aName) {
			var result = aParent.Find(aName);
			if (result != null)
				return result;
			foreach (Transform child in aParent) {
				result = child.FindDeepChild(aName);
				if (result != null)
					return result;
			}
			return null;
		}

		/// <summary>
		/// Transforms an euler rotation in world space to one relative to a Transform.
		/// </summary>
		/// <param name="transform"></param>
		/// <param name="eulerAngle"></param>
		/// <returns></returns>
		public static Vector3 InverseTransformEulerAngle (this Transform transform, Vector3 eulerAngle) {
			return (eulerAngle - transform.eulerAngles).ToPositiveEuler();
		}

		/// <summary>
		/// Transforms an euler rotation relative to a Transform to one in world space.
		/// </summary>
		/// <param name="transform"></param>
		/// <param name="eulerAngle"></param>
		/// <returns></returns>
		public static Vector3 TransformEulerAngle (this Transform transform, Vector3 eulerAngle) {
			return ClampRange(eulerAngle + transform.eulerAngles);
		}

		/// <summary>
		/// Converts an euler rotation in the -180 - 180 range to one in the 0 to 360 range.
		/// </summary>
		/// <param name="eulerAngle"></param>
		/// <returns></returns>
		public static Vector3 ToPositiveEuler (this Vector3 eulerAngle) {
			float x = eulerAngle.x;
			float y = eulerAngle.y;
			float z = eulerAngle.z;

			if (x < 0) x = 360 + x;
			if (y < 0) y = 360 + y;
			if (z < 0) z = 360 + z;

			return new Vector3(x, y, z);
		}

		/// <summary>
		/// Converts an euler rotation in the 0 - 360 range to one in the -180 to 180 range.
		/// </summary>
		/// <param name="eulerAngle"></param>
		/// <returns></returns>
		public static Vector3 ToNegativeEuler (this Vector3 eulerAngle) {
			float x = eulerAngle.x;
			float y = eulerAngle.y;
			float z = eulerAngle.z;

			if (x > 180) x -= 360;
			if (y > 180) y -= 360;
			if (z > 180) z -= 360;

			return new Vector3(x, y, z);
		}

		private static Vector3 ClampRange (Vector3 eulerAngle) {
			float x = eulerAngle.x;
			float y = eulerAngle.y;
			float z = eulerAngle.z;

			if (x > 360) x -= 360;
			if (y > 360) y -= 360;
			if (z > 360) z -= 360;

			return new Vector3(x, y, z).ToPositiveEuler();
		}

		/// <summary>
		/// Returns the previous marker to current in a list of EmotionMarkers.
		/// </summary>
		/// <param name="list"></param>
		/// <param name="current"></param>
		/// <returns></returns>
		public static EmotionMarker PreviousMarker (this List<EmotionMarker> list, EmotionMarker current) {
			int index = list.IndexOf(current) - 1;
			if (index >= 0)
				return list[index];
			return null;
		}

		/// <summary>
		/// Returns the next marker to current in a list of EmotionMarkers.
		/// </summary>
		/// <param name="list"></param>
		/// <param name="current"></param>
		/// <returns></returns>
		public static EmotionMarker NextMarker (this List<EmotionMarker> list, EmotionMarker current) {
			int index = list.IndexOf(current) + 1;
			if (index < list.Count)
				return list[index];
			return null;
		}
	}
}