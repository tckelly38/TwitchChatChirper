using System;

namespace TwitchChatChirper.Utilities
{

    /// <summary>
    /// This class essentially acts as a wrapper for the FileStreamHandler class
    /// we write and read information from the config file related to the user entered
    /// fields
    /// </summary>
    public class UserOptionFile
    {
        /// <summary>
        /// filestream low-level interaction
        /// </summary>
        private FileStreamHandler m_FileStreamHandler;


        public string m_OauthValue, m_usernameValue, m_ChannelValue;

        /// <summary>
        /// each entry coorelatates with a field in the config file
        /// </summary>
        public enum Field
        {
            OAuthPassword,
            Username,
            Channel
        }

        /// <summary>
        /// establish the file stream reader
        /// creates file (if DNE, then create, o.w. do nothing)
        /// </summary>
        public UserOptionFile()
        {
            m_FileStreamHandler = new FileStreamHandler(GlobalConfiguration.m_FileLocation);
        }

       
        /// <summary>
        /// retrieve the requested info from the file
        /// </summary>
        /// <param name="field">field you wish to aquire</param>
        /// <returns>string value of the "key/value" pair from file</returns>
        public string GetField(Field field)
        {
            return m_FileStreamHandler.GetValue(field.ToString());
        }

        /// <summary>
        /// this should be called when the user makes an insertion into the UI settings for this mod
        /// wiil update the config file with the info
        ///
        /// specifically, if the field does not exist we will just add  the new field to the file
        /// if the field does exist then we will "update" that field (under the hood we literally rewrite
        /// the enitre file to make that update :))
        /// 
        /// will add or update line in file to look like <field:value>
        /// </summary>
        /// <param name="field">key value or field you want to set</param>
        /// <param name="value">value to match with field</param>
        /// <exception cref="ArgumentException"></exception>
        public void SetField(Field field, string value)
        {
            // TODO: should we sha256 oauth password field?
            if (GetField(field) == null)
                m_FileStreamHandler.AppendLine(field.ToString() + ":" + value.Trim());
            else
                m_FileStreamHandler.UpdateField(field.ToString(), value.Trim());
        }
        

        public void UpdateFile()
        {
            //m_FileStreamHandler.EraseFile();
            SetField(Field.OAuthPassword, m_OauthValue);
            SetField(Field.Username, m_usernameValue);
            SetField(Field.Channel, m_ChannelValue);
        }
    }
}
