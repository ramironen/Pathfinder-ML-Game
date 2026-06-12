using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Loads exported PyTorch weights (ML/export_for_unity.py) and runs inference in C#.
/// Architecture and sizes come from model.json (must match ML/model.py).
/// </summary>
public class PathfinderMlPredictor
{
    [Serializable]
    class ModelJson
    {
        public int input_size;
        public int hidden1;
        public int hidden2;
        public float[] scaler_mean;
        public float[] scaler_scale;
        public float[] w1;
        public float[] b1;
        public float[] w2;
        public float[] b2;
        public float[] w3;
        public float[] b3;
    }

    int _inputSize;
    int _hidden1;
    int _hidden2;
    float[] _mean;
    float[] _scale;
    float[] _w1;
    float[] _b1;
    float[] _w2;
    float[] _b2;
    float[] _w3;
    float[] _b3;
    bool _loaded;

    public bool IsLoaded => _loaded;
    public int InputSize => _inputSize;

    public bool TryLoad()
    {
        _loaded = false;
        string path = Path.Combine(Application.streamingAssetsPath, "ML", "model.json");
        if (!File.Exists(path))
        {
            Debug.LogWarning("Pathfinder ML: model.json not found at " + path
                + ". Run: python ML/train.py && python ML/export_for_unity.py");
            return false;
        }

        try
        {
            string json = File.ReadAllText(path);
            ModelJson data = JsonUtility.FromJson<ModelJson>(json);
            if (data == null || data.scaler_mean == null || data.w1 == null)
            {
                Debug.LogWarning("Pathfinder ML: invalid model.json");
                return false;
            }

            if (data.input_size <= 0 || data.hidden1 <= 0 || data.hidden2 <= 0)
            {
                Debug.LogWarning("Pathfinder ML: model.json missing input_size / hidden1 / hidden2");
                return false;
            }

            if (!ValidateShapes(data))
                return false;

            _inputSize = data.input_size;
            _hidden1 = data.hidden1;
            _hidden2 = data.hidden2;
            _mean = data.scaler_mean;
            _scale = data.scaler_scale;
            _w1 = data.w1;
            _b1 = data.b1;
            _w2 = data.w2;
            _b2 = data.b2;
            _w3 = data.w3;
            _b3 = data.b3;
            _loaded = true;
            Debug.Log("Pathfinder ML: loaded " + _inputSize + "→" + _hidden1 + "→" + _hidden2 + "→1 from " + path);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Pathfinder ML: failed to load model — " + ex.Message);
            return false;
        }
    }

    static bool ValidateShapes(ModelJson data)
    {
        int inSz = data.input_size;
        int h1 = data.hidden1;
        int h2 = data.hidden2;

        if (data.scaler_mean.Length != inSz || data.scaler_scale.Length != inSz)
        {
            Debug.LogWarning("Pathfinder ML: scaler length mismatch (expected " + inSz + ")");
            return false;
        }
        if (data.w1.Length != h1 * inSz || data.b1.Length != h1)
        {
            Debug.LogWarning("Pathfinder ML: layer 1 weight/bias size mismatch");
            return false;
        }
        if (data.w2.Length != h2 * h1 || data.b2.Length != h2)
        {
            Debug.LogWarning("Pathfinder ML: layer 2 weight/bias size mismatch");
            return false;
        }
        if (data.w3.Length != h2 || data.b3.Length != 1)
        {
            Debug.LogWarning("Pathfinder ML: output layer weight/bias size mismatch");
            return false;
        }
        return true;
    }

    public float Predict(float[] features)
    {
        if (!_loaded || features == null || features.Length != _inputSize)
            return -1f;

        float[] x = new float[_inputSize];
        for (int i = 0; i < _inputSize; i++)
        {
            float s = _scale[i] != 0f ? _scale[i] : 1f;
            x[i] = (features[i] - _mean[i]) / s;
        }

        float[] h1 = new float[_hidden1];
        for (int o = 0; o < _hidden1; o++)
        {
            float sum = _b1[o];
            for (int i = 0; i < _inputSize; i++)
                sum += x[i] * _w1[o * _inputSize + i];
            h1[o] = Mathf.Max(0f, sum);
        }

        float[] h2 = new float[_hidden2];
        for (int o = 0; o < _hidden2; o++)
        {
            float sum = _b2[o];
            for (int i = 0; i < _hidden1; i++)
                sum += h1[i] * _w2[o * _hidden1 + i];
            h2[o] = Mathf.Max(0f, sum);
        }

        float outSum = _b3[0];
        for (int i = 0; i < _hidden2; i++)
            outSum += h2[i] * _w3[i];

        return Sigmoid(outSum);
    }

    static float Sigmoid(float z)
    {
        return 1f / (1f + Mathf.Exp(-z));
    }
}
