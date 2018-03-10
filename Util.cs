using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows;

namespace qqEd

{
    // Util
    // Generic functions, not dependent on the current project
    // Gestione errori: tramite codici di errore.
    public partial class Util
    {
        // \param string command : command line, including parameters
        // \param string currentDirectory : working directory ('null' if not defined)
        // \param int waitForExit : if > 0, the method waits for the end of the process. The value 'waitForExit' is also the timeout (unit:msec).
        // \param System.Diagnostics.ProcessWindowStyle p_windowStyle: the window style (Minimized, Maximized, ...)
        // \param out string errMsg
        // Return: 'false' in caso di errore
        // Error management: by error code
        public static bool CreateProcess(string command, string currentDirectory, int waitForExit, out string errMsg, System.Diagnostics.ProcessWindowStyle p_windowStyle)
        {
            errMsg = "";
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            try
            {
                string exe = Util.StrListGetFirst(ref command, ' ');

                process.StartInfo.FileName = exe;
                process.StartInfo.Arguments = command;
                process.StartInfo.WorkingDirectory = currentDirectory;
                process.StartInfo.WindowStyle = p_windowStyle;
                process.Start();
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return (false);  // Error 
            }

            if (waitForExit > 0)
            {
                while ((waitForExit > 0)
                    && (process.HasExited == false))
                {
                    System.Threading.Thread.Sleep(100);
                    waitForExit -= 100;
                }
                if (process.HasExited == false)     // If timeout ...
                {
                    errMsg = "Process timeout";
                    return (false);  // Error 
                }
            }

            return (true);  // Ok
        }

        // \param string command : command line, including parameters
        // \param string currentDirectory : working directory ('null' if not defined)
        // \param int waitForExit : if > 0, the method waits for the end of the process. The value 'waitForExit' is also the timeout (unit:msec).
        // \param out string errMsg
        // Return: 'false' in caso di errore
        // Error management: by error code
        public static bool CreateProcess(string command, string currentDirectory, int waitForExit, out string errMsg)
        {
            return CreateProcess(command, currentDirectory, waitForExit, out errMsg, System.Diagnostics.ProcessWindowStyle.Maximized);
        }

        // Overload
        public static bool CreateProcess(string command)
        {
            string errMsg = "";
            return (CreateProcess(command, null, 0, out errMsg, System.Diagnostics.ProcessWindowStyle.Maximized));
        }
        // Overload
        public static bool CreateProcess(string command, string currentDirectory)
        {
            string errMsg = "";
            return (CreateProcess(command, currentDirectory, 0, out errMsg, System.Diagnostics.ProcessWindowStyle.Maximized));
        }
        // Overload
        public static bool CreateProcess(string command, out string errMsg)
        {
            return (CreateProcess(command, null, 0, out errMsg, System.Diagnostics.ProcessWindowStyle.Maximized));
        }

        // Ex. textEditor = "C:\Program Files\Notepad++\notepad++.exe -n%line% %file%"
        // Parameters:
        //      string lineNr = "1" : can be also 'null'
        // Error management: no error returned
        public static bool FileOpenInTextEditor(string fl, string lineNr = "1")
        {
            // if (Settings.Instance == null)
            // {
            //     return (false);
            // }

            // string textEditor = Settings.Instance.ReadPath("TextEditor", true);
            string textEditor = "";

            fl = Util.StrAddQuotes(fl);

            try
            {
                if (lineNr == null)
                {
                    // Ugly patch that works only with Notepad++
                    // Ugly patch, but useful when a file is already opened on Notepad++ and no lineNr is specified
                    // => re-open at the same line as before
                    textEditor = textEditor.Replace(" -n%line%", "");
                }

                if (lineNr == null)
                {
                    lineNr = "1";
                }

                // Set file name
                string cmdLine = textEditor;
                if (cmdLine.IndexOf("%file%") >= 0)
                {
                    cmdLine = cmdLine.Replace("%file%", fl);
                }
                else
                {
                    cmdLine = cmdLine + " " + fl;
                }

                // Set line number
                if (cmdLine.IndexOf("%line%") >= 0)
                {
                    cmdLine = cmdLine.Replace("%line%", lineNr);
                }

                if (CreateProcess(cmdLine) == false)
                {
                    string errMsg = String.Format("Cannot open file:\n{0}" + "\nDetails: ");
                    MessageBox.Show(errMsg,
                        "Cannot open file",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch
            {
                return (false);
            }
            return (true);
        }

        // Given a range of line numbers, return a set of lines from a text file.
        // EOL characters are discarded (input file can be unix-EOL or windows-EOL).
        // Last line of the file, if empty, is discarded.
        // Parameters:
        //      int lineStart : number of the first line to be read (starting from 0)
        //      int lineEnd   : number of the last line to be read (starting from 0)
        // Error management: by error code
        // Return: true if succeeded
        public static bool FileRead(string filePath, int lineStart, int lineEnd, out List<string> lines)
        {
            lines = new List<string>();
            if (!File.Exists(filePath))
            {
                return (false);
            }
            string line = "";
            using (StreamReader sr = File.OpenText(filePath))
            {
                int i = 0;
                while (sr.Peek() >= 0)
                {
                    line = sr.ReadLine();
                    if ((i >= lineStart) && (i <= lineEnd))
                    {
                        lines.Add(line);
                        if (i > lineEnd)
                        {
                            break;
                        }
                    }
                    i++;
                }
                sr.Close();
            }
            if (lines.Count == 0)
            {
                return (false);     // Noting read
            }
            return (true);
        }

        // Return the content of a file as an int array.
        // Error management: by error code
        // Return: true if succeeded
        public static bool FileRead(string filePath, out int[] fileContent)
        {
            fileContent = null;
            if (!File.Exists(filePath))
            {
                return (false);
            }
            StringBuilder sb = new StringBuilder();
            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
                {
                    int length = (int)reader.BaseStream.Length;
                    fileContent = new int[length / sizeof(int)];
                    int pos = 0;
                    int i = 0;
                    while (pos < length)
                    {
                        fileContent[i] = reader.ReadInt32();
                        pos += sizeof(int);
                        i++;
                    }
                }
            }
            catch
            {
                return (false);
            }
            return (true);
        }

        public static void FileWrite(string fileOut, int[] buffer, int bufferSize)
        {
            using (BinaryWriter writer = new BinaryWriter(File.Open(fileOut, FileMode.Create)))
            {
                foreach (int i in buffer)
                {
                    writer.Write(i);
                }
            }
        }

        public static void FileWrite(string fileOut, string fileContent)
        {
            StreamWriter sw = new StreamWriter(fileOut);
            sw.Write(fileContent);
            sw.Close();
        }

        public static void FileWrite(string fileOut, string fileContent, bool append)
        {
            StreamWriter sw = new StreamWriter(fileOut, append);
            sw.Write(fileContent);
            sw.Close();
        }

        public static void FileWrite(string fileOut, string[] fileContent)
        {
            using (StreamWriter file = new StreamWriter(fileOut))
            {
                foreach (string line in fileContent)
                {
                    file.WriteLine(line);
                }
            }

        }

        // Given a byte array, return its digest (format 'hex string').
        public static string Hash_ByteArray2HexDigest(byte[] ba, int maxLength)
        {
            byte[] hash = new MD5CryptoServiceProvider().ComputeHash(ba);
            string outStr = StrByteArray2HexString(ref hash);
            if (maxLength > outStr.Length)
            {
                maxLength = outStr.Length;
            }
            return (outStr.Substring(0, maxLength));
        }

        // Given an array of digests, extract a 'ngram' at a given position, and return the ngram-digest (i.e. digest of an ngram of digest).
        public static int Hash_DigestArray2NGram(int[] digest, int pos, int nGramSize)
        {
            int start = pos - nGramSize + 1;     // 'ngram' is calculated from 'start' to 'pos' (included)
            if (start < 0)
            {
                start = 0;
            }

            // Get 'ngram' as a 'bytes_list'
            List<byte> bytes_list = new List<byte>();
            for (int i = start; i <= pos; i++)
            {
                byte[] bytes = BitConverter.GetBytes(digest[i]);
                bytes_list.AddRange(bytes);
            }

            // From 'bytes_list' to hash
            byte[] hash = new MD5CryptoServiceProvider().ComputeHash(bytes_list.ToArray());
            int ngram = (hash[0] << 24)
                + (hash[1] << 16)
                + (hash[2] << 8)
                + hash[3];  // Use only 4 bytes of the hash code
            return ngram;
        }

        // Given a list of strings, calculate the per-item-digest, that is an array with one digest (int) for each string.
        // Similar to Hash_File2DigestArray().
        // The per-line-digest is used to compare strings / files.
        // Parameters:
        //  string str
        //  bool ignoreDigits
        public static int[] Hash_StringList2DigestArray(List<string> listStr, bool ignoreDigits)
        {
            byte[] line2;
            byte[] hash;
            List<int> digest_list = new List<int>();
            foreach (string l in listStr)
            {
                string line = l;
                if (ignoreDigits)
                {
                    line = StrRemoveDigits(line);
                }

                line2 = ASCIIEncoding.ASCII.GetBytes(line);
                hash = new MD5CryptoServiceProvider().ComputeHash(line2);
                digest_list.Add((hash[0] << 24)
                    + (hash[1] << 16)
                    + (hash[2] << 8)
                    + hash[3]);
            }
            int[] digest = digest_list.ToArray();
            return (digest);
        }

        public static bool RangeOverlapping(int start1, int end1, int start2, int end2)
        {
            if ((end1 < start2) || (start1 > end2))
            {
                return false;
            }
            return true;
        }

        // Least significant byte is on the left (i.e. address 0, 'little endian')
        // Parameters:
        //      string separator : 
        //      bool addQuotes : added to surround the description, only if the decsription contains spaces
        public static string StrByteArray2HexString(ref byte[] array, int length, string separator, bool addQuotes)
        {
            StringBuilder sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(separator);
                sb.Append(array[i].ToString("X2"));
            }
            string sOutput = sb.ToString().Trim();
            if ((addQuotes) && (sOutput.IndexOf(' ') >= 0))
            {
                sOutput = "'" + sOutput + "'";
            }
            return sOutput;
        }

        public static string StrByteArray2HexString(ref byte[] array)
        {
            return (StrByteArray2HexString(ref array, array.Length, "", false));
        }

        // Given a string that contains separated values, returns the first element, removing it from the list
        public static string StrListGetFirst(ref string list, char sep)
        {
            string retVal = "";
            if (list == "")
            {
                retVal = "";
            }
            else if (list.Substring(0, 1) == "\x22")    // If starts with a "
            {
                if (list.Length == 1)
                {
                    retVal = "";
                    list = "";
                }
                else if (list.IndexOf('\x22', 1) >= 0)  // If there a second "
                {
                    retVal = list.Substring(1, list.IndexOf('\x22', 1) - 1);
                    list = list.Substring(list.IndexOf('\x22', 1) + 1);
                    if (list.Trim().StartsWith(sep.ToString()))
                    {
                        list = list.Trim().Substring(1);    // Remove the 'sep'
                    }
                }
                else
                {
                    retVal = list.Substring(1);
                    list = "";
                }
            }
            else
            {
                if (list.IndexOf(sep) >= 0)
                {
                    retVal = list.Substring(0, list.IndexOf(sep));
                    list = list.Substring(list.IndexOf(sep) + 1);
                }
                else
                {
                    retVal = list;
                    list = "";
                }
            }
            return (retVal);
        }

        // Remove decimal and hexadecimal numbers from a string.
        // This is used to ignore digits when comparing strings.
        // Todo: well, this function does more and also contains application specific parts
        public static string StrRemoveDigits(string s)
        {
            // Remove time stamp '33 28777809 250-1:7:96|...'
            while (true)
            {
                Match match = Regex.Match(s, @"([^0-9]*)[0-9]+ [0-9]+ [0-9]+-[0-9]+:[0-9]+:[0-9]+\|(.*)");
                if (!match.Success)
                {
                    break;
                }
                s = match.Groups[1].Value + "{ts}" + match.Groups[2].Value;
            }
            // Remove hexadecimal e.g. '0xABCD'
            while (true)
            {
                Match match = Regex.Match(s, @"(.*)0x[0-9a-fA-F]+(.*)");
                if (!match.Success)
                {
                    break;
                }
                s = match.Groups[1].Value + "{hex}" + match.Groups[2].Value;
            }
            // Remove decimal numbers e.g. 1234
            while (true)
            {
                Match match = Regex.Match(s, @"([^0-9]*)[0-9]+(.*)");
                if (!match.Success)
                {
                    break;
                }
                s = match.Groups[1].Value + "{d}" + match.Groups[2].Value;
            }
            return s;
        }

        // Given a RichTextBox, return the column number of the current caret position
        public static int Rtb_GetCaretColumnPos(RichTextBox rtb)
        {
            TextPointer caretPos = rtb.CaretPosition;
            TextPointer lineStartPos = caretPos.GetLineStartPosition(0);
            return Math.Max(lineStartPos.GetOffsetToPosition(caretPos) - 1, 0);
        }

        // Given a RichTextBox, return the current caret position
        public static int Rtb_GetCaretPos(RichTextBox rtb)
        {
            TextPointer firstVisibleChar = rtb.GetPositionFromPoint(new Point(0, 0), true);
            return firstVisibleChar.DocumentStart.GetOffsetToPosition(firstVisibleChar);
        }

        // Given a RichTextBox, return the line text of the current caret position
        public static string Rtb_GetCaretLineText(RichTextBox rtb)
        {
            TextPointer caretPos = rtb.CaretPosition;
            if (caretPos.GetLineStartPosition(1) == null)
            {
                return "<null>";
            }
            return new TextRange(caretPos.GetLineStartPosition(0), caretPos.GetLineStartPosition(1)).Text;
        }

        // Given a RichTextBox, return the line number of the current caret position
        public static int Rtb_GetCaretLinePos(RichTextBox rtb)
        {
            TextPointer caretPos = rtb.CaretPosition;
            TextPointer caretLineStart = caretPos.GetLineStartPosition(0);
            TextPointer pp = rtb.Document.ContentStart.GetLineStartPosition(0);
            int caretLinePos = 0;
            while (true)
            {
                if (caretLineStart.CompareTo(pp) < 0)
                {
                    break;
                }
                int result;
                pp = pp.GetLineStartPosition(1, out result);
                if (result == 0)
                {
                    break;
                }
                caretLinePos++;
            }
            return caretLinePos;
        }

        public static string Rtb_GetContent(RichTextBox textBox)
        {
            string text_str = new TextRange(textBox.Document.ContentStart, textBox.Document.ContentEnd).Text;

            if (text_str.EndsWith("\r\n"))
            {
                text_str = text_str.Substring(0, text_str.Length - 2);  // Because RichTextBox always adds a new line at the end
            }
            else
            {
                throw new Exception(String.Format("Ma non doveva finire così!"));
            }
            return text_str;
        }

        public static void Rtb_SetContent(RichTextBox textBox, string text)
        {
            textBox.Document.Blocks.Clear();
            Paragraph p = new Paragraph();
            p.Margin = new Thickness(0, 0, 0, 0);
            p.Inlines.Add(new Run(text));
            textBox.Document.Blocks.Add(p);      // This always add an EOL at the end of the file
            textBox.IsUndoEnabled = false;   // To remove programmatical modification from undo-stack
            textBox.IsUndoEnabled = true;
        }

        // Set the content of a RTB by using a StrDelta.
        public static void Rtb_SetContent(RichTextBox textBox, Paragraph p)
        {
            textBox.Document.Blocks.Clear();
            textBox.Document.Blocks.Add(p);
            textBox.IsUndoEnabled = false;   // To remove programmatical modifications from undo-stack
            textBox.IsUndoEnabled = true;
        }

        public static string StrTrimQuotes(string s)
        {
            char[] trimChars = { '"' };
            s = s.Trim(trimChars);
            return (s);
        }

        public static string StrAddQuotes(string s)
        {
            s = Util.StrTrimQuotes(s);
            s = '"' + s + '"';
            return (s);
        }

        private static void Swap<T>(ref T arg1, ref T arg2)
        {
            T temp = arg1;
            arg1 = arg2;
            arg2 = temp;
        }

    }   // public class Util
}
