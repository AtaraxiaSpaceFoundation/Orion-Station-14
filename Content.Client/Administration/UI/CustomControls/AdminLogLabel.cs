// SPDX-FileCopyrightText: 2021 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2021 Javier Guardia Fernández <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 mirrorcult <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Administration.Logs;
using Content.Shared.Database; // Orion-Edit
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility; // Orion-Edit

namespace Content.Client.Administration.UI.CustomControls;

public sealed class AdminLogLabel : RichTextLabel
{
    public AdminLogLabel(ref SharedAdminLog log, HSeparator separator)
    {
        Log = log;
        Separator = separator;
        // Orion-Edit Start
        var impactColor = GetImpactColor(log.Impact);
        var impactText = $"[color={impactColor}]█[/color]";

        var formatted = new FormattedMessage();
        formatted.AddMarkupOrThrow($"{impactText} [bold]{log.Date:HH:mm:ss}[/bold]: {log.Message}");

        SetMessage(formatted);
        // Orion-Edit End
        OnVisibilityChanged += VisibilityChanged;
    }

    public SharedAdminLog Log { get; }

    public HSeparator Separator { get; }

    private void VisibilityChanged(Control control)
    {
        Separator.Visible = Visible;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        OnVisibilityChanged -= VisibilityChanged;
    }
    // Orion-Edit Start
    private static string GetImpactColor(LogImpact impact) => impact switch
    {
        LogImpact.Extreme => "red",
        LogImpact.High => "orange",
        LogImpact.Medium => "yellow",
        LogImpact.Low => "lightgreen",
        _ => "gray"
    };
    // Orion-Edit End
}
