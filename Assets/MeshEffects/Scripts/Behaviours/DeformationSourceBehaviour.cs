using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class DeformationSourceBehaviour : MonoBehaviour {

	public enum DeformationSourceType{
		Directionnal, Point, Spirale
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
			if (obj.GetType () == typeof(DeformationSourceBehaviour)) {
				DeformationSourceBehaviour source = (DeformationSourceBehaviour)obj;
				return source.effectRange == range && source.effectCoef == coef && base.Equals(source.transform);
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
	[SerializeField]
	public float mEffectRange = 1.0f;
	[SerializeField]
	private float mEffectCoef = 1.0f;
	[SerializeField]
	private bool mEffectIsExponential = false;

	[SerializeField]
	private DeformationSourceType mDeformationType = DeformationSourceType.Point;

	[SerializeField]
	private LayerMask mCullingMask = 255;

	// refreshing
	[SerializeField]
	private bool mRefreshOnMove = true;
	[SerializeField]
	private bool mRefreshOnObjectsMove = true;

	// directionnal attributes
	[SerializeField]
	private bool mIsBidirectionnal = true;

	// Spirale attributes
	[SerializeField]
	private bool mRotateAroundSource = true;
	[SerializeField]
	private float mSpiraleAmplitude = 360;
	#endregion

	#region PRIVATE_ATTRIBUTES
	// for original and new meshes
	private List<GameObject> mDeformedObjects = new List<GameObject>();
	private List<Vector3[]> mOriginalMeshes = new List<Vector3[]>();

	// delegate to determine the operation to apply for the effect (linear or exponential)
	private delegate float EffectValueMethod(float x);
	private EffectValueMethod mEffectValueMethod;

	// movement memo
	private List<ObjectTransformState> mPreviousTransforms;
	private DeformationSourceState mSourcePreviousTransform;

	#endregion

	#region MONOBEHAVIOUR

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
	public bool isBidirectionnal{
		get{return mIsBidirectionnal;}
		set{mIsBidirectionnal = value;}
	}
	public bool refreshOnObjectsMove{
		set{
			mRefreshOnObjectsMove = value;
			if(mRefreshOnObjectsMove && mPreviousTransforms != null){
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
		set{mRefreshOnMove = value;}
		get{return mRefreshOnMove;}
	}
	public float effectRange{
		set{mEffectRange = value;}
		get{return mEffectRange;}
	}
	public float effectCoef{
		set{mEffectCoef = value;}
		get{return mEffectCoef;}
	}
	public DeformationSourceType deformationType{
		get{return mDeformationType;}
		set{mDeformationType = value;}
	}
	public LayerMask cullingMask{
		get{return mCullingMask;}
		set{mCullingMask = value;}
	}
	public bool rotateAroundSource{
		get{return mRotateAroundSource;}
		set{mRotateAroundSource = value;}
	}
	public float spiraleAmplitude{
		get{return mSpiraleAmplitude;}
		set{mSpiraleAmplitude = value;}
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

		if(mRefreshOnObjectsMove) {
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
	 * Remove deformation on all objects
	 * */
	public void ResetDeformations(){
		for (int i = 0; i < mDeformedObjects.Count; i++) {
			_ResetDeformationOn(mDeformedObjects[i],mOriginalMeshes[i]);
		}
	}

	/**
	 * Apply deformation on all objects
	 * */
	public void ApplyDeformations(){
		for (int i = 0; i < mDeformedObjects.Count; i++) {
			_ApplyDeformationOn(mDeformedObjects[i],mOriginalMeshes[i]);
		}
	}

	/**
	 * Remove deformation effect on mesh
	 * */
	private void _ResetDeformationOn(GameObject obj,Vector3[] vertices){
		MeshFilter meshFilter = obj.GetComponent<MeshFilter> ();
		meshFilter.mesh.vertices = vertices;
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

				// if it is not bidirectionnal and the point is not in the good side
				if(!mIsBidirectionnal && vertexDistance < 0){
					vertexDistance = mEffectRange + 1;
					break;
				}
				
				direction = vertexDistance * -transform.up;

				if(vertexDistance < 0){
					vertexDistance *= -1;
				}
				break;
			case DeformationSourceType.Spirale:
				vertexDistance = Vector3.Distance (vertex, this.transform.position);
				direction = (transform.position - vertex);
				break;
			}

			if(vertexDistance <= mEffectRange){
				float relDistance = vertexDistance / mEffectRange;
				float effect = mEffectValueMethod(relDistance);

				vertex += direction * effect;

				// deformation spirale, apply rotation to each vertex
				if(mDeformationType == DeformationSourceType.Spirale){
					Vector3 rotationAxis = 
						mRotateAroundSource ? transform.up
							: obj.transform.up;
					float angle = (1 - relDistance) * mSpiraleAmplitude;
					Quaternion rotation = Quaternion.Euler(rotationAxis * angle);
					// local positionning
					vertex -= transform.position;
					// rotate the point
					vertex = rotation * vertex;
					// replace in world
					vertex += transform.position;	
				}
				
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

	#region UTILS

	#endregion

	#region DELEGATE
	private float _ExponentialDeformation(float relDistance){
		return relDistance <= 0 ? 1 : 1 - Mathf.Exp (-relDistance * mEffectCoef);
	}
	private float _LinearDeformation(float relDistance){
		float result = (1 - relDistance) * mEffectCoef;
		return result > 1 ? 1 : result;
	}
	#endregion
}
