# SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
# SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

reagent-effect-guidebook-deal-stamina-damage =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Сделки
               *[-1] Исцеляет
            }
       *[other]
            { $deltasign ->
                [1] сделка
               *[-1] исцеление
            }
    } { $amount } { $immediate ->
        [true] немедленно
       *[false] сверхурочные
    } урон выносливости
