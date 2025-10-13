using System;
using System.Runtime.InteropServices;
using UnityEngine;

public static class ML2IRTRackingPluginImports

{
    private const string LIB = "ml2irtrackingplugin"; 

    
    [StructLayout(LayoutKind.Sequential)]
    public struct PoseResult_CS
    {
        public byte hasPose; 

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public float[] worldTObject;

        public static PoseResult_CS Create()
            => new PoseResult_CS
            {
                hasPose = 0,
                worldTObject = new float[16],
                
            };
    }

    // --------- Exports ----------
    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ml2_log_loaded();

    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CreatePoseEstimatorNative();

    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern void DestroyPoseEstimatorNative(IntPtr estimator);

    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CreateKalmanFilterNative(float measNoise, float posNoise, float velNoise);

    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern void DestroyKalmanFilterNative(IntPtr kf);

    [DllImport(LIB, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte RunTrackingAndEstimatePose(
        IntPtr estimator,
        IntPtr kalman,
        [In] float[] depth, int width, int height,        // width*height
        [In] float[] objectPoints, int objectPointsCount, // N*3
        [In] float[] camera3x3,                           // 9 (row-major)
        [In] float[] dist5,                               // 5: k1,k2,p1,p2,k3
        [In, Out] float[] io_rvec,                        // 3
        [In, Out] float[] io_tvec,                        // 3
        ref byte ioKalmanInitialized,                     // 0/1 (in/out)
        ref PoseResult_CS outResult,
        [Out] float[] outBlobXY,
        ref int outBlobCount
    );

}
