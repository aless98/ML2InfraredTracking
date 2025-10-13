using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MagicLeap.OpenXR.Features.PixelSensors;


public static class Utils
{
    // -------------------------
    // Rendering / preview utils
    // -------------------------

    // Draw crosses + circle outlines on Raw Depth image
    public static void DrawBlobsMarkers(Texture2D texture, List<Vector2> centers, int crossSize = 3, int circleRadius = 6, Color? color = null)
    {
        if (texture == null || centers == null || centers.Count == 0) return;

        Color drawColor = color ?? Color.red;
        int width = texture.width;
        int height = texture.height;

        foreach (Vector2 center in centers)
        {
            int cx = Mathf.RoundToInt(center.x);
            int cy = Mathf.RoundToInt(center.y);

            // Cross
            for (int dx = -crossSize; dx <= crossSize; dx++)
            {
                int x = cx + dx;
                if ((uint)x < (uint)width && (uint)cy < (uint)height)
                    texture.SetPixel(x, cy, drawColor);
            }
            for (int dy = -crossSize; dy <= crossSize; dy++)
            {
                int y = cy + dy;
                if ((uint)cx < (uint)width && (uint)y < (uint)height)
                    texture.SetPixel(cx, y, drawColor);
            }

            // Circle outline
            int r2 = circleRadius * circleRadius;
            for (int y = -circleRadius; y <= circleRadius; y++)
            {
                for (int x = -circleRadius; x <= circleRadius; x++)
                {
                    int px = cx + x, py = cy + y;
                    if ((uint)px < (uint)width && (uint)py < (uint)height)
                    {
                        int d2 = x * x + y * y;
                        if (Mathf.Abs(d2 - r2) <= circleRadius)
                            texture.SetPixel(px, py, drawColor);
                    }
                }
            }
        }

        texture.Apply(false, false);
    }


    // Creates(or reinitializes) a GPU Texture2D with the right size and format for the incoming depth frame.It guarantees you always have a valid render target without garbage from previous frames.
    public static void EnsureTargetTexture(ref Texture2D targetTexture, PixelSensorFrameType frameType, int w, int h)
    {
        TextureFormat fmt = TextureFormat.RFloat;
        if (targetTexture == null)
        {
            targetTexture = new Texture2D(w, h, fmt, false) { filterMode = FilterMode.Bilinear };
        }
        else if (targetTexture.width != w || targetTexture.height != h || targetTexture.format != fmt)
        {
            targetTexture.Reinitialize(w, h, fmt, false);
            targetTexture.filterMode = FilterMode.Bilinear;
        }
    }

    //Uploads the raw depth bytes from the current PixelSensorPlane into the GPU texture(zero-copy path via LoadRawTextureData).
    //It also performs a quick size sanity check to catch format/stride mismatches.After Apply(), the texture is ready for rendering.
    public static void UploadMainTexture(PixelSensorFrameType frameType, ref PixelSensorPlane plane, Texture2D targetTexture)
    {
        if (targetTexture == null) return;

        
        int w = (int)plane.Width;
        int h = (int)plane.Height;
        int expectedSize = targetTexture.format switch
        {
            TextureFormat.R16 => w * h * 2,
            TextureFormat.RFloat => w * h * 4,
            _ => plane.ByteData.Length 
        };
        if (expectedSize != plane.ByteData.Length)
        {
            Debug.LogWarning($"[ML2Tracking] ByteData size mismatch. expected={expectedSize} got={plane.ByteData.Length} fmt={targetTexture.format}");
        }
        targetTexture.LoadRawTextureData(plane.ByteData);
        targetTexture.Apply(false, false);
    }

    // Converts a 16-float column-major array (from the plugin) into a Unity Matrix4x4.
    public static Matrix4x4 MatrixFromColumnMajor(float[] c)
    {
        
        Matrix4x4 m = new Matrix4x4();
        m.m00 = c[0]; m.m10 = c[1]; m.m20 = c[2]; m.m30 = c[3];
        m.m01 = c[4]; m.m11 = c[5]; m.m21 = c[6]; m.m31 = c[7];
        m.m02 = c[8]; m.m12 = c[9]; m.m22 = c[10]; m.m32 = c[11];
        m.m03 = c[12]; m.m13 = c[13]; m.m23 = c[14]; m.m33 = c[15];
        return m;
    }

}

// Material table type 
[Serializable]
public class PixelSensorMaterialTable
{
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private List<PixelSensorMaterialPair> materialPairs = new();

    private Dictionary<PixelSensorFrameType, Material> _table;

    public Material GetMaterialForFrameType(PixelSensorFrameType frameType)
    {
        _table ??= materialPairs.ToDictionary(mp => mp.frameType, mp => mp.frameTypeMaterial);
        return _table != null && _table.TryGetValue(frameType, out var m) ? m : defaultMaterial;
    }

    [Serializable]
    public struct PixelSensorMaterialPair
    {
        public PixelSensorFrameType frameType;
        public Material frameTypeMaterial;
    }
}
