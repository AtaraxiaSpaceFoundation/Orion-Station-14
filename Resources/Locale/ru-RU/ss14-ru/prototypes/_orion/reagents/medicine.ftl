reagent-name-convermol = конвермол
reagent-desc-convermol = Мощное средство от гипоксии с токсическим побочным эффектом. При передозировке снимается ограничение на лечение, что усиливает побочную токсичность.
reagent-physical-desc-convermol = кисловатое
reagent-effect-guidebook-convermol =
    { $chance ->
        [1] Переводит Нехватка воздуха ({ $rate }урона/ед. реагента) в токсины пропорцией 1:{ $ratio }. Переводит полный вылеченный урон в яды при { $od }ед
       *[other] С вероятностью { NATURALPERCENT($chance, 1) } лечит удушье с токсическим побочным эффектом.
    }

reagent-name-salbutamol = сальбутамол
reagent-desc-salbutamol = Замедляет дальнейшее удушье и стабилизирует дыхание пациента. Хорошо подходит для экстренной стабилизации.
reagent-physical-desc-salbutamol = прозрачное
