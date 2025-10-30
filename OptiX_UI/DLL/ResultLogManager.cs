using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OptiX.Common;

namespace OptiX.DLL
{
    //25.10.30 - ResultLogManager 구조 변경 (Zone별 CIM 즉시 생성 → 전체 EECP/Summary 생성)
    // 결과 로그 생성 관리 클래스
    public static class ResultLogManager
    {
        // WAD 각도 개수 (7개: 0도, 30도, 45도, 60도, 15도, A도, B도)
        public const int WAD_COUNT = 7;
        
        // 패턴 개수 (17개: W, R, G, B, WG, WG2~WG13)
        public const int PATTERN_COUNT = 17;
        
        //25.10.30 - Zone별 CIM 로그 생성 (각 Zone SEQ 완료 시 즉시 호출)
        /// <summary>
        /// Zone별 CIM 로그 생성 (Zone SEQ 완료 시 즉시 호출, 병렬 실행 가능)
        /// </summary>
        public static bool CreateCIMForZone(
            DateTime startTime, 
            DateTime endTime, 
            string cellId, 
            string innerId, 
            int zoneNumber, 
            Output output)
        {
            try
            {
                ErrorLogger.Log($"CIM 로그 생성 시작", ErrorLogger.LogLevel.INFO, zoneNumber);
                
                // CIM 로그 생성 (OPTIC)
                string createCim = GlobalDataManager.GetValue("MTP", "CREATE_CIM", "F");
                if (createCim == "T")
                {
                    string cimData = $"Zone_{zoneNumber}_CIM_Data";
                    OptiX.Result_LOG.OPTIC.OpticCIMLogger.LogCIMData(startTime, endTime, cellId, innerId, zoneNumber, cimData);
                    ErrorLogger.Log($"OPTIC CIM 로그 생성 완료", ErrorLogger.LogLevel.INFO, zoneNumber);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException(ex, "OPTIC CIM 로그 생성 중 오류", zoneNumber);
                return false;
            }
        }
        
        //25.10.30 - 모든 Zone 완료 후 EECP/Summary 생성 (전체 데이터 통합)
        /// <summary>
        /// 모든 Zone 완료 후 EECP와 EECP_SUMMARY 로그 생성
        /// </summary>
        public static bool CreateAllResultLogs(
            DateTime startTime,
            DateTime endTime,
            Dictionary<int, (string cellId, string innerId, Output output)> zoneData)
        {
            try
            {
                ErrorLogger.Log($"전체 결과 로그 생성 시작 (Zone {zoneData.Count}개)", ErrorLogger.LogLevel.INFO);
                
                bool allSuccess = true;
                
                // EECP 로그 생성 (모든 Zone 데이터)
                try
                {
                    string createEecp = GlobalDataManager.GetValue("MTP", "CREATE_EECP", "F");
                    if (createEecp == "T")
                    {
                        var eecpLogger = OptiX.Result_LOG.OPTIC.OpticEECPLogger.Instance;
                        
                        foreach (var kvp in zoneData.OrderBy(x => x.Key))
                        {
                            int zoneNumber = kvp.Key;
                            var data = kvp.Value;
                            eecpLogger.LogEECPData(startTime, endTime, data.cellId, data.innerId, zoneNumber, data.output);
                        }
                        
                        ErrorLogger.Log($"OPTIC EECP 로그 생성 완료 (Zone {zoneData.Count}개)", ErrorLogger.LogLevel.INFO);
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogException(ex, "OPTIC EECP 로그 생성 중 오류");
                    allSuccess = false;
                }
                
                // EECP_SUMMARY 로그 생성 (모든 Zone 데이터)
                try
                {
                    string createEecpSummary = GlobalDataManager.GetValue("MTP", "CREATE_EECP_SUMMARY", "F");
                    if (createEecpSummary == "T")
                    {
                        var eecpSummaryLogger = OptiX.Result_LOG.OPTIC.OpticEECPSummaryLogger.Instance;
                        
                        foreach (var kvp in zoneData.OrderBy(x => x.Key))
                        {
                            int zoneNumber = kvp.Key;
                            var data = kvp.Value;
                            string summaryData = $"Zone_{zoneNumber}_Summary_Data";
                            eecpSummaryLogger.LogEECPSummaryData(startTime, endTime, data.cellId, data.innerId, zoneNumber, summaryData);
                        }
                        
                        ErrorLogger.Log($"OPTIC EECP_SUMMARY 로그 생성 완료 (Zone {zoneData.Count}개)", ErrorLogger.LogLevel.INFO);
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogException(ex, "OPTIC EECP_SUMMARY 로그 생성 중 오류");
                    allSuccess = false;
                }
                
                return allSuccess;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException(ex, "전체 로그 생성 중 오류");
                return false;
            }
        }
        
        
        //25.10.30 - IPVS Zone별 CIM 로그 생성 (각 Zone SEQ 완료 시 즉시 호출)
        /// <summary>
        /// IPVS Zone별 CIM 로그 생성 (Zone SEQ 완료 시 즉시 호출, 병렬 실행 가능)
        /// </summary>
        public static bool CreateIPVSCIMForZone(
            DateTime startTime, 
            DateTime endTime, 
            string cellId, 
            string innerId, 
            int zoneNumber, 
            Output output)
        {
            try
            {
                ErrorLogger.Log($"IPVS CIM 로그 생성 시작", ErrorLogger.LogLevel.INFO, zoneNumber);
                
                // CIM 로그 생성 (IPVS)
                string createCim = GlobalDataManager.GetValue("IPVS", "CREATE_CIM", "F");
                if (createCim == "T")
                {
                    string cimData = $"Zone_{zoneNumber}_CIM_Data";
                    OptiX.Result_LOG.IPVS.IPVSCIMLogger.LogCIMData(startTime, endTime, cellId, innerId, zoneNumber, cimData);
                    ErrorLogger.Log($"IPVS CIM 로그 생성 완료", ErrorLogger.LogLevel.INFO, zoneNumber);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException(ex, "IPVS CIM 로그 생성 중 오류", zoneNumber);
                return false;
            }
        }
        
        //25.10.30 - IPVS 모든 Zone 완료 후 EECP/Summary 생성 (전체 데이터 통합)
        /// <summary>
        /// IPVS 모든 Zone 완료 후 EECP와 EECP_SUMMARY 로그 생성
        /// </summary>
        public static bool CreateIPVSAllResultLogs(
            DateTime startTime,
            DateTime endTime,
            Dictionary<int, (string cellId, string innerId, Output output)> zoneData)
        {
            try
            {
                ErrorLogger.Log($"IPVS 전체 결과 로그 생성 시작 (Zone {zoneData.Count}개)", ErrorLogger.LogLevel.INFO);
                
                bool allSuccess = true;
                
                // EECP 로그 생성 (모든 Zone 데이터)
                try
                {
                    string createEecp = GlobalDataManager.GetValue("IPVS", "CREATE_EECP", "F");
                    if (createEecp == "T")
                    {
                        var eecpLogger = OptiX.Result_LOG.IPVS.IPVSEECPLogger.Instance;
                        
                        foreach (var kvp in zoneData.OrderBy(x => x.Key))
                        {
                            int zoneNumber = kvp.Key;
                            var data = kvp.Value;
                            eecpLogger.LogEECPData(startTime, endTime, data.cellId, data.innerId, zoneNumber, data.output);
                        }
                        
                        ErrorLogger.Log($"IPVS EECP 로그 생성 완료 (Zone {zoneData.Count}개)", ErrorLogger.LogLevel.INFO);
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogException(ex, "IPVS EECP 로그 생성 중 오류");
                    allSuccess = false;
                }
                
                // EECP_SUMMARY 로그 생성 (모든 Zone 데이터)
                try
                {
                    string createEecpSummary = GlobalDataManager.GetValue("IPVS", "CREATE_EECP_SUMMARY", "F");
                    if (createEecpSummary == "T")
                    {
                        var eecpSummaryLogger = OptiX.Result_LOG.IPVS.IPVSEECPSummaryLogger.Instance;
                        
                        foreach (var kvp in zoneData.OrderBy(x => x.Key))
                        {
                            int zoneNumber = kvp.Key;
                            var data = kvp.Value;
                            string summaryData = $"Zone_{zoneNumber}_Summary_Data";
                            eecpSummaryLogger.LogEECPSummaryData(startTime, endTime, data.cellId, data.innerId, zoneNumber, summaryData);
                        }
                        
                        ErrorLogger.Log($"IPVS EECP_SUMMARY 로그 생성 완료 (Zone {zoneData.Count}개)", ErrorLogger.LogLevel.INFO);
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogException(ex, "IPVS EECP_SUMMARY 로그 생성 중 오류");
                    allSuccess = false;
                }
                
                return allSuccess;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException(ex, "IPVS 전체 로그 생성 중 오류");
                return false;
            }
        }
    }
}

