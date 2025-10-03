using System;
using System.Collections.Generic;

namespace OptiX.Models
{
    /// <summary>
    /// 검사 데이터 구조체 (MTP/IPVS 공통)
    /// </summary>
    public class InspectionData
    {
        public int Zone { get; set; }
        public string CellId { get; set; } = "";
        public string InnerId { get; set; } = "";
        public string PgPort { get; set; } = "";
        public Dictionary<string, string> MeasPorts { get; set; } = new Dictionary<string, string>();
        
        // 파일 생성 여부
        public bool CreateEecp { get; set; } = false;
        public bool CreateCim { get; set; } = false;
        public bool CreateEecpSummary { get; set; } = false;
        public bool CreateValidation { get; set; } = false;
    }

    /// <summary>
    /// MTP 검사 데이터 관리자
    /// </summary>
    public class MTPDataManager
    {
        private static Dictionary<int, InspectionData> _mtpData = new Dictionary<int, InspectionData>();
        
        public static void LoadFromIni(IniFileManager iniManager)
        {
            _mtpData.Clear();
            
            // Settings에서 MTP_ZONE 읽기
            string zoneCountStr = iniManager.ReadValue("Settings", "MTP_ZONE", "2");
            int zoneCount = int.Parse(zoneCountStr);
            
            // WAD 값 읽기 (예: "0,15,30,45,60")
            string wadString = iniManager.ReadValue("MTP", "WAD", "0,15,30,45,60");
            string[] wadValues = wadString.Split(',');
            
            for (int zone = 1; zone <= zoneCount; zone++)
            {
                var data = new InspectionData
                {
                    Zone = zone,
                    CellId = iniManager.ReadValue("MTP", $"CELL_ID_ZONE_{zone}", ""),
                    InnerId = iniManager.ReadValue("MTP", $"INNER_ID_ZONE_{zone}", ""),
                    PgPort = iniManager.ReadValue("MTP", $"PG_PORT_{zone}", ""),
                    CreateEecp = iniManager.ReadValue("MTP", "CREATE_EECP", "F").ToUpper() == "T",
                    CreateCim = iniManager.ReadValue("MTP", "CREATE_CIM", "F").ToUpper() == "T",
                    CreateEecpSummary = iniManager.ReadValue("MTP", "CREATE_EECP_SUMMARY", "F").ToUpper() == "T",
                    CreateValidation = iniManager.ReadValue("MTP", "CREATE_VALIDATION", "F").ToUpper() == "T"
                };
                
                // WAD 기반으로 동적 MEAS 포트들 생성 및 로드
                foreach (string wad in wadValues)
                {
                    string wadTrimmed = wad.Trim();
                    string measKey = $"MEAS_PORT_{wadTrimmed}_{zone}";
                    data.MeasPorts[measKey] = iniManager.ReadValue("MTP", measKey, "");
                }
                
                _mtpData[zone] = data;
            }
        }
        
        public static InspectionData GetData(int zone)
        {
            return _mtpData.ContainsKey(zone) ? _mtpData[zone] : new InspectionData { Zone = zone };
        }
        
        public static void SaveToIni(IniFileManager iniManager, int zone, InspectionData data)
        {
            iniManager.WriteValue("MTP", $"CELL_ID_ZONE_{zone}", data.CellId);
            iniManager.WriteValue("MTP", $"INNER_ID_ZONE_{zone}", data.InnerId);
            iniManager.WriteValue("MTP", $"PG_PORT_{zone}", data.PgPort);
            
            iniManager.WriteValue("MTP", "CREATE_EECP", data.CreateEecp ? "T" : "F");
            iniManager.WriteValue("MTP", "CREATE_CIM", data.CreateCim ? "T" : "F");
            iniManager.WriteValue("MTP", "CREATE_EECP_SUMMARY", data.CreateEecpSummary ? "T" : "F");
            iniManager.WriteValue("MTP", "CREATE_VALIDATION", data.CreateValidation ? "T" : "F");
            
            // WAD 기반 MEAS 포트들 저장
            foreach (var kvp in data.MeasPorts)
            {
                iniManager.WriteValue("MTP", kvp.Key, kvp.Value);
            }
            
            _mtpData[zone] = data;
        }
    }

    /// <summary>
    /// IPVS 검사 데이터 관리자
    /// </summary>
    public class IPVSDataManager
    {
        private static Dictionary<int, InspectionData> _ipvsData = new Dictionary<int, InspectionData>();
        
        public static void LoadFromIni(IniFileManager iniManager)
        {
            _ipvsData.Clear();
            
            // Settings에서 IPVS_ZONE 읽기
            string zoneCountStr = iniManager.ReadValue("Settings", "IPVS_ZONE", "2");
            int zoneCount = int.Parse(zoneCountStr);
            
            // WAD 값 읽기 (IPVS 섹션에서 읽기)
            string wadString = iniManager.ReadValue("IPVS", "WAD", "0,30,60,90,120");
            string[] wadValues = wadString.Split(',');
            
            for (int zone = 1; zone <= zoneCount; zone++)
            {
                var data = new InspectionData
                {
                    Zone = zone,
                    CellId = iniManager.ReadValue("IPVS", $"CELL_ID_ZONE_{zone}", ""),
                    InnerId = iniManager.ReadValue("IPVS", $"INNER_ID_ZONE_{zone}", ""),
                    PgPort = iniManager.ReadValue("IPVS", $"PG_PORT_{zone}", ""),
                    CreateEecp = iniManager.ReadValue("IPVS", "CREATE_EECP", "F").ToUpper() == "T",
                    CreateCim = iniManager.ReadValue("IPVS", "CREATE_CIM", "F").ToUpper() == "T",
                    CreateEecpSummary = iniManager.ReadValue("IPVS", "CREATE_EECP_SUMMARY", "F").ToUpper() == "T",
                    CreateValidation = iniManager.ReadValue("IPVS", "CREATE_VALIDATION", "F").ToUpper() == "T"
                };
                
                // WAD 기반으로 동적 MEAS 포트들 생성 및 로드 (IPVS 섹션에서 읽기)
                foreach (string wad in wadValues)
                {
                    string wadTrimmed = wad.Trim();
                    string measKey = $"MEAS_PORT_{wadTrimmed}_{zone}";
                    data.MeasPorts[measKey] = iniManager.ReadValue("IPVS", measKey, "");
                }
                
                _ipvsData[zone] = data;
            }
        }
        
        public static InspectionData GetData(int zone)
        {
            return _ipvsData.ContainsKey(zone) ? _ipvsData[zone] : new InspectionData { Zone = zone };
        }
        
        public static void SaveToIni(IniFileManager iniManager, int zone, InspectionData data)
        {
            iniManager.WriteValue("IPVS", $"CELL_ID_ZONE_{zone}", data.CellId);
            iniManager.WriteValue("IPVS", $"INNER_ID_ZONE_{zone}", data.InnerId);
            iniManager.WriteValue("IPVS", $"PG_PORT_{zone}", data.PgPort);
            
            iniManager.WriteValue("IPVS", "CREATE_EECP", data.CreateEecp ? "T" : "F");
            iniManager.WriteValue("IPVS", "CREATE_CIM", data.CreateCim ? "T" : "F");
            iniManager.WriteValue("IPVS", "CREATE_EECP_SUMMARY", data.CreateEecpSummary ? "T" : "F");
            iniManager.WriteValue("IPVS", "CREATE_VALIDATION", data.CreateValidation ? "T" : "F");
            
            // WAD 기반 MEAS 포트들은 IPVS 섹션에 저장
            foreach (var kvp in data.MeasPorts)
            {
                iniManager.WriteValue("IPVS", kvp.Key, kvp.Value);
            }
            
            _ipvsData[zone] = data;
        }
    }
}
