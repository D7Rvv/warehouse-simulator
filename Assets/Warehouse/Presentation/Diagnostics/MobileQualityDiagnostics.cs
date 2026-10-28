using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;

namespace Warehouse.Presentation
{
    public readonly struct MobileDiagnosticSnapshot
    {
        public readonly int SampleCount, CameraSwitchCount, ActiveRenderers;
        public readonly double MeanFrameMs, P95FrameMs, P99FrameMs, LastSwitchMs, MaxSwitchMs;
        public readonly long AllocatedMemoryBytes;
        public MobileDiagnosticSnapshot(int sampleCount, double mean, double p95, double p99,
            int switches, double last, double max, int renderers, long memory)
        {
            SampleCount = sampleCount; MeanFrameMs = mean; P95FrameMs = p95; P99FrameMs = p99;
            CameraSwitchCount = switches; LastSwitchMs = last; MaxSwitchMs = max;
            ActiveRenderers = renderers; AllocatedMemoryBytes = memory;
        }
    }

    /// <summary>
    /// Bounded real-frame telemetry, never a claim of mobile FPS or thermal acceptance.
    /// Samples CPU-visible frame intervals; GPU timing and temperature need device tools.
    /// </summary>
    public sealed class MobileQualityDiagnostics : MonoBehaviour
    {
        const int Capacity = 1800;
        readonly double[] frames = new double[Capacity];
        readonly double[] sorted = new double[Capacity];
        int next, count, switches;
        double lastSwitch, maxSwitch;
        Func<int> rendererCounter;
        float nextOverlayUpdate;
        string overlay;
        public bool DebugVisible { get; set; }

        public void Initialize(Func<int> countActiveRenderers)
        {
            rendererCounter = countActiveRenderers;
        }

        void Update()
        {
            RecordFrame(Time.unscaledDeltaTime * 1000.0);
            if (DebugVisible && Time.unscaledTime >= nextOverlayUpdate)
            {
                var sample = Snapshot();
                overlay = string.Format(CultureInfo.InvariantCulture,
                    "DIAGNOSTICS · NOT DEVICE ACCEPTANCE\nFrame mean {0:0.0}ms  p95 {1:0.0}  p99 {2:0.0}\nSwitches {3}  last {4:0.00}ms  max {5:0.00}ms\nRenderers {6}  allocated {7:0.0}MiB",
                    sample.MeanFrameMs, sample.P95FrameMs, sample.P99FrameMs,
                    sample.CameraSwitchCount, sample.LastSwitchMs, sample.MaxSwitchMs,
                    sample.ActiveRenderers, sample.AllocatedMemoryBytes / (1024.0 * 1024));
                nextOverlayUpdate = Time.unscaledTime + 0.5f;
            }
        }

        public void RecordFrame(double milliseconds)
        {
            if (milliseconds <= 0 || double.IsNaN(milliseconds) || double.IsInfinity(milliseconds)) return;
            frames[next] = milliseconds;
            next = (next + 1) % Capacity;
            if (count < Capacity) count++;
        }

        public void RecordStationSwitch(double elapsedMs)
        {
            if (elapsedMs < 0 || double.IsNaN(elapsedMs) || double.IsInfinity(elapsedMs)) return;
            switches++;
            lastSwitch = elapsedMs;
            maxSwitch = Math.Max(maxSwitch, elapsedMs);
        }

        public MobileDiagnosticSnapshot Snapshot()
        {
            double sum = 0;
            for (int i = 0; i < count; i++) { sorted[i] = frames[i]; sum += frames[i]; }
            Array.Sort(sorted, 0, count);
            double p95 = count == 0 ? 0 : sorted[Math.Max(0, (int)Math.Ceiling(count * 0.95) - 1)];
            double p99 = count == 0 ? 0 : sorted[Math.Max(0, (int)Math.Ceiling(count * 0.99) - 1)];
            return new MobileDiagnosticSnapshot(count, count == 0 ? 0 : sum / count, p95, p99,
                switches, lastSwitch, maxSwitch, rendererCounter == null ? 0 : rendererCounter(),
                Profiler.GetTotalAllocatedMemoryLong());
        }

        public string WriteCsv()
        {
            var sample = Snapshot();
            string directory = Path.Combine(Application.persistentDataPath, "diagnostics");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "station-sample-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff", CultureInfo.InvariantCulture) + ".csv");
            using (var file = new StreamWriter(path, false))
            {
                file.WriteLine("scope,device,platform,unity,sample_count,mean_frame_ms,p95_frame_ms,p99_frame_ms,switch_count,last_switch_ms,max_switch_ms,active_renderers,allocated_bytes,thermal_acceptance");
                file.WriteLine(string.Join(",", "runtime_diagnostic_not_acceptance", Csv(SystemInfo.deviceModel),
                    Application.platform.ToString(), Application.unityVersion,
                    sample.SampleCount.ToString(CultureInfo.InvariantCulture),
                    sample.MeanFrameMs.ToString("F4", CultureInfo.InvariantCulture),
                    sample.P95FrameMs.ToString("F4", CultureInfo.InvariantCulture),
                    sample.P99FrameMs.ToString("F4", CultureInfo.InvariantCulture),
                    sample.CameraSwitchCount.ToString(CultureInfo.InvariantCulture),
                    sample.LastSwitchMs.ToString("F4", CultureInfo.InvariantCulture),
                    sample.MaxSwitchMs.ToString("F4", CultureInfo.InvariantCulture),
                    sample.ActiveRenderers.ToString(CultureInfo.InvariantCulture),
                    sample.AllocatedMemoryBytes.ToString(CultureInfo.InvariantCulture), "NOT_RUN"));
            }
            Debug.Log("Station diagnostic sample saved: " + path + ". GPU/thermal acceptance NOT_RUN.");
            return path;
        }

        static string Csv(string value) { return "\"" + (value ?? "").Replace("\"", "\"\"") + "\""; }

        void OnGUI()
        {
            if (!DebugVisible || string.IsNullOrEmpty(overlay)) return;
            Rect safe = Screen.safeArea;
            GUI.Box(new Rect(safe.x + 12, Screen.height - safe.yMax + 90, Mathf.Min(510, safe.width - 24), 116), overlay);
        }

        void OnApplicationPause(bool paused)
        {
            // A resumed frame includes wall-clock background time, not rendering work.
            if (paused) { next = 0; count = 0; }
        }
    }
}
