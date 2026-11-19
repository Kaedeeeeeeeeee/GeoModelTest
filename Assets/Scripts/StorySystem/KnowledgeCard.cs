using System;
using System.Collections.Generic;
using TMPro;
using Core;
using GeoModel.AudioSystem;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace StorySystem
{
    // ===== JSON (Resources/Story/*.json の dialogues[].interaction) =====

    /// <summary>
    /// 知識インタラクションカード。確認問題の前に、図を見るだけでなく自分で並べる・分ける・タップする。
    /// type: "order"（正しい順にタップ）/ "sort"（カードを選んで箱をタップ）/ "columns"（柱状図の目的の層をタップ）/
    /// "choose"（集めた証拠を見て結論をえらぶ。採点しない）。
    /// </summary>
    [Serializable]
    public class StoryInteraction
    {
        public string id;
        public string type;
        public string title;
        public string titleKey;
        public string wrong;
        public string wrongKey;
        public string done;
        public string doneKey;
        public List<StoryInteractionItem> items;
        public List<StoryInteractionBin> bins;
        public List<StoryInteractionColumn> columns;
        public List<StoryInteractionText> evidence; // choose: 上にならべる証拠カード
    }

    [Serializable]
    public class StoryInteractionText
    {
        public string text;
        public string textKey;
    }

    [Serializable]
    public class StoryInteractionItem
    {
        public string id;
        public string text;
        public string textKey;
        public string note;
        public string noteKey;
        public string bin;      // sort: 正解の箱 id
        public int order;       // order: 正解の順番（1 から）。表示は items の並び順
        public string hint;
        public string hintKey;  // まちがえたときの一言（なければ card の wrong）
        public bool correct;    // choose: 正しい結論
    }

    [Serializable]
    public class StoryInteractionBin
    {
        public string id;
        public string text;
        public string textKey;
    }

    [Serializable]
    public class StoryInteractionColumn
    {
        public string label;
        public string labelKey;
        public List<StoryInteractionLayer> layers; // 上（浅い）→ 下（深い）
    }

    [Serializable]
    public class StoryInteractionLayer
    {
        public string text;
        public string textKey;
        public string color;   // "#RRGGBB"
        public float size;     // 相対的な厚さ（0 以下は 1）
        public bool target;    // タップすべき層
        public string hint;
        public string hintKey;
    }

    // ===== 実行時（ローカライズ済み） =====

    public sealed class KnowledgeCard
    {
        public const string TypeOrder = "order";
        public const string TypeSort = "sort";
        public const string TypeColumns = "columns";
        public const string TypeChoose = "choose";

        public string Id;
        public string Type;
        public string Title;
        public string Wrong;
        public string Done;
        public string DoneKey; // 配音ファイル名
        public readonly List<Item> Items = new List<Item>();
        public readonly List<Bin> Bins = new List<Bin>();
        public readonly List<Column> Columns = new List<Column>();
        public readonly List<string> Evidence = new List<string>();

        public sealed class Item { public string Id; public string Text; public string Note; public string Bin; public int Order; public string Hint; public bool Correct; }
        public sealed class Bin { public string Id; public string Text; }
        public sealed class Column { public string Label; public readonly List<Layer> Layers = new List<Layer>(); }
        public sealed class Layer { public string Text; public Color Color; public float Size; public bool Target; public string Hint; }

        /// <summary>カードとして遊べる内容があるか（JsonUtility は欠けたフィールドも空オブジェクトにするため）。</summary>
        public bool IsPlayable
        {
            get
            {
                switch (Type)
                {
                    case TypeOrder:
                        if (Items.Count < 2) return false;
                        for (int o = 1; o <= Items.Count; o++)
                            if (Items.FindAll(i => i.Order == o).Count != 1) return false;
                        return true;
                    case TypeSort:
                        if (Items.Count == 0 || Bins.Count < 2) return false;
                        foreach (var item in Items)
                            if (Bins.FindIndex(b => b.Id == item.Bin) < 0) return false;
                        return true;
                    case TypeColumns:
                        if (Columns.Count == 0) return false;
                        foreach (var c in Columns)
                            if (!c.Layers.Exists(l => l.Target)) return false;
                        return true;
                    case TypeChoose: return Items.Count >= 2 && Items.Exists(i => i.Correct);
                    default: return false;
                }
            }
        }

        public static KnowledgeCard From(StoryInteraction source, Func<string, string, string> resolve)
        {
            if (source == null || string.IsNullOrWhiteSpace(source.type)) return null;
            var card = new KnowledgeCard
            {
                Id = string.IsNullOrWhiteSpace(source.id) ? "card" : source.id.Trim(),
                Type = source.type.Trim().ToLowerInvariant(),
                Title = resolve(source.title, source.titleKey),
                Wrong = resolve(source.wrong, source.wrongKey),
                Done = resolve(source.done, source.doneKey),
                DoneKey = source.doneKey
            };
            if (source.items != null)
                foreach (var i in source.items)
                {
                    if (i == null) continue;
                    card.Items.Add(new Item
                    {
                        Id = i.id,
                        Text = resolve(i.text, i.textKey),
                        Note = resolve(i.note, i.noteKey),
                        Bin = i.bin,
                        Order = i.order,
                        Hint = resolve(i.hint, i.hintKey),
                        Correct = i.correct
                    });
                }
            if (source.bins != null)
                foreach (var b in source.bins)
                    if (b != null) card.Bins.Add(new Bin { Id = b.id, Text = resolve(b.text, b.textKey) });
            if (source.evidence != null)
                foreach (var e in source.evidence)
                    if (e != null) card.Evidence.Add(resolve(e.text, e.textKey));
            if (source.columns != null)
                foreach (var c in source.columns)
                {
                    if (c == null) continue;
                    var column = new Column { Label = resolve(c.label, c.labelKey) };
                    if (c.layers != null)
                        foreach (var l in c.layers)
                        {
                            if (l == null) continue;
                            Color color;
                            if (string.IsNullOrEmpty(l.color) || !ColorUtility.TryParseHtmlString(l.color, out color))
                                color = new Color(0.6f, 0.6f, 0.6f);
                            column.Layers.Add(new Layer
                            {
                                Text = resolve(l.text, l.textKey),
                                Color = color,
                                Size = l.size > 0f ? l.size : 1f,
                                Target = l.target,
                                Hint = resolve(l.hint, l.hintKey)
                            });
                        }
                    card.Columns.Add(column);
                }
            return card.IsPlayable ? card : null;
        }
    }

    /// <summary>
    /// カード 1 枚ぶんの UI。StoryDirector のコルーチンが <see cref="Done"/> を待つ。
    /// まちがえても減点しない（スコアは確認問題だけ）。まちがい回数と時間はログに残す。
    /// </summary>
    public sealed class KnowledgeCardSession
    {
        private static readonly Color CardColor = new Color(0.12f, 0.16f, 0.22f, 0.96f);
        private static readonly Color SelectedColor = new Color(0.20f, 0.42f, 0.38f, 1f);
        private static readonly Color WrongColor = new Color(0.55f, 0.20f, 0.20f, 1f);
        private static readonly Color PlacedColor = new Color(0.16f, 0.34f, 0.30f, 1f);
        private static readonly Color SlotColor = new Color(0.07f, 0.12f, 0.15f, 1f);

        /// <summary>表示中のカード（PlayMode テストが本物の操作で解くため）。</summary>
        public static KnowledgeCardSession Active { get; private set; }

        public GameObject Root { get; private set; }
        public bool Done { get; private set; }
        public int WrongCount { get; private set; }
        public KnowledgeCard Card { get; }

        private readonly Action<TMP_Text> style;
        private TextMeshProUGUI status;
        private RectTransform content;
        private Image lastWrong;
        private Color lastWrongColor;

        // order / sort
        private readonly List<Button> itemButtons = new List<Button>();
        private readonly List<Image> slotImages = new List<Image>();
        private readonly List<TextMeshProUGUI> slotLabels = new List<TextMeshProUGUI>();
        private readonly List<RectTransform> binLists = new List<RectTransform>();
        private int nextOrder = 1;
        private int selected = -1;
        private int placedCount;

        // columns
        private readonly List<RectTransform> tappedTargets = new List<RectTransform>();
        private int targetsLeft;
        private readonly Dictionary<Button, KnowledgeCard.Layer> layerOf = new Dictionary<Button, KnowledgeCard.Layer>();

        private KnowledgeCardSession(KnowledgeCard card, Action<TMP_Text> style)
        {
            Card = card;
            this.style = style;
        }

        public static KnowledgeCardSession Build(Transform parent, KnowledgeCard card, Action<TMP_Text> style)
        {
            if (parent == null || card == null || !card.IsPlayable) return null;
            var session = new KnowledgeCardSession(card, style);
            session.BuildFrame(parent);
            Active = session;
            switch (card.Type)
            {
                case KnowledgeCard.TypeOrder: session.BuildOrder(); break;
                case KnowledgeCard.TypeSort: session.BuildSort(); break;
                case KnowledgeCard.TypeColumns: session.BuildColumns(); break;
                case KnowledgeCard.TypeChoose: session.BuildChoose(); break;
            }
            return session;
        }

        public void Destroy()
        {
            if (Root != null) UnityEngine.Object.Destroy(Root);
            Root = null;
            if (Active == this) Active = null;
        }

        /// <summary>正しい操作を順に行う（テスト用）。ボタンと同じハンドラを通るので判定ロジックも検証される。</summary>
        public void SolveForTesting()
        {
            switch (Card.Type)
            {
                case KnowledgeCard.TypeOrder:
                    for (int o = nextOrder; o <= Card.Items.Count && !Done; o++)
                        OnOrderTap(Card.Items.FindIndex(i => i.Order == o));
                    break;
                case KnowledgeCard.TypeSort:
                    for (int i = 0; i < Card.Items.Count && !Done; i++)
                    {
                        if (!itemButtons[i].interactable) continue;
                        OnItemSelect(i);
                        OnBinTap(Card.Bins.FindIndex(b => b.Id == Card.Items[i].Bin));
                    }
                    break;
                case KnowledgeCard.TypeChoose:
                    OnChoose(Card.Items.FindIndex(i => i.Correct));
                    break;
                case KnowledgeCard.TypeColumns:
                    foreach (var btn in Root.GetComponentsInChildren<Button>())
                    {
                        if (Done) break;
                        if (btn.interactable && btn.name.StartsWith("Layer") && layerOf.TryGetValue(btn, out var layer) && layer.Target)
                            OnLayerTap(btn, (Image)btn.targetGraphic, layer);
                    }
                    break;
            }
        }

        // ----- 共通 -----

        private void BuildFrame(Transform parent)
        {
            var panel = GameUI.Box(parent, "KnowledgeCard", new Color(0.05f, 0.09f, 0.12f, 0.97f),
                new Vector2(0.17f, 0.36f), new Vector2(0.83f, 0.97f));
            Root = panel.gameObject;

            var title = MakeText(panel.transform, "Title", Card.Title, 30, new Vector2(0.03f, 0.87f), new Vector2(0.97f, 0.99f));
            title.color = GameUI.Accent;
            title.alignment = TextAlignmentOptions.Center;

            content = GameUI.Rect(panel.transform, "Content", new Vector2(0.03f, 0.14f), new Vector2(0.97f, 0.86f));

            status = MakeText(panel.transform, "Status", string.Empty, 24, new Vector2(0.03f, 0.01f), new Vector2(0.97f, 0.13f));
            status.alignment = TextAlignmentOptions.Center;
            status.color = GameUI.Ink;
        }

        private TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Vector2 min, Vector2 max)
        {
            var rt = GameUI.Rect(parent, name, min, max);
            var label = rt.gameObject.AddComponent<TextMeshProUGUI>();
            style?.Invoke(label);
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.enableAutoSizing = true;
            label.fontSizeMax = size;
            label.fontSizeMin = Mathf.Min(16f, size);
            label.lineSpacing = 18f; // 注音ぶんの行間
            label.alignment = TextAlignmentOptions.Center;
            label.color = GameUI.Ink;
            label.text = FuriganaProcessor.Process(text ?? string.Empty);
            return label;
        }

        private Button CardButton(Transform parent, string name, string text, string note, Vector2 min, Vector2 max, Action onClick)
        {
            var img = GameUI.Box(parent, name, CardColor, min, max);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.75f, 0.9f, 1f, 1f);
            colors.pressedColor = new Color(0.6f, 0.8f, 0.95f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.35f);
            btn.colors = colors;
            var nav = btn.navigation; nav.mode = Navigation.Mode.None; btn.navigation = nav;
            btn.onClick.AddListener(() => { if (!GameInputState.IsModalOpen) onClick(); });
            bool hasNote = !string.IsNullOrEmpty(note);
            MakeText(img.transform, "Label", text, 28, new Vector2(0.05f, hasNote ? 0.42f : 0.06f), new Vector2(0.95f, 0.94f));
            if (hasNote)
            {
                var n = MakeText(img.transform, "Note", note, 20, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.44f));
                n.color = GameUI.Muted;
            }
            return btn;
        }

        private void SetStatus(string text) => status.text = FuriganaProcessor.Process(text ?? string.Empty);

        private void MarkWrong(Image img, string hint)
        {
            ClearWrong();
            WrongCount++;
            AudioManager.Instance?.PlayUI(AudioKeys.UI.PanelClose);
            if (img != null)
            {
                lastWrong = img;
                lastWrongColor = img.color;
                img.color = WrongColor;
            }
            SetStatus(!string.IsNullOrEmpty(hint) ? hint : Card.Wrong);
        }

        private void ClearWrong()
        {
            if (lastWrong != null) lastWrong.color = lastWrongColor;
            lastWrong = null;
        }

        private void Finish()
        {
            ClearWrong();
            Done = true;
            SetStatus(string.Empty);
            AudioManager.Instance?.PlaySFX(AudioKeys.SFX.SamplePickup);
        }

        private string PickPrompt => LocalizedOr("card.common.pick", "カードをえらんでね");

        private static string LocalizedOr(string key, string fallback)
        {
            var lm = LocalizationManager.Instance;
            return lm != null && lm.HasText(key) ? lm.GetText(key) : fallback;
        }

        private static (Vector2 min, Vector2 max) Cell(int index, int count, float y0, float y1, float gap = 0.015f)
        {
            float w = 1f / Mathf.Max(1, count);
            return (new Vector2(index * w + gap, y0), new Vector2((index + 1) * w - gap, y1));
        }

        // ----- order：正しい順にタップ -----

        private void BuildOrder()
        {
            int n = Card.Items.Count;
            for (int s = 0; s < n; s++)
            {
                var (min, max) = Cell(s, n, 0.56f, 1f);
                var slot = GameUI.Box(content, $"Slot{s}", SlotColor, min, max);
                slotImages.Add(slot);
                var num = MakeText(slot.transform, "Number", (s + 1).ToString(), 26, new Vector2(0f, 0.72f), new Vector2(1f, 0.98f));
                num.color = GameUI.Muted;
                slotLabels.Add(MakeText(slot.transform, "Label", string.Empty, 24, new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.74f)));
                if (s < n - 1)
                {
                    var arrow = MakeText(content, $"Arrow{s}", "→", 30,
                        new Vector2(max.x - 0.01f, 0.70f), new Vector2(max.x + 0.04f, 0.86f));
                    arrow.color = GameUI.Muted;
                }
            }
            for (int i = 0; i < n; i++)
            {
                int idx = i;
                var item = Card.Items[i];
                var (min, max) = Cell(i, n, 0f, 0.48f);
                itemButtons.Add(CardButton(content, $"Item{i}", item.Text, item.Note, min, max, () => OnOrderTap(idx)));
            }
            SetStatus(LocalizedOr("card.common.order", "1ばんめから じゅんに タップしよう"));
        }

        private void OnOrderTap(int idx)
        {
            if (Done) return;
            var item = Card.Items[idx];
            var btn = itemButtons[idx];
            if (item.Order != nextOrder)
            {
                MarkWrong((Image)btn.targetGraphic, item.Hint);
                return;
            }
            ClearWrong();
            AudioManager.Instance?.PlayUI(AudioKeys.UI.Click);
            int slot = nextOrder - 1;
            if (slot < slotLabels.Count)
            {
                slotLabels[slot].text = FuriganaProcessor.Process(item.Text);
                slotImages[slot].color = PlacedColor;
            }
            btn.interactable = false;
            btn.gameObject.SetActive(false);
            nextOrder++;
            if (nextOrder > Card.Items.Count) Finish();
            else SetStatus(string.Empty);
        }

        // ----- sort：カード → 箱 -----

        private void BuildSort()
        {
            int nb = Card.Bins.Count;
            for (int b = 0; b < nb; b++)
            {
                int bi = b;
                var (min, max) = Cell(b, nb, 0.52f, 1f);
                var binImg = GameUI.Box(content, $"Bin{b}", SlotColor, min, max);
                var btn = binImg.gameObject.AddComponent<Button>();
                btn.targetGraphic = binImg;
                var nav = btn.navigation; nav.mode = Navigation.Mode.None; btn.navigation = nav;
                btn.onClick.AddListener(() => { if (!GameInputState.IsModalOpen) OnBinTap(bi); });
                slotImages.Add(binImg);
                var header = MakeText(binImg.transform, "Header", Card.Bins[b].Text, 26, new Vector2(0.04f, 0.70f), new Vector2(0.96f, 0.98f));
                header.color = GameUI.Accent;
                var list = GameUI.Rect(binImg.transform, "List", new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.68f));
                var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 4f;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                binLists.Add(list);
            }
            int n = Card.Items.Count;
            for (int i = 0; i < n; i++)
            {
                int idx = i;
                var item = Card.Items[i];
                var (min, max) = Cell(i, n, 0f, 0.44f);
                itemButtons.Add(CardButton(content, $"Item{i}", item.Text, item.Note, min, max, () => OnItemSelect(idx)));
            }
            SetStatus(LocalizedOr("card.common.sort", "カードをえらんで、入れる箱をタップしよう"));
        }

        private void OnItemSelect(int idx)
        {
            if (Done) return;
            ClearWrong();
            if (selected >= 0 && selected < itemButtons.Count && itemButtons[selected] != null)
                ((Image)itemButtons[selected].targetGraphic).color = CardColor;
            selected = idx;
            ((Image)itemButtons[idx].targetGraphic).color = SelectedColor;
            AudioManager.Instance?.PlayUI(AudioKeys.UI.Click);
            SetStatus(LocalizedOr("card.common.choose_bin", "どの箱に入る？"));
        }

        private void OnBinTap(int bin)
        {
            if (Done) return;
            if (selected < 0)
            {
                SetStatus(PickPrompt);
                return;
            }
            var item = Card.Items[selected];
            var btn = itemButtons[selected];
            if (item.Bin != Card.Bins[bin].Id)
            {
                MarkWrong(slotImages[bin], item.Hint);
                ((Image)btn.targetGraphic).color = SelectedColor;
                return;
            }
            ClearWrong();
            AudioManager.Instance?.PlayUI(AudioKeys.UI.Click);
            var chip = GameUI.Box(binLists[bin], $"Placed{selected}", PlacedColor, Vector2.zero, Vector2.one);
            var le = chip.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 40f;
            le.minHeight = 34f;
            MakeText(chip.transform, "Label", item.Text, 22, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f));
            btn.interactable = false;
            btn.gameObject.SetActive(false);
            selected = -1;
            placedCount++;
            if (placedCount >= Card.Items.Count) Finish();
            else SetStatus(PickPrompt);
        }

        // ----- choose：証拠を見て結論をえらぶ -----

        private void BuildChoose()
        {
            int ne = Card.Evidence.Count;
            bool hasEvidence = ne > 0;
            for (int e = 0; e < ne; e++)
            {
                var (min, max) = Cell(e, ne, 0.74f, 1f, 0.008f);
                var chip = GameUI.Box(content, $"Evidence{e}", SlotColor, min, max);
                var outline = chip.gameObject.AddComponent<Outline>();
                outline.effectColor = GameUI.Accent;
                outline.effectDistance = new Vector2(2f, -2f);
                MakeText(chip.transform, "Label", Card.Evidence[e], 22, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));
            }
            int n = Card.Items.Count;
            float top = hasEvidence ? 0.70f : 1f;
            float h = top / n;
            for (int i = 0; i < n; i++)
            {
                int idx = i;
                var item = Card.Items[i];
                var btn = CardButton(content, $"Item{i}", item.Text, item.Note,
                    new Vector2(0.04f, top - (i + 1) * h + 0.012f), new Vector2(0.96f, top - i * h - 0.012f), () => OnChoose(idx));
                itemButtons.Add(btn);
            }
            SetStatus(LocalizedOr("card.common.choose", "いちばん合（あ）うものを、ひとつえらぼう"));
        }

        private void OnChoose(int idx)
        {
            if (Done || idx < 0) return;
            var btn = itemButtons[idx];
            if (!Card.Items[idx].Correct)
            {
                MarkWrong((Image)btn.targetGraphic, Card.Items[idx].Hint);
                btn.interactable = false;
                return;
            }
            ((Image)btn.targetGraphic).color = PlacedColor;
            foreach (var other in itemButtons) other.interactable = false;
            Finish();
        }

        // ----- columns：柱状図の目的の層をタップ → つなぐ -----

        private void BuildColumns()
        {
            int nc = Card.Columns.Count;
            for (int c = 0; c < nc; c++)
            {
                var column = Card.Columns[c];
                var (min, max) = Cell(c, nc, 0f, 1f, 0.07f);
                var colRt = GameUI.Rect(content, $"Column{c}", min, max);
                var label = MakeText(colRt, "Label", column.Label, 26, new Vector2(0f, 0.88f), new Vector2(1f, 1f));
                label.color = GameUI.Accent;
                float total = 0f;
                foreach (var l in column.Layers) total += l.Size;
                float top = 0.86f, height = 0.86f;
                for (int k = 0; k < column.Layers.Count; k++)
                {
                    var layer = column.Layers[k];
                    float h = height * layer.Size / Mathf.Max(0.001f, total);
                    var img = GameUI.Box(colRt, $"Layer{k}", layer.Color, new Vector2(0f, top - h), new Vector2(1f, top));
                    top -= h;
                    var outline = img.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
                    outline.effectDistance = new Vector2(1.5f, -1.5f);
                    var btn = img.gameObject.AddComponent<Button>();
                    btn.targetGraphic = img;
                    var nav = btn.navigation; nav.mode = Navigation.Mode.None; btn.navigation = nav;
                    var t = MakeText(img.transform, "Label", layer.Text, 22, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f));
                    t.color = layer.Color.grayscale > 0.6f ? new Color(0.1f, 0.1f, 0.1f) : Color.white;
                    if (layer.Target) targetsLeft++;
                    var captured = layer;
                    layerOf[btn] = layer;
                    btn.onClick.AddListener(() => { if (!GameInputState.IsModalOpen) OnLayerTap(btn, img, captured); });
                }
            }
            SetStatus(LocalizedOr("card.common.columns", "それぞれの柱（はしら）で、目（め）じるしの層（そう）をタップしよう"));
        }

        private void OnLayerTap(Button btn, Image img, KnowledgeCard.Layer layer)
        {
            if (Done) return;
            if (!layer.Target)
            {
                MarkWrong(img, layer.Hint);
                return;
            }
            ClearWrong();
            AudioManager.Instance?.PlayUI(AudioKeys.UI.Click);
            btn.interactable = false;
            var colors = btn.colors; colors.disabledColor = Color.white; btn.colors = colors;
            var outline = img.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = GameUI.Accent;
                outline.effectDistance = new Vector2(4f, -4f);
            }
            tappedTargets.Add((RectTransform)img.transform);
            targetsLeft--;
            if (targetsLeft > 0)
            {
                SetStatus(string.Empty);
                return;
            }
            ConnectTargets();
            Finish();
        }

        /// <summary>タップした層の中心どうしを線でつなぎ、かたむきが目で見えるようにする。</summary>
        private void ConnectTargets()
        {
            if (tappedTargets.Count < 2) return;
            Canvas.ForceUpdateCanvases();
            var points = new List<Vector2>();
            foreach (var rt in tappedTargets)
            {
                if (rt == null) continue;
                Vector3 world = rt.TransformPoint(rt.rect.center);
                points.Add(content.InverseTransformPoint(world));
            }
            points.Sort((a, b) => a.x.CompareTo(b.x));
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 a = points[i], b = points[i + 1];
                var line = new GameObject($"KeyLine{i}", typeof(RectTransform));
                line.transform.SetParent(content, false);
                var rt = (RectTransform)line.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                Vector2 centerOffset = content.rect.center;
                rt.anchoredPosition = (a + b) * 0.5f - centerOffset;
                rt.sizeDelta = new Vector2(Vector2.Distance(a, b), 6f);
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
                var img = line.AddComponent<Image>();
                img.color = new Color(1f, 0.85f, 0.25f, 0.95f);
                img.raycastTarget = false;
            }
        }
    }
}
