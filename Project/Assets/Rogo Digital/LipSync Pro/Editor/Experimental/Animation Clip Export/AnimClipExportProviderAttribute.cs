using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RogoDigital.Lipsync.Experimental
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class AnimClipExportProviderAttribute : Attribute
	{
		public Type blendSystemType;

		public AnimClipExportProviderAttribute(Type blendSystemType)
		{
			this.blendSystemType = blendSystemType;
		}
	}
}