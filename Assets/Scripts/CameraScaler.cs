using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class CameraScaler : MonoBehaviour {

    public static CameraScaler inst;

    public float orthogonalWidth = 7;
    public float orthogonalHeight = 7;

    public Camera RenderTexCamera;

    private void Awake() {
        inst = this;
    }


    float delay;
    // Use this for initialization
    void Update () {
        Camera c = Camera.main;

        float proportion = Screen.width / (float)Screen.height;
        float target = orthogonalWidth / orthogonalHeight;

        c.orthographicSize = Mathf.Lerp(c.orthographicSize, Mathf.Max(orthogonalHeight, orthogonalWidth * (target / proportion)), Time.deltaTime);
      
        delay -= Time.deltaTime;
        if (delay < 0) { RenderTexCamera.Render(); delay = 0.01f; }
       // outline.SetColor("_Color", tmpCol);
        //c.backgroundColor = Color.Lerp(c.backgroundColor, col, Time.deltaTime);

    }
	

}
