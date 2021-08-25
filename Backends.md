# Backends

`youtube-dl-wpf` supports both the original [`youtube-dl`](https://github.com/ytdl-org/youtube-dl) and the better-maintained [`yt-dlp`](https://github.com/yt-dlp/yt-dlp). It tries to automatically detect the backend type when setting the path to backend binary. You can also toggle the backend mode manually.

## Differences

1. Download and embed subtitles: `youtube-dl` requires both `--write-sub` and `--embed-subs`, while `yt-dlp` only requires `--embed-subs`. Using `--write-sub` with `yt-dlp` results in the downloaded subtitle file being kept after embedding.
