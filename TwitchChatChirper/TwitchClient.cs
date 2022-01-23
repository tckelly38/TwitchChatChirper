using System;
using System.Net.Sockets;
using System.IO;
using TwitchChatChirper.Utilities;
using ColossalFramework;
using UnityEngine;

namespace TwitchChatChirper
{
    /// <summary>
    /// this Twitch client will handle the connection and processing of incoming messages from the
    /// IRC twitch client
    /// 
    /// TODO: we currently pass all messages received in twitch to the Chirper
    /// would it be better to instead only pass certain info to the chirper instead
    /// to avoid barage of messages?
    /// 
    /// ideas:
    ///     * require users to send a command to have message read !chirp hello there
    ///     * send only on subscribe/follow/donations
    ///     * rate limit somehow per user (maybe allow an individual user to send a message once every 10 seconds (all others ignored)
    /// </summary>
    public class TwitchClient
    {
        /// <summary>
        /// stream for reading irc responses
        /// </summary>
        StreamReader m_StreamReader;

        /// <summary>
        /// stream for writing message to irc endpoint
        /// </summary>
        StreamWriter m_StreamWriter;

        /// <summary>
        /// stream for determining if content is on stream
        /// </summary>
        NetworkStream m_NetworkStream;

        /// <summary>
        /// client to connect with irc stream
        /// </summary>
        TcpClient m_TcpClient;

        /// <summary>
        /// How we interact with user options file for this mod
        /// </summary>
        readonly UserOptionFile m_UserOptionFile;

        public TwitchClient()
        {
            m_UserOptionFile = new UserOptionFile();
        }

        /// <summary>
        /// closes all connections and streams
        /// </summary>
        public void CloseAll()
        {
            if (m_TcpClient != null)
                m_TcpClient.Close();
            if (m_StreamReader != null)
                m_StreamReader.Close();
            if (m_StreamWriter != null)
                m_StreamWriter.Close();
            if (m_NetworkStream != null)
                m_NetworkStream.Close();

            m_TcpClient = null;
            m_StreamWriter = null;
            m_StreamReader = null;
            m_NetworkStream = null;
        }
        
        /// <summary>
        /// We attempt to establich a connection with the twitch chat irc endpoint
        /// </summary>
        /// <returns> whether connection was successful</returns>
        public bool AttemptConnect()
        {
            // ensure we are at a good starting point by closing / resetting all streams and clients 
            CloseAll();

            // establish connection with twitch IRC endpoint
            m_TcpClient = new TcpClient();
            m_TcpClient.Connect(GlobalConfiguration.m_ipAddress, GlobalConfiguration.m_port);

            if (!m_TcpClient.Connected)
            {
                Debug.Log("Failed to Connect!");
                return false;
            }

            Debug.Log("Sucessful IRC Connection");

            // establish read and write streams to interact with chat
            m_StreamReader = new StreamReader(m_TcpClient.GetStream());
            m_StreamWriter = new StreamWriter(m_TcpClient.GetStream()) { NewLine = "\r\n", AutoFlush = true }; // note: irc requires message lines end with \r\n

            // a network stream will allow us to check for content on stream w/o blocking
            m_NetworkStream = m_TcpClient.GetStream();

            // twitch chat irc requires these details be sent in this order
            m_StreamWriter.WriteLine("PASS " + m_UserOptionFile.GetField(UserOptionFile.Field.OAuthPassword));
            m_StreamWriter.WriteLine("NICK " + m_UserOptionFile.GetField(UserOptionFile.Field.Username));
            m_StreamWriter.WriteLine("JOIN #" + m_UserOptionFile.GetField(UserOptionFile.Field.Channel));
            m_StreamWriter.Flush();

            // after we write the above lines to the irc end point our response will look like this:
            //> :tmi.twitch.tv 001 <user> :Welcome, GLHF!
            //> :tmi.twitch.tv 002 <user> :Your host is tmi.twitch.tv
            //> :tmi.twitch.tv 003 <user> :This server is rather new
            //> :tmi.twitch.tv 004 <user> :-
            //> :tmi.twitch.tv 375 <user> :-
            //> :tmi.twitch.tv 372 <user> :You are in a maze of twisty passages.
            //> :tmi.twitch.tv 376 <user> :>
            //> :<user>!<user>@<user>.tmi.twitch.tv JOIN #<channel>
            //> :<user>.tmi.twitch.tv 353 <user> = #<channel> :<user>
            //> :<user>.tmi.twitch.tv 366 <user> #<channel> :End of /NAMES list
            //
            // we want to get past these responses so remove them from the stream
            // that we ensure we start reading twitch chat message immediately upon loading
            //
            // ReadLine() BLOCKS! there is a potential here that this will hang...
            // TODO: come up with better strategy
            while (!m_StreamReader.ReadLine().Contains("366"))
            {
                continue;
            }
            
            return true;
        }

        /// <summary>
        /// CALLED ONCE PER FRAME
        /// 
        /// essentially this function will check if there is content on the incoming stream from the irc
        /// client... if so then it will process message accordingly (either PONG or  display message in
        /// Chirper)
        /// </summary>
        public void Update()
        {
            // ensure line retrieved from stream is not null or empty
            string line = "";
            try
            {
                if (m_NetworkStream.DataAvailable)
                {
                    line = m_StreamReader.ReadLine();
                    m_NetworkStream.Flush();
                }
            }
            catch (IOException e) // saw this exception once in testing but have yet to get it to come up
                                  // deserves more testing
            {
                Debug.Log("IO ERROR OCCURRED: " + e.Message);
                Debug.Log("Attempting to reconnect");
                AttemptConnect();
                return;
            }
            catch (OutOfMemoryException e)
            {
                Debug.Log("OUT OF MEMORY ERROR" + e.Message);
                return;
            }
            catch (ArgumentOutOfRangeException e)
            {
                Debug.Log("OUT OF RANGE EXCEPTION: " + e.Message);
                return;
            }

            // we actually know that this can't be null or empty b/c ReadLine blocks until it receives
            // something on stream... but leaving for now
            if (line.IsNullOrWhiteSpace())
                return;

            //Debug.Log("Input Line: " + line);

            // now attempt to parse message
            string[] split = line.Split(' ');

            //PING :tmi.twitch.tv
            //Respond with PONG :tmi.twitch.tv
            if (line.StartsWith("PING") && split.Length > 1)
            {
                m_StreamWriter.WriteLine("PONG " + split[1]);
                //Debug.Log("Sending: PONG " + split[1]);
                return;
            }

            if (split.Length > 1 && split[1] == "PRIVMSG")
            {
                //:mytwitchchannel!mytwitchchannel@mytwitchchannel.tmi.twitch.tv 
                // ^^^^^^^^
                //Grab this name here
                int exclamationPointPosition = split[0].IndexOf("!");
                if (exclamationPointPosition < 2)
                    return;

                string username = split[0].Substring(1, exclamationPointPosition - 1);

                //Skip the first character, the first colon, then find the next colon
                int secondColonPosition = line.IndexOf(':', 1);//the 1 here is what skips the first character
                if (secondColonPosition < 0)
                    return;

                string message = line.Substring(secondColonPosition + 1);//Everything past the second colon

                //Debug.Log("Username:message: " + username + ":" + message);

                Singleton<ChirpPanel>.instance.AddMessage(new ChirperMessage(1, username, message), true);
            }
            m_StreamWriter.Flush();
        }
    }
}
