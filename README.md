<h1 align="center"> <img alt="Orion Station" width="420" height="120" src="https://raw.githubusercontent.com/AtaraxiaSpaceFoundation/asset-dump/refs/heads/master/OrionStation/Orion-Banner-Big.png" /> </h1>

<p align="center">
  Ваш проводник в космический симулятор безумия!<br>
  Основан на идеях <a href="https://github.com/tgstation/tgstation">/tg/station</a> и <a href="https://github.com/shiptest-ss13/Shiptest">Shiptest</a> из Space Station 13.
</p>

<div align="center">

  [![Steam](https://img.shields.io/badge/Steam-Скачать-blue?style=for-the-badge)](https://store.steampowered.com/app/1255460/Space_Station_14/)
  [![Client](https://img.shields.io/badge/Клиент-Скачать-purple?style=for-the-badge)](https://spacestation14.io/about/nightlies/)

</div>

---

**Orion** — это русскоязычный форк [Goob Station](https://github.com/Goob-Station/Goob-Station), который стремится возродить дух классического геймплея Space Station 13, черпая вдохновение из таких проектов, как [/tg/station](https://github.com/tgstation/tgstation) и [Shiptest](https://github.com/shiptest-ss13/Shiptest). Мы фокусируемся на сочетании проверенных временем механик с инновационными идеями, создавая уникальный и приятный опыт игры в космическое безумие.

---

<div align="center">
    
## Ссылки

</div>

[<img src="https://github.com/AtaraxiaSpaceFoundation/asset-dump/blob/master/Misc/Discord/discord-banner.png" alt="Discord" width="150" align="left">](https://discord.gg/K48JujjjsC)
**[Discord Server](https://discord.gg/K48JujjjsC)**<br>В космосе вас никто не услышит.

[<img src="https://i.imgur.com/XiS9QP5.png" alt="ASF" width="150" align="left">](https://github.com/AtaraxiaSpaceFoundation)
**[Ataraxia Space Foundation](https://github.com/AtaraxiaSpaceFoundation)**<br>Специализируемся на разработке этого билда.

---
<div align="center">

## Активность репозитория

![Активность PR](https://repobeats.axiom.co/api/embed/fb55493406a54b21282bac43ecb28472b47a762c.svg "ZZZ")

</div>

---
<div align="center">

## Политика Orion

Любой сервер, заявляющий, что он является официальным представителем этого билда — не одобрен этой организацией.
Однако мы хотели бы пригласить всех желающих создать сервер на базе билда Orion.

</div>

> [!WARNING]  
> **Orion не имеет официальных игровых серверов**.

---

<div align="center">

## Документация

</div>

Проект имеет обширную [документацию](https://docs.goobstation.com/), которая охватывает все аспекты: от контента и сборки до движка, дизайна игры и многого другого. Это также отличный ресурс для новичков, желающих внести свой вклад в разработку.

---
<div align="center">

## Контрибуция

</div>

Мы всегда рады помощи в разработке, если вы хотите внести свой вклад, присоединяйтесь к [серверу разработки в Discord](https://discord.gg/zXk2cyhzPN). Вы можете помочь нам, решая проблемы из [списка открытых проблем](https://github.com/Goob-Station/Goob-Station/issues) или предлагая свои идеи. Не стесняйтесь задавать вопросы — мы всегда готовы помочь!

---
<div align="center">

## Сборка

</div>

</div>

> [!TIP]
> Используйте [IDE Rider](https://github.com/designinlife/jetbrains), он неимоверно облегчит вам жизнь, если вы собираетесь влиться в разработку (код), или билдить сборку, больше пары раз.

Следуйте гайду от [Джубами](https://docs.goobstation.com/en/general-development/setup.html) по настройке рабочей среды, но учитывайте, что репозитории отличаются друг от друга и некоторые вещи могут отличаться.
Ниже перечислены скрипты и методы облегчающие работу с билдом.

### Windows

> 1. Клонируйте данный репозиторий.
```shell
git clone https://github.com/AtaraxiaSpaceFoundation/Orion-Station-14.git
```
> 2. Откройте коммандную строку в папке репозитория и введите команду для того, чтобы скачать движок игры.
```shell
git submodule update --init --recursive
```
> 3. Следующим этапом идёт билд-билда, для этого нужно ввести команду с указанием того, для чего вы билдите, для этого нужно написать Release, Tools или Debug.
```shell
dotnet build --configuration Release/Tools/Debug
```
> [!TIP]
> К примеру **Release** - полная версия, **Tools** - урезаная версия, **Debug** - урезаная версия, но которая будет вылетать при любой ошибке. В большинстве случаев вам хватит **Tools**, что-бы не перенапрягать машину.

> 4. Далее вам требуется запустить сервер с клиентом, для этого есть несколько способов.
> - 4.1. Командами, в конце так же можно указать вместо Tools любой интересующий вас тип.
```shell
dotnet run --project Content.Server --configuration Tools
```
```shell
dotnet run --project Content.Client --configuration Tools
```
> - 4.2. Запуск .bat файла, который автоматически выполнит те же команды.
```shell
Scripts/bat/runQuickAll.bat
```
> 5. Подключитесь к **localhost** в появившемся окне и играйте!

---
<div align="center">

## Лицензия

</div>

All code in this codebase is released under the [AGPL-3.0](LICENSE-AGPLv3.TXT)-or-later license. Each file includes REUSE Specification headers or separate .license files that specify a dual license option. This dual licensing is provided to simplify the process for projects that are not using AGPL, allowing them to adopt the relevant portions of the code under an alternative license.

Most media assets are licensed under [CC-BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/) unless stated otherwise. Assets have their license and the copyright in the metadata file. [Example](https://github.com/space-wizards/space-station-14/blob/master/Resources/Textures/Objects/Tools/crowbar.rsi/meta.json).

By submitting a pull request or making a commit to the Ataraxia Space Foundation / Orion-Station, you agree to the terms of our [Contributor License Agreement](LICENSE-CLA.TXT). This agreement grants us the right to distribute your contributions under any license we choose, while you retain your copyright ownership.

</div>

> [!NOTE]
> Some assets are licensed under the non-commercial [CC-BY-NC-SA 3.0](https://creativecommons.org/licenses/by-nc-sa/3.0/) or similar non-commercial licenses and will need to be removed if you wish to use this project commercially.

<h1 align="right"> <img alt="Orion Station" src="https://raw.githubusercontent.com/AtaraxiaSpaceFoundation/asset-dump/refs/heads/master/OrionStation/Orion-Banner-Small.png" />  </h1>
