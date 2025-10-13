/*
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Calib3dModule;

public sealed class PoseEstimation
{
    public sealed class Result
    {
        public bool HasPose;
        public Matrix4x4 WorldTObject;   // world ← object
        public Vector3 PositionTool;
        public Quaternion RotationTool;
        public float ReprojectionError;
    }

    /// <summary>
    /// Function to estimate the pose of the tracked object in the world reference frame.
    /// </summary>
    public bool TryEstimateWorldPose(
        List<Vector2> blobCenters,              
        List<Vector3> objectPoints,             
        Mat cameraMatrix,
        MatOfDouble distCoeffs,
        ref Mat previousRvec,                   
        ref Mat previousTvec,                   
        kalmanFilterTracking kalman,           
        ref bool kalmanInitialized,
        ref float lastTrackingTime,
        Pose sensorWorldPose,                   
        out Result result)
    {
        result = new Result { HasPose = false };

     
        if (blobCenters == null || blobCenters.Count < 4 || objectPoints == null || objectPoints.Count < 4)
            return false;

        int count = blobCenters.Count;
        int pnpMethod;
        bool useExtrinsicGuess;

        // Choose the PnP solver based on how many markers were detected.
        // Rationale:
        // - With exactly 4 points we prefer AP3P or EPNP: it’s fast and typically stable if the marker configuration is not ambigous or too many points on the same line.
        // - With 5 co-planar points we switch to IPPE and enable useExtrinsicGuess=True: in practice
        //   this has shown better numerical stability and fewer flips/ambiguities.
        //
        // Feel free to tune this logic for your setup. An object with 5 markers is very stable

        switch (count)
        {   
            case 4:
                pnpMethod = Calib3d.SOLVEPNP_EPNP;
                useExtrinsicGuess = true;
                break;
            case 5:
                pnpMethod = Calib3d.SOLVEPNP_IPPE;
                useExtrinsicGuess = true;
                break;
            default:
                return false;
        }

        
        MatOfPoint2f imgPts = ConvertToMatOfPoint2f(blobCenters);
        var perms = GetPermutations(objectPoints, count);

        float bestErr = float.MaxValue;
        Mat bestRvec = null, bestTvec = null;
        MatOfPoint3f bestObjPts = null;

        // Because we do not know the correspondence between 2D and 3D points we apply a brute force approach computing the pose with all the permutations of 2D <-> 3D points and we find the best correspondence and the best pose as the one minimizing the reprojection error
        //N.B!! This method is robust up to 5 markers. if the object to track has more than 5 markers this approach is too slow.
        foreach (var perm in perms)
        {
            MatOfPoint3f objPts = ConvertToMatOfPoint3f(perm);

            Mat rvec, tvec;
            if (useExtrinsicGuess && previousRvec != null && previousTvec != null)
            {
                rvec = previousRvec.clone();
                tvec = previousTvec.clone();
            }
            else
            {
                rvec = new Mat(3, 1, CvType.CV_64F);
                tvec = new Mat(3, 1, CvType.CV_64F);
                rvec.put(0, 0, 0); rvec.put(1, 0, 0); rvec.put(2, 0, 0);
                tvec.put(0, 0, 0); tvec.put(1, 0, 0); tvec.put(2, 0, 0.5);
            }

            bool ok = Calib3d.solvePnP(objPts, imgPts, cameraMatrix, distCoeffs,
                                       rvec, tvec, useExtrinsicGuess, pnpMethod);

            if (ok)
            {
                float err = ComputeReprojectionError(rvec, tvec, objPts, imgPts, cameraMatrix, distCoeffs);
                if (err < bestErr)
                {
                    bestErr = err;
                    bestObjPts = objPts;
                    bestRvec?.release();
                    bestTvec?.release();
                    bestRvec = rvec.clone();
                    bestTvec = tvec.clone();
                }
            }

            rvec.release();
            tvec.release();
            objPts.release();
        }

        // Discard the frame if the reprojection error is more than a treshold (in this case 3 pixels)
        if (bestErr == float.MaxValue || bestErr >= 3f)
        {
            imgPts.release();
            bestRvec?.release();
            bestTvec?.release();
            return false;
        }

        
        // Refining the estimated pose using the best correspondence and best rotation and translation found in the previous step
        Calib3d.solvePnPRefineLM(bestObjPts, imgPts, cameraMatrix, distCoeffs, bestRvec, bestTvec);

        
        Mat R = new Mat();
        Calib3d.Rodrigues(bestRvec, R);
        Matrix4x4 camTobj = Matrix4x4.identity;
        for (int r = 0; r < 3; r++)
        {
            camTobj[r, 0] = (float)R.get(r, 0)[0];
            camTobj[r, 1] = (float)R.get(r, 1)[0];
            camTobj[r, 2] = (float)R.get(r, 2)[0];
            camTobj[r, 3] = (float)bestTvec.get(r, 0)[0];
        }
        camTobj[3, 3] = 1f;
        R.release();

        // Filtering the position with the Kalman filter
        Vector3 bestPos = new Vector3(camTobj.m03, camTobj.m13, camTobj.m23);
        if (!kalmanInitialized)
        {
            kalman.Initialize(bestPos);
            kalmanInitialized = true;
        }
        Vector3 filteredPos = kalman.Filter(bestPos);
        camTobj.m03 = filteredPos.x;
        camTobj.m13 = filteredPos.y;
        camTobj.m23 = filteredPos.z;

        // Warm-start per il prossimo frame
        previousRvec?.release();
        previousTvec?.release();
        previousRvec = bestRvec;
        previousTvec = bestTvec;

        lastTrackingTime = Time.time;

        // We compute the World_T_Object as World_T_depthsensor (given by the PIxelSensor API) and cam_T_obj (the pose of the object in the depth camera)
        Matrix4x4 worldTsensor = Matrix4x4.TRS(sensorWorldPose.position, sensorWorldPose.rotation, Vector3.one);
        Matrix4x4 worldTobject = worldTsensor * camTobj;

        result.HasPose = true;
        result.WorldTObject = worldTobject;
        result.PositionTool = worldTobject.GetColumn(3);
        result.RotationTool = Quaternion.LookRotation(worldTobject.GetColumn(2), worldTobject.GetColumn(1));
        result.ReprojectionError = bestErr;

        imgPts.release();
        return true;
    }

    // ===== Helpers (privati) =====
    private static MatOfPoint2f ConvertToMatOfPoint2f(List<Vector2> points)
    {
        var arr = points.Select(p => new Point(p.x, p.y)).ToArray();
        var mat = new MatOfPoint2f(); mat.fromArray(arr); return mat;
    }

    private static MatOfPoint3f ConvertToMatOfPoint3f(List<Vector3> points)
    {
        var arr = points.Select(p => new Point3(p.x, p.y, p.z)).ToArray();
        var mat = new MatOfPoint3f(); mat.fromArray(arr); return mat;
    }

    private static float ComputeReprojectionError(Mat rvec, Mat tvec,
        MatOfPoint3f objectPoints, MatOfPoint2f imagePoints, Mat K, MatOfDouble D)
    {
        MatOfPoint2f proj = new MatOfPoint2f();
        Calib3d.projectPoints(objectPoints, rvec, tvec, K, D, proj);
        var p = proj.toArray(); var q = imagePoints.toArray();
        float err = 0f;
        for (int i = 0; i < p.Length; i++)
        {
            float dx = (float)(p[i].x - q[i].x);
            float dy = (float)(p[i].y - q[i].y);
            err += dx * dx + dy * dy;
        }
        proj.release();
        return err / Mathf.Max(1, p.Length);
    }

    // Function to get the N permutation of my 3D object markers. N is the number of detected image points (blob centroid)
    private static IEnumerable<List<Vector3>> GetPermutations(List<Vector3> list, int length)
    {
        if (length == 1) { foreach (var it in list) yield return new List<Vector3> { it }; yield break; }
        for (int i = 0; i < list.Count; i++)
        {
            var current = list[i];
            var remaining = list.Where((_, idx) => idx != i).ToList();
            foreach (var perm in GetPermutations(remaining, length - 1))
            { perm.Insert(0, current); yield return perm; }
        }
    }
}
*/