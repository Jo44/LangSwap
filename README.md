# LangSwap

Un plugin Dalamud pour Final Fantasy XIV qui permet de changer temporairement la langue du HUD et des tooltips en maintenant un combo de touches.

## Fonctionnalités

- **Changement de langue à la volée** : Maintenez un combo de touches pour basculer vers une langue cible configurée
- **Restauration automatique** : Relâchez les touches pour revenir à votre langue d'origine
- **Configuration simple** : Interface de configuration pour choisir la langue cible

## Installation

1. Compilez le projet dans Visual Studio
2. Ajoutez le chemin vers `LangSwap.dll` dans les paramètres de Dalamud (`/xlsettings` > Experimental > Dev Plugin Locations)
3. Activez le plugin via `/xlplugins` > Dev Tools > Installed Dev Plugins

## Utilisation

1. Ouvrez la configuration avec `/langswap` ou via le bouton dans le Plugin Installer
2. Sélectionnez la langue cible que vous souhaitez utiliser lorsque vous maintenez le combo de touches
3. Sélectionnez le choix du combo de touches à utiliser
4. En jeu, maintenez le combo de touches pour basculer temporairement vers la langue cible
5. Relâchez les touches pour revenir à votre langue d'origine

## Langues supportées

- Japonais (0)
- Anglais (1)
- Allemand (2)
- Français (3)

## Développement

### Structure du projet

```
ffxiv-swap-lang/
├── LangSwap.sln          # Solution Visual Studio
└── LangSwap/
    ├── LangSwap.csproj   # Fichier projet
    ├── LangSwap.json     # Métadonnées du plugin
    ├── Plugin.cs         # Classe principale du plugin
    ├── Configuration.cs  # Gestion de la configuration
    └── Windows/
        └── ConfigWindow.cs # Fenêtre de configuration
```

### TODO

- [ ] Implémenter la récupération de la langue actuelle du jeu
- [ ] Implémenter le changement de langue via l'API du jeu
- [ ] Tester la détection des touches du combo de touches

## Notes

Ce plugin nécessite l'implémentation de l'accès aux paramètres de langue du jeu via l'API Dalamud. Les méthodes `GetCurrentLanguage()` et `SetLanguage()` sont actuellement des placeholders et doivent être complétées avec l'API appropriée.
