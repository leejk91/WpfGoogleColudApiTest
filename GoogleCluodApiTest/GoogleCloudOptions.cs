namespace GoogleCluodApiTest
{
    public class GoogleCloudOptions
    {
        /// <summary>
        /// 서비스계정키 JSON 경로
        /// </summary>
        public string ServiceAccountKeyPath { get; set; } = "";
        /// <summary>
        /// 프로젝트 ID
        /// </summary>
        public string ProjectId { get; set; } = "";
        /// <summary>
        /// 리전(Region) 설정, 기본값은 "global"
        /// </summary>
        public string Location { get; set; } = "global";
        public string DocumentLocation { get; set; } = "us"; // Document AI 용 리전
        public string DocumentProcessorId { get; set; } = ""; // Document AI 용 프로세서 ID
    }
}
