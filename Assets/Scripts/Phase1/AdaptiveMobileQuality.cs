using UnityEngine;
using UnityEngine.Rendering;

namespace WarehouseSimulator.Phase1
{
    /// <summary>
    /// Conservative frame pacing and slow resolution adaptation (hysteresis).
    /// Temperature is NOT assumed or inferred from FPS; real-device thermal
    /// measurements are a separate production validation step.
    /// </summary>
    public sealed class AdaptiveMobileQuality : MonoBehaviour
    {
        public float CurrentScale { get; private set; } = 1f;
        private float _sampleSeconds;
        private int _sampleFrames;
        private int _slowWindows;
        private int _fastWindows;

        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 30;
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.antiAliasing = 0;
            CurrentScale = Application.isMobilePlatform ? .85f : 1f;
            SetScale(CurrentScale);
        }

        private void Update()
        {
            _sampleSeconds += Time.unscaledDeltaTime;
            _sampleFrames++;
            if (_sampleSeconds < 5f) return;
            float fps = _sampleFrames / Mathf.Max(.001f, _sampleSeconds);
            _sampleSeconds = 0f;
            _sampleFrames = 0;

            if (fps < 25f) { _slowWindows++; _fastWindows = 0; }
            else if (fps > 29f) { _fastWindows++; _slowWindows = 0; }
            else { _slowWindows = 0; _fastWindows = 0; }

            // Avoid rapidly oscillating resolution.
            if (_slowWindows >= 2)
            {
                SetScale(Mathf.Max(.65f, CurrentScale - .08f));
                _slowWindows = 0;
            }
            else if (_fastWindows >= 6)
            {
                SetScale(Mathf.Min(Application.isMobilePlatform ? .9f : 1f, CurrentScale + .05f));
                _fastWindows = 0;
            }
        }

        private void SetScale(float scale)
        {
            CurrentScale = scale;
            // Dynamic resolution requires an appropriately configured render pipeline.
            // ScalableBufferManager may be ignored by some platforms / URP configurations.
            ScalableBufferManager.ResizeBuffers(scale, scale);
        }
    }
}
