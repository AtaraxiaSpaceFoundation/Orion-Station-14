reagent-general = General
reagent-incendiary = Incendiary

reagent-name-convermol = convermol
reagent-desc-convermol = Rapidly treats asphyxiation, producing toxins as a byproduct. Both effects scale with reagent quantity in the bloodstream. Overdose removes the toxin production cap.
reagent-physical-desc-convermol = tart
reagent-effect-guidebook-convermol =
    { $chance ->
        [1] Heals asphyxiation ({ $rate } u/u reagent), producing toxins at a 1:{ $ratio } ratio. Overdose threshold: { $od } u.
       *[other] With { NATURALPERCENT($chance, 1) } chance, heals asphyxiation with toxic side effects.
    }
