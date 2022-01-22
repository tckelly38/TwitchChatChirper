using ICities;
using ColossalFramework;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using UnityEngine;

namespace TwitchChatChirper
{
    public class TwitchChatChirper : IUserMod
    {
        public string Name
        {
            get { return "Twitch Chat Chirper"; }
        }
        public string Description
        {
            get { return "Incoming chirps will refect your currently active twitch chat."; }
        }

    }
    public class ChirperExtensions : IChirperExtension
    {
        StreamReader streamReader;
        StreamWriter streamWriter;
        TcpClient tcpClient;
        uint sentinel;
        string ip = "irc.chat.twitch.tv";
        int port = 6667;
        string password = "oauth:kjab1mw4btq0p0socfle5fmddt72ic";
        string botUsername = "tcjellyson";
        // wait 400 frames
        const uint frameDelay = 400;
        public void OnCreated(IChirper chirper)
        {
            sentinel = 0;
            Debug.Log("Starting chirp thread");
            
            tcpClient = new TcpClient();
            tcpClient.Connect(ip, port);

            if (!tcpClient.Connected)
            {
                Debug.Log("Failed to Connect!");
                return;
            }
            Debug.Log("Sucessful IRC Connection");
            streamReader = new StreamReader(tcpClient.GetStream());
            streamWriter = new StreamWriter(tcpClient.GetStream()) { NewLine = "\r\n", AutoFlush = true };

            streamWriter.WriteLine("PASS " + password);
            streamWriter.WriteLine("NICK " + botUsername);
            streamWriter.WriteLine("JOIN #" + botUsername);
            streamWriter.Flush();
        }

        public void OnMessagesUpdated()
        {
            //throw new System.NotImplementedException();
        }

        public void OnNewMessage(IChirperMessage message)
        {
            //throw new System.NotImplementedException();
        }

        public void OnReleased()
        {
            /*streamReader.Close();
            streamWriter.Close();
            tcpClient.Close();
            tcpClient = null;*/
            //throw new System.NotImplementedException();
        }

        public void OnUpdate()
        {
            // only attempt to read from stream if connection was sucessful
            if (!tcpClient.Connected) return;

            // only attempt to read 1 in 400 frames
            sentinel++;
            if (sentinel % frameDelay != 0)
            {
                return;
            }

            // ensure line retrieved from stream is not null or empty
            string line = streamReader.ReadLine();
            if (line.IsNullOrWhiteSpace())
                return;

            Debug.Log("Input Line: " + line);

            // now attempt to parse message
            string[] split = line.Split(new char[] { ' ' });
            
            //PING :tmi.twitch.tv
            //Respond with PONG :tmi.twitch.tv
            if (line.StartsWith("PING"))
            {
                streamWriter.WriteLine("PONG " + split[1]);
                Debug.Log("Sending: PONG " + split[1]);
                return;
            }

            if (split.Length > 1 && split[1] == "PRIVMSG")
            {
                //:mytwitchchannel!mytwitchchannel@mytwitchchannel.tmi.twitch.tv 
                // ^^^^^^^^
                //Grab this name here
                int exclamationPointPosition = split[0].IndexOf("!");
                string username = split[0].Substring(1, exclamationPointPosition - 1);
                //Skip the first character, the first colon, then find the next colon
                int secondColonPosition = line.IndexOf(':', 1);//the 1 here is what skips the first character
                string message = line.Substring(secondColonPosition + 1);//Everything past the second colon

                Debug.Log("Username:message: " + username + ":" + message);

                //Singleton<ChirpPanel>.instance.AddEntry(new ChirperMessage(0, username, message), true);
                Singleton<ChirpPanel>.instance.AddMessage(new ChirperMessage(1, username, message), true);
                //Singleton<ChirpPanel>.instance.SynchronizeMessages();
            }
            streamWriter.Flush();
        }
        private class ChirperMessage : IChirperMessage
        {
            private uint m_SenderID;
            private string m_senderName, m_Text;

            public ChirperMessage(uint senderID, string senderName, string text)
            {
                m_SenderID = senderID;
                m_senderName = senderName;
                m_Text = text;
            }

            public uint senderID
            {
                get { return m_SenderID; }
            }
            public string senderName { get { return m_senderName; } }

            public string text { get { return m_Text; } }

        }
    }
}