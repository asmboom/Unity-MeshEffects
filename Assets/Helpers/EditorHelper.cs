using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class EditorHelper {

	public static string[] GetLayerMasksNames(){
		List<string> layerMasks = new List<string> ();

		int maskValue = 1;
		string maskName = "";
		while((maskName = LayerMask.LayerToName(maskValue)).Length > 0){
			layerMasks.Add(maskName);
			maskValue = maskValue << 1;
		}

		return layerMasks.ToArray ();
	}
}
