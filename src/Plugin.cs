using System;
using AmongUs.GameOptions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace ImpostorForce
{
    [BepInPlugin("local.impostorforce", "ImpostorForce", "1.0.0")]
    public class Plugin : BasePlugin
    {
        internal static ManualLogSource Logger;
        internal static ConfigEntry<bool> Force;
        internal static ConfigEntry<KeyCode> MenuKey;
        internal static ConfigEntry<KeyCode> ToggleKey;

        public override void Load()
        {
            Logger = Log;
            Force = Config.Bind("General", "ForceImpostor", false, "Force the local player onto the impostor team at the next game start (host only).");
            MenuKey = Config.Bind("Keys", "MenuKey", KeyCode.Delete, "Opens and closes the menu.");
            ToggleKey = Config.Bind("Keys", "ToggleKey", KeyCode.F6, "Toggles force impostor and shows a notification.");

            ClassInjector.RegisterTypeInIl2Cpp<Ui>();
            AddComponent<Ui>();
            new Harmony("local.impostorforce").PatchAll();
            Log.LogInfo("ImpostorForce loaded");
        }
    }

    // Role calls inside RoleManager.SelectRoles are re-targeted so the local player takes the first
    // impostor-team role and that player takes the local role. The role multiset stays exactly as vanilla assigned it.
    internal static class Roles
    {
        internal static bool InSelect;
        internal static bool Guard;

        private static PlayerControl displaced;
        private static bool swapped;
        private static bool localImpostor;
        private static bool localSeen;
        private static RoleTypes localRole;
        internal static bool Fixup;

        internal static PlayerControl Displaced => displaced;
        internal static RoleTypes LocalRole => localRole;

        internal static void Reset()
        {
            displaced = null;
            swapped = false;
            localImpostor = false;
            localSeen = false;
            localRole = RoleTypes.Crewmate;
            Fixup = false;
        }

        // [enum names | dump.cs build 7489 2026-11-23 | team membership is an assumption, not in the dump]
        private static bool IsImpostorTeam(RoleTypes r) =>
            r == RoleTypes.Impostor || r == RoleTypes.Shapeshifter || r == RoleTypes.Phantom || r == RoleTypes.Viper;

        internal static PlayerControl Route(PlayerControl player, RoleTypes role)
        {
            PlayerControl local = PlayerControl.LocalPlayer;
            if (local == null) return player;

            if (player == local)
            {
                if (IsImpostorTeam(role))
                {
                    localImpostor = true;
                    return player;
                }
                if (swapped) return displaced;
                localSeen = true;
                localRole = role;
                return player;
            }
            if (IsImpostorTeam(role) && !swapped && !localImpostor)
            {
                displaced = player;
                swapped = true;
                Fixup = localSeen;
                return local;
            }
            return player;
        }

        internal static string Summary() =>
            swapped ? "swapped with the first impostor-team player"
            : localImpostor ? "already impostor-team, nothing to swap"
            : "no impostor-team role seen, nothing swapped";
    }

    // Only the host runs SelectRoles, so this is host-only by construction.
    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    internal static class SelectRolesPatch
    {
        public static void Prefix()
        {
            Roles.Reset();
            Roles.InSelect = true;
        }

        public static Exception Finalizer(Exception __exception)
        {
            Roles.InSelect = false;
            if (Plugin.Force.Value) Plugin.Logger.LogInfo("SelectRoles done: " + Roles.Summary());
            return __exception;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
    internal static class RpcSetRolePatch
    {
        public static bool Prefix(PlayerControl __instance, RoleTypes roleType, bool canOverrideRole)
        {
            if (!Roles.InSelect || Roles.Guard || !Plugin.Force.Value) return true;

            PlayerControl target = Roles.Route(__instance, roleType);
            if (target == __instance) return true;

            Plugin.Logger.LogInfo("RpcSetRole " + roleType + " redirected to another player");
            Roles.Guard = true;
            try
            {
                target.RpcSetRole(roleType, canOverrideRole);
                if (Roles.Fixup)
                {
                    Roles.Fixup = false;
                    Roles.Displaced.RpcSetRole(Roles.LocalRole, true);
                }
            }
            finally
            {
                Roles.Guard = false;
            }
            return false;
        }
    }

    public class Ui : MonoBehaviour
    {
        private bool open;
        private bool announced;

        public Ui(IntPtr ptr) : base(ptr) { }

        private static void Notify(string text)
        {
            if (HudManager.Instance == null || HudManager.Instance.Notifier == null)
            {
                Plugin.Logger.LogInfo(text);
                return;
            }
            HudManager.Instance.Notifier.AddDisconnectMessage(text);
        }

        private static void SetForce(bool on)
        {
            Plugin.Force.Value = on;
            Notify("Force impostor: " + (on ? "ON" : "OFF"));
        }

        public void Update()
        {
            if (!announced && HudManager.Instance != null && HudManager.Instance.Notifier != null)
            {
                announced = true;
                Notify("ImpostorForce loaded (" + Plugin.MenuKey.Value + " menu, " + Plugin.ToggleKey.Value + " toggle)");
            }
            if (Input.GetKeyDown(Plugin.MenuKey.Value)) open = !open;
            if (Input.GetKeyDown(Plugin.ToggleKey.Value)) SetForce(!Plugin.Force.Value);
        }

        public void OnGUI()
        {
            if (!open) return;
            GUI.Box(new Rect(20, 20, 280, 96), "ImpostorForce");
            bool now = GUI.Toggle(new Rect(36, 50, 250, 24), Plugin.Force.Value, " Force me impostor (host only)");
            if (now != Plugin.Force.Value) SetForce(now);
            GUI.Label(new Rect(36, 80, 250, 24), Plugin.ToggleKey.Value + " toggles, " + Plugin.MenuKey.Value + " closes");
        }
    }
}
