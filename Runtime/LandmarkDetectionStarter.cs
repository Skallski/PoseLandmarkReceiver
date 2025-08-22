using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoseLandmarkReceiver
{
    public class LandmarkDetectionStarter : MonoBehaviour
    {
        private const string PROCESS_NAME = "PoseLandmarkSender";
        
        public static event Action OnLandmarkDetectionStarted;
        public static event Action OnLandmarkDetectionStopped;
        
        private string _fileName;
        private Process _process;

        private void Start() => StartProcess();

        private void OnApplicationQuit() => StopProcess();

        private void OnDisable() => StopProcess();

        private void StartProcess()
        {
            _fileName = $"{PROCESS_NAME}.exe";
            
            // Try attach to existing process
            Process existing = Process.GetProcessesByName(PROCESS_NAME)
                .FirstOrDefault(p => p.HasExited == false);
            
            if (existing != null)
            {
                _process = existing;
                
                PoseLandmarkLogger.Log($"{_fileName} already running – attached.");
                OnLandmarkDetectionStarted?.Invoke();
                return;
            }
            
            // If process does no exist, run new instance
            string exePath = Path.Combine(Application.streamingAssetsPath, "PoseLandmarkSender", _fileName);
            if (File.Exists(exePath) == false)
            {
                PoseLandmarkLogger.LogError($"File: {_fileName} not found!");
                return;
            }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(exePath)
                {
                    WorkingDirectory = Application.streamingAssetsPath,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                _process = Process.Start(psi);
                PoseLandmarkLogger.Log($"{_fileName} started successfully");

                OnLandmarkDetectionStarted?.Invoke();
            }
            catch (Exception e)
            {
                PoseLandmarkLogger.LogError($"Failed to start {_fileName}: {e.Message}");
            }
        }

        private void StopProcess()
        {
            try
            {
                if (_process is { HasExited: false })
                {
                    OnLandmarkDetectionStopped?.Invoke();

                    _process.Kill();
                    _process.WaitForExit(2000);
                    
                    PoseLandmarkLogger.Log($"{_fileName} closed successfully");
                }
            }
            catch
            {
                // ignore error while closing
            }
            finally
            {
                _process?.Dispose();
                _process = null;
            }
        }
    }
}