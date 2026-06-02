# Orion Economy, Banking, And Vending Map

## Inspect These Paths First

- Economy code: `Content.Shared/_Orion/Economy/`, `Content.Server/_Orion/Economy/`, `Content.Client/_Orion/Economy/`.
- Vending pricing component: `Content.Shared/_Orion/VendingMachines/Components/VendingMachinePricingComponent.cs`.
- Economy/vending data: `Resources/Prototypes/_Orion/Economy/`, `Resources/Prototypes/_Orion/Catalog/VendingMachines/`, `Resources/Prototypes/_Orion/Entities/Objects/Economy/`, and `Resources/Prototypes/_Orion/Entities/Structures/Machines/vending_machines.yml`.
- Locale: `Resources/Locale/en-US/_Orion/economy/`, `Resources/Locale/ru-RU/_Orion/economy/`, `Resources/Locale/en-US/_Orion/vending-machines/`, and `Resources/Locale/ru-RU/_Orion/vending-machines/`.

## Preserve Authority

- Never debit money on the client or trust client-provided/UI prices.
- Recompute final price, discounts, account source, and affordability on the server.
- Keep discounts tied to the current banking/payroll/department-account source; do not infer discounts from ID access unless the existing authoritative flow does that.
- Account for station account credit in vending sales when the current sale path supports it.
- Localize UI labels, denial reasons, receipts, and operation messages in both Orion locales when adding player-facing text.

## Validate

- Build after C# changes: `dotnet build --configuration DebugOpt --no-restore /m` after restore when needed.
- Run `dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj -c DebugOpt` after prototype or FTL changes.
- Manually or test-verify purchase denial, discounted purchase, and account-credit paths when possible.
