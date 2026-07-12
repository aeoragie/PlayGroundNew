namespace Generator.Database.Generators
{
    /// <summary>
    /// 생성 코드 네임스페이스 조립. RootNamespace가 비면 {db}.{suffix}(구 동작),
    /// 있으면 {root}.{db}.{suffix}.
    /// 예: ("PlayGround.Persistence.Database.Generated", "Soccer", "Entities")
    ///     → "PlayGround.Persistence.Database.Generated.Soccer.Entities"
    /// </summary>
    public static class NamespaceBuilder
    {
        public static string Build(string rootNamespace, string database, string suffix)
        {
            return string.IsNullOrWhiteSpace(rootNamespace)
                ? $"{database}.{suffix}"
                : $"{rootNamespace}.{database}.{suffix}";
        }
    }
}
