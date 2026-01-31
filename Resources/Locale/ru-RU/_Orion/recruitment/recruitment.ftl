recruitment-start-user = Вы начинаете вводить данные об { $target } в устройство.
recruitment-start-target = { $user } записывает вас в организацию.

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

recruitment-list-ui-title = Рекруты организации
recruitment-member-label = Участники организации
recruitment-member-list-count = Количество: { $count }
recruitment-member-list-empty = Участники отсутствуют!
recruitment-member-list-footer = Выйти из организации можно только посмертно! Удачного дня.
recruitment-member-list-organization = { $organization } - Участники

# Table headers
recruitment-member-list-header-name = Имя
recruitment-member-list-header-recruiter = Завербован
recruitment-member-list-header-time = Время

# Time formatting
recruitment-member-list-time = { $minutes } { $minutes ->
        [1] минуту
        [few] минуты
       *[other] минут
    } { $seconds } { $seconds ->
        [1] секунду
        [few] секунды
       *[other] секунд
    } назад.
