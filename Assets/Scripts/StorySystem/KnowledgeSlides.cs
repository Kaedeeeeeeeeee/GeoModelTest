using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace StorySystem
{
    /// <summary>
    /// 知識スライド（interaction type "slides"）。土台の図は変えずに、タップするたびに部品が出る・動く・はねる。
    /// 正解・不正解はない。StoryDirector が一歩ずつ <see cref="ShowStep"/> を呼び、Kaede の一言を下の会話欄に出す。
    /// 図がまだ無いときは仮の枠で動くので、絵ができる前でも流れを確かめられる。
    /// </summary>
    public sealed class KnowledgeSlideSession
    {
        public const string Folder = "Story/Slides/";
        // 変化の過程が見えるよう、ゆっくりめに動かす（2026-11-23 に 2 倍へ）
        private const float Slow = 2f;
        private const float FadeSec = 0.35f * Slow;
        private const float MoveSec = 0.8f * Slow;
        private const float PulseSec = 0.45f * Slow;
        private const float SfxVolume = 0.6f; // Kaede の声より控えめに
        /// <summary>図の枠の位置（画面の割合）。下は会話欄のすぐ上、上は画面のふち。</summary>
        public static readonly Vector2 FrameMin = new Vector2(0.17f, StoryLayout.DialogTop);
        public static readonly Vector2 FrameMax = new Vector2(0.83f, 1f);
        private const float DefaultAspect = 1.5f; // 1536×1024

        /// <summary>表示中のスライド（PlayMode テスト用）。</summary>
        public static KnowledgeSlideSession Active { get; private set; }

        public KnowledgeCard Card { get; }
        public GameObject Root { get; private set; }
        /// <summary>いま見えている一歩（-1 は土台の図だけ）。</summary>
        public int Step { get; private set; } = -1;
        public int StepCount => Card.Steps.Count;

        private readonly Action<TMP_Text> style;
        private readonly Dictionary<string, Graphic> parts = new Dictionary<string, Graphic>();
        private readonly Dictionary<string, Vector2> home = new Dictionary<string, Vector2>();
        private RectTransform stage;
        private Button back;
        private Runner runner;

        private sealed class Runner : MonoBehaviour { }

        private KnowledgeSlideSession(KnowledgeCard card, Action<TMP_Text> style)
        {
            Card = card;
            this.style = style;
        }

        /// <param name="onTap">図をタップしたとき（次へ）。</param>
        /// <param name="onBack">◀ を押したとき（ひとつ戻る）。</param>
        public static KnowledgeSlideSession Build(Transform parent, KnowledgeCard card, Action<TMP_Text> style, Action onTap, Action onBack)
        {
            if (parent == null || card == null || card.Type != KnowledgeCard.TypeSlides || !card.IsPlayable) return null;
            var session = new KnowledgeSlideSession(card, style);
            session.BuildFrame(parent, onTap, onBack);
            Active = session;
            session.ShowStep(-1);
            return session;
        }

        public void Destroy()
        {
            if (Root != null) UnityEngine.Object.Destroy(Root);
            Root = null;
            if (Active == this) Active = null;
        }

        /// <summary>
        /// 一歩を見せる。それまでの歩みは一瞬で反映し、この一歩の変化だけをアニメーションする。
        /// 戻るときも同じ方法で作り直すので、どの順に押しても同じ絵になる。
        /// </summary>
        public void ShowStep(int index)
        {
            if (Root == null) return;
            index = Mathf.Clamp(index, -1, Card.Steps.Count - 1);
            Step = index;
            runner.StopAllCoroutines();
            targetAlpha.Clear();
            foreach (var pair in parts)
            {
                SetAlpha(pair.Value, 0f);
                Place(pair.Key, home[pair.Key]);
                pair.Value.rectTransform.localScale = Vector3.one;
            }
            for (int s = 0; s < index; s++) Apply(Card.Steps[s], false);
            if (index >= 0)
            {
                Apply(Card.Steps[index], true);
                PlaySfx(Card.Steps[index]);
            }
            if (back != null) back.gameObject.SetActive(index > 0);
        }

        /// <summary>いまの一歩で見えているはずの部品か（テスト用）。</summary>
        public bool IsShown(string id) => parts.ContainsKey(id) && TargetAlpha(id) > 0.5f;

        private readonly Dictionary<string, float> targetAlpha = new Dictionary<string, float>();
        private float TargetAlpha(string id) => targetAlpha.TryGetValue(id, out var a) ? a : 0f;

        private void Apply(KnowledgeCard.Step step, bool animate)
        {
            if (step == null) return;
            foreach (var id in step.Hide)
            {
                targetAlpha[id] = 0f;
                if (!parts.TryGetValue(id, out var g)) continue;
                if (animate && g.color.a > 0f) runner.StartCoroutine(Fade(g, 0f, 0f));
                else SetAlpha(g, 0f);
            }
            foreach (var id in step.Show)
            {
                targetAlpha[id] = 1f;
                if (!parts.TryGetValue(id, out var g)) continue;
                if (animate) runner.StartCoroutine(Appear(g, Delay(id)));
                else SetAlpha(g, 1f);
            }
            foreach (var move in step.Move)
            {
                if (!parts.ContainsKey(move.Key)) continue;
                if (animate) runner.StartCoroutine(Move(move.Key, move.Value));
                else Place(move.Key, move.Value);
            }
            if (animate)
                foreach (var id in step.Pulse)
                    if (parts.TryGetValue(id, out var g)) runner.StartCoroutine(Pulse(g, Delay(id)));
            // 前の一歩で出たものは、消されるまで出たまま
            foreach (var pair in targetAlpha)
                if (!animate && parts.TryGetValue(pair.Key, out var g)) SetAlpha(g, pair.Value);
        }

        private float Delay(string id)
        {
            var part = Card.Parts.Find(p => p.Id == id);
            return part != null ? part.Delay * Slow : 0f;
        }

        private void PlaySfx(KnowledgeCard.Step step)
        {
            if (step == null || string.IsNullOrEmpty(step.Sfx)) return;
            runner.StartCoroutine(PlaySfxAfter(step.Sfx, step.SfxDelay));
        }

        private static IEnumerator PlaySfxAfter(string key, float delay)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            GeoModel.AudioSystem.AudioManager.Instance?.PlaySFX(key, SfxVolume);
        }

        // ----- UI -----

        private void BuildFrame(Transform parent, Action onTap, Action onBack)
        {
            // 枠は図と同じ縦横比にして、上は画面のふち・下は会話欄にぴったりつける（点や余白は置かない）
            var holder = GameUI.Rect(parent, "KnowledgeSlides", FrameMin, FrameMax);
            Root = holder.gameObject;
            runner = Root.AddComponent<Runner>();
            targetAlpha.Clear();

            var baseSprite = Load(Card.Image);
            float aspect = baseSprite != null && baseSprite.rect.height > 0f ? baseSprite.rect.width / baseSprite.rect.height : DefaultAspect;
            var panel = GameUI.Box(holder, "Frame", new Color(0.05f, 0.09f, 0.12f, 0.97f), Vector2.zero, Vector2.one);
            var panelFit = panel.gameObject.AddComponent<AspectRatioFitter>();
            panelFit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            panelFit.aspectRatio = aspect;
            var panelRt = panel.rectTransform;
            panelRt.pivot = new Vector2(0.5f, 0f); // 下（会話欄）にそろえる

            var area = GameUI.Rect(panel.transform, "Area", Vector2.zero, Vector2.one);
            area.offsetMin = new Vector2(4f, 4f);
            area.offsetMax = new Vector2(-4f, -4f);
            var baseImage = GameUI.Box(area, "Stage", baseSprite != null ? Color.white : new Color(0.80f, 0.76f, 0.66f), Vector2.zero, Vector2.one);
            if (baseSprite != null) baseImage.sprite = baseSprite;
            stage = baseImage.rectTransform;
            var fitter = stage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = aspect;
            var tap = stage.gameObject.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(() => onTap?.Invoke());

            foreach (var part in Card.Parts) BuildPart(part);

            // ひとつ戻る：図の左下にのせる
            back = GameUI.Button(stage, "Back", "◀", new Vector2(0.015f, 0.02f), new Vector2(0.095f, 0.13f), () => onBack?.Invoke());
            back.transform.SetAsLastSibling();
        }

        private void BuildPart(KnowledgeCard.Part part)
        {
            if (part == null || parts.ContainsKey(part.Id)) return;
            var rt = GameUI.Rect(stage, "Part_" + part.Id, Vector2.zero, Vector2.zero);
            rt.sizeDelta = Vector2.zero;
            rt.localEulerAngles = new Vector3(0f, 0f, part.Rotation);
            Graphic graphic;
            if (!string.IsNullOrEmpty(part.Text))
            {
                var pill = rt.gameObject.AddComponent<Image>();
                pill.color = new Color(0.08f, 0.12f, 0.16f, 0.88f);
                var label = GameUI.Rect(rt, "Text", new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f)).gameObject.AddComponent<TextMeshProUGUI>();
                style?.Invoke(label);
                label.text = FuriganaProcessor.Process(part.Text);
                label.enableAutoSizing = true;
                label.fontSizeMin = 12f;
                label.fontSizeMax = 30f;
                label.alignment = TextAlignmentOptions.Center;
                label.color = GameUI.Ink;
                label.raycastTarget = false;
                graphic = pill;
            }
            else
            {
                var img = rt.gameObject.AddComponent<Image>();
                var sprite = Load(part.Image);
                if (sprite != null)
                {
                    img.sprite = sprite;
                    img.preserveAspect = true;
                    img.color = Color.white;
                }
                else if (part.Color.HasValue) img.color = part.Color.Value;
                else
                {
                    // 図がまだ無い部品は、名前入りの仮の枠
                    img.color = new Color(1f, 0.85f, 0.4f, 0.55f);
                    var label = GameUI.Rect(rt, "Placeholder", Vector2.zero, Vector2.one).gameObject.AddComponent<TextMeshProUGUI>();
                    style?.Invoke(label);
                    label.text = part.Id;
                    label.enableAutoSizing = true;
                    label.fontSizeMin = 8f;
                    label.fontSizeMax = 20f;
                    label.alignment = TextAlignmentOptions.Center;
                    label.color = Color.black;
                    label.raycastTarget = false;
                }
                graphic = img;
            }
            graphic.raycastTarget = false;
            baseAlpha[graphic] = graphic.color.a > 0f ? graphic.color.a : 1f;
            parts[part.Id] = graphic;
            home[part.Id] = part.Area.center;
            SetSize(rt, part.Area.size);
            Place(part.Id, part.Area.center);
            SetAlpha(graphic, 0f);
        }

        private static void SetSize(RectTransform rt, Vector2 size)
        {
            // 位置は anchor の中心で決め、大きさは anchor の幅で決める
            rt.anchorMin = new Vector2(0.5f - size.x / 2f, 0.5f - size.y / 2f);
            rt.anchorMax = new Vector2(0.5f + size.x / 2f, 0.5f + size.y / 2f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private void Place(string id, Vector2 center)
        {
            var rt = parts[id].rectTransform;
            Vector2 half = (rt.anchorMax - rt.anchorMin) / 2f;
            rt.anchorMin = center - half;
            rt.anchorMax = center + half;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static Sprite Load(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            var sprite = Resources.Load<Sprite>(Folder + key);
            if (sprite != null) return sprite;
            var tex = Resources.Load<Texture2D>(Folder + key);
            return tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)) : null;
        }

        // ----- アニメーション -----

        // 色の帯や仮の枠は、もとの透明度を上限にする
        private readonly Dictionary<Graphic, float> baseAlpha = new Dictionary<Graphic, float>();
        private float BaseAlpha(Graphic g) => baseAlpha.TryGetValue(g, out var a) ? a : 1f;

        private void SetAlpha(Graphic g, float a)
        {
            var c = g.color;
            c.a = BaseAlpha(g) * a;
            g.color = c;
            foreach (var child in g.GetComponentsInChildren<TMP_Text>(true)) child.alpha = a;
        }

        private static float Ease(float t) => 1f - (1f - t) * (1f - t);

        private IEnumerator Appear(Graphic g, float delay)
        {
            SetAlpha(g, 0f);
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            var rt = g.rectTransform;
            for (float t = 0f; t < FadeSec; t += Time.unscaledDeltaTime)
            {
                float k = Ease(t / FadeSec);
                SetAlpha(g, k);
                rt.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, k);
                yield return null;
            }
            SetAlpha(g, 1f);
            rt.localScale = Vector3.one;
        }

        private IEnumerator Fade(Graphic g, float to, float delay)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            float from = g.color.a / BaseAlpha(g);
            for (float t = 0f; t < FadeSec; t += Time.unscaledDeltaTime)
            {
                SetAlpha(g, Mathf.Lerp(from, to, t / FadeSec));
                yield return null;
            }
            SetAlpha(g, to);
        }

        private IEnumerator Move(string id, Vector2 to)
        {
            var rt = parts[id].rectTransform;
            Vector2 from = (rt.anchorMin + rt.anchorMax) / 2f;
            for (float t = 0f; t < MoveSec; t += Time.unscaledDeltaTime)
            {
                Place(id, Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / MoveSec)));
                yield return null;
            }
            Place(id, to);
        }

        private IEnumerator Pulse(Graphic g, float delay)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            var rt = g.rectTransform;
            for (float t = 0f; t < PulseSec; t += Time.unscaledDeltaTime)
            {
                rt.localScale = Vector3.one * (1f + 0.18f * Mathf.Sin(Mathf.PI * t / PulseSec));
                yield return null;
            }
            rt.localScale = Vector3.one;
        }
    }
}
