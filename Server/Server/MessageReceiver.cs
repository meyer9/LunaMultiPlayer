﻿using System;
using System.Collections.Generic;
using Lidgren.Network;
using LunaCommon;
using LunaCommon.Enums;
using LunaCommon.Message.Interface;
using LunaServer.Client;
using LunaServer.Context;
using LunaServer.Log;
using LunaServer.Message.Reader;
using LunaServer.Message.Reader.Base;
using LunaServer.Plugin;

namespace LunaServer.Server
{
    public class MessageReceiver
    {
        #region Handlers

        private static readonly Dictionary<ClientMessageType, ReaderBase> HandlerDictionary = new Dictionary
            <ClientMessageType, ReaderBase>
        {
            [ClientMessageType.Groups] = new GroupMsgReader(),
            [ClientMessageType.Admin] = new AdminMsgReader(),
            [ClientMessageType.Handshake] = new HandshakeMsgReader(),
            [ClientMessageType.Chat] = new ChatMsgReader(),
            [ClientMessageType.PlayerStatus] = new PlayerStatusMsgReader(),
            [ClientMessageType.PlayerColor] = new PlayerColorMsgReader(),
            [ClientMessageType.Scenario] = new ScenarioDataMsgReader(),
            [ClientMessageType.SyncTime] = new SyncTimeRequestMsgReader(),
            [ClientMessageType.Kerbal] = new KerbalMsgReader(),
            [ClientMessageType.Settings] = new SettingsMsgReader(),
            [ClientMessageType.Vessel] = new VesselMsgReader(),
            [ClientMessageType.CraftLibrary] = new CraftLibraryMsgReader(),
            [ClientMessageType.Flag] = new FlagSyncMsgReader(),
            [ClientMessageType.Motd] = new MotdMsgReader(),
            [ClientMessageType.Warp] = new WarpControlMsgReader(),
            [ClientMessageType.Lock] = new LockSystemMsgReader(),
            [ClientMessageType.Mod] = new ModDataMsgReader()
        };

        #endregion

        public void ReceiveCallback(ClientStructure client, NetIncomingMessage msg)
        {
            if (client == null || msg.LengthBytes <= 1) return;

            if (client.ConnectionStatus == ConnectionStatus.Connected)
                client.LastReceiveTime = ServerContext.ServerClock.ElapsedMilliseconds;

            var messageBytes = msg.ReadBytes(msg.LengthBytes);
            var message = ServerContext.ClientMessageFactory.Deserialize(messageBytes, DateTime.UtcNow.Ticks) as IClientMessageBase;

            if (message == null)
            {
                LunaLog.Error("Error deserializing message!");
                return;
            }

            LmpPluginHandler.FireOnMessageReceived(client, message);
            //A plugin has handled this message and requested suppression of the default behavior
            if (message.Handled) return;

            if (message.VersionMismatch)
            {
                MessageQueuer.SendConnectionEnd(client, $"Version mismatch. Your version doesn't match the server's version: {VersionInfo.VersionNumber}.  Update your plugin.");
                return;
            }

            //Clients can only send HANDSHAKE until they are Authenticated.
            if (!client.Authenticated && message.MessageType != ClientMessageType.Handshake)
            {
                MessageQueuer.SendConnectionEnd(client, $"You must authenticate before sending a {message.MessageType} message");
                return;
            }

            LunaLog.Debug(message.MessageType.ToString());

            //Handle the message
            HandlerDictionary[message.MessageType].HandleMessage(client, message.Data);
        }
    }
}