using System;
using System.Collections.Generic;
using System.Windows;

namespace OptiX
{
    /// <summary>
    /// 다국어 지원을 위한 언어 매니저
    /// </summary>
    public static class LanguageManager
    {
        private static string currentLanguage = "Korean";
        
        // 언어별 텍스트 딕셔너리
        private static Dictionary<string, Dictionary<string, string>> languageTexts = new Dictionary<string, Dictionary<string, string>>
        {
            ["Korean"] = new Dictionary<string, string>
            {
                // 메인 윈도우
                ["MainWindow.Title"] = "OptiX",
                ["MainWindow.Characteristics"] = "특성",
                ["MainWindow.IPVS"] = "IPVS",
                ["MainWindow.Manual"] = "수동",
                ["MainWindow.LUT"] = "LUT",
                ["MainWindow.Settings"] = "설정",
                
                // MainSettingsWindow
                ["MainSettings.TCPIPConnection"] = "TCP/IP 연결",
                ["MainSettings.DLLFolderSettings"] = "DLL 폴더 설정",
                
                // PathSettingWindow
                ["PathSettings.FileGenerationStatus"] = "파일 생성 여부",
                ["PathSettings.FolderPathSettings"] = "폴더 경로 설정",
                ["PathSettings.FilePathSettings"] = "파일 경로 설정",
                ["PathSettings.SelectFolder"] = "폴더를 선택하세요",
                
                // 호버 툴팁
                ["MainWindow.CharacteristicsTooltip.Title"] = "Optic",
                ["MainWindow.CharacteristicsTooltip.Description"] = "1. 패널의 중심점(Center Point) 계측\n2. 패널 점등 및 특성 데이터(휘도, 색좌표, VACS, 전류 등) 취득\n3. 특성 데이터 판정\n4. Cell MTP 및 AI진행",
                ["MainWindow.CharacteristicsTooltip.CenterPoint"] = "Center Point 계측",
                ["MainWindow.IPVSTooltip.Title"] = "IPVS",
                ["MainWindow.IPVSTooltip.Description"] = "1. Cell MTP 전압 인가\n2. User 설정 포인트 계측 이후 특성 데이터(휘도, 색좌표, VACS, 전류 등) 취득\n3. IPVS, WON 로직 진입\n4. 판정 수행",
                ["MainWindow.ManualTooltip.Description"] = "Pattern 점등 후 Manual 계측",
                ["MainWindow.SettingsTooltip.Title"] = "설정",
                ["MainWindow.SettingsTooltip.Description"] = "애플리케이션 설정 및 환경 구성",
                
                // Optic 페이지
                ["OpticPage.Back"] = "←",
                ["OpticPage.Title"] = "📊 특성 데이터 테이블",
                ["OpticPage.WAD"] = "WAD:",
                ["OpticPage.Reset"] = "🔄 RESET",
                ["OpticPage.Setting"] = "⚙️ Setting",
                ["OpticPage.Path"] = "📁 Path",
                ["OpticPage.Start"] = "▶ Start",
                ["OpticPage.Stop"] = "■ Stop",
                ["OpticPage.Chart"] = "📊 Chart",
                ["OpticPage.Report"] = "📄 Report",
                ["OpticPage.Exit"] = "❌ Exit",
                ["OpticPage.CharacteristicDataTable"] = "📊 OPTIC 데이터 테이블",
                ["OpticPage.CharacteristicJudgmentStatus"] = "📊 판정 현황",
                ["OpticPage.Quantity"] = "수량",
                ["OpticPage.OccurrenceRate"] = "Rate",
                ["OpticPage.ControlPanel"] = "⚙️ 컨트롤 패널",
                
                // IPVS 페이지
                ["IPVSPage.Back"] = "←",
                ["IPVSPage.Title"] = "📊 IPVS 데이터 테이블",
                ["IPVSPage.DataTable"] = "데이터 테이블",
                ["IPVSPage.WAD"] = "WAD:",
                ["IPVSPage.Reset"] = "🔄 RESET",
                ["IPVSPage.Setting"] = "⚙️ Setting",
                ["IPVSPage.Path"] = "📁 Path",
                ["IPVSPage.Start"] = "▶ Start",
                ["IPVSPage.Stop"] = "■ Stop",
                ["IPVSPage.Chart"] = "📊 Chart",
                ["IPVSPage.Report"] = "📄 Report",
                ["IPVSPage.Exit"] = "❌ Exit",
                ["IPVSPage.CharacteristicDataTable"] = "📊 OPTIC 데이터 테이블",
                ["IPVSPage.CharacteristicJudgmentStatus"] = "📊 판정 현황",
                ["IPVSPage.Quantity"] = "수량",
                ["IPVSPage.OccurrenceRate"] = "Rate",
                ["IPVSPage.ControlPanel"] = "⚙️ 컨트롤 패널",
                
                // 설정 창들
                ["Settings.Title"] = "설정",
                ["Settings.Language"] = "언어 설정",
                ["Settings.Korean"] = "한국어",
                ["Settings.English"] = "English",
                ["Settings.Vietnamese"] = "Tiếng Việt",
                ["Settings.PortConnection"] = "Port 연결",
                ["Settings.Connect"] = "Connect",
                ["Settings.TcpIp"] = "TCP/IP",
                ["Settings.Save"] = "💾 SAVE",
                ["Settings.Cancel"] = "❌ CANCEL",
                
                // Cell ID 입력 창
                ["CellIdInput.Title"] = "Zone 1 OPTIC SETTING",
                ["CellIdInput.CellInfo"] = "Cell 정보",
                ["CellIdInput.FileGeneration"] = "파일 생성 여부",
                ["CellIdInput.PortConnection"] = "Port 연결",
                ["CellIdInput.Cancel"] = "CANCEL",
                ["CellIdInput.Save"] = "💾 SAVE",
                
                // Path 설정 창
                ["PathSettings.FolderPathSettings"] = "폴더 경로 설정",
                ["PathSettings.FilePathSettings"] = "파일 경로 설정",
                ["PathSettings.SelectFolder"] = "폴더를 선택하세요",
                ["PathSettings.Sequence"] = "📄 Seq.",
                ["PathSettings.Save"] = "💾 SAVE",
                ["PathSettings.Cancel"] = "❌ CANCEL"
            },
            
            ["English"] = new Dictionary<string, string>
            {
                // 메인 윈도우
                ["MainWindow.Title"] = "OptiX",
                ["MainWindow.Characteristics"] = "OPTIC",
                ["MainWindow.IPVS"] = "IPVS",
                ["MainWindow.Manual"] = "Manual",
                ["MainWindow.LUT"] = "LUT",
                ["MainWindow.Settings"] = "Settings",
                
                // MainSettingsWindow
                ["MainSettings.TCPIPConnection"] = "TCP/IP Connection",
                ["MainSettings.DLLFolderSettings"] = "DLL Folder Settings",
                
                // PathSettingWindow
                ["PathSettings.FileGenerationStatus"] = "File Generation Status",
                ["PathSettings.FolderPathSettings"] = "Folder Path Settings",
                ["PathSettings.FilePathSettings"] = "File Path Settings",
                ["PathSettings.SelectFolder"] = "Please select folder",
                
                // 호버 툴팁
                ["MainWindow.CharacteristicsTooltip.Title"] = "OPTIC",
                ["MainWindow.CharacteristicsTooltip.Description"] = "1. Panel Center Point Measurement\n2. Panel Lighting and Characteristic Data (Luminance, Color Coordinates, VACS, Current, etc.) Acquisition\n3. Characteristic Data Judgment\n4. Cell MTP and AI Processing",
                ["MainWindow.CharacteristicsTooltip.CenterPoint"] = "Center Point Measurement",
                ["MainWindow.IPVSTooltip.Title"] = "IPVS",
                ["MainWindow.IPVSTooltip.Description"] = "1. Cell MTP Voltage Application\n2. User Set Point Measurement and Characteristic Data (Luminance, Color Coordinates, VACS, Current, etc.) Acquisition\n3. IPVS, WON Logic Entry\n4. Judgment Execution",
                ["MainWindow.ManualTooltip.Description"] = "Manual measurement after Pattern lighting",
                ["MainWindow.SettingsTooltip.Title"] = "Settings",
                ["MainWindow.SettingsTooltip.Description"] = "Application Settings and Environment Configuration",
                
                // Optic 페이지
                ["OpticPage.Back"] = "←",
                ["OpticPage.Title"] = "📊 Characteristics Data Table",
                ["OpticPage.WAD"] = "WAD:",
                ["OpticPage.Reset"] = "🔄 RESET",
                ["OpticPage.Setting"] = "⚙️ Setting",
                ["OpticPage.Path"] = "📁 Path",
                ["OpticPage.Start"] = "▶ Start",
                ["OpticPage.Stop"] = "■ Stop",
                ["OpticPage.Chart"] = "📊 Chart",
                ["OpticPage.Report"] = "📄 Report",
                ["OpticPage.Exit"] = "❌ Exit",
                ["OpticPage.CharacteristicDataTable"] = "📊 OPTIC Data Table",
                ["OpticPage.CharacteristicJudgmentStatus"] = "📊 Judgment Status",
                ["OpticPage.Quantity"] = "Quantity",
                ["OpticPage.OccurrenceRate"] = "Rate",
                ["OpticPage.ControlPanel"] = "⚙️ Control Panel",
                
                // IPVS 페이지
                ["IPVSPage.Back"] = "←",
                ["IPVSPage.Title"] = "📊 IPVS Data Table",
                ["IPVSPage.DataTable"] = "Data Table",
                ["IPVSPage.WAD"] = "WAD:",
                ["IPVSPage.Reset"] = "🔄 RESET",
                ["IPVSPage.Setting"] = "⚙️ Setting",
                ["IPVSPage.Path"] = "📁 Path",
                ["IPVSPage.Start"] = "▶ Start",
                ["IPVSPage.Stop"] = "■ Stop",
                ["IPVSPage.Chart"] = "📊 Chart",
                ["IPVSPage.Report"] = "📄 Report",
                ["IPVSPage.Exit"] = "❌ Exit",
                ["IPVSPage.CharacteristicDataTable"] = "📊 OPTIC Data Table",
                ["IPVSPage.CharacteristicJudgmentStatus"] = "📊 Judgment Status",
                ["IPVSPage.Quantity"] = "Quantity",
                ["IPVSPage.OccurrenceRate"] = "Rate",
                ["IPVSPage.ControlPanel"] = "⚙️ Control Panel",
                
                // 설정 창들
                ["Settings.Title"] = "Settings",
                ["Settings.Language"] = "Language Settings",
                ["Settings.Korean"] = "한국어",
                ["Settings.English"] = "English",
                ["Settings.Vietnamese"] = "Tiếng Việt",
                ["Settings.PortConnection"] = "Port Connection",
                ["Settings.Connect"] = "Connect",
                ["Settings.TcpIp"] = "TCP/IP",
                ["Settings.Save"] = "💾 SAVE",
                ["Settings.Cancel"] = "❌ CANCEL",
                
                // Cell ID 입력 창
                ["CellIdInput.Title"] = "Zone 1 OPTIC SETTING",
                ["CellIdInput.CellInfo"] = "Cell Info",
                ["CellIdInput.FileGeneration"] = "File Generation",
                ["CellIdInput.PortConnection"] = "Port Connection",
                ["CellIdInput.Cancel"] = "CANCEL",
                ["CellIdInput.Save"] = "💾 SAVE",
                
                // Path 설정 창
                ["PathSettings.FolderPathSettings"] = "Folder Path Settings",
                ["PathSettings.FilePathSettings"] = "File Path Settings",
                ["PathSettings.SelectFolder"] = "Please select a folder",
                ["PathSettings.FilePath"] = "File Path Settings",
                ["PathSettings.Sequence"] = "📄 Seq.",
                ["PathSettings.Save"] = "💾 SAVE",
                ["PathSettings.Cancel"] = "❌ CANCEL"
            },
            
            ["Vietnamese"] = new Dictionary<string, string>
            {
                // 메인 윈도우
                ["MainWindow.Title"] = "OptiX",
                ["MainWindow.Characteristics"] = "OPTIC",
                ["MainWindow.IPVS"] = "IPVS",
                ["MainWindow.Manual"] = "Thủ công",
                ["MainWindow.LUT"] = "LUT",
                ["MainWindow.Settings"] = "Cài đặt",
                
                // MainSettingsWindow
                ["MainSettings.TCPIPConnection"] = "Kết nối TCP/IP",
                ["MainSettings.DLLFolderSettings"] = "Cài đặt thư mục DLL",
                
                // PathSettingWindow
                ["PathSettings.FileGenerationStatus"] = "Trạng thái tạo file",
                ["PathSettings.FolderPathSettings"] = "Cài đặt đường dẫn thư mục",
                ["PathSettings.FilePathSettings"] = "Cài đặt đường dẫn file",
                ["PathSettings.SelectFolder"] = "Vui lòng chọn thư mục",
                
                // 호버 툴팁
                ["MainWindow.CharacteristicsTooltip.Title"] = "OPTIC",
                ["MainWindow.CharacteristicsTooltip.Description"] = "1. Đo điểm trung tâm (Center Point) của panel\n2. Bật sáng panel và thu thập dữ liệu đặc tính (độ sáng, tọa độ màu, VACS, dòng điện, v.v.)\n3. Đánh giá dữ liệu đặc tính\n4. Cell MTP và xử lý AI",
                ["MainWindow.CharacteristicsTooltip.CenterPoint"] = "Đo điểm trung tâm",
                ["MainWindow.IPVSTooltip.Title"] = "IPVS",
                ["MainWindow.IPVSTooltip.Description"] = "1. Áp dụng điện áp Cell MTP\n2. Đo điểm do người dùng thiết lập và thu thập dữ liệu đặc tính (độ sáng, tọa độ màu, VACS, dòng điện, v.v.)\n3. Vào logic IPVS, WON\n4. Thực hiện đánh giá",
                ["MainWindow.ManualTooltip.Description"] = "Đo thủ công sau khi bật Pattern",
                ["MainWindow.SettingsTooltip.Title"] = "Cài đặt",
                ["MainWindow.SettingsTooltip.Description"] = "Cài đặt Ứng dụng và Cấu hình Môi trường",
                
                // Optic 페이지
                ["OpticPage.Back"] = "←",
                ["OpticPage.Title"] = "📊 Bảng Dữ liệu Đặc tính",
                ["OpticPage.WAD"] = "WAD:",
                ["OpticPage.Reset"] = "🔄 RESET",
                ["OpticPage.Setting"] = "⚙️ Setting",
                ["OpticPage.Path"] = "📁 Path",
                ["OpticPage.Start"] = "▶ Start",
                ["OpticPage.Stop"] = "■ Stop",
                ["OpticPage.Chart"] = "📊 Chart",
                ["OpticPage.Report"] = "📄 Report",
                ["OpticPage.Exit"] = "❌ Exit",
                ["OpticPage.CharacteristicDataTable"] = "📊 Bảng Dữ liệu OPTIC",
                ["OpticPage.CharacteristicJudgmentStatus"] = "📊 Tình trạng Đánh giá",
                ["OpticPage.Quantity"] = "Số lượng",
                ["OpticPage.OccurrenceRate"] = "Rate",
                ["OpticPage.ControlPanel"] = "⚙️ Bảng Điều khiển",
                
                // IPVS 페이지
                ["IPVSPage.Back"] = "←",
                ["IPVSPage.Title"] = "📊 Bảng Dữ liệu IPVS",
                ["IPVSPage.DataTable"] = "Bảng Dữ liệu",
                ["IPVSPage.WAD"] = "WAD:",
                ["IPVSPage.Reset"] = "🔄 RESET",
                ["IPVSPage.Setting"] = "⚙️ Setting",
                ["IPVSPage.Path"] = "📁 Path",
                ["IPVSPage.Start"] = "▶ Start",
                ["IPVSPage.Stop"] = "■ Stop",
                ["IPVSPage.Chart"] = "📊 Chart",
                ["IPVSPage.Report"] = "📄 Report",
                ["IPVSPage.Exit"] = "❌ Exit",
                ["IPVSPage.CharacteristicDataTable"] = "📊 Bảng Dữ liệu OPTIC",
                ["IPVSPage.CharacteristicJudgmentStatus"] = "📊 Tình trạng Đánh giá",
                ["IPVSPage.Quantity"] = "Số lượng",
                ["IPVSPage.OccurrenceRate"] = "Rate",
                ["IPVSPage.ControlPanel"] = "⚙️ Bảng Điều khiển",
                
                // 설정 창들
                ["Settings.Title"] = "Cài đặt",
                ["Settings.Language"] = "Cài đặt Ngôn ngữ",
                ["Settings.Korean"] = "한국어",
                ["Settings.English"] = "English",
                ["Settings.Vietnamese"] = "Tiếng Việt",
                ["Settings.PortConnection"] = "Kết nối Port",
                ["Settings.Connect"] = "Connect",
                ["Settings.TcpIp"] = "TCP/IP",
                ["Settings.Save"] = "💾 SAVE",
                ["Settings.Cancel"] = "❌ CANCEL",
                
                // Cell ID 입력 창
                ["CellIdInput.Title"] = "Zone 1 OPTIC SETTING",
                ["CellIdInput.CellInfo"] = "Thông tin Cell",
                ["CellIdInput.FileGeneration"] = "Tạo File",
                ["CellIdInput.PortConnection"] = "Kết nối Port",
                ["CellIdInput.Cancel"] = "CANCEL",
                ["CellIdInput.Save"] = "💾 SAVE",
                
                // Path 설정 창
                ["PathSettings.FolderPathSettings"] = "Cài đặt Đường dẫn Thư mục",
                ["PathSettings.FilePathSettings"] = "Cài đặt Đường dẫn File",
                ["PathSettings.SelectFolder"] = "Vui lòng chọn thư mục",
                ["PathSettings.Sequence"] = "📄 Seq.",
                ["PathSettings.Save"] = "💾 SAVE",
                ["PathSettings.Cancel"] = "❌ CANCEL"
            }
        };

        /// <summary>
        /// 현재 언어 설정
        /// </summary>
        public static string CurrentLanguage
        {
            get { return currentLanguage; }
            set { currentLanguage = value; }
        }

        /// <summary>
        /// 언어별 텍스트 가져오기
        /// </summary>
        /// <param name="key">텍스트 키</param>
        /// <returns>현재 언어에 해당하는 텍스트</returns>
        public static string GetText(string key)
        {
            if (languageTexts.ContainsKey(currentLanguage) && 
                languageTexts[currentLanguage].ContainsKey(key))
            {
                return languageTexts[currentLanguage][key];
            }
            
            // 현재 언어에 없으면 한국어에서 찾기
            if (languageTexts.ContainsKey("Korean") && 
                languageTexts["Korean"].ContainsKey(key))
            {
                return languageTexts["Korean"][key];
            }
            
            return key; // 키 자체를 반환
        }

        /// <summary>
        /// 언어 변경 이벤트
        /// </summary>
        public static event EventHandler LanguageChanged;

        /// <summary>
        /// 언어 설정 변경
        /// </summary>
        /// <param name="language">새 언어</param>
        public static void SetLanguage(string language)
        {
            if (currentLanguage != language && languageTexts.ContainsKey(language))
            {
                currentLanguage = language;
                LanguageChanged?.Invoke(null, EventArgs.Empty);
            }
        }
    }
}
