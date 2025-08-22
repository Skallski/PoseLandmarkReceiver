using System;
using UnityEngine;

namespace PoseLandmarkReceiver
{
    public struct FrameData
    {
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
                PoseLandmarkLogger.LogError("Frame cannot be converted to Texture2D, because it is null!");
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
                PoseLandmarkLogger.LogWarning("No landmarks found in current frame");
                return default;
            }

            return Landmarks[(int)landmarkType];
        }
    }
}