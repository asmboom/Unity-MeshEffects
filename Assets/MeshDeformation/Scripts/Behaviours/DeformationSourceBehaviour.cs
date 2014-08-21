using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class DeformationSourceBehaviour : MonoBehaviour {

	public enum DeformationSourceType{
		Directionnal = 0, Point = 1, Spirale = 2
	}

	#region PRIVATE CLASS

	private class ObjectTransformState
	{
		public Vector3 position;
		public Quaternion rotation;
		public Vector3 scale;

		public ObjectTransformState(Transform transform){
			Update(transform);
		}

		public void Update(Transform transform){
			position = transform.position;
			rotation = transform.rotation;
			scale = transform.lossyScale;
		}

		public override bool Equals(object obj){
			if (obj.GetType () == typeof(Transform)) {
				Transform t = (Transform)obj;
				return t.position.Equals (position) && t.rotation.Equals (rotation) && t.lossyScale.Equals (scale);
			} else if (obj.GetType () == typeof(ObjectTransformState)) {
				ObjectTransformState t = (ObjectTransformState)obj;
				return t.position.Equals (position) && t.rotation.Equals (rotation) && t.scale.Equals (scale);
			} else {
				return base.Equals(obj);
			}
		}

		public override int GetHashCode ()
		{
			return base.GetHashCode ();
		}
	}

	private class DeformationSourceState : ObjectTransformState
	{
		public float range;
		public float coef;

		public DeformationSourceState(DeformationSourceBehaviour source)
			: base(source.transform){
		}

		public void Update(DeformationSourceBehaviour source){
			range = source.mEffectRange;
			coef = source.mEffectCoef;
			base.Update (source.transform);
		}
		
		public override bool Equals(object obj){
			if (obj.GetType () == typeof(DeformationSourceState)) {
				DeformationSourceState source = (DeformationSourceState)obj;
				return source.range == range && source.coef == coef && base.Equals(obj);
			} else {
				return base.Equals(obj);
			}
		}
		
		public override int GetHashCode ()
		{
			return base.GetHashCode ();
		}
	}
	#endregion

	#region CONFIG_ATTRIBUTES
	private float mEffectRange = 10.0f;
	private float mEffectCoef = 1.0f;
	private bool mEffectIsExponential = true;

	private DeformationSourceType mDeformationType = DeformationSourceType.Point;

	private LayerMask mCullingMask = 255;

	// refreshing
	private bool mRefreshOnMove = true;
	private bool mRefreshOnObjectsMove = false;

	// directionnal attributes
	private bool mIsBidirectionnal = true;
	#endregion

	#region PRIVATE_ATTRIBUTES
	// for original and new meshes
	private List<GameObject> mDeformedObjects;
	private List<Vector3[]> mOriginalMeshes;

	// delegate to determine the operation to apply for the effect (linear or exponential)
	private delegate float EffectValueMethod(float x);
	private EffectValueMethod mEffectValueMethod;

	// movement memo
	private List<ObjectTransformState> mPreviousTransforms;
	private DeformationSourceState mSourcePreviousTransform;

	#endregion

	#region MONOBEHAVIOUR
	void Awake(){
		mDeformedObjects = new List<GameObject>();
		mOriginalMeshes = new List<Vector3[]>();
	}

	void Start () {
		ApplyMask();

		isEffectExponential = mEffectIsExponential;

		// first application of the deformation
		ApplyDeformations ();
		mSourcePreviousTransform = new DeformationSourceState (this);
	}
	
	void FixedUpdate () {
		// allow refreshing on source move and the source did move
		if (mRefreshOnMove && !mSourcePreviousTransform.Equals(this)) {
			ApplyDeformations();
			mSourcePreviousTransform.Update(this);
		}
		// allow refreshing on objects move
		if(mRefreshOnObjectsMove){
			for(int i = 0 ; i < mDeformedObjects.Count; i++){
				// the object did move
				GameObject obj = mDeformedObjects[i];
				if(!mPreviousTransforms[i].Equals(obj)){
					_ApplyDeformationOn(obj,mOriginalMeshes[i]);
					mPreviousTransforms[i].Update(obj.transform);
				}
			}
		}
	}
	#endregion

	#region GETTER-SETTER
	public bool isEffectExponential{
		set{
			mEffectIsExponential = value;
			mEffectValueMethod = mEffectIsExponential ? (EffectValueMethod)_ExponentialDeformation : (EffectValueMethod)_LinearDeformation;
		}
		get{return mEffectIsExponential;}
	}
	public bool refreshOnObjectsMove{
		set{
			mRefreshOnObjectsMove = value;
			if(mRefreshOnObjectsMove){
				mPreviousTransforms.Clear();
				mPreviousTransforms = null;
			}
			else{
				_BuildObjectTransformsList();
			}
		}
		get{return mRefreshOnObjectsMove;}
	}
	public bool refreshOnMove{
	}
	public float effectRange{
		set{mEffectRange = value;}
		get{return mEffectRange;}
	}
	public float effectCoef{
		set{mEffectCoef = value;}
		get{return mEffectCoef;}
	}
	#endregion

	#region INIT
	/**
	 * Apply the mask by searching oall the gameobject affected by the deformation source
	 * */
	public void ApplyMask(){
		GameObject[] allGameObjects = (GameObject[])FindObjectsOfType (typeof(GameObject));
		foreach (GameObject obj in allGameObjects) {
			MeshFilter meshFilter = null;
			int combinedLayer = (obj.layer & mCullingMask.value);
			if(combinedLayer != obj.layer || combinedLayer == 0 && mCullingMask.value != 0){ // manage the "Default" Layer ( == 0)
				continue;
			}
			if((meshFilter = obj.GetComponent<MeshFilter>()) != null && !mDeformedObjects.Contains(obj)){
				mDeformedObjects.Add(obj); // save object
				Vector3[] originalVertices = new Vector3[meshFilter.mesh.vertices.Length];
				meshFilter.mesh.vertices.CopyTo(originalVertices,0);
				mOriginalMeshes.Add(originalVertices); // save original mesh
			}
		}

		iff(refreshOnObjectsMove) {
			_BuildObjectTransformsList();
		}
	}

	/**
	 * Fill the list with the concerned objects' Transform structure
	 * */
	private void _BuildObjectTransformsList(){
		if (mPreviousTransforms == null) {
			mPreviousTransforms = new List<ObjectTransformState> ();
		} else {
			mPreviousTransforms.Clear();
		}

		foreach (GameObject obj in mDeformedObjects) {
			mPreviousTransforms.Add(new ObjectTransformState(obj.transform));
		}
	}
	#endregion

	#region MESH_DEFORMATION
	/**
	 * Apply deformation on all objects
	 * */
	public void ApplyDeformations(){
		for (int i = 0; i < mDeformedObjects.Count; i++) {
			_ApplyDeformationOn(mDeformedObjects[i],mOriginalMeshes[i]);
		}
	}

	/**
	 * Apply deformation on mesh for the object
	 * */
	private void _ApplyDeformationOn(GameObject obj,Vector3[] vertices){
		
		MeshFilter meshFilter = obj.GetComponent<MeshFilter> ();
		Mesh deformedMesh = meshFilter.mesh; /// copy structure

		Vector3[] newVertices = deformedMesh.vertices;
		for (int i = 0; i < vertices.Length; i++) {
			Vector3 vertex = obj.transform.TransformPoint(vertices[i]);

			// calculate effect for this vertex
			float vertexDistance = 0.0f;
			Vector3 direction = Vector3.zero;
			switch(mDeformationType){
			case DeformationSourceType.Point:
				vertexDistance = Vector3.Distance (vertex, this.transform.position);
				direction = (transform.position - vertex);
				break;
			case DeformationSourceType.Directionnal :
				Plane refPlane = new Plane(transform.up,transform.position);
				vertexDistance = refPlane.GetDistanceToPoint(vertex);
				direction = vertexDistance * transform.up;
				break;
			case DeformationSourceType.Spirale :
				break;
			}

			if(vertexDistance <= mEffectRange){
				float relDistance = vertexDistance / mEffectRange;
				float effect = mEffectValueMethod(relDistance);

				vertex += direction * effect;

				newVertices[i] = obj.transform.InverseTransformPoint(vertex);
			}
			else{
				newVertices[i] = vertices[i];
			}
		}

		deformedMesh.vertices = newVertices;
		meshFilter.mesh = deformedMesh;

	}
	#endregion

	#region DELEGATE
	private float _ExponentialDeformation(float relDistance){
		return relDistance == 0 ? 1 : 1 - Mathf.Exp (-relDistance * mEffectCoef);
	}
	private float _LinearDeformation(float relDistance){
		float result = (1 - relDistance) * mEffectCoef / 10;
		return result > 1 ? 1 : result;
	}
	#endregion
}
