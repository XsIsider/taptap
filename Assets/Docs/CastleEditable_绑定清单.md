# CastleEditable 实际场景绑定清单

由创建场景工具导出，对应已保存的原生 uGUI 场景。修改布局不需要修改 Id，也不需要修改脚本。

`CastleGame` 挂 CastleGame、AudioSource；`Canvas` 挂 Canvas、CanvasScaler、GraphicRaycaster、CastleSceneUi；`EventSystem` 挂 EventSystem、StandaloneInputModule。

## Title

面板脚本：`CastleUiView`。其 Controls 列表使用以下 Id 与组件引用。

| Id | 绑定组件 | 相对面板的对象路径 |
|---|---|---|
|Background|RawImage|Background|
|StartButton|Button|StartButton|
|StartButtonLabel|Text|StartButton/StartButtonLabel|
|ContinueButton|Button|ContinueButton|
|ContinueButtonLabel|Text|ContinueButton/ContinueButtonLabel|
|SettingsButton|Button|SettingsButton|
|SettingsButtonLabel|Text|SettingsButton/SettingsButtonLabel|
|CreditsButton|Button|CreditsButton|
|CreditsButtonLabel|Text|CreditsButton/CreditsButtonLabel|
|ExitButton|Button|ExitButton|
|ExitButtonLabel|Text|ExitButton/ExitButtonLabel|

## Outside

面板脚本：`CastleUiView`。其 Controls 列表使用以下 Id 与组件引用。

| Id | 绑定组件 | 相对面板的对象路径 |
|---|---|---|
|Background|RawImage|Background|
|OpenInvitationButton|Button|OpenInvitationButton|
|OpenInvitationButtonLabel|Text|OpenInvitationButton/OpenInvitationButtonLabel|
|EnterButton|Button|EnterButton|
|EnterButtonLabel|Text|EnterButton/EnterButtonLabel|
|HintText|Text|HintText|

## Invitation

面板脚本：`CastleUiView`。其 Controls 列表使用以下 Id 与组件引用。

| Id | 绑定组件 | 相对面板的对象路径 |
|---|---|---|
|Background|RawImage|Background|
|CollectButton|Button|CollectButton|
|CollectButtonLabel|Text|CollectButton/CollectButtonLabel|

## Collected

面板脚本：`CastleUiView`。其 Controls 列表使用以下 Id 与组件引用。

| Id | 绑定组件 | 相对面板的对象路径 |
|---|---|---|
|Background|RawImage|Background|
|ConfirmButton|Button|ConfirmButton|
|ConfirmButtonLabel|Text|ConfirmButton/ConfirmButtonLabel|

## Lounge

面板脚本：`CastleUiView`。其 Controls 列表使用以下 Id 与组件引用。

| Id | 绑定组件 | 相对面板的对象路径 |
|---|---|---|
|Background|RawImage|Background|
|Line0|Text|Panel/Line0|
|Line1|Text|Panel/Line1|
|Line2|Text|Panel/Line2|
|ProgressText|Text|Panel/ProgressText|
|ContinueButton|Button|Panel/ContinueButton|
|ContinueButtonLabel|Text|Panel/ContinueButton/ContinueButtonLabel|
|MapButton|Button|Panel/MapButton|
|MapButtonLabel|Text|Panel/MapButton/MapButtonLabel|

## Room

面板脚本：`CastleUiView`。其 Controls 列表使用以下 Id 与组件引用。

| Id | 绑定组件 | 相对面板的对象路径 |
|---|---|---|
|Background|RawImage|Background|
|ControlRoot|RectTransform|ControlRoot|
|WorkbenchButton|Button|ControlRoot/WorkbenchButton|
|WorkbenchButtonLabel|Text|ControlRoot/WorkbenchButton/WorkbenchButtonLabel|
|OtherRoomRoot|RectTransform|OtherRoomRoot|
|RoomNameText|Text|OtherRoomRoot/Panel/RoomNameText|
|DescriptionText|Text|OtherRoomRoot/Panel/DescriptionText|
|MapButton|Button|OtherRoomRoot/Panel/MapButton|
|MapButtonLabel|Text|OtherRoomRoot/Panel/MapButton/MapButtonLabel|

## Browse

面板脚本：`CastleUiView`。其 Controls 列表使用以下 Id 与组件引用。

| Id | 绑定组件 | 相对面板的对象路径 |
|---|---|---|
|Background|RawImage|Background|
|BackButton|Button|BackButton|
|BackButtonLabel|Text|BackButton/BackButtonLabel|
|JournalButton|Button|JournalButton|
|JournalButtonLabel|Text|JournalButton/JournalButtonLabel|
|SettingsButton|Button|SettingsButton|
|SettingsButtonLabel|Text|SettingsButton/SettingsButtonLabel|
|BrowseButton|Button|BrowseButton|
|BrowseButtonLabel|Text|BrowseButton/BrowseButtonLabel|
|AlignButton|Button|AlignButton|
|AlignButtonLabel|Text|AlignButton/AlignButtonLabel|
|SoundButton|Button|SoundButton|
|SoundButtonLabel|Text|SoundButton/SoundButtonLabel|
|Rec01Button|Button|Rec01Button|
|Rec01ButtonLabel|Text|Rec01Button/Rec01ButtonLabel|
|Rec02Button|Button|Rec02Button|
|Rec02ButtonLabel|Text|Rec02Button/Rec02ButtonLabel|
|Rec03Button|Button|Rec03Button|
|Rec03ButtonLabel|Text|Rec03Button/Rec03ButtonLabel|
|PersonalButton|Button|PersonalButton|
|PersonalButtonLabel|Text|PersonalButton/PersonalButtonLabel|
|IdentityButton|Button|IdentityButton|
|IdentityButtonLabel|Text|IdentityButton/IdentityButtonLabel|
|FileInfoText|Text|FileInfoText|
|TranscriptRoot|RectTransform|TranscriptRoot|
|TranscriptScroll|ScrollRect|TranscriptRoot/TranscriptScroll|
|Row0|Button|TranscriptRoot/TranscriptScroll/Viewport/Content/Row0|
|Row0Label|Text|TranscriptRoot/TranscriptScroll/Viewport/Content/Row0/Row0Label|
|Excerpt0|Button|TranscriptRoot/TranscriptScroll/Viewport/Content/Excerpt0|
|Excerpt0Label|Text|TranscriptRoot/TranscriptScroll/Viewport/Content/Excerpt0/Excerpt0Label|
|Row1|Button|TranscriptRoot/TranscriptScroll/Viewport/Content/Row1|
|Row1Label|Text|TranscriptRoot/TranscriptScroll/Viewport/Content/Row1/Row1Label|
|AnchorButton|Button|TranscriptRoot/TranscriptScroll/Viewport/Content/AnchorButton|
|AnchorButtonLabel|Text|TranscriptRoot/TranscriptScroll/Viewport/Content/AnchorButton/AnchorButtonLabel|
|Row2|Button|TranscriptRoot/TranscriptScroll/Viewport/Content/Row2|
|Row2Label|Text|TranscriptRoot/TranscriptScroll/Viewport/Content/Row2/Row2Label|
|Excerpt2|Button|TranscriptRoot/TranscriptScroll/Viewport/Content/Excerpt2|
|Excerpt2Label|Text|TranscriptRoot/TranscriptScroll/Viewport/Content/Excerpt2/Excerpt2Label|
|Row3|Button|TranscriptRoot/TranscriptScroll/Viewport/Content/Row3|
|Row3Label|Text|TranscriptRoot/TranscriptScroll/Viewport/Content/Row3/Row3Label|
|Excerpt3|Button|TranscriptRoot/TranscriptScroll/Viewport/Content/Excerpt3|
|Excerpt3Label|Text|TranscriptRoot/TranscriptScroll/Viewport/Content/Excerpt3/Excerpt3Label|
|ConversationRoot|RectTransform|ConversationRoot|
|ConversationScroll|ScrollRect|ConversationRoot/ConversationScroll|
|ConversationText|Text|ConversationRoot/ConversationScroll/Viewport/Content/ConversationText|
|PlaybackRoot|RectTransform|PlaybackRoot|
|PlayButton|Button|PlaybackRoot/PlayButton|
|PlayButtonLabel|Text|PlaybackRoot/PlayButton/PlayButtonLabel|
|ProgressSlider|Slider|PlaybackRoot/ProgressSlider|
|PlaybackText|Text|PlaybackRoot/PlaybackText|
|MarkCompleteButton|Button|PlaybackRoot/MarkCompleteButton|
|MarkCompleteButtonLabel|Text|PlaybackRoot/MarkCompleteButton/MarkCompleteButtonLabel|

## Align

面板脚本：`CastleUiView`。其 Controls 列表使用以下 Id 与组件引用。

| Id | 绑定组件 | 相对面板的对象路径 |
|---|---|---|
|Background|RawImage|Background|
|BackButton|Button|BackButton|
|BackButtonLabel|Text|BackButton/BackButtonLabel|
|JournalButton|Button|JournalButton|
|JournalButtonLabel|Text|JournalButton/JournalButtonLabel|
|SettingsButton|Button|SettingsButton|
|SettingsButtonLabel|Text|SettingsButton/SettingsButtonLabel|
|BrowseButton|Button|BrowseButton|
|BrowseButtonLabel|Text|BrowseButton/BrowseButtonLabel|
|AlignButton|Button|AlignButton|
|AlignButtonLabel|Text|AlignButton/AlignButtonLabel|
|SoundButton|Button|SoundButton|
|SoundButtonLabel|Text|SoundButton/SoundButtonLabel|
|CandidateTitle|Text|CandidateTitle|
|AnchorAButton|Button|AnchorAButton|
|AnchorAButtonLabel|Text|AnchorAButton/AnchorAButtonLabel|
|AnchorBButton|Button|AnchorBButton|
|AnchorBButtonLabel|Text|AnchorBButton/AnchorBButtonLabel|
|BackToBrowseButton|Button|BackToBrowseButton|
|BackToBrowseButtonLabel|Text|BackToBrowseButton/BackToBrowseButtonLabel|
|AlignmentScroll|ScrollRect|AlignmentScroll|
|ReferenceRail|RectTransform|AlignmentScroll/Viewport/Content/ReferenceTrack/ReferenceRail|
|ReferenceMarker|RectTransform|AlignmentScroll/Viewport/Content/ReferenceTrack/ReferenceMarker|
|TargetRail|RectTransform|AlignmentScroll/Viewport/Content/TargetTrack/TargetRail|
|TargetMarker|RectTransform|AlignmentScroll/Viewport/Content/TargetTrack/TargetMarker|
|TrackDrag|CastleTimelineDrag|AlignmentScroll/Viewport/Content/TargetTrack|
|OffsetSlider|Slider|AlignmentScroll/Viewport/Content/OffsetSlider|
|OffsetText|Text|OffsetText|
|StatusText|Text|StatusText|
|CheckButton|Button|CheckButton|
|CheckButtonLabel|Text|CheckButton/CheckButtonLabel|
|NextButton|Button|NextButton|
|NextButtonLabel|Text|NextButton/NextButtonLabel|

## Sound

面板脚本：`CastleUiView`。其 Controls 列表使用以下 Id 与组件引用。

| Id | 绑定组件 | 相对面板的对象路径 |
|---|---|---|
|Background|RawImage|Background|
|BackButton|Button|BackButton|
|BackButtonLabel|Text|BackButton/BackButtonLabel|
|JournalButton|Button|JournalButton|
|JournalButtonLabel|Text|JournalButton/JournalButtonLabel|
|SettingsButton|Button|SettingsButton|
|SettingsButtonLabel|Text|SettingsButton/SettingsButtonLabel|
|BrowseButton|Button|BrowseButton|
|BrowseButtonLabel|Text|BrowseButton/BrowseButtonLabel|
|AlignButton|Button|AlignButton|
|AlignButtonLabel|Text|AlignButton/AlignButtonLabel|
|SoundButton|Button|SoundButton|
|SoundButtonLabel|Text|SoundButton/SoundButtonLabel|
|ListTitle|Text|ListTitle|
|Marker0|Button|Marker0|
|Marker0Label|Text|Marker0/Marker0Label|
|Marker1|Button|Marker1|
|Marker1Label|Text|Marker1/Marker1Label|
|Marker2|Button|Marker2|
|Marker2Label|Text|Marker2/Marker2Label|
|UndoButton|Button|UndoButton|
|UndoButtonLabel|Text|UndoButton/UndoButtonLabel|
|Floor1Button|Button|Floor1Button|
|Floor1ButtonLabel|Text|Floor1Button/Floor1ButtonLabel|
|Floor2Button|Button|Floor2Button|
|Floor2ButtonLabel|Text|Floor2Button/Floor2ButtonLabel|
|Map|CastleMapView|Map|
|InspectorTitle|Text|InspectorTitle|
|SourceInfoText|Text|SourceInfoText|
|Door0|Button|Door0|
|Door0Label|Text|Door0/Door0Label|
|Door1|Button|Door1|
|Door1Label|Text|Door1/Door1Label|
|Door2|Button|Door2|
|Door2Label|Text|Door2/Door2Label|
|EvidenceButton|Button|EvidenceButton|
|EvidenceButtonLabel|Text|EvidenceButton/EvidenceButtonLabel|
|DraftText|Text|DraftText|
|SaveDraftButton|Button|SaveDraftButton|
|SaveDraftButtonLabel|Text|SaveDraftButton/SaveDraftButtonLabel|
|VerifyButton|Button|VerifyButton|
|VerifyButtonLabel|Text|VerifyButton/VerifyButtonLabel|

## Event

面板脚本：`CastleUiView`。其 Controls 列表使用以下 Id 与组件引用。

| Id | 绑定组件 | 相对面板的对象路径 |
|---|---|---|
|Background|RawImage|Background|
|BackButton|Button|BackButton|
|BackButtonLabel|Text|BackButton/BackButtonLabel|
|JournalButton|Button|JournalButton|
|JournalButtonLabel|Text|JournalButton/JournalButtonLabel|
|SettingsButton|Button|SettingsButton|
|SettingsButtonLabel|Text|SettingsButton/SettingsButtonLabel|
|BrowseButton|Button|BrowseButton|
|BrowseButtonLabel|Text|BrowseButton/BrowseButtonLabel|
|AlignButton|Button|AlignButton|
|AlignButtonLabel|Text|AlignButton/AlignButtonLabel|
|SoundButton|Button|SoundButton|
|SoundButtonLabel|Text|SoundButton/SoundButtonLabel|
|RecordText|Text|Panel/RecordText|
|IdentityButton|Button|Panel/IdentityButton|
|IdentityButtonLabel|Text|Panel/IdentityButton/IdentityButtonLabel|
|RecordingButton|Button|RecordingButton|
|RecordingButtonLabel|Text|RecordingButton/RecordingButtonLabel|

## Identity

面板脚本：`CastleUiView`。其 Controls 列表使用以下 Id 与组件引用。

| Id | 绑定组件 | 相对面板的对象路径 |
|---|---|---|
|Background|RawImage|Background|
|BackButton|Button|BackButton|
|BackButtonLabel|Text|BackButton/BackButtonLabel|
|JournalButton|Button|JournalButton|
|JournalButtonLabel|Text|JournalButton/JournalButtonLabel|
|SettingsButton|Button|SettingsButton|
|SettingsButtonLabel|Text|SettingsButton/SettingsButtonLabel|
|BrowseButton|Button|BrowseButton|
|BrowseButtonLabel|Text|BrowseButton/BrowseButtonLabel|
|AlignButton|Button|AlignButton|
|AlignButtonLabel|Text|AlignButton/AlignButtonLabel|
|SoundButton|Button|SoundButton|
|SoundButtonLabel|Text|SoundButton/SoundButtonLabel|
|LinButton|Button|Panel/LinButton|
|LinButtonLabel|Text|Panel/LinButton/LinButtonLabel|
|ChenButton|Button|Panel/ChenButton|
|ChenButtonLabel|Text|Panel/ChenButton/ChenButtonLabel|
|EvidenceButton|Button|Panel/EvidenceButton|
|EvidenceButtonLabel|Text|Panel/EvidenceButton/EvidenceButtonLabel|
|StatusText|Text|Panel/StatusText|
|ConfirmButton|Button|ConfirmButton|
|ConfirmButtonLabel|Text|ConfirmButton/ConfirmButtonLabel|

## Route

面板脚本：`CastleUiView`。其 Controls 列表使用以下 Id 与组件引用。

| Id | 绑定组件 | 相对面板的对象路径 |
|---|---|---|
|Background|RawImage|Background|
|BackButton|Button|BackButton|
|BackButtonLabel|Text|BackButton/BackButtonLabel|
|JournalButton|Button|JournalButton|
|JournalButtonLabel|Text|JournalButton/JournalButtonLabel|
|SettingsButton|Button|SettingsButton|
|SettingsButtonLabel|Text|SettingsButton/SettingsButtonLabel|
|BrowseButton|Button|BrowseButton|
|BrowseButtonLabel|Text|BrowseButton/BrowseButtonLabel|
|AlignButton|Button|AlignButton|
|AlignButtonLabel|Text|AlignButton/AlignButtonLabel|
|SoundButton|Button|SoundButton|
|SoundButtonLabel|Text|SoundButton/SoundButtonLabel|
|Node0|Button|Panel/Node0|
|Node0Label|Text|Panel/Node0/Node0Label|
|Node1|Button|Panel/Node1|
|Node1Label|Text|Panel/Node1/Node1Label|
|Node2|Button|Panel/Node2|
|Node2Label|Text|Panel/Node2/Node2Label|
|UndoButton|Button|Panel/UndoButton|
|UndoButtonLabel|Text|Panel/UndoButton/UndoButtonLabel|
|Floor1Button|Button|Floor1Button|
|Floor1ButtonLabel|Text|Floor1Button/Floor1ButtonLabel|
|Floor2Button|Button|Floor2Button|
|Floor2ButtonLabel|Text|Floor2Button/Floor2ButtonLabel|
|Map|CastleMapView|Map|
|NodeText|Text|Panel/NodeText|
|EvidenceButton|Button|Panel/EvidenceButton|
|EvidenceButtonLabel|Text|Panel/EvidenceButton/EvidenceButtonLabel|
|StatusText|Text|Panel/StatusText|
|VerifyButton|Button|VerifyButton|
|VerifyButtonLabel|Text|VerifyButton/VerifyButtonLabel|
|NextButton|Button|NextButton|
|NextButtonLabel|Text|NextButton/NextButtonLabel|

## Final

面板脚本：`CastleUiView`。其 Controls 列表使用以下 Id 与组件引用。

| Id | 绑定组件 | 相对面板的对象路径 |
|---|---|---|
|Background|RawImage|Background|
|BackButton|Button|BackButton|
|BackButtonLabel|Text|BackButton/BackButtonLabel|
|JournalButton|Button|JournalButton|
|JournalButtonLabel|Text|JournalButton/JournalButtonLabel|
|SettingsButton|Button|SettingsButton|
|SettingsButtonLabel|Text|SettingsButton/SettingsButtonLabel|
|BrowseButton|Button|BrowseButton|
|BrowseButtonLabel|Text|BrowseButton/BrowseButtonLabel|
|AlignButton|Button|AlignButton|
|AlignButtonLabel|Text|AlignButton/AlignButtonLabel|
|SoundButton|Button|SoundButton|
|SoundButtonLabel|Text|SoundButton/SoundButtonLabel|
|RelationsText|Text|Panel/RelationsText|
|EvidenceButton|Button|Panel/EvidenceButton|
|EvidenceButtonLabel|Text|Panel/EvidenceButton/EvidenceButtonLabel|
|SubmitButton|Button|SubmitButton|
|SubmitButtonLabel|Text|SubmitButton/SubmitButtonLabel|

## Truth

面板脚本：`CastleUiView`。其 Controls 列表使用以下 Id 与组件引用。

| Id | 绑定组件 | 相对面板的对象路径 |
|---|---|---|
|Background|RawImage|Background|
|BackButton|Button|BackButton|
|BackButtonLabel|Text|BackButton/BackButtonLabel|
|JournalButton|Button|JournalButton|
|JournalButtonLabel|Text|JournalButton/JournalButtonLabel|
|SettingsButton|Button|SettingsButton|
|SettingsButtonLabel|Text|SettingsButton/SettingsButtonLabel|
|BrowseButton|Button|BrowseButton|
|BrowseButtonLabel|Text|BrowseButton/BrowseButtonLabel|
|AlignButton|Button|AlignButton|
|AlignButtonLabel|Text|AlignButton/AlignButtonLabel|
|SoundButton|Button|SoundButton|
|SoundButtonLabel|Text|SoundButton/SoundButtonLabel|
|IndexText|Text|Panel/IndexText|
|StoryText|Text|Panel/StoryText|
|NextButton|Button|NextButton|
|NextButtonLabel|Text|NextButton/NextButtonLabel|

## Choice

面板脚本：`CastleUiView`。其 Controls 列表使用以下 Id 与组件引用。

| Id | 绑定组件 | 相对面板的对象路径 |
|---|---|---|
|Background|RawImage|Background|
|PublicButton|Button|Panel/PublicButton|
|PublicButtonLabel|Text|Panel/PublicButton/PublicButtonLabel|
|ConfrontButton|Button|Panel/ConfrontButton|
|ConfrontButtonLabel|Text|Panel/ConfrontButton/ConfrontButtonLabel|
|JournalButton|Button|Panel/JournalButton|
|JournalButtonLabel|Text|Panel/JournalButton/JournalButtonLabel|
|BackButton|Button|Panel/BackButton|
|BackButtonLabel|Text|Panel/BackButton/BackButtonLabel|

## Ending

面板脚本：`CastleUiView`。其 Controls 列表使用以下 Id 与组件引用。

| Id | 绑定组件 | 相对面板的对象路径 |
|---|---|---|
|Background|RawImage|Background|
|EndingText|Text|Panel/EndingText|
|StoryText|Text|Panel/StoryText|
|JournalButton|Button|Panel/JournalButton|
|JournalButtonLabel|Text|Panel/JournalButton/JournalButtonLabel|
|BackButton|Button|Panel/BackButton|
|BackButtonLabel|Text|Panel/BackButton/BackButtonLabel|
|TitleButton|Button|Panel/TitleButton|
|TitleButtonLabel|Text|Panel/TitleButton/TitleButtonLabel|

## Navigation

面板脚本：`CastleUiView`。其 Controls 列表使用以下 Id 与组件引用。

| Id | 绑定组件 | 相对面板的对象路径 |
|---|---|---|
|Heading|Text|Heading|
|LocationText|Text|LocationText|
|Floor1Button|Button|Floor1Button|
|Floor1ButtonLabel|Text|Floor1Button/Floor1ButtonLabel|
|Floor2Button|Button|Floor2Button|
|Floor2ButtonLabel|Text|Floor2Button/Floor2ButtonLabel|
|Map|CastleMapView|Map|
|CloseButton|Button|CloseButton|
|CloseButtonLabel|Text|CloseButton/CloseButtonLabel|
|SelectionText|Text|SelectionText|
|WaitButton|Button|WaitButton|
|WaitButtonLabel|Text|WaitButton/WaitButtonLabel|
|EnterButton|Button|EnterButton|
|EnterButtonLabel|Text|EnterButton/EnterButtonLabel|

## Settings

面板脚本：`CastleUiView`。其 Controls 列表使用以下 Id 与组件引用。

| Id | 绑定组件 | 相对面板的对象路径 |
|---|---|---|
|SizeText|Text|Panel/SizeText|
|SizeSlider|Slider|Panel/SizeSlider|
|VolumeSlider|Slider|Panel/VolumeSlider|
|MotionButton|Button|Panel/MotionButton|
|MotionButtonLabel|Text|Panel/MotionButton/MotionButtonLabel|
|TitleButton|Button|Panel/TitleButton|
|TitleButtonLabel|Text|Panel/TitleButton/TitleButtonLabel|
|ContinueButton|Button|Panel/ContinueButton|
|ContinueButtonLabel|Text|Panel/ContinueButton/ContinueButtonLabel|

## Journal

面板脚本：`CastleUiView`。其 Controls 列表使用以下 Id 与组件引用。

| Id | 绑定组件 | 相对面板的对象路径 |
|---|---|---|
|CloseButton|Button|CloseButton|
|CloseButtonLabel|Text|CloseButton/CloseButtonLabel|
|PeopleButton|Button|PeopleButton|
|PeopleButtonLabel|Text|PeopleButton/PeopleButtonLabel|
|ItemsButton|Button|ItemsButton|
|ItemsButtonLabel|Text|ItemsButton/ItemsButtonLabel|
|EventsButton|Button|EventsButton|
|EventsButtonLabel|Text|EventsButton/EventsButtonLabel|
|CountText|Text|CountText|
|GridScroll|ScrollRect|Journal grid|
|EmptyText|Text|EmptyText|

## JournalDetail

面板脚本：`CastleUiView`。其 Controls 列表使用以下 Id 与组件引用。

| Id | 绑定组件 | 相对面板的对象路径 |
|---|---|---|
|BackButton|Button|BackButton|
|BackButtonLabel|Text|BackButton/BackButtonLabel|
|CloseButton|Button|CloseButton|
|CloseButtonLabel|Text|CloseButton/CloseButtonLabel|
|CategoryText|Text|CategoryText|
|TitleText|Text|TitleText|
|DetailScroll|ScrollRect|DetailScroll|
|BodyText|Text|DetailScroll/Viewport/Content/BodyText|
|ActionButton|Button|ActionButton|
|ActionButtonLabel|Text|ActionButton/ActionButtonLabel|

## InvitationArchive

面板脚本：`CastleUiView`。其 Controls 列表使用以下 Id 与组件引用。

| Id | 绑定组件 | 相对面板的对象路径 |
|---|---|---|
|Background|RawImage|Background|
|BackButton|Button|BackButton|
|BackButtonLabel|Text|BackButton/BackButtonLabel|

## Message

面板脚本：`CastleUiView`。其 Controls 列表使用以下 Id 与组件引用。

| Id | 绑定组件 | 相对面板的对象路径 |
|---|---|---|
|TitleText|Text|Panel/TitleText|
|BodyText|Text|Panel/BodyText|
|CancelButton|Button|Panel/CancelButton|
|CancelButtonLabel|Text|Panel/CancelButton/CancelButtonLabel|
|ConfirmButton|Button|Panel/ConfirmButton|
|ConfirmButtonLabel|Text|Panel/ConfirmButton/ConfirmButtonLabel|

## SceneHud

面板脚本：`CastleUiView`。其 Controls 列表使用以下 Id 与组件引用。

| Id | 绑定组件 | 相对面板的对象路径 |
|---|---|---|
|LocationText|Text|LocationText|
|ClockText|Text|ClockText|
|MapButton|Button|MapButton|
|MapButtonLabel|Text|MapButton/MapButtonLabel|
|JournalButton|Button|JournalButton|
|JournalButtonLabel|Text|JournalButton/JournalButtonLabel|
|SettingsButton|Button|SettingsButton|
|SettingsButtonLabel|Text|SettingsButton/SettingsButtonLabel|

## 地图和卡片

每个 Map 对象另挂 CastleMapView，Rooms 列表保存房间 ID、Button、Text、MarkerAnchor 和 Corridor 路点；Lines 与 Markers 已预置。移动按钮和路点即可修改地图布局。

JournalCard.prefab 根对象挂 Image、Button、CastleJournalCardView，三个子对象为 CategoryText、TitleText、SummaryText，均挂 Text。Journal/GridScroll 的 Content 挂 GridLayoutGroup 与 ContentSizeFitter。卡片数量根据当前记录实例化。
