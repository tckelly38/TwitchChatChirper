using ICities;
namespace TwitchChatChirper.Utilities
{
    /// <summary>
    /// simple class to hold the message we are going to send to the chirper
    /// </summary>
    public class ChirperMessage : IChirperMessage
    {
        private uint m_SenderID;
        private string m_senderName, m_Text;

        public ChirperMessage(uint senderID, string senderName, string text)
        {
            m_SenderID = senderID;
            m_senderName = senderName;
            m_Text = text;
        }

        public uint senderID { get { return m_SenderID; } }
        public string senderName { get { return m_senderName; } }
        public string text { get { return m_Text; } }
    }
}
