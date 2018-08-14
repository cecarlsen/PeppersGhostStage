/*
    Copyright © Carl Emil Carlsen 2018
    http://cec.dk
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoCamera : MonoBehaviour
{
    [SerializeField] Transform _forusTransform;
    [SerializeField] float _distance = 8; // m
    [SerializeField] float _cycleDuration = 4; // m
    [SerializeField] float _angleRange = 75f;
    [SerializeField] float _offsetY = 1.5f;


    float _angle;


	void Start()
	{
		
	}
	
	
    void Update()
	{
        _angle += ( Time.deltaTime * Mathf.PI * 2 ) / _cycleDuration;
        float t = Mathf.Sin( _angle );
        transform.position = _forusTransform.position + Quaternion.Euler( 0, t * _angleRange, 0 ) * Vector3.back * _distance + Vector3.up * _offsetY; 
        transform.LookAt( _forusTransform );
	}
}