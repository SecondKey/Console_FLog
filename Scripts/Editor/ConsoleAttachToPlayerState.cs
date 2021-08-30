using System;
using System.Globalization;
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Networking.PlayerConnection;
using UnityEditor.Experimental.Networking.PlayerConnection;
using ConnectionGUILayout = UnityEditor.Experimental.Networking.PlayerConnection.EditorGUILayout;
using EditorGUI = UnityEditor.EditorGUI;
using EditorGUILayout = UnityEditor.EditorGUILayout;
using EditorGUIUtility = UnityEditor.EditorGUIUtility;
using CoreLog = UnityEditor;
using System.IO;


namespace ConsoleTiny
{
    class ConsoleAttachToPlayerState : GeneralConnectionState
    {
        static class Content
        {
            public static GUIContent PlayerLogging = EditorGUIUtility.TrTextContent("Player Logging");
            public static GUIContent FullLog = EditorGUIUtility.TrTextContent("Full Log (Developer Mode Only)");
        }

        public ConsoleAttachToPlayerState(EditorWindow parentWindow, Action<string> connectedCallback = null) : base(parentWindow, connectedCallback) { }

        bool IsConnected()
        {
            return PlayerConnectionLogReceiver.instance.State != PlayerConnectionLogReceiver.ConnectionState.Disconnected;
        }

        void PlayerLoggingOptionSelected()
        {
            PlayerConnectionLogReceiver.instance.State = IsConnected() ? PlayerConnectionLogReceiver.ConnectionState.Disconnected : PlayerConnectionLogReceiver.ConnectionState.CleanLog;
        }

        bool IsLoggingFullLog()
        {
            return PlayerConnectionLogReceiver.instance.State == PlayerConnectionLogReceiver.ConnectionState.FullLog;
        }

        void FullLogOptionSelected()
        {
            PlayerConnectionLogReceiver.instance.State = IsLoggingFullLog() ? PlayerConnectionLogReceiver.ConnectionState.CleanLog : PlayerConnectionLogReceiver.ConnectionState.FullLog;
        }

        public override void AddItemsToMenu(GenericMenu menu, Rect position)
        {
            // option to turn logging and the connection on or of
            menu.AddItem(Content.PlayerLogging, IsConnected(), PlayerLoggingOptionSelected);
            if (IsConnected())
            {
                // All other options but the first are only available if logging is enabled
                menu.AddItem(Content.FullLog, IsLoggingFullLog(), FullLogOptionSelected);
                menu.AddSeparator("");
                base.AddItemsToMenu(menu, position);
            }
        }
    }
}