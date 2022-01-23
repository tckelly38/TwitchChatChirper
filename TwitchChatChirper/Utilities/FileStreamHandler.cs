using System;
using System.IO;

namespace TwitchChatChirper.Utilities
{
    /// <summary>
    /// this is a low level class that allows us to interact with the file
    /// </summary>
    internal class FileStreamHandler
    {
        /// <summary>
        /// the file this class will be interacting with
        /// </summary>
        private string m_file;
        
        /// <summary>
        /// you can only use this class once you supply a file name
        /// creates file
        /// </summary>
        /// <param name="file">file name that this file stream interacts with</param>
        public FileStreamHandler(string file)
        {
            m_file = file;
            CreateFile();
        }

        /// <summary>
        /// Performs logic for updating a field in the file
        /// </summary>
        /// <param name="field">field we wish to update</param>
        /// <param name="newLine">content to replace with</param>
        public void UpdateField(string field, string newLine)
        {
            int lineNo = GetLineNumber(field);
            if (lineNo > -1)
            {
                LineChanger(newLine, lineNo);
            }

        }

        /// <summary>
        /// retieves the value at the given field
        /// </summary>
        /// <param name="field">which field/key in the file we wish to obtain the value of</param>
        /// <returns>value of the field/key</returns>
        public string GetValue(string field)
        {
            string[] arrLine = File.ReadAllLines(m_file);

            for (int i = 0; i < arrLine.Length; i++)
            {
                if (arrLine[i].Contains(field))
                {
                    return arrLine[i].Split(new[] { ':' }, 2)[1];
                }
            }
            return "";
        }

        /// <summary>
        /// we append the line to the file (at the end)
        /// </summary>
        /// <param name="line">line to append</param>
        public void AppendLine(string line)
        {
            File.AppendAllText(m_file, line + Environment.NewLine);
        }

        /// <summary>
        /// if file DNE then create (and close) o.w. do nothing
        /// </summary>
        private void CreateFile()
        {
            if (!File.Exists(m_file))
            {
                File.Create(m_file).Close();
            }
        }

        /// <summary>
        /// replaces line at lineToEdit with the newText
        /// this will completely REWRITE the file so be considerate (if dealing with large file then this is BAD)
        /// </summary>
        /// <param name="newText">text/line to add</param>
        /// <param name="lineToEdit">the line number (0-index based) to replace in the underlying file</param>
        private void LineChanger(string newText, int lineToEdit)
        {
            string[] arrLine = File.ReadAllLines(m_file);
            arrLine[lineToEdit] = newText;
            File.WriteAllLines(m_file, arrLine);
        }

        /// <summary>
        /// retrieves the line number (0-index based) that contains the provided field
        /// </summary>
        /// <param name="field">which field in the file we are inquiring about</param>
        /// <returns>line number of the field</returns>
        private int GetLineNumber(string field)
        {
            string[] arrLine = File.ReadAllLines(m_file);

            for (int i = 0; i < arrLine.Length; i++)
            {
                if (arrLine[i].Contains(field))
                    return i;
            }
            return -1;

        }
    }
}
