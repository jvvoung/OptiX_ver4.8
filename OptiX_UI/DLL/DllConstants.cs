using System;

namespace OptiX.DLL
{
    /// <summary>
    /// DLL 관련 상수 정의 클래스
    /// 모든 매직 넘버를 여기에 정의하여 유지보수성 향상
    /// </summary>
    public static class DllConstants
    {
        #region Array Size Constants
        
        /// <summary>
        /// WAD 각도 개수 (0도, 15도, 30도, 45도, 60도, A, B)
        /// </summary>
        public const int MAX_WAD_COUNT = 7;
        
        /// <summary>
        /// OPTIC 패턴 개수 (W, R, G, B, WG, WG2~WG13)
        /// </summary>
        public const int MAX_PATTERN_COUNT = 17;
        
        /// <summary>
        /// IPVS 포인트 개수
        /// </summary>
        public const int MAX_POINT_COUNT = 10;
        
        /// <summary>
        /// OPTIC 데이터 배열 크기 (MAX_WAD_COUNT * MAX_PATTERN_COUNT)
        /// </summary>
        public const int OPTIC_DATA_SIZE = MAX_WAD_COUNT * MAX_PATTERN_COUNT; // 119
        
        /// <summary>
        /// IPVS 데이터 배열 크기 (MAX_WAD_COUNT * MAX_POINT_COUNT)
        /// </summary>
        public const int IPVS_DATA_SIZE = MAX_WAD_COUNT * MAX_POINT_COUNT; // 70
        
        /// <summary>
        /// 문자열 버퍼 크기 (CELL_ID, INNER_ID)
        /// </summary>
        public const int STRING_BUFFER_SIZE = 256;
        
        /// <summary>
        /// RGB 채널 개수
        /// </summary>
        public const int RGB_CHANNEL_COUNT = 3;
        
        #endregion
        
        #region Judgment Constants - OPTIC
        
        /// <summary>
        /// OPTIC OK 판정 임계값 (OK 개수가 이 값 이상이면 OK)
        /// </summary>
        public const int OPTIC_OK_THRESHOLD = 100;
        
        /// <summary>
        /// OPTIC PTN 판정 임계값 (PTN 개수가 이 값 이상이면 PTN)
        /// </summary>
        public const int OPTIC_PTN_THRESHOLD = 13;
        
        #endregion
        
        #region Judgment Constants - IPVS
        
        /// <summary>
        /// IPVS OK 판정 임계값 (OK 개수가 이 값 이상이면 OK)
        /// </summary>
        public const int IPVS_OK_THRESHOLD = 60;
        
        /// <summary>
        /// IPVS PTN 판정 임계값 (PTN 개수가 이 값 이상이면 PTN)
        /// </summary>
        public const int IPVS_PTN_THRESHOLD = 10;
        
        #endregion
        
        #region Result Flag Constants
        
        /// <summary>
        /// 판정 결과: OK
        /// </summary>
        public const int RESULT_OK = 0;
        
        /// <summary>
        /// 판정 결과: NG (Reject)
        /// </summary>
        public const int RESULT_NG = 1;
        
        /// <summary>
        /// 판정 결과: PTN (Pattern)
        /// </summary>
        public const int RESULT_PTN = 2;
        
        #endregion
        
        #region Measurement Range Constants
        
        /// <summary>
        /// X 좌표 최소값 (CIE 1931)
        /// </summary>
        public const float X_MIN = 0.0f;
        
        /// <summary>
        /// X 좌표 최대값 (CIE 1931)
        /// </summary>
        public const float X_MAX = 1.0f;
        
        /// <summary>
        /// Y 좌표 최소값 (CIE 1931)
        /// </summary>
        public const float Y_MIN = 0.0f;
        
        /// <summary>
        /// Y 좌표 최대값 (CIE 1931)
        /// </summary>
        public const float Y_MAX = 1.0f;
        
        /// <summary>
        /// 휘도 최소값 (cd/m²)
        /// </summary>
        public const float LUMINANCE_MIN = 0.0f;
        
        /// <summary>
        /// 휘도 최대값 (cd/m²)
        /// </summary>
        public const float LUMINANCE_MAX = 1000.0f;
        
        /// <summary>
        /// 전류 최소값 (mA)
        /// </summary>
        public const float CURRENT_MIN = 0.0f;
        
        /// <summary>
        /// 전류 최대값 (mA)
        /// </summary>
        public const float CURRENT_MAX = 100.0f;
        
        /// <summary>
        /// 효율 최소값 (%)
        /// </summary>
        public const float EFFICIENCY_MIN = 0.0f;
        
        /// <summary>
        /// 효율 최대값 (%)
        /// </summary>
        public const float EFFICIENCY_MAX = 100.0f;
        
        #endregion
        
        #region UI Constants
        
        /// <summary>
        /// 그래프 데이터 포인트 최대 개수 (FIFO 방식으로 유지)
        /// </summary>
        public const int MAX_GRAPH_DATA_POINTS = 300;
        
        #endregion
        
        #region Time Management Constants
        
        /// <summary>
        /// Zone 종료 시간 키 오프셋 (시작/종료 시간 구분용)
        /// 예: Zone 1 시작 = 1, Zone 1 종료 = 1001
        /// </summary>
        public const int ZONE_END_TIME_OFFSET = 1000;
        
        /// <summary>
        /// Zone 완료 대기 시간 (ms) - Race Condition 방지
        /// </summary>
        public const int ZONE_COMPLETION_DELAY_MS = 50;
        
        /// <summary>
        /// 로그 생성 간격 대기 시간 (ms)
        /// </summary>
        public const int LOG_GENERATION_DELAY_MS = 10;
        
        #endregion
        
        #region Sequence Constants
        
        /// <summary>
        /// 기본 IPVS 포인트 개수 (total_point)
        /// </summary>
        public const int DEFAULT_IPVS_TOTAL_POINT = 5;
        
        /// <summary>
        /// 기본 현재 포인트 (cur_point)
        /// </summary>
        public const int DEFAULT_CURRENT_POINT = 0;
        
        #endregion
    }
}

