using ICities;
using TwitchChatChirper.Utilities;

namespace TwitchChatChirper
{
    /// <summary>
    /// basis for our mod
    /// </summary>
    public class TwitchChatChirper : IUserMod
    {
        /// <summary>
        /// used to interact with the config user options config file for this mod
        /// </summary>
        UserOptionFile m_UserOperationFile;
        
        public string Name
        {
            get { return "Twitch Chat Chirper"; }
        }
        public string Description
        {
            get { return "Incoming chirps will reflect your currently active twitch chat."; }
        }

        /// <summary>
        /// adds an options menu for this mods so that the user can supply their twitch information
        /// </summary>
        /// <param name="helper"></param>
        public void OnSettingsUI(UIHelperBase helper)
        {
            UIHelperBase group = helper.AddGroup("Twitch Chat Chirper Group");
            m_UserOperationFile = new UserOptionFile();

            group.AddTextfield("Twitch Chat OAuth Password [https://twitchapps.com/tmi/]",
                m_UserOperationFile.GetField(UserOptionFile.Field.OAuthPassword),
                (value) => { },
                (value) => m_UserOperationFile.SetField(UserOptionFile.Field.OAuthPassword, value));

            group.AddTextfield("Twitch Chat Username [Twitch Username]",
                m_UserOperationFile.GetField(UserOptionFile.Field.Username),
                (value) => { },
                (value) => m_UserOperationFile.SetField(UserOptionFile.Field.Username, value));

            group.AddTextfield("Twitch Chat Channel [Twitch Channel to Read]",
                m_UserOperationFile.GetField(UserOptionFile.Field.Channel),
                (value) => { },
                (value) => m_UserOperationFile.SetField(UserOptionFile.Field.Channel, value));

        }

    }
}