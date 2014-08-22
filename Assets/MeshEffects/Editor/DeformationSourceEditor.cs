using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DeformationSourceBehaviour))]
[InitializeOnLoad]
public class DeformationSourceEditor : Editor {

	//private static readonly string SOURCE_ICON_NAME = "deformation_component_texture";
	//private static Texture gSourceIcon;
	private static string[] gLayerMasksNames;

	static DeformationSourceEditor()
	{
		//gSourceIcon = (Texture)Resources.Load(SOURCE_ICON_NAME);
		gLayerMasksNames = EditorHelper.GetLayerMasksNames ();
	}

	#region INSPECTOR

	public override void OnInspectorGUI() {

		DeformationSourceBehaviour deformationSourceTarget = (DeformationSourceBehaviour) target; 

		bool memoTri = deformationSourceTarget.rotateAroundSource;

		// effect parameters
		deformationSourceTarget.effectRange = EditorGUILayout.FloatField("Effect Range",deformationSourceTarget.effectRange);
		if (deformationSourceTarget.effectRange < 0) {
			deformationSourceTarget.effectRange = 0;
		}
		deformationSourceTarget.effectCoef = EditorGUILayout.FloatField("Effect Coef",deformationSourceTarget.effectCoef);
		if (deformationSourceTarget.effectCoef < 0) {
			deformationSourceTarget.effectCoef = 0;
		}
		deformationSourceTarget.isEffectExponential = EditorGUILayout.Toggle("Is Effect Exponential ?",deformationSourceTarget.isEffectExponential);

		// effect type
		int selectedType = EditorGUILayout.Popup ("Deformation Type",(int)deformationSourceTarget.deformationType, new string[]{
			DeformationSourceBehaviour.DeformationSourceType.Directionnal.ToString(),
			DeformationSourceBehaviour.DeformationSourceType.Point.ToString(), 
			DeformationSourceBehaviour.DeformationSourceType.Spirale.ToString()
		});
		deformationSourceTarget.deformationType = ( DeformationSourceBehaviour.DeformationSourceType) selectedType;

		GUIStyle style = new GUIStyle();
		style.margin = new RectOffset(40,0,0,0);

		switch (selectedType) {
		case (int)DeformationSourceBehaviour.DeformationSourceType.Directionnal:
			EditorGUILayout.BeginVertical(style);
				deformationSourceTarget.isBidirectionnal = EditorGUILayout.Toggle("Is Bidirectionnal ?",deformationSourceTarget.isBidirectionnal);
			EditorGUILayout.EndVertical();
			break;
		case (int)DeformationSourceBehaviour.DeformationSourceType.Spirale:
			EditorGUILayout.BeginVertical(style);
			//spirale rotation axis
			EditorGUILayout.LabelField("Rotation axis :");
			EditorGUILayout.BeginHorizontal("box");
			bool tri = EditorGUILayout.Toggle("Object Normal",memoTri);
			bool bi = EditorGUILayout.Toggle("Source Normal", !memoTri);
			EditorGUILayout.EndHorizontal();
			// amplitude
			deformationSourceTarget.spiraleAmplitude = EditorGUILayout.FloatField("Spirale Amplitude",deformationSourceTarget.spiraleAmplitude);
			EditorGUILayout.EndVertical();

			// manage radioButton
			if(!(tri || bi)){
				tri = memoTri;
				bi = !tri;
			}
			else if(tri == bi){
				if(tri == memoTri){
					 tri = !tri;
				}
				else{
					bi = !bi;
				}
				memoTri = tri;
			}

			// apply changins in behaviour
			deformationSourceTarget.rotateAroundSource = bi;

			break;
		}

		//Refreshing
		deformationSourceTarget.refreshOnMove = EditorGUILayout.Toggle("Refresh on deformation source transform changed ?",deformationSourceTarget.refreshOnMove);
		deformationSourceTarget.refreshOnObjectsMove = EditorGUILayout.Toggle("Refresh on objects' transform changed ?",deformationSourceTarget.refreshOnObjectsMove);

		// culling mask
		deformationSourceTarget.cullingMask = EditorGUILayout.MaskField ("Culling Mask", deformationSourceTarget.cullingMask.value,gLayerMasksNames);

		if (GUI.changed)
			EditorUtility.SetDirty (target);
	}
	#endregion
	
	#region SCENE
	public void OnSceneGUI(){
		DeformationSourceBehaviour deformationSourceTarget = (DeformationSourceBehaviour) target;

		switch (deformationSourceTarget.deformationType) {
		case DeformationSourceBehaviour.DeformationSourceType.Directionnal:

			float planeSide = 1.0f;
			//draw first plane
			for(int i = -5; i <= 5; i++){
				Vector3 p1v = new Vector3(i,0,-5) * planeSide / 2;
				Vector3 p2v = new Vector3(i,0,5) * planeSide / 2;
				Vector3 p1h = new Vector3(-5,0,i) * planeSide / 2;
				Vector3 p2h = new Vector3(5,0,i) * planeSide / 2;
				Vector3 p1vr = p1v + new Vector3(0,deformationSourceTarget.effectRange,0);
				Vector3 p2vr = p2v + new Vector3(0,deformationSourceTarget.effectRange,0);
				Vector3 p1hr = p1h + new Vector3(0,deformationSourceTarget.effectRange,0);
				Vector3 p2hr = p2h + new Vector3(0,deformationSourceTarget.effectRange,0);
				
				p1v = deformationSourceTarget.transform.TransformPoint(p1v);
				p2v = deformationSourceTarget.transform.TransformPoint(p2v);
				p1h = deformationSourceTarget.transform.TransformPoint(p1h);
				p2h = deformationSourceTarget.transform.TransformPoint(p2h);
				
				p1vr = deformationSourceTarget.transform.TransformPoint(p1vr);
				p2vr = deformationSourceTarget.transform.TransformPoint(p2vr);
				p1hr = deformationSourceTarget.transform.TransformPoint(p1hr);
				p2hr = deformationSourceTarget.transform.TransformPoint(p2hr);
				
				Handles.color = Color.green;
				Handles.DrawLine(p1v,p2v);
				Handles.DrawLine(p1h,p2h);

				Handles.color = Color.yellow;
				Handles.DrawLine(p1vr,p2vr);
				Handles.DrawLine(p1hr,p2hr);

				if(deformationSourceTarget.isBidirectionnal){
					Vector3 p1vb = new Vector3(i,0,-5) * planeSide / 2 + new Vector3(0,-deformationSourceTarget.effectRange,0);
					Vector3 p2vb = new Vector3(i,0,5) * planeSide / 2 + new Vector3(0,-deformationSourceTarget.effectRange,0);
					Vector3 p1hb = new Vector3(-5,0,i) * planeSide / 2 + new Vector3(0,-deformationSourceTarget.effectRange,0);
					Vector3 p2hb = new Vector3(5,0,i) * planeSide / 2 + new Vector3(0,-deformationSourceTarget.effectRange,0);

					p1vb = deformationSourceTarget.transform.TransformPoint(p1vb);
					p2vb = deformationSourceTarget.transform.TransformPoint(p2vb);
					p1hb = deformationSourceTarget.transform.TransformPoint(p1hb);
					p2hb = deformationSourceTarget.transform.TransformPoint(p2hb);

					Handles.DrawLine(p1vb,p2vb);
					Handles.DrawLine(p1hb,p2hb);
				}
			}

			break;
		case DeformationSourceBehaviour.DeformationSourceType.Spirale:
			Handles.color = Color.green;
			Handles.RadiusHandle(deformationSourceTarget.transform.rotation,deformationSourceTarget.transform.position,deformationSourceTarget.effectRange);
			Handles.color = Color.yellow;
			if(!deformationSourceTarget.rotateAroundSource){
				Quaternion rotation = Quaternion.AngleAxis(90,deformationSourceTarget.transform.right);
				Handles.CircleCap(0,deformationSourceTarget.transform.position, rotation,deformationSourceTarget.effectRange * 0.75f);
				Handles.CircleCap(0,deformationSourceTarget.transform.position, rotation,deformationSourceTarget.effectRange * 0.5f);
				Handles.CircleCap(0,deformationSourceTarget.transform.position, rotation,deformationSourceTarget.effectRange * 0.25f);
			}
			else{
				Handles.RadiusHandle(deformationSourceTarget.transform.rotation,deformationSourceTarget.transform.position,deformationSourceTarget.effectRange * 0.75f);
				Handles.RadiusHandle(deformationSourceTarget.transform.rotation,deformationSourceTarget.transform.position,deformationSourceTarget.effectRange * 0.5f);
				Handles.RadiusHandle(deformationSourceTarget.transform.rotation,deformationSourceTarget.transform.position,deformationSourceTarget.effectRange * 0.25f);
			}
			break;
		case DeformationSourceBehaviour.DeformationSourceType.Point:
			Handles.color = Color.green;
			Handles.RadiusHandle(deformationSourceTarget.transform.rotation,deformationSourceTarget.transform.position,deformationSourceTarget.effectRange);
			break;
		}
	}
		
	/*private void OnScene(SceneView sceneview)
	{
		DeformationSourceBehaviour deformationSourceTarget = (DeformationSourceBehaviour) target;

		//Handles.Label (deformationSourceTarget.transform.position,gSourceIcon);
	}*/

	/*public void OnEnable()
	{
		SceneView.onSceneGUIDelegate -= OnScene;
		SceneView.onSceneGUIDelegate += OnScene;
	}
	*/

	/*public void OnDisable(){
	}
	*/
	#endregion
}
