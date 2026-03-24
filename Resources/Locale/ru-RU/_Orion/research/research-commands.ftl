unlockallresearches-command-description = Разблокирует все исследования на указанном сервере исследований.
unlockallresearches-command-help = Использование: { $command } <serverUid>
unlockallresearches-command-hint-server = uid сервера исследований
unlockallresearches-command-success = Разблокировано технологий: { $count } на сервере { $serverUid } ({ $serverName }).

addresearchpoints-command-description = Добавляет очки указанного типа на указанный сервер исследований.
addresearchpoints-command-help = Использование: { $command } <serverUid> <pointType> <amount>
addresearchpoints-command-hint-server = uid сервера исследований
addresearchpoints-command-hint-type = тип очков исследований
addresearchpoints-command-hint-amount = изменение очков (может быть отрицательным)
addresearchpoints-command-success = На сервере { $serverUid } ({ $serverName }) теперь { $balance } очков типа '{ $type }'.

command-error-invalid-server-uid = Некорректный UID сервера.
command-error-not-research-server = Сущность не является сервером исследований.
command-error-missing-tech-database = У сервера исследований отсутствует компонент базы технологий.
command-error-empty-point-type = Тип очков не может быть пустым.
command-error-invalid-amount = Некорректное количество очков.
