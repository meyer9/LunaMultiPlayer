﻿using System;

namespace LunaCommon.Locks
{
    public class LockDefinition
    {
        /// <summary>
        /// Player who owns the lock. It should never be null
        /// </summary>
        public string PlayerName { get; set; }

        /// <summary>
        /// Vessel id assigned to the lock. Can be null for an asteroid lock
        /// </summary>
        public Guid VesselId { get; set; }

        /// <summary>
        /// The type of the lock. It should never be null
        /// </summary>
        public LockType Type { get; set; }

        /// <summary>
        /// Parameterless constructor should not be used except for deserialization
        /// </summary>
        internal LockDefinition()
        {
        }

        /// <summary>
        /// Most basic constructor
        /// </summary>
        public LockDefinition(LockType type, string playerName)
        {
            Type = type;
            PlayerName = playerName;
        }

        /// <summary>
        /// Standard constructor
        /// </summary>
        public LockDefinition(LockType type, string playerName, Guid vesselId)
        {
            Type = type;
            PlayerName = playerName;
            VesselId = vesselId;
        }

        public override string ToString()
        {
            return VesselId != Guid.Empty ? $"{Type} - {VesselId}" : $"{Type}";
        }
    }
}
