﻿using System;
using DataStorageSolutions.Model;
using FCSCommon.Utilities;
using HarmonyLib;

namespace DataStorageSolutions.Patches
{
    [HarmonyPatch(typeof(Player))]
    [HarmonyPatch("Update")]
    internal class Player_Update
    {
        private static float _timeLeft = 1f;
        private static bool _error;

        [HarmonyPostfix]
        public static void Postfix(ref Player __instance)
        {
            try
            {
                _timeLeft -= DayNightCycle.main.deltaTime;
                if (_timeLeft < 0)
                {
                    BaseManager.RemoveDestroyedBases();
                    BaseManager.OnPlayerTick?.Invoke();
                    BaseManager.PerformOperations();
                    BaseManager.PerformCraft();
                    _timeLeft = 1f;
                }
            }
            catch (Exception e)
            {
                if (!_error)
                {
                    QuickLogger.Error($"Message: {e.Message} | StackTrace: {e.StackTrace}");
                    _error = true;
                }
            }
        }
    }
}
