# Udon Knucklebones

[![Build Release](https://github.com/wipeseals/UdonKnucklebones/actions/workflows/release.yml/badge.svg)](https://github.com/wipeseals/UdonKnucklebones/actions/workflows/release.yml) [![Build Repo Listing](https://github.com/wipeseals/UdonKnucklebones/actions/workflows/build-listing.yml/badge.svg)](https://github.com/wipeseals/UdonKnucklebones/actions/workflows/build-listing.yml)

![banner](Docs~/banner.png)

VRChat で遊べる運と駆け引きのゲーム。

- 簡単操作のテーブルゲーム
- VCC (VRChat Creator Companion) 追加 & Prefab 追加で導入完了
- Player/CPU 対戦可能
- UdonChips あり版/なし版それぞれ対応
- ゲーム説明用のボード・画像を同梱

## 遊び方

- 交互にサイコロを振り、3 列のうち空いている列にサイコロを配置する
- 以下のルールに従いサイコロを取り除く
- どちらかがサイコロを置けなくなった時点でゲーム終了となり、得点の高いほうが勝利

![manual3](/Packages/me.wipeseals.udon-knucklebones/Runtime/Textures/Manual/UdonKnucklebones-Manual-3.png)

## 導入

### 1. 前準備

以下 URL より、VCC に UdonKnucklebones を追加する。
<https://wipeseals.github.io/UdonKnucklebones/>

自分のワールドプロジェクトに VCC から UdonKnucklebones を追加する。
![vcc](Docs~/screenshot/add-vcc.png)

### 2.A. Prefab 追加 (UdonChips 非対応版)

`Packages/Udon Knucklebones/Runtime/Prefabs` に UdonKnucklebones の Prefab があるので、これをシーン中に追加する。
![prefab](Docs~/screenshot/prefab-locate.png)

![add scene](Docs~/screenshot/add-scene.png)

### 2.B. Prefab 追加 (UdonChips 対応版)

#### 注意

2.B. は事前に UdonChips の導入を済ませ、Scene 中に UdonChips の Object がある前提の手順です。

UdonChips 未導入の場合は **実施しないように** お願いします。実施してしまった場合は `Assets/UdonKnucklebonesSupportUdonChips` の手動削除で対処可能です。

#### 手順

`Packages/Udon Knucklebones/Runtime/UnityPackages` に UdonKnucklebones(UdonChips) 導入用の UnityPackage があるので、これをダブルクリックして導入します。

![package](Docs~/screenshot/uc-unitypackage-locate.png)

以下ファイルが `Assets/UdonKnucklebonesSupportUdonChips` に追加されるので Import を押す。

![import](Docs~/screenshot/uc-unitypackage-import.png)

`Assets/UdonKnucklebonesSupportUdonChips/Prefabs` に UdonChips 対応版の UdonKnucklebones の Prefab があるので、これを Scene に追加。

![prefab](Docs~/screenshot/uc-prefab-locate.png)

![add scene](Docs~/screenshot/uc-add-scene.png)

### セットアップ

Prefab ポン置きで動作するので特別作業はないですが、以下の設定が可能です。

| 設定名                  | 内容                                                                                                                                |
| ----------------------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| Is Debug                | true の場合、Debug.Log で動作状況が出力されます                                                                                     |
| Udon Chips Player Rate  | UdonChips 対応版の Player 戦で、勝敗の点差に応じて移動する UdonChips のレート (例えば、100 に設定している場合、1 点差=100UdonChips) |
| Udon Chips Cpu Rate     | 上記レートの CPU 戦版                                                                                                               |
| Think Time For Cpu      | CPU の思考時間平均。キビキビ動作させたい場合は減らす                                                                                |
| Polling Sec For Rolling | サイコロを転がす際の監視間隔。通常は変更不要                                                                                        |
| Is Column Index Crossed | Player1 の列と Player2 の列の番号が交差している場合は true。通常は変更不要                                                          |
| Dice Roll Force Range   | サイコロを振るときの強さ最大値                                                                                                      |
| Dice Roll Timeout Sec   | サイコロの目を決定するときのタイムアウト                                                                                            |
| Dice Value For XXXX     | サイコロの面と目の同期用。通常は変更不要                                                                                            |

![config](Docs~/screenshot/config-need.png)

### トラブルシューティング

#### コンパイルエラーになる

エラーの内容確認をお願いします。
![trouble](Docs~/screenshot/error-compile.png)

もし UdonChips 対応不要なのに誤って導入してしまった場合、導入したが UdonChips が準備できていない場合は

![trouble](Docs~/screenshot/error-uc-not-import.png)
