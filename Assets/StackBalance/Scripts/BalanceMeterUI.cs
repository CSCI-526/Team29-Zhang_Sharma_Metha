using UnityEngine;
using UnityEngine.UI;

namespace StackBalance
{
    public sealed class BalanceMeterUI : MonoBehaviour
    {
        [SerializeField] private RectTransform pointer;
        [SerializeField] private RectTransform meterTrack;
        [SerializeField] private Text valueLabel;

        public void Configure(RectTransform newPointer, RectTransform newTrack, Text newLabel)
        {
            pointer = newPointer;
            meterTrack = newTrack;
            valueLabel = newLabel;
        }

        public void SetBalance(float normalizedBalance, float torque)
        {
            if (pointer != null && meterTrack != null)
            {
                float halfWidth = meterTrack.rect.width * 0.5f;
                Vector2 position = pointer.anchoredPosition;
                position.x = Mathf.Clamp(normalizedBalance, -1f, 1f) * halfWidth;
                pointer.anchoredPosition = position;
            }

            if (valueLabel != null)
            {
                string direction = Mathf.Abs(torque) < 0.05f ? "SAFE" : torque < 0f ? "LEFT" : "RIGHT";
                valueLabel.text = $"Balance: {direction}  ({torque:0.0})";
            }
        }
    }
}
