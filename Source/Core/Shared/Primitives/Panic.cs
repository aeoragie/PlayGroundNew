using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace PlayGround.Shared.Primitives
{
    public static class Panic
    {
        [DoesNotReturn]
        public static void Fail(string message)
        {
            Debug.Assert(false, message);
            throw new InvalidOperationException(message);
        }

        [DoesNotReturn]
        public static T Fail<T>(string message)
        {
            Debug.Assert(false, message);
            throw new InvalidOperationException(message);
        }
    }
}
