# 画像ビューアへの画像スコア表示機能

このディレクトリで管理しているdotnetアプリのAvaloniaPictureViewerに、開いている画像のスコアリング機能を実装します。スコア計算は`experimental/scoring-tool` のworkerモードを使います。また、スコア計算には時間がかかるので、ファイル名と計算済みのスコアの辞書をメモリに持っておき、起動中に一度計算したスコアは再利用してください。

## 実装方針

### 概要

- AvaloniaPictureViewerに、現在表示中画像の美的スコアを表示する機能を追加する。
- スコア表示はタイトルではなく、画像表示領域の下部バーに出す。
- スコア計算は`experimental/scoring-tool`のworkerモードを常駐起動して利用する。
- アプリ起動中のみ、画像の絶対パスと計算済みスコアの辞書をメモリに保持し、同じ画像では再計算しない。

### Avalonia側の変更

- `ViewModel`にスコア表示用の状態を追加する。
  - 未選択
  - 読み込み中
  - スコア表示
  - エラー表示
- 画像パスが更新されたとき、まずメモリキャッシュを確認する。
- キャッシュがない場合は非同期でworkerにスコア計算を依頼する。
- 画像移動中に古いworker応答が返ってきても、現在表示中の画像へ誤って反映しないようにする。

### worker連携

- `experimental/scoring-tool`配下で`uv run score-photo --worker`を起動する。
- worker起動時に`--local-files-only`は付けず、モデル未キャッシュ時の自動取得を許可する。
- stdin/stdoutのJSON Linesでリクエストとレスポンスを送受信する。
- worker起動失敗、モデルロード失敗、スコア計算失敗はアプリを落とさず、下部バーに短いエラーとして表示する。
- アプリ終了時は可能なら`{"type":"shutdown"}`を送信し、失敗時はプロセス破棄で終了する。

### UI

- `MainWindow.xaml`に下部バーを追加し、スコア状態を表示する。
- 画像表示は現在の`Stretch="Uniform"`を維持する。
- 下部バーは画像閲覧を邪魔しない高さと控えめな配色にする。
- GUI変更後は開発者にデモ画面を見せて承認を得る。

### テスト

- macで`dotnet build AvaloniaPictureViewer.sln`を実行し、ビルド成功を確認する。
- `uv run score-photo --worker`のJSON Lines契約を確認する。
- アプリ上で以下を確認する。
  - 初回スコア計算中の表示
  - スコア表示
  - 画像移動時のスコア更新
  - 再表示時のキャッシュ利用
  - workerエラー時の表示

### コミット方針

- 変更は随時コミットする。
- 各コミット前にmacで`dotnet build AvaloniaPictureViewer.sln`を通す。
- コミット時はAgentによるコミットだと分かるようにGit authorを設定する。
