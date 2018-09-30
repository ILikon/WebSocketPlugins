using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Rainmeter;
using WebSocketSharp;
using System.Text.RegularExpressions;

// Overview: This is a blank canvas on which to build your plugin.

// Note: GetString, ExecuteBang and an unnamed function for use as a section variable
// have been commented out. If you need GetString, ExecuteBang, and/or section variables 
// and you have read what they are used for from the SDK docs, uncomment the function(s)
// and/or add a function name to use for the section variable function(s). 
// Otherwise leave them commented out (or get rid of them)!

namespace WebSocketClient
{
    class Measure
    {
        WebSocket ws;
        IntPtr Skin;
        String cmdOnOpen;
        String cmdOnMessage;
        String cmdOnError;
        String cmdOnClose;

        bool SendAsync; // Use async communication
        bool KeepConnectionAlive; // Try to reconnect when connection is lost
        bool PingServer; // Ping server to make sure connection is alive
        int MaxReconnectAttempts; // If 0 try forever
        bool TryToReconnect;
        int CurrentAttempt;
        DateTime LastConnectionAttempt;
        DateTime LastPing;

        struct Command
        {
            public String Tag;
            public String cmdOnTag;
        }
        List<Command> Commands;

        static public implicit operator Measure(IntPtr data)
        {
            return (Measure)GCHandle.FromIntPtr(data).Target;
        }


        public void Setup(Rainmeter.API api)
        {
            // Init members
            SendAsync = (api.ReadInt("SendAsync", 0) > 0);
            TryToReconnect = false;
            KeepConnectionAlive = (api.ReadInt("KeepAlive", 1) > 0);
            PingServer = (api.ReadInt("PingServer", 0) > 0);
            MaxReconnectAttempts = api.ReadInt("MaxReconnectAttempts", 0);
            CurrentAttempt = 0;
            LastConnectionAttempt = DateTime.Now;
            LastPing = LastConnectionAttempt;

            // Setup WebSocket
            String Address = api.ReadString("Address", "");
            if (!Address.IsNullOrEmpty())
            {
                ws = new WebSocket(Address);
                ws.NoDelay = true;
                ws.OnOpen += OnOpen;
                ws.OnMessage += OnMessage;
                ws.OnError += OnError;
                ws.OnClose += OnClose;
                ws.ConnectAsync();
            }

            // Get Commands
            Skin = api.GetSkin();
            cmdOnOpen = api.ReadString("OnOpen", "");
            cmdOnMessage = api.ReadString("OnMessage", "");
            cmdOnError = api.ReadString("OnError", "");
            cmdOnClose = api.ReadString("OnClose", "");

            // Get Parsable Commands

            // Ex: ParseCommands="AA:|BB:|CMD:"
            // OnAA:=[Some Bang]
            // OnBB:=[Some Bang]
            // OnCMD:=[Some Bang]

            Commands = new List<Command>();
            String cmds = api.ReadString("ParseCommands", "");
            if (!cmds.IsNullOrEmpty())
            {
                var tags = cmds.Split('|');
                foreach (var tag in tags)
                {
                    String c = api.ReadString("On" + tag, "");
                    Commands.Add(new Command { Tag = tag, cmdOnTag = c });
                }
            }

        }

        public void Close()
        {
            if (ws.ReadyState == WebSocketState.Open)
                ws.CloseAsync();
        }

        public void Send(String s)
        {
            if (ws.ReadyState == WebSocketState.Open)
            {
                if (SendAsync)
                    ws.SendAsync(s, null);
                else
                    ws.Send(s);
                LastPing = DateTime.Now;
            }
        }

        public void KeepAlive()
        {
            if (!KeepConnectionAlive)
                return;

            // Ping after some time to make sure connection is alive
            if (PingServer && ws.ReadyState == WebSocketState.Open && DateTime.Now.Subtract(LastPing).Seconds > 3)
            {
                LastPing = DateTime.Now;
                if (ws.Ping()) // For some reason Ping breaks the connection on my tests, something to do with mask bit
                    return;
            }

            if (!TryToReconnect)
                return;

            if (MaxReconnectAttempts > 0 && CurrentAttempt > MaxReconnectAttempts)
                return;

            // Retry only if some time has passed
            if (DateTime.Now.Subtract(LastConnectionAttempt).Seconds > 10)
            {
                ws.ConnectAsync();
                CurrentAttempt++;
                LastConnectionAttempt = DateTime.Now;
            }
        }

        public WebSocketState GetState()
        {
            return ws.ReadyState;
        }

        void OnOpen(object sender, EventArgs e)
        {
            API.Execute(Skin, cmdOnOpen);
            TryToReconnect = false;
            LastPing = DateTime.Now;
        }

        void OnMessage(object sender, MessageEventArgs e)
        {
            LastPing = DateTime.Now;

            foreach (var Data in e.Data.Split(';'))
            {
                bool cmdParsed = false;

                // Parse cmds
                foreach (var C in Commands)
                {
                    if (Data.StartsWith(C.Tag))
                    {
                        string msg = Data.Substring(C.Tag.Length);
                        string cmdmsg = Regex.Replace(C.cmdOnTag, "\\$message\\$", msg, RegexOptions.IgnoreCase);
                        API.Execute(Skin, cmdmsg);
                        cmdParsed = true;
                        break;
                    }
                }

                if (cmdParsed)
                    continue;

                //If no command is recognized
                string cmd = Regex.Replace(cmdOnMessage, "\\$message\\$", Data, RegexOptions.IgnoreCase);
                API.Execute(Skin, cmd);
            }
        }

        void OnError(object sender, ErrorEventArgs e)
        {
            //Use regular experssion to replace $Message$ since str.replace can only be case sensitive
            string cmd = Regex.Replace(cmdOnError, "\\$message\\$", e.Message, RegexOptions.IgnoreCase);
            API.Execute(Skin, cmd);
        }

        void OnClose(object sender, CloseEventArgs e)
        {
            //Use regular experssion to replace $Message$ since str.replace can only be case sensitive
            string cmd = Regex.Replace(cmdOnClose, "\\$message\\$", e.Reason, RegexOptions.IgnoreCase);
            API.Execute(Skin, cmd);

            TryToReconnect = !e.WasClean;
            LastConnectionAttempt = DateTime.Now; // Wait a bit before reconnecting
        }

    }

    public class Plugin
    {
        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            Measure measure = new Measure();
            data = GCHandle.ToIntPtr(GCHandle.Alloc(measure));
            Rainmeter.API api = (Rainmeter.API)rm;
            measure.Setup(api);
        }

        [DllExport]
        public static void Finalize(IntPtr data)
        {
            Measure measure = (Measure)data;
            measure.Close();

            GCHandle.FromIntPtr(data).Free();
        }

        [DllExport]
        public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
        {
            Measure measure = (Measure)data;
        }

        [DllExport]
        public static double Update(IntPtr data)
        {
            Measure measure = (Measure)data;
            measure.KeepAlive();
            double state = (double)measure.GetState();
            return state;
        }

        //[DllExport]
        //public static IntPtr GetString(IntPtr data)
        //{
        //    Measure measure = (Measure)data;
        //
        //    return Marshal.StringToHGlobalUni(""); //returning IntPtr.Zero will result in it not being used
        //}

        [DllExport]
        public static void ExecuteBang(IntPtr data, [MarshalAs(UnmanagedType.LPWStr)]String args)
        {
            Measure measure = (Measure)data;
            measure.Send(args);
        }

        //[DllExport]
        //public static IntPtr (IntPtr data, int argc,
        //    [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] argv)
        //{
        //    Measure measure = (Measure)data;
        //
        //    return Marshal.StringToHGlobalUni(""); //returning IntPtr.Zero will result in it not being used
        //}
    }
}
