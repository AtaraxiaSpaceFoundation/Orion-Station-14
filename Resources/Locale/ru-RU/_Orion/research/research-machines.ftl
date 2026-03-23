research-machine-common-server-list = Список серверов
research-machine-common-server-none = Сервер не подключён
research-machine-common-server-current = Активный сервер: { $name }
research-machine-common-balances-title = Баланс очков
research-machine-common-balances-empty = Нет доступных очков.
research-machine-common-last-operation-title = Последняя операция
research-machine-common-last-subject = Предмет
research-machine-common-last-result = Результат
research-machine-common-none = нет данных
research-machine-common-labeled-value = { $label }: { $value }
research-machine-common-no-server = Операция не выполнена: нет подключения к серверу РнД.

research-machine-destructive-window-title = Деструктивный анализатор
research-machine-destructive-requirements-title = Требования
research-machine-destructive-requirements-line-1 = • Принимает только исследуемые образцы.
research-machine-destructive-requirements-line-2 = • Образец должен давать исследовательскую награду.
research-machine-destructive-last-result-success = Образец обработан успешно: { $result }.
research-machine-destructive-last-result-invalid-item = Предмет не подходит для деструктивного анализа.

research-machine-experimental-destructive-scanner-window-title = Экспериментальный деструктивный сканер
research-machine-experimental-destructive-scanner-requirements-title = Операции и требования
research-machine-experimental-destructive-scanner-operations-empty = Нет доступных операций.
research-machine-experimental-destructive-scanner-last-result = Операция завершена: { $result }.
research-machine-experimental-destructive-scanner-last-result-no-operation = Для этого предмета нет подходящей операции.

research-machine-experimental-destructive-scanner-perform = Выполнить эксперимент
research-machine-experimental-destructive-scanner-item-loaded = Образец загружен.
research-machine-experimental-destructive-scanner-no-item = Образец не вставлен.
research-machine-experimental-destructive-scanner-already-scanned = Этот образец уже сканировался.
research-machine-experimental-destructive-scanner-no-matching-experiment = Нет активного эксперимента для этого образца.
research-machine-experimental-destructive-scanner-progressed = Прогресс задачи обновлён.
research-machine-experimental-destructive-scanner-completed = { $count ->
    [one] Завершён { $count } эксперимент.
    [few] Завершено { $count } эксперимента.
   *[many] Завершено { $count } экспериментов.
}
research-machine-experimental-destructive-scanner-completed-named = { $count ->
    [one] Завершён { $count } эксперимент: { $experiments }.
    [few] Завершено { $count } эксперимента: { $experiments }.
   *[many] Завершено { $count } экспериментов: { $experiments }.
}
research-machine-destructive-run = Запустить анализ
research-machine-eject-button = Извлечь предмет
research-machine-destructive-item-loaded = Образец загружен.
research-machine-destructive-no-item = Образец не вставлен.
research-machine-destructive-already-analyzed = Загруженный образец уже проанализирован.
research-machine-destructive-unsupported-method = Выбран неподдерживаемый метод анализа.
research-experiment-objective-servertrigger = Триггер сервера
research-experiment-objective-scanentity = Сканировать сущность
research-experiment-objective-presentitem = Сканировать предмет
research-experiment-objective-scandifferententities = Сканировать разные сущности
research-experiment-objective-scansamples = Сканировать образцы
research-experiment-objective-actioncount = Количество действий
research-experiment-objective-debugmanual = Вручную

research-machine-experimental-destructive-scanner-no-items = На тайле сканера не обнаружено предметов.
research-machine-experimental-destructive-scanner-busy = Сканер уже обрабатывает образцы.
research-machine-experimental-destructive-scanner-processing = { $count ->
    [one] Сканер закрылся и начал обработку { $count } образца.
    [few] Сканер закрылся и начал обработку { $count } образца.
   *[many] Сканер закрылся и начал обработку { $count } образцов.
}
research-machine-experimental-destructive-scanner-state-processing = обработка...
research-machine-experimental-destructive-scanner-status = Статус

research-machine-destructive-method-deconstruct = Деконструкция
research-machine-destructive-method-illegaltechnology = Нелегальная технология
research-machine-destructive-method-alientechnology = Инопланетная технология
research-machine-destructive-method-advancedanalysis = Продвинутый анализ

research-machine-destructive-living-subject-blocked = Живых существ нельзя деструктивно анализировать.

research-machine-experiment-progress = { $objective }: { $progress }/{ $target }

research-machine-destructive-method-reveal-technology = Раскрыть технологию: { $technology }
research-machine-destructive-method-unknown = Неизвестный метод

research-machine-destructive-chat-result = Результат деструктивного анализа: { $result }
research-machine-destructive-result-generic = награда применена к сети исследований
research-machine-destructive-result-points = начислены очки: { $points }
research-machine-destructive-result-points-entry = { $type} { $amount }
research-machine-destructive-result-revealed-tech = раскрыта технология: { $technology }
research-machine-destructive-result-unlocked-tech = открыта технология: { $technology }

research-machine-experimental-destructive-scanner-chat-result = Результат экспериментального деструктивного сканера: { $result }

research-machine-destructive-busy = Анализатор уже обрабатывает образец.
research-machine-destructive-processing = Анализатор начал обработку образца.

research-destructive-analyzer-success = Образец успешно деструктивно проанализирован.
research-experimental-destructive-scanner-success = Экспериментальный деструктивный сканер успешно завершил операцию.
research-experimental-destructive-scanner-failure = Операция экспериментального деструктивного сканера завершилась неудачей.

research-experi-scanner-progress = Сканирование { $target } успешно, прогресс обновлён.
research-experi-scanner-completed = Сканирование { $target } успешно. Завершено экспериментов: { $count }.
research-experi-scanner-no-server = Сканирование не выполнено: нет подключения к серверу РнД.
research-experi-scanner-no-compatible-experiments = Нет активных экспериментов, совместимых с ручным сканером.
research-experi-scanner-no-match = Для этой цели нет подходящего активного ручного эксперимента.
research-experi-scanner-already-scanned = Эта цель уже была засчитана для активного ручного эксперимента.
research-netlog-experi-scanner-scan = { $user } отсканировал { $target } при помощи { $scanner } (прогресс: { $progressed }, завершено: { $completed }).

research-experi-scanner-window-title = Экспери-сканер
research-experi-scanner-available-experiments = Доступные эксперименты
research-experi-scanner-no-experiments = Нет доступных экспериментов, совместимых с ручным сканером.
research-experi-scanner-completed-named = Сканирование успешно. Завершены эксперименты: { $experiments }.
