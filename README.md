# StorageSorter

**English** | [日本語](#日本語)

Sort every box in your storage room with a single key press.

## English

### Features
- Press **F9** in-game and every storage shelf is sorted at once.
- Boxes are ordered by **product category → product ID**. For the same product, fuller boxes come first.
- Shelves are filled starting from the one closest to the storage reference point, which is the same priority employees use when they fetch boxes.
- **Merges partially filled boxes** of the same product, up to the per-box limit. The total number of items never changes.
- **Works with [OrderAndOrganize](https://thunderstore.io/c/supermarket-together/p/ddqminik/OrderAndOrganize/) (optional):** shelves you assigned a category to are filled with that category first.
- **Multiplayer safe:** changes go through the game's own server-side storage logic, so they sync to every player and are saved normally.

### How to use
1. Install with a mod manager, or put `StorageSorter.dll` in `BepInEx/plugins/`.
2. Load a store **as the host**. This includes single-player.
3. Press **F9** while walking around, not while a menu is open.

A message on screen tells you how many slots were updated.

### Sorting rules
1. All boxes from all storage shelves are collected and sorted by category, then by product ID.
2. If OrderAndOrganize is installed, shelves with a category get boxes of that category first. Anything that doesn't fit goes to shelves without a category, and only then to shelves of other categories.
3. With `MergeBoxes = true`, partial boxes of the same product are combined. A box that ends up empty this way disappears.
4. Empty boxes that were already in storage (x0) are kept and placed after the other boxes of the same product.
5. Label-only slots (reserved slots with a product label but no box) are cleared. A new label appears when you take a box out of a slot again.

### Configuration
Edit `BepInEx/config/penguin4glte.supermarkettogether.storagesorter.cfg`, or use ConfigurationManager (F1).

| Setting | Default | Description |
|---|---|---|
| `SortHotkey` | `F9` | Key that runs the sort. |
| `MergeBoxes` | `true` | Combine partial boxes of the same product. |
| `UseCategoryShelves` | `true` | Respect OrderAndOrganize shelf categories. Has no effect if OrderAndOrganize is not installed. |
| `Language` | `Auto` | Message language: `Auto`, `en`, `ja`, or any language you added. `Auto` uses Japanese if the game is set to Japanese, and English otherwise. |

### Translations
Messages are built in for **English** and **Japanese**. To add another language:
1. Open the mod's folder (`BepInEx/plugins/.../StorageSorter/lang/`).
2. Copy `_template.txt`, rename the copy to `<code>.txt` (e.g. `de.txt`), and translate the text after each `=`. Save it as UTF-8.
3. Set `Language = de` in the config and restart the game.

Keys you leave out fall back to English. Translations are welcome. Feel free to send one in.

### Notes and limitations
- **Only the host can sort.** Clients don't need the mod installed. It doesn't hurt if they have it, though.
- Sorting takes about 0.3 seconds. During that time, employees briefly pause putting boxes into storage and simply retry afterwards.
- If a client takes or places a box during that 0.3 seconds, a box may end up in a different slot. No items are lost, and a warning is written to the BepInEx log.
- Manufacturing storage shelves are not touched.
- Back up your save before trying any new mod. Saves are in `%USERPROFILE%\AppData\LocalLow\DDTNL\Supermarket Together`.

---

## 日本語

### 特徴
- ゲーム中に **F9** を押すと、倉庫の棚をすべて一度に並び替えます。
- 箱は **商品カテゴリ → 商品ID** の順に並びます。同じ商品なら、中身の多い箱が先です。
- 棚は倉庫の基準点に近いものから順に使います。従業員が箱を取りに行くときと同じ優先順です。
- **同じ商品の中途半端な箱を、1 箱の上限までまとめます。** 商品の総数は変わりません。
- **[OrderAndOrganize](https://thunderstore.io/c/supermarket-together/p/ddqminik/OrderAndOrganize/) に対応しています（なくても動きます）。** カテゴリを設定した棚には、そのカテゴリの商品を先に入れます。
- **マルチプレイでも使えます。** ゲーム本体のサーバー側の倉庫処理を使って書き換えるので、全員に同期され、セーブにもそのまま残ります。

### 使い方
1. Mod マネージャーでインストールします。手動で入れる場合は `StorageSorter.dll` を `BepInEx/plugins/` に置きます。
2. **ホストとして**店舗をロードします（シングルプレイもホスト扱いです）。
3. メニューを開いていない、歩いている状態で **F9** を押します。

終わると、更新したスロット数が画面に表示されます。

### 並び替えのルール
1. すべての倉庫棚から箱を集め、カテゴリ → 商品ID の順に並べます。
2. OrderAndOrganize が入っている場合、カテゴリを設定した棚にはそのカテゴリの箱を先に入れます。入りきらない分はカテゴリなしの棚へ入れ、それでも足りなければ別カテゴリの棚も使います。
3. `MergeBoxes = true` のときは、同じ商品の中途半端な箱をまとめます。まとめた結果、中身がなくなった箱は消えます。
4. もともと倉庫にあった空箱（x0）は消さずに残し、その商品の箱の最後に並べます。
5. ラベルだけのスロット（箱がなく、商品ラベルだけが付いた予約枠）は外します。スロットから箱を取り出すと、またラベルが付きます。

### 設定
`BepInEx/config/penguin4glte.supermarkettogether.storagesorter.cfg` を編集するか、ConfigurationManager（F1）で変更できます。

| 設定 | 初期値 | 説明 |
|---|---|---|
| `SortHotkey` | `F9` | 並び替えを実行するキー |
| `MergeBoxes` | `true` | 同じ商品の中途半端な箱をまとめる |
| `UseCategoryShelves` | `true` | OrderAndOrganize の棚カテゴリを優先する（OrderAndOrganize がなければ何もしません） |
| `Language` | `Auto` | メッセージの言語。`Auto`・`en`・`ja`、または追加した言語。`Auto` はゲームが日本語なら日本語、それ以外は英語 |

### 翻訳
メッセージは **英語** と **日本語** を内蔵しています。ほかの言語を追加するには：
1. Mod のフォルダ（`BepInEx/plugins/.../StorageSorter/lang/`）を開きます。
2. `_template.txt` をコピーして `<言語コード>.txt`（例：`de.txt`）に名前を変え、各行の `=` の右側を翻訳します。UTF-8 で保存してください。
3. 設定で `Language = de` にして、ゲームを再起動します。

翻訳していない項目は英語で表示されます。

### 注意点・制限
- **並び替えを実行できるのはホストだけです。** 参加者は Mod を入れなくても大丈夫です（入れていても問題ありません）。
- 並び替えには約 0.3 秒かかります。その間、従業員は倉庫へ箱を置くのを一時的に止め、終わったあとにやり直します。
- その 0.3 秒の間に参加者が箱を出し入れすると、箱が別のスロットに入ることがあります。商品はなくならず、BepInEx のログに警告が出ます。
- 製造用の倉庫棚は並び替えの対象外です。
- 新しい Mod を試す前には、セーブデータのバックアップをおすすめします。セーブの場所は `%USERPROFILE%\AppData\LocalLow\DDTNL\Supermarket Together` です。
