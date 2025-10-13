/*
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MagicLeap.OpenXR.Features.PixelSensors;
using OpenCVForUnity.CoreModule;
using System.Diagnostics;
using OpenCVForUnity.ImgprocModule;


public class IRToolTrack : MonoBehaviour
{
    // Renderer that shows the depth raw frame texture
    [SerializeField] private Renderer targetRenderer;
    // Mapping from PixelSensorFrameType -> Material, used to visualize different frame types
    [SerializeField] private PixelSensorMaterialTable materialTable = new();
    // List of marker GameObjects (one per marker) whose local positions define the 3D model points
    [SerializeField] List<GameObject> Markers;
   

    // The object in the scene that will be driven by the estimated pose
    public GameObject trackedobj;
    List<Vector3> objectPoints = new List<Vector3>();

    // Camera intrinsics and distortion parameters (OpenCV mats)
    private Mat cameraMatrix = new Mat(3, 3, CvType.CV_64F);
    private MatOfDouble distCoeffs = new MatOfDouble(0, 0, 0, 0, 0);

    // GPU textures used to preview raw and filtered images
    private Texture2D targetTexture;

    // CPU-side reusable buffer to copy float depth frames and materials declaration
    private float[] _floatBuffer;
    MaterialPropertyBlock _mpb, _mpbFiltered;
   

    // Depth range used to scale/visualize depth textures
    private float minDepth = 0;
    private float maxDepth = 5;

    // vectors used for estimating the pose of the tracked object
    private Mat rvec = new Mat();
    private Mat tvec = new Mat();
    private Mat previousRvec = null;
    private Mat previousTvec = null;
    private Vector3 finalPosition;
    private Quaternion finalRotation;
    Quaternion previousFilteredRot = Quaternion.identity;

    // Simple Kalman filter wrapper 
    kalmanFilterTracking filter;
    bool kf_initialized = false;
    float lastStamp = 0f;
    float lastTrackingTime = 0f;
    float maxNoTrackingDuration = 2f; // in seconds

    // Pose estimator class
    private readonly PoseEstimation poseEstimator = new PoseEstimation();

   
   
    public void Reset()
    {
        if (targetTexture == null)
        {
            return;
        }
        Destroy(targetTexture);
        targetTexture = null;
    }

    private void Start()
    {
       // Initialize render material properties, Kalman filter, object markers and custom depth sesor camera matrix parameters.
       // These were the results of a calibration process because the one provided by the MagicLeap2 metadata were inaccurate
      
        _mpb = new MaterialPropertyBlock();
        var materialToUse = materialTable.GetMaterialForFrameType(PixelSensorFrameType.DepthRaw);
        targetRenderer.sharedMaterial = materialToUse; // use sharedMaterial to avoid instancing

        
        filter = new kalmanFilterTracking();

        foreach (GameObject go in Markers)
            objectPoints.Add(go.transform.localPosition);

 
        cameraMatrix.put(0, 0, 365.3747);
        cameraMatrix.put(0, 1, 0);
        cameraMatrix.put(0, 2, 262.2158);
        cameraMatrix.put(1, 0, 0);
        cameraMatrix.put(1, 1, 364.9302);
        cameraMatrix.put(1, 2, 240.9972);
        cameraMatrix.put(2, 0, 0);
        cameraMatrix.put(2, 1, 0);
        cameraMatrix.put(2, 2, 1);
        distCoeffs = new MatOfDouble(-0.1052, 0.0647, 0, 0, -0.1254);
    }

    private void OnDestroy()
    {
        // Cleanup GPU textures and hardware buffer resources
        Reset();

        // Clean up instantiated material if any (defensive)
        if (targetRenderer.material != null)
        {
            Destroy(targetRenderer.material);
        }
    }


    // Called once when a stream is selected: reads supported min/max depth from capabilities
    public void Initialize(uint streamId, MagicLeapPixelSensorFeature pixelSensorFeature, PixelSensorId sensorType)
    {
        // Only for depth-capable sensors
        if (!sensorType.SensorName.Contains("depth", StringComparison.CurrentCultureIgnoreCase))
        {
            return;
        }

        // Query depth capability (range)
        if (!pixelSensorFeature.QueryPixelSensorCapability(sensorType, PixelSensorCapabilityType.Depth, streamId, out var range))
        {
            return;
        }

        // Try both integer and float ranges (some firmwares expose one or the other)
        if (range.IntRange.HasValue)
        {
            minDepth = range.IntRange.Value.Min;
            maxDepth = range.IntRange.Value.Max;
        }

        if (range.FloatRange.HasValue)
        {
            minDepth = range.FloatRange.Value.Min;
            maxDepth = range.FloatRange.Value.Max;
        }
    }

    // Main per-frame entry: builds textures, runs OpenCV preprocessing, PoseEstimation, and finally updates 'finalPosition' & 'finalRotation'
    public void ProcessFrame(in PixelSensorFrame frame, in PixelSensorMetaData[] metaData, in Pose sensorPose)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // Early-out if frame is invalid or there’s nothing to render into
        if (!frame.IsValid || targetRenderer == null || frame.Planes.Length == 0)
        {
            return;
        }

        // If you wanted to auto-fill camera matrix intrinsics from metadata instead of using the custom one, uncomment this.
        // It is commented out on purpose since we are using custom calibrated values.
        
//        for (int i = 0; i < metaData.Length; i++)
//        {
//            var Data = metaData[i];
//             switch (Data)
//             {
//                 case PixelSensorPinholeIntrinsics pinholeIntrinsics:
//                     {
//                         // Example: read FOV, focal length, principal point, distortion
//                         cameraMatrix.put(0, 0, pinholeIntrinsics.FocalLength[0]);
//                         cameraMatrix.put(0, 1, 0);
//                         cameraMatrix.put(0, 2, pinholeIntrinsics.PrincipalPoint[0]);
//                         cameraMatrix.put(1, 0, 0);
//                         cameraMatrix.put(1, 1, pinholeIntrinsics.FocalLength[1]);
//                         cameraMatrix.put(1, 2, pinholeIntrinsics.PrincipalPoint[1]);
//                         cameraMatrix.put(2, 0, 0);
//                         cameraMatrix.put(2, 1, 0);
//                         cameraMatrix.put(2, 2, 1);
//                         distCoeffs = new MatOfDouble(
//                             pinholeIntrinsics.Distortion[0],
//                             pinholeIntrinsics.Distortion[1],
//                             pinholeIntrinsics.Distortion[2],
//                             pinholeIntrinsics.Distortion[3],
//                             pinholeIntrinsics.Distortion[4]);
//                        break;
//                     }
//             }
//         }
        

        if (_mpb == null) _mpb = new MaterialPropertyBlock();
        if (_mpbFiltered == null) _mpbFiltered = new MaterialPropertyBlock();

        var frameType = frame.FrameType;
        var firstPlane = frame.Planes[0];

       
        switch (frameType)
        {
            
            case PixelSensorFrameType.DepthRaw:
                {   
                    // Render raw depth sensor data 
                    Utils.EnsureTargetTexture(ref targetTexture, frameType, (int)firstPlane.Width, (int)firstPlane.Height);
                    Utils.UploadMainTexture(frameType, ref firstPlane, targetTexture);

                    // 1) Copia i float della texture in un buffer riusabile
                    int pxCount = targetTexture.width * targetTexture.height;
                    if (_floatBuffer == null || _floatBuffer.Length != pxCount)
                        _floatBuffer = new float[pxCount];

                    targetTexture.GetPixelData<float>(0).CopyTo(_floatBuffer);

                    var imgMat = new Mat(targetTexture.height, targetTexture.width, CvType.CV_32FC1);
                    var byteMat = new Mat();
                    var kernel = Imgproc.getStructuringElement(Imgproc.MORPH_RECT, new Size(3, 3));
                    var resultFloatMat = new Mat();

                    imgMat.put(0, 0, _floatBuffer);
                    Core.normalize(imgMat, imgMat, 0, 1, Core.NORM_MINMAX);
                    imgMat.convertTo(byteMat, CvType.CV_8UC1, 255.0);

                    Imgproc.GaussianBlur(byteMat, byteMat, new Size(3, 3), 0);
                    Imgproc.threshold(byteMat, byteMat, 180, 255, Imgproc.THRESH_BINARY);
                    Imgproc.morphologyEx(byteMat, byteMat, Imgproc.MORPH_OPEN, kernel);
                    Imgproc.morphologyEx(byteMat, byteMat, Imgproc.MORPH_CLOSE, kernel);

                    byteMat.convertTo(resultFloatMat, CvType.CV_32FC1, 1.0 / 255.0);


                    List<Vector2> blobCenters = BlobDetector.FindBlobs(byteMat);
                    if (blobCenters.Count != 0) UnityEngine.Debug.Log($"coarse centers: {blobCenters[0]}");

                    Utils.DrawBlobsMarkers(targetTexture, blobCenters, 3, 6, Color.green);
                    targetRenderer.material.mainTexture = targetTexture;
       
                    float now = Time.time;
                                    

                    // --- POSE ESTIMATION ---
                    var okPose = poseEstimator.TryEstimateWorldPose(
                        blobCenters,
                        objectPoints,
                        cameraMatrix,
                        distCoeffs,
                        ref previousRvec,
                        ref previousTvec,
                        filter,
                        ref kf_initialized,
                        ref lastTrackingTime,
                        sensorPose,
                        out var poseRes
                    );

                    // Timestamp
                    lastStamp = now;

                    if (okPose && poseRes.HasPose)
                    {   
                        //Apply filtered tracked tool position and smoothed rotation
                        finalPosition = poseRes.PositionTool;
                        finalRotation = Quaternion.Slerp(previousFilteredRot, poseRes.RotationTool, 0.8f);
                        previousFilteredRot = finalRotation;

                        UnityEngine.Debug.Log($"Best PnP error: {poseRes.ReprojectionError}");
                        UnityEngine.Debug.Log($"Best Matrix: {poseRes.WorldTObject}");
                    }
                    else
                    {
                        // If i do not have tracking for more than my maxtime, reinitialize Kalman filter
                        if ((Time.time - lastTrackingTime) > maxNoTrackingDuration)
                        {
                            kf_initialized = false;
                            UnityEngine.Debug.LogWarning("Tracking lost for too long. Kalman re-initialized.");
                        }
                    }

                    break;
                }      

        }
        
    }

    private void Update()
    {
        // Apply the latest computed pose to the tracked object in the scene
        trackedobj.transform.SetPositionAndRotation(finalPosition, finalRotation);
    }

    

    // Data structure to configure which material to use for each incoming frame type
    [Serializable]
    public class PixelSensorMaterialTable
    {
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private List<PixelSensorMaterialPair> materialPairs = new();

        private Dictionary<PixelSensorFrameType, Material> materialTable;

        public Material GetMaterialForFrameType(PixelSensorFrameType frameType)
        {
            materialTable ??= materialPairs.ToDictionary(mp => mp.frameType, mp => mp.frameTypeMaterial);
            return materialTable.GetValueOrDefault(frameType, defaultMaterial);
        }

        [Serializable]
        public struct PixelSensorMaterialPair
        {
            [SerializeField] public PixelSensorFrameType frameType;
            [SerializeField] public Material frameTypeMaterial;
        }
    }
}

*/