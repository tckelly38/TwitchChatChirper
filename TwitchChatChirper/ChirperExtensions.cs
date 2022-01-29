using ICities;
using UnityEngine;

namespace TwitchChatChirper
{
    /// <summary>
    /// will interact with twitch client and in-game chirper
    /// </summary>
    public class ChirperExtensions : ChirperExtensionBase
    {
        /// <summary>
        /// so we can interact with twitch client
        /// </summary>
        TwitchClient m_TwitchClient;

        /// <summary>
        /// keeps track of how many times we have called OnUpdate (essentially which frame are we on?)
        /// </summary>
        uint sentinel;

        /// <summary>
        /// how often should we check for a new twitch message (by frame)
        /// </summary>
        const uint frameDelay = 150;

        /// <summary>
        /// keeps track of whether we have a successful connection with the irc twitch client
        /// </summary>
        bool successfulConnection = false;

        /// <summary>
        /// this gets called when we create our Chirper object... basically we call this once when we load a level
        /// </summary>
        /// <param name="c">standard chirper interface, we dont mess with this object</param>
        public override void OnCreated(IChirper c)
        {
            base.OnCreated(c);

            // establish connection with twitch client
            // TODO: perhaps perform some retry/backoff logic to account for failures
            sentinel = 0;
            m_TwitchClient = new TwitchClient();
            if ((successfulConnection = m_TwitchClient.AttemptConnect()) == false)
            {
                Debug.Log("Connection Unsucessful");
                m_TwitchClient.CloseAll();
            }
        }

        /// <summary>
        /// called once perframe
        /// we first ensure that we have a succesful connection (TODO: currently not updated on "reconnects")
        /// </summary>
        public override void OnUpdate()
        {
            base.OnUpdate();
            
            // only attempt to read from stream if connection was sucessful
            if (!successfulConnection)
                return;

            // only attempt to read 1 in 150 frames
            sentinel++;
            if (sentinel % frameDelay != 0)
            {
                return;
            }
            // OnCreated will be called before, null check just in case
            if (m_TwitchClient != null)
                m_TwitchClient.Update();
        }
    }
}
