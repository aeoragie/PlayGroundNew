using System.Linq;

namespace PlayGround.Client.Components.Shared.Forms
{
    /// <summary>입력 자동 포맷 (Design.FormPatterns 컨트롤별). 숫자만 남기고 구분자를 넣는다.</summary>
    public enum FormInputFormat
    {
        None,
        /// <summary>생년월일 — 20110314 → 2011. 03. 14 (inputmode numeric)</summary>
        BirthDate,
        /// <summary>전화 — 01012345678 → 010-1234-5678 (inputmode tel)</summary>
        PhoneNumber,
        /// <summary>연·월 — 202403 → 2024. 03 (inputmode numeric).
        /// 커리어 기간처럼 과거 연도를 고르는 자리는 캘린더를 쓰지 않는다(Design.DatePicker 3분법).</summary>
        YearMonth,
    }

    public static class FormInputFormatter
    {
        /// <summary>입력값을 형식에 맞게 정리. None이면 원본 그대로.</summary>
        public static string Apply(string? value, FormInputFormat format)
        {
            if (string.IsNullOrEmpty(value) || format == FormInputFormat.None)
            {
                return value ?? string.Empty;
            }

            string digits = new(value.Where(char.IsAsciiDigit).ToArray());
            return format switch
            {
                FormInputFormat.BirthDate => FormatBirthDate(digits),
                FormInputFormat.PhoneNumber => FormatPhone(digits),
                FormInputFormat.YearMonth => FormatYearMonth(digits),
                _ => value,
            };
        }

        /// <summary>HTML inputmode 속성값 — 모바일 숫자 키패드 유도.</summary>
        public static string? InputModeOf(FormInputFormat format) => format switch
        {
            FormInputFormat.BirthDate => "numeric",
            FormInputFormat.PhoneNumber => "tel",
            FormInputFormat.YearMonth => "numeric",
            _ => null,
        };

        // 2024. 03 — 6자리까지만 (연 4 + 월 2)
        private static string FormatYearMonth(string digits)
        {
            if (digits.Length > 6)
            {
                digits = digits[..6];
            }

            return digits.Length <= 4 ? digits : $"{digits[..4]}. {digits[4..]}";
        }

        // 2011. 03. 14 — 자리수만큼만 채운다(입력 중 잘림 없음)
        private static string FormatBirthDate(string digits)
        {
            if (digits.Length > 8)
            {
                digits = digits[..8];
            }

            if (digits.Length <= 4)
            {
                return digits;
            }

            if (digits.Length <= 6)
            {
                return $"{digits[..4]}. {digits[4..]}";
            }

            return $"{digits[..4]}. {digits[4..6]}. {digits[6..]}";
        }

        // 010-1234-5678 / 02-123-4567 등 — 서울(02)만 2자리 지역번호
        private static string FormatPhone(string digits)
        {
            if (digits.Length > 11)
            {
                digits = digits[..11];
            }

            bool isSeoul = digits.StartsWith("02");
            int head = isSeoul ? 2 : 3;

            if (digits.Length <= head)
            {
                return digits;
            }

            // 중간 블록은 전체 길이에 따라 3자리 또는 4자리
            int middle = digits.Length >= (isSeoul ? 10 : 11) ? 4 : 3;
            if (digits.Length <= head + middle)
            {
                return $"{digits[..head]}-{digits[head..]}";
            }

            return $"{digits[..head]}-{digits.Substring(head, middle)}-{digits[(head + middle)..]}";
        }
    }
}
