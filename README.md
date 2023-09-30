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

### Links
- [MCTools (Stable)](https://mctools.mullak99.co.uk)
- [MCTools (Beta)](https://mctools-beta.mullak99.co.uk)

## Features

### Textures Tool

Tool for comparing textures within resource pack against vanilla. Tool will output a list of matching, missing, and unused textures.

Allows for customising what textures are excluded.

### Vanilla Assets Downloader

Tool for downloading vanilla assets for specific versions (currently limited to Java).

## API

The current API is closed-source, instead it's being rewritten and will be implemented into Obsidian's API instead. Current progress can be seen here:  
[Tools Controller](https://github.com/mullak99s-Faithful/Obsidian/blob/master/Obsidian.API/Controllers/ToolsController.cs)  
[Tools Logic](https://github.com/mullak99s-Faithful/Obsidian/blob/master/Obsidian.API/Logic/ToolsLogic.cs)  

Current instances (Stable and Beta) are using the legacy API, which is running here:  
[Legacy MCTools API](https://mctoolsapi.mullak99.co.uk/swagger/index.html)
