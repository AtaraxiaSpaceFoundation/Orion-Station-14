mood-show-effects-start = [font size=12][color=DarkGray]Вы оцениваете своё настроение.[/color][/font]
mood-show-sanity-line = [font size=10]Рассудок: { $sanity }[/font]
mood-show-no-effects = Я не чувствую ничего особенного.

mood-effect-HungerOverfed = { GENDER($entity) ->
    [male] Я так много съел, что чувствую, будто вот-вот лопну!
    [female] Я так много съела, что чувствую, будто вот-вот лопну!
    [epicene] Они так много поели, что чувствуют, будто вот-вот лопнут!
   *[neuter] Я так много поело, что чувствую, будто вот-вот лопну!
}
mood-effect-HungerOkay = { GENDER($entity) ->
    [male] Я сыт.
    [female] Я сыта.
    [epicene] Они сыты.
   *[neuter] Я сыто.
}
mood-effect-HungerPeckish = Сейчас бы перекусить.
mood-effect-HungerStarving = МНЕ НУЖНА ЕДА!

mood-effect-ThirstOverHydrated = { GENDER($entity) ->
    [male] Я напился воды вдоволь.
    [female] Я напилась воды вдоволь.
    [epicene] Они напились воды вдоволь.
   *[neuter] Я напилось воды вдоволь.
}
mood-effect-ThirstOkay = { GENDER($entity) ->
    [male] Я чувствую себя бодрым.
    [female] Я чувствую себя бодрой.
    [epicene] Они чувствуют себя бодро.
   *[neuter] Я чувствую себя бодро.
}
mood-effect-ThirstThirsty = Губы немного пересохли.
mood-effect-ThirstParched = МНЕ НУЖНА ВОДА!

mood-effect-HealthNoDamage = Мне не больно.
mood-effect-HealthLightDamage = Это всего лишь царапина, но всё равно болит.
mood-effect-HealthSevereDamage = Боль почти невыносима!
mood-effect-HealthHeavyDamage = Агония разгрызает мою душу!

mood-effect-Handcuffed = Меня держат в плену.

mood-effect-Suffocating = Я... не... могу... дышать...

mood-effect-OnFire = ГОРЮ!!!

mood-effect-Creampied = Меня окремили. На вкус как пирог.

mood-effect-MobSlipped = { GENDER($entity) ->
    [male] Я поскользнулся! В следующий раз буду осторожнее.
    [female] Я поскользнулась! В следующий раз буду осторожнее.
    [epicene] Они поскользнулись! В следующий раз будут осторожнее.
   *[neuter] Я поскользнулось! В следующий раз буду осторожнее.
}

mood-effect-MobVomit = Моя еда ужасно на вкус, когда выходит обратно.

mood-effect-MobLowPressure = Кажется, всё моё тело вот-вот лопнет!

mood-effect-MobHighPressure = Я чувствую, будто меня со всех сторон давят!

mood-effect-TraitSaturnine = Всё просто отстой. Я ненавижу эту работу.

mood-effect-Dead = { GENDER($entity) ->
    [male] Я мёртв.
    [female] Я мертва.
    [epicene] Они мертвы.
   *[neuter] Я мертво.
}

mood-effect-BeingHugged = Обнимашки — это здорово.

mood-effect-BeingPet = Кто-то меня погладил!

mood-effect-ArcadePlay = Мне было весело играть в интересную аркадную игру.

mood-effect-GotBlessed = Меня благословили.

mood-effect-PetAnimal = Животные такие милые, я не могу перестать их гладить!

mood-effect-SavedLife = Как здорово спасти кому-то жизнь!

mood-effect-TraitorFocused = У меня есть цель, и я добьюсь её несмотря ни на что.

mood-effect-RevolutionFocused = ДА ЗДРАВСТВУЕТ РЕВОЛЮЦИЯ!!!

mood-effect-CultFocused = Тёмные боги, дайте мне силы!

mood-effect-TraitSanguine = Мне нечего бояться. В конце концов всё определённо будет хорошо!

mood-effect-HeirloomSecure = Моя реликвия в безопасности, а с ней и воспоминания о тех, кто был до меня.
mood-effect-HeirloomLost = Я не могу найти свою реликвию, как же теперь прошлое будет в безопасности?
mood-effect-HeirloomNeutral = Моей реликвии нет, но, возможно, однажды она вернётся.

# Зависимости
# Addictions
mood-effect-LotoTranscendence =
    Я ВИЖУ ВСЁ, ЧТО ЕСТЬ, ВСЁ, ЧТО БУДЕТ И ВСЁ, ЧТО БЫЛО. ВСЁ ТВОРЕНИЕ ОТКРЫЛОСЬ МОЕМУ УМУ!
    Я ДОЛЖЕН ВСЁ ИМЕТЬ. Я ДОЛЖЕН ВСЁ ЗНАТЬ. ВСЁ. НАВСЕГДА!
mood-effect-LotoEnthrallment =
    Это покинуло меня... Сердце всего творения ушло из моей души, оставив пустоту, которую я не могу вынести.
    Я боюсь, что превращусь в ничто, если не смогу снова напиться из чаши знания.

mood-effect-NicotineBenefit =
    { GENDER($entity) ->
        [male] Я чувствую, будто всю жизнь стоял, а теперь сел.
        [female] Я чувствую, будто всю жизнь стояла, а теперь села.
        [epicene] Они чувствуют, будто всю жизнь стояли, а теперь сели.
       *[neuter] Я чувствую, будто всю жизнь стояло, а теперь село.
    }
mood-effect-NicotineWithdrawal =
    { GENDER($entity) ->
        [male] Прямо сейчас я бы с удовольствием покурил.
        [female] Прямо сейчас я бы с удовольствием покурила.
        [epicene] Прямо сейчас мы бы с удовольствием покурили.
       *[neuter] Прямо сейчас я бы с удовольствием покурило.
    }

# Хирургия
mood-effect-MorphineBenefit =
    Морфин помогает мне забыть о тревогах.
mood-effect-MorphineWithdrawal =
    Всё слишком громкое, слишком яркое. Мне нужно что-то, чтобы снять напряжение...

# Surgery
mood-effect-SurgeryPain = Надрез болит.

# Наркотики
# Drugs
mood-effect-EthanolBenefit =
    { GENDER($entity) ->
        [male] Я так расслабился от выпивки.
        [female] Я так расслабилась от выпивки.
        [epicene] Они так расслабились от выпивки.
       *[neuter] Я так расслабилось от выпивки.
    }
mood-effect-SpaceDrugsBenefit =
    Ух ты, какие красивые цвета, чувак. Кажется, я слышу цвета и ощущаю на вкус звуки, чувак.

# Плазмочеловек
# Plasmaman
mood-effect-PlasmamanIngestPlasma =
    Моё тело омолодилось от свежей плазмы, текущей по моему телу.

mood-effect-PlasmamanIngestMilk =
    Я чувствую, как кальций из молока восстанавливает мои кости. Это просто восхитительно!

# Floor Juice
mood-effect-DrankBlood =
    { GENDER($entity) ->
        [male] Я только что выпил солёную, тёплую кровь. Это отвратительно!
        [female] Я только что выпила солёную, тёплую кровь. Это отвратительно!
        [epicene] Они только что выпили солёную, тёплую кровь. Это отвратительно!
       *[neuter] Я только что выпило солёную, тёплую кровь. Это отвратительно!
    }
mood-effect-DrankBloodVampiric =
    О, что за сладкий нектар, словно выдержанное вино.
mood-effect-DrankInsectBlood =
    { GENDER($entity) ->
        [male] Я только что выпил слизкую насекомую жижу. Это отвратительно!
        [female] Я только что выпила слизкую насекомую жижу. Это отвратительно!
        [epicene] Они только что выпили слизкую насекомую жижу. Это отвратительно!
       *[neuter] Я только что выпило слизкую насекомую жижу. Это отвратительно!
    }
mood-effect-DrankVomit =
    { GENDER($entity) ->
        [male] Зачем я только что выпил эту рвоту? Она на вкус как рвота!
        [female] Зачем я только что выпила эту рвоту? Она на вкус как рвота!
        [epicene] Зачем они только что выпили эту рвоту? Она на вкус как рвота!
       *[neuter] Зачем я только что выпило эту рвоту? Она на вкус как рвота!
    }
mood-effect-DrankZombieBlood =
    ЭТО БЫЛО ОТВРАТИТЕЛЬНО, КАК СМЕРТЬ В ЖИДКОЙ ФОРМЕ!

# Medicines
mood-effect-EpinephrineEffect =
    Моя кровь словно горит!
mood-effect-PsicodineEffect =
    Я чувствую полный покой.
mood-effect-StrongStimulant =
    ПОЕХАЛИ НАХУЙ!!!
mood-effect-MildPaincauser =
    Моё тело ноет.
mood-effect-StrongPaincauser =
    Агония гложет мою душу.
mood-effect-MildPainkiller =
    Боль немного отступила.
mood-effect-StrongPainkiller =
    Я почти ничего не чувствую, вся боль будто смыта и исчезла.

# Poisons
mood-effect-LacerinolEffect =
    МИЛЛИАРДЫ МАЛЕНЬКИХ НОЖЕЙ ВНУТРИ МЕНЯ, УБЕРИТЕ ИХ!
mood-effect-PuncturaseEffect =
    МОЁ ТЕЛО ПОЛНО ИГОЛОК, УБЕРИТЕ ИХ!
mood-effect-BruizineEffect =
    МЕНЯ СЛОВНО ДАВИТ КОСМИЧЕСКИЙ КОРАБЛЬ В ТЫСЯЧУ ТОНН!
mood-effect-TearGasEffect =
    МОИ ГЛАЗА ГОРЯТ, ЭТО ТАК БОЛЬНО!
mood-effect-BuzzochloricBeesEffect =
    О НЕТ, ПЧЁЛЫ! НЕ ПЧЁЛЫ! НЕ ПЧЁЛЫ ААААААААА! ОНИ У МЕНЯ В ГЛАЗАХ!
mood-effect-RomerolEffect =
    КАЖЕТСЯ, Я УМРУ. Я ЛИШЬ ТРУП, ЖДУЩИЙ СВОЕЙ МОГИЛЫ.
mood-effect-PaxEffect =
    Воу...

# Food
mood-effect-SweetenerEffect =
    Это было очень сладко.
mood-effect-SpicyEffect =
    Это было остро, но приятно.
mood-effect-OilyEffect =
    { GENDER($entity) ->
        [male] Я съел что-то, приготовленное в масле.
        [female] Я съела что-то, приготовленное в масле.
        [epicene] Они съели что-то, приготовленное в масле.
       *[neuter] Я съело что-то, приготовленное в масле.
    }
mood-effect-SaltyEffect =
    { GENDER($entity) ->
        [male] Я съел что-то солёное, было очень вкусно!
        [female] Я съела что-то солёное, было очень вкусно!
        [epicene] Они съели что-то солёное, было очень вкусно!
       *[neuter] Я съело что-то солёное, было очень вкусно!
    }
mood-effect-MintyEffect =
    { GENDER($entity) ->
        [male] Я съел что-то мятное, это было прохладно и освежающе.
        [female] Я съела что-то мятное, это было прохладно и освежающе.
        [epicene] Они съели что-то мятное, это было прохладно и освежающе.
       *[neuter] Я съело что-то мятное, это было прохладно и освежающе.
    }
mood-effect-PepperEffect =
    { GENDER($entity) ->
        [male] Я съел что-то перчёное, было очень вкусно!
        [female] Я съела что-то перчёное, было очень вкусно!
        [epicene] Они съели что-то перчёное, было очень вкусно!
       *[neuter] Я съело что-то перчёное, было очень вкусно!
    }
mood-effect-ChocolateEffect =
    { GENDER($entity) ->
        [male] Я съел что-то с шоколадом, это было невероятно вкусно!
        [female] Я съела что-то с шоколадом, это было невероятно вкусно!
        [epicene] Они съели что-то с шоколадом, это было невероятно вкусно!
       *[neuter] Я съело что-то с шоколадом, это было невероятно вкусно!
    }
mood-effect-ButterEffect =
    { GENDER($entity) ->
        [male] Я съел масляное лакомство, мог бы есть это весь день.
        [female] Я съела масляное лакомство, могла бы есть это весь день.
        [epicene] Они съели масляное лакомство и могли бы есть это весь день.
       *[neuter] Я съело масляное лакомство, могло бы есть это весь день.
    }
mood-effect-DeepFriedEffect =
    { GENDER($entity) ->
        [male] Я съел что-то во фритюре! Это было самое вкусное, что я когда-либо ел!
        [female] Я съела что-то во фритюре! Это было самое вкусное, что я когда-либо ела!
        [epicene] Они съели что-то во фритюре! Это было самое вкусное, что они когда-либо ели!
       *[neuter] Я съело что-то во фритюре! Это было самое вкусное, что я когда-либо ело!
    }
mood-effect-TastyEffect =
    Это было очень вкусно!
