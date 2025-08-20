using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace PoseLandmarkSender.Editor
{
    public class Installer : EditorWindow
    {
        private const string Owner   = "Skallski";
        private const string Repo    = "PoseLandmarkSender";
        private const string Tag     = "v1.2";
        private const string ExeName = "PoseLandmarkSender.exe";
        private const string CfgName = "config.json";

        private static readonly HttpClient Http = CreateHttp();
        
        private static HttpClient CreateHttp()
        {
            HttpClient h = new HttpClient(new HttpClientHandler {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });
            h.DefaultRequestHeaders.UserAgent.ParseAdd("UnityEditor/PoseLandmarkSenderInstaller");
            h.Timeout = TimeSpan.FromMinutes(5);
            return h;
        }

        private static string Url(string asset) => $"https://github.com/{Owner}/{Repo}/releases/download/{Tag}/{asset}";

        private CancellationTokenSource _cts;
        private bool IsRunning => _cts != null;

        [MenuItem("Tools/Pose Landmark Sender/Install to StreamingAssets")]
        private static void Open()
        {
            GetWindow<Installer>("Pose Sender Installer");
        }

        private void OnGUI()
        {
            string sa = Path.Combine(Application.dataPath, "StreamingAssets");
            bool installed = File.Exists(Path.Combine(sa, ExeName)) && File.Exists(Path.Combine(sa, CfgName));

            using (new EditorGUI.DisabledScope(installed || IsRunning))
            {
                if (GUILayout.Button($"Install {Tag} → StreamingAssets", GUILayout.Height(36)))
                {
                    RunBackgroundInstall();
                }
            }

            GUILayout.Space(6);

            if (IsRunning)
            {
                EditorGUILayout.HelpBox("Installing...", MessageType.Info);
                if (GUILayout.Button("Cancel", GUILayout.Height(24)))
                {
                    _cts?.Cancel();
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    installed ? "Files already present."
                              : $"Downloads {ExeName} & {CfgName} in the background to Assets/StreamingAssets.",
                    installed ? MessageType.Info : MessageType.Warning);
            }
        }

        private void RunBackgroundInstall()
        {
            if (IsRunning)
            {
                return;
            }

            _cts = new CancellationTokenSource();

            int progressId = Progress.Start("Pose Landmark Sender", "Downloading…", Progress.Options.Indefinite | Progress.Options.Sticky);
            Progress.RegisterCancelCallback(progressId, () => { _cts.Cancel(); return true; });

            string sa = Path.Combine(Application.dataPath, "StreamingAssets");
            Directory.CreateDirectory(sa);

            Task.Run(async () =>
            {
                try
                {
                    await DownloadToFileAsync(Url(ExeName), Path.Combine(sa, ExeName), _cts.Token, progressId, "Downloading exe…");
                    await DownloadToFileAsync(Url(CfgName), Path.Combine(sa, CfgName), _cts.Token, progressId, "Downloading config…");

                    EditorApplication.delayCall += () =>
                    {
                        if (_cts.IsCancellationRequested)
                        {
                            Finish(progressId, Progress.Status.Canceled);
                            return;
                        }
                        
                        AssetDatabase.Refresh();
                        Finish(progressId, Progress.Status.Succeeded);
                        EditorUtility.DisplayDialog("Pose Landmark Sender", "Installed to Assets/StreamingAssets.", "OK");
                    };
                }
                catch (OperationCanceledException)
                {
                    EditorApplication.delayCall += () => Finish(progressId, Progress.Status.Canceled);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    EditorApplication.delayCall += () =>
                    {
                        Finish(progressId, Progress.Status.Failed);
                        EditorUtility.DisplayDialog("Pose Landmark Sender – error", e.Message, "OK");
                    };
                }
            });
        }

        private void Finish(int progressId, Progress.Status status)
        {
            Progress.Finish(progressId, status);
            _cts?.Dispose(); _cts = null;
            Repaint();
        }

        static async Task DownloadToFileAsync(string url, string dest, CancellationToken ct, int progressId, string label)
        {
            using (HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, url))
            using (HttpResponseMessage resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct))
            {
                resp.EnsureSuccessStatusCode();
                Directory.CreateDirectory(Path.GetDirectoryName(dest));

                using (Stream httpStream = await resp.Content.ReadAsStreamAsync())
                using (FileStream fileStream = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20, useAsync: true))
                {
                    byte[] buffer = new byte[1 << 20];
                    int read;
                    while ((read = await httpStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, read, ct);
                    }
                }
            }

            Progress.Report(progressId, -1f, label);
        }
    }
}