using System;
using System.Collections.Generic;
using System.Linq;
using OptiX.OPTIC;
using OptiX.IPVS;

namespace OptiX.DLL
{
    /// <summary>
    /// DLL 결과 처리 전담 클래스
    /// OPTIC과 IPVS의 DLL 결과를 DataTable에 업데이트하는 로직을 처리합니다.
    /// </summary>
    public class DllResultHandler
    {
        #region OPTIC 결과 처리 (7x17 = 119개)
        
        /// <summary>
        /// OPTIC DLL 결과를 DataTable에 업데이트
        /// </summary>
        /// <param name="output">DLL 출력 구조체</param>
        /// <param name="targetZone">대상 Zone (1~4)</param>
        /// <param name="dataTableManager">데이터 테이블 관리자</param>
        /// <param name="selectedWadIndex">현재 선택된 WAD 인덱스 (0~6)</param>
        /// <param name="categoryNames">카테고리 이름 배열 (W, R, G, B, WG, ...)</param>
        /// <param name="onZoneJudgmentUpdate">Zone 판정 결과 콜백 (판정 현황 표 업데이트용)</param>
        /// <returns>Zone 전체 판정 결과 (OK/NG/PTN)</returns>
        public string ProcessOpticResult(
            Output output,
            string targetZone,
            OpticDataTableManager dataTableManager,
            int selectedWadIndex,
            string[] categoryNames,
            Action<string> onZoneJudgmentUpdate)
        {
            try
            {
                int zoneNum = int.TryParse(targetZone, out int zn) ? zn : 0;
                Common.ErrorLogger.Log($"=== OPTIC 결과 처리 시작 ===", Common.ErrorLogger.LogLevel.INFO, zoneNum > 0 ? zoneNum : (int?)null);

                // actualZone: Zone 번호를 int로 변환 (1~4)
                int actualZone = int.TryParse(targetZone, out int z) ? z : 1;

                // Cell ID와 Inner ID 읽기 (캐시된 Zone 정보 사용)
                var (cellId, innerId) = Common.GlobalDataManager.GetZoneInfo(actualZone);

                Common.ErrorLogger.Log($"Cell ID: '{cellId}', Inner ID: '{innerId}'", Common.ErrorLogger.LogLevel.INFO, actualZone);

                // TACT 계산 (해당 Zone의 SEQ 시작 시간부터 종료 시간까지의 소요 시간)
                DateTime zoneSeqStartTime = SeqExecutionManager.GetZoneSeqStartTime(actualZone);
                DateTime zoneSeqEndTime = SeqExecutionManager.GetZoneSeqEndTime(actualZone);
                
                // Race Condition 방지: 종료 시간이 아직 설정되지 않았으면 현재 시간 사용
                if (zoneSeqEndTime == default(DateTime) || zoneSeqEndTime < zoneSeqStartTime)
                {
                    zoneSeqEndTime = DateTime.Now;
                    Common.ErrorLogger.Log($"종료 시간 미설정 - 현재 시간 사용", Common.ErrorLogger.LogLevel.WARNING, actualZone);
                }
                
                double tactSeconds = (zoneSeqEndTime - zoneSeqStartTime).TotalSeconds;
                string tactValue = tactSeconds.ToString("F3");
                
                Common.ErrorLogger.Log($"TACT: {tactValue}초 (시작: {zoneSeqStartTime:HH:mm:ss.fff}, 종료: {zoneSeqEndTime:HH:mm:ss.fff})", Common.ErrorLogger.LogLevel.INFO, actualZone);

                // DLL output 구조체를 2차원 배열로 변환 (result 값 추출)
                int[,] resultArray = new int[DllConstants.MAX_WAD_COUNT, DllConstants.MAX_PATTERN_COUNT];
                for (int wad = 0; wad < DllConstants.MAX_WAD_COUNT; wad++)
                {
                    for (int pattern = 0; pattern < DllConstants.MAX_PATTERN_COUNT; pattern++)
                    {
                        int index = wad * DllConstants.MAX_PATTERN_COUNT + pattern;
                        if (index < output.data.Length)
                        {
                            resultArray[wad, pattern] = output.data[index].result;
                        }
                    }
                }

                // Zone 전체 판정 수행
                string zoneJudgment = OpticJudgment.Instance.JudgeZoneFromResults(resultArray);
                Common.ErrorLogger.Log($"Zone 전체 판정: {zoneJudgment}", Common.ErrorLogger.LogLevel.INFO, actualZone);

                // DataTableManager를 통해 데이터 업데이트
                int availableCategories = Math.Min(categoryNames.Length, DllConstants.MAX_PATTERN_COUNT);
                for (int categoryIndex = 0; categoryIndex < availableCategories; categoryIndex++)
                {
                    // 현재 선택된 WAD에서 해당 패턴의 데이터 가져오기
                    int patternIndex = OpticHelpers.GetPatternArrayIndex(categoryNames[categoryIndex]);
                    int dataIndex = selectedWadIndex * DllConstants.MAX_PATTERN_COUNT + patternIndex;

                    if (dataIndex >= output.data.Length) continue;

                    var pattern = output.data[dataIndex];
                    int result = pattern.result;

                    //Common.ErrorLogger.Log($"Category {categoryNames[categoryIndex]}, result={result}", Common.ErrorLogger.LogLevel.DEBUG, actualZone);

                    // 개별 패턴 판정
                    string patternJudgment = OpticJudgment.Instance.GetPatternJudgment(result);

                    // DataTableManager를 통해 아이템 업데이트
                    dataTableManager.UpdateDataItem(
                        targetZone,
                        categoryNames[categoryIndex],
                        pattern.x.ToString("F2"),
                        pattern.y.ToString("F2"),
                        pattern.L.ToString("F2"),
                        pattern.cur.ToString("F3"),
                        pattern.eff.ToString("F2"),
                        cellId,
                        innerId,
                        tactValue,
                        patternJudgment
                    );

                    Common.ErrorLogger.Log($"개별 판정 결과: {categoryNames[categoryIndex]} - Judgment: {patternJudgment}", Common.ErrorLogger.LogLevel.DEBUG, actualZone);
                }

                // Zone 전체 판정을 모든 아이템에 적용
                dataTableManager.UpdateZoneJudgment(targetZone, zoneJudgment);
                Common.ErrorLogger.Log($"전체 판정 적용: {zoneJudgment}", Common.ErrorLogger.LogLevel.DEBUG, actualZone);

                // 판정 현황 표 업데이트 콜백 호출
                onZoneJudgmentUpdate?.Invoke(zoneJudgment);

                Common.ErrorLogger.Log($"=== OPTIC 결과 처리 완료 - 판정: {zoneJudgment} ===", Common.ErrorLogger.LogLevel.INFO, zoneNum > 0 ? zoneNum : (int?)null);
                
                return zoneJudgment;
            }
            catch (Exception ex)
            {
                int zoneNum = int.TryParse(targetZone, out int z) ? z : 0;
                Common.ErrorLogger.LogException(ex, $"OPTIC 결과 처리 중 예외", zoneNum > 0 ? zoneNum : (int?)null);
                throw;
            }
        }

        #endregion

        #region IPVS 결과 처리 (7x10 = 70개)

        /// <summary>
        /// IPVS DLL 결과를 DataTable에 업데이트
        /// </summary>
        /// <param name="output">DLL 출력 구조체</param>
        /// <param name="targetZone">대상 Zone (1~4)</param>
        /// <param name="dataTableManager">데이터 테이블 관리자</param>
        /// <param name="selectedWadIndex">현재 선택된 WAD 인덱스 (0~6)</param>
        /// <param name="onZoneJudgmentUpdate">Zone 판정 결과 콜백 (판정 현황 표 업데이트용)</param>
        /// <returns>Zone 전체 판정 결과 (OK/R/J)</returns>
        public string ProcessIPVSResult(
            Output output,
            string targetZone,
            IPVSDataTableManager dataTableManager,
            int selectedWadIndex,
            Action<string> onZoneJudgmentUpdate)
        {
            try
            {
                // actualZone: Zone 번호를 int로 변환 (1~4)
                int actualZone = int.TryParse(targetZone, out int z) ? z : 1;
                Common.ErrorLogger.Log($"=== IPVS 결과 처리 시작 ===", Common.ErrorLogger.LogLevel.INFO, actualZone);

                // Cell ID와 Inner ID 읽기 (캐시된 IPVS Zone 정보 사용)
                var (cellId, innerId) = Common.GlobalDataManager.GetIPVSZoneInfo(actualZone);

                Common.ErrorLogger.Log($"Cell ID: '{cellId}', Inner ID: '{innerId}'", Common.ErrorLogger.LogLevel.INFO, actualZone);

                // TACT 계산
                DateTime zoneSeqStartTime = SeqExecutionManager.GetZoneSeqStartTime(actualZone);
                DateTime zoneSeqEndTime = SeqExecutionManager.GetZoneSeqEndTime(actualZone);
                
                if (zoneSeqEndTime == default(DateTime) || zoneSeqEndTime < zoneSeqStartTime)
                {
                    zoneSeqEndTime = DateTime.Now;
                }
                
                double tactSeconds = (zoneSeqEndTime - zoneSeqStartTime).TotalSeconds;
                string tactValue = tactSeconds.ToString("F3");
                
                Common.ErrorLogger.Log($"TACT: {tactValue}초 (시작: {zoneSeqStartTime:HH:mm:ss.fff}, 종료: {zoneSeqEndTime:HH:mm:ss.fff})", Common.ErrorLogger.LogLevel.INFO, actualZone);

                // POINT==1 행만 업데이트 (현재 선택된 WAD의 첫 번째 포인트)
                // IPVS_data는 1차원 배열 [IPVS_DATA_SIZE] (MAX_WAD_COUNT × MAX_POINT_COUNT)
                // 접근: [wadIndex * MAX_POINT_COUNT + pointIndex]
                int dataIndex = selectedWadIndex * DllConstants.MAX_POINT_COUNT + 0; // POINT==1이므로 pointIndex=0
                var pattern = output.IPVS_data[dataIndex];
                
                // DataTableManager를 통해 POINT==1 행 업데이트
                dataTableManager.UpdateDataItem(
                    targetZone,
                    "1", // Point
                    pattern.x.ToString("F2"),
                    pattern.y.ToString("F2"),
                    pattern.L.ToString("F2"),
                    pattern.cur.ToString("F3"),
                    pattern.eff.ToString("F2"),
                    cellId,
                    innerId,
                    tactValue
                );

                //Common.ErrorLogger.Log($"POINT==1 업데이트 완료", Common.ErrorLogger.LogLevel.DEBUG, actualZone);

                // 판정: [0][0] ~ [MAX_WAD_COUNT-1][0] 데이터로 수행 (MAX_WAD_COUNT개 WAD 각도)
                int okCount = 0;
                int totalWad = 0;
                
                for (int wadIdx = 0; wadIdx < DllConstants.MAX_WAD_COUNT; wadIdx++)
                {
                    int idx = wadIdx * DllConstants.MAX_POINT_COUNT + 0; // 각 WAD의 첫 번째 포인트
                    if (idx < output.IPVS_data.Length)
                    {
                        totalWad++;
                        int result = output.IPVS_data[idx].result;
                        
                        // IPVSJudgment 클래스 사용
                        string judgment = IPVSJudgment.Instance.GetPointJudgment(result);
                        if (judgment == "OK") okCount++;
                        
                       // Common.ErrorLogger.Log($"WAD[{wadIdx}][0] (index={idx}) result={result} → {judgment}", Common.ErrorLogger.LogLevel.DEBUG, actualZone);
                    }
                }

                string zoneJudgment = (okCount == totalWad) ? "OK" : "R/J";
                Common.ErrorLogger.Log($"Zone 전체 판정: {zoneJudgment} (OK: {okCount}/{totalWad})", Common.ErrorLogger.LogLevel.INFO, actualZone);

                // Zone 전체 판정을 모든 아이템에 적용
                dataTableManager.UpdateZoneJudgment(targetZone, zoneJudgment);

                // 판정 현황 표 업데이트 콜백 호출
                onZoneJudgmentUpdate?.Invoke(zoneJudgment);

                Common.ErrorLogger.Log($"=== IPVS 결과 처리 완료 - 판정: {zoneJudgment} ===", Common.ErrorLogger.LogLevel.INFO, actualZone);
                
                return zoneJudgment;
            }
            catch (Exception ex)
            {
                int zoneNum = int.TryParse(targetZone, out int z) ? z : 0;
                Common.ErrorLogger.LogException(ex, $"IPVS 결과 처리 중 예외", zoneNum > 0 ? zoneNum : (int?)null);
                throw;
            }
        }

        #endregion
    }
}

