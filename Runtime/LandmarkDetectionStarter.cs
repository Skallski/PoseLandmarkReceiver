using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoseLandmarkReceiver
{
    public class LandmarkDetectionStarter : MonoBehaviour
    {
        private const string FILE_NAME = "PoseLandmarkSender.exe";

        private Process _process;

        public static event Action OnLandmarkDetectionStarted;
        public static event Action OnLandmarkDetectionStopped;

        private void Start()
        {
            string exePath = Path.Combine(Application.streamingAssetsPath, FILE_NAME);
            if (File.Exists(exePath) == false)
            {
                Debug.LogError($"file: {FILE_NAME} not found!");
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
                Debug.Log($"{FILE_NAME} started successfully");

                OnLandmarkDetectionStarted?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to start {FILE_NAME}: {e.Message}");
            }
        }

        private void OnApplicationQuit()
        {
            StopProcess();
        }

        private void OnDisable()
        {
            StopProcess();
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