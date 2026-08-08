using System.Text.RegularExpressions;

namespace PlayGround.Client.Localization
{
    /// <summary>한국어 조사 모디파이어 해석 — 리소스의 `{0:이/가}`를 앞 값의 받침 유무로 골라 `{0}이`/`{0}가`로 바꾼다.
    /// **문법은 리소스가 소유한다** — 조사가 없는 언어(일본어·영어)는 모디파이어를 쓰지 않으면 그만이라,
    /// 컴포넌트 코드에는 문화권 분기가 생기지 않는다. 표기 순서는 `{n:받침있음/받침없음}` (이/가, 은/는, 을/를, 과/와, 으로/로).</summary>
    public static partial class KoreanParticle
    {
        [GeneratedRegex(@"\{(\d+):([^/}]+)/([^}]+)\}")]
        private static partial Regex ModifierPattern();

        public static string Resolve(string template, object[] args)
        {
            if (template.IndexOf(":", StringComparison.Ordinal) < 0)
            {
                return template; // 모디파이어 없는 일반 템플릿 — 그대로
            }

            return ModifierPattern().Replace(template, match =>
            {
                int index = int.Parse(match.Groups[1].Value);
                string withFinal = match.Groups[2].Value;      // 받침 있을 때 (이/은/을/과/으로)
                string withoutFinal = match.Groups[3].Value;   // 받침 없을 때 (가/는/를/와/로)

                if (index >= args.Length)
                {
                    return match.Value; // 인자 부족 — 원문 유지(개발 중 발견 목적)
                }

                string value = args[index]?.ToString() ?? string.Empty;

                // 으로/로 계열만 ㄹ 받침을 '받침 없음'으로 본다 ('이메일로'가 맞고 '이메일으로'는 틀리다).
                bool hasFinal = withFinal.StartsWith("으", StringComparison.Ordinal)
                    ? HasFinalConsonantExceptRieul(value)
                    : HasFinalConsonant(value);

                return $"{{{index}}}" + (hasFinal ? withFinal : withoutFinal);
            });
        }

        /// <summary>마지막 글자의 받침(종성) 유무. 한글은 유니코드 규칙, 숫자·영문은 한국어 발음 기준.</summary>
        private static bool HasFinalConsonant(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            char last = value[^1];

            // 한글 음절 (가~힣) — (코드 - '가') % 28 != 0 이면 종성 있음
            if (last is >= '가' and <= '힣')
            {
                return (last - '가') % 28 != 0;
            }

            // 숫자 — 한국어 발음: 영·일·삼·육·칠·팔은 받침, 이·사·오·구는 없음
            if (last is >= '0' and <= '9')
            {
                return last is '0' or '1' or '3' or '6' or '7' or '8';
            }

            char upper = char.ToUpperInvariant(last);
            if (upper is >= 'A' and <= 'Z')
            {
                return upper is 'L' or 'M' or 'N' or 'R';
            }

            return false; // 기호·기타 — 받침 없음으로 처리
        }

        private static bool HasFinalConsonantExceptRieul(string value)
        {
            if (!HasFinalConsonant(value))
            {
                return false;
            }

            char last = value[^1];
            const int Rieul = 8; // 한글 종성 인덱스 (ㄹ)
            return !(last is >= '가' and <= '힣') || (last - '가') % 28 != Rieul;
        }
    }
}
