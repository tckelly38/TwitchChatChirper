namespace TwitchChatChirper.Utilities
{
    /// <summary>
    /// stores some global information we will use throughout this mod
    /// </summary>
    internal class GlobalConfiguration
    {
        static public readonly string m_ipAddress = "irc.chat.twitch.tv";
        static public readonly int m_port = 6667;
        static public readonly string m_FileLocation = "TwitchChatChirperConnectionSettings.txt";
    }
}
