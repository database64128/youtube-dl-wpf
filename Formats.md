# Format Selection Design Goals

## Container Selection (editable):

```
- Auto
- webm
- mp4
- mkv
- opus
- flac
- ogg
- m4a
- mp3
```

## Format Selection (editable):

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
- YouTube 4K 60fps HDR webm (337+251)
- YouTube 4K 60fps webm (315+251)
- YouTube 4K 60fps AV1 + AAC (401+140)
- YouTube 4K webm (313+251)
- YouTube 4K AV1 + AAC (401+140)
- YouTube 1440p60 HDR webm (336+251)
- YouTube 1440p60 webm (308+251)
- YouTube 1440p60 AV1 + AAC (400+140)
- YouTube 1440p webm (271+251)
- YouTube 1440p AV1 + AAC (400+140)
- YouTube 1080p60 webm (303+251)
- YouTube 1080p60 AV1 + AAC (399+140)
- YouTube 1080p60 AVC + AAC (299+140)
- YouTube 1080p webm (248+251)
- YouTube 1080p AV1 + AAC (399+140)
- YouTube 1080p AVC + AAC (137+140)
- YouTube 720p60 webm (302+251)
- YouTube 720p60 AV1 + AAC (398+140)
- YouTube 720p60 AVC + AAC (298+140)
- YouTube 720p webm (247+251)
- YouTube 720p AV1 + AAC (398+140)
- YouTube 720p AVC + AAC (136+140)
- 1080p
- 720p
```

## Generated Format Selection Strings:

- `Auto + bestvideo+bestaudio/best == bestvideo+bestaudio/best`
- `Auto + "248+251" == 248+251`
- `webm + Auto == webm`
- `webm + bestvideo+bestaudio/best == (bestvideo+bestaudio/best)[ext=webm]`
- `webm + "248+251" == (248+251)[ext=webm]`
- `mp4 + Custom + "137+140" == 137[ext=mp4]+140[ext=m4a]`

## Rules

- Can select format ONLY WHEN container is `Auto`.
