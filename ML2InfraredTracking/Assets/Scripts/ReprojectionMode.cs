using System;
using MagicLeap.OpenXR.Features.Reprojection;
using UnityEngine;
using UnityEngine.XR.OpenXR;

public class ReprojectionTest : MonoBehaviour
{
    // set one of available reprojection modes: PlanarManual, PlanarFromDepth, Depth
    public MagicLeapReprojectionFeature.ReprojectionMode reprojectionMode = MagicLeapReprojectionFeature.ReprojectionMode.PlanarFromDepth;
    private MagicLeapReprojectionFeature reprojectionFeature;

    private void Start()
    {
        reprojectionFeature = OpenXRSettings.Instance.GetFeature<MagicLeapReprojectionFeature>();

        if (reprojectionFeature == null || !reprojectionFeature.enabled)
        {
            Debug.LogError("MagicLeapReprojectionFeature is not enabled!");
            enabled = false;
            return;
        }

        // Enable reprojeciton at start
        reprojectionFeature.EnableReprojection = true;

        reprojectionFeature.SetReprojectionMode(reprojectionMode);
        Debug.Log($"Reprojection mode set to: {reprojectionMode}");
    }
}