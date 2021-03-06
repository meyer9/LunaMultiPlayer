﻿using LunaClient.Base;
using LunaClient.Base.Interface;
using LunaClient.Network;
using LunaClient.Utilities;
using LunaCommon.Message.Client;
using LunaCommon.Message.Data.Vessel;
using LunaCommon.Message.Interface;
using System.Collections.Generic;


namespace LunaClient.Systems.VesselProtoSys
{
    public class VesselProtoMessageSender : SubSystem<VesselProtoSystem>, IMessageSender
    {
        public void SendMessage(IMessageData msg)
        {
            NetworkSender.QueueOutgoingMessage(MessageFactory.CreateNew<VesselCliMsg>(msg));
        }

        public void SendVesselMessage(Vessel vessel)
        {
            if (vessel == null) return;

            //Doing a Vessel.Backup takes a lot of time... So we use a handler that accepts multithreading

            var creator = new VesselProtoBackup();
            creator.PrepareBackup(vessel.parts);
            TaskFactory.StartNew(() => creator.BackupVessel(vessel)).ContinueWith(prev => SendVesselMessage(prev.Result));

            //Do not call it in this way as therefore we are sending a NOT UPDATED protovessel!
            //SendVesselMessage(vessel.protoVessel);
        }



        public void SendVesselMessage(ProtoVessel protoVessel)
        {
            if (protoVessel == null) return;
            TaskFactory.StartNew(() => PrepareAndSendProtoVessel(protoVessel));
        }

        public void SendVesselMessage(IEnumerable<Vessel> vessels)
        {
            foreach (var vessel in vessels)
            {
                SendVesselMessage(vessel);
            }
        }

        #region Private methods

        /// <summary>
        /// This method prepares the protovessel class and send the message, it's intended to be run in another thread
        /// </summary>
        private void PrepareAndSendProtoVessel(ProtoVessel protoVessel)
        {
            var vesselBytes = VesselSerializer.SerializeVessel(protoVessel);
            if (vesselBytes.Length > 0)
            {
                UniverseSyncCache.QueueToCache(vesselBytes);

                SendMessage(new VesselProtoMsgData
                {
                    VesselId = protoVessel.vesselID,
                    VesselData = vesselBytes
                });
            }
            else
            {
                LunaLog.LogError($"[LMP]: Failed to create byte[] data for {protoVessel.vesselID}");
            }
        }

        #endregion
    }
}
