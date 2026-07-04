# Branding

Statlyn branding must use official repo assets only. Do not generate placeholder logos or fetch external brand assets.

## Logo Assets

Current repo-root logo files:

- `StatLyn_Logo.png`
- `StatLyn_Logo_Reversed.png`
- `StatLyn_Mark_White_Tight.png`
- `StatLyn_Transparant_Black.png`
- `Statlyn_Logo_Black-text.png`
- `Statlyn_Logo_White-text.png`

Milestone 1.9.1 copied these files into Unity at:

```text
Statlyn.UnityApp/Assets/Resources/Branding/
```

Unity resource keys:

- `Branding/Statlyn_Logo_Black-text` for light/glassy UI.
- `Branding/Statlyn_Logo_White-text` for dark surfaces if introduced later.
- `Branding/StatLyn_Logo` for full light-background branding cards or splash/loading use.
- `Branding/StatLyn_Logo_Reversed` for full dark-background branding cards or splash/loading use.
- `Branding/StatLyn_Mark_White_Tight` for compact dark-surface icon-only use.
- `Branding/StatLyn_Transparant_Black` for compact light-surface icon-only use.

The React/Tauri desktop app packages the official assets under:

```text
Statlyn.Desktop/public/branding/
```

Desktop UI usage:

- `statlyn-wordmark-white.png` for the black glass sidebar and other dark surfaces.
- `statlyn-mark-white.png` for dark-surface app marks, loading states and compact identity marks.
- `statlyn-wordmark-black.png` and `statlyn-mark-black.png` for future light export, print or onboarding surfaces.
- Original un-cropped supplied files remain in the same folder for reference and future variants.

The Tauri Windows icon is generated from the official white mark so packaged builds use Statlyn branding instead of a placeholder.

## Unity Usage

The Unity shell/sidebar loads the official light logo resource through `StatlynUiFactory.LightLogoResourceKey`. Dashboard and Player Profile headers can use a small logo mark, but Data Sources and Recruitment Centre pages should keep ordinary page headings and avoid repeating the logo on every card.
