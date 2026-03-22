// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 FaDeOkno <143940725+FaDeOkno@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 FaDeOkno <logkedr18@gmail.com>
// SPDX-FileCopyrightText: 2025 coderabbitai[bot] <136622811+coderabbitai[bot]@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Goobstation.Client.Research.UI;

/// <summary>
/// UI element for visualizing technologies prerequisites
/// </summary>
public sealed class ResearchesContainerPanel : LayoutContainer // Orion-Edit: Was partial
{
    // Orion-Start
    private const int DashedLineDistanceThreshold = 6;
    private const int MaxLineDistanceThreshold = 12;
    private const float DashLength = 8f;
    private const float DashGap = 5f;
    // Orion-End

    // Orion-Edit-Start
    protected override void Draw(DrawingHandleScreen handle)
    {
        var items = new Dictionary<string, FancyResearchConsoleItem>();
        foreach (var child in Children.OfType<FancyResearchConsoleItem>())
        {
            items.TryAdd(child.Prototype.ID, child);
        }

        foreach (var item in items.Values)
        {
            if (item.Prototype.TechnologyPrerequisites.Count <= 0)
                continue;

            foreach (var prerequisiteId in item.Prototype.TechnologyPrerequisites)
            {
                if (!items.TryGetValue(prerequisiteId, out var prerequisite))
                    continue;

                DrawConnection(handle, item, prerequisite);
            }
        }
    }
    // Orion-Edit-End

    // Orion-Start
    private static void DrawConnection(DrawingHandleScreen handle, FancyResearchConsoleItem item, FancyResearchConsoleItem prerequisite)
    {
        var delta = item.Prototype.Position - prerequisite.Prototype.Position;
        var distanceInCells = Math.Abs(delta.X) + Math.Abs(delta.Y);
        if (distanceInCells > MaxLineDistanceThreshold)
            return;

        var isDashed = distanceInCells > DashedLineDistanceThreshold;

        var itemCenter = GetCenter(item);
        var prerequisiteCenter = GetCenter(prerequisite);

        if (delta.Y == 0)
        {
            var start = GetHorizontalEdge(itemCenter, item.PixelWidth, rightSide: delta.X < 0);
            var end = GetHorizontalEdge(prerequisiteCenter, prerequisite.PixelWidth, rightSide: delta.X >= 0);
            DrawConnectionLine(handle, start, end, isDashed);
            return;
        }

        var startEdge = GetHorizontalEdge(itemCenter, item.PixelWidth, rightSide: delta.X < 0);
        var endEdge = GetHorizontalEdge(prerequisiteCenter, prerequisite.PixelWidth, rightSide: delta.X >= 0);
        var corridorX = (startEdge.X + endEdge.X) / 2f;

        DrawConnectionLine(handle, startEdge, new Vector2(corridorX, startEdge.Y), isDashed);
        DrawConnectionLine(handle, new Vector2(corridorX, startEdge.Y), new Vector2(corridorX, endEdge.Y), isDashed);
        DrawConnectionLine(handle, new Vector2(corridorX, endEdge.Y), endEdge, isDashed);
    }

    private static Vector2 GetCenter(FancyResearchConsoleItem item)
    {
        return new Vector2(item.PixelPosition.X + item.PixelWidth / 2f, item.PixelPosition.Y + item.PixelHeight / 2f);
    }

    private static Vector2 GetHorizontalEdge(Vector2 center, float width, bool rightSide)
    {
        var offset = width / 2f;
        return center + new Vector2(rightSide ? offset : -offset, 0f);
    }

    private static void DrawConnectionLine(DrawingHandleScreen handle, Vector2 start, Vector2 end, bool dashed)
    {
        if (!dashed)
        {
            handle.DrawLine(start, end, Color.White);
            return;
        }

        var direction = end - start;
        var length = direction.Length();
        if (length <= 0)
            return;

        var normalizedDirection = direction / length;
        for (var offset = 0f; offset < length; offset += DashLength + DashGap)
        {
            var dashEnd = Math.Min(offset + DashLength, length);
            handle.DrawLine(start + normalizedDirection * offset, start + normalizedDirection * dashEnd, Color.White);
        }
    }
    // Orion-End
}
