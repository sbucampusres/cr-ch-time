# CRCardSwipe Project Memory

## Publishing

**Do NOT use `--output` flag when publishing.** The `.csproj` has a custom `CopyToRdpShare` MSBuild target that automatically copies the publish output to `/Users/wa/Documents/RDP Share/publish/cr-cardswipe` after publish.

Correct command:
```
dotnet publish CRCardSwipe/CRCardSwipe.csproj --configuration Release
```

**Why:** The custom target deletes and recreates the destination directory as part of its copy step. If `--output` points to the same destination, it deletes the publish output before copying, leaving only `web.config`.

The intermediate publish goes to `CRCardSwipe/bin/Release/net8.0/publish/`, and the target copies it from there.
