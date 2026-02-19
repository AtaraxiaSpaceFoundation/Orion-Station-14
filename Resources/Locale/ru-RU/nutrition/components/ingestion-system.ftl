### Interaction Messages

# System

## When trying to ingest without the required utensil... but you gotta hold it
ingestion-you-need-to-hold-utensil = Вам нужно держать {INDEFINITE($utensil)} {$utensil}, чтобы это съесть!

ingestion-try-use-is-empty = {CAPITALIZE($entity)} пусто!
ingestion-try-use-wrong-utensil = Вы не можете {$verb} {$food} с помощью {INDEFINITE($utensil)} {$utensil}.

ingestion-remove-mask = Сначала нужно снять {$entity}.

## Failed Ingestion

ingestion-you-cannot-ingest-any-more = Вы больше не можете {$verb}!
ingestion-other-cannot-ingest-any-more = {CAPITALIZE(SUBJECT($target))} больше не может {$verb}!

ingestion-cant-digest = Вы не можете переварить {$entity}!
ingestion-cant-digest-other = {CAPITALIZE(SUBJECT($target))} не может переварить {$entity}!

## Action Verbs, not to be confused with Verbs

ingestion-verb-food = Съесть
ingestion-verb-drink = Выпить

# Edible Component

edible-nom = Ням. {$flavors}
edible-nom-other = Ням.
edible-slurp = Чавк. {$flavors}
edible-slurp-other = Чавк.
edible-swallow = Вы проглатываете {$food}
edible-gulp = Глоток. {$flavors}
edible-gulp-other = Глоток.

edible-has-used-storage = Вы не можете {$verb} {$food}, так как предмет хранится внутри.

## Nouns

edible-noun-edible = съедобное
edible-noun-food = еда
edible-noun-drink = напиток
edible-noun-pill = таблетка

## Verbs

edible-verb-edible = проглотить
edible-verb-food = съесть
edible-verb-drink = выпить
edible-verb-pill = проглотить

## Force feeding

edible-force-feed = {CAPITALIZE($user)} пытается заставить вас {$verb} что-то!
edible-force-feed-success = {CAPITALIZE($user)} заставил вас {$verb} что-то! {$flavors}
edible-force-feed-success-user = Вы успешно накормил(а) {$target}
