using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace StackBalance
{
    public sealed class GameUIController : MonoBehaviour
    {
        [SerializeField] private Text scoreText;
        [SerializeField] private Text heightText;
        [SerializeField] private Text weightText;
        [SerializeField] private Text feedbackText;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Text gameOverReasonText;
        [SerializeField] private Text finalScoreText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button endGameButton;

        private Coroutine feedbackRoutine;

        public void Configure(Text score, Text height, Text weight, Text feedback,
            GameObject panel, Text reason, Text finalScore, Button restart, Button endGame)
        {
            scoreText = score;
            heightText = height;
            weightText = weight;
            feedbackText = feedback;
            gameOverPanel = panel;
            gameOverReasonText = reason;
            finalScoreText = finalScore;
            restartButton = restart;
            endGameButton = endGame;
        }

        public void BindRestart(UnityEngine.Events.UnityAction restartAction)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(restartAction);
        }

        public void BindEndGame(UnityEngine.Events.UnityAction endGameAction)
        {
            EnsureEndGameButton();
            endGameButton.onClick.RemoveAllListeners();
            endGameButton.onClick.AddListener(endGameAction);
        }

        public void UpdateScore(int score, int height)
        {
            scoreText.text = $"Score: {score}";
            heightText.text = $"Height: {height}";
        }

        public void SetCurrentWeight(BlockWeightType type)
        {
            weightText.text = $"Current: {type}";
        }

        public void ShowFeedback(string message)
        {
            if (feedbackRoutine != null)
            {
                StopCoroutine(feedbackRoutine);
            }
            feedbackRoutine = StartCoroutine(ShowFeedbackRoutine(message));
        }

        public void HideGameOver()
        {
            gameOverPanel.SetActive(false);
            feedbackText.text = string.Empty;
        }

        public void ShowGameOver(string reason, int score)
        {
            gameOverReasonText.text = reason;
            finalScoreText.text = $"Final score: {score}";
            gameOverPanel.SetActive(true);
        }

        private void EnsureEndGameButton()
        {
            if (endGameButton != null)
            {
                return;
            }

            RectTransform restartRect = restartButton.GetComponent<RectTransform>();
            restartRect.anchoredPosition = new Vector2(-145f, restartRect.anchoredPosition.y);
            restartRect.sizeDelta = new Vector2(240f, restartRect.sizeDelta.y);

            endGameButton = Instantiate(restartButton, restartButton.transform.parent);
            endGameButton.name = "End Game Button";
            RectTransform endRect = endGameButton.GetComponent<RectTransform>();
            endRect.anchoredPosition = new Vector2(145f, restartRect.anchoredPosition.y);
            endRect.sizeDelta = restartRect.sizeDelta;

            Text label = endGameButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = "End Game";
            }
        }

        private IEnumerator ShowFeedbackRoutine(string message)
        {
            feedbackText.text = message;
            yield return new WaitForSeconds(0.8f);
            feedbackText.text = string.Empty;
            feedbackRoutine = null;
        }
    }
}
