using System.Text;

namespace PlayGround.Shared.Logging
{
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

            // MEL 템플릿에서 장식용 중괄호는 {{ }}로 이스케이프해야 한다 — 안 하면 렌더 시 FormatException
            builder.Append(template ? " {{ " : " { ");
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

            builder.Append(template ? " }}" : " }");
            return builder.ToString();
        }
    }
}
