using Content.Shared._Orion.Research.Components;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._Orion.Research.UI;

public static class ResearchExperimentUiHelpers
{
    public static string FormatServerName(string name)
    {
        const string prefix = "RND-Server";
        if (!name.StartsWith(prefix, StringComparison.Ordinal))
            return name;

        var suffix = name[prefix.Length..].Trim();
        return string.IsNullOrWhiteSpace(suffix)
            ? Loc.GetString("research-server-name-base")
            : Loc.GetString("research-server-name-with-suffix", ("suffix", suffix));
    }

    public static Control BuildExperimentEntry(ResearchMachineExperimentUiData experiment)
    {
        var headerLabel = new RichTextLabel();
        var headerMessage = new FormattedMessage();
        headerMessage.AddMarkupOrThrow($"[color=lightblue]{FormattedMessage.EscapeText(experiment.Name)}[/color]");
        headerLabel.SetMessage(headerMessage);

        var progressText = Loc.GetString("research-machine-experiment-progress",
            ("objective", experiment.Objective),
            ("progress", experiment.Progress),
            ("target", experiment.Target));

        var descriptionLabel = new RichTextLabel
        {
            HorizontalExpand = true,
        };
        var descriptionMessage = new FormattedMessage();
        descriptionMessage.AddText(experiment.Description);
        descriptionLabel.SetMessage(descriptionMessage);

        var goalLabel = CreateExperimentGoalLabel(experiment.Goal);

        var progressLabel = new RichTextLabel
        {
            HorizontalExpand = true,
        };
        var progressMessage = new FormattedMessage();
        progressMessage.AddMarkupOrThrow($"[color=lightgray]{FormattedMessage.EscapeText(progressText)}[/color]");
        progressLabel.SetMessage(progressMessage);

        var content = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 2,
            Margin = new Thickness(6),
            RectClipContent = true,
            Children =
            {
                headerLabel,
                descriptionLabel,
                goalLabel,
                progressLabel,
            },
        };

        return new PanelContainer
        {
            HorizontalExpand = true,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#23232A"),
                BorderColor = Color.FromHex("#4A4A56"),
                BorderThickness = new Thickness(1),
            },
            Children = { content },
        };
    }

    public static FormattedMessage CreateLastOperationMessage(string? lastResult)
    {
        var result = string.IsNullOrWhiteSpace(lastResult)
            ? Loc.GetString("research-machine-common-none")
            : lastResult;

        var lastOperation = new FormattedMessage();
        lastOperation.AddText(Loc.GetString("research-machine-common-labeled-value",
            ("label", Loc.GetString("research-machine-common-last-result")),
            ("value", result)));
        return lastOperation;
    }

    public static RichTextLabel CreateExperimentGoalLabel(string goal)
    {
        var goalLabel = new RichTextLabel
        {
            HorizontalExpand = true,
        };

        var goalMessage = new FormattedMessage();
        goalMessage.AddMarkupOrThrow($"[color=#D0D0D0]{FormattedMessage.EscapeText(Loc.GetString("research-machine-experiment-goal", ("goal", goal)))}[/color]");
        goalLabel.SetMessage(goalMessage);

        return goalLabel;
    }
}
