using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UISystem;
using Core;

namespace StorySystem
{
    public static class InvestigationReport
    {
        private static Canvas _activeCanvas;
        public static IEnumerator Show(Action onComplete = null)
        {
            if (_activeCanvas != null) yield break;
            var summary = QuizScoreManager.Instance.BuildSummary();
            var canvas = GameUI.Canvas("ReportCanvas", 32767);
            _activeCanvas = canvas;
            bool close = false;
            var input = GameInputState.Acquire();
            var layer = ModalCanvasLayerGuard.Activate(canvas);
            GameUI.Box(canvas.transform, "Dim", new Color(0.01f, 0.04f, 0.06f, 0.92f), Vector2.zero, Vector2.one);
            var card = GameUI.Box(canvas.transform, "Report", GameUI.Surface, new Vector2(0.17f, 0.07f), new Vector2(0.83f, 0.93f));
            var cardButton = card.gameObject.AddComponent<Button>();
            cardButton.targetGraphic = card;
            cardButton.transition = Selectable.Transition.None;
            cardButton.onClick.AddListener(() => { if (!Backend.SurveyGateway.Instance.IsBusy) close = true; });
            GameUI.Box(card.transform, "Accent", GameUI.Accent, new Vector2(0, 0.989f), Vector2.one).raycastTarget = false;
            GameUI.Label(card.transform, "Eyebrow", "G-LAB  /  INVESTIGATION REPORT", 19, new Vector2(0.07f, 0.91f), new Vector2(0.94f, 0.96f)).color = GameUI.Accent;
            GameUI.Label(card.transform, "Title", GameUI.L("report.complete_title"), 43, new Vector2(0.07f, 0.79f), new Vector2(0.94f, 0.90f));
            GameUI.Label(card.transform, "Message", GameUI.L("report.complete_message"), 25, new Vector2(0.07f, 0.70f), new Vector2(0.94f, 0.80f)).color = GameUI.Muted;
            Metric(card.transform, "FirstCorrect", GameUI.L("report.first_correct"), $"{summary.FirstCorrectCount} / {summary.ExpectedQuestionCount}", 0.50f, 0.69f, 0.07f, 0.47f);
            Metric(card.transform, "WrongAttempts", GameUI.L("report.wrong_attempts"), summary.WrongAttemptCount.ToString(), 0.50f, 0.69f, 0.53f, 0.93f);
            Metric(card.transform, "Mastered", GameUI.L("report.final_mastery"), $"{summary.FinalMasteredCount} / {summary.ExpectedQuestionCount}", 0.29f, 0.48f, 0.07f, 0.47f);
            Metric(card.transform, "Practice", GameUI.L("report.practice"), $"{InvestigationProgress.ActivityCount} / {InvestigationProgress.ActivityTotal}", 0.29f, 0.48f, 0.53f, 0.93f);
            GameUI.Label(card.transform, "PracticeDetails", GameUI.L("report.practice_details"), 22, new Vector2(0.07f, 0.19f), new Vector2(0.94f, 0.28f)).color = GameUI.Muted;
            var surveyStatus = GameUI.Label(card.transform, "SurveyStatus", "", 18, new Vector2(0.07f, 0.01f), new Vector2(0.94f, 0.055f));
            surveyStatus.color = GameUI.Muted;
            var returnButton = GameUI.Button(card.transform, "ReturnToTitle", GameUI.L("ui.session.return_title"), new Vector2(0.07f, 0.07f), new Vector2(0.47f, 0.16f), () => close = true);
            var surveyButton = GameUI.Button(card.transform, "AnswerSurvey", GameUI.L("survey.answer"), new Vector2(0.53f, 0.07f), new Vector2(0.93f, 0.16f), () => Backend.SurveyGateway.Instance.Open(message => surveyStatus.text = message), true);
            if (!Backend.SurveyGateway.IsEligible) surveyStatus.text = GameUI.L("survey.research_only");
            yield return null;
            while (!close)
            {
                bool busy = Backend.SurveyGateway.Instance.IsBusy;
                returnButton.interactable = !busy;
                surveyButton.interactable = !busy && Backend.SurveyGateway.IsEligible;
                if (!busy && !SettingsManager.Instance.IsSettingsOpen &&
                    (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))) close = true;
                yield return null;
            }
            layer.Dispose();
            input.Dispose();
            UnityEngine.Object.Destroy(canvas.gameObject);
            onComplete?.Invoke();
            SceneSystem.GameSession.Instance.ReturnToTitle();
        }

        private static void Metric(Transform parent, string name, string label, string value, float bottom, float top, float left, float right)
        {
            var box = GameUI.Box(parent, name, GameUI.Panel, new Vector2(left, bottom), new Vector2(right, top));
            box.raycastTarget = false;
            GameUI.Label(box.transform, "Label", label, 22, new Vector2(0.06f, 0.63f), new Vector2(0.96f, 0.95f)).color = GameUI.Muted;
            GameUI.Label(box.transform, "Value", value, 50, new Vector2(0.06f, 0.08f), new Vector2(0.96f, 0.66f)).color = GameUI.Accent;
        }
    }
}
