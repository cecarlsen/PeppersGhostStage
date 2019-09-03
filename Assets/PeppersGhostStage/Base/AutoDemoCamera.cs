/*
    Copyright © Carl Emil Carlsen 2018
    http://cec.dk
*/

using UnityEngine;

public class AutoDemoCamera : MonoBehaviour
{
    [SerializeField] Transform _focusTransform;
    [SerializeField] float _distance = 8; // m
    [SerializeField] float _cycleDuration = 4; // m
    [SerializeField] float _angleMin = 0;
    [SerializeField] float _angleMax = 100f;
    [SerializeField] float _offsetY = 1.5f;

    float _driveAngle = -Mathf.PI * 0.5f;
	
	
    void Update()
	{
        _driveAngle += ( Time.deltaTime * Mathf.PI * 2 ) / _cycleDuration;
        float t = Mathf.Sin( _driveAngle ) * 0.5f + 0.5f;
        float angle = Mathf.Lerp( _angleMin, _angleMax, t );
        transform.position = _focusTransform.position + Quaternion.Euler( 0, angle, 0 ) * Vector3.back * _distance + Vector3.up * _offsetY; 
        transform.LookAt( _focusTransform );
	}
}