entity-effect-guidebook-modify-disgust =
    { $chance ->
        [1] { $deltasign ->
                [1] Повышает
                *[-1] Понижает
            }
        *[other]
            { $deltasign ->
                [1] повышение
                *[-1] понижение
            }
    } уровень отвращения на { $amount }
