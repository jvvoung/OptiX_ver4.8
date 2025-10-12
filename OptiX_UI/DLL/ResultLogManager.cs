using System;
using System.Diagnostics;
using OptiX.Common;

namespace OptiX.DLL
{
    // 결과 로그 생성 관리 클래스
    public static class ResultLogManager
    {
        // Zone 간 로그 파일 접근 동기화용 Lock 객체
        private static readonly object _logFileLock = new object();
        
        // WAD 각도 개수 (7개: 0도, 30도, 45도, 60도, 15도, A도, B도)
        public const int WAD_COUNT = 7;
        
        // 패턴 개수 (17개: W, R, G, B, WG, WG2~WG13)
        public const int PATTERN_COUNT = 17;
        
        // Zone별 결과 로그 생성
        public static bool CreateResultLogsForZone(
            DateTime startTime, 
            DateTime endTime, 
            string cellId, 
            string innerId, 
            int zoneNumber, 
            Output output)
        {
            // Zone 간 파일 충돌 방지를 위한 Lock (여러 Zone이 동시에 같은 파일에 쓰기 방지)
            lock (_logFileLock)
            {
                try
                {
                    Debug.WriteLine($"[Zone {zoneNumber}] 로그 생성 시작 (Lock 획득)");
                
                bool allSuccess = true;
                
                // EECP 로그 생성 (OPTIC)
                try
                {
                    string createEecp = GlobalDataManager.GetValue("MTP", "CREATE_EECP", "F");
                    if (createEecp == "T")
                    {
                        var eecpLogger = OptiX.Result_LOG.OPTIC.OpticEECPLogger.Instance;
                        eecpLogger.LogEECPData(startTime, endTime, cellId, innerId, zoneNumber, output);
                        Debug.WriteLine($"[Zone {zoneNumber}] OPTIC EECP 로그 생성 완료");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Zone {zoneNumber}] OPTIC EECP 로그 오류: {ex.Message}");
                    allSuccess = false;
                }
                
                // EECP_SUMMARY 로그 생성 (OPTIC)
                try
                {
                    string createEecpSummary = GlobalDataManager.GetValue("MTP", "CREATE_EECP_SUMMARY", "F");
                    if (createEecpSummary == "T")
                    {
                        var eecpSummaryLogger = OptiX.Result_LOG.OPTIC.OpticEECPSummaryLogger.Instance;
                        string summaryData = $"Zone_{zoneNumber}_Summary_Data";
                        eecpSummaryLogger.LogEECPSummaryData(startTime, endTime, cellId, innerId, zoneNumber, summaryData);
                        Debug.WriteLine($"[Zone {zoneNumber}] OPTIC EECP_SUMMARY 로그 생성 완료");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Zone {zoneNumber}] OPTIC EECP_SUMMARY 로그 오류: {ex.Message}");
                    allSuccess = false;
                }
                
                // CIM 로그 생성 (OPTIC)
                try
                {
                    string createCim = GlobalDataManager.GetValue("MTP", "CREATE_CIM", "F");
                    if (createCim == "T")
                    {
                        var cimLogger = OptiX.Result_LOG.OPTIC.OpticCIMLogger.Instance;
                        string cimData = $"Zone_{zoneNumber}_CIM_Data";
                        cimLogger.LogCIMData(startTime, endTime, cellId, innerId, zoneNumber, cimData);
                        Debug.WriteLine($"[Zone {zoneNumber}] OPTIC CIM 로그 생성 완료");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Zone {zoneNumber}] OPTIC CIM 로그 오류: {ex.Message}");
                    allSuccess = false;
                }
                
                // VALIDATION 로그 생성 (OPTIC)
                try
                {
                    string createValidation = GlobalDataManager.GetValue("MTP", "CREATE_VALIDATION", "F");
                    if (createValidation == "T")
                    {
                        var validationLogger = OptiX.Result_LOG.OPTIC.OpticValidationLogger.Instance;
                        string validationData = $"Zone_{zoneNumber}_Validation_Data";
                        validationLogger.LogValidationData(startTime, endTime, cellId, innerId, zoneNumber, validationData);
                        Debug.WriteLine($"[Zone {zoneNumber}] OPTIC VALIDATION 로그 생성 완료");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Zone {zoneNumber}] OPTIC VALIDATION 로그 오류: {ex.Message}");
                    allSuccess = false;
                }
                
                    return allSuccess;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Zone {zoneNumber}] 로그 생성 전체 오류: {ex.Message}");
                    return false;
                }
            } // lock 종료
        }
        
        
        /// <summary>
        /// IPVS Zone별 결과 로그 생성
        /// </summary>
        public static bool CreateIPVSResultLogsForZone(
            DateTime startTime, 
            DateTime endTime, 
            string cellId, 
            string innerId, 
            int zoneNumber, 
            Output output)
        {
            lock (_logFileLock)
            {
                try
                {
                    Debug.WriteLine($"[IPVS Zone {zoneNumber}] 로그 생성 시작 (Lock 획득)");
                
                    bool allSuccess = true;
                    
                    // EECP 로그 생성 (IPVS) - Zone별 1회 (모든 Point 데이터 포함)
                    try
                    {
                        string createEecp = GlobalDataManager.GetValue("IPVS", "CREATE_EECP", "F");
                        if (createEecp == "T")
                        {
                            var eecpLogger = OptiX.Result_LOG.IPVS.IPVSEECPLogger.Instance;
                            eecpLogger.LogEECPData(startTime, endTime, cellId, innerId, zoneNumber, output);
                            Debug.WriteLine($"[IPVS Zone {zoneNumber}] EECP 로그 생성 완료 (모든 Point 데이터 포함)");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[IPVS Zone {zoneNumber}] EECP 로그 오류: {ex.Message}");
                        allSuccess = false;
                    }
                    
                    // EECP_SUMMARY 로그 생성 (IPVS) - Zone별 1회
                    try
                    {
                        string createEecpSummary = GlobalDataManager.GetValue("IPVS", "CREATE_EECP_SUMMARY", "F");
                        if (createEecpSummary == "T")
                        {
                            var eecpSummaryLogger = OptiX.Result_LOG.IPVS.IPVSEECPSummaryLogger.Instance;
                            string summaryData = $"Zone_{zoneNumber}_Summary_Data";
                            eecpSummaryLogger.LogEECPSummaryData(startTime, endTime, cellId, innerId, zoneNumber, summaryData);
                            Debug.WriteLine($"[IPVS Zone {zoneNumber}] EECP_SUMMARY 로그 생성 완료");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[IPVS Zone {zoneNumber}] EECP_SUMMARY 로그 오류: {ex.Message}");
                        allSuccess = false;
                    }
                    
                    // CIM 로그 생성 (IPVS)
                    try
                    {
                        string createCim = GlobalDataManager.GetValue("IPVS", "CREATE_CIM", "F");
                        if (createCim == "T")
                        {
                            var cimLogger = OptiX.Result_LOG.IPVS.IPVSCIMLogger.Instance;
                            string cimData = $"Zone_{zoneNumber}_CIM_Data";
                            cimLogger.LogCIMData(startTime, endTime, cellId, innerId, zoneNumber, cimData);
                            Debug.WriteLine($"[IPVS Zone {zoneNumber}] CIM 로그 생성 완료");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[IPVS Zone {zoneNumber}] CIM 로그 오류: {ex.Message}");
                        allSuccess = false;
                    }
                    
                    // VALIDATION 로그 생성 (IPVS)
                    try
                    {
                        string createValidation = GlobalDataManager.GetValue("IPVS", "CREATE_VALIDATION", "F");
                        if (createValidation == "T")
                        {
                            var validationLogger = OptiX.Result_LOG.IPVS.IPVSValidationLogger.Instance;
                            string validationData = $"Zone_{zoneNumber}_Validation_Data";
                            validationLogger.LogValidationData(startTime, endTime, cellId, innerId, zoneNumber, validationData);
                            Debug.WriteLine($"[IPVS Zone {zoneNumber}] VALIDATION 로그 생성 완료");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[IPVS Zone {zoneNumber}] VALIDATION 로그 오류: {ex.Message}");
                        allSuccess = false;
                    }
                    
                    return allSuccess;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[IPVS Zone {zoneNumber}] 로그 생성 전체 오류: {ex.Message}");
                    return false;
                }
            } // lock 종료
        }
    }
}

