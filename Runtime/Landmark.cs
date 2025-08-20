
namespace PoseLandmarkReceiver
{
    [System.Serializable]
    public struct Landmark
    {
        public float x, y, z;

        public override string ToString()
        {
            return $"{x}, {y}, {z}";
        }
    }
}