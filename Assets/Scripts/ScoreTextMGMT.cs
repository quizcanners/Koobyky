using System.Collections;
using System.Collections.Generic;
using QuizCannersUtilities;
using TMPro;
using UnityEngine;

public class ScoreTextMGMT : MonoBehaviour
{
  
    public TextMeshProUGUI text;
    private float currentScore = 0;
    public int targetScore = 0;

    private ShaderProperty.FloatValue faceDialate = new ShaderProperty.FloatValue("_FaceDilate");



    public void Restart(int score = 0) {
        currentScore = score+1;
        targetScore = score;
    }

    void Update() {

        if (currentScore != targetScore) {
            LerpUtils.IsLerpingBySpeed(ref currentScore, targetScore, 75);

            float diff = Mathf.Max(0,targetScore - currentScore);

            faceDialate.SetOn(text.fontSharedMaterial, Mathf.Clamp01(Random.Range(diff*0.5f, diff) *0.01f)*0.6f);
            text.text = ((int) currentScore).ToString();
        }

    }
}
