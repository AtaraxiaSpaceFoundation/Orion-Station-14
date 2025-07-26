# SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
# SPDX-FileCopyrightText: 2025 SX-7 <92227810+SX-7@users.noreply.github.com>
# SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

server-currency-name-singular = Евродоллар
server-currency-name-plural = Евродоллары

## Commands

server-currency-gift-command = подарок
server-currency-gift-command-description = Дарит часть своего баланса другому игроку.
server-currency-gift-command-help = Использование: gift <player> <value>
server-currency-gift-command-error-1 = Ты не можешь подарить себе сам!
server-currency-gift-command-error-2 = Вы не можете позволить себе такой подарок! У вас на балансе {$balance}.
server-currency-gift-command-giver = Вы дарите {$player} {$amount}§.
server-currency-gift-command-reciever = {$player} подарил вам {$amount}§.

server-currency-balance-command = баланс
server-currency-balance-command-description = Возвращает ваш баланс.
server-currency-balance-command-help = Использование: balance
server-currency-balance-command-return = У вас {$balance}§.

server-currency-add-command = balance:add
server-currency-add-command-description = Adds currency to a player's balance.
server-currency-add-command-help = Usage: balance:add <player> <value>

server-currency-remove-command = balance:rem
server-currency-remove-command-description = Removes currency from a player's balance.
server-currency-remove-command-help = Usage: balance:rem <player> <value>

server-currency-set-command = balance:set
server-currency-set-command-description = Sets a player's balance.
server-currency-set-command-help = Usage: balance:set <player> <value>

server-currency-get-command = balance:get
server-currency-get-command-description = Gets the balance of a player.
server-currency-get-command-help = Usage: balance:get <player>

server-currency-command-completion-1 = Сикей
server-currency-command-completion-2 = Сумма
server-currency-command-error-1 = Игрок не найден.
server-currency-command-error-2 = Value must be an integer.
server-currency-command-return = {$player} имеет {$balance}§.

# 65% Update

gs-balanceui-title = Магазин
gs-balanceui-confirm = Подтвердить

gs-balanceui-gift-label = Перевести:
gs-balanceui-gift-player = Игрок
gs-balanceui-gift-player-tooltip = Введите имя игрока, которому вы хотите отправить деньги
gs-balanceui-gift-value = Сумма
gs-balanceui-gift-value-tooltip = Сумма для перевода

gs-balanceui-shop-label = Магазин
gs-balanceui-shop-empty = Нет в наличии!
gs-balanceui-shop-buy = Купить
gs-balanceui-shop-footer = ⚠ Ахелп что-бы использовать ваш токен. Используйте только 1 раз в день.

gs-balanceui-shop-token-label = Токены
gs-balanceui-shop-tittle-label = Названия

gs-balanceui-shop-buy-token-antag = Купите токен АНТАГОНИСТА - {$price}§
gs-balanceui-shop-buy-token-admin-abuse = Купите токен АдминАрбуза- {$price}§
gs-balanceui-shop-buy-token-hat = Купите токен на Шапку - {$price}§

gs-balanceui-shop-token-antag = Токен Антагониста высокого уровня
gs-balanceui-shop-token-admin-abuse = Токен АдминАрбуза
gs-balanceui-shop-token-hat = Токен шапки

gs-balanceui-shop-buy-token-antag-desc = Позволяет вам стать любым антагонистом. (За исключением волшебника)
gs-balanceui-shop-buy-token-admin-abuse-desc = Позволяет вам попросить администратора злоупотребить своими полномочиями по отношению к вам. Администраторам рекомендуется выходить из себя.
gs-balanceui-shop-buy-token-hat-desc = Администратор выдаст вам случайную шляпу.

gs-balanceui-admin-add-label = Прибавьте (или убавьте) деньги:
gs-balanceui-admin-add-player = Имя игрока
gs-balanceui-admin-add-value = Сумма

gs-balanceui-remark-token-antag = Купил токен антагониста.
gs-balanceui-remark-token-admin-abuse = Купил токен админарбуза.
gs-balanceui-remark-token-hat = Купил токен шапки.
gs-balanceui-shop-click-confirm = Нажмите еще раз для подтверждения
gs-balanceui-shop-purchased = Куплен {$item}
