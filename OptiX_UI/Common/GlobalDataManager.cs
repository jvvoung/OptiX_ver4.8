using System;
using System.Collections.Generic;
using System.IO;

namespace OptiX.Common
{
    /// <summary>
    /// 전역 데이터 매니저 - INI 파일 데이터를 메모리에 캐시하여 성능 향상
    /// </summary>
    public static class GlobalDataManager
    {
        private static Dictionary<string, string> _iniData = new Dictionary<string, string>();
        private static IniFileManager _iniManager;
        private static string _iniFilePath;
        private static bool _isInitialized = false;
        
        // Zone별 정보를 전역 변수로 저장 (INI 파일 읽기 최소화)
        private static Dictionary<int, (string cellId, string innerId)> _mtpZoneInfo = new Dictionary<int, (string, string)>();
        private static Dictionary<int, (string cellId, string innerId)> _ipvsZoneInfo = new Dictionary<int, (string, string)>();
        
        // Sequence 파일 경로 캐시
        private static string _mtpSequencePath = "";
        private static string _ipvsSequencePath = "";

        /// <summary>
        /// 전역 데이터 매니저 초기화 (프로그램 시작 시 호출)
        /// </summary>
        /// <param name="iniFilePath">INI 파일 경로</param>
        public static void Initialize(string iniFilePath)
        {
            try
            {
                _iniFilePath = iniFilePath;
                _iniManager = new IniFileManager(iniFilePath);
                LoadAllIniData();
                _isInitialized = true;
                
                System.Diagnostics.Debug.WriteLine($"GlobalDataManager 초기화 완료: {_iniData.Count}개 항목 로드됨");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GlobalDataManager 초기화 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// INI 파일에서 모든 데이터를 읽어서 메모리에 저장
        /// </summary>
        private static void LoadAllIniData()
        {
            if (_iniManager == null) return;

            _iniData.Clear();

            try
            {
                // 모든 섹션과 키를 읽어서 메모리에 저장
                // MTP 섹션
                LoadSectionData("MTP");
                
                // MTP_PATHS 섹션
                LoadSectionData("MTP_PATHS");
                // MTP Sequence 경로 캐시 (MTP_PATHS 로드 직후)
                string mtpSeqKey = "MTP_PATHS.SEQUENCE_FOLDER";
                _mtpSequencePath = _iniData.ContainsKey(mtpSeqKey) ? _iniData[mtpSeqKey] : @"D:\Project\Recipe\Sequence\Sequence_Optic.ini";
                System.Diagnostics.Debug.WriteLine($"[MTP] SEQUENCE_FOLDER 캐시: {_mtpSequencePath}");
                
                // IPVS 섹션
                LoadSectionData("IPVS");
                
                // IPVS_PATHS 섹션
                LoadSectionData("IPVS_PATHS");
                // IPVS Sequence 경로 캐시 (IPVS_PATHS 로드 직후)
                string ipvsSeqKey = "IPVS_PATHS.SEQUENCE_FOLDER";
                _ipvsSequencePath = _iniData.ContainsKey(ipvsSeqKey) ? _iniData[ipvsSeqKey] : System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sequence_IPVS.ini");
                System.Diagnostics.Debug.WriteLine($"[IPVS] SEQUENCE_FOLDER 캐시: {_ipvsSequencePath}");
                
                // Settings 섹션
                LoadSectionData("Settings");
                
                // Theme 섹션
                LoadSectionData("Theme");
                
                // Zone 정보 로드 (MTP 성능 최적화)
                LoadZoneInfo();

                System.Diagnostics.Debug.WriteLine($"전역 데이터 로드 완료: {_iniData.Count}개 항목");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"INI 데이터 로드 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 특정 섹션의 모든 데이터를 로드
        /// </summary>
        /// <param name="section">섹션 이름</param>
        private static void LoadSectionData(string section)
        {
            if (_iniManager == null)
            {
                System.Diagnostics.Debug.WriteLine($"오류: _iniManager가 null입니다!");
                return;
            }

            // 일반적인 키들을 미리 정의하여 로드
            var commonKeys = new[]
            {
                "Category", "WAD", "TCP_IP", "CREATE_EECP", "CREATE_CIM", 
                "CREATE_EECP_SUMMARY", "CREATE_VALIDATION", "MAX_POINT",
                "EECP_FOLDER", "CIM_FOLDER", "VALID_FOLDER", "DLL_FOLDER", "SEQUENCE_FOLDER", "EECP_SUMMARY_FOLDER",
                "IsDarkMode", "LANGUAGE", "MTP_ZONE", "IPVS_ZONE"
            };

                foreach (var key in commonKeys)
                {
                    string value = _iniManager.ReadValue(section, key, "");
                    if (!string.IsNullOrEmpty(value))
                    {
                        string fullKey = $"{section}.{key}";
                        _iniData[fullKey] = value;
                    }
                }

            // Zone별 Cell ID와 Inner ID를 동적으로 로드
            // Settings 섹션에서 MTP_ZONE과 IPVS_ZONE을 읽어서 해당 수만큼 로드
            if (section == "MTP")
            {
                string mtpZoneStr = _iniManager.ReadValue("Settings", "MTP_ZONE", "2");
                int mtpZoneCount = int.Parse(mtpZoneStr);
                
                for (int zone = 1; zone <= mtpZoneCount; zone++)
                {
                    string cellIdKey = $"CELL_ID_ZONE_{zone}";
                    string innerIdKey = $"INNER_ID_ZONE_{zone}";
                    
                    string cellId = _iniManager.ReadValue(section, cellIdKey, "");
                    string innerId = _iniManager.ReadValue(section, innerIdKey, "");
                    
                    if (!string.IsNullOrEmpty(cellId))
                    {
                        string fullKey = $"{section}.{cellIdKey}";
                        _iniData[fullKey] = cellId;
                    }
                    if (!string.IsNullOrEmpty(innerId))
                    {
                        string fullKey = $"{section}.{innerIdKey}";
                        _iniData[fullKey] = innerId;
                    }
                }
            }
            else if (section == "IPVS")
            {
                string ipvsZoneStr = _iniManager.ReadValue("Settings", "IPVS_ZONE", "2");
                int ipvsZoneCount = int.Parse(ipvsZoneStr);
                
                System.Diagnostics.Debug.WriteLine($"[LoadSectionData] IPVS 섹션 로드 시작: {ipvsZoneCount}개 Zone");
                
                for (int zone = 1; zone <= ipvsZoneCount; zone++)
                {
                    string cellIdKey = $"CELL_ID_ZONE_{zone}";
                    string innerIdKey = $"INNER_ID_ZONE_{zone}";
                    
                    string cellId = _iniManager.ReadValue(section, cellIdKey, "");
                    string innerId = _iniManager.ReadValue(section, innerIdKey, "");
                    
                    System.Diagnostics.Debug.WriteLine($"[LoadSectionData] IPVS Zone {zone} - INI에서 읽음: Cell ID='{cellId}', Inner ID='{innerId}'");
                    
                    if (!string.IsNullOrEmpty(cellId))
                    {
                        string fullKey = $"{section}.{cellIdKey}";
                        _iniData[fullKey] = cellId;
                        System.Diagnostics.Debug.WriteLine($"[LoadSectionData] 캐시에 저장: {fullKey} = '{cellId}'");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[LoadSectionData] ❌ Cell ID가 비어있음: {section}.{cellIdKey}");
                    }
                    
                    if (!string.IsNullOrEmpty(innerId))
                    {
                        string fullKey = $"{section}.{innerIdKey}";
                        _iniData[fullKey] = innerId;
                        System.Diagnostics.Debug.WriteLine($"[LoadSectionData] 캐시에 저장: {fullKey} = '{innerId}'");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[LoadSectionData] ❌ Inner ID가 비어있음: {section}.{innerIdKey}");
                    }
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"LoadSectionData 완료: {section}");
        }

        /// <summary>
        /// 메모리에서 데이터 읽기 (빠른 접근)
        /// </summary>
        /// <param name="section">섹션 이름</param>
        /// <param name="key">키 이름</param>
        /// <param name="defaultValue">기본값</param>
        /// <returns>값</returns>
        public static string GetValue(string section, string key, string defaultValue = "")
        {
            if (!_isInitialized)
            {
                System.Diagnostics.Debug.WriteLine("GlobalDataManager가 초기화되지 않았습니다.");
                return defaultValue;
            }

            string fullKey = $"{section}.{key}";
            bool exists = _iniData.ContainsKey(fullKey);
            string result = exists ? _iniData[fullKey] : defaultValue;
            
            
            return result;
        }
        
        /// <summary>
        /// 섹션 전체 읽기 (Dictionary 반환)
        /// </summary>
        public static Dictionary<string, string> ReadSection(string section)
        {
            try
            {
                if (_iniManager == null)
                {
                    System.Diagnostics.Debug.WriteLine("GlobalDataManager가 초기화되지 않았습니다.");
                    return new Dictionary<string, string>();
                }

                return _iniManager.ReadSection(section);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ReadSection 오류: {ex.Message}");
                return new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// INI 파일에 값 저장 (메모리 캐시도 함께 업데이트)
        /// </summary>
        public static void SetValue(string section, string key, string value)
        {
            try
            {
                if (_iniManager == null)
                {
                    System.Diagnostics.Debug.WriteLine("GlobalDataManager가 초기화되지 않았습니다.");
                    return;
                }

                // INI 파일에 쓰기
                _iniManager.WriteValue(section, key, value);
                
                // 메모리 캐시도 업데이트
                string fullKey = $"{section}.{key}";
                _iniData[fullKey] = value;
                
                System.Diagnostics.Debug.WriteLine($"GlobalDataManager.SetValue: [{section}] {key} = {value}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetValue 오류: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// INI 파일을 다시 읽어서 메모리 캐시 갱신 (SAVE 버튼 클릭 후 호출)
        /// </summary>
        public static void Reload()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("GlobalDataManager.Reload() 호출됨");
                LoadAllIniData();
                System.Diagnostics.Debug.WriteLine("GlobalDataManager 갱신 완료");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Reload 오류: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Zone별 Cell ID와 Inner ID를 전역 변수에 로드 (MTP 및 IPVS 성능 최적화)
        /// ⚠️ 이 함수는 LoadAllIniData() 내부에서 호출되므로, _isInitialized가 아직 false입니다.
        /// 따라서 GetValue() 대신 _iniData를 직접 읽어야 합니다.
        /// </summary>
        private static void LoadZoneInfo()
        {
            _mtpZoneInfo.Clear();
            _ipvsZoneInfo.Clear();
            
            try
            {
                // MTP Zone 정보 로드
                string mtpZoneKey = "Settings.MTP_ZONE";
                string mtpZoneStr = _iniData.ContainsKey(mtpZoneKey) ? _iniData[mtpZoneKey] : "2";
                int mtpZoneCount = int.Parse(mtpZoneStr);
                
                System.Diagnostics.Debug.WriteLine($"=== MTP Zone 정보 로드 시작 (총 {mtpZoneCount}개) ===");
                
                for (int zone = 1; zone <= mtpZoneCount; zone++)
                {
                    string cellIdKey = $"MTP.CELL_ID_ZONE_{zone}";
                    string innerIdKey = $"MTP.INNER_ID_ZONE_{zone}";
                    
                    string cellId = _iniData.ContainsKey(cellIdKey) ? _iniData[cellIdKey] : "";
                    string innerId = _iniData.ContainsKey(innerIdKey) ? _iniData[innerIdKey] : "";
                    
                    _mtpZoneInfo[zone] = (cellId, innerId);
                    
                    System.Diagnostics.Debug.WriteLine($"[MTP] Zone {zone} 로드: Cell ID='{cellId}', Inner ID='{innerId}'");
                }
                
                // IPVS Zone 정보 로드
                string ipvsZoneKey = "Settings.IPVS_ZONE";
                string ipvsZoneStr = _iniData.ContainsKey(ipvsZoneKey) ? _iniData[ipvsZoneKey] : "2";
                int ipvsZoneCount = int.Parse(ipvsZoneStr);
                
                System.Diagnostics.Debug.WriteLine($"=== IPVS Zone 정보 로드 시작 (총 {ipvsZoneCount}개) ===");
                
                for (int zone = 1; zone <= ipvsZoneCount; zone++)
                {
                    string cellIdKey = $"IPVS.CELL_ID_ZONE_{zone}";
                    string innerIdKey = $"IPVS.INNER_ID_ZONE_{zone}";
                    
                    string cellId = _iniData.ContainsKey(cellIdKey) ? _iniData[cellIdKey] : "";
                    string innerId = _iniData.ContainsKey(innerIdKey) ? _iniData[innerIdKey] : "";
                    
                    _ipvsZoneInfo[zone] = (cellId, innerId);
                    
                    System.Diagnostics.Debug.WriteLine($"[IPVS] Zone {zone} 로드: Cell ID='{cellId}', Inner ID='{innerId}'");
                }
                
                System.Diagnostics.Debug.WriteLine($"Zone 정보 로드 완료: MTP {_mtpZoneInfo.Count}개, IPVS {_ipvsZoneInfo.Count}개");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Zone 정보 로드 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Zone별 Cell ID와 Inner ID를 가져오기 (MTP 전용)
        /// </summary>
        public static (string cellId, string innerId) GetZoneInfo(int zoneNumber)
        {
            if (_mtpZoneInfo.ContainsKey(zoneNumber))
            {
                var result = _mtpZoneInfo[zoneNumber];
                System.Diagnostics.Debug.WriteLine($"[MTP] Zone {zoneNumber} 정보 반환: Cell ID='{result.cellId}', Inner ID='{result.innerId}'");
                return result;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[MTP] Zone {zoneNumber} 정보가 없음 - 기본값 반환 (총 {_mtpZoneInfo.Count}개 Zone)");
                return ("", "");
            }
        }
        
        /// <summary>
        /// Zone별 Cell ID와 Inner ID를 가져오기 (IPVS 전용)
        /// </summary>
        public static (string cellId, string innerId) GetIPVSZoneInfo(int zoneNumber)
        {
            if (_ipvsZoneInfo.ContainsKey(zoneNumber))
            {
                var result = _ipvsZoneInfo[zoneNumber];
                System.Diagnostics.Debug.WriteLine($"[IPVS] Zone {zoneNumber} 정보 반환: Cell ID='{result.cellId}', Inner ID='{result.innerId}'");
                return result;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[IPVS] Zone {zoneNumber} 정보가 없음 - 기본값 반환 (총 {_ipvsZoneInfo.Count}개 Zone)");
                return ("", "");
            }
        }
        
        /// <summary>
        /// MTP(OPTIC) Sequence 파일 경로 가져오기
        /// </summary>
        public static string GetMTPSequencePath()
        {
            if (!_isInitialized)
            {
                System.Diagnostics.Debug.WriteLine("GlobalDataManager가 초기화되지 않음!");
                return @"D:\Project\Recipe\Sequence\Sequence_Optic.ini";
            }
            
            return _mtpSequencePath;
        }
        
        /// <summary>
        /// IPVS Sequence 파일 경로 가져오기
        /// </summary>
        public static string GetIPVSSequencePath()
        {
            if (!_isInitialized)
            {
                System.Diagnostics.Debug.WriteLine("GlobalDataManager가 초기화되지 않음!");
                return System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Sequence_IPVS.ini"
                );
            }
            
            return _ipvsSequencePath;
        }
    }
}
