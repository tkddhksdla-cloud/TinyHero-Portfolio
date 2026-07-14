# TinyHero Remote Addressables

## Initial player release

1. Run `TinyHero/Addressables/Build Initial Remote Content` when validating content without a player build.
2. Build the release Windows player through the existing TinyHero custom build.
3. Preserve the generated `addressables_content_state.bin` for that player release.
4. Publish the contents of `ServerData` at the URL configured in `TinyHeroContentEndpoint.json`.

The first remote-enabled player build is mandatory. A player built without a remote catalog cannot discover later content updates.

## Content update

```powershell
./Tools/Addressables/Invoke-TinyHeroContentUpdate.ps1 `
  -ContentStatePath ./Assets/AddressableAssetsData/Windows/addressables_content_state.bin `
  -PublishPath PublishedContent `
  -LocalServerPath C:/TinyHeroLocalServer/TinyHeroContent
```

Upload or serve the resulting `PublishedContent` directory without changing its platform subdirectory structure.

## Change a built player's endpoint

```powershell
./Tools/Addressables/Set-TinyHeroBuildContentEndpoint.ps1 `
  -BuildPath ./Builds/Windows/100 `
  -RemoteBaseUrl http://127.0.0.1:8082/TinyHeroContent
```

Runtime endpoint priority is command line `-tinyHeroContentUrl`, environment variable `TINYHERO_CONTENT_URL`, `StreamingAssets/TinyHeroContentEndpoint.json`, then the localhost default.

For live builds, keep `requireRemoteContent` set to `true`. Catalog check, required catalog update, or required bundle download failures then block gameplay instead of silently mixing fallback data with a newer catalog. The endpoint script writes this live-safe value by default.

## Jenkins

- `PLAYER_BUILD` builds the Windows player, stages `ServerData` in `CONTENT_PUBLISH_PATH`, optionally copies it to `LOCAL_CONTENT_SERVER_PATH`, and writes `CONTENT_BASE_URL` into the built player.
- `CONTENT_UPDATE` builds an update from `CONTENT_STATE_PATH`, stages it, and optionally deploys it to the same local server path without rebuilding the player.
- Set `LOCAL_CONTENT_SERVER_PATH` to the physical directory served as `http://127.0.0.1:8082/TinyHeroContent`. Leave it empty when only Jenkins artifacts are needed.
- Keep the content-state file paired with its player release. Do not use a state file generated for a different player release.
