# Statlyn managed plugin assemblies

This folder is populated by `tools/copy-managed-to-unity.ps1` after the managed solution builds.

The Unity Player Profile slice references the shared Statlyn managed assemblies from here so it can render a `UnityProfileRenderModel` built from `MaskedPlayerProfileViewModel`. Do not commit generated DLLs; rebuild and rerun the copy script before manual Unity editor validation.
