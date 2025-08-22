using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace PoseLandmarkReceiver
{
    [RequireComponent(typeof(LandmarkDetectionStarter))]
    public class UdpLandmarksReceiver : MonoBehaviour
    {
        [System.Serializable]
        private class UdpConnectionConfig
        {
            [SerializeField] public string udp_ip;
            [SerializeField] public int udp_port;
            [field: SerializeField] public bool IsLoaded { get; set; }
        }

        [System.Serializable]
        private class ReceivedPacket
        {
            public List<Landmark> pts;
            public string frame_b64;

            public override string ToString()
            {
                const int show = 5;
                int landmarksCount = pts?.Count ?? 0;

                StringBuilder sb = new StringBuilder();
                sb.Append("Landmarks: ");

                if (landmarksCount == 0)
                {
                    sb.Append("{ none }");
                }
                else
                {
                    sb.Append("{");

                    for (int i = 0; i < show; i++)
                    {
                        sb.Append($"{i + 1}: [{pts?[i].ToString()}]");
                        sb.Append(i < landmarksCount - 1 ? "," : ", ... and more");
                    }

                    sb.Append("}");
                }

                return sb.ToString();
            }
        }

        public static event Action<FrameData> OnFrameDataReceived;

        [SerializeField] private UdpConnectionConfig _udpCfg;
        [SerializeField] private bool _logReceivedPackets;

        private UdpClient _udpClient;

        private Thread _udpReceiveThread;
        private volatile bool _running;
        private readonly object _lock = new object();

        private readonly Queue<ReceivedPacket> _receivedPacketsQueue = new Queue<ReceivedPacket>();
        private const int MAX_QUEUE = 5;

        private void OnEnable()
        {
            LandmarkDetectionStarter.OnLandmarkDetectionStarted += OnLandmarkDetectionStarted;
            LandmarkDetectionStarter.OnLandmarkDetectionStopped += OnLandmarkDetectionStopped;
        }

        private void OnDisable()
        {
            LandmarkDetectionStarter.OnLandmarkDetectionStarted -= OnLandmarkDetectionStarted;
            LandmarkDetectionStarter.OnLandmarkDetectionStopped -= OnLandmarkDetectionStopped;
        }

        private void Update()
        {
            ReceivedPacket packet = null;
            lock (_lock)
            {
                if (_receivedPacketsQueue.Count > 0)
                {
                    packet = _receivedPacketsQueue.Dequeue();
                }
            }

            if (packet != null)
            {
                if (_logReceivedPackets)
                {
                    PoseLandmarkLogger.Log($"Packet received: {packet}");
                }

                OnFrameDataReceived?.Invoke(new FrameData(packet.pts.ToArray(), packet.frame_b64));
            }
        }

        private async void OnLandmarkDetectionStarted()
        {
            if (_udpCfg == null || _udpCfg.IsLoaded == false)
            {
                await LoadConfig();
            }

            InitUdp();
        }

        private void OnLandmarkDetectionStopped()
        {
            _running = false;
            try
            {
                _udpClient?.Close();
            }
            catch
            {
                /* ignore */
            }

            try
            {
                _udpReceiveThread?.Join(200);
            }
            catch
            {
                /* ignore */
            }

            PoseLandmarkLogger.Log("UDP connection closed successfully");
        }

        private async Task LoadConfig()
        {
            if (_udpCfg is { IsLoaded: true })
            {
                return;
            }

            _udpCfg = new UdpConnectionConfig();

            string path = Path.Combine(Application.streamingAssetsPath, "PoseLandmarkSender", "config.json");
            if (File.Exists(path))
            {
                try
                {
                    string jsonText = await File.ReadAllTextAsync(path);
                    if (string.IsNullOrEmpty(jsonText) == false)
                    {
                        _udpCfg = JsonUtility.FromJson<UdpConnectionConfig>(jsonText);
                        _udpCfg.IsLoaded = true;

                        PoseLandmarkLogger.Log($"Config loaded successfully. ip: {_udpCfg.udp_ip}, port: {_udpCfg.udp_port}>");
                    }
                }
                catch (Exception e)
                {
                    PoseLandmarkLogger.LogError($"Config error occured: {e.Message}");
                }
            }
            else
            {
                PoseLandmarkLogger.LogError("Config file not found!");
            }
        }

        private void InitUdp()
        {
            try
            {
                _udpClient = new UdpClient(_udpCfg.udp_port);
                _udpClient.Client.ReceiveBufferSize = 1 << 20; // 1MB
                _running = true;

                _udpReceiveThread = new Thread(ReceiveLoop)
                {
                    IsBackground = true,
                    Name = "UdpLandmarksReceiver_Thread"
                };
                _udpReceiveThread.Start();

                PoseLandmarkLogger.Log($"UDP connection started successfully. Listening on port: {_udpCfg.udp_port}");
            }
            catch (Exception e)
            {
                PoseLandmarkLogger.LogError($"Failed to start UDP connection: {e.Message}");
            }
        }

        private void ReceiveLoop()
        {
            IPEndPoint any = new IPEndPoint(IPAddress.Any, _udpCfg.udp_port);
            IPAddress filterIp = null;
            if (string.IsNullOrWhiteSpace(_udpCfg.udp_ip) == false &&
                IPAddress.TryParse(_udpCfg.udp_ip, out IPAddress parsed))
            {
                filterIp = parsed;
            }

            while (_running)
            {
                try
                {
                    byte[] data = _udpClient.Receive(ref any);

                    if (filterIp != null && any.Address.Equals(filterIp) == false)
                    {
                        continue;
                    }

                    string json = Encoding.UTF8.GetString(data);
                    ReceivedPacket packet = JsonUtility.FromJson<ReceivedPacket>(json);
                    if (packet != null && (packet.pts != null || !string.IsNullOrEmpty(packet.frame_b64)))
                    {
                        lock (_lock)
                        {
                            while (_receivedPacketsQueue.Count >= MAX_QUEUE)
                            {
                                _receivedPacketsQueue.Dequeue();
                            }

                            _receivedPacketsQueue.Enqueue(packet);
                        }
                    }
                }
                catch (SocketException e)
                {
                    if (_running)
                    {
                        PoseLandmarkLogger.LogError($"Socket Exception in ReceiveLoop: {e.Message}");
                    }
                }
                catch (Exception e)
                {
                    PoseLandmarkLogger.LogError($"Exception in ReceiveLoop: {e.Message}");
                }
            }
        }
    }
}