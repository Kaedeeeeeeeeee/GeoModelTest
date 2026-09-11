using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
            bool surveyAttempted = false;
            int surveyClickFrame = -1;
            bool CanReturnToTitle()
            {
                return !Backend.SurveyGateway.Instance.IsBusy &&
                    (surveyAttempted || !Backend.SurveyGateway.IsEligible);
            }
            void RequestReturnToTitle()
            {
                if (CanReturnToTitle()) close = true;
            }
            var input = GameInputState.Acquire();
            var layer = ModalCanvasLayerGuard.Activate(canvas);
            GameUI.Box(canvas.transform, "Dim", new Color(0.01f, 0.04f, 0.06f, 0.92f), Vector2.zero, Vector2.one);
            var card = GameUI.Box(canvas.transform, "Report", GameUI.Surface, new Vector2(0.17f, 0.07f), new Vector2(0.83f, 0.93f));
            var cardButton = card.gameObject.AddComponent<Button>();
            cardButton.targetGraphic = card;
            cardButton.transition = Selectable.Transition.None;
            cardButton.onClick.AddListener(RequestReturnToTitle);
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
            var returnButton = GameUI.Button(card.transform, "ReturnToTitle", GameUI.L("ui.session.return_title"), new Vector2(0.07f, 0.07f), new Vector2(0.47f, 0.16f), RequestReturnToTitle);
            var returnLabel = returnButton.GetComponentInChildren<Text>();
            Button surveyButton = null;
            surveyButton = GameUI.Button(card.transform, "AnswerSurvey", GameUI.L("survey.answer"), new Vector2(0.53f, 0.07f), new Vector2(0.93f, 0.16f), () =>
            {
                if (Backend.SurveyGateway.Instance.IsBusy || !Backend.SurveyGateway.IsEligible) return;
                // An attempt is enough: a failed connection must not trap the player here.
                surveyAttempted = true;
                surveyClickFrame = Time.frameCount;
                Backend.SurveyGateway.Instance.Open(message => surveyStatus.text = message);
                RefreshButtons();
            }, true);
            void RefreshButtons()
            {
                bool canReturn = CanReturnToTitle();
                returnButton.interactable = canReturn;
                returnLabel.color = canReturn ? GameUI.Ink : new Color(0.46f, 0.52f, 0.53f);
                surveyButton.interactable = !Backend.SurveyGateway.Instance.IsBusy && Backend.SurveyGateway.IsEligible;
            }
            surveyStatus.text = GameUI.L(Backend.SurveyGateway.IsEligible ? "survey.open_first" : "survey.research_only");
            RefreshButtons();
            yield return null;
            while (!close)
            {
                RefreshButtons();
                bool surveySelected = EventSystem.current != null &&
                    EventSystem.current.currentSelectedGameObject == surveyButton.gameObject;
                var keyboard = Keyboard.current;
                if (CanReturnToTitle() && Time.frameCount > surveyClickFrame && !surveySelected &&
                    !SettingsManager.Instance.IsSettingsOpen && keyboard != null &&
                    (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)) RequestReturnToTitle();
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
