using UnityEngine;
using System;
using System.Collections.Generic;
using MagicLeap.OpenXR.Features.PixelSensors;

public class ML2ToolTrackingManager : MonoBehaviour
{
    [Header("Tracking Data")]
    public List<GameObject> Markers;         // Scene markers whose local positions form the 3D model points
    public GameObject TrackedTool;           // Scene object to drive with the estimated pose
    public Renderer targetRenderer;          // Renderer that previews the depth texture
    public float maxNoTrackingDuration = 2.0f; // Seconds before forcing Kalman re-init when tracking is lost

    [SerializeField] private PixelSensorMaterialTable materialList = new();
    Texture2D targetTexture, filteredTexture;

    // --- Native handles/state ---
    private IntPtr _poseEstimatorPtr = IntPtr.Zero;
    private IntPtr _kalmanFilterPtr = IntPtr.Zero;

    private float[] _previousRvecNative = new float[3];  
    private float[] _previousTvecNative = new float[3];  
    private float[] _cameraMatrixNative = new float[9];  // 3x3 fx,fy,cx,cy
    private float[] _distCoeffsNative;                   // k1,k2,p1,p2,k3
    private byte _kfInitialized = 0;                     // state that shows if the kalman filter is initailized or not

    // --- Depth buffers ---
    private float[] _floatBuffer;  

    // --- Model points (3D) & blobs (2D) ---
    private readonly List<Vector3> _objectPoints = new();
    private List<Vector2> blobCenters = new List<Vector2>();
    private float[] _objectPointsFlat;
    private float[] blobXY = new float[40];  // 20 blobs max (x,y pairs). This is just an initialization
    private int blobCount = 0;

    // --- Rendering helpers ---
    private MaterialPropertyBlock _mpb;
    private float minDepth = 0, maxDepth = 5;

    // --- Final pose in world space (drives TrackedTool) ---
    public Vector3 finalPosition { get; private set; }
    public Quaternion finalRotation { get; private set; }
    private Quaternion previousFilteredRot = Quaternion.identity;
    private float lastTrackingTime;

    // ----------------- Helpers -----------------

    // Converts Magic Leap raw plane into a float[] depth map in meters.
    private float[] GetRawDepthData(in PixelSensorFrame frame, ref float[] buffer)
    {
        if (frame.Planes.Length == 0) return null;
        var firstPlane = frame.Planes[0];

        int size = (int)(firstPlane.Width * firstPlane.Height);
        if (size <= 0)
        {
            Debug.LogWarning("[ML2Tracking] Invalid frame size.");
            return null;
        }

        var asFloat = firstPlane.ByteData.Reinterpret<float>(sizeof(float));
        if (asFloat.Length == size)
        {
            if (buffer == null || buffer.Length != size) buffer = new float[size];
            asFloat.CopyTo(buffer);
            Debug.LogWarning("[ML2Tracking] Depth interpreted as FLOAT32.");
            return buffer;
        }

       
        return null;
    }

    private static float[] FlattenObjectPoints(List<Vector3> pts)
    {
        if (pts == null || pts.Count == 0) return Array.Empty<float>();
        var flat = new float[pts.Count * 3];
        int k = 0;
        for (int i = 0; i < pts.Count; i++)
        {
            flat[k++] = pts[i].x;
            flat[k++] = pts[i].y;
            flat[k++] = pts[i].z;
        }
        return flat;
    }


    private void Start()
    {
        Debug.Log("[ML2Tracking] Start() - initializing preview, native handles and calibration.");

        _mpb = new MaterialPropertyBlock();

        // Assign the material 
        var mat = materialList.GetMaterialForFrameType(PixelSensorFrameType.DepthRaw);
        if (targetRenderer) targetRenderer.sharedMaterial = mat;

        // Create PoseEstimator and KalmanFilter (measurementNoise = 1.0f, float positionNoise = 1e-4f, float velocityNoise = 3.0f) this can be changed
        _poseEstimatorPtr = ML2IRTRackingPluginImports.CreatePoseEstimatorNative();
        _kalmanFilterPtr = ML2IRTRackingPluginImports.CreateKalmanFilterNative(1.0f, 1e-4f, 3.0f);
        Debug.Log($"[ML2Tracking] Native handles -> PoseEstimator=0x{_poseEstimatorPtr.ToInt64():X}, Kalman=0x{_kalmanFilterPtr.ToInt64():X}");

        if (_poseEstimatorPtr == IntPtr.Zero || _kalmanFilterPtr == IntPtr.Zero)
        {
            Debug.LogError("[ML2Tracking] Failed to initialize native plugin objects. Disabling component.");
            enabled = false;
            return;
        }

        // Gather 3D model points from markers
        _objectPoints.Clear();
        if (Markers != null && Markers.Count > 0)
        {
            foreach (var go in Markers) if (go) _objectPoints.Add(go.transform.localPosition);
            Debug.Log($"[ML2Tracking] Loaded {_objectPoints.Count} object points from scene markers.");
        }
        else
        {
            Debug.LogWarning("[ML2Tracking] No markers assigned.");
        }

        // Depth sensor Intrinsics.
        _cameraMatrixNative[0] = 365.3747f; _cameraMatrixNative[1] = 0.0f; _cameraMatrixNative[2] = 262.2158f;
        _cameraMatrixNative[3] = 0.0f; _cameraMatrixNative[4] = 364.9302f; _cameraMatrixNative[5] = 240.9972f;
        _cameraMatrixNative[6] = 0.0f; _cameraMatrixNative[7] = 0.0f; _cameraMatrixNative[8] = 1.0f;
        _distCoeffsNative = new float[] { -0.1052f, 0.0647f, 0.0f, 0.0f, -0.1254f };

        lastTrackingTime = Time.time;
        finalRotation = previousFilteredRot = Quaternion.identity;

        Debug.Log("[ML2Tracking] Initialization completed.");
    }

  
    public void Initialize(uint streamId, MagicLeapPixelSensorFeature feature, PixelSensorId sensorType)
    {
        if (!sensorType.SensorName.Contains("depth", StringComparison.CurrentCultureIgnoreCase)) return;

        if (feature.QueryPixelSensorCapability(sensorType, PixelSensorCapabilityType.Depth, streamId, out var range))
        {
            if (range.IntRange.HasValue) { minDepth = range.IntRange.Value.Min; maxDepth = range.IntRange.Value.Max; }
            if (range.FloatRange.HasValue) { minDepth = range.FloatRange.Value.Min; maxDepth = range.FloatRange.Value.Max; }
            Debug.Log($"[ML2Tracking] Depth capability range: {minDepth:F3}..{maxDepth:F3} meters.");
        }
        else
        {
            Debug.LogWarning("[ML2Tracking] QueryPixelSensorCapability(Depth) failed.");
        }
    }

    public void Reset()
    {
        if (targetTexture) { Destroy(targetTexture); targetTexture = null; }
        if (filteredTexture) { Destroy(filteredTexture); filteredTexture = null; }
    }

    private void OnDestroy()
    {
        Reset();
        if (targetRenderer && targetRenderer.material) Destroy(targetRenderer.material);

        if (_kalmanFilterPtr != IntPtr.Zero) { ML2IRTRackingPluginImports.DestroyKalmanFilterNative(_kalmanFilterPtr); _kalmanFilterPtr = IntPtr.Zero; }
        if (_poseEstimatorPtr != IntPtr.Zero) { ML2IRTRackingPluginImports.DestroyPoseEstimatorNative(_poseEstimatorPtr); _poseEstimatorPtr = IntPtr.Zero; }

        Debug.Log("[ML2Tracking] Cleanup completed (textures, material, native handles).");
    }

    // ----------------- Main per-frame processing -----------------
    public void ProcessFrame(in PixelSensorFrame frame, in PixelSensorMetaData[] metaData, in Pose sensorPose)
    {
        if (!frame.IsValid || frame.Planes.Length == 0) return;
        if (_poseEstimatorPtr == IntPtr.Zero || _kalmanFilterPtr == IntPtr.Zero) return;
        if (frame.FrameType != PixelSensorFrameType.DepthRaw) return;

        var frameType = frame.FrameType;
        var firstPlane = frame.Planes[0];
        int w = (int)firstPlane.Width;
        int h = (int)firstPlane.Height;

        // Ensure preview texture and upload raw plane 
        Utils.EnsureTargetTexture(ref targetTexture, frameType, w, h);
        Utils.UploadMainTexture(frameType, ref firstPlane, targetTexture);

        switch (frameType)
        {
            case PixelSensorFrameType.DepthRaw:
                {
                    // Build a CPU float[] depth map (meters) for the native pipeline
                    var depthData = GetRawDepthData(in frame, ref _floatBuffer);
                    if (depthData == null) return;

                    if (_objectPoints.Count < 4)
                    {
                        Debug.LogWarning("[ML2Tracking] At least 4 object points are required for PnP.");
                        return;
                    }

                    // Rebuild flattened 3D points if count changed
                    if (_objectPointsFlat == null || _objectPointsFlat.Length != _objectPoints.Count * 3)
                        _objectPointsFlat = FlattenObjectPoints(_objectPoints);

                    // Prepare native out struct and blob buffers
                    var outRes = ML2IRTRackingPluginImports.PoseResult_CS.Create();
                    // blobXY has fixed capacity; native will clamp and write up to capacity
                    blobCount = 0;

                    // Call native: runs thresholding, blob detection, permutations+PnP, LM refine, Kalman (in camera space)
                    byte ok = ML2IRTRackingPluginImports.RunTrackingAndEstimatePose(
                        _poseEstimatorPtr,
                        _kalmanFilterPtr,
                        depthData, w, h,
                        _objectPointsFlat, _objectPoints.Count,
                        _cameraMatrixNative,
                        _distCoeffsNative,
                        _previousRvecNative,
                        _previousTvecNative,
                        ref _kfInitialized,
                        ref outRes,
                        blobXY,
                        ref blobCount
                    );

                    // Rebuild blob list for debug drawing
                    blobCenters = new List<Vector2>(blobCount);
                    for (int i = 0; i < blobCount; i++)
                        blobCenters.Add(new Vector2(blobXY[2 * i], blobXY[2 * i + 1]));

                    Utils.DrawBlobsMarkers(targetTexture, blobCenters, 3, 6, Color.green);
                    targetRenderer.material.mainTexture = targetTexture;

                    if (ok != 0 && outRes.hasPose != 0)
                    {
                        lastTrackingTime = Time.time;

                        // Native returns cam_T_object as column-major float[16]; convert to Unity matrix
                        Matrix4x4 camTobj = Utils.MatrixFromColumnMajor(outRes.worldTObject);
                        Matrix4x4 worldTsensor = Matrix4x4.TRS(sensorPose.position, sensorPose.rotation, Vector3.one);
                        Matrix4x4 worldTobject = worldTsensor * camTobj;

                        // Extract world-space position and rotation (Unity: forward=Z, up=Y from matrix columns)
                        Vector3 pos = worldTobject.GetColumn(3);
                        Quaternion rot = Quaternion.LookRotation(worldTobject.GetColumn(2), worldTobject.GetColumn(1));

                        // Optional smoothing on rotation (position filtered natively by Kalman)
                        finalPosition = pos;
                        finalRotation = Quaternion.Slerp(previousFilteredRot, rot, 0.8f);
                        previousFilteredRot = finalRotation;

                        if (TrackedTool) TrackedTool.transform.SetPositionAndRotation(finalPosition, finalRotation);

                        Debug.Log($"[ML2Tracking] Tracking OK (kf={_kfInitialized}). pos={finalPosition}, rot={finalRotation}, blobs={blobCount}");
                    }
                    else
                    {
                        // Handle tracking loss; if too long without pose, force Kalman reset
                        if ((Time.time - lastTrackingTime) > maxNoTrackingDuration)
                        {
                            if (_kfInitialized != 0) Debug.LogWarning("[ML2Tracking] Tracking lost for too long. Re-initializing Kalman.");
                            _kfInitialized = 0;
                        }
                    }
                    break;
                }
        }
    }

    private void Update()
    {
        // Apply last computed pose (kept in finalPosition/finalRotation) to the tool
        if (TrackedTool) TrackedTool.transform.SetPositionAndRotation(finalPosition, finalRotation);
    }
}
