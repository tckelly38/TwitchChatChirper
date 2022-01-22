using ICities;
using ColossalFramework;
using System.Net.Sockets;
using System.IO;
using UnityEngine;
using System;
namespace TwitchChatChirper
{
    public class ConnectionDetails
    {
        static public readonly string m_ipAddress = "irc.chat.twitch.tv";

        static public readonly int m_port = 6667;
    }
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

        public uint senderID
        {
            get { return m_SenderID; }
        }
        public string senderName { get { return m_senderName; } }

        public string text { get { return m_Text; } }

    }
    public sealed class FileStreamHandler
    {
        static public readonly string m_FileLocation = "TwitchChatChirperConnectionSettings.txt";
        private FileStreamHandler()
        {
            if (!File.Exists(m_FileLocation))
            { 
                File.Create(m_FileLocation).Close();
            }
        }
        public static FileStreamHandler Instance { get { return Nested.instance; } }
        private class Nested
        {
            static Nested() { }
            internal static readonly FileStreamHandler instance = new FileStreamHandler();
        }
        

        private void AppendLine(string line)
        { 
            File.AppendAllText(m_FileLocation, line + Environment.NewLine);
        }
        
        private void LineChanger(string newText, int lineToEdit)
        { 
            string [] arrLine = File.ReadAllLines(m_FileLocation);
            arrLine[lineToEdit] = newText;
            File.WriteAllLines(m_FileLocation, arrLine);
        }

        private int GetLineNumber(string field)
        {
            string[] arrLine = File.ReadAllLines(m_FileLocation);
            
            for (int i = 0; i < arrLine.Length; i++)
            {
                if (arrLine[i].Contains(field))
                    return i;
            }
            return -1;
        }

        private void UpdateField(string field, string newLine)
        {
            int lineNo = GetLineNumber(field);
            if (lineNo > -1)
            { 
                LineChanger(newLine, lineNo);
            }

        }
        public void SetOAuthPassword(string oauthPassword)
        {
            // if empty then just add line, o.w we need to remove/update line in file
            if (GetOAuthPassword() == "")
                AppendLine("oauthPassword:" + oauthPassword);
            else
                UpdateField("oauthPassword", oauthPassword);
            
        }
        public void SetUsername(string username)
        {
            if (GetUsername() == "")
                AppendLine("username:" + username);
            else
                UpdateField("username", username);
        }
        public void SetChannel(string channel)
        {
            if (GetChannel() == "")
                AppendLine("channel:" + channel);
            else
                UpdateField("channel", channel);
        }
        private string GetValue(string field)
        {
            string[] arrLine = File.ReadAllLines(m_FileLocation);

            for (int i = 0; i < arrLine.Length; i++)
            {
                if (arrLine[i].Contains(field))
                {
                    return arrLine[i].Split(new[] { ':' }, 2)[1];
                }
            }
            return "";
        }
        public string GetOAuthPassword()
        {
            return GetValue("oauthPassword");
        }
        public string GetUsername()
        {
            return GetValue("username");
        }
        public string GetChannel()
        {
            return GetValue("channel");
        }

    }
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

        public void OnSettingsUI(UIHelperBase helper)
        {
            UIHelperBase group = helper.AddGroup("Twitch Chat Chirper Group");
            group.AddTextfield("Twitch Chat OAuth Password", FileStreamHandler.Instance.GetOAuthPassword(), (value) => { }, (value) => FileStreamHandler.Instance.SetOAuthPassword(value));
            group.AddTextfield("Twitch Chat Username", FileStreamHandler.Instance.GetUsername(), (value) => { }, (value) => FileStreamHandler.Instance.SetUsername(value));
            group.AddTextfield("Twitch Chat Channel", FileStreamHandler.Instance.GetChannel(), (value) => { }, (value) => FileStreamHandler.Instance.SetChannel(value));
        }

    }
    public class TwitchClient
    {
        StreamReader m_StreamReader;
        StreamWriter m_StreamWriter;
        NetworkStream m_NetworkStream;
        TcpClient m_TcpClient;

        /*
         * Close all connections and streams
         */
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
        /*
         * We attempt to establich a connection with the twitch chat irc endpoint
         * return true: sucesffuly connected
         * return false: unable to connect
         */
        public bool AttemptConnect()
        {
            // ensure we are at a good starting point by closing / resetting all streams and clients 
            CloseAll();

            // establish connection with twitch IRC endpoint
            m_TcpClient = new TcpClient();
            m_TcpClient.Connect(ConnectionDetails.m_ipAddress, ConnectionDetails.m_port);

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
            m_StreamWriter.WriteLine("PASS " + FileStreamHandler.Instance.GetOAuthPassword());
            m_StreamWriter.WriteLine("NICK " + FileStreamHandler.Instance.GetUsername());
            m_StreamWriter.WriteLine("JOIN #" + FileStreamHandler.Instance.GetChannel());
            m_StreamWriter.Flush();

            // after we send that output we should get a message sent back containing "001... GLHF"
            // becuase ReadLine() blocks, we will wait here until we get something on the wire
            // TODO: get past all the beginning text so we dont waste time at beginning updating on that
            if (!m_StreamReader.ReadLine().Contains("001"))
                return false;
            return true;
        }

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
    public class ChirperExtensions : ChirperExtensionBase
    {
        TwitchClient m_TwitchClient;
        uint sentinel;
        // wait 150 frames
        const uint frameDelay = 150;
        bool successfulConnection = false;

        public override void OnCreated(IChirper c)
        {
            base.OnCreated(c);
            sentinel = 0;
            m_TwitchClient = new TwitchClient();
            if ((successfulConnection = m_TwitchClient.AttemptConnect()) == false)
            {
                Debug.Log("Connection Unsucessful");
                m_TwitchClient.CloseAll();
            }
        }

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
            if(m_TwitchClient != null)
                m_TwitchClient.Update();
        }
    }
}