namespace GoogleCluodApiTest.Models
{
    /// <summary>
    /// 텍스트 음성 합성( TTS ) 요청 옵션입니다.
    /// </summary>
    public class TtsOptions
    {
        /// <summary>합성할 텍스트</summary>
        public string Text { get; set; } = "";
        /// <summary>언어 코드 (예: ko-KR)</summary>
        public string LanguageCode { get; set; } = "ko-KR";
        /// <summary>보이스 이름 (공백일 경우 공급자 기본)</summary>
        public string VoiceName { get; set; } = "ko-KR-Neural2-C"; // 가용 보이스 중 하나 예시
        /// <summary>말하기 속도 (0.25 ~ 4.0)</summary>
        public double SpeakingRate { get; set; } = 1.0; // 0.25 ~ 4.0
        /// <summary>피치 (-20.0 ~ 20.0)</summary>
        public double Pitch { get; set; } = 0.0;       // -20.0 ~ 20.0
        /// <summary>볼륨 게인 dB (-96.0 ~ 16.0)</summary>
        public double VolumeGainDb { get; set; } = 0.0;// -96.0 ~ 16.0
        /// <summary>오디오 인코딩 (예: MP3, OGG_OPUS, LINEAR16)</summary>
        public string AudioEncoding { get; set; } = "MP3";
    }
}
