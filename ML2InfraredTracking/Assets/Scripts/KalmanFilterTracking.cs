/*
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.VideoModule;
using UnityEngine;

public class kalmanFilterTracking 
{
    private KalmanFilter kf;
    private Mat measurement;   // 3x1
    private bool initialized;

    // Varianze (default identici ai tuoi)
    private readonly float measurementNoise;
    private readonly float positionNoise;
    private readonly float velocityNoise;

    // --- Costruttori ---
    public kalmanFilterTracking(
        float measurementNoise = 1,
        float positionNoise = 1e-4f,
        float velocityNoise = 3f)
    {
        this.measurementNoise = measurementNoise;
        this.positionNoise = positionNoise;
        this.velocityNoise = velocityNoise;

        // Alloc e configurazione fissa (H, Q, R). Qui i costruttori funzionano: NON è un MonoBehaviour.
        kf = new KalmanFilter(6, 3, 0, CvType.CV_32F);
        measurement = new Mat(3, 1, CvType.CV_32F);

        // H: misura solo posizione
        Mat H = Mat.zeros(3, 6, CvType.CV_32F);
        H.put(0, 0, 1f); H.put(1, 1, 1f); H.put(2, 2, 1f);
        kf.set_measurementMatrix(H);

        // Q: diagonale fissa (posNoise su pos, velNoise su vel)
        Mat Q = Mat.zeros(6, 6, CvType.CV_32F);
        Q.put(0, 0, positionNoise);
        Q.put(1, 1, positionNoise);
        Q.put(2, 2, positionNoise);
        Q.put(3, 3, velocityNoise);
        Q.put(4, 4, velocityNoise);
        Q.put(5, 5, velocityNoise);
        kf.set_processNoiseCov(Q);

        // R = measurementNoise * I
        Mat R = Mat.eye(3, 3, CvType.CV_32F);
        R.put(0, 0, measurementNoise);
        R.put(1, 1, measurementNoise);
        R.put(2, 2, measurementNoise);
        kf.set_measurementNoiseCov(R);
    }

    /// <summary>Imposta transizione A (dt=1) e stato iniziale [pos, vel=0].</summary>
    public void Initialize(Vector3 value)
    {
        // A: dt=1 implicito (x+=vx, ecc.)
        Mat A = Mat.eye(6, 6, CvType.CV_32F);
        A.put(0, 3, 1f);
        A.put(1, 4, 1f);
        A.put(2, 5, 1f);
        kf.set_transitionMatrix(A);

        // Stato iniziale
        Mat x = new Mat(6, 1, CvType.CV_32F);
        x.put(0, 0, value.x);
        x.put(1, 0, value.y);
        x.put(2, 0, value.z);
        x.put(3, 0, 0f);
        x.put(4, 0, 0f);
        x.put(5, 0, 0f);
        kf.set_statePre(x);
        kf.set_statePost(x.clone());   // robusto

        // P iniziale
        Mat P = Mat.eye(6, 6, CvType.CV_32F);
        kf.set_errorCovPost(P);

        initialized = true;
    }

    /// <summary>Predict() + Correct() con misura posizione 3D. Ritorna posa filtrata.</summary>
    public Vector3 Filter(Vector3 measuredPosition)
    {
        if (!initialized) Initialize(measuredPosition);

        kf.predict();

        measurement.put(0, 0, measuredPosition.x);
        measurement.put(1, 0, measuredPosition.y);
        measurement.put(2, 0, measuredPosition.z);

        Mat corrected = kf.correct(measurement);

        return new Vector3(
            (float)corrected.get(0, 0)[0],
            (float)corrected.get(1, 0)[0],
            (float)corrected.get(2, 0)[0]
        );
    }

    /// <summary>Solo predict(). Utile quando non hai misura.</summary>
    public Vector3 Predict()
    {
        if (!initialized) return Vector3.zero;

        Mat pred = kf.predict();
        return new Vector3(
            (float)pred.get(0, 0)[0],
            (float)pred.get(1, 0)[0],
            (float)pred.get(2, 0)[0]
        );
    }

    public bool IsInitialized => initialized;

    public void Reset()
    {
        Dispose();
        // ricrea con gli stessi parametri
        kf = new KalmanFilter(6, 3, 0, CvType.CV_32F);
        measurement = new Mat(3, 1, CvType.CV_32F);

        // reimposta H, Q, R come nel costruttore
        Mat H = Mat.zeros(3, 6, CvType.CV_32F);
        H.put(0, 0, 1f); H.put(1, 1, 1f); H.put(2, 2, 1f);
        kf.set_measurementMatrix(H);

        Mat Q = Mat.zeros(6, 6, CvType.CV_32F);
        Q.put(0, 0, positionNoise);
        Q.put(1, 1, positionNoise);
        Q.put(2, 2, positionNoise);
        Q.put(3, 3, velocityNoise);
        Q.put(4, 4, velocityNoise);
        Q.put(5, 5, velocityNoise);
        kf.set_processNoiseCov(Q);

        Mat R = Mat.eye(3, 3, CvType.CV_32F);
        R.put(0, 0, measurementNoise);
        R.put(1, 1, measurementNoise);
        R.put(2, 2, measurementNoise);
        kf.set_measurementNoiseCov(R);

        initialized = false;
    }

    public void Dispose()
    {
        measurement?.Dispose();
        kf?.Dispose();
    }
}
*/