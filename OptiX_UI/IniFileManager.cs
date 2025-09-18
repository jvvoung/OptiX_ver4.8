using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace OptiX
{
    public class IniFileManager
    {
        private string _filePath;

        // Windows API 사용
        [DllImport("kernel32.dll")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);


        public IniFileManager(string filePath)
        {
            _filePath = filePath;
        }

        // INI 파일에서 값 읽기
        public string ReadValue(string section, string key, string defaultValue = "")
        {
            StringBuilder sb = new StringBuilder(255);
            GetPrivateProfileString(section, key, defaultValue, sb, 255, _filePath);
            return sb.ToString();
        }

        // 섹션의 모든 키-값 쌍 읽기
        public Dictionary<string, string> ReadSection(string section)
        {
            var result = new Dictionary<string, string>();
            
            if (!File.Exists(_filePath))
                return result;

            try
            {
                string[] lines = File.ReadAllLines(_filePath);
                bool inSection = false;

                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();
                    
                    // 섹션 시작
                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        string currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                        inSection = currentSection.Equals(section, StringComparison.OrdinalIgnoreCase);
                        continue;
                    }

                    // 현재 섹션 내에서 키=값 파싱
                    if (inSection && trimmedLine.Contains("="))
                    {
                        int equalIndex = trimmedLine.IndexOf('=');
                        string key = trimmedLine.Substring(0, equalIndex).Trim();
                        string value = trimmedLine.Substring(equalIndex + 1).Trim();
                        result[key] = value;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"INI 파일 읽기 오류: {ex.Message}");
            }

            return result;
        }

        // 모든 섹션 읽기
        public Dictionary<string, Dictionary<string, string>> ReadAllSections()
        {
            var result = new Dictionary<string, Dictionary<string, string>>();
            
            if (!File.Exists(_filePath))
                return result;

            try
            {
                string[] lines = File.ReadAllLines(_filePath);
                string currentSection = "";
                var currentSectionData = new Dictionary<string, string>();

                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();
                    
                    // 섹션 시작
                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        // 이전 섹션 저장
                        if (!string.IsNullOrEmpty(currentSection))
                        {
                            result[currentSection] = new Dictionary<string, string>(currentSectionData);
                            currentSectionData.Clear();
                        }
                        
                        currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                        continue;
                    }

                    // 키=값 파싱
                    if (!string.IsNullOrEmpty(currentSection) && trimmedLine.Contains("="))
                    {
                        int equalIndex = trimmedLine.IndexOf('=');
                        string key = trimmedLine.Substring(0, equalIndex).Trim();
                        string value = trimmedLine.Substring(equalIndex + 1).Trim();
                        currentSectionData[key] = value;
                    }
                }

                // 마지막 섹션 저장
                if (!string.IsNullOrEmpty(currentSection))
                {
                    result[currentSection] = new Dictionary<string, string>(currentSectionData);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"INI 파일 읽기 오류: {ex.Message}");
            }

            return result;
        }

        // INI 파일이 존재하는지 확인
        public bool Exists()
        {
            return File.Exists(_filePath);
        }

    }
}
