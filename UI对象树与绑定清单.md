## 2026-10-20 对话框点击推进绑定

- `Explore/DialoguePanel` 挂已有 `V24LineHit`，运行时在 `Explore()` 中将 `Clicked` 绑定 `AdvanceDialogueFromPanel()`；底层 `TextBackdrop/Image` 开启 Raycast Target，保留层级和尺寸。
- 对白行 `Body` 的 `FlowTextLink` 继续处理链接，`V24LineHit` 对非链接点击推进；选项回复同样绑定。Continue、Auto、Choices、DialogueScroll 和滚动条原绑定保留，不在正文上覆盖透明按钮。

## 2026-10-20 对话选项绑定

- CastleV24Controller.DialogueChoicePrefab → Assets/Castle/Prefabs/V24/DialogueChoice.prefab；绑定 Title、Body（隐藏）、Select、Background。模板挂 V24DialogueChoice，Border 指向根 Image，Glow 指向 HighlightMask，FirstBorder／RegularBorder 分别绑定 button_talking_01／02。
- HighlightMask 使用 02 的透明轮廓裁切子级 GoldTexture（03），仅悬停／选中／按下显示，不拦截鼠标。
- Explore/DialoguePanel/Choices 改为 VerticalLayoutGroup，位置 (50,-586)、尺寸 425×162、间距 8；第一项高 54，其余高 46。选项期间 DialogueScroll／Viewport 高度 480，结束后恢复。

## 2026-10-20 新增绑定与字体

- UIFlow / CastleV24Controller：WorkbenchFont 为点点像素体-方形，ReadingFont 为 I.Ming-7.00，ButtonFont 为万事全兴体。TMP 字库 V24Reading／V24Workbench；中控、阅读与主界面字体按用途分配。
- Acquire/IconBack 保留原区域 RectTransform，仅在获得道具时停用静态 Image 并显示子级 ItemArt（RawImage）；线索、人物灵感等非道具恢复原固定 Image，隐藏 ItemArt。绑定键 ItemArt 保留。
- Burn/FilesScroll 的内容使用 RecordingFolder／RecordingFile 预制件；List、Burn、Close 原绑定保持不变。
- 新增 Overlays/Credits（Image 黑底 + CastleUiView），登记 Ui.Modals；Credits/Close 绑定关闭按钮，标题页 Credits 按钮运行时打开此弹层。Heading、Role0～10、Name0～10 为可编辑的固定白色文本。
- Space 的 DoorOpen／DoorClosed 按当前标签独立设置；Details 显示各标签状态。已放置房间点击选择、拖放移动。

# V2.4 UI 对象树与绑定清单

固定组件已保存到 CastleEditable.unity；运行时仅复制列表 Prefab。UIFlow 挂 CastleV24Controller（旧 CastleFlowController 已禁用）。Canvas 挂 CastleFlowUi。

## Title

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Title | Text | UIFlow/Canvas/Stage/Pages/Title/Title |
| Subtitle | Text | UIFlow/Canvas/Stage/Pages/Title/Subtitle |
| Start | Button | UIFlow/Canvas/Stage/Pages/Title/Start |
| StartLabel | Text | UIFlow/Canvas/Stage/Pages/Title/Start/StartLabel |
| Continue | Button | UIFlow/Canvas/Stage/Pages/Title/Continue |
| ContinueLabel | Text | UIFlow/Canvas/Stage/Pages/Title/Continue/ContinueLabel |
| Settings | Button | UIFlow/Canvas/Stage/Pages/Title/Settings |
| SettingsLabel | Text | UIFlow/Canvas/Stage/Pages/Title/Settings/SettingsLabel |
| Credits | Button | UIFlow/Canvas/Stage/Pages/Title/Credits |
| CreditsLabel | Text | UIFlow/Canvas/Stage/Pages/Title/Credits/CreditsLabel |
| Quit | Button | UIFlow/Canvas/Stage/Pages/Title/Quit |
| QuitLabel | Text | UIFlow/Canvas/Stage/Pages/Title/Quit/QuitLabel |

## Invitation

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Surface | Button | UIFlow/Canvas/Stage/Pages/Invitation/Surface |
| SurfaceLabel | Text | UIFlow/Canvas/Stage/Pages/Invitation/Surface/SurfaceLabel |
| Heading | Text | UIFlow/Canvas/Stage/Pages/Invitation/Heading |
| Body | Text | UIFlow/Canvas/Stage/Pages/Invitation/Body |
| Artwork | Image | UIFlow/Canvas/Stage/Pages/Invitation/Letter |

## Explore

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| RoomName | Text | UIFlow/Canvas/Stage/Pages/Explore/RoomName |
| Portrait | Text | UIFlow/Canvas/Stage/Pages/Explore/Portrait |
| People | RectTransform | UIFlow/Canvas/Stage/Pages/Explore/PeopleScroll/Viewport/People |
| PeopleScroll | ScrollRect | UIFlow/Canvas/Stage/Pages/Explore/PeopleScroll |
| FreePanel | RectTransform | UIFlow/Canvas/Stage/Pages/Explore/FreePanel |
| RoomHint | Text | UIFlow/Canvas/Stage/Pages/Explore/FreePanel/RoomHint |
| Talk | Button | UIFlow/Canvas/Stage/Pages/Explore/FreePanel/Talk |
| TalkLabel | Text | UIFlow/Canvas/Stage/Pages/Explore/FreePanel/Talk/TalkLabel |
| Map | Button | UIFlow/Canvas/Stage/Pages/Explore/FreePanel/Map |
| MapLabel | Text | UIFlow/Canvas/Stage/Pages/Explore/FreePanel/Map/MapLabel |
| ExploreRoom | Button | UIFlow/Canvas/Stage/Pages/Explore/FreePanel/ExploreRoom |
| ExploreRoomLabel | Text | UIFlow/Canvas/Stage/Pages/Explore/FreePanel/ExploreRoom/ExploreRoomLabel |
| Workbench | Button | UIFlow/Canvas/Stage/Pages/Explore/FreePanel/Workbench |
| WorkbenchLabel | Text | UIFlow/Canvas/Stage/Pages/Explore/FreePanel/Workbench/WorkbenchLabel |
| DialoguePanel | RectTransform | UIFlow/Canvas/Stage/Pages/Explore/DialoguePanel |
| Dialogue | RectTransform | UIFlow/Canvas/Stage/Pages/Explore/DialoguePanel/DialogueScroll/Viewport/Dialogue |
| DialogueScroll | ScrollRect | UIFlow/Canvas/Stage/Pages/Explore/DialoguePanel/DialogueScroll |
| Choices | RectTransform | UIFlow/Canvas/Stage/Pages/Explore/DialoguePanel/Choices |
| Auto | Button | UIFlow/Canvas/Stage/Pages/Explore/DialoguePanel/Auto |
| AutoLabel | Text | UIFlow/Canvas/Stage/Pages/Explore/DialoguePanel/Auto/AutoLabel |
| Continue | Button | UIFlow/Canvas/Stage/Pages/Explore/DialoguePanel/Continue |
| ContinueLabel | Text | UIFlow/Canvas/Stage/Pages/Explore/DialoguePanel/Continue/ContinueLabel |
| ExitTape | Button | UIFlow/Canvas/Stage/Pages/Explore/DialoguePanel/ExitTape |
| ExitTapeLabel | Text | UIFlow/Canvas/Stage/Pages/Explore/DialoguePanel/ExitTape/ExitTapeLabel |
| CharacterImage | Image | UIFlow/Canvas/Stage/Pages/Explore/CharacterImage |
| Protagonist | Text | UIFlow/Canvas/Stage/Pages/Explore/Protagonist |
| HeroineImage | Image | UIFlow/Canvas/Stage/Pages/Explore/HeroineImage |
| ZoomHint | Text | UIFlow/Canvas/Stage/Pages/Explore/ZoomHint |
| AvatarStack | Button | UIFlow/Canvas/Stage/Pages/Explore/AvatarStack |
| StackHead | RawImage | UIFlow/Canvas/Stage/Pages/Explore/AvatarStack/Head |

## Recordings

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Recordings | Button | UIFlow/Canvas/Stage/Pages/Recordings/Recordings |
| RecordingsLabel | Text | UIFlow/Canvas/Stage/Pages/Recordings/Recordings/RecordingsLabel |
| Anchors | Button | UIFlow/Canvas/Stage/Pages/Recordings/Anchors |
| AnchorsLabel | Text | UIFlow/Canvas/Stage/Pages/Recordings/Anchors/AnchorsLabel |
| Space | Button | UIFlow/Canvas/Stage/Pages/Recordings/Space |
| SpaceLabel | Text | UIFlow/Canvas/Stage/Pages/Recordings/Space/SpaceLabel |
| PreviousPuzzle | Button | UIFlow/Canvas/Stage/Pages/Recordings/PreviousPuzzle |
| PreviousPuzzleLabel | Text | UIFlow/Canvas/Stage/Pages/Recordings/PreviousPuzzle/PreviousPuzzleLabel |
| PuzzleName | Text | UIFlow/Canvas/Stage/Pages/Recordings/PuzzleName |
| NextPuzzle | Button | UIFlow/Canvas/Stage/Pages/Recordings/NextPuzzle |
| NextPuzzleLabel | Text | UIFlow/Canvas/Stage/Pages/Recordings/NextPuzzle/NextPuzzleLabel |
| List | RectTransform | UIFlow/Canvas/Stage/Pages/Recordings/ListScroll/Viewport/List |
| ListScroll | ScrollRect | UIFlow/Canvas/Stage/Pages/Recordings/ListScroll |
| FileInfo | Text | UIFlow/Canvas/Stage/Pages/Recordings/FileInfo |
| Lines | RectTransform | UIFlow/Canvas/Stage/Pages/Recordings/LinesScroll/Viewport/Lines |
| LinesScroll | ScrollRect | UIFlow/Canvas/Stage/Pages/Recordings/LinesScroll |
| Playback | Text | UIFlow/Canvas/Stage/Pages/Recordings/Playback |
| Play | Button | UIFlow/Canvas/Stage/Pages/Recordings/Play |
| PlayLabel | Text | UIFlow/Canvas/Stage/Pages/Recordings/Play/PlayLabel |
| Progress | Slider | UIFlow/Canvas/Stage/Pages/Recordings/Progress |
| Done | Button | UIFlow/Canvas/Stage/Pages/Recordings/Done |
| DoneLabel | Text | UIFlow/Canvas/Stage/Pages/Recordings/Done/DoneLabel |
| Extract | Button | UIFlow/Canvas/Stage/Pages/Recordings/Extract |
| ExtractLabel | Text | UIFlow/Canvas/Stage/Pages/Recordings/Extract/Label |
| Duration | Text | UIFlow/Canvas/Stage/Pages/Recordings/Duration |

## Anchors

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Recordings | Button | UIFlow/Canvas/Stage/Pages/Anchors/Recordings |
| RecordingsLabel | Text | UIFlow/Canvas/Stage/Pages/Anchors/Recordings/RecordingsLabel |
| Anchors | Button | UIFlow/Canvas/Stage/Pages/Anchors/Anchors |
| AnchorsLabel | Text | UIFlow/Canvas/Stage/Pages/Anchors/Anchors/AnchorsLabel |
| Space | Button | UIFlow/Canvas/Stage/Pages/Anchors/Space |
| SpaceLabel | Text | UIFlow/Canvas/Stage/Pages/Anchors/Space/SpaceLabel |
| PreviousPuzzle | Button | UIFlow/Canvas/Stage/Pages/Anchors/PreviousPuzzle |
| PreviousPuzzleLabel | Text | UIFlow/Canvas/Stage/Pages/Anchors/PreviousPuzzle/PreviousPuzzleLabel |
| PuzzleName | Text | UIFlow/Canvas/Stage/Pages/Anchors/PuzzleName |
| NextPuzzle | Button | UIFlow/Canvas/Stage/Pages/Anchors/NextPuzzle |
| NextPuzzleLabel | Text | UIFlow/Canvas/Stage/Pages/Anchors/NextPuzzle/NextPuzzleLabel |
| List | RectTransform | UIFlow/Canvas/Stage/Pages/Anchors/ListScroll/Viewport/List |
| ListScroll | ScrollRect | UIFlow/Canvas/Stage/Pages/Anchors/ListScroll |
| DropZone | FlowDrag | UIFlow/Canvas/Stage/Pages/Anchors/TrackDropArea |
| Tracks | RectTransform | UIFlow/Canvas/Stage/Pages/Anchors/TrackDropArea/TracksScroll/Viewport/Tracks |
| TracksScroll | ScrollRect | UIFlow/Canvas/Stage/Pages/Anchors/TrackDropArea/TracksScroll |
| Status | Text | UIFlow/Canvas/Stage/Pages/Anchors/Status |
| Check | Button | UIFlow/Canvas/Stage/Pages/Anchors/Check |
| CheckLabel | Text | UIFlow/Canvas/Stage/Pages/Anchors/Check/CheckLabel |

## Space

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Recordings | Button | UIFlow/Canvas/Stage/Pages/Space/Recordings |
| RecordingsLabel | Text | UIFlow/Canvas/Stage/Pages/Space/Recordings/RecordingsLabel |
| Anchors | Button | UIFlow/Canvas/Stage/Pages/Space/Anchors |
| AnchorsLabel | Text | UIFlow/Canvas/Stage/Pages/Space/Anchors/AnchorsLabel |
| Space | Button | UIFlow/Canvas/Stage/Pages/Space/Space |
| SpaceLabel | Text | UIFlow/Canvas/Stage/Pages/Space/Space/SpaceLabel |
| PreviousPuzzle | Button | UIFlow/Canvas/Stage/Pages/Space/PreviousPuzzle |
| PreviousPuzzleLabel | Text | UIFlow/Canvas/Stage/Pages/Space/PreviousPuzzle/PreviousPuzzleLabel |
| PuzzleName | Text | UIFlow/Canvas/Stage/Pages/Space/PuzzleName |
| NextPuzzle | Button | UIFlow/Canvas/Stage/Pages/Space/NextPuzzle |
| NextPuzzleLabel | Text | UIFlow/Canvas/Stage/Pages/Space/NextPuzzle/NextPuzzleLabel |
| List | RectTransform | UIFlow/Canvas/Stage/Pages/Space/ListScroll/Viewport/List |
| ListScroll | ScrollRect | UIFlow/Canvas/Stage/Pages/Space/ListScroll |
| FloorImage | RawImage | UIFlow/Canvas/Stage/Pages/Space/MapViewport/Rooms |
| Rooms | RectTransform | UIFlow/Canvas/Stage/Pages/Space/MapViewport/Rooms |
| Tooltip | Text | UIFlow/Canvas/Stage/Pages/Space/TooltipPanel/Tooltip |
| Details | Text | UIFlow/Canvas/Stage/Pages/Space/Details |
| Floor1 | Button | UIFlow/Canvas/Stage/Pages/Space/Floor1 |
| Floor1Label | Text | UIFlow/Canvas/Stage/Pages/Space/Floor1/Floor1Label |
| Floor2 | Button | UIFlow/Canvas/Stage/Pages/Space/Floor2 |
| Floor2Label | Text | UIFlow/Canvas/Stage/Pages/Space/Floor2/Floor2Label |
| Verify | Button | UIFlow/Canvas/Stage/Pages/Space/Verify |
| VerifyLabel | Text | UIFlow/Canvas/Stage/Pages/Space/Verify/VerifyLabel |
| TooltipPanel | RectTransform | UIFlow/Canvas/Stage/Pages/Space/TooltipPanel |
| ResetView | Button | UIFlow/Canvas/Stage/Pages/Space/ResetView |
| ResetViewLabel | Text | UIFlow/Canvas/Stage/Pages/Space/ResetView/Label |
| MapPan | V24MapPan | UIFlow/Canvas/Stage/Pages/Space/MapViewport |
| ZoomIn | Button | UIFlow/Canvas/Stage/Pages/Space/ZoomIn |
| ZoomOut | Button | UIFlow/Canvas/Stage/Pages/Space/ZoomOut |
| Hypothesis | InputField | UIFlow/Canvas/Stage/Pages/Space/Hypothesis |
| DoorOpen | Button | UIFlow/Canvas/Stage/Pages/Space/DoorOpen |
| DoorClosed | Button | UIFlow/Canvas/Stage/Pages/Space/DoorClosed |

## Timeline

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Heading | Text | UIFlow/Canvas/Stage/Pages/Timeline/Heading |
| SortRoot | RectTransform | UIFlow/Canvas/Stage/Pages/Timeline/SortRoot |
| Clues | RectTransform | UIFlow/Canvas/Stage/Pages/Timeline/SortRoot/CluesScroll/Viewport/Clues |
| CluesScroll | ScrollRect | UIFlow/Canvas/Stage/Pages/Timeline/SortRoot/CluesScroll |
| Slots | RectTransform | UIFlow/Canvas/Stage/Pages/Timeline/SortRoot/Slots |
| Hint | Text | UIFlow/Canvas/Stage/Pages/Timeline/SortRoot/Hint |
| Submit | Button | UIFlow/Canvas/Stage/Pages/Timeline/SortRoot/Submit |
| SubmitLabel | Text | UIFlow/Canvas/Stage/Pages/Timeline/SortRoot/Submit/SubmitLabel |
| RevealRoot | RectTransform | UIFlow/Canvas/Stage/Pages/Timeline/RevealRoot |
| Wave | RectTransform | UIFlow/Canvas/Stage/Pages/Timeline/RevealRoot/Wave |
| EndingStatus | Text | UIFlow/Canvas/Stage/Pages/Timeline/RevealRoot/EndingStatus |
| EndingText | Text | UIFlow/Canvas/Stage/Pages/Timeline/RevealRoot/EndingText |
| Progress | Slider | UIFlow/Canvas/Stage/Pages/Timeline/RevealRoot/Progress |
| Play | Button | UIFlow/Canvas/Stage/Pages/Timeline/RevealRoot/Play |
| PlayLabel | Text | UIFlow/Canvas/Stage/Pages/Timeline/RevealRoot/Play/PlayLabel |
| Replay | Button | UIFlow/Canvas/Stage/Pages/Timeline/RevealRoot/Replay |
| ReplayLabel | Text | UIFlow/Canvas/Stage/Pages/Timeline/RevealRoot/Replay/ReplayLabel |
| Requirements | Text | UIFlow/Canvas/Stage/Pages/Timeline/Requirements |
| Heroine | Image | UIFlow/Canvas/Stage/Pages/Timeline/RevealRoot/Heroine |
| Partner | Image | UIFlow/Canvas/Stage/Pages/Timeline/RevealRoot/Partner |
| SoundWave | V24EndingWave | UIFlow/Canvas/Stage/Pages/Timeline/RevealRoot/SoundWave |

## Tape

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Recordings | Button | UIFlow/Canvas/Stage/Pages/Tape/Recordings |
| RecordingsLabel | Text | UIFlow/Canvas/Stage/Pages/Tape/Recordings/RecordingsLabel |
| Anchors | Button | UIFlow/Canvas/Stage/Pages/Tape/Anchors |
| AnchorsLabel | Text | UIFlow/Canvas/Stage/Pages/Tape/Anchors/AnchorsLabel |
| Space | Button | UIFlow/Canvas/Stage/Pages/Tape/Space |
| SpaceLabel | Text | UIFlow/Canvas/Stage/Pages/Tape/Space/SpaceLabel |
| PreviousPuzzle | Button | UIFlow/Canvas/Stage/Pages/Tape/PreviousPuzzle |
| PreviousPuzzleLabel | Text | UIFlow/Canvas/Stage/Pages/Tape/PreviousPuzzle/PreviousPuzzleLabel |
| PuzzleName | Text | UIFlow/Canvas/Stage/Pages/Tape/PuzzleName |
| NextPuzzle | Button | UIFlow/Canvas/Stage/Pages/Tape/NextPuzzle |
| NextPuzzleLabel | Text | UIFlow/Canvas/Stage/Pages/Tape/NextPuzzle/NextPuzzleLabel |
| List | RectTransform | UIFlow/Canvas/Stage/Pages/Tape/ListScroll/Viewport/List |
| ListScroll | ScrollRect | UIFlow/Canvas/Stage/Pages/Tape/ListScroll |
| FileInfo | Text | UIFlow/Canvas/Stage/Pages/Tape/FileInfo |
| Lines | RectTransform | UIFlow/Canvas/Stage/Pages/Tape/LinesScroll/Viewport/Lines |
| LinesScroll | ScrollRect | UIFlow/Canvas/Stage/Pages/Tape/LinesScroll |
| Playback | Text | UIFlow/Canvas/Stage/Pages/Tape/Playback |
| Play | Button | UIFlow/Canvas/Stage/Pages/Tape/Play |
| PlayLabel | Text | UIFlow/Canvas/Stage/Pages/Tape/Play/PlayLabel |
| Progress | Slider | UIFlow/Canvas/Stage/Pages/Tape/Progress |
| Done | Button | UIFlow/Canvas/Stage/Pages/Tape/Done |
| DoneLabel | Text | UIFlow/Canvas/Stage/Pages/Tape/Done/DoneLabel |
| Extract | Button | UIFlow/Canvas/Stage/Pages/Tape/Extract |
| ExtractLabel | Text | UIFlow/Canvas/Stage/Pages/Tape/Extract/Label |

## Cutscene

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Surface | Button | UIFlow/Canvas/Stage/Pages/Cutscene |
| Body | Text | UIFlow/Canvas/Stage/Pages/Cutscene/Body |

## EndCard

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Surface | Button | UIFlow/Canvas/Stage/Pages/EndCard |
| Body | Text | UIFlow/Canvas/Stage/Pages/EndCard/Body |

## Map

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Heading | Text | UIFlow/Canvas/Stage/Overlays/Map/Heading |
| Close | Button | UIFlow/Canvas/Stage/Overlays/Map/Close |
| CloseLabel | Text | UIFlow/Canvas/Stage/Overlays/Map/Close/CloseLabel |
| FloorImage | RawImage | UIFlow/Canvas/Stage/Overlays/Map/Rooms |
| Rooms | RectTransform | UIFlow/Canvas/Stage/Overlays/Map/Rooms |
| Tooltip | Text | UIFlow/Canvas/Stage/Overlays/Map/TooltipPanel/Tooltip |
| Floor1 | Button | UIFlow/Canvas/Stage/Overlays/Map/Floor1 |
| Floor1Label | Text | UIFlow/Canvas/Stage/Overlays/Map/Floor1/Floor1Label |
| Floor2 | Button | UIFlow/Canvas/Stage/Overlays/Map/Floor2 |
| Floor2Label | Text | UIFlow/Canvas/Stage/Overlays/Map/Floor2/Floor2Label |
| Details | Text | UIFlow/Canvas/Stage/Overlays/Map/Details |
| Hint | Text | UIFlow/Canvas/Stage/Overlays/Map/Hint |
| Enter | Button | UIFlow/Canvas/Stage/Overlays/Map/Enter |
| EnterLabel | Text | UIFlow/Canvas/Stage/Overlays/Map/Enter/EnterLabel |
| TooltipPanel | RectTransform | UIFlow/Canvas/Stage/Overlays/Map/TooltipPanel |

## Journal

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Close | Button | UIFlow/Canvas/Stage/Overlays/Journal/Close |
| CloseLabel | Text | UIFlow/Canvas/Stage/Overlays/Journal/Close/CloseLabel |
| Tab0 | Button | UIFlow/Canvas/Stage/Overlays/Journal/Tab0 |
| Tab0Label | Text | UIFlow/Canvas/Stage/Overlays/Journal/Tab0/Tab0Label |
| Tab1 | Button | UIFlow/Canvas/Stage/Overlays/Journal/Tab1 |
| Tab1Label | Text | UIFlow/Canvas/Stage/Overlays/Journal/Tab1/Tab1Label |
| Tab2 | Button | UIFlow/Canvas/Stage/Overlays/Journal/Tab2 |
| Tab2Label | Text | UIFlow/Canvas/Stage/Overlays/Journal/Tab2/Tab2Label |
| Category | Text | UIFlow/Canvas/Stage/Overlays/Journal/Category |
| PageNumber | Text | UIFlow/Canvas/Stage/Overlays/Journal/PageNumber |
| Empty | Text | UIFlow/Canvas/Stage/Overlays/Journal/Empty |
| LeftCards | RectTransform | UIFlow/Canvas/Stage/Overlays/Journal/LeftCards |
| RightCards | RectTransform | UIFlow/Canvas/Stage/Overlays/Journal/RightCards |
| Details | RectTransform | UIFlow/Canvas/Stage/Overlays/Journal/Details |
| DetailTitle | Text | UIFlow/Canvas/Stage/Overlays/Journal/Details/DetailTitle |
| DetailBody | Text | UIFlow/Canvas/Stage/Overlays/Journal/Details/DetailScroll/DetailBody |
| Previous | Button | UIFlow/Canvas/Stage/Overlays/Journal/Previous |
| PreviousLabel | Text | UIFlow/Canvas/Stage/Overlays/Journal/Previous/PreviousLabel |
| Next | Button | UIFlow/Canvas/Stage/Overlays/Journal/Next |
| NextLabel | Text | UIFlow/Canvas/Stage/Overlays/Journal/Next/NextLabel |
| Timeline | Button | UIFlow/Canvas/Stage/Overlays/Journal/Timeline |
| TimelineLabel | Text | UIFlow/Canvas/Stage/Overlays/Journal/Timeline/TimelineLabel |
| FlipSheet | RectTransform | UIFlow/Canvas/Stage/Overlays/Journal/FlipSheet |
| CharacterPath | Button | UIFlow/Canvas/Stage/Overlays/Journal/CharacterPath |
| CharacterPathLabel | Text | UIFlow/Canvas/Stage/Overlays/Journal/CharacterPath/Label |
| CharacterDecoration | RectTransform | UIFlow/Canvas/Stage/Overlays/Journal/Details/CharacterDecoration |
| DetailHead | RawImage | UIFlow/Canvas/Stage/Overlays/Journal/Details/CharacterDecoration/Head |
| RecordDecoration | RectTransform | UIFlow/Canvas/Stage/Overlays/Journal/Details/RecordDecoration |
| DetailScroll | ScrollRect | UIFlow/Canvas/Stage/Overlays/Journal/Details/DetailScroll |

## Acquire

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Heading | Text | UIFlow/Canvas/Stage/Overlays/Acquire/Heading |
| Icon | Text | UIFlow/Canvas/Stage/Overlays/Acquire/Icon |
| Name | Text | UIFlow/Canvas/Stage/Overlays/Acquire/Name |
| Body | Text | UIFlow/Canvas/Stage/Overlays/Acquire/Body |
| Filed | Text | UIFlow/Canvas/Stage/Overlays/Acquire/Filed |
| Confirm | Button | UIFlow/Canvas/Stage/Overlays/Acquire/Confirm |
| ConfirmLabel | Text | UIFlow/Canvas/Stage/Overlays/Acquire/Confirm/ConfirmLabel |
| ItemArt | RawImage | UIFlow/Canvas/Stage/Overlays/Acquire/IconBack/ItemArt |

## Letter

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Body | Text | UIFlow/Canvas/Stage/Overlays/Letter/Body |
| Close | Button | UIFlow/Canvas/Stage/Overlays/Letter/Close |
| CloseLabel | Text | UIFlow/Canvas/Stage/Overlays/Letter/Close/CloseLabel |
| ItemArt | RawImage | UIFlow/Canvas/Stage/Overlays/Letter/ItemArt |

## Settings

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Heading | Text | UIFlow/Canvas/Stage/Overlays/Settings/Heading |
| SpeedLabel | Text | UIFlow/Canvas/Stage/Overlays/Settings/SpeedLabel |
| Speed | Slider | UIFlow/Canvas/Stage/Overlays/Settings/Speed |
| SpeedValue | Text | UIFlow/Canvas/Stage/Overlays/Settings/SpeedValue |
| VolumeLabel | Text | UIFlow/Canvas/Stage/Overlays/Settings/VolumeLabel |
| Volume | Slider | UIFlow/Canvas/Stage/Overlays/Settings/Volume |
| Motion | Button | UIFlow/Canvas/Stage/Overlays/Settings/Motion |
| MotionLabel | Text | UIFlow/Canvas/Stage/Overlays/Settings/Motion/MotionLabel |
| Title | Button | UIFlow/Canvas/Stage/Overlays/Settings/Title |
| TitleLabel | Text | UIFlow/Canvas/Stage/Overlays/Settings/Title/TitleLabel |
| Close | Button | UIFlow/Canvas/Stage/Overlays/Settings/Close |
| CloseLabel | Text | UIFlow/Canvas/Stage/Overlays/Settings/Close/CloseLabel |
| SaveGame | Button | UIFlow/Canvas/Stage/Overlays/Settings/SaveGame |
| SaveGameLabel | Text | UIFlow/Canvas/Stage/Overlays/Settings/SaveGame/Label |
| LoadGame | Button | UIFlow/Canvas/Stage/Overlays/Settings/LoadGame |
| LoadGameLabel | Text | UIFlow/Canvas/Stage/Overlays/Settings/LoadGame/Label |
| EffectsVolume | Slider | UIFlow/Canvas/Stage/Overlays/Settings/SoundEffect |
| ExitGame | Button | UIFlow/Canvas/Stage/Overlays/Settings/ExitGame |

## Confirm

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Body | Text | UIFlow/Canvas/Stage/Overlays/Confirm/Body |
| Cancel | Button | UIFlow/Canvas/Stage/Overlays/Confirm/Cancel |
| CancelLabel | Text | UIFlow/Canvas/Stage/Overlays/Confirm/Cancel/CancelLabel |
| Confirm | Button | UIFlow/Canvas/Stage/Overlays/Confirm/Confirm |
| ConfirmLabel | Text | UIFlow/Canvas/Stage/Overlays/Confirm/Confirm/ConfirmLabel |

## Path

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Recordings | Button | UIFlow/Canvas/Stage/Overlays/Path/Recordings |
| RecordingsLabel | Text | UIFlow/Canvas/Stage/Overlays/Path/Recordings/RecordingsLabel |
| Anchors | Button | UIFlow/Canvas/Stage/Overlays/Path/Anchors |
| AnchorsLabel | Text | UIFlow/Canvas/Stage/Overlays/Path/Anchors/AnchorsLabel |
| Space | Button | UIFlow/Canvas/Stage/Overlays/Path/Space |
| SpaceLabel | Text | UIFlow/Canvas/Stage/Overlays/Path/Space/SpaceLabel |
| PreviousPuzzle | Button | UIFlow/Canvas/Stage/Overlays/Path/PreviousPuzzle |
| PreviousPuzzleLabel | Text | UIFlow/Canvas/Stage/Overlays/Path/PreviousPuzzle/PreviousPuzzleLabel |
| PuzzleName | Text | UIFlow/Canvas/Stage/Overlays/Path/PuzzleName |
| NextPuzzle | Button | UIFlow/Canvas/Stage/Overlays/Path/NextPuzzle |
| NextPuzzleLabel | Text | UIFlow/Canvas/Stage/Overlays/Path/NextPuzzle/NextPuzzleLabel |
| List | RectTransform | UIFlow/Canvas/Stage/Overlays/Path/ListScroll/Viewport/List |
| ListScroll | ScrollRect | UIFlow/Canvas/Stage/Overlays/Path/ListScroll |
| FloorImage | RawImage | UIFlow/Canvas/Stage/Overlays/Path/MapViewport/Rooms |
| Rooms | RectTransform | UIFlow/Canvas/Stage/Overlays/Path/MapViewport/Rooms |
| Tooltip | Text | UIFlow/Canvas/Stage/Overlays/Path/TooltipPanel/Tooltip |
| Details | Text | UIFlow/Canvas/Stage/Overlays/Path/Details |
| Floor1 | Button | UIFlow/Canvas/Stage/Overlays/Path/Floor1 |
| Floor1Label | Text | UIFlow/Canvas/Stage/Overlays/Path/Floor1/Floor1Label |
| Floor2 | Button | UIFlow/Canvas/Stage/Overlays/Path/Floor2 |
| Floor2Label | Text | UIFlow/Canvas/Stage/Overlays/Path/Floor2/Floor2Label |
| Verify | Button | UIFlow/Canvas/Stage/Overlays/Path/Verify |
| VerifyLabel | Text | UIFlow/Canvas/Stage/Overlays/Path/Verify/VerifyLabel |
| TooltipPanel | RectTransform | UIFlow/Canvas/Stage/Overlays/Path/TooltipPanel |
| Close | Button | UIFlow/Canvas/Stage/Overlays/Path/Close |
| CloseLabel | Text | UIFlow/Canvas/Stage/Overlays/Path/Close/Label |
| ResetView | Button | UIFlow/Canvas/Stage/Overlays/Path/ResetView |
| ResetViewLabel | Text | UIFlow/Canvas/Stage/Overlays/Path/ResetView/Label |
| MapPan | V24MapPan | UIFlow/Canvas/Stage/Overlays/Path/MapViewport |
| ZoomIn | Button | UIFlow/Canvas/Stage/Overlays/Path/ZoomIn |
| ZoomOut | Button | UIFlow/Canvas/Stage/Overlays/Path/ZoomOut |
| Hypothesis | InputField | UIFlow/Canvas/Stage/Overlays/Path/Hypothesis |

## Burn

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Heading | Text | UIFlow/Canvas/Stage/Overlays/Burn/Heading |
| Close | Button | UIFlow/Canvas/Stage/Overlays/Burn/Close |
| CloseLabel | Text | UIFlow/Canvas/Stage/Overlays/Burn/Close/CloseLabel |
| FloorImage | RawImage | UIFlow/Canvas/Stage/Overlays/Burn/FloorImage |
| Rooms | RectTransform | UIFlow/Canvas/Stage/Overlays/Burn/Rooms |
| Tooltip | Text | UIFlow/Canvas/Stage/Overlays/Burn/TooltipPanel/Tooltip |
| Floor1 | Button | UIFlow/Canvas/Stage/Overlays/Burn/Floor1 |
| Floor1Label | Text | UIFlow/Canvas/Stage/Overlays/Burn/Floor1/Floor1Label |
| Floor2 | Button | UIFlow/Canvas/Stage/Overlays/Burn/Floor2 |
| Floor2Label | Text | UIFlow/Canvas/Stage/Overlays/Burn/Floor2/Floor2Label |
| Details | Text | UIFlow/Canvas/Stage/Overlays/Burn/Details |
| Hint | Text | UIFlow/Canvas/Stage/Overlays/Burn/Hint |
| Enter | Button | UIFlow/Canvas/Stage/Overlays/Burn/Enter |
| EnterLabel | Text | UIFlow/Canvas/Stage/Overlays/Burn/Enter/EnterLabel |
| TooltipPanel | RectTransform | UIFlow/Canvas/Stage/Overlays/Burn/TooltipPanel |
| Floor3 | Button | UIFlow/Canvas/Stage/Overlays/Burn/Floor3 |
| Floor3Label | Text | UIFlow/Canvas/Stage/Overlays/Burn/Floor3/Label |
| List | RectTransform | UIFlow/Canvas/Stage/Overlays/Burn/FilesScroll/Viewport/List |
| Burn | Button | UIFlow/Canvas/Stage/Overlays/Burn/Burn |
| BurnLabel | Text | UIFlow/Canvas/Stage/Overlays/Burn/Burn/Label |

## TapeSelect

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Heading | Text | UIFlow/Canvas/Stage/Overlays/TapeSelect/Heading |
| Close | Button | UIFlow/Canvas/Stage/Overlays/TapeSelect/Close |
| CloseLabel | Text | UIFlow/Canvas/Stage/Overlays/TapeSelect/Close/CloseLabel |
| FloorImage | RawImage | UIFlow/Canvas/Stage/Overlays/TapeSelect/FloorImage |
| Rooms | RectTransform | UIFlow/Canvas/Stage/Overlays/TapeSelect/Rooms |
| Tooltip | Text | UIFlow/Canvas/Stage/Overlays/TapeSelect/TooltipPanel/Tooltip |
| Floor1 | Button | UIFlow/Canvas/Stage/Overlays/TapeSelect/Floor1 |
| Floor1Label | Text | UIFlow/Canvas/Stage/Overlays/TapeSelect/Floor1/Floor1Label |
| Floor2 | Button | UIFlow/Canvas/Stage/Overlays/TapeSelect/Floor2 |
| Floor2Label | Text | UIFlow/Canvas/Stage/Overlays/TapeSelect/Floor2/Floor2Label |
| Details | Text | UIFlow/Canvas/Stage/Overlays/TapeSelect/Details |
| Hint | Text | UIFlow/Canvas/Stage/Overlays/TapeSelect/Hint |
| Enter | Button | UIFlow/Canvas/Stage/Overlays/TapeSelect/Enter |
| EnterLabel | Text | UIFlow/Canvas/Stage/Overlays/TapeSelect/Enter/EnterLabel |
| TooltipPanel | RectTransform | UIFlow/Canvas/Stage/Overlays/TapeSelect/TooltipPanel |
| Floor3 | Button | UIFlow/Canvas/Stage/Overlays/TapeSelect/Floor3 |
| Floor3Label | Text | UIFlow/Canvas/Stage/Overlays/TapeSelect/Floor3/Label |
| List | RectTransform | UIFlow/Canvas/Stage/Overlays/TapeSelect/FilesScroll/Viewport/List |
| Burn | Button | UIFlow/Canvas/Stage/Overlays/TapeSelect/Burn |
| BurnLabel | Text | UIFlow/Canvas/Stage/Overlays/TapeSelect/Burn/Label |

## Credits

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Close | Button | UIFlow/Canvas/Stage/Overlays/Credits/Close |

## Hud

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Location | Text | UIFlow/Canvas/Stage/Hud/bg_location/Location |
| Back | Button | UIFlow/Canvas/Stage/Hud/Back |
| BackLabel | Text | UIFlow/Canvas/Stage/Hud/Back/BackLabel |
| Map | Button | UIFlow/Canvas/Stage/Hud/Map |
| MapLabel | Text | UIFlow/Canvas/Stage/Hud/Map/MapLabel |
| Journal | Button | UIFlow/Canvas/Stage/Hud/Journal |
| JournalLabel | Text | UIFlow/Canvas/Stage/Hud/Journal/JournalLabel |
| Settings | Button | UIFlow/Canvas/Stage/Hud/Settings |
| SettingsLabel | Text | UIFlow/Canvas/Stage/Hud/Settings/SettingsLabel |
| CurrentTime | Text | UIFlow/Canvas/Stage/Hud/bg_location/CurrentTime |
| LocationGroup | RectTransform | UIFlow/Canvas/Stage/Hud/bg_location |

## Toast

| 控件 ID | 组件 | 对象路径 |
|---|---|---|
| Body | Text | UIFlow/Canvas/Stage/Toast/Body |

## 列表模板

FlowCard：Button、Image、CastleUiView、FlowDrag、FlowPointer、CanvasGroup。FlowTrack：参考／移除按钮及偏移 Slider。DialogueLine：TMP、FlowTextLink、V24LineHit。PathLine：Image。RecordingFolder：可展开设备文件夹。RecordingFile：缩进文件与状态图标。RecordingLine：整条 Button、时间、说话人、正文、标记菱形与分隔线。模板资源均可直接编辑。
