﻿namespace LunaCommon.Enums
{
    public enum ClientState
    {
        DisconnectRequested = -1,
        Disconnected = 0,
        Connecting = 1,
        Connected = 2,
        Handshaking = 3,
        Authenticated = 4,

        TimeSyncing = 5,
        TimeSynced = 6,
        SyncingKerbals = 7,
        KerbalsSynced = 8,
        SyncingSettings = 9,
        SettingsSynced = 10,
        SyncingWarpsubspaces = 11,
        WarpsubspacesSynced = 12,
        SyncingColors = 13,
        ColorsSynced = 14,
        SyncingPlayers = 15,
        PlayersSynced = 16,
        SyncingScenarios = 17,
        ScneariosSynced = 18,
        SyncingCraftlibrary = 19,
        CraftlibrarySynced = 20,
        SyncingChat = 21,
        ChatSynced = 22,
        SyncingLocks = 23,
        LocksSynced = 24,
        SyncingAdmins = 25,
        AdminsSynced = 26,
        SyncingGroups = 27,
        GroupsSynced = 28,
        SyncingVessels = 29,
        VesselsSynced = 30,
        TimeLocking = 31,
        TimeLocked = 32,

        Starting = 33,
        Running = 34
    }
}