<h1 align="center">
  MCTools
</h1>
<p align="center">
    <a href="https://mctools.mullak99.co.uk" alt="MCTools Stable">
        <img src="https://github.com/mullak99/MCTools/actions/workflows/master_deploy.yml/badge.svg" />
    </a>
    <a href="https://mctools-beta.mullak99.co.uk" alt="MCTools Beta">
        <img src="https://github.com/mullak99/MCTools/actions/workflows/beta_deploy.yml/badge.svg" />
    </a>
	<a href="https://mctools-api.mullak99.co.uk/swagger" alt="MCTools API Stable">
        <img src="https://github.com/mullak99/MCTools/actions/workflows/api_master_deploy.yml/badge.svg" />
    </a>
    <a href="https://mctools-api-beta.mullak99.co.uk/swagger" alt="MCTools API Beta">
        <img src="https://github.com/mullak99/MCTools/actions/workflows/api_beta_deploy.yml/badge.svg" />
    </a>
    <a href="https://github.com/mullak99/MCTools/issues" alt="MCTools Issues">
        <img src="https://img.shields.io/github/issues/mullak99/MCTools" />
    </a>
    <a href="https://github.com/mullak99/MCTools/pulls" alt="MCTools Pull Requests">
        <img src="https://img.shields.io/github/issues-pr/mullak99/MCTools" />
    </a>
    <a href="https://github.com/mullak99/MCTools/stargazers" alt="MCTools Stars">
        <img src="https://img.shields.io/github/stars/mullak99/MCTools" />
    </a>
</p>

A web app for a few Minecraft-related tools, specifically for resource packs.

### Links (Web App)
- [MCTools (Stable)](https://mctools.mullak99.co.uk)
- [MCTools (Beta)](https://mctools-beta.mullak99.co.uk)

### Links (API)
- [MCTools API (Stable)](https://mctools-api.mullak99.co.uk/swagger)
- [MCTools API (Beta)](https://mctools-api-beta.mullak99.co.uk/swagger)

## Features

### Textures Tool

Tool for comparing textures within resource pack against vanilla. Tool will output a list of matching, missing, and unused textures.

Allows for customising what textures are excluded.

### Potion Converter

Tool for converting potions between Java and Bedrock formats (currently limited to converting from Java only).

### Vanilla Assets

Tool for downloading vanilla assets for specific versions.

### Version Difference

Tool for comparing two versions of Vanilla (Java) Minecraft's assets. Lists showing added, removed, changed, and unchanged textures will be generated. These can be exported, or paths copied.

Changed textures additionally can be compared pixel-by-pixel and exported, resulting in Blue and Magenta pixels showing what has or hasn't changed. A README is provided in the exported zip explaining this.

## API

The Stable and Beta API's can be found linked above. API-related code can be found in the MCTools.API directory.

## SDK

The SDK can be manually downloaded from the GitHub Actions:
- [Stable SDK](https://github.com/mullak99/MCTools/actions/workflows/sdk_master_package.yml)
- [Beta SDK](https://github.com/mullak99/MCTools/actions/workflows/sdk_beta_package.yml)

Or via the [NuGet](https://nuget.mullak99.co.uk/packages/mctools.sdk).
This can also be used within Visual Studio by adding `https://nuget.mullak99.co.uk/v3/index.json` as a package source.