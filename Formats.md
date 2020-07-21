# Format Selection Design Goals

## Container Selections (editable):

```
- Auto
- .webm
- .mp4
- .mkv
- .opus
- .flac
- .ogg
- .m4a
- .mp3
```

## Quality Selections (non-editable):

```
- Auto
- Custom
- bestvideo+bestaudio/best
- bestvideo+bestaudio
- bestvideo+worstaudio
- worstvideo+bestaudio
- worstvideo+worstaudio
- worstvideo+worstaudio/worst
- best
- worst
- bestvideo
- worstvideo
- bestaudio
- worstaudio
```

## Generated Format Selection Strings:

- `Auto + bestvideo+bestaudio/best == bestvideo+bestaudio/best`
- `Auto + Custom + 248 + 251 == 248 + 251`
- `.webm + Auto == webm`
- `.webm + bestvideo+bestaudio/best == (bestvideo+bestaudio/best)[ext=webm]`
- `.webm + Custom + 248 + 251 == (248+251)[ext=webm]`
- `.mp4 + Custom + 137 + 140 == 137[ext=mp4]+140[ext=m4a]`

## Rules

- Can select quality and custom formats ONLY WHEN container is `Auto`.
- Can specify custom formats ONLY WHEN quality is `Custom`.
