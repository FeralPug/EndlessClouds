using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CloudMaterialSettings
{
    [Header("Cloud Color")]
    public Color color = Color.white;
   
    [Header("BrightnessBoost"), Range(0f, 1f)]
    public float CloudBrightnessBoost = 0.6f;

    [Header("Sun Brightness Size"), Range(0f, 1f)]
    public float SunHighlightsSize = 0.7f;

    [Header("Alpha Fade Cutoff"), Range(0f, 1f)]
    public float cloudAlphaCutoff = 0.2f;

    [Header("Distance Fade Min")]
    public float CloudAlphaFadeDistance = 900f;

    [Header("Cloud Shadow Amount"), Range(0f, 1f)]
    public float cloudShadowAmount = 0.2f;

    [Header("Cloud Shadow Value"), Range(0f, 1f)]
    public float cloudShadowValue = 0.3f;

    [Header("Cloud Texture (R)")]
    public Texture2D cloudTexture;

    public CloudTextureSettings TexSettings1;

    public CloudTextureSettings TexSettings2;

    [Header("Horizon Bending"), Range(0.000001f, 0.001f)]
    public float bending = 0.00001f;
}

[System.Serializable]
public struct CloudTextureSettings
{
    [Header("Tex Offset & Scale")]
    public Vector4 CloudST;

    [Header("Wind Direction")]
    public Vector2 CloudDirection;

    [Header("Wind Speed")]
    public float CloudSpeed;
}
