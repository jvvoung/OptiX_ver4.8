using System;
using System.Collections.Generic;

namespace OptiX.Common
{
    /// <summary>
    /// 포트 연결 상태를 메모리에서 관리하는 싱글톤 클래스
    /// - INI 파일이 아닌 메모리 기반으로 실제 연결 상태만 저장
    /// - 프로그램 종료 시 자동으로 초기화됨
    /// - 하드웨어 상태와 동기화 보장
    /// </summary>
    public sealed class PortConnectionManager
    {
        private static readonly Lazy<PortConnectionManager> _instance = 
            new Lazy<PortConnectionManager>(() => new PortConnectionManager());

        public static PortConnectionManager Instance => _instance.Value;

        // 포트 연결 상태 (Key: "MTP_PG_PORT_1", Value: true/false)
        private readonly Dictionary<string, bool> _connectionStates = new Dictionary<string, bool>();
        private readonly object _lock = new object();

        private PortConnectionManager()
        {
            System.Diagnostics.Debug.WriteLine("[PortConnectionManager] 초기화됨 (메모리 기반)");
        }

        /// <summary>
        /// 포트 연결 상태 설정
        /// </summary>
        public void SetConnectionState(string stateKey, bool isConnected)
        {
            lock (_lock)
            {
                _connectionStates[stateKey] = isConnected;
                System.Diagnostics.Debug.WriteLine($"[PortConnectionManager] 상태 저장: {stateKey} = {isConnected}");
            }
        }

        /// <summary>
        /// 포트 연결 상태 조회
        /// </summary>
        public bool GetConnectionState(string stateKey)
        {
            lock (_lock)
            {
                if (_connectionStates.TryGetValue(stateKey, out bool state))
                {
                    return state;
                }
                return false; // 기본값: 연결 안 됨
            }
        }

        /// <summary>
        /// 모든 상태 초기화 (프로그램 종료 시 또는 Disconnect All 시)
        /// </summary>
        public void ClearAll()
        {
            lock (_lock)
            {
                _connectionStates.Clear();
                System.Diagnostics.Debug.WriteLine("[PortConnectionManager] 모든 연결 상태 초기화됨");
            }
        }

        /// <summary>
        /// 특정 섹션의 모든 상태 초기화 (예: MTP 또는 IPVS)
        /// </summary>
        public void ClearSection(string sectionPrefix)
        {
            lock (_lock)
            {
                var keysToRemove = new List<string>();
                foreach (var key in _connectionStates.Keys)
                {
                    if (key.StartsWith(sectionPrefix))
                    {
                        keysToRemove.Add(key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    _connectionStates.Remove(key);
                }

                System.Diagnostics.Debug.WriteLine($"[PortConnectionManager] {sectionPrefix} 섹션 초기화됨 ({keysToRemove.Count}개)");
            }
        }

        /// <summary>
        /// 디버그: 모든 연결 상태 출력
        /// </summary>
        public void PrintAllStates()
        {
            lock (_lock)
            {
                System.Diagnostics.Debug.WriteLine("[PortConnectionManager] === 현재 연결 상태 ===");
                foreach (var kvp in _connectionStates)
                {
                    System.Diagnostics.Debug.WriteLine($"  {kvp.Key}: {(kvp.Value ? "연결됨 🟢" : "끊김 ⚪")}");
                }
                System.Diagnostics.Debug.WriteLine($"[PortConnectionManager] 총 {_connectionStates.Count}개");
            }
        }
    }
}
