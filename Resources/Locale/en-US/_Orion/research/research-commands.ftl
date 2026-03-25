unlockallresearches-command-description = Unlock all research technologies on a specific research server.
unlockallresearches-command-help = Usage: { $command } <serverUid>
unlockallresearches-command-hint-server = research server uid
unlockallresearches-command-success = Unlocked { $count } technologies on server { $serverUid } ({ $serverName }).

addresearchpoints-command-description = Add research points of a specific type on a specific research server.
addresearchpoints-command-help = Usage: { $command } <serverUid> <pointType> <amount>
addresearchpoints-command-hint-server = research server uid
addresearchpoints-command-hint-type = research point type
addresearchpoints-command-hint-amount = point delta (can be negative)
addresearchpoints-command-success = Server { $serverUid } ({ $serverName }) now has { $balance } points of type '{ $type }'.

command-error-invalid-server-uid = Invalid server entity uid.
command-error-not-research-server = Entity is not a research server.
command-error-missing-tech-database = Research server has no technology database component.
command-error-empty-point-type = Point type cannot be empty.
command-error-invalid-amount = Invalid amount.
