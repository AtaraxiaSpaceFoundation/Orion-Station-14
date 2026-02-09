# Popups
recruitment-start-user-temporal-popup = Передайте { $target } устройство для оформления.

recruitment-start-user = Вы начинаете вводить данные об { $target } в устройство.
recruitment-start-target = { $user } приглашает вас в организацию.

recruitment-success = { $target } теперь является частью организации!
recruitment-decline = { $target } отказывается от вступления!
recruitment-already = { $target } уже находится в базе данных!
recruitment-failed = { $target } не может быть в организации!
recruitment-too-far = Цель слишком далеко!

# UI strings
recruitment-ui-title = Приглашение в организацию
recruitment-ui-invitation = Вас приглашают вступить в организацию!
recruitment-ui-organization = Организация: 
recruitment-ui-implant = Имплантация: 
recruitment-ui-warning = ❗ ВНИМАНИЕ ❗
recruitment-ui-warning-text = Вступая в { $organization }, вам будет установлен { $implant }. Это действие необратимо!
recruitment-ui-accept = Подписать
recruitment-ui-decline = Отказаться

recruitment-list-ui-title = База данных организации
recruitment-member-list-organization = Манифест организации { $organization }
recruitment-member-list-count = Общее количество: { $count }
recruitment-member-list-empty = Участники отсутствуют!

# Table headers
recruitment-member-list-header-name = Имя
recruitment-member-list-header-recruiter = Вербовщик
recruitment-member-list-header-time = Стаж
recruitment-member-list-unknown = Неизвестный

# Time formatting
recruitment-member-list-time = { $minutes } { $minutes ->
        [1] минуту
        [few] минуты
       *[other] минут
    } и { $seconds } { $seconds ->
        [1] секунду
        [few] секунды
       *[other] секунд
    }
