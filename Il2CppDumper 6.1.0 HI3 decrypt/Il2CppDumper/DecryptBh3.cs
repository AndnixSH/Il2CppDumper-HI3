using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Il2CppDumper
{
    class DecryptTool
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private extern static IntPtr LoadLibrary(string path);

        //获取函数地址
        [DllImport("kernel32.dll", SetLastError = true)]
        private extern static IntPtr GetProcAddress(IntPtr lib, string funcName);

        //释放相应的库
        [DllImport("kernel32.dll")]
        private extern static bool FreeLibrary(IntPtr lib);

        //解密global-metadata.dat，获取字符串时要先调用一次
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr DecryptMetadata(byte[] data, int length);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr GetStringFromIndex(byte[] data, uint index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr GetStringLiteralFromIndex(byte[] data, uint index,ref int len);

        static IntPtr libHndle;
        static IntPtr UnityMainAddr;
        static int UnityMainOffset = 0x00AE82F0;
        static IntPtr Dmetadata;
        static int DMetadataOffset = 0x002B2A0;
        static IntPtr Gstring;
        static int GstringOffset = 0x0031B00;
        static IntPtr GStrL;
        static int GStrLOffset = 0x00353A0;

        static DecryptMetadata DecryptMetadataf;
        static GetStringFromIndex GetStringFromIndexf;
        static GetStringLiteralFromIndex GetStringLiteralFromIndexf;
        static byte[] metadatabytes;

        static public void LoadLibAndFunc()
        {
            libHndle = LoadLibrary("UnityPlayer.dll");
            UnityMainAddr = GetProcAddress(libHndle, "UnityMain");
            Dmetadata = UnityMainAddr - UnityMainOffset + DMetadataOffset;
            Gstring = UnityMainAddr - UnityMainOffset + GstringOffset;
            GStrL = UnityMainAddr - UnityMainOffset + GStrLOffset;
            DecryptMetadataf = (DecryptMetadata)Marshal.GetDelegateForFunctionPointer(Dmetadata, typeof(DecryptMetadata));
            GetStringFromIndexf = (GetStringFromIndex)Marshal.GetDelegateForFunctionPointer(Gstring, typeof(GetStringFromIndex));
            GetStringLiteralFromIndexf = (GetStringLiteralFromIndex)Marshal.GetDelegateForFunctionPointer(GStrL, typeof(GetStringLiteralFromIndex));
        }

        static public byte[] Decrypt(byte[] data)
        {
            if (libHndle == IntPtr.Zero) LoadLibAndFunc();
            IntPtr dataoutp = DecryptMetadataf(data, data.Length);
            byte[] dataout = new byte[data.Length];
            Marshal.Copy(dataoutp, dataout, 0, dataout.Length);
            metadatabytes = dataout;
            return dataout;
        }
        static public string GetString(uint index)
        {
            if (libHndle == IntPtr.Zero) LoadLibAndFunc();
            IntPtr strp = GetStringFromIndexf(metadatabytes, index);
            return Marshal.PtrToStringAnsi(strp);
        }

        static public string GetStringLiteral(uint index)
        {
            if (libHndle == IntPtr.Zero) LoadLibAndFunc();
            int len = 0;
            IntPtr strp = GetStringLiteralFromIndexf(metadatabytes, index, ref len);
            byte[] strbytes = new byte[len];
            Marshal.Copy(strp, strbytes, 0, len);
            string s = Encoding.UTF8.GetString(strbytes);
            return s;
        }

    }
}
