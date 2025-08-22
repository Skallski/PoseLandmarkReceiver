using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PoseLandmarkReceiver
{
    public class FramePreview : MonoBehaviour
    {
        [SerializeField] private bool _showPreview = true;
        [SerializeField] private bool _showlandmarks = true;
        [SerializeField] private RawImage _preview;

        private Texture2D _frameTexture;

#if UNITY_EDITOR
        // MediaPipe Pose connections (33-keypoint skeleton).
        private static readonly int[,] PoseConnections = new int[,]
        {
            {0, 1},   // nose -> left eye inner
            {1, 2},   // left eye inner -> left eye
            {2, 3},   // left eye -> left eye outer
            {3, 7},   // left eye outer -> left ear
            {0, 4},   // nose -> right eye inner
            {4, 5},   // right eye inner -> right eye
            {5, 6},   // right eye -> right eye outer
            {6, 8},   // right eye outer -> right ear
            {9, 10},  // mouth left -> mouth right

            {11, 12}, // left shoulder -> right shoulder
            {11, 13}, // left shoulder -> left elbow
            {13, 15}, // left elbow -> left wrist
            {15, 17}, // left wrist -> left pinky
            {15, 19}, // left wrist -> left index
            {15, 21}, // left wrist -> left thumb
            {17, 19}, // left pinky -> left index (hand web)

            {12, 14}, // right shoulder -> right elbow
            {14, 16}, // right elbow -> right wrist
            {16, 18}, // right wrist -> right pinky
            {16, 20}, // right wrist -> right index
            {16, 22}, // right wrist -> right thumb
            {18, 20}, // right pinky -> right index (hand web)

            {11, 23}, // left shoulder -> left hip
            {12, 24}, // right shoulder -> right hip
            {23, 24}, // left hip -> right hip (pelvis)

            {23, 25}, // left hip -> left knee
            {24, 26}, // right hip -> right knee
            {25, 27}, // left knee -> left ankle
            {26, 28}, // right knee -> right ankle
            {27, 29}, // left ankle -> left heel
            {28, 30}, // right ankle -> right heel
            {29, 31}, // left heel -> left foot index (toe)
            {30, 32}, // right heel -> right foot index (toe)
            {27, 31}, // left ankle -> left foot index (toe top)
            {28, 32}, // right ankle -> right foot index (toe top)
        };
        
        private readonly List<Vector2> _landmarks = new List<Vector2>();

        private void OnDrawGizmos()
        {
            if (_showlandmarks == false || _preview == null || _landmarks == null || _landmarks.Count == 0)
            {
                return;
            }

            RectTransform rt = _preview.rectTransform;
            Vector2 size = rt.rect.size;

            // Convert all landmarks from normalized [0..1] space into world space points
            List<Vector3> worldPoints = new List<Vector3>(_landmarks.Count);

            foreach (var lm in _landmarks)
            {
                // Scale from normalized coordinates to preview rect size
                float px = lm.x * size.x;
                float py = (1f - lm.y) * size.y; // flip Y

                // Move origin from bottom-left corner to rect center
                Vector3 localPos = new Vector3(px, py, 0f);
                localPos -= new Vector3(size.x / 2f, size.y / 2f, 0f);

                // Transform local rect coordinates into world space
                worldPoints.Add(rt.TransformPoint(localPos));
            }

            // Draw landmarks
            Gizmos.color = Color.green;
            foreach (Vector3 p in worldPoints)
            {
                Gizmos.DrawSphere(p, 2f);
            }

            // Draw skeleton connections
            Gizmos.color = Color.white;
            for (int i = 0; i < PoseConnections.GetLength(0); i++)
            {
                int a = PoseConnections[i, 0];
                int b = PoseConnections[i, 1];

                if (a < worldPoints.Count && b < worldPoints.Count)
                {
                    Gizmos.DrawLine(worldPoints[a], worldPoints[b]);
                }
            }
        }
#endif

        private void OnEnable()
        {
            UdpLandmarksReceiver.OnFrameDataReceived += OnFrameDataReceived;
        }
        
        private void OnDisable()
        {
            UdpLandmarksReceiver.OnFrameDataReceived -= OnFrameDataReceived;
        }

        private void OnFrameDataReceived(FrameData frameData)
        {

#if UNITY_EDITOR
            // Show landmarks
            if (_showlandmarks && frameData.Landmarks != null)
            {
                _landmarks.Clear();
                foreach (Landmark lm in frameData.Landmarks)
                {
                    _landmarks.Add(new Vector2(lm.x, lm.y));
                }
            }
#endif

            // Show frame preview
            if (_showPreview && frameData.Frame != null)
            {
                if (_frameTexture == null)
                {
                    _frameTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                }
                    
                _frameTexture.LoadImage(frameData.Frame);
                
                if (_preview != null)
                {
                    _preview.texture = _frameTexture;

                    RectTransform rt = _preview.rectTransform;
                    Vector2 sizeDelta = rt.sizeDelta;
                    float aspect = (float)_frameTexture.width / _frameTexture.height;
                    rt.sizeDelta = new Vector2(sizeDelta.y * aspect, sizeDelta.y);
                }
            }
        }
    }
}