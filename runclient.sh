# SPDX-FileCopyrightText: 2024 flashgnash <12749214+flashgnash@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

#!/bin/sh
dotnet run --project Content.Goobstation.Client
dotnet run --project Content.Europa.AGPL.Client
dotnet run --project Content.Europa.MIT.Client
read -p "Press enter to continue"
