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
