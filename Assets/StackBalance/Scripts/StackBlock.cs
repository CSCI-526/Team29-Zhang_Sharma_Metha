using UnityEngine;

namespace StackBalance
{
    public enum BlockWeightType
    {
        Light,
        Normal,
        Heavy
    }

    public sealed class StackBlock : MonoBehaviour
    {
        public BlockWeightType WeightType { get; private set; }
        public float Mass { get; private set; }
        public float Width => transform.localScale.x;
        public float Height => transform.localScale.y;
        public float CenterX => transform.position.x;

        public void Configure(BlockWeightType weightType, float mass, Color color)
        {
            WeightType = weightType;
            Mass = mass;

            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = color;
            }
        }

        public void SetGeometry(float centerX, float width)
        {
            transform.position = new Vector3(centerX, transform.position.y, transform.position.z);
            transform.localScale = new Vector3(width, transform.localScale.y, 1f);
        }
    }
}
