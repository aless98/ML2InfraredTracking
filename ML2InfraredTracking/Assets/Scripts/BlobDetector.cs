/*
using System;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Features2dModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using UnityEngine;

public static class BlobDetector
{
    

    public static List<Vector2> FindBlobs(
        Mat binary)
    {
        var centers = new List<Vector2>();
        if (binary == null || binary.empty()) return centers;

        SimpleBlobDetector_Params param = new SimpleBlobDetector_Params();

        param.set_filterByColor(false);
        param.set_filterByArea(true);
        param.set_minArea(10);
        param.set_maxArea(100000);
        param.set_filterByCircularity(true);
        param.set_minCircularity(0.65f);
        param.set_filterByInertia(false);
        param.set_minInertiaRatio(0.6f);
        param.set_filterByConvexity(false);

        SimpleBlobDetector blobDetector = SimpleBlobDetector.create(param);

        var kps = new MatOfKeyPoint();
        blobDetector.detect(binary, kps);

        foreach (var kp in kps.toArray())
            centers.Add(new Vector2((float)kp.pt.x, (float)kp.pt.y));

        kps.Dispose();
        blobDetector.Dispose();

        return centers;
    }



}
*/
