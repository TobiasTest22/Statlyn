# Branding

Statlyn branding must use official repo assets only. Do not generate placeholder logos or fetch external brand assets.

## Logo Assets

Current repo-root logo files:

- `StatLyn_Logo.png`
- `StatLyn_Logo_Reversed.png`
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

The milestone brief also referenced `StatLyn_Transparant_Black.png` and `StatLyn_Transparant_White.png`; those exact files were not present in the repo at implementation time. Until they are added, use the closest available official Statlyn black-text or white-text logo assets above.

## Unity Usage

The Unity shell/sidebar loads the official light logo resource through `StatlynUiFactory.LightLogoResourceKey`. Dashboard headers can use a small logo mark, but Data Sources and Recruitment Centre pages should keep ordinary page headings and avoid repeating the logo on every card.
