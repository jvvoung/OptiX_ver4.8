using System;
using System.Runtime.InteropServices;

namespace OptiXClient
{
    /// <summary>
    /// OptiX UI와 Client 간 통신 프로토콜 구조체 정의
    /// </summary>
    public static class CommunicationProtocol
    {
        // 메시지 ID 정의
        public const int SMID_IPVS_START = 1;
        public const int SMID_OT_START = 2;

        /// <summary>
        /// IPVS 시작 요청 구조체
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct SMPACK_IPVS_START
        {
            public int msgID;           // SMID_IPVS_START
            public int SetLength;       // sizeof(SMPACK_IPVS_START)
            
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] cETX;         // ETX
            
            public byte select;         // 선택
            public byte currentPoint;   // 현재 포인트
            public byte TotalPoint;     // 전체 포인트
            
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] InnerID;      // Inner ID
            
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] McrID;        // MCR ID
            
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] cETX2;        // ETX

            public SMPACK_IPVS_START(byte select = 0, byte currentPoint = 0, byte totalPoint = 0)
            {
                this.msgID = SMID_IPVS_START;
                this.SetLength = Marshal.SizeOf(typeof(SMPACK_IPVS_START));
                this.cETX = new byte[32];
                this.select = select;
                this.currentPoint = currentPoint;
                this.TotalPoint = totalPoint;
                this.InnerID = new byte[32];
                this.McrID = new byte[32];
                this.cETX2 = new byte[32];
            }
        }

        /// <summary>
        /// OPTIC 시작 요청 구조체 (QT_START)
        /// C++ 원본: char lotID[2][32], char innerID[2][32], char mcrID[2][32]
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct SMPACK_OT_START
        {
            public int msgID;           // SMID_OT_START
            public int SetLength;       // sizeof(SMPACK_OT_START) - sizeof(SMPACK_OT_REQ_HEAD)
            
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] cETX;         // ETX (헤더)

            public byte select;         // 선택
            
            // lotID[2][32] - 2차원 배열을 1차원으로 flatten
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]  // 2 * 32 = 64
            public byte[] lotID;        // Lot ID [2][32]
            
            // innerID[2][32]
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]  // 2 * 32 = 64
            public byte[] innerID;      // Inner ID [2][32]
            
            // mcrID[2][32]
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]  // 2 * 32 = 64
            public byte[] mcrID;        // MCR ID [2][32]
            
            public byte cETX2;          // ETX (1바이트)

            public SMPACK_OT_START(byte select = 0)
            {
                this.msgID = SMID_OT_START;
                this.SetLength = 0; // 나중에 계산
                this.cETX = new byte[32];
                this.select = select;
                this.lotID = new byte[64];      // 2 * 32
                this.innerID = new byte[64];    // 2 * 32
                this.mcrID = new byte[64];      // 2 * 32
                this.cETX2 = 0;
            }

            /// <summary>
            /// lotID[index]에 문자열 설정 (index: 0 또는 1)
            /// </summary>
            public void SetLotID(int index, string value)
            {
                if (index < 0 || index > 1) return;
                byte[] bytes = CommunicationProtocol.StringToByteArray(value, 32);
                Array.Copy(bytes, 0, lotID, index * 32, 32);
            }

            /// <summary>
            /// innerID[index]에 문자열 설정 (index: 0 또는 1)
            /// </summary>
            public void SetInnerID(int index, string value)
            {
                if (index < 0 || index > 1) return;
                byte[] bytes = CommunicationProtocol.StringToByteArray(value, 32);
                Array.Copy(bytes, 0, innerID, index * 32, 32);
            }

            /// <summary>
            /// mcrID[index]에 문자열 설정 (index: 0 또는 1)
            /// </summary>
            public void SetMcrID(int index, string value)
            {
                if (index < 0 || index > 1) return;
                byte[] bytes = CommunicationProtocol.StringToByteArray(value, 32);
                Array.Copy(bytes, 0, mcrID, index * 32, 32);
            }

            /// <summary>
            /// lotID[index] 문자열 가져오기 (index: 0 또는 1)
            /// </summary>
            public string GetLotID(int index)
            {
                if (index < 0 || index > 1) return "";
                byte[] bytes = new byte[32];
                Array.Copy(lotID, index * 32, bytes, 0, 32);
                return CommunicationProtocol.ByteArrayToString(bytes);
            }

            /// <summary>
            /// innerID[index] 문자열 가져오기 (index: 0 또는 1)
            /// </summary>
            public string GetInnerID(int index)
            {
                if (index < 0 || index > 1) return "";
                byte[] bytes = new byte[32];
                Array.Copy(innerID, index * 32, bytes, 0, 32);
                return CommunicationProtocol.ByteArrayToString(bytes);
            }

            /// <summary>
            /// mcrID[index] 문자열 가져오기 (index: 0 또는 1)
            /// </summary>
            public string GetMcrID(int index)
            {
                if (index < 0 || index > 1) return "";
                byte[] bytes = new byte[32];
                Array.Copy(mcrID, index * 32, bytes, 0, 32);
                return CommunicationProtocol.ByteArrayToString(bytes);
            }
        }

        /// <summary>
        /// 구조체를 바이트 배열로 변환
        /// </summary>
        public static byte[] StructureToByteArray<T>(T structure) where T : struct
        {
            int size = Marshal.SizeOf(structure);
            byte[] arr = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(structure, ptr, true);
                Marshal.Copy(ptr, arr, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return arr;
        }

        /// <summary>
        /// 바이트 배열을 구조체로 변환
        /// </summary>
        public static T ByteArrayToStructure<T>(byte[] byteArray) where T : struct
        {
            T structure;
            int size = Marshal.SizeOf(typeof(T));
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(byteArray, 0, ptr, size);
                structure = (T)Marshal.PtrToStructure(ptr, typeof(T));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return structure;
        }

        /// <summary>
        /// 문자열을 바이트 배열로 변환 (고정 크기)
        /// </summary>
        public static byte[] StringToByteArray(string str, int size)
        {
            byte[] result = new byte[size];
            if (!string.IsNullOrEmpty(str))
            {
                byte[] strBytes = System.Text.Encoding.ASCII.GetBytes(str);
                int copyLength = Math.Min(strBytes.Length, size);
                Array.Copy(strBytes, result, copyLength);
            }
            return result;
        }

        /// <summary>
        /// 바이트 배열을 문자열로 변환
        /// </summary>
        public static string ByteArrayToString(byte[] byteArray)
        {
            int nullIndex = Array.IndexOf(byteArray, (byte)0);
            if (nullIndex >= 0)
            {
                return System.Text.Encoding.ASCII.GetString(byteArray, 0, nullIndex);
            }
            return System.Text.Encoding.ASCII.GetString(byteArray);
        }
    }
}

