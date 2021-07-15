using System;

namespace RogoDigital.Lipsync {
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public class BlendSystemButton : Attribute {
		public string displayName;

		public BlendSystemButton (string displayName) {
			this.displayName = displayName;
		}

		public struct Reference {
			public string displayName;
			public System.Reflection.MethodInfo method;

			public Reference (string displayName, System.Reflection.MethodInfo method) {
				this.displayName = displayName;
				this.method = method;
			}
		}
	}
}