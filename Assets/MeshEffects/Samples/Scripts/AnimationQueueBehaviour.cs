using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animation))]
public class AnimationQueueBehaviour : MonoBehaviour {

	void Start () {
		Animation animation = this.GetComponent<Animation> ();
		int ind = 0;

		foreach (AnimationState state in animation) {
			if(ind >= animation.GetClipCount()){
				break;
			}
			if(ind == 0){
				animation.PlayQueued (state.name,QueueMode.PlayNow);
			}
			else{
				animation.PlayQueued (state.name,QueueMode.CompleteOthers);
			}
			ind ++;
		}
	}
}
