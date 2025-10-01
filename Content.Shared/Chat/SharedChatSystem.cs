// SPDX-FileCopyrightText: 2022 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Alex Evgrashin <aevgrashin@yandex.ru>
// SPDX-FileCopyrightText: 2023 HerCoyote23 <131214189+HerCoyote23@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Interrobang01 <113810873+Interrobang01@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Kara <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Visne <39844191+Visne@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Vordenburg <114301317+Vordenburg@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 gus <august.eymann@gmail.ccom>
// SPDX-FileCopyrightText: 2023 gus <august.eymann@gmail.com>
// SPDX-FileCopyrightText: 2023 router <messagebus@vk.com>
// SPDX-FileCopyrightText: 2024 Kot <1192090+koteq@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2024 SlamBamActionman <83650252+SlamBamActionman@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Thomas <87614336+Aeshus@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Vasilis <vasilis@pikachu.systems>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 CerberusWolfie <wb.johnb.willis@gmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Ilya246 <57039557+Ilya246@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 John Willis <143434770+CerberusWolfie@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 Rinary <72972221+Rinary1@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Timfa <timfalken@hotmail.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Frozen;
using System.Text.RegularExpressions;
using Content.Shared._Starlight.CollectiveMind;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Content.Shared.Speech;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using static System.Text.RegularExpressions.RegexOptions;

// Goobstation - Starlight collective mind port
// Einstein Engines - Language

namespace Content.Shared.Chat;

public abstract class SharedChatSystem : EntitySystem
{
    public const char RadioCommonPrefix = ';';
    public const char RadioChannelPrefix = ':';
    private const char RadioChannelAltPrefix = '.';
    public const char LocalPrefix = '>';
    public const char ConsolePrefix = '/';
    public const char DeadPrefix = '\\';
    public const char LoocPrefix = '(';
    public const char OOCPrefix = '[';
    public const char EmotesPrefix = '@';
    public const char EmotesAltPrefix = '*';
    public const char AdminPrefix = ']';
    public const char WhisperPrefix = ',';
    public const char TelepathicPrefix = '=';
    public const char CollectiveMindPrefix = '+'; // Goobstation - Starlight collective mind port
    public const char DefaultChannelKey = 'h';

    public const int VoiceRange = 10;
    public const int WhisperClearRange = 2;
    public const int WhisperMuffledRange = 5;

    public static readonly ProtoId<RadioChannelPrototype> CommonChannel = "Common";
    public static readonly string DefaultChannelPrefix = $"{RadioChannelPrefix}{DefaultChannelKey}";
    public static readonly ProtoId<SpeechVerbPrototype> DefaultSpeechVerb = "Default";

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private FrozenDictionary<char, RadioChannelPrototype> _keyCodes = default!;
    private FrozenDictionary<char, CollectiveMindPrototype> _mindKeyCodes = default!; // Goobstation - Starlight collective mind port


    public override void Initialize()
    {
        base.Initialize();
        DebugTools.Assert(_prototypeManager.HasIndex(CommonChannel));
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypeReload);
        CacheRadios();
        CacheCollectiveMinds(); // Goobstation - Starlight collective mind port
    }

    protected virtual void OnPrototypeReload(PrototypesReloadedEventArgs obj)
    {
        if (obj.WasModified<RadioChannelPrototype>())
            CacheRadios();

        if (obj.WasModified<CollectiveMindPrototype>()) // Goobstation - Starlight collective mind port
            CacheCollectiveMinds();
    }

    private void CacheRadios()
    {
        _keyCodes = _prototypeManager.EnumeratePrototypes<RadioChannelPrototype>()
            .ToFrozenDictionary(x => x.KeyCode);
    }

    private void CacheCollectiveMinds() // Goobstation - Starlight collective mind port
    {
        _prototypeManager.PrototypesReloaded -= OnPrototypeReload;
        _mindKeyCodes = _prototypeManager.EnumeratePrototypes<CollectiveMindPrototype>()
            .ToFrozenDictionary(x => x.KeyCode);
    }

    public SpeechVerbPrototype GetSpeechVerb(EntityUid source, string message, SpeechComponent? speech = null)
    {
        if (!Resolve(source, ref speech, false))
            return _prototypeManager.Index(DefaultSpeechVerb);

        SpeechVerbPrototype? current = null;
        foreach (var (str, id) in speech.SuffixSpeechVerbs)
        {
            var proto = _prototypeManager.Index(id);
            if (message.EndsWith(Loc.GetString(str)) && proto.Priority >= (current?.Priority ?? 0))
            {
                current = proto;
            }
        }

        return current ?? _prototypeManager.Index(speech.SpeechVerb);
    }

    protected void GetRadioKeycodePrefix(EntityUid source, string input, out string output, out string prefix)
    {
        prefix = string.Empty;
        output = input;

        if (input.Length <= 2)
            return;

        if (!(input.StartsWith(RadioChannelPrefix) || input.StartsWith(RadioChannelAltPrefix)))
            return;

        if (!_keyCodes.TryGetValue(char.ToLower(input[1]), out _))
            return;

        prefix = input[..2];
        output = input[2..];
    }

    public bool TryProccessRadioMessage(
        EntityUid source,
        string input,
        out string output,
        out RadioChannelPrototype? channel,
        bool quiet = false)
    {
        output = input.Trim();
        channel = null;

        if (input.Length == 0)
            return false;

        if (input.StartsWith(RadioCommonPrefix))
        {
            output = SanitizeMessageCapital(input.Substring(1).TrimStart());
            channel = _prototypeManager.Index(CommonChannel);
            return true;
        }

        if (!(input.StartsWith(RadioChannelPrefix) || input.StartsWith(RadioChannelAltPrefix)))
            return false;

        if (input.Length < 2 || char.IsWhiteSpace(input[1]))
        {
            output = SanitizeMessageCapital(input.Substring(1).TrimStart());
            if (!quiet)
                _popup.PopupEntity(Loc.GetString("chat-manager-no-radio-key"), source, source);
            return true;
        }

        var channelKey = char.ToLower(input[1]);
        output = SanitizeMessageCapital(input.Substring(2).TrimStart());

        if (channelKey == DefaultChannelKey)
        {
            var ev = new GetDefaultRadioChannelEvent();
            RaiseLocalEvent(source, ev);

            if (ev.Channel != null)
                _prototypeManager.TryIndex(ev.Channel, out channel);
            return true;
        }

        if (!_keyCodes.TryGetValue(channelKey, out channel) && !quiet)
        {
            var msg = Loc.GetString("chat-manager-no-such-channel", ("key", channelKey));
            _popup.PopupEntity(msg, source, source);
        }

        return true;
    }

    public bool TryProccessCollectiveMindMessage( // Goobstation - Starlight collective mind port
        EntityUid source,
        string input,
        out string output,
        out CollectiveMindPrototype? channel,
        bool quiet = false)
    {
        output = input.Trim();
        channel = null;

        if (input.Length == 0)
            return false;

        if (!input.StartsWith(CollectiveMindPrefix))
            return false;

        ProtoId<CollectiveMindPrototype>? defaultChannel = null;
        if (TryComp<CollectiveMindComponent>(source, out var mind))
            defaultChannel = mind.DefaultChannel;

        if (input.Length < 2 || (char.IsWhiteSpace(input[1]) && defaultChannel == null))
        {
            output = SanitizeMessageCapital(input.Substring(1).TrimStart());
            if (!quiet)
                _popup.PopupEntity(Loc.GetString("chat-manager-no-radio-key"), source, source);
            return true;
        }

        var channelKey = char.ToLower(input[1]);

        if (_mindKeyCodes.TryGetValue(channelKey, out channel))
        {
            output = SanitizeMessageCapital(input.Substring(2).TrimStart());
            return true;
        }

        if (defaultChannel != null)
        {
            output = SanitizeMessageCapital(input.Substring(1).TrimStart());
            channel = _prototypeManager.Index(defaultChannel.Value);
            return true;
        }

        if (!quiet)
        {
            var msg = Loc.GetString("chat-manager-no-such-channel", ("key", channelKey));
            _popup.PopupEntity(msg, source, source);
        }

        return false;
    }

    public virtual void TrySendInGameICMessage(
        EntityUid source,
        string message,
        InGameICChatType desiredType,
        bool hideChat,
        bool hideLog = false,
        IConsoleShell? shell = null,
        ICommonSession? player = null,
        string? nameOverride = null,
        bool checkRadioPrefix = true,
        bool ignoreActionBlocker = false,
        string wrappedMessagePostfix = "" // Goobstation
    )
    {
    }

    protected string SanitizeMessageCapital(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        return string.Concat(char.ToUpper(message[0]).ToString(), message.Substring(1));
    }

    protected string SanitizeMessageCapitalizeTheWordI(string message, string theWordI = "i")
    {
        if (string.IsNullOrEmpty(message))
            return message;

        for (var index = message.IndexOf(theWordI, StringComparison.Ordinal); index != -1; index = message.IndexOf(theWordI, index + 1, StringComparison.Ordinal))
        {
            if (index + 1 < message.Length && char.IsLetter(message[index + 1]))
                continue;
            if (index - 1 >= 0 && char.IsLetter(message[index - 1]))
                continue;

            var beforeTarget = message[..index];
            var target = message.Substring(index, theWordI.Length);
            var afterTarget = message[(index + theWordI.Length)..];

            message = beforeTarget + target.ToUpper() + afterTarget;
        }

        return message;
    }

    public static string SanitizeAnnouncement(string message, int maxLength = 0, int maxNewlines = 2)
    {
        var trimmed = message.Trim();
        if (maxLength > 0 && trimmed.Length > maxLength)
        {
            trimmed = $"{message.Substring(0, maxLength)}...";
        }

        if (maxNewlines > 0)
        {
            var chars = trimmed.ToCharArray();
            var newlines = 0;
            for (var i = 0; i < chars.Length; i++)
            {
                if (chars[i] != '\n')
                    continue;

                if (newlines >= maxNewlines)
                    chars[i] = ' ';

                newlines++;
            }

            return new string(chars);
        }

        return trimmed;
    }

    public static string InjectTagInsideTag(ChatMessage message, string outerTag, string innerTag, string? tagParameter)
    {
        var rawmsg = message.WrappedMessage;
        var tagStart = rawmsg.IndexOf($"[{outerTag}]", StringComparison.Ordinal);
        var tagEnd = rawmsg.IndexOf($"[/{outerTag}]", StringComparison.Ordinal);

        if (tagStart < 0 || tagEnd < 0)
            return rawmsg;

        tagStart += outerTag.Length + 2;
        var innerTagProcessed = tagParameter != null ? $"[{innerTag}={tagParameter}]" : $"[{innerTag}]";

        rawmsg = rawmsg.Insert(tagEnd, $"[/{innerTag}]");
        rawmsg = rawmsg.Insert(tagStart, innerTagProcessed);

        return rawmsg;
    }

    public static string InjectTagAroundString(ChatMessage message, string targetString, string tag, string? tagParameter)
    {
        var rawmsg = message.WrappedMessage;

        var escapedTarget = Regex.Escape(targetString);
        var pattern = $"(?i)({escapedTarget})(?-i)(?![^[]*\\])";
        var regex = new Regex(pattern, Compiled);

        return regex.Replace(rawmsg, $"[{tag}={tagParameter}]$1[/{tag}]");
    }

    public static string GetStringInsideTag(ChatMessage message, string tag)
    {
        var rawmsg = message.WrappedMessage;
        var tagStart = rawmsg.IndexOf($"[{tag}]", StringComparison.Ordinal);
        var tagEnd = rawmsg.IndexOf($"[/{tag}]", StringComparison.Ordinal);

        if (tagStart < 0 || tagEnd < 0)
            return "";

        tagStart += tag.Length + 2;
        return rawmsg.Substring(tagStart, tagEnd - tagStart);
    }

    public static string GetStringInsideTag(string message, string tag)
    {
        var tagStart = message.IndexOf($"[{tag}]", StringComparison.Ordinal);
        var tagEnd = message.IndexOf($"[/{tag}]", StringComparison.Ordinal);

        if (tagStart < 0 || tagEnd < 0)
            return "";

        tagStart += tag.Length + 2;
        return message.Substring(tagStart, tagEnd - tagStart);
    }
}

// Einstein Engines - Language begin
[Serializable, NetSerializable]
public enum InGameICChatType : byte
{
    Speak,
    Emote,
    Whisper,
    Telepathic, // Goobstation Change
    CollectiveMind // Goobstation - Starlight collective mind port
}

[Serializable, NetSerializable]
public enum InGameOOCChatType : byte
{
    Looc,
    Dead
}

[Serializable, NetSerializable]
public enum ChatTransmitRange : byte
{
    Normal,
    GhostRangeLimit,
    HideChat,
    NoGhosts
}
// Einstein Engines - Language end
