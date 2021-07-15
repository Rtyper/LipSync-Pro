using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace RogoDigital {
	public static class ContinuationManager {
		public class Job {
			public Job (Func<bool> completed, System.Action continueWith) {
				Completed = completed;
				ContinueWith = continueWith;
			}

			public Func<bool> Completed { get; private set; }
			public System.Action ContinueWith { get; private set; }
		}

		private static readonly List<Job> jobs = new List<Job>();

		public static void Add (Func<bool> completed, System.Action continueWith) {
			if (!jobs.Any()) EditorApplication.update += Update;
			Job job = new Job(completed, continueWith);
			jobs.Add(job);
		}

		private static void Update () {
			for (int i = jobs.Count - 1; i >= 0; --i) {
				var jobIt = jobs[i];
				if (jobIt.Completed()) {
					jobs.RemoveAt(i);
					jobIt.ContinueWith();
				}
			}
			if (!jobs.Any()) EditorApplication.update -= Update;
		}
	}
}