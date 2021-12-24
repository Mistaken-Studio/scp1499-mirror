// -----------------------------------------------------------------------
// <copyright file="Translation.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#pragma warning disable CS1591 // Brak komentarza XML dla widocznego publicznie typu lub składowej

using System;
using Exiled.API.Enums;
using Exiled.API.Interfaces;
using Mistaken.API;

namespace Mistaken.SCP1499
{
    /// <inheritdoc/>
    public class Translation : ITranslation
    {
        public string InfoCooldown { get; set; } = "<color=red>SCP 1499</color> is on cooldown for next <color=yellow>{0}</color> seconds";

        public string InfoReady { get; set; } = "<color=red>SCP 1499</color> is <color=yellow>ready</color>";

        public string InfoFirst { get; set; } = "<br>[<color=yellow>LMB</color>] <color=yellow>{0} <color=#FF{1}>|</color> {2}</color>";

        public string InfoSecond { get; set; } = "<br>[<color=yellow>RMB</color>] <color=yellow>{0} <color=#FF{1}>|</color> {2}</color>";

        // public string InfoDecont { get; set; } = "<br><color=#FFFF00{0}>LCZ Decontamination in {1}m {2}s</color>";
        public string Info268 { get; set; } = "You can't have both <color=yellow>SCP 268</color> and <color=yellow>SCP 1499</color> at the same time";

        public string InfoDenied { get; set; } = "<b><color=red>Access Denied</color></b><br>This door <color=yellow>require</color> <b>Containment Level 3</b> access";

        public string Holding { get; set; } = "You are holding <color=yellow>SCP-1499</color>";
    }
}
