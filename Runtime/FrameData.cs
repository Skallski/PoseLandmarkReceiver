using System;
using UnityEngine;

namespace PoseLandmarkReceiver
{
    public struct FrameData
    {
        public enum LandmarkType
        {
            Nose = 0,
            LeftEyeInner = 1,
            LeftEye = 2,
            LeftEyeOuter = 3,
            RightEyeInner = 4,
            RightEye = 5,
            RightEyeOuter = 6,
            LeftEar = 7,
            RightEar = 8,
            MouthLeft = 9,
            MouthRight = 10,
            LeftShoulder = 11,
            RightShoulder = 12,
            LeftElbow = 13,
            RightElbow = 14,
            LeftWrist = 15,
            RightWrist = 16,
            LeftPinky = 17,
            RightPinky = 18,
            LeftIndex = 19,
            RightIndex = 20,
            LeftThumb = 21,
            RightThumb = 22,
            LeftHip = 23,
            RightHip = 24,
            LeftKnee = 25,
            RightKnee = 26,
            LeftAnkle = 27,
            RightAnkle = 28,
            LeftHeel = 29,
            RightHeel = 30,
            LeftFootIndex = 31,
            RightFootIndex = 32
        }

        public readonly Landmark[] Landmarks;
        public readonly byte[] Frame;

        public FrameData(Landmark[] landmarks, string frameB64)
        {
            Landmarks = landmarks;
            Frame = string.IsNullOrEmpty(frameB64) == false ? Convert.FromBase64String(frameB64) : null;
        }

        public Texture2D GetFrameAsTexture()
        {
            Texture2D res = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            if (Frame == null)
            {
                Debug.LogError("Frame cannot be converted to Texture2D, because it is null!");
            }
            else
            {
                res.LoadImage(Frame);
            }

            return res;
        }

        public Landmark GetLandmarkByType(LandmarkType landmarkType)
        {
            if (Landmarks.Length == 0)
            {
                Debug.LogWarning("No landmarks found in current frame");
                return default;
            }

            return Landmarks[(int)landmarkType];
        }
    }
}