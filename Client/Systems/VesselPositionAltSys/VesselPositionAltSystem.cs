﻿using LunaClient.Base;
using LunaClient.Systems.SettingsSys;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UniLinq;
using UnityEngine;

namespace LunaClient.Systems.VesselPositionAltSys
{
    /// <summary>
    /// System that handle the received vessel update messages and also sends them
    /// </summary>
    public class VesselPositionAltSystem : MessageSystem<VesselPositionAltSystem, VesselPositionMessageAltSender, VesselPositionMessageAltHandler>
    {
        #region Fields & properties

        private static bool MustSendFastUpdates => VesselCommon.PlayerVesselsNearby() || VesselCommon.IsNearKsc(20000);
        private static int FastVesselUpdatesSendMsInterval => SettingsSystem.ServerSettings.VesselUpdatesSendMsInterval;
        private static int SlowVesselUpdatesSendMsInterval => FastVesselUpdatesSendMsInterval * 5;

        public bool PositionUpdateSystemReady => Enabled && FlightGlobals.ActiveVessel != null && Time.timeSinceLevelLoad > 1f &&
                                         FlightGlobals.ready && FlightGlobals.ActiveVessel.loaded &&
                                         FlightGlobals.ActiveVessel.state != Vessel.State.DEAD && !FlightGlobals.ActiveVessel.packed &&
                                         FlightGlobals.ActiveVessel.vesselType != VesselType.Flag;

        public bool PositionUpdateSystemBasicReady => Enabled && Time.timeSinceLevelLoad > 1f &&
            PositionUpdateSystemReady || HighLogic.LoadedScene == GameScenes.TRACKSTATION;

        public static ConcurrentDictionary<Guid, VesselPositionAltUpdate> CurrentVesselUpdate { get; } =
            new ConcurrentDictionary<Guid, VesselPositionAltUpdate>();

        public static ConcurrentDictionary<Guid, VesselPositionAltUpdate> TargetVesselUpdate { get; } =
            new ConcurrentDictionary<Guid, VesselPositionAltUpdate>();

        public static Queue<Guid> VesselsToRemove { get; } = new Queue<Guid>();

        #endregion

        #region Base overrides

        protected override bool ProcessMessagesInUnityThread => false;

        protected override void OnEnabled()
        {
            if (!SettingsSystem.CurrentSettings.UseAlternativePositionSystem) return;

            base.OnEnabled();

            TimingManager.FixedUpdateAdd(TimingManager.TimingStage.ObscenelyEarly, DisableVesselPrecalculate);
            TimingManager.FixedUpdateAdd(TimingManager.TimingStage.Precalc, ActivatePrecalc);

            SetupRoutine(new RoutineDefinition(0, RoutineExecution.FixedUpdate, HandleVesselUpdates));

            SetupRoutine(new RoutineDefinition(FastVesselUpdatesSendMsInterval,
                RoutineExecution.LateUpdate, SendVesselPositionUpdates));

            //SetupRoutine(new RoutineDefinition(SettingsSystem.ServerSettings.SecondaryVesselUpdatesSendMsInterval,
            //    RoutineExecution.Update, SendSecondaryVesselPositionUpdates));
        }

        protected override void OnDisabled()
        {
            if (!SettingsSystem.CurrentSettings.UseAlternativePositionSystem) return;

            base.OnDisabled();
            CurrentVesselUpdate.Clear();

            TimingManager.FixedUpdateRemove(TimingManager.TimingStage.ObscenelyEarly, DisableVesselPrecalculate);
        }

        private static void ActivatePrecalc()
        {
            if (FlightGlobals.Vessels == null) return;

            for (var i = FlightGlobals.Vessels.Count - 1; i >= 0; --i)
            {
                var vessel = FlightGlobals.Vessels[i];
                if (vessel?.precalc == null || vessel.id == FlightGlobals.ActiveVessel?.id)
                {
                    continue;
                }

                vessel.precalc.enabled = true;
                vessel.precalc.MainPhysics(true);
            }
        }

        private static void HandleVesselUpdates()
        {
            if (FlightGlobals.ActiveVessel == null) return;

            foreach (var keyVal in CurrentVesselUpdate)
            {
                keyVal.Value.ApplyVesselUpdate();
                //FlightGlobals.ActiveVessel.StartCoroutine(ApplyVesselUpdate(keyVal.Value));
            }

            while (VesselsToRemove.Count > 0)
            {
                var vesselToRemove = VesselsToRemove.Dequeue();
                TargetVesselUpdate.TryRemove(vesselToRemove, out _);
                CurrentVesselUpdate.TryRemove(vesselToRemove, out _);
            }
        }

        //private static IEnumerator ApplyVesselUpdate(VesselPositionAltUpdate vesselPosition)
        //{
        //    yield return new WaitForFixedUpdate();
        //    try
        //    {
        //        vesselPosition.ApplyVesselUpdate();
        //    }
        //    catch (Exception e)
        //    {
        //        // ignored
        //    }
        //}

        private static void DisableVesselPrecalculate()
        {
            if (FlightGlobals.ActiveVessel == null) return;

            foreach (var vessel in FlightGlobals.Vessels.Where(v => v.id != FlightGlobals.ActiveVessel.id && v.precalc != null))
            {
                vessel.precalc.enabled = false;
            }
        }

        #endregion

        #region FixedUpdate methods

        /// <summary>
        /// Send the updates of our own vessel. We only send them after an interval specified.
        /// If the other player vessels are far we don't send them very often.
        /// </summary>
        private void SendVesselPositionUpdates()
        {
            if (PositionUpdateSystemReady && !VesselCommon.IsSpectating)
            {
                MessageSender.SendVesselPositionUpdate(FlightGlobals.ActiveVessel);
                ChangeRoutineExecutionInterval("SendVesselPositionUpdates",
                    MustSendFastUpdates ? FastVesselUpdatesSendMsInterval : SlowVesselUpdatesSendMsInterval);
            }
        }

        /// <summary>
        /// Send updates for vessels that we own the update lock.
        /// </summary>
        private void SendSecondaryVesselPositionUpdates()
        {
            if (PositionUpdateSystemReady && !VesselCommon.IsSpectating)
            {
                var secondaryVesselsToUpdate = VesselCommon.GetSecondaryVessels();
                foreach (var secondaryVessel in secondaryVesselsToUpdate)
                {
                    MessageSender.SendVesselPositionUpdate(secondaryVessel);
                }
            }
        }

        #endregion

        /// <summary>
        /// Gets the latest received position of a vessel
        /// </summary>
        public double[] GetLatestVesselPosition(Guid vesselId)
        {
            return TargetVesselUpdate.TryGetValue(vesselId, out var vesselPosition) ? vesselPosition.LatLonAlt : new double[0];
        }
    }
}
