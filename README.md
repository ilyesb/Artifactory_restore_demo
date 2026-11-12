# ArtifactoryRestoreDemo

Projet .NET servant à valider la restauration de paquets NuGet via un dépôt
**remote Artifactory** (proxy de nuget.org) depuis **Azure DevOps Server**.
Le projet référence **75 paquets directs** (plusieurs centaines avec les dépendances transitives).

## Contenu

| Fichier | Rôle |
|---|---|
| `nuget.config` | Vide les sources par défaut et pointe uniquement vers Artifactory |
| `ArtifactoryRestoreDemo.csproj` | Projet console avec 75 paquets de test, regroupés par thème |
| `Program.cs` | Utilise quelques paquets pour prouver que la restauration a fonctionné |
| `azure-pipelines.yml` | Pipeline : restore (connexion de service) → contrôle du nombre de paquets → build → run |

## 1. Adapter l'URL Artifactory

Dans `nuget.config`, remplacer la valeur par l'URL de votre dépôt remote :

- NuGet V3 (recommandé) : `https://<hote>/artifactory/api/nuget/v3/<repo-remote>/index.json`
- NuGet V2 : `https://<hote>/artifactory/api/nuget/<repo-remote>`

## 2. Créer la connexion de service dans Azure DevOps Server

1. **Project Settings → Service connections → New service connection → NuGet**
2. **Feed URL** : exactement la même URL que dans `nuget.config`
3. **Authentication** :
   - *Basic* : utilisateur Artifactory + mot de passe / token d'accès (ou clé API chiffrée Artifactory), ou
   - *Token based* / *API key* selon votre configuration Artifactory
4. **Service connection name** : `Artifactory-NuGet` (ou ajuster la variable `artifactoryServiceConnection`)
5. Autoriser l'utilisation par les pipelines (« Grant access permission to all pipelines » ou autorisation au premier run)

> Le compte Artifactory utilisé doit avoir au minimum le droit **Read** (et **Cache/Resolve**)
> sur le dépôt remote.

## 3. Créer le pipeline

**Pipelines → New pipeline → Azure Repos Git → Existing Azure Pipelines YAML file →
`/azure-pipelines.yml`**, puis ajuster le `pool` (agent auto-hébergé).

### Ce que fait le pipeline pour forcer un vrai test

- `NUGET_PACKAGES` pointe vers un dossier temporaire du job : le cache global de l'agent n'est pas réutilisé,
  donc tous les paquets sont réellement téléchargés via Artifactory.
- `--force --no-cache` ignorent `project.assets.json` et le cache HTTP NuGet.
- Une étape PowerShell lit `project.assets.json` et échoue si moins de `minDirectPackages` (50) paquets directs
  sont restaurés ; elle affiche aussi le total avec les dépendances transitives.
- Côté Artifactory, les paquets apparaissent ensuite dans le cache du dépôt remote (`<repo-remote>-cache`).

> Si une version exacte n'existe pas sur nuget.org, NuGet prend la version supérieure la plus proche
> (avertissement NU1603, non bloquant ici). Ajustez les versions au besoin.

## 4. Tester en local

```bash
dotnet restore --configfile nuget.config
# En local, il faut des identifiants : par exemple
dotnet nuget update source artifactory-nuget --configfile nuget.config \
  --username <user> --password <token> --store-password-in-clear-text
# (ne pas committer ce nuget.config modifié)
dotnet run
```

## Dépannage

| Symptôme | Piste |
|---|---|
| `NU1301` / `Unable to load the service index` | URL incorrecte, ou agent qui ne joint pas Artifactory (proxy, certificat) |
| `401 Unauthorized` | Identifiants de la connexion de service invalides ; l'URL du feed ne correspond pas à celle du `nuget.config` |
| `NU1101` paquet introuvable | Droits Artifactory insuffisants, ou dépôt remote qui bloque le paquet (Xray, listes d'exclusion) |
| Erreur de certificat TLS | Importer l'AC interne dans le magasin de certificats de l'agent |
| `NU1900` | Audit de vulnérabilités ; désactivé dans le `.csproj` via `NuGetAudit=false` |
