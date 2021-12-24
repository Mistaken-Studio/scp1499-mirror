// -----------------------------------------------------------------------
// <copyright file="SCP1499CustomItem.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Spawn;
using Exiled.CustomItems.API.EventArgs;
using Exiled.Events.EventArgs;
using MEC;
using Mistaken.API;
using Mistaken.API.Extensions;
using Mistaken.API.GUI;
using UnityEngine;

namespace Mistaken.SCP1499
{
    /// <inheritdoc/>
    public class SCP1499CustomItem : Mistaken.API.CustomItems.MistakenCustomGrenade
    {
        /// <inheritdoc/>
        public override bool ExplodeOnCollision { get; set; } = false;

        /// <inheritdoc/>
        public override float FuseTime { get; set; } = 0.1f;

        /// <inheritdoc/>
        public override API.CustomItems.MistakenCustomItems CustomItem => API.CustomItems.MistakenCustomItems.SCP_1499;

        /// <inheritdoc/>
        public override string Name { get; set; } = "SCP-1499";

        /// <inheritdoc/>
        public override string Description { get; set; } = "SCP 1499";

        /// <inheritdoc/>
        public override float Weight { get; set; } = 1;

        /// <inheritdoc/>
        public override string DisplayName { get; set; } = "<color=red>SCP-1499</color>";

        /// <inheritdoc/>
        public override SpawnProperties SpawnProperties { get; set; } = new SpawnProperties()
        {
            StaticSpawnPoints = new List<StaticSpawnPoint>
            {
                new StaticSpawnPoint
                {
                    Chance = 100,
                    Name = "SCP 1499 Containment Chamber",
                    Position = new Vector3(-26, 1020, -44),
                },
            },
        };

        /// <inheritdoc/>
        public override ItemType Type => ItemType.GrenadeFlash;

        /// <inheritdoc/>
        public override Pickup Spawn(Vector3 position, Item item)
        {
            var pickup = base.Spawn(position, item);
            pickup.Scale = new Vector3(1.5f, 0.5f, 1.5f);
            return pickup;
        }

        /// <inheritdoc/>
        public override Pickup Spawn(Vector3 position)
        {
            var pickup = base.Spawn(position);
            pickup.Scale = new Vector3(1.5f, 0.5f, 1.5f);
            return pickup;
        }

        internal const float CooldownLength = 90;

        /// <inheritdoc/>
        protected override void OnThrowing(ThrowingItemEventArgs ev)
        {
            if (Warhead.IsDetonated)
                return;
            if (!ev.Player.IsConnected)
                return;
            ev.IsAllowed = false;

            Timing.RunCoroutine(this.Use1499(ev), "Use1499");
        }

        /// <inheritdoc/>
        protected override void OnOwnerChangingRole(OwnerChangingRoleEventArgs ev)
        {
            base.OnOwnerChangingRole(ev);
            ev.Player.SetGUI("scp1499", PseudoGUIPosition.BOTTOM, null);
        }

        /// <inheritdoc/>
        protected override void OnOwnerEscaping(OwnerEscapingEventArgs ev)
        {
            base.OnOwnerEscaping(ev);
            ev.Player.SetGUI("scp1499", PseudoGUIPosition.BOTTOM, null);
        }

        /// <inheritdoc/>
        protected override void OnOwnerHandcuffing(OwnerHandcuffingEventArgs ev)
        {
            base.OnOwnerHandcuffing(ev);
            ev.Target.SetGUI("scp1499", PseudoGUIPosition.BOTTOM, null);
        }

        /// <inheritdoc/>
        protected override void OnWaitingForPlayers()
        {
            this.firstFlashRoom = null;
            this.secondFlashRoom = null;

            base.OnWaitingForPlayers();

            this.rooms = Map.Rooms.ToArray();
            this.firstFlashRoom = this.GetFreeRoom(null);
            this.secondFlashRoom = this.GetFreeRoom(null);
        }

        /// <inheritdoc/>
        protected override void ShowSelectedMessage(Player player)
        {
        }

        /// <inheritdoc/>
        protected override void OnChanging(ChangingItemEventArgs ev)
        {
            base.OnChanging(ev);
            Timing.RunCoroutine(this.UpdateFlashCooldown(ev.Player), "UpdateFlashCooldown");
            ev.Player.SetGUI("scp1499", PseudoGUIPosition.BOTTOM, PluginHandler.Instance.Translation.Holding);
        }

        /// <inheritdoc/>
        protected override void OnHiding(ChangingItemEventArgs ev)
        {
            base.OnHiding(ev);
            ev.Player.SetGUI("scp1499", PseudoGUIPosition.BOTTOM, null);
        }

        /// <inheritdoc/>
        protected override void SubscribeEvents()
        {
            base.SubscribeEvents();

            Events.Handlers.CustomEvents.RequestPickItem += this.CustomEvents_OnRequestPickItem;
            Exiled.Events.Handlers.Player.InteractingDoor += this.Player_InteractingDoor;
            Exiled.Events.Handlers.Scp079.TriggeringDoor += this.Scp079_TriggeringDoor;
        }

        /// <inheritdoc/>
        protected override void UnsubscribeEvents()
        {
            base.UnsubscribeEvents();

            Events.Handlers.CustomEvents.RequestPickItem -= this.CustomEvents_OnRequestPickItem;
            Exiled.Events.Handlers.Player.InteractingDoor -= this.Player_InteractingDoor;
            Exiled.Events.Handlers.Scp079.TriggeringDoor -= this.Scp079_TriggeringDoor;
        }

        private readonly HashSet<RoomType> disallowedRoomTypes = new HashSet<RoomType>
        {
            RoomType.EzShelter,
            RoomType.EzCollapsedTunnel,
            RoomType.HczTesla,
            RoomType.Lcz173,
            RoomType.Hcz939,
            RoomType.LczArmory,
            RoomType.Pocket,
        };

        private DateTime cooldown = default;
        private Room firstFlashRoom;
        private Room secondFlashRoom;
        private Room[] rooms;

        private Room RandomRoom => this.rooms[UnityEngine.Random.Range(0, this.rooms.Length)] ?? this.RandomRoom;

        private Vector3 FirstFlashPosition => this.firstFlashRoom?.gameObject == null ? default : this.firstFlashRoom.Position;

        private Vector3 SecondFlashPosition => this.secondFlashRoom?.gameObject == null ? default : this.secondFlashRoom.Position;

        private void Scp079_TriggeringDoor(Exiled.Events.EventArgs.TriggeringDoorEventArgs ev)
        {
            if (ev.Door.Nametag == "SCP1499Chamber")
            {
                if (ev.Door.IsOpen)
                    ev.IsAllowed = false;
                ev.AuxiliaryPowerCost = 110;
            }
        }

        private void CustomEvents_OnRequestPickItem(Events.EventArgs.PickItemRequestEventArgs ev)
        {
            if (
                (ev.Pickup.Type == ItemType.GrenadeFlash && this.Check(ev.Pickup) && ev.Player.Items.Any(i => i.Type == ItemType.SCP268)) ||
                (ev.Pickup.Type == ItemType.SCP268 && ev.Player.Items.Any(i => this.Check(i))))
            {
                ev.IsAllowed = false;
                ev.Player.SetGUI("scp1499&scp268", PseudoGUIPosition.MIDDLE, PluginHandler.Instance.Translation.Info268, 5);
            }
        }

        private IEnumerator<float> UpdateFlashCooldown(Player player)
        {
            yield return MEC.Timing.WaitForSeconds(0.1f);
            while (Round.IsStarted && this.Check(player?.CurrentItem))
            {
                try
                {
                    var cooldown = Math.Round((this.cooldown - DateTime.Now).TotalSeconds);
                    string message;
                    if ((cooldown >= 0) && player?.Position.y > -1900)
                        message = string.Format(PluginHandler.Instance.Translation.InfoCooldown, cooldown);
                    else
                        message = PluginHandler.Instance.Translation.InfoReady;
                    if (MapPlus.IsLCZDecontaminated(45))
                    {
                        if (this.firstFlashRoom?.Zone == ZoneType.LightContainment)
                            this.firstFlashRoom = this.GetFreeRoom(this.firstFlashRoom);
                        if (this.secondFlashRoom?.Zone == ZoneType.LightContainment)
                            this.secondFlashRoom = this.GetFreeRoom(this.secondFlashRoom);
                    }

                    message += string.Format(PluginHandler.Instance.Translation.InfoFirst, this.ForceLength(this.firstFlashRoom?.Zone.ToString(), 16), this.GetRoomColor(this.firstFlashRoom), this.ForceLength(this.firstFlashRoom?.Type.ToString(), 15));
                    message += string.Format(PluginHandler.Instance.Translation.InfoSecond, this.ForceLength(this.secondFlashRoom?.Zone.ToString(), 16), this.GetRoomColor(this.secondFlashRoom), this.ForceLength(this.secondFlashRoom?.Type.ToString(), 15));
                    if (player.IsConnected)
                        player.SetGUI("scp1499", PseudoGUIPosition.BOTTOM, $"{message}"); // <br>{PluginHandler.Instance.Translation.Holding}
                }
                catch (System.Exception ex)
                {
                    Log.Error(ex.Message);
                    Log.Error(ex.StackTrace);
                }

                yield return MEC.Timing.WaitForSeconds(1f);
            }

            player.SetGUI("scp1499", PseudoGUIPosition.BOTTOM, null);
        }

        private void Player_InteractingDoor(Exiled.Events.EventArgs.InteractingDoorEventArgs ev)
        {
            if (ev.Door.Nametag != "SCP1499Chamber")
                return;
            if (ev.Door.IsOpen)
            {
                ev.IsAllowed = false;
                return;
            }

            if (!ev.Player.IsBypassModeEnabled && ev.Player.Role != RoleType.Scp079)
            {
                var currentItemType = ev.Player.CurrentItem.Type;
                if (!(currentItemType == ItemType.KeycardO5 || currentItemType == ItemType.KeycardFacilityManager || currentItemType == ItemType.KeycardContainmentEngineer))
                {
                    ev.IsAllowed = false;
                    ev.Player.SetGUI("scp1499Door", PseudoGUIPosition.MIDDLE, PluginHandler.Instance.Translation.InfoDenied, 5);
                }
            }
        }

        private IEnumerator<float> Use1499(ThrowingItemEventArgs ev)
        {
            Vector3 target;
            bool enablePocketEffect = false;

            if (ev.Player.Position.y > -1990)
            {
                if (this.cooldown.Ticks > DateTime.Now.Ticks)
                {
                    if (ev.RequestType != ThrowRequest.BeginThrow)
                    {
                        var item = ev.Player.CurrentItem;
                        if (!ev.Player.RemoveItem(item, false))
                            Log.Warn("Failed to remove item");
                        yield return MEC.Timing.WaitForSeconds(.1f);
                        ev.Player.AddItem(item);
                        ev.Player.CurrentItem = item;
                    }

                    yield break;
                }

                this.Throw(ev.Player.Position, 0, .1f, grenadeType: ItemType.GrenadeFlash, ev.Player);
            }
            else if (ev.RequestType == ThrowRequest.BeginThrow)
                yield break;

            try
            {
                if (ev.RequestType == ThrowRequest.BeginThrow)
                {
                    target = new Vector3(0, -1996, 0);
                    enablePocketEffect = true;
                }
                else
                    target = (ev.RequestType == ThrowRequest.WeakThrow ? this.SecondFlashPosition : this.FirstFlashPosition) + new Vector3(0, 1, 0);
                this.cooldown = DateTime.Now.AddSeconds(CooldownLength);
                if (target == default)
                    target = new Vector3(0, 1002, 0);
                try
                {
                    ev.Player.Position = target;
                    if (enablePocketEffect)
                        ev.Player.EnableEffect<CustomPlayerEffects.Corroding>();
                    else
                    {
                        ev.Player.EnableEffect<CustomPlayerEffects.Flashed>(10);
                        ev.Player.EnableEffect<CustomPlayerEffects.Blinded>(15);
                        ev.Player.EnableEffect<CustomPlayerEffects.Deafened>(15);
                        ev.Player.EnableEffect<CustomPlayerEffects.Concussed>(30);

                        this.firstFlashRoom = this.GetFreeRoom(this.firstFlashRoom);
                        this.secondFlashRoom = this.GetFreeRoom(this.secondFlashRoom);
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error(ex.Message);
                    Log.Error(ex.StackTrace);
                }
            }
            catch (System.Exception ex)
            {
                Log.Error(ex.Message);
                Log.Error(ex.StackTrace);
            }

            if (ev.RequestType != ThrowRequest.BeginThrow)
            {
                var item = ev.Player.CurrentItem;
                if (!ev.Player.RemoveItem(item, false))
                    Log.Warn("Failed to remove item");
                while (ev.Player.Items.Any(x => x.Serial == item.Serial))
                    ev.Player.RemoveItem(ev.Player.Items.First(x => x.Serial == item.Serial), false);
                yield return MEC.Timing.WaitForSeconds(.1f);
                ev.Player.AddItem(item);
                ev.Player.CurrentItem = item;
            }

            if (!enablePocketEffect)
            {
                yield return MEC.Timing.WaitForSeconds(10);
                try
                {
                    if (!ev.Player.IsInPocketDimension)
                        ev.Player.DisableEffect<CustomPlayerEffects.Corroding>();
                }
                catch (System.Exception ex)
                {
                    Log.Error(ex.Message);
                    Log.Error(ex.StackTrace);
                }
            }
        }

        private string ForceLength(string input, int length)
        {
            while (input.Length < length)
                input += " ";
            return input;
        }

        private string GetRoomColor(Room room)
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

        private Room GetFreeRoom(Room current)
        {
            Room targetRoom = this.RandomRoom;
            int trie = 0;
            while (!this.IsRoomOK(targetRoom) || (current?.Position == targetRoom.Position || this.FirstFlashPosition == targetRoom.Position || this.SecondFlashPosition == targetRoom.Position || (current != this.firstFlashRoom && this.firstFlashRoom?.Zone == targetRoom?.Zone) || (current != this.secondFlashRoom && this.secondFlashRoom?.Zone == targetRoom?.Zone)))
            {
                targetRoom = this.RandomRoom;
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

        private bool IsRoomOK(Room room)
        {
            if (room == null)
                return false;
            if (this.disallowedRoomTypes.Contains(room.Type))
                return false;
            if (MapPlus.IsLCZDecontaminated(60) && room.Zone == ZoneType.LightContainment)
                return false;
            if (!UnityEngine.Physics.Raycast(room.Position + (Vector3.up / 2), Vector3.down, 5))
                return false;
            return true;
        }
    }
}
