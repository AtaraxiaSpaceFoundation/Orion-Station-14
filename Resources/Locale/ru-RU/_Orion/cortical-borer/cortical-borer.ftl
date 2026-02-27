
## Infest Messages
cortical-borer-has-host = Уже есть носитель.
cortical-borer-host-already-infested = { $target } уже заражён.
cortical-borer-invalid-host = { $target } не является подходящим носителем.
cortical-borer-face-covered = Лицо { $target } закрыто!
cortical-borer-headless = { $target } не имеет головы!
cortical-borer-start-infest = Вы начинаете заползать в { $target }.

## Generic messages
cortical-borer-no-host = Нет носителя.
cortical-borer-dead-host = Ваш носитель мертв.
cortical-borer-not-enough-chem = Недостаточно химикатов.
cortical-borer-not-enough-chem-storage = Недостаточно химикатов.
cortical-borer-sugar-block = Вы чувствуете что-то сладкое... ФУ!

## Control messages
cortical-borer-already-control = Вы уже управляете своим носителем.
cortical-borer-vomit = { $name } выблёвывает { $egg }!

## UI
cortical-borer-dispenser-window-cost = { $cost } химикатов
cortical-borer-ghostrole-name = Кортикальный Паразит
cortical-borer-ghostrole-desc = Космический червь с единственной целью в жизни - проникать в головы людей и откладывать свои яйца.

cortical-borer-force-speak-window-title = Принудительная речь
cortical-borer-force-speak-label = Принудительная речь
cortical-borer-force-speak-placeholder = Введите текст, который скажет носитель
cortical-borer-force-speak-button = Заставить говорить

cortical-borer-willing-title = Добровольное подчинение
cortical-borer-willing-question = Вы хотите быть добровольным носителем?
cortical-borer-willing-yes = Да
cortical-borer-willing-no = Нет
cortical-borer-willing-result-yes = { $host } соглашается стать добровольным носителем.
cortical-borer-willing-result-no = { $host } отказывается от добровольного подчинения!

cortical-borer-round-end-willing = { $borer } получил { $count ->
        [one] добровольного носителя
        [few] добровольных носителя
       *[other] добровольных носителей
    }: { $hosts }.
cortical-borer-round-end-objective-survive = Выжить
cortical-borer-round-end-objective-willing = Завести добровольных носителей: { $current }/{ $target }
cortical-borer-round-end-objective-eggs = Отложенные яйца: { $current }/{ $target }

## Examine Text
cortical-borer-infested-examine = [color=#d842fc]Движения выглядят крайне неестественно...[/color]

infested-control-examined = Осталось [color=#d842fc]{ $timeremaining }[/color] секунд управления этим телом.
cortical-borer-self-examine = Доступно [color=#d842fc]{ $chempoints }[/color] химикатов.

cortical-borer-round-end-agent-name = кортикальный паразит
roles-antag-cortical-borer-name = Кортикальный паразит
roles-antag-cortical-borer-objective = Захватывайте носителей, размножайтесь и выживите.
cortical-borer-round-end-objectives = Цели { $borer }: выживание [{ $survive }], добровольные носители ({ $willingCount }/3) [{ $willingResult }], отложенные яйца ({ $eggs }/5) [{ $eggsResult }].
