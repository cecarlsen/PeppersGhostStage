﻿// Modified from source: http://wiki.unity3d.com/index.php/MirrorReflection4

using UnityEngine;
using System.Collections;

namespace PeppersGhost
{
	[ExecuteInEditMode]
	public class MirrorReflection : MonoBehaviour
	{
		[SerializeField] int _resolution = 512;
		[SerializeField] float _clipPlaneOffset = 0.07f;
		[SerializeField] bool _flipNormal = false;
		[SerializeField] LayerMask _reflectLayers = -1;

		Hashtable _reflectionCameras = new Hashtable(); // Camera -> Camera table
		RenderTexture _reflectionTexture = null;
		int _oldReflectionTextureSize = 0;
		static bool isInsideRendering = false;

		int _refectionTexPropId = -1;
		Vector3 _normal;


		// This is called when it's known that the object will be rendered by some
		// camera. We render reflections and do other updates here.
		// Because the script executes in edit mode, reflections for the scene view
		// camera will just work!
		public void OnWillRenderObject()
		{
			if( _reflectLayers == -1 ) _refectionTexPropId = Shader.PropertyToID( "_ReflectionTex" );

			Renderer rend = GetComponent<Renderer>();
			if( !enabled || !rend || !rend.sharedMaterial || !rend.enabled ) return;

			Camera cam = Camera.current;
			if( !cam ) return;

			// Safeguard from recursive reflections.        
			if( isInsideRendering ) return;
			isInsideRendering = true;

			Camera reflectionCamera;
			CreateMirrorObjects( cam, out reflectionCamera );

			// find out the reflection plane: position and normal in world space
			Vector3 pos = transform.position;
			_normal = transform.up;
			if( _flipNormal ) _normal = -_normal;

			UpdateCameraModes( cam, reflectionCamera );

			// Render reflection
			// Reflect camera around reflection plane
			float d = -Vector3.Dot( _normal, pos ) - _clipPlaneOffset;
			Vector4 reflectionPlane = new Vector4( _normal.x, _normal.y, _normal.z, d );

			Matrix4x4 reflection = Matrix4x4.zero;
			CalculateReflectionMatrix (ref reflection, reflectionPlane);
			Vector3 oldpos = cam.transform.position;
			Vector3 newpos = reflection.MultiplyPoint( oldpos );
			reflectionCamera.worldToCameraMatrix = cam.worldToCameraMatrix * reflection;

			// Setup oblique projection matrix so that near plane is our reflection
			// plane. This way we clip everything below/above it for free.
			Vector4 clipPlane = CameraSpacePlane( reflectionCamera, pos, _normal, 1.0f );
			//Matrix4x4 projection = cam.projectionMatrix;
			Matrix4x4 projection = cam.CalculateObliqueMatrix(clipPlane);
			reflectionCamera.projectionMatrix = projection;

			reflectionCamera.cullingMask = ~(1<<4) & _reflectLayers.value; // never render water layer
			reflectionCamera.targetTexture = _reflectionTexture;
			GL.invertCulling = true;
			reflectionCamera.transform.position = newpos;
			Vector3 euler = cam.transform.eulerAngles;
			reflectionCamera.transform.eulerAngles = new Vector3( 0, euler.y, euler.z );
			reflectionCamera.Render();
			reflectionCamera.transform.position = oldpos;
			GL.invertCulling = false;
			Material[] materials = rend.sharedMaterials;
			foreach( Material mat in materials ) {
				if( mat.HasProperty( _refectionTexPropId ) ) mat.SetTexture( _refectionTexPropId, _reflectionTexture );
			}

			isInsideRendering = false;
		}


		void OnDisable()
		{
			if( _reflectionTexture ) {
				_reflectionTexture.Release();
				_reflectionTexture = null;
			}
			foreach( DictionaryEntry kvp in _reflectionCameras ) DestroyImmediate( ((Camera)kvp.Value).gameObject );
			_reflectionCameras.Clear();
		}


		void OnDrawGizmosSelected()
		{
			Renderer rend = GetComponent<Renderer>();
			if( !rend ) return;

			Vector3 center = rend.bounds.center;

			Gizmos.color = Color.green;
			Gizmos.DrawLine( center, center + _normal );
		}


		void UpdateCameraModes( Camera src, Camera dest )
		{
			if( dest == null ) return;

			// set camera to clear the same way as current camera
			dest.clearFlags = src.clearFlags;
			dest.backgroundColor = src.backgroundColor;        
			if( src.clearFlags == CameraClearFlags.Skybox )
			{
				Skybox sky = src.GetComponent(typeof(Skybox)) as Skybox;
				Skybox mysky = dest.GetComponent(typeof(Skybox)) as Skybox;
				if( !sky || !sky.material )
				{
					mysky.enabled = false;
				}
				else
				{
					mysky.enabled = true;
					mysky.material = sky.material;
				}
			}
			// update other values to match current camera.
			// even if we are supplying custom camera&projection matrices,
			// some of values are used elsewhere (e.g. skybox uses far plane)
			dest.farClipPlane = src.farClipPlane;
			dest.nearClipPlane = src.nearClipPlane;
			dest.orthographic = src.orthographic;
			dest.fieldOfView = src.fieldOfView;
			dest.aspect = src.aspect;
			dest.orthographicSize = src.orthographicSize;
		}


		// On-demand create any objects we need
		void CreateMirrorObjects( Camera currentCamera, out Camera reflectionCamera )
		{
			reflectionCamera = null;

			// Reflection render texture
			if( !_reflectionTexture || _oldReflectionTextureSize != _resolution )
			{
				if( _reflectionTexture )
					DestroyImmediate( _reflectionTexture );
				_reflectionTexture = new RenderTexture( _resolution, _resolution, 16 );
				_reflectionTexture.name = "__MirrorReflection" + GetInstanceID();
				_reflectionTexture.isPowerOfTwo = true;
				_reflectionTexture.hideFlags = HideFlags.DontSave;
				_oldReflectionTextureSize = _resolution;
			}

			// Camera for reflection
			reflectionCamera = _reflectionCameras[currentCamera] as Camera;
			if( !reflectionCamera ) // catch both not-in-dictionary and in-dictionary-but-deleted-GO
			{
				GameObject go = new GameObject( "Mirror Refl Camera id" + GetInstanceID() + " for " + currentCamera.GetInstanceID(), typeof(Camera), typeof(Skybox) );
				reflectionCamera = go.GetComponent<Camera>();
				reflectionCamera.enabled = false;
				reflectionCamera.transform.position = transform.position;
				reflectionCamera.transform.rotation = transform.rotation;
				reflectionCamera.gameObject.AddComponent<FlareLayer>();
				go.hideFlags = HideFlags.HideAndDontSave;
				_reflectionCameras[currentCamera] = reflectionCamera;
			}        
		}

		// Extended sign: returns -1, 0 or 1 based on sign of a
		static float sgn(float a)
		{
			if (a > 0.0f) return 1.0f;
			if (a < 0.0f) return -1.0f;
			return 0.0f;
		}

		// Given position/normal of the plane, calculates plane in camera space.
		Vector4 CameraSpacePlane (Camera cam, Vector3 pos, Vector3 normal, float sideSign)
		{
			Vector3 offsetPos = pos + normal * _clipPlaneOffset;
			Matrix4x4 m = cam.worldToCameraMatrix;
			Vector3 cpos = m.MultiplyPoint( offsetPos );
			Vector3 cnormal = m.MultiplyVector( normal ).normalized * sideSign;
			return new Vector4( cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos,cnormal) );
		}

		// Calculates reflection matrix around the given plane
		static void CalculateReflectionMatrix (ref Matrix4x4 reflectionMat, Vector4 plane)
		{
			reflectionMat.m00 = (1F - 2F*plane[0]*plane[0]);
			reflectionMat.m01 = (   - 2F*plane[0]*plane[1]);
			reflectionMat.m02 = (   - 2F*plane[0]*plane[2]);
			reflectionMat.m03 = (   - 2F*plane[3]*plane[0]);

			reflectionMat.m10 = (   - 2F*plane[1]*plane[0]);
			reflectionMat.m11 = (1F - 2F*plane[1]*plane[1]);
			reflectionMat.m12 = (   - 2F*plane[1]*plane[2]);
			reflectionMat.m13 = (   - 2F*plane[3]*plane[1]);

			reflectionMat.m20 = (   - 2F*plane[2]*plane[0]);
			reflectionMat.m21 = (   - 2F*plane[2]*plane[1]);
			reflectionMat.m22 = (1F - 2F*plane[2]*plane[2]);
			reflectionMat.m23 = (   - 2F*plane[3]*plane[2]);

			reflectionMat.m30 = 0F;
			reflectionMat.m31 = 0F;
			reflectionMat.m32 = 0F;
			reflectionMat.m33 = 1F;
		}
	}
}