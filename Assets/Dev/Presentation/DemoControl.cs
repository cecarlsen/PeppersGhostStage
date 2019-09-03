/*
	Copyright © Carl Emil Carlsen 2019
	http://cec.dk
*/

using UnityEngine;

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
    [SerializeField] State[] _states;

    TweenData _roomLightTween = new TweenData();
    TweenData _projectionLightTween = new TweenData();
    TweenData _stageLightTween = new TweenData();
    TweenData _cameraAngleTween = new TweenData();
    TweenData _cameraDistTween = new TweenData();
    TweenData _cameraOffsetYTween = new TweenData();
    TweenData _cameraPitchTween = new TweenData();

    Transform _cameraTransform;
    float roomLightIntensityMin;
    float[] _stageLightIntensityMax;

    int _stateIndex;


    static class ShaderIDs
    {
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
    }


	void Update()
	{
        // Handle input.
        if( Input.GetKeyDown( KeyCode.RightArrow ) && _stateIndex < _states.Length-1 ) _stateIndex++;
        else if( Input.GetKeyDown( KeyCode.LeftArrow ) && _stateIndex > 0  ) _stateIndex--;

        // Set state.
        State state = _states[_stateIndex];
        _roomLightTween.target = state.roomLight ? 1 : 0;
        _projectionLightTween.target = state.projectionLights ? 1 : 0;
        _stageLightTween.target = state.stageLights ? 1 : 0;
        _cameraAngleTween.target = state.cameraAngle;
        _cameraDistTween.target = state.cameraDist;
        _cameraOffsetYTween.target = state.cameraOffsetY;
        _cameraPitchTween.target = state.cameraPitch;

        // Update tweens,
        _roomLightTween.Update();
        _projectionLightTween.Update();
        _stageLightTween.Update();
        _cameraAngleTween.Update();
        _cameraDistTween.Update();
        _cameraOffsetYTween.Update();
        _cameraPitchTween.Update();

        // Apply.
        _roomLight.intensity = Mathf.Lerp( roomLightIntensityMin, _roomLightIntensityMax, _roomLightTween.value );
        Color projectionColor = Color.Lerp( Color.black, Color.white, _projectionLightTween.value );
        _projectorLightMaterial.SetColor( ShaderIDs.emissionColor, projectionColor );
        _canvasMaterial.SetColor( ShaderIDs.emissionColor, projectionColor );
        for( int i = 0; i < _stageLights.Length; i++ ) _stageLights[i].intensity = _stageLightTween.value * _stageLightIntensityMax[i];
        _profileLightMaterial.SetColor( ShaderIDs.emissionColor, Color.Lerp( Color.black, Color.white, _stageLightTween .value ) );

        _cameraRigTransform.position = _focusTransform.position + Quaternion.Euler( 0, _cameraAngleTween.value, 0 ) * Vector3.back * _cameraDistTween.value + Vector3.up * _cameraOffsetYTween.value;
        _cameraRigTransform.LookAt( _focusTransform );
        _cameraTransform.localRotation = Quaternion.AngleAxis( _cameraPitchTween.value, Vector3.right );
    }


    class TweenData
    {
        public float value;
        public float target;
        float _vel;

        public void Update()
        {
            value = Mathf.SmoothDamp( value , target, ref _vel, 0.5f );
        }
    }

    [System.Serializable]
    class State
    {
        public bool roomLight;
        public bool projectionLights;
        public bool stageLights;
        public float cameraAngle;
        public float cameraDist;
        public float cameraOffsetY;
        public float cameraPitch;
    }
}
