using System.Globalization;
using System.Text;

namespace StorySystem
{
    /// <summary>
    /// 把作者文本里的 inline 注音 `漢字（かな）` 转成 TextMeshPro 的「头顶注音(ruby)」富文本。
    ///
    /// 规则（非破坏）：
    ///   - 仅当「一段汉字」后面紧跟「全角括号且括号内是纯假名」时，才当作注音处理；
    ///   - 旁注/嵌套非假名括号（如 あわ（二酸化炭素（にさんかたんそ）））原样保留，
    ///     其中真正的注音部分（二酸化炭素（にさんかたんそ））仍会被正确识别；
    ///   - 未匹配的任何字符都原样输出。
    ///
    /// 实现用 TMP 富文本 `<voffset><size><space>` 把读音抬到基字上方并水平居中。
    /// 下面几个常量是「看到效果后再微调」的视觉参数。
    /// </summary>
    public static class FuriganaProcessor
    {
        private const float ReadingSizePct = 55f;            // 注音相对正文字号(%)
        private const float ReadingRiseEm = 0.95f;           // 注音上移量(em)
        private const float ReadingCharEm = ReadingSizePct / 100f; // 每个假名约多宽(em)

        public static bool IsKanji(char c)
        {
            return (c >= 0x4E00 && c <= 0x9FFF)   // CJK 统一汉字
                || (c >= 0x3400 && c <= 0x4DBF)   // 扩展A
                || (c >= 0xF900 && c <= 0xFAFF)   // 兼容汉字
                || c == '々' || c == '〆' || c == 'ヶ' || c == 'ヵ';
        }

        public static bool IsKana(char c)
        {
            return (c >= 0x3040 && c <= 0x309F)   // 平假名
                || (c >= 0x30A0 && c <= 0x30FF)   // 片假名
                || c == 'ー';                      // 长音符
        }

        private static bool RangeAllKana(string s, int start, int end)
        {
            if (end <= start) return false;
            for (int i = start; i < end; i++)
            {
                if (!IsKana(s[i])) return false;
            }
            return true;
        }

        /// <summary>
        /// 语言感知入口：仅日文启用注音；其它语言原样返回
        /// （避免把中文里如「燧石（チャート）」这类括号误判为注音）。
        /// </summary>
        public static string Process(string input)
        {
            var lm = LocalizationManager.Instance;
            if (lm != null && lm.CurrentLanguage != LanguageSettings.Language.Japanese)
            {
                return input;
            }
            return ToRubyMarkup(input);
        }

        /// <summary>把 漢字（かな） 转成 TMP ruby 富文本。无注音时返回原串。</summary>
        public static string ToRubyMarkup(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            if (input.IndexOf('（') < 0) return input; // 没有全角左括号 → 肯定没有注音

            var sb = new StringBuilder(input.Length + 32);
            int i = 0;
            int n = input.Length;

            while (i < n)
            {
                char c = input[i];
                if (IsKanji(c))
                {
                    int j = i;
                    while (j < n && IsKanji(input[j])) j++; // 汉字串 [i, j)

                    if (j < n && input[j] == '（')
                    {
                        int close = input.IndexOf('）', j + 1);
                        if (close > j + 1 && RangeAllKana(input, j + 1, close))
                        {
                            string baseText = input.Substring(i, j - i);
                            string reading = input.Substring(j + 1, close - (j + 1));
                            sb.Append(BuildRuby(baseText, reading));
                            i = close + 1;
                            continue;
                        }
                    }

                    sb.Append(input, i, j - i);
                    i = j;
                }
                else
                {
                    sb.Append(c);
                    i++;
                }
            }

            return sb.ToString();
        }

        private static string BuildRuby(string baseText, string reading)
        {
            float baseW = baseText.Length;                 // 基字按 1em/字
            float readW = reading.Length * ReadingCharEm;  // 读音按 ReadingCharEm/字
            float lead = (baseW - readW) / 2f;             // 居中：读音前的留白
            float back = -(readW + lead);                  // 退回到基字起点

            var ci = CultureInfo.InvariantCulture;
            return "<space=" + lead.ToString("0.###", ci) + "em>"
                 + "<voffset=" + ReadingRiseEm.ToString("0.###", ci) + "em>"
                 + "<size=" + ReadingSizePct.ToString("0.#", ci) + "%>"
                 + reading
                 + "</size></voffset>"
                 + "<space=" + back.ToString("0.###", ci) + "em>"
                 + baseText;
        }
    }
}
