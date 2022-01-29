using ICities;
using TwitchChatChirper.Utilities;

namespace TwitchChatChirper
{
    /// <summary>
    /// basis for our mod
    /// </summary>
    public class TwitchChatChirper : IUserMod
    { 
        UserOptionFile m_UserOptionFile { get; set; }
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
            m_UserOptionFile = new UserOptionFile();
            
            group.AddTextfield("Twitch Chat OAuth Password [https://twitchapps.com/tmi/]",
                m_UserOptionFile.GetField(UserOptionFile.Field.OAuthPassword) == null ? "" : m_UserOptionFile.GetField(UserOptionFile.Field.OAuthPassword),
                (value) => { m_UserOptionFile.m_OauthValue = value; },
                (value) => { m_UserOptionFile.m_OauthValue = value; }
                );

            group.AddTextfield("Twitch Chat Username [Twitch Username]",
                m_UserOptionFile.GetField(UserOptionFile.Field.Username) == null ? "" : m_UserOptionFile.GetField(UserOptionFile.Field.Username),
                (value) => { m_UserOptionFile.m_usernameValue = value; },
                (value) => { m_UserOptionFile.m_usernameValue = value; }
                );

            group.AddTextfield("Twitch Chat Channel [Twitch Channel to Read]",
                m_UserOptionFile.GetField(UserOptionFile.Field.Channel) == null ? "" : m_UserOptionFile.GetField(UserOptionFile.Field.Channel),
                (value) => { m_UserOptionFile.m_ChannelValue = value; },
                (value) => { m_UserOptionFile.m_ChannelValue = value; }
                );

            group.AddButton("Update Info", () => m_UserOptionFile.UpdateFile());

        }

    }
}