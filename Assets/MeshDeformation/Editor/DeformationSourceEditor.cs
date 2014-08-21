using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DeformationSourceBehaviour))]
[InitializeOnLoad]
public class DeformationSourceEditor : Editor {

	private static readonly string SOURCE_ICON_NAME = "deformation_component_texture";
	private static Texture gSourceIcon;

	static DeformationSourceEditor()
	{
		gSourceIcon = (Texture)Resources.Load(SOURCE_ICON_NAME);
	}

	#region INSPECTOR
	public override void OnInspectorGUI() {

		DeformationSourceBehaviour deformationSourceTarget = (DeformationSourceBehaviour) target;

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

		switch (selectedType) {
		case (int)DeformationSourceBehaviour.DeformationSourceType.Directionnal:
			EditorGUILayout.BeginToggleGroup("Directionnal Parameters",true);

			EditorGUILayout.EndToggleGroup();
			break;
		case (int)DeformationSourceBehaviour.DeformationSourceType.Spirale:
			EditorGUILayout.BeginToggleGroup("Spirale Parameters",true);
			EditorGUILayout.EndToggleGroup();
			break;
		}

		//Refreshing
		deformationSourceTarget.refreshOnMove = EditorGUILayout.Toggle("Refresh on deformation source transform changed ?",deformationSourceTarget.refreshOnMove);
		deformationSourceTarget.refreshOnObjectsMove = EditorGUILayout.Toggle("Refresh on objects' transform changed ?",deformationSourceTarget.refreshOnObjectsMove);

		// culling mask
		deformationSourceTarget.cullingMask = EditorGUILayout.MaskField ("Culling Mask", deformationSourceTarget.cullingMask.value,EditorHelper.GetLayerMasksNames());

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
			}

			break;
		case DeformationSourceBehaviour.DeformationSourceType.Spirale:
			break;
		case DeformationSourceBehaviour.DeformationSourceType.Point:
			Handles.color = Color.green;
			Handles.RadiusHandle(deformationSourceTarget.transform.rotation,deformationSourceTarget.transform.position,deformationSourceTarget.effectRange);
			break;
		}
	}
		
	private void OnScene(SceneView sceneview)
	{
		DeformationSourceBehaviour deformationSourceTarget = (DeformationSourceBehaviour) target;

		//Handles.Label (deformationSourceTarget.transform.position,gSourceIcon);
	}

	public void OnEnable()
	{
		SceneView.onSceneGUIDelegate -= OnScene;
		SceneView.onSceneGUIDelegate += OnScene;
	}
	#endregion
}
