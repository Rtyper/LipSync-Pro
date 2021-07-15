using System;

namespace RogoDigital.Lipsync.AutoSync
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class AutoSyncModuleInfoAttribute : Attribute
	{
		public string displayName;
		public string description;
		public string author;
		public Type moduleSettingsType;

		public AutoSyncModuleInfoAttribute (string description)
		{
			displayName = GetType().Name;
			this.description = description;
			author = "";
		}

		public AutoSyncModuleInfoAttribute (string displayName, string description, string author)
		{
			this.displayName = displayName;
			this.description = description;
			this.author = author;
		}
	}
}