﻿using LunaClient.Base;
using LunaClient.Base.Interface;
using LunaClient.Network;
using LunaClient.Systems.VesselRemoveSys;
using LunaCommon.Message.Client;
using LunaCommon.Message.Interface;

namespace LunaClient.Systems.VesselPositionAltSys
{
    public class VesselPositionMessageAltSender : SubSystem<VesselPositionAltSystem>, IMessageSender
    {
        public void SendMessage(IMessageData msg)
        {
            NetworkSender.QueueOutgoingMessage(MessageFactory.CreateNew<VesselCliMsg>(msg));
        }

        public void SendVesselPositionUpdate(Vessel vessel)
        {
            var update = new VesselPositionAltUpdate(vessel);
            TaskFactory.StartNew(() => SendVesselPositionUpdate(update));
        }

        public void SendVesselPositionUpdate(VesselPositionAltUpdate update)
        {
            if (SystemsContainer.Get<VesselRemoveSystem>().VesselWillBeKilled(update.VesselId))
                return;

            SendMessage(update.AsSimpleMessage());
        }
    }
}
