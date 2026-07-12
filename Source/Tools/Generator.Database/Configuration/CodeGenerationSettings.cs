namespace Generator.Database.Configuration
{
    public class CodeGenerationSettings
    {
        /// <summary>생성물 출력 루트 (CommonPath) — Persistence의 Generated 폴더</summary>
        public string CommonPath { get; set; } = string.Empty;

        /// <summary>생성 코드 네임스페이스 접두사 (비면 {DB}.Entities 형태). 예: PlayGround.Persistence.Database.Generated</summary>
        public string RootNamespace { get; set; } = string.Empty;

        public Dictionary<string, DatabaseOptions> Databases { get; set; } = new();
    }
}
