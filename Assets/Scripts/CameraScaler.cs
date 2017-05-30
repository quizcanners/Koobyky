using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class CameraScaler : MonoBehaviour {

    public float orthogonalWidth = 7;
    public float orthogonalHeight = 7;
	// Use this for initialization
	void Update () {
        Camera c = Camera.main;

        float proportion = Screen.width / (float)Screen.height;
        float target = orthogonalWidth / orthogonalHeight;

        c.orthographicSize = Mathf.Lerp(c.orthographicSize, Mathf.Max(orthogonalHeight, orthogonalWidth * (target / proportion)), Time.deltaTime);

	}
	

}
