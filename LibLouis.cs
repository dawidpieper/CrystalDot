using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace CrystalDot
{
    public class LibLouis
    {

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        static LibLouis()
        {
            var dir = System.AppContext.BaseDirectory;
            var is64 = IntPtr.Size == 8;
            var libDir = dir + (is64 ? "\\lib64\\" : "\\lib32\\");
            LoadLibrary(libDir + "liblouis.dll");
        }

        [DllImport("liblouis.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        private static extern int lou_translateString(
            [MarshalAs(UnmanagedType.LPStr)] string tableList,
            IntPtr inbuf,
            ref int inlen,
            IntPtr outbuf,
            ref int outlen,
            IntPtr typeform,
            IntPtr spacing,
            int mode);

        [DllImport("liblouis.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        private static extern int lou_charToDots(
            [MarshalAs(UnmanagedType.LPStr)] string tableList,
            IntPtr inbuf,
            IntPtr outbuf,
            int length,
            int mode);

        public static string TranslateToBraille(string brailleTable, string input)
        {
            int inlen = input.Length;
            int outlen = 2 * inlen; // output buffer should be at least twice as large as input
            var outbuf = new byte[outlen * 4];

            IntPtr inbufPtr = Marshal.AllocHGlobal(inlen * 4);
            IntPtr outbufPtr = Marshal.AllocHGlobal(outlen * 4);

            Marshal.Copy(Encoding.UTF32.GetBytes(input), 0, inbufPtr, inlen * 4);

            int result = lou_translateString(
                brailleTable,
                inbufPtr,
                ref inlen,
                outbufPtr,
                ref outlen,
                IntPtr.Zero,
                IntPtr.Zero,
                0);

            Marshal.Copy(outbufPtr, outbuf, 0, outlen * 4);
            Marshal.FreeHGlobal(outbufPtr);
            Marshal.FreeHGlobal(inbufPtr);

            if (result != 1) return "";

            return Encoding.UTF32.GetString(outbuf, 0, outlen * 4);
        }

        public static byte[] BrailleToDots(string brailleTable, string input)
        {
            int length = input.Length;
            var outbuf = new byte[length * 4];

            IntPtr inbufPtr = Marshal.AllocHGlobal(length * 4);
            IntPtr outbufPtr = Marshal.AllocHGlobal(length * 4);

            Marshal.Copy(Encoding.UTF32.GetBytes(input), 0, inbufPtr, length * 4);

            int result = lou_charToDots(
                brailleTable,
                inbufPtr,
                outbufPtr,
                length,
                0);


            Marshal.Copy(outbufPtr, outbuf, 0, length * 4);
            Marshal.FreeHGlobal(outbufPtr);
            Marshal.FreeHGlobal(inbufPtr);

            var r = new byte[length];
            if (result != 1) return r;
            for (int i = 0; i < length; ++i) r[i] = outbuf[i * 4];

            return r;
        }

        public class BrailleTable
        {
            public string Location;
            public string? DisplayName;
            public string? Language;
            public override string? ToString() => DisplayName;
            public BrailleTable(string location)
            {
                Location = location;
            }
        }

        public static BrailleTable[] GetTables()
        {
            var dir = System.AppContext.BaseDirectory;
            var tables = new List<BrailleTable>();
            var tblFiles = Directory.GetFiles(dir + "\\liblouis/tables", "*.tbl");
            foreach (var filePath in tblFiles)
            {
                var fileContent = File.ReadAllText(filePath);
                var displayNameLine = fileContent.Split('\n').FirstOrDefault(line => line.Contains("#-display-name:"));
                if (displayNameLine != null)
                {
                    var displayName = displayNameLine.Split(':').Last().Trim();
                    var languageLine = fileContent.Split('\n').FirstOrDefault(line => line.Contains("#+language:"));
                    var language = languageLine?.Split(':').Last().Trim();
                    var t = new BrailleTable(filePath);
                    t.DisplayName = displayName;
                    t.Language = language;
                    tables.Add(t);
                }
            }
            return tables.ToArray();
        }
    }
}