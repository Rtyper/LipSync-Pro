using NodeCanvas.Framework;
using ParadoxNotion.Design;
using RogoDigital.Lipsync;

namespace NodeCanvas.Tasks.RogoLipSync{

	[Category("Third Party/LipSync")]
	[Icon("LipSync")]
	public class Stop : ActionTask<LipSync>{

		protected override void OnExecute(){
			agent.Stop(true);
			EndAction();
		}
	}
}