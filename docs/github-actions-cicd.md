# GitHub Actions CI/CD

This repository includes GitHub Actions workflows for MAUI build/release automation:

* `.github/workflows/dotnet-windows.yml`
  * Triggers: push and pull request on `main`, plus manual dispatch
  * Purpose: restore and build Windows target (`net10.0-windows10.0.19041.0`)

* `.github/workflows/android-release.yml`
  * Trigger: manual (`workflow_dispatch`)
  * Input: `package_format` (`both`, `aab`, `apk`)
  * Purpose: signed Android release publish and artifact upload
  * Artifacts: signed outputs only (`*-Signed.aab`, `*-Signed.apk`)

* `.github/workflows/ios-release.yml`
  * Trigger: manual (`workflow_dispatch`)
  * Purpose: validate iOS signing, build signed IPA, and upload artifact
  * Artifacts: built `.ipa`

All repository secrets (single list):

```dos
ANDROID_KEYSTORE=
ANDROID_KEY_ALIAS=
ANDROID_KEY_PASSWORD=
ANDROID_KEYSTORE_PASSWORD=

IOS_CODESIGN_KEY=
IOS_P12_BASE64=
IOS_P12_PASSWORD=
IOS_MOBILEPROVISION_BASE64=

APPSTORE_USERNAME=
APPSTORE_APP_PASSWORD=
APPSTORE_PROVIDER_PUBLIC_ID=
```

Secret meaning and how to create values:

* `IOS_CODESIGN_KEY`
  * What it is: the exact certificate identity name used for code signing.
  * Example value: `Apple Distribution: Your Company Name (TEAMID1234)`
  * How to find it on macOS:
    * Run: `security find-identity -v -p codesigning`
    * Copy the full identity string exactly.

* `IOS_P12_BASE64`
  * What it is: base64 of the exported signing certificate `.p12` file.
  * How to create `.p12`:
    * Open Keychain Access -> My Certificates.
    * Export your Apple Distribution certificate as `.p12`.
    * Set an export password (this becomes `IOS_P12_PASSWORD`).
  * How to produce base64 (macOS):
```xml
base64 -i ios_dist.p12 | pbcopy
```
  * How to produce base64 (Windows PowerShell):
```xml
$b64 = [Convert]::ToBase64String(
  [IO.File]::ReadAllBytes("C:\path\ios_dist.p12")
)
$b64 | Set-Clipboard
Write-Output $b64
```

* `IOS_P12_PASSWORD`
  * What it is: the password you entered when exporting the `.p12` certificate.
  * Important: this is not your Apple ID password.

* `IOS_MOBILEPROVISION_BASE64`
  * What it is: base64 of the provisioning profile `.mobileprovision` used for this app id.
  * How to get profile:
    * Apple Developer Portal -> Profiles -> create/download App Store profile for `com.appomobi.drawnui.shaderscam`.
  * How to produce base64 (macOS):
```xml
base64 -i ShadersCam.mobileprovision | pbcopy
```
  * How to produce base64 (Windows PowerShell):
```xml
$b64 = [Convert]::ToBase64String(
  [IO.File]::ReadAllBytes("C:\path\ShadersCam.mobileprovision")
)
$b64 | Set-Clipboard
Write-Output $b64
```

App Store upload secrets:

* `APPSTORE_USERNAME`
  * What it is: Apple ID email used for App Store Connect uploads.
  * Example: `your.name@company.com`

* `APPSTORE_APP_PASSWORD`
  * What it is: Apple app-specific password (not your Apple ID login password).
  * Where to create it:
    * Go to https://account.apple.com
    * Sign in with the Apple ID from `APPSTORE_USERNAME`
    * Open Sign-In and Security -> App-Specific Passwords
    * Create a new app-specific password and copy it immediately
  * Store that generated value in this secret.

* `APPSTORE_PROVIDER_PUBLIC_ID` (optional)
  * What it is: provider public id used only if the Apple ID belongs to multiple providers/teams.
  * Leave empty unless upload fails with provider ambiguity.

Recommended combined flow in one workflow file:

* Validate signing assets
* Build signed IPA
* Upload IPA to App Store Connect

Minimal upload example in workflow step:

* `xcrun altool --upload-app --type ios --file <path-to-ipa> --username "$APPSTORE_USERNAME" --password "$APPSTORE_APP_PASSWORD"`

Notes:

* SDK is pinned via `global.json`.
* Android version/build numbers are derived from manifest/project values plus GitHub run number.
* iOS profile name is parsed from the provisioning profile during the workflow.