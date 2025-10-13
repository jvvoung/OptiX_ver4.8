using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OptiX.Common;

namespace OptiX.DLL
{
    /// <summary>
    /// 시퀀스 캐싱 관리 (Singleton, Thread-safe)
    /// OPTIC과 IPVS의 시퀀스를 각각 캐싱하여 멀티스레드 환경에서 안전하게 공유
    /// </summary>
    public sealed class SequenceCacheManager
    {
        #region Singleton
        private static readonly SequenceCacheManager _instance = new SequenceCacheManager();
        public static SequenceCacheManager Instance => _instance;
        private SequenceCacheManager() { }
        #endregion

        #region OPTIC 시퀀스 캐싱
        private Queue<string> _opticSequence = null;
        private bool _opticLoaded = false;
        private readonly object _opticLock = new object();
        #endregion

        #region IPVS 시퀀스 캐싱
        private Queue<string> _ipvsSequence = null;
        private bool _ipvsLoaded = false;
        private readonly object _ipvsLock = new object();
        #endregion

        /// <summary>
        /// OPTIC 시퀀스 로드 및 캐싱 (Thread-safe)
        /// 여러 Zone이 동시에 호출해도 안전하며, 한 번만 파일을 읽습니다.
        /// </summary>
        public void LoadOpticSequence()
        {
            lock (_opticLock)
            {
                if (_opticLoaded && _opticSequence != null)
                {
                    Debug.WriteLine("✅ OPTIC 시퀀스 이미 캐싱됨 - 재로드 생략");
                    return;
                }

                try
                {
                    string seqPath = GlobalDataManager.GetMTPSequencePath();
                    Debug.WriteLine($"📋 OPTIC 시퀀스 로드 시작: {seqPath}");

                    if (!System.IO.File.Exists(seqPath))
                    {
                        Debug.WriteLine($"⚠️ OPTIC 시퀀스 파일 없음: {seqPath}");
                        _opticSequence = new Queue<string>();
                        _opticLoaded = false;
                        return;
                    }

                    var iniManager = new IniFileManager(seqPath);
                    
                    // SEQ_COUNT 읽기
                    string countStr = iniManager.ReadValue("SETTING", "SEQ_COUNT", "0");
                    if (!int.TryParse(countStr, out int count) || count <= 0)
                    {
                        Debug.WriteLine($"⚠️ OPTIC SEQ_COUNT가 유효하지 않음: {countStr}");
                        _opticSequence = new Queue<string>();
                        _opticLoaded = false;
                        return;
                    }

                    // 시퀀스 로드
                    _opticSequence = new Queue<string>();
                    for (int i = 0; i < count; i++)
                    {
                        string seqKey = $"SEQ{i:D2}"; // SEQ00, SEQ01, SEQ02, ...
                        string seqValue = iniManager.ReadValue("SEQ", seqKey, "");
                        
                        if (!string.IsNullOrEmpty(seqValue))
                        {
                            _opticSequence.Enqueue(seqValue);
                            Debug.WriteLine($"   ✅ {seqKey} = {seqValue}");
                        }
                        else
                        {
                            Debug.WriteLine($"   ⚠️ {seqKey} 비어있음 - 건너뜀");
                        }
                    }

                    _opticLoaded = true;
                    Debug.WriteLine($"✅ OPTIC 시퀀스 캐싱 완료: {_opticSequence.Count}개");
                    Common.ErrorLogger.Log($"OPTIC 시퀀스 캐싱 완료: {_opticSequence.Count}개", Common.ErrorLogger.LogLevel.INFO);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ OPTIC 시퀀스 로드 실패: {ex.Message}");
                    ErrorLogger.LogException(ex, "OPTIC 시퀀스 로드 중 예외 발생");
                    _opticSequence = new Queue<string>();
                    _opticLoaded = false;
                }
            }
        }

        /// <summary>
        /// IPVS 시퀀스 로드 및 캐싱 (Thread-safe)
        /// 여러 Zone이 동시에 호출해도 안전하며, 한 번만 파일을 읽습니다.
        /// </summary>
        public void LoadIPVSSequence()
        {
            lock (_ipvsLock)
            {
                if (_ipvsLoaded && _ipvsSequence != null)
                {
                    Debug.WriteLine("✅ IPVS 시퀀스 이미 캐싱됨 - 재로드 생략");
                    return;
                }

                try
                {
                    string seqPath = GlobalDataManager.GetIPVSSequencePath();
                    Debug.WriteLine($"📋 IPVS 시퀀스 로드 시작: {seqPath}");

                    if (!System.IO.File.Exists(seqPath))
                    {
                        Debug.WriteLine($"⚠️ IPVS 시퀀스 파일 없음: {seqPath} - 기본 시퀀스 사용");
                        _ipvsSequence = GetDefaultIPVSSequence();
                        _ipvsLoaded = true;
                        return;
                    }

                    var iniManager = new IniFileManager(seqPath);
                    
                    // SEQ_COUNT 읽기 ([SETTING] 섹션에서 읽음 - OPTIC과 동일)
                    string countStr = iniManager.ReadValue("SETTING", "SEQ_COUNT", "5");
                    if (!int.TryParse(countStr, out int count) || count <= 0)
                    {
                        Debug.WriteLine($"⚠️ IPVS SEQ_COUNT가 유효하지 않음: {countStr} - 기본 시퀀스 사용");
                        _ipvsSequence = GetDefaultIPVSSequence();
                        _ipvsLoaded = true;
                        return;
                    }

                    // 시퀀스 로드 (SEQ00, SEQ01, SEQ02... 형식 - OPTIC과 동일)
                    _ipvsSequence = new Queue<string>();
                    for (int i = 0; i < count; i++)
                    {
                        string seqKey = $"SEQ{i:D2}"; // SEQ00, SEQ01, SEQ02, ...
                        string seqValue = iniManager.ReadValue("SEQ", seqKey, "");
                        
                        if (!string.IsNullOrEmpty(seqValue))
                        {
                            _ipvsSequence.Enqueue(seqValue);
                            Debug.WriteLine($"   ✅ {seqKey} = {seqValue}");
                        }
                        else
                        {
                            Debug.WriteLine($"   ⚠️ {seqKey} 비어있음 - 건너뜀");
                        }
                    }

                    _ipvsLoaded = true;
                    Debug.WriteLine($"✅ IPVS 시퀀스 캐싱 완료: {_ipvsSequence.Count}개");
                    Common.ErrorLogger.Log($"IPVS 시퀀스 캐싱 완료: {_ipvsSequence.Count}개", Common.ErrorLogger.LogLevel.INFO);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ IPVS 시퀀스 로드 실패: {ex.Message} - 기본 시퀀스 사용");
                    ErrorLogger.LogException(ex, "IPVS 시퀀스 로드 중 예외 발생");
                    _ipvsSequence = GetDefaultIPVSSequence();
                    _ipvsLoaded = true;
                }
            }
        }

        /// <summary>
        /// OPTIC 시퀀스 복사본 반환 (Thread-safe)
        /// 각 Zone이 독립적으로 사용할 수 있도록 복사본을 반환합니다.
        /// 원본은 보호되므로 여러 Zone이 동시에 사용해도 안전합니다.
        /// </summary>
        public Queue<string> GetOpticSequenceCopy()
        {
            lock (_opticLock)
            {
                if (_opticSequence == null || !_opticLoaded)
                {
                    Debug.WriteLine("⚠️ OPTIC 시퀀스 미로드 - 빈 Queue 반환");
                    return new Queue<string>();
                }
                
                // 각 Zone이 독립적으로 사용할 복사본 반환
                var copy = new Queue<string>(_opticSequence);
                Debug.WriteLine($"📋 OPTIC 시퀀스 복사본 반환: {copy.Count}개");
                return copy;
            }
        }

        /// <summary>
        /// IPVS 시퀀스 List 반환 (Thread-safe)
        /// 각 Zone이 독립적으로 사용할 수 있도록 List로 반환합니다.
        /// </summary>
        public List<string> GetIPVSSequenceList()
        {
            lock (_ipvsLock)
            {
                if (_ipvsSequence == null || !_ipvsLoaded)
                {
                    Debug.WriteLine("⚠️ IPVS 시퀀스 미로드 - 기본 시퀀스 반환");
                    return GetDefaultIPVSSequence().ToList();
                }
                
                var list = _ipvsSequence.ToList();
                Debug.WriteLine($"📋 IPVS 시퀀스 List 반환: {list.Count}개");
                return list;
            }
        }

        /// <summary>
        /// IPVS 기본 시퀀스 (파일 로드 실패 시 사용)
        /// </summary>
        private Queue<string> GetDefaultIPVSSequence()
        {
            var queue = new Queue<string>();
            queue.Enqueue("PGTurn");
            queue.Enqueue("MEASTurn");
            queue.Enqueue("PGPattern");
            queue.Enqueue("MEAS");
            queue.Enqueue("IPVS_TEST");
            return queue;
        }

        /// <summary>
        /// 캐시 초기화 (테스트용)
        /// </summary>
        public void ClearCache()
        {
            lock (_opticLock)
            {
                _opticSequence = null;
                _opticLoaded = false;
                Debug.WriteLine("🔄 OPTIC 시퀀스 캐시 초기화");
            }

            lock (_ipvsLock)
            {
                _ipvsSequence = null;
                _ipvsLoaded = false;
                Debug.WriteLine("🔄 IPVS 시퀀스 캐시 초기화");
            }
        }
    }
}

