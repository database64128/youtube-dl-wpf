# 🎞⬇ Cube YouTube Downloader - `youtube-dl-wpf`

[![Build](https://github.com/database64128/youtube-dl-wpf/workflows/Build/badge.svg)](https://github.com/database64128/youtube-dl-wpf/actions?query=workflow%3ABuild)
[![Release](https://github.com/database64128/youtube-dl-wpf/workflows/Release/badge.svg)](https://github.com/database64128/youtube-dl-wpf/actions?query=workflow%3ARelease)

A simple GUI wrapper for [`youtube-dl`](https://github.com/ytdl-org/youtube-dl) and [`yt-dlp`](https://github.com/yt-dlp/yt-dlp).

![Home](Home.png "Home")
![Settings](Settings.png "Settings")

## Features

- Follow 🎨 system color mode, or choose between 🌃 dark mode and 🔆 light mode.
- Update `youtube-dl`/`yt-dlp` on startup.
- List all available formats.
- Override video, audio formats and output container.
- Embed metadata into downloaded file.
- Download and embed thumbnails and subtitles.
- Download playlists.
- Specify custom download path.
- Specify custom `ffmpeg` path.
- Specify custom proxy.

## Usage

1. Download the pre-built binary or build it from source.
2. Download [`youtube-dl`](https://github.com/ytdl-org/youtube-dl) or [`yt-dlp`](https://github.com/yt-dlp/yt-dlp) from the upstream. _Optionally but recommended_, get `ffmpeg` either by [building from source](https://www.ffmpeg.org/) or downloading [pre-built binaries](https://ffmpeg.zeranoe.com/builds/) for Windows.
3. The framework-dependent binary requires an installed [.NET 5 Runtime](https://dotnet.microsoft.com/download/dotnet/5.0) to run. Alternatively, download the self-contained binary that bundles the runtime. The upstream `youtube-dl` binary requires MSVC++ 2010 x86 runtime.
4. Run `youtube-dl-wpf.exe`. Go to __Settings__. Set the path to `youtube-dl`/`yt-dlp` and `ffmpeg`.
5. Go back to the home tab. Paste a video URL and start downloading! 🚀

## FAQ

1.  Q: The __Download__ button is grayed out and I can't click it!

    A: `youtube-dl-wpf` is a simple GUI wrapper. It doesn't bundle any downloader with it. You have to download `youtube-dl`/`yt-dlp` from the upstream. The `ffmpeg` binary is required by `youtube-dl`/`yt-dlp` when downloading and merging separate video and audio tracks, which is the case for any video resolution higher than 360p on YouTube.

2.  Q: How can I use a proxy to download?

    A: Leave the proxy field empty to use system proxy settings. Otherwise the format is similar to how `curl` accepts proxy strings. Examples are, `socks5://localhost:1080/`, `http://localhost:8080/`. Currently the upstream doesn't accept `socks5h` protocol and treat `socks5` as `socks5h` by always resolving the hostname using the proxy. This is tracked in [this issue](https://github.com/ytdl-org/youtube-dl/issues/22618).

3.  Q: Downloading the whole playlist doesn't work!

    A: It's an upstream bug, just like many other issues you might discover. There's nothing I can do. Just report the bug to the [upstream](https://github.com/ytdl-org/youtube-dl).

4.  Q: `youtube-dl` and `yt-dlp` behave differently!

    A: In some cases, yes, and `youtube-dl-wpf` tries to align their behavior by sending different options and arguments for different backends. See the [backends documentation](Backends.md) for more information.

## Known Issues

- 🎉 No known issues!

## To-Do

- [ ] v2.0 - The Parallel Update: download management and download queue for parallel downloads.

## Build

Prerequisites: .NET 5 SDK

Note for packagers: The application by default uses executable directory as config directory. To use user's config directory, define the constant `PACKAGED` when building.

```bash
# Build with Release configuration
$ dotnet build -c Release

# Publish as framework-dependent
$ dotnet publish YoutubeDl.Wpf -c Release

# Publish as self-contained for Windows x64
$ dotnet publish YoutubeDl.Wpf -c Release -r win-x64 --self-contained

# Publish as self-contained for packaging on Windows x64
$ dotnet publish YoutubeDl.Wpf -c Release -p:DefineConstants=PACKAGED -r win-x64 --self-contained
```

## License

- This project is licensed under [GPLv3](LICENSE).
- The icons are from [Material Design Icons](https://materialdesignicons.com/) and are licensed under the [Pictogrammers Free License](https://dev.materialdesignicons.com/license).
- [`youtube-dl`](https://github.com/ytdl-org/youtube-dl) and [`yt-dlp`](https://github.com/yt-dlp/yt-dlp) are licensed under [The Unlicense](https://github.com/ytdl-org/youtube-dl/blob/master/LICENSE).
- [Material Design Themes](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit) is licensed under [MIT](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/blob/master/LICENSE).
- [Roboto Mono](https://fonts.google.com/specimen/Roboto+Mono) is licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
- [ReactiveUI](https://github.com/reactiveui/ReactiveUI) and its dependencies are licensed under [MIT](https://github.com/reactiveui/ReactiveUI/blob/main/LICENSE).

© 2021 database64128
