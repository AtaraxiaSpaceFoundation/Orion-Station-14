reagent-effect-guidebook-add-moodlet =
    Modifies mood by {$amount}
    and lasts for {$timeout} seconds.

reagent-effect-guidebook-remove-moodlet =
    Removes the {$name} moodlet.

reagent-effect-guidebook-purge-moodlets =
    Removes all active non-permanent moodlets.

reagent-effect-guidebook-oxygenate =
    { $chance ->
        [1] Improves oxygenation by { NATURALFIXED($factor, 1) } and slows further suffocation damage
       *[other] Improves oxygenation by { NATURALFIXED($factor, 1) } and slows further suffocation damage
    }
