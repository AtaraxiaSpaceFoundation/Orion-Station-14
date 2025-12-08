// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Palladinium <patrick.chieppe@hotmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server.Administration.Managers;

/// <summary>
///     This manager sends a webhook notification whenever a player with an active
///     watchlist joins the server.
/// </summary>
public interface IWatchlistWebhookManager
{
    void Initialize();
    void Update();
}