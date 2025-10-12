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
        private static Dictionary<int, (string cellId, string innerId)> _zoneInfo = new Dictionary<int, (string, string)>();

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
                
                // IPVS 섹션
                LoadSectionData("IPVS");
                
                // IPVS_PATHS 섹션
                LoadSectionData("IPVS_PATHS");
                
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
                "EECP_FOLDER", "CIM_FOLDER", "VALID_FOLDER", "DLL_FOLDER", "SEQUENCE_FOLDER",
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
                
                for (int zone = 1; zone <= ipvsZoneCount; zone++)
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
        /// Zone별 Cell ID와 Inner ID를 전역 변수에 로드 (MTP 성능 최적화)
        /// </summary>
        private static void LoadZoneInfo()
        {
            _zoneInfo.Clear();
            
            try
            {
                // 캐시된 _iniData에서 읽기 (INI 파일 재읽기 방지)
                string mtpZoneStr = GetValue("Settings", "MTP_ZONE", "2");
                int mtpZoneCount = int.Parse(mtpZoneStr);
                
                System.Diagnostics.Debug.WriteLine($"MTP_ZONE 개수: {mtpZoneCount}");
                
                for (int zone = 1; zone <= mtpZoneCount; zone++)
                {
                    string cellId = GetValue("MTP", $"CELL_ID_ZONE_{zone}", "");
                    string innerId = GetValue("MTP", $"INNER_ID_ZONE_{zone}", "");
                    _zoneInfo[zone] = (cellId, innerId);
                    
                    System.Diagnostics.Debug.WriteLine($"Zone {zone} 로드: Cell ID='{cellId}', Inner ID='{innerId}' (캐시에서 읽음)");
                }
                
                System.Diagnostics.Debug.WriteLine($"Zone 정보 로드 완료: {_zoneInfo.Count}개 Zone");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Zone 정보 로드 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Zone별 Cell ID와 Inner ID를 가져오기 (전역 변수에서)
        /// </summary>
        public static (string cellId, string innerId) GetZoneInfo(int zoneNumber)
        {
            if (_zoneInfo.ContainsKey(zoneNumber))
            {
                var result = _zoneInfo[zoneNumber];
                System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber} 정보 반환: Cell ID='{result.cellId}', Inner ID='{result.innerId}'");
                return result;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Zone {zoneNumber} 정보가 없음 - 기본값 반환 (총 {_zoneInfo.Count}개 Zone)");
                return ("", "");
            }
        }
    }
}
