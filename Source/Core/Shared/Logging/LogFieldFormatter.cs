using System.Text;

namespace PlayGround.Shared.Logging
{
    /// <summary>로그 메시지 형태의 단일 정의 — <c>문장. { Key:Value }</c>. NLog와 MEL이 함께 쓴다.</summary>
    public static class LogFieldFormatter
    {
        public static string BuildRendered(string message, (string Key, object? Value)[] fields) =>
            Build(message, fields, template: false);

        public static string BuildTemplate(string message, (string Key, object? Value)[] fields) =>
            Build(message, fields, template: true);

        private static string Build(string message, (string Key, object? Value)[] fields, bool template)
        {
            if (fields.Length == 0)
            {
                return message;
            }

            var builder = new StringBuilder(message);
            if (!message.EndsWith('.'))
            {
                builder.Append('.');
            }

            builder.Append(" { ");
            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(fields[i].Key).Append(':');
                if (template)
                {
                    builder.Append('{').Append(fields[i].Key).Append('}');
                }
                else
                {
                    builder.Append(fields[i].Value?.ToString() ?? "null");
                }
            }

            builder.Append(" }");
            return builder.ToString();
        }
    }
}
