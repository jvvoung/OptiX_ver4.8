using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace OptiX.Common
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
                ErrorLogger.LogException(ex, $"INI 섹션 읽기 중 예외 - Section: [{section}], File: {_filePath}");
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
                ErrorLogger.LogException(ex, $"INI 전체 섹션 읽기 중 예외 - File: {_filePath}");
            }

            return result;
        }

        // INI 파일이 존재하는지 확인
        public bool Exists()
        {
            return File.Exists(_filePath);
        }

        // INI 파일에 값 쓰기
        public void WriteValue(string section, string key, string value)
        {
            try
            {
                // INI 파일이 존재하지 않으면 생성
                if (!File.Exists(_filePath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
                    File.Create(_filePath).Close();
                }

                // 파일 내용 읽기
                List<string> lines = new List<string>();
                if (File.Exists(_filePath))
                {
                    lines.AddRange(File.ReadAllLines(_filePath));
                }

                // 섹션 찾기 또는 생성
                int sectionIndex = FindSectionIndex(lines, section);
                if (sectionIndex == -1)
                {
                    // 섹션이 없으면 추가
                    if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[lines.Count - 1]))
                    {
                        lines.Add(""); // 빈 줄 추가
                    }
                    lines.Add($"[{section}]");
                    sectionIndex = lines.Count - 1;
                }

                // 키 찾기 또는 생성
                int keyIndex = FindKeyIndex(lines, sectionIndex, key);
                if (keyIndex == -1)
                {
                    // 키가 없으면 섹션 다음에 추가
                    lines.Insert(sectionIndex + 1, $"{key}={value}");
                }
                else
                {
                    // 키가 있으면 값 업데이트
                    lines[keyIndex] = $"{key}={value}";
                }

                // 파일에 쓰기
                File.WriteAllLines(_filePath, lines);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"INI 파일 쓰기 오류: {ex.Message}");
                ErrorLogger.LogException(ex, $"INI 파일 쓰기 중 예외 - File: {_filePath}");
                throw;
            }
        }

        // 섹션 인덱스 찾기
        private int FindSectionIndex(List<string> lines, string section)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    string currentSection = line.Substring(1, line.Length - 2);
                    if (currentSection.Equals(section, StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        // 키 인덱스 찾기 (특정 섹션 내에서)
        private int FindKeyIndex(List<string> lines, int sectionIndex, string key)
        {
            for (int i = sectionIndex + 1; i < lines.Count; i++)
            {
                string line = lines[i].Trim();
                
                // 다음 섹션을 만나면 중단
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    break;
                }

                // 키=값 형태인지 확인
                if (line.Contains("="))
                {
                    int equalIndex = line.IndexOf('=');
                    string currentKey = line.Substring(0, equalIndex).Trim();
                    if (currentKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

    }
}
