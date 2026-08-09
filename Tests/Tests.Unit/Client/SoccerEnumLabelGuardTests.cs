using System.Reflection;
using FluentAssertions;
using PlayGround.Client.Models;
using Xunit;

namespace PlayGround.Tests.Unit.Client
{
    /// <summary>enum → 표기 전이 지점(SoccerDomainEnumLabels)의 전수 가드.
    /// enum에 멤버를 추가하고 라벨을 빠뜨리면 여기서 잡힌다 — switch의 `_` 폴백 때문에
    /// 컴파일러는 누락을 모른다. 통과형(ToString 반환) 라벨도 같은 계약으로 묶는다.</summary>
    [Collection(LocalizationCollection.Name)]
    public class SoccerEnumLabelGuardTests
    {
        private static IEnumerable<MethodInfo> LabelMethods()
        {
            return typeof(SoccerDomainEnumLabels)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.GetParameters() is { Length: 1 } p
                            && p[0].ParameterType.IsEnum
                            && m.ReturnType == typeof(string));
        }

        public static TheoryData<string, string> AllLabelCalls()
        {
            var data = new TheoryData<string, string>();
            foreach (MethodInfo method in LabelMethods())
            {
                foreach (object member in Enum.GetValues(method.GetParameters()[0].ParameterType))
                {
                    data.Add(method.Name + ":" + method.GetParameters()[0].ParameterType.Name, member.ToString()!);
                }
            }

            return data;
        }

        [Fact]
        public void LabelMethods_AreDiscovered()
        {
            // 리플렉션 대상이 비면 이 가드 전체가 헛돈다 — 최소 개수로 발견 자체를 고정
            LabelMethods().Count().Should().BeGreaterThanOrEqualTo(10);
        }

        [Theory]
        [MemberData(nameof(AllLabelCalls))]
        public void EveryMember_HasNonEmptyLabel(string methodKey, string memberName)
        {
            string[] parts = methodKey.Split(':');
            MethodInfo method = LabelMethods().Single(m => m.Name == parts[0] && m.GetParameters()[0].ParameterType.Name == parts[1]);
            Type enumType = method.GetParameters()[0].ParameterType;
            object member = Enum.Parse(enumType, memberName);

            object? result = method.Invoke(null, new[] { member });

            bool isUnknown = Convert.ToInt32(member) == 0;
            if (isUnknown)
            {
                // Unknown은 null(통과형) 또는 폴백 라벨 — 예외 없이 돌아오기만 하면 된다
                return;
            }

            result.Should().BeOfType<string>().Which.Should().NotBeNullOrWhiteSpace(
                $"{enumType.Name}.{memberName} needs a label in {method.Name}");
        }
    }
}
