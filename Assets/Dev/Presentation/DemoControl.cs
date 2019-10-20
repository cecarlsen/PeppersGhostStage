/*
	Copyright © Carl Emil Carlsen 2019
	http://cec.dk
*/

using UnityEngine;
using System;
using System.Collections.Generic;

public class DemoControl : MonoBehaviour
{
    [SerializeField] Transform _cameraRigTransform;
    [SerializeField] Transform _focusTransform;
    [SerializeField] Material _profileLightMaterial;
    [SerializeField] Material _projectorLightMaterial;
    [SerializeField] Material _canvasMaterial;
    [SerializeField] Light[] _stageLights;
    [SerializeField] Light _roomLight;
    [SerializeField] float _roomLightIntensityMax = 13f;
    [SerializeField] Material _fenseMaterial;
    [SerializeField] State[] _states;

    TweenData _roomLightTween = new TweenData();
    TweenData _projectionLightTween = new TweenData();
    TweenData _stageLightTween = new TweenData();
    TweenData _cameraAngleTween = new TweenData();
    TweenData _cameraDistTween = new TweenData();
    TweenData _cameraOffsetXTween = new TweenData();
    TweenData _cameraOffsetYTween = new TweenData();
    TweenData _cameraPitchTween = new TweenData();
    TweenData _fenseTween = new TweenData();

    Transform _cameraTransform;
    float roomLightIntensityMin;
    float[] _stageLightIntensityMax;
    Color _fenseColor;

    int _stateIndex;
    State _state;

    Dictionary<Material,TweenData> _tweeningMaterials = new Dictionary<Material,TweenData>();
    


    static class ShaderIDs
    {
        public static readonly int color = Shader.PropertyToID( "_Color" );
        public static readonly int emissionColor = Shader.PropertyToID( "_EmissionColor" );
    }


    void Awake()
	{
        Application.targetFrameRate = 60;

        _cameraTransform = _cameraRigTransform.GetComponentInChildren<Camera>().transform;

        _stageLightIntensityMax = new float[_stageLights.Length];
        for( int i = 0; i < _stageLights.Length; i++ ) _stageLightIntensityMax[i] = _stageLights[i].intensity;

        roomLightIntensityMin = _roomLight.intensity;

        _roomLightTween.value = 1;
        _roomLightTween.target = 1;

        _cameraDistTween.value = _states[0].cameraDist;

        _fenseColor = _fenseMaterial.GetColor( ShaderIDs.color );

        UpdateState();
    }


    void OnDestroy()
    {
        foreach( Material mat in _state.highlightMaterials ) mat.SetColor( ShaderIDs.emissionColor, Color.black );
        _fenseColor.a = 1;
        _fenseMaterial.SetColor( ShaderIDs.color, _fenseColor );
    }


    void Update()
	{
        // Handle input.
        if( Input.anyKeyDown ) {
            if( Input.GetKeyDown( KeyCode.RightArrow ) && _stateIndex < _states.Length-1 ) _stateIndex++;
            else if( Input.GetKeyDown( KeyCode.LeftArrow ) && _stateIndex > 0  ) _stateIndex--;
            UpdateState();
        }

        _roomLightTween.target = _state.roomLight ? 1 : 0;
        _projectionLightTween.target = _state.projectionLights ? 1 : 0;
        _stageLightTween.target = _state.stageLights ? 1 : 0;
        _fenseTween.target = _state.fense ? 1 : 0f;
        _cameraAngleTween.target = _state.cameraAngle;
        _cameraDistTween.target = _state.cameraDist;
        _cameraOffsetXTween.target = _state.cameraOffsetX;
        _cameraOffsetYTween.target = _state.cameraOffsetY;
        _cameraPitchTween.target = _state.cameraPitch;

        // Update tweens.
        _roomLightTween.Update();
        _projectionLightTween.Update();
        _stageLightTween.Update();
        _fenseTween.Update();
        _cameraAngleTween.Update();
        _cameraDistTween.Update();
        _cameraOffsetXTween.Update();
        _cameraOffsetYTween.Update();
        _cameraPitchTween.Update();

        // Highlight tweens.
        foreach( KeyValuePair<Material,TweenData> pair in _tweeningMaterials ) {
            pair.Value.Update();
            pair.Key.SetColor( ShaderIDs.emissionColor, new Color( pair.Value.value * 0.5f, 0, 0 ) );
        }

        // Apply.
        _roomLight.intensity = Mathf.Lerp( roomLightIntensityMin, _roomLightIntensityMax, _roomLightTween.value );
        Color projectionColor = Color.Lerp( Color.black, Color.white, _projectionLightTween.value );
        _projectorLightMaterial.SetColor( ShaderIDs.emissionColor, projectionColor );
        _canvasMaterial.SetColor( ShaderIDs.emissionColor, projectionColor );
        for( int i = 0; i < _stageLights.Length; i++ ) _stageLights[i].intensity = _stageLightTween.value * _stageLightIntensityMax[i];
        _profileLightMaterial.SetColor( ShaderIDs.emissionColor, Color.Lerp( Color.black, Color.white, _stageLightTween .value ) );
        _fenseColor.a = _fenseTween.value;
        _fenseMaterial.SetColor( ShaderIDs.color, _fenseColor );

        _cameraRigTransform.position = _focusTransform.position + Quaternion.Euler( 0, _cameraAngleTween.value, 0 ) * Vector3.back * _cameraDistTween.value + Vector3.up * _cameraOffsetYTween.value;
        _cameraRigTransform.LookAt( _focusTransform );
        _cameraTransform.localPosition = Vector3.right * _cameraOffsetXTween.value;
        _cameraTransform.localRotation = Quaternion.AngleAxis( _cameraPitchTween.value, Vector3.right );
    }


    void UpdateState()
    {
        _state = _states[_stateIndex];
        foreach( TweenData dat in _tweeningMaterials.Values ) dat.target = 0;
        foreach( Material mat in _state.highlightMaterials ) {
            TweenData dat;
            if( !_tweeningMaterials.TryGetValue( mat, out dat ) ) {
                dat = new TweenData();
                dat.duration = 0.2f;
                _tweeningMaterials.Add( mat, dat );
            }
            dat.target = 1;
            dat.delay = _state.highlightMaterialDelay;
        }

        Debug.Log( _state.name );
    }


    class TweenData
    {
        public float delay;
        public float value;
        public float target;
        public float duration = 0.5f;
        float _vel;

        public void Update()
        {
            if( delay > 0 ) {
                delay -= Time.deltaTime;
                return;
            }
            value = Mathf.SmoothDamp( value , target, ref _vel, duration );
        }
    }

    [System.Serializable]
    class State
    {
        public string name;
        public bool roomLight;
        public bool projectionLights;
        public bool stageLights;
        public bool fense = true;
        public float cameraAngle;
        public float cameraDist;
        public float cameraOffsetX;
        public float cameraOffsetY;
        public float cameraPitch;
        public float highlightMaterialDelay = 1.7f;
        public Material[] highlightMaterials;
    }
}
