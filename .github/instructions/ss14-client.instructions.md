<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

---
applyTo: "Content.Client/**/*.cs,Content.Client/**/*.xaml,Content.Client/**/*.xaml.cs,Content.Goobstation.Client/**/*.cs,Content.Goobstation.Client/**/*.xaml,Content.Goobstation.Client/**/*.xaml.cs,Content.Goobstation.UIKit/**/*.cs,Content.Goobstation.UIKit/**/*.xaml,Content.Goobstation.UIKit/**/*.xaml.cs"
---

For client content, Goobstation client/UIKit, and XAML work:

- Load `ss14-ui-bui`, `ss14-prediction`, `ss14-localization-strings`, and `ss14-localization-code`.
- Prefer XAML over building windows entirely in C#.
- Reuse nearby `FancyWindow`, bound UI, and style patterns before inventing a new structure.
- Prefer reading already-networked component state instead of duplicating BUI state unless the existing pattern requires it.
