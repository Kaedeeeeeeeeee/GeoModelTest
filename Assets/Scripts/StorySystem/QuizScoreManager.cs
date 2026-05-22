using System.Collections.Generic;

namespace StorySystem
{
    /// <summary>
    /// 单条答题结果。
    /// </summary>
    public struct QuizResult
    {
        public string QuestionId;
        public bool Correct;

        public QuizResult(string questionId, bool correct)
        {
            QuestionId = questionId;
            Correct = correct;
        }
    }

    /// <summary>
    /// 教学测验计分器（纯 C# 单例，跨场景保留一次游玩的成绩）。
    /// 对话内嵌选择题作答后调用 Record，结尾报告界面读取 Grade。
    /// </summary>
    public class QuizScoreManager
    {
        private static QuizScoreManager _instance;
        public static QuizScoreManager Instance => _instance ?? (_instance = new QuizScoreManager());

        private readonly List<QuizResult> _results = new List<QuizResult>();

        public IReadOnlyList<QuizResult> Results => _results;

        public int Total => _results.Count;

        public int CorrectCount
        {
            get
            {
                int c = 0;
                foreach (var r in _results) if (r.Correct) c++;
                return c;
            }
        }

        public float Ratio => Total == 0 ? 0f : (float)CorrectCount / Total;

        /// <summary>
        /// 记录一题结果。带非空 QuestionId 时，重复作答会覆盖旧结果（避免重播/回退重复计分）。
        /// </summary>
        public void Record(string questionId, bool isCorrect)
        {
            if (!string.IsNullOrEmpty(questionId))
            {
                for (int i = 0; i < _results.Count; i++)
                {
                    if (_results[i].QuestionId == questionId)
                    {
                        _results[i] = new QuizResult(questionId, isCorrect);
                        return;
                    }
                }
            }
            _results.Add(new QuizResult(questionId ?? string.Empty, isCorrect));
        }

        public void Reset() => _results.Clear();

        /// <summary>
        /// 评级：A (>=80%) / B (>=60%) / C，未作答时返回 "-"。
        /// </summary>
        public string Grade
        {
            get
            {
                if (Total == 0) return "-";
                float ratio = Ratio;
                if (ratio >= 0.8f) return "A";
                if (ratio >= 0.6f) return "B";
                return "C";
            }
        }
    }
}
