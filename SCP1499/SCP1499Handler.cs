// -----------------------------------------------------------------------
// <copyright file="SCP1499Handler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Mistaken.API;
using Mistaken.API.Diagnostics;
using Mistaken.API.Extensions;
using Mistaken.API.GUI;
using Mistaken.CustomItems;
using UnityEngine;

namespace Mistaken.SCP1499
{
    /// <inheritdoc/>
    public class SCP1499Handler : Module
    {
        /// <inheritdoc cref="Module.Module(Exiled.API.Interfaces.IPlugin{Exiled.API.Interfaces.IConfig})"/>
        public SCP1499Handler(PluginHandler plugin)
            : base(plugin)
        {
            Instance = this;
            Log = base.Log;
            new SCP1499CustomItem();
        }

        /// <inheritdoc/>
        public override string Name => nameof(SCP1499Handler);

        /// <inheritdoc/>
        public override void OnEnable()
        {
            Exiled.Events.Handlers.Server.RestartingRound += this.Handle(() => this.Server_RestartingRound(), "RoundRestart");
            Exiled.Events.Handlers.Server.RoundStarted += this.Handle(() => this.Server_RoundStarted(), "RoundStart");
            Exiled.Events.Handlers.Player.ChangingRole += this.Handle<Exiled.Events.EventArgs.ChangingRoleEventArgs>((ev) => this.Player_ChangingRole(ev));
            Events.Handlers.CustomEvents.OnRequestPickItem += this.Handle<Events.EventArgs.PickItemRequestEventArgs>((ev) => this.CustomEvents_OnRequestPickItem(ev));
            Exiled.Events.Handlers.Player.InteractingDoor += this.Handle<Exiled.Events.EventArgs.InteractingDoorEventArgs>((ev) => this.Player_InteractingDoor(ev));
            Exiled.Events.Handlers.Scp079.TriggeringDoor += this.Handle<Exiled.Events.EventArgs.TriggeringDoorEventArgs>((ev) => this.Scp079_TriggeringDoor(ev));
        }

        /// <inheritdoc/>
        public override void OnDisable()
        {
            Exiled.Events.Handlers.Server.RestartingRound -= this.Handle(() => this.Server_RestartingRound(), "RoundRestart");
            Exiled.Events.Handlers.Server.RoundStarted -= this.Handle(() => this.Server_RoundStarted(), "RoundStart");
            Exiled.Events.Handlers.Player.ChangingRole -= this.Handle<Exiled.Events.EventArgs.ChangingRoleEventArgs>((ev) => this.Player_ChangingRole(ev));
            Events.Handlers.CustomEvents.OnRequestPickItem -= this.Handle<Events.EventArgs.PickItemRequestEventArgs>((ev) => this.CustomEvents_OnRequestPickItem(ev));
            Exiled.Events.Handlers.Player.InteractingDoor -= this.Handle<Exiled.Events.EventArgs.InteractingDoorEventArgs>((ev) => this.Player_InteractingDoor(ev));
            Exiled.Events.Handlers.Scp079.TriggeringDoor -= this.Handle<Exiled.Events.EventArgs.TriggeringDoorEventArgs>((ev) => this.Scp079_TriggeringDoor(ev));
        }

        /// <inheritdoc/>
        public class SCP1499CustomItem : CustomItem
        {
            /// <inheritdoc cref="CustomItem.Register"/>
            public SCP1499CustomItem() => this.Register();

            /// <inheritdoc/>
            public override string ItemName => "SCP-1499";

            /// <inheritdoc/>
            public override ItemType Item => ItemType.GrenadeFlash;

            /// <inheritdoc/>
            public override SessionVarType SessionVarType => SessionVarType.CI_SCP1499;

            /// <inheritdoc/>
            public override int Durability => 149;

            /// <inheritdoc/>
            public override Vector3 Size => new Vector3(1.5f, 0.5f, 1.5f);

            /// <inheritdoc/>
            public override bool OnThrow(Player player, Inventory.SyncItemInfo item, bool slow)
            {
                if (Warhead.IsDetonated)
                    return true;
                Vector3 target;
                int damage = 0;
                bool enablePocketEffect = false;
                try
                {
                    if (player.Position.y > -1990)
                    {
                        target = new Vector3(0, -1996, 0);
                        enablePocketEffect = true;
                        if (cooldown.Ticks > DateTime.Now.Ticks)
                            return false;
                    }
                    else
                        target = (slow ? SecondFlashPosition : FirstFlashPosition) + new Vector3(0, 1, 0);
                    cooldown = DateTime.Now.AddSeconds(CooldownLength);
                    if (target == default)
                        target = new Vector3(0, 1002, 0);
                    Instance.RunCoroutine(Instance.Use1499(player, target, enablePocketEffect, damage), "SCP1499.Use1499");
                }
                catch (System.Exception ex)
                {
                    Log.Error(ex.Message);
                    Log.Error(ex.StackTrace);
                }

                return false;
            }

            /// <inheritdoc/>
            public override void OnStartHolding(Player player, Inventory.SyncItemInfo item)
            {
                Instance.RunCoroutine(Instance.UpdateFlashCooldown(player), "SCP1499.UpdateFlashCooldown");
                player.SetGUI("scp1499", PseudoGUIPosition.BOTTOM, PluginHandler.Instance.Translation.Holding);
            }

            /// <inheritdoc/>
            public override void OnStopHolding(Player player, Inventory.SyncItemInfo item)
            {
                player.SetGUI("scp1499", PseudoGUIPosition.BOTTOM, null);
            }

            /// <inheritdoc/>
            public override void OnForceclass(Player player)
            {
                base.OnForceclass(player);
                player.SetGUI("scp1499", PseudoGUIPosition.BOTTOM, null);
            }

            internal const float CooldownLength = 90;
        }

        private static readonly HashSet<RoomType> DisallowedRoomTypes = new HashSet<RoomType>
        {
            RoomType.EzShelter,
            RoomType.EzCollapsedTunnel,
            RoomType.HczTesla,
            RoomType.Lcz173,
            RoomType.Hcz939,
            RoomType.LczArmory,
            RoomType.Pocket,
        };

        private static DateTime cooldown = default;
        private static Room firstFlashRoom;
        private static Room secondFlashRoom;
        private static Room[] rooms;

        private static Room RandomRoom => rooms[UnityEngine.Random.Range(0, rooms.Length)] ?? RandomRoom;

        private static Vector3 FirstFlashPosition => firstFlashRoom?.gameObject == null ? default : firstFlashRoom.Position;

        private static Vector3 SecondFlashPosition => secondFlashRoom?.gameObject == null ? default : secondFlashRoom.Position;

        private static new ModuleLogger Log { get; set; }

        private static SCP1499Handler Instance { get; set; }

        private static string ForceLength(string input, int length)
        {
            while (input.Length < length)
                input += " ";
            return input;
        }

        private static string GetTimeColor(float time)
        {
            try
            {
                var input = Math.Round((1 - (time / MapPlus.DecontaminationEndTime)) * 8);
                string tor = string.Empty;
                tor += GetHexChar((int)input % 16);
                input /= 16;
                return GetHexChar((int)input % 16) + tor;
            }
            catch
            {
                return "00FF00";
            }
        }

        private static string GetHexChar(int input)
        {
            switch (input)
            {
                case 10:
                    return "A";
                case 11:
                    return "B";
                case 12:
                    return "C";
                case 13:
                    return "D";
                case 14:
                    return "E";
                case 15:
                    return "F";
                default:
                    return input.ToString();
            }
        }

        private static string GetRoomColor(Room room)
        {
            try
            {
                if (room?.Players?.Any(p => p?.IsAlive ?? false) ?? true)
                    return "0000";
                float nearest = 9999;
                foreach (var player in RealPlayers.List.Where(p => p?.IsAlive ?? false))
                {
                    float distance = Vector3.Distance(player.Position, room.Position);
                    if (distance < nearest)
                        nearest = distance;
                }

                if (nearest > 25)
                    return "FFFF";
                else if (nearest > 15)
                    return "b5b5";
                else
                    return "6666";
            }
            catch
            {
                return "00FF00";
            }
        }

        private static Room GetFreeRoom(Room current)
        {
            Room targetRoom = RandomRoom;
            int trie = 0;
            while (!IsRoomOK(targetRoom) || (current?.Position == targetRoom.Position || FirstFlashPosition == targetRoom.Position || SecondFlashPosition == targetRoom.Position || (current != firstFlashRoom && firstFlashRoom?.Zone == targetRoom?.Zone) || (current != secondFlashRoom && secondFlashRoom?.Zone == targetRoom?.Zone)))
            {
                targetRoom = RandomRoom;
                trie++;
                if (trie >= 1000)
                {
                    Log.Error("Failed to generate teleport position in 1000 tries");
                    return Map.Rooms.First(r => r.Type == RoomType.Surface);
                }
            }

            Log.Debug($"New position is {targetRoom.Position} | {targetRoom.Zone}", PluginHandler.VerbouseOutput);
            return targetRoom;
        }

        private static bool IsRoomOK(Room room)
        {
            if (room == null)
                return false;
            if (DisallowedRoomTypes.Contains(room.Type))
                return false;
            if (MapPlus.IsLCZDecontaminated(60) && room.Zone == ZoneType.LightContainment)
                return false;
            if (!UnityEngine.Physics.Raycast(room.Position + (Vector3.up / 2), Vector3.down, 5))
                return false;
            return true;
        }

        private void Scp079_TriggeringDoor(Exiled.Events.EventArgs.TriggeringDoorEventArgs ev)
        {
            if (ev.Door.GetNametag() == "SCP1499Chamber")
            {
                if (ev.Door.NetworkTargetState)
                    ev.IsAllowed = false;
                ev.AuxiliaryPowerCost = 110;
            }
        }

        private void CustomEvents_OnRequestPickItem(Events.EventArgs.PickItemRequestEventArgs ev)
        {
            if (
                (ev.Pickup.ItemId == ItemType.GrenadeFlash && ev.Pickup.durability == 149000f && ev.Player.Inventory.items.Any(i => i.id == ItemType.SCP268)) ||
                (ev.Pickup.ItemId == ItemType.SCP268 && ev.Player.Inventory.items.Any(i => i.id == ItemType.GrenadeFlash && i.durability == 149000f)))
            {
                ev.IsAllowed = false;
                ev.Player.SetGUI("scp1499&scp268", PseudoGUIPosition.MIDDLE, PluginHandler.Instance.Translation.Info268, 5);
            }
        }

        private IEnumerator<float> UpdateFlashCooldown(Player player)
        {
            yield return MEC.Timing.WaitForSeconds(0.1f);
            while (Round.IsStarted && player?.CurrentItem.id == ItemType.GrenadeFlash && player?.CurrentItem.durability == 149000f)
            {
                try
                {
                    var cooldown = Math.Round((SCP1499Handler.cooldown - DateTime.Now).TotalSeconds);
                    string message;
                    if ((cooldown >= 0) && player?.Position.y > -1900)
                        message = string.Format(PluginHandler.Instance.Translation.InfoCooldown, cooldown);
                    else
                        message = PluginHandler.Instance.Translation.InfoReady;
                    if (MapPlus.IsLCZDecontaminated(45))
                    {
                        if (firstFlashRoom?.Zone == ZoneType.LightContainment)
                            firstFlashRoom = GetFreeRoom(firstFlashRoom);
                        if (secondFlashRoom?.Zone == ZoneType.LightContainment)
                            secondFlashRoom = GetFreeRoom(secondFlashRoom);
                    }

                    message += string.Format(PluginHandler.Instance.Translation.InfoFirst, ForceLength(firstFlashRoom?.Zone.ToString(), 16), GetRoomColor(firstFlashRoom), ForceLength(firstFlashRoom?.Type.ToString(), 15));
                    message += string.Format(PluginHandler.Instance.Translation.InfoSecond, ForceLength(secondFlashRoom?.Zone.ToString(), 16), GetRoomColor(secondFlashRoom), ForceLength(secondFlashRoom?.Type.ToString(), 15));
                    if (!MapPlus.IsLCZDecontaminated(out float lczTime))
                        message += string.Format(PluginHandler.Instance.Translation.InfoDecont, GetTimeColor(lczTime), ((lczTime - (lczTime % 60)) / 60).ToString("00"), Mathf.RoundToInt(lczTime % 60).ToString("00"));
                    int pulse = ((int)Round.ElapsedTime.TotalSeconds % 8) + 4;
                    if (player.IsConnected)
                        player.SetGUI("scp1499", PseudoGUIPosition.BOTTOM, $"{message}<br>{PluginHandler.Instance.Translation.Holding}>");
                }
                catch (System.Exception ex)
                {
                    Log.Error(ex.Message);
                    Log.Error(ex.StackTrace);
                }

                yield return MEC.Timing.WaitForSeconds(1f);
            }
        }

        private void Player_ChangingRole(Exiled.Events.EventArgs.ChangingRoleEventArgs ev)
        {
            if (ev.IsEscaped || ev.NewRole == RoleType.Spectator)
            {
                if (ev.Player.Inventory.items.Any(i => i.id == ItemType.GrenadeFlash && i.durability == 149000f))
                {
                    ev.Player.RemoveItem(ev.Player.Inventory.items.First(i => i.id == ItemType.GrenadeFlash));
                    var pos = ev.Player.Position;
                    this.CallDelayed(
                        1,
                        () =>
                        {
                            MapPlus.Spawn(
                                new Inventory.SyncItemInfo
                                {
                                    id = ItemType.GrenadeFlash,
                                    durability = 149000f,
                                },
                                ev.Player.Role == RoleType.Spectator ? pos : ev.Player.Position,
                                Quaternion.identity,
                                new Vector3(1.5f, 0.5f, 1.5f));
                        },
                        "ChanginRole");
                }
            }
        }

        private void Server_RoundStarted()
        {
            var positionToSpawn = new Vector3(-26, 1020, -44);
            this.CallDelayed(
                5,
                () =>
                {
                    var tmp = MapPlus.Spawn(
                        new Inventory.SyncItemInfo
                        {
                            id = ItemType.GrenadeFlash,
                            durability = 149000f,
                        },
                        Vector3.zero,
                        Quaternion.identity,
                        new Vector3(1.5f, 0.5f, 1.5f));
                    this.CallDelayed(5, () => tmp.Delete(), "RoundStart2");
                    MapPlus.Spawn(
                        new Inventory.SyncItemInfo
                        {
                            id = ItemType.GrenadeFlash,
                            durability = 149000f,
                        },
                        positionToSpawn,
                        Quaternion.identity,
                        new Vector3(1.5f, 0.5f, 1.5f));
                },
                "RoundStart1");

            rooms = Map.Rooms.ToArray();
            firstFlashRoom = GetFreeRoom(null);
            secondFlashRoom = GetFreeRoom(null);
        }

        private void Server_RestartingRound()
        {
            firstFlashRoom = null;
            secondFlashRoom = null;
        }

        private void Player_InteractingDoor(Exiled.Events.EventArgs.InteractingDoorEventArgs ev)
        {
            if (ev.Door.GetNametag() != "SCP1499Chamber")
                return;
            if (ev.Door.NetworkTargetState)
            {
                ev.IsAllowed = false;
                return;
            }

            if (!ev.Player.IsBypassModeEnabled && ev.Player.Role != RoleType.Scp079)
            {
                var currentItemType = ev.Player.CurrentItem.id;
                if (!(currentItemType == ItemType.KeycardO5 || currentItemType == ItemType.KeycardFacilityManager || currentItemType == ItemType.KeycardContainmentEngineer))
                {
                    ev.IsAllowed = false;
                    ev.Player.SetGUI("scp1499Door", PseudoGUIPosition.MIDDLE, PluginHandler.Instance.Translation.InfoDenied, 5);
                }
            }
        }

        private IEnumerator<float> Use1499(Player player, Vector3 pos, bool pocket, int damage)
        {
            yield return MEC.Timing.WaitForSeconds(1f);
            if (!player.IsConnected)
                yield break;
            ReferenceHub rh = player.ReferenceHub;
            var pec = rh.playerEffectsController;
            try
            {
                player.Position = pos;
                if (pocket)
                {
                    pec.GetEffect<CustomPlayerEffects.Corroding>().IsInPd = true;
                    player.EnableEffect<CustomPlayerEffects.Corroding>();
                }
                else
                {
                    player.EnableEffect<CustomPlayerEffects.Flashed>(10);
                    player.EnableEffect<CustomPlayerEffects.Blinded>(15);
                    player.EnableEffect<CustomPlayerEffects.Deafened>(15);
                    player.EnableEffect<CustomPlayerEffects.Concussed>(30);

                    firstFlashRoom = GetFreeRoom(firstFlashRoom);
                    secondFlashRoom = GetFreeRoom(secondFlashRoom);
                }

                if (damage != 0)
                    player.Health -= damage;
            }
            catch (System.Exception ex)
            {
                Log.Error(ex.Message);
                Log.Error(ex.StackTrace);
            }

            if (!pocket)
            {
                yield return MEC.Timing.WaitForSeconds(10);
                try
                {
                    if (!player.IsInPocketDimension)
                    {
                        pec.GetEffect<CustomPlayerEffects.Corroding>().IsInPd = false;
                        player.DisableEffect<CustomPlayerEffects.Corroding>();
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error(ex.Message);
                    Log.Error(ex.StackTrace);
                }
            }
        }
    }
}
