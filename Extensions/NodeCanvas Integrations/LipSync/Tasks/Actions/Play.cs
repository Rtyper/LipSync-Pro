using NodeCanvas.Framework;
using ParadoxNotion.Design;
using RogoDigital.Lipsync;

namespace NodeCanvas.Tasks.RogoLipSync{

	[Category("Third Party/LipSync")]
	[Icon("LipSync")]
	public class Play : ActionTask<LipSync>{

		[RequiredField]
		public BBParameter<LipSyncData> clip;
		public bool waitActionFinish = true;

		protected override void OnExecute(){
			agent.Play(clip.value);
			if (!waitActionFinish)
				EndAction();
		}

		protected override void OnUpdate(){
			if (!agent.IsPlaying){
				EndAction();
			}
		}

		protected override void OnStop(){
			if (agent.IsPlaying){
				agent.Stop(true);
			}			
		}
	}
}