using System;
using System.Text;

namespace PlayGround.Shared.Text
{
    /// <summary>
    /// 이름 → URL 슬러그 변환. 한글은 국어의 로마자 표기법(음절 단위) 기반으로 로마자화하고,
    /// 영문·숫자는 소문자로, 그 외 문자는 구분자로 처리해 하이픈으로 연결한다.
    /// 중복 방지 접미사(-2 등)는 저장 계층에서 부여한다.
    /// </summary>
    public static class SlugGenerator
    {
        private const int HangulBase = 0xAC00;
        private const int HangulEnd = 0xD7A3;
        private const int MedialCount = 21;
        private const int FinalCount = 28;

        private static readonly string[] Initials =
            { "g", "kk", "n", "d", "tt", "r", "m", "b", "pp", "s", "ss", "", "j", "jj", "ch", "k", "t", "p", "h" };

        private static readonly string[] Medials =
            { "a", "ae", "ya", "yae", "eo", "e", "yeo", "ye", "o", "wa", "wae", "oe", "yo", "u", "wo", "we", "wi", "yu", "eu", "ui", "i" };

        private static readonly string[] Finals =
            { "", "k", "k", "k", "n", "n", "n", "t", "l", "k", "m", "l", "l", "l", "p", "l", "m", "p", "p", "t", "t", "ng", "t", "t", "k", "t", "p", "t" };

        public static string Generate(string? name, int maxLength = 80)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            StringBuilder buffer = new(name.Length * 3);
            foreach (char c in name.Trim())
            {
                if (c >= HangulBase && c <= HangulEnd)
                {
                    int index = c - HangulBase;
                    int initial = index / (MedialCount * FinalCount);
                    int medial = index % (MedialCount * FinalCount) / FinalCount;
                    int final = index % FinalCount;
                    buffer.Append(Initials[initial]);
                    buffer.Append(Medials[medial]);
                    buffer.Append(Finals[final]);
                }
                else if (c is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
                {
                    buffer.Append(c);
                }
                else if (c is >= 'A' and <= 'Z')
                {
                    buffer.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    buffer.Append(' ');
                }
            }

            string[] parts = buffer.ToString()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string slug = string.Join("-", parts);

            if (slug.Length > maxLength)
            {
                slug = slug[..maxLength].TrimEnd('-');
            }
            return slug;
        }

        /// <summary>
        /// 기준 슬러그가 예약어이거나 이미 사용 중이면 접미사(-2, -3…)를 붙여 고유한 값을 만든다.
        /// <paramref name="isUnavailable"/>는 예약어 집합 + 저장소 존재 여부를 합쳐 판단하는 조건이다.
        /// 빈 기준 슬러그는 <paramref name="fallback"/>을 사용한다.
        /// </summary>
        public static string MakeUnique(string? baseSlug, Func<string, bool> isUnavailable, string fallback = "item")
        {
            if (isUnavailable is null)
            {
                throw new ArgumentNullException(nameof(isUnavailable));
            }

            string candidate = string.IsNullOrWhiteSpace(baseSlug) ? fallback : baseSlug;
            if (!isUnavailable(candidate))
            {
                return candidate;
            }

            for (int suffix = 2; suffix <= 10000; suffix++)
            {
                string next = $"{candidate}-{suffix}";
                if (!isUnavailable(next))
                {
                    return next;
                }
            }

            throw new InvalidOperationException($"Could not resolve a unique slug for '{candidate}'");
        }
    }
}
