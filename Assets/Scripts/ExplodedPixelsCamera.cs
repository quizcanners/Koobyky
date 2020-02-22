using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ExplodedPixelsCamera : MonoBehaviour {
    public static ExplodedPixelsCamera inst;
    public Camera cam;
	// Use this for initialization
	void Start () {
        inst = this;
        if (cam == null) cam = GetComponent<Camera>();
	}

    public static void Render() {
        inst.cam.Render();
        inst.renderedThisTure = true;
    }

    bool renderedThisTure = false;


    private void LateUpdate() {
        if ((cam != null) && (!renderedThisTure))
            cam.Render();
        renderedThisTure = false;
    }
}
