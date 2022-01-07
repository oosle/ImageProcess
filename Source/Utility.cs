using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Drawing;

namespace ImageProcess
{
    public static class Utility
    {
        #region Useful global properties derived from executing assembly via reflection

        public static string pConfigFile
        {
            get
            {
                string sConfig = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
                return (sConfig);
            }
        }

        public static string pLogFile
        {
            get
            {
                string sPath = Path.GetDirectoryName(pConfigFile);
                string sLog = Path.GetFileNameWithoutExtension(pConfigFile);
                sLog = Path.GetFileNameWithoutExtension(sLog) + ".log";
                return (Path.Combine(sPath, sLog));
            }
        }

        public static String pAppStartPath
        {
            get
            {
                string sPath = Path.GetDirectoryName(pConfigFile);
                return (sPath);
            }
        }

        public static string pAppName
        {
            get
            {
                string sName = Assembly.GetEntryAssembly().GetName().Name.ToString();
                string sVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
                return (sName + " v" + sVersion);
            }
        }

        public static string pAssembly
        {
            get
            {
                return (Assembly.GetEntryAssembly().GetName().Name);
            }
        }

        public static Version pVersion
        {
            get
            {
                return (Assembly.GetEntryAssembly().GetName().Version);
            }
        }

        public static string pDBConnect
        {
            get
            {
                // Database connection string pulled from config file if needed
                string db = ConfigurationManager.ConnectionStrings?["Connection"]?.ToString() ?? string.Empty;
                return (db);
            }
        }

        public static long GetTickCount()
        {
            return ((long)Environment.TickCount);
        }

        #endregion

        #region Add some simple and useful extension methods to the char class

        public static bool IsLower(this char ch)
        {
            return ((ch >= 'a') && (ch <= 'z'));
        }

        public static bool IsUpper(this char ch)
        {
            return ((ch >= 'A') && (ch <= 'Z'));
        }

        public static bool IsDigit(this char ch)
        {
            return ((ch >= '0') && (ch <= '9'));
        }

        public static char ToLower(this char ch)
        {
            return (char.ToLower(ch));
        }

        public static char ToUpper(this char ch)
        {
            return (char.ToUpper(ch));
        }

        #endregion

        #region Add some simple and useful extension methods to the string class

        public static bool IsNullOrEmpty(this string str)
        {
            return (IsNullOrWhiteSpace(str));
        }

        public static bool IsNullOrWhiteSpace(this string str)
        {
            string s = string.Empty;
            if (str != null) { s = str.Trim(); }

            return (s == null || s == string.Empty);
        }

        public static bool IsAlpha(this string str)
        {
            Regex reg = new Regex(@"^[a-zA-Z]*$");

            return (reg.IsMatch(str));
        }

        public static bool IsNumeric(this string str)
        {
            Regex reg = new Regex("^[0-9]*$");

            return (reg.IsMatch(str));
        }

        public static bool IsSpecial(this string str)
        {
            Regex reg = new Regex(@"^[ \|!#$%&/()=?»«@£§€{}.;'<>_,"":-]*$");

            return (reg.IsMatch(str));
        }

        public static bool IsAlphaNumeric(this string str)
        {
            Regex reg = new Regex(@"^[0-9a-zA-Z]*$");

            return (reg.IsMatch(str));
        }

        public static bool IsAlphaNumericSpecial(this string str)
        {
            Regex reg = new Regex(@"^[0-9a-zA-Z \|!#$%&/()=?»«@£§€{}.;'<>_,"":-]*$");

            return (reg.IsMatch(str));
        }

        public static bool IsValidUrl(this string text)
        {
            Regex reg = new Regex(@"http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?");

            return (reg.IsMatch(text));
        }

        public static bool IsValidEmail(this string email)
        {
            Regex reg = new Regex(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$");

            return (reg.IsMatch(email));
        }

        #endregion

        #region Some useful global utilities, nice little SQL beautifier here

        /// <summary>
        /// Recursively find and replace text in all files with a regular expression mask.
        /// </summary>
        public static void ReplaceText(string root, string files, string regex, string replace)
        {
            var regExp = new Regex(regex);

            foreach (var file in Directory.GetFiles(root, files, SearchOption.AllDirectories))
            {
                var contents = File.ReadAllText(file);
                var newContent = regExp.Replace(contents, replace);
                File.WriteAllText(file, newContent);
            }
        }

        /// <summary>
        /// ToTitleCase() does a pretty bad job on SQL field names, customized version here.
        /// </summary>
        private static int BlankColCount = 0;
        public static string ToTitleCaseSQL(this string str)
        {
            string s = str;
            string[] data = null;
            string[] nums = { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine" };

            data = s.Split(' ', '_', '-', '.', '+', '&');
            s = string.Empty;

            // Do some intelligent handling of capitalization before filtering out special chars
            foreach (var item in data)
            {
                if (!item.IsNullOrEmpty())
                {
                    if (item?.Length > 0 && item[0].IsLower())
                    {
                        s += item?[0].ToUpper() + item?.Substring(1);
                    }
                    else if (item[0].IsDigit())
                    {
                        // Sometimes get SQL names that start with a number, C# needs this to be fixed!
                        int value = ((byte)item[0]) - 48;
                        if (value >= 0 && value <= 9)
                        {
                            if (item.Length > 2)
                                s += nums?[value] + item?[1].ToUpper() + item?.Substring(2);
                            else if (item.Length == 2)
                                s += nums?[value] + item?[1].ToUpper();
                            else if (item.Length == 1)
                                s += nums?[value];
                        }
                        else
                        {
                            if (item.Length == 2)
                                s += item?[1].ToUpper() + item?.Substring(2);
                            else if (item.Length == 1)
                                s += nums?[value];
                        }
                    }
                    else if (!item.IsNullOrEmpty() && item.Length > 0 && item[0] == '@')
                    {
                        s += string.Format("@{0}{1}", item?[1].ToUpper(), item?.Substring(2));
                    }
                    else
                    {
                        s += item;
                    }
                }
            }
            s = Regex.Replace(s, @"[^@0-9a-zA-Z]+", "");

            // Could potentially return blank string or numeric column name, ensure we don't
            if (s.IsNullOrEmpty() || s.IsNumeric())
            {
                s = string.Format("BlankColumn{0}", BlankColCount++);
            }

            return (s);
        }

        #endregion

        #region Bitmap library utility dependancies needed to use the bitmap library

        public static byte[] ToByteArray(this string str)
        {
            return (Encoding.ASCII.GetBytes(str));
        }

        /// <summary>
        /// Serialize object to a byte array, not all objects are serializable, can produce wierd results.
        /// </summary>
        public static byte[] ToByteArray<T>(this T obj)
        {
            if (obj == null) { return null; }

            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return (ms.ToArray());
            }
        }

        /// <summary>
        /// Uses pointer manipulation to locate a byte stream in a larger array of bytes, returns offset or -1.
        /// </summary>
        public static unsafe long FindByte(this byte[] Haystack, byte[] Needle)
        {
            unsafe
            {
                fixed (byte* H = Haystack) fixed (byte* N = Needle)
                {
                    long i = 0;
                    for (byte* hNext = H, hEnd = H + Haystack.LongLength; hNext < hEnd; i++, hNext++)
                    {
                        bool Found = true;
                        for (byte* hInc = hNext, nInc = N, nEnd = N + Needle.LongLength;
                            Found && nInc < nEnd; Found = *nInc == *hInc, nInc++, hInc++) ;
                        if (Found) return (i);
                    }
                    return (-1);
                }
            }
        }

        /// <summary>
        /// Takes a bounding set of points and returns the minimum and maximum X,Y values. 
        /// </summary>
        public static void GetPointBounds(this System.Drawing.Drawing2D.Matrix matrix,
            PointF[] points, out float xmin, out float xmax, out float ymin, out float ymax)
        {
            xmin = points[0].X;
            xmax = xmin;
            ymin = points[0].Y;
            ymax = ymin;

            foreach (PointF point in points)
            {
                if (xmin > point.X) xmin = point.X;
                if (xmax < point.X) xmax = point.X;
                if (ymin > point.Y) ymin = point.Y;
                if (ymax < point.Y) ymax = point.Y;
            }
        }

        #endregion
    }
}
