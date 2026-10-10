# Shos.Windows リポジトリ レビュー

- レビュー日: 2026-10-10
- 対象: `master` (HEAD: `e27d875`) の全ファイル (18 ファイル)
- 方法: 静的レビュー (コードリーディング)。ビルド・実行による検証は行っていません。

## 概要

WPF (.NET 8) 向けの小規模ライブラリで、`FontComboBox` コントロールと `EnumToBooleanConverter` を提供しています。コードは短く読みやすいのですが、**`FontComboBox` の選択同期ロジックに実際の動作に関わる不具合が複数あります**。特に、記号フォントの扱い、`FontFamily` の参照比較、`SetValue` によるバインディングの破棄の 3 点は利用者から見て挙動が誤るので、優先して修正することを勧めます。

| 重要度 | 件数 |
|---|---|
| 高 (動作不良) | 4 |
| 中 (堅牢性・API 設計) | 7 |
| 低 (保守性・スタイル・ドキュメント) | 多数 |

---

## 1. `Shos.Windows/FontComboBox.xaml.cs`

### 🔴 H-1. 記号フォントの `FontFamilyName` が既定フォント名に置き換えられ、選択が別のフォントに飛ぶ

`FontComboBox.xaml.cs:32`

```csharp
FontFamilyName = fontFamily.IsSymbol() ? DefaultFontFamily.Source : fontFamily?.Source ?? "";
```

記号フォント (Marlett、Wingdings、Symbol など) の表示用フォントを既定フォントにするため、`FontFamilyName` 自体を `SystemFonts.MenuFontFamily.Source` (例: `"Segoe UI"`、日本語環境では `"Yu Gothic UI"` など) に書き換えています。しかし `FontFamilyName` は**表示用**と**識別キー兼公開プロパティの値**の両方に使われているため、次の問題が起きます。

1. 記号フォントを選ぶと、`SelectedFontFamilyName` は本来のフォント名ではなく `"Segoe UI"` になる。
2. `SelectedFontFamilyNamePropertyChanged` (`:99`) は `FontFamilyName == fontFamilyName` で `FirstOrDefault` するので、外部から `SelectedFontFamilyName = "Segoe UI"` を設定すると、ソート順で先に来る記号フォント (例: `Bookshelf Symbol 7`、`Marlett`) が選ばれてしまう。
3. `DefaultLocalFontFamilyName` (`:51`) も同じ検索をしているため、`SelectedLocalFontFamilyName` の既定値が記号フォントのローカル名 (`"Marlett"` など) になることがある。

**修正案**: 識別用の名前と表示用のフォントを分けます。

```csharp
class FontComboBoxItemViewModel
{
    public FontFamily FontFamily          { get; }
    public string     FontFamilyName      { get; }   // 常に fontFamily.Source
    public string     LocalFontFamilyName { get; }
    public FontFamily DisplayFontFamily   { get; }   // 記号フォントなら既定フォント

    public FontComboBoxItemViewModel(FontFamily fontFamily)
    {
        FontFamily          = fontFamily;
        FontFamilyName      = fontFamily.Source;
        LocalFontFamilyName = ToLocalFontFamilyName(fontFamily);
        DisplayFontFamily   = fontFamily.IsSymbol() ? DefaultFontFamily : fontFamily;
    }
}
```

`FontComboBox.xaml` の `ItemTemplate` は `FontFamily="{Binding DisplayFontFamily}"` に変えます。

### 🔴 H-2. `FontFamily` を `==` で比較しているため、外部から渡した `SelectedFontFamily` がほぼ一致しない

`FontComboBox.xaml.cs:72`

```csharp
itemsSource.FirstOrDefault(vm => vm.FontFamily == fontFamily);
```

`System.Windows.Media.FontFamily` は `Equals` をオーバーライドしていますが、`==` 演算子はオーバーロードしていません。そのため、この比較は**参照比較**になります。`Fonts.SystemFontFamilies` から得たインスタンス以外は一致しません。

- XAML で `SelectedFontFamily="Arial"` と書く (`FontFamilyConverter` が新しいインスタンスを作る)
- `new FontFamily("Arial")` をバインドする
- 既定値の `SystemFonts.MenuFontFamily`

いずれの場合も選択に反映されません。

**修正案**: `Equals` を使うか、`Source` で比較します。

```csharp
itemsSource.FirstOrDefault(vm => Equals(vm.FontFamily, fontFamily))
// あるいは
itemsSource.FirstOrDefault(vm => string.Equals(vm.FontFamilyName, fontFamily?.Source, StringComparison.OrdinalIgnoreCase))
```

### 🔴 H-3. コールバック内の `SetValue` が利用者の OneWay バインディングを破棄する／既定で TwoWay にならない

`FontComboBox.xaml.cs:74-76, 101-103, 128-130, 143-145`

- 依存関係プロパティは `PropertyMetadata` で登録されているため、バインディングの既定モードは **OneWay** です。利用者は毎回 `Mode=TwoWay` を書く必要があります (サンプルでも明示しています)。`ComboBox.SelectedItem` などの WPF 標準コントロールとは挙動が異なります。
- コントロール内部で `SetValue` / プロパティ setter を呼ぶと、そのプロパティに設定されている **OneWay バインディングはローカル値で置き換えられて消えます**。たとえば `SelectedFontFamilyName="{Binding Name}"` (OneWay) と書いた利用者がコンボボックスで一度選択すると、以後 ViewModel の変更が反映されなくなります。

**修正案**:

```csharp
public static readonly DependencyProperty SelectedFontFamilyNameProperty
    = DependencyProperty.Register(
          nameof(SelectedFontFamilyName),
          typeof(string),
          typeof(FontComboBox),
          new FrameworkPropertyMetadata(
              DefaultFontFamily.Source,
              FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
              SelectedFontFamilyNamePropertyChanged));
```

コントロール内部からの更新は `d.SetCurrentValue(...)` に統一します。

### 🔴 H-4. 初期状態で `SelectedItem` がプロパティ値と同期していない

依存関係プロパティの既定値は `"Segoe UI"` などですが、`PropertyChangedCallback` は値が**変化したとき**にしか呼ばれません。そのため次のような状態になります。

- 何もバインドしない場合: `SelectedFontFamilyName` は `"Segoe UI"` なのに、コンボボックスは未選択のまま。
- 既定値と同じ値をバインドした場合も、コールバックが呼ばれず未選択のまま。

**修正案**: コンストラクター (または `OnInitialized` / `Loaded`) で現在のプロパティ値から `SelectedItem` を一度確定させます。あるいは、3 つのプロパティを `SelectedItem` から `CoerceValue` で導出する設計に寄せます。

### 🟠 M-1. 型初期化時に全システムフォントを読み込むため、例外が出るとコントロール全体が使えなくなる

`FontComboBox.xaml.cs:16, 42-46`

- `static readonly itemsSource` の初期化で、すべての `Fonts.SystemFontFamilies` に対して `GetTypefaces().First()` と `TryGetGlyphTypeface` を実行しています。
  - `GetTypefaces()` が空のファミリーがあると `First()` が `InvalidOperationException` を投げます。破損フォントの読み込みでも例外が出ることがあります。
  - 型初期化子の中での例外は `TypeInitializationException` になり、以後そのプロセスでは `FontComboBox` を一切生成できません。
- フォント数が多い環境では起動時 (初回生成時) のコストも大きくなります。デザイナー上でも毎回実行されます。

**修正案**: `FirstOrDefault()` にして、フォントごとに `try/catch` で防御します。`Lazy<T>` で遅延初期化することも検討してください。

```csharp
public static bool IsSymbol(this FontFamily @this)
{
    try {
        var typeface = @this.GetTypefaces().FirstOrDefault();
        return typeface is not null
            && typeface.TryGetGlyphTypeface(out var glyphTypeface)
            && glyphTypeface.Symbol;
    } catch (Exception) when (/* FileFormatException 等 */ true) {
        return false;
    }
}
```

### 🟠 M-2. 一致する項目がないと状態が不整合のまま残る

`:73, :100, :127` の `if (selected != null)` では、一致しない値 (存在しないフォント名、大文字小文字違い、`null`) を設定すると何も起こりません。その結果、たとえば `SelectedFontFamilyName = "NoSuchFont"` なのに選択は `Arial` のまま、`SelectedFontFamily` も `Arial` のまま、という状態になります。

**修正案**: 一致しない場合の方針を決めて実装・文書化します。選択を解除する (`SelectedItem = null`) か、`CoerceValueCallback` で直前の値に戻すかのどちらかです。名前比較は `StringComparison.OrdinalIgnoreCase` にすることを勧めます。

### 🟠 M-3. 相互に再入する更新ロジック

3 つの `PropertyChangedCallback` と `SelectionChanged` ハンドラーが互いに他のプロパティを設定し合っているため、1 回の操作で多段の再入が起きます。H-1 のように識別キーが重複すると、選択が一時的に別項目へ切り替わり、`SelectionChanged` が複数回発火します。

**修正案**: 「真のソースは `SelectedItem` だけ」と決め、ほかの 3 プロパティは `SelectedItem` から導出するように一方向化します。外部からの設定時だけ `SelectedItem` を逆引きし、更新中フラグ (`bool isUpdating`) で再入を止めます。これでコードも大幅に短くなります。

### 🟠 M-4. キーボード検索とアクセシビリティ

`ItemTemplate` だけを指定し、`TextSearch.TextPath` も `ToString()` のオーバーライドもありません。そのため次のような問題があります。

- コンボボックスにフォーカスして文字を打っても、頭文字検索が効きません (検索対象が `"Shos.Windows.Controls.FontComboBoxItemViewModel"` になる)。
- スクリーンリーダーが項目を型名で読み上げます。

**修正案**: `FontComboBox.xaml` に `TextSearch.TextPath="LocalFontFamilyName"` を追加し、`FontComboBoxItemViewModel.ToString()` が `LocalFontFamilyName` を返すようにします。

### 🟡 L-1. その他 (FontComboBox)

- `:16` `... && glyphTypeface != null ? glyphTypeface.Symbol : false` は `... && glyphTypeface.Symbol` で十分です。
- `:32` `fontFamily.IsSymbol()` を呼んだ後に `fontFamily?.Source` と null 条件演算子を使っていて、null 前提が一貫していません。引数は非 null なので `?.` は不要です。同様に `FontFamily?` プロパティ (`:25`) も非 null にできます。プロパティは `{ get; }` (読み取り専用自動プロパティ) で十分です。
- `:75-76, :102-103, :129-130` は null チェック後なので、`selectedFontComboBoxItemViewModel?.` の `?.` は不要です。
- `:37` `FamilyNames.FirstOrDefault(kv => kv.Key == language)` は `FamilyNames.TryGetValue(language, out var name)` で書けます。カルチャ名が完全一致しない場合 (`"ja"` など) や、英語名へのフォールバックも考慮すると親切です。
- ローカル名には `CurrentCulture` を使っていますが、表示言語としては `CurrentUICulture` の方が適切な場合が多いです。また、static 側 (`:21`) は型初期化時のカルチャで固定され、インスタンス側 (`:138`) は生成時のカルチャを使うので不一致の可能性があります。
- `:138` `Language = ...` はローカル値を設定するため、親要素から継承される `Language` を上書きします。意図的でなければ削除を検討してください。
- `SelectionChanged +=` をコンストラクターで購読するより、`OnSelectionChanged` をオーバーライドする方が派生コントロールとして自然です。
- 依存関係プロパティ名は文字列リテラルではなく `nameof(SelectedFontFamily)` を使う方が安全です。
- static フィールドの初期化順序 (`itemsSource` → `DefaultLocalFontFamilyName` を使う DP 登録) がソース上の宣言順に依存していて壊れやすいです。
- `ItemsSource` は public なので、利用者が上書きすると内部の `itemsSource` 検索と矛盾します。必要であれば文書化してください。
- public API に XML ドキュメントコメントがありません。

## 2. `Shos.Windows/FontComboBox.xaml`

- `FontSize="16"` がハードコードされていて、コンボボックス自体の `FontSize` を変えても項目サイズが変わりません。`FontSize` を指定しない (継承させる) か、`{Binding FontSize, RelativeSource={RelativeSource AncestorType=ComboBox}}` を使ってください。
- `ScrollViewer.VerticalScrollBarVisibility="Visible"` は常にスクロールバーを表示します。`Auto` で十分です。
- `VirtualizingStackPanel` の `CanVerticallyScroll="True"` は `ScrollViewer` が設定するプロパティなので、指定は不要です。
- `xmlns:d` / `mc:Ignorable` の `DesignHeight/Width` は `ComboBox` ルートでは効果が薄いので削除してもかまいません (好み)。

## 3. `Shos.Windows/EnumToBooleanConverter.cs`

### 🟠 M-5. 例外になる入力が多い

| 箇所 | 入力 | 結果 |
|---|---|---|
| `:13` `value.GetType()` | `value == null` (null 許容 enum のプロパティなど) | `NullReferenceException` |
| `:13` `Enum.IsDefined` | `value` が enum でない | `ArgumentException` |
| `:16` `Enum.Parse` | `ConverterParameter` の綴り誤り | `ArgumentException` (アプリがクラッシュ) |
| `:17` `(int)parameterValue == (int)value` | 基になる型が `int` 以外 (`byte`, `long` など) | `InvalidCastException` |
| `:25` `Enum.Parse(targetType, ...)` | `targetType` が `Nullable<TEnum>` | `ArgumentException` |

さらに、`[Flags]` で値を組み合わせた場合は `Enum.IsDefined` が `false` になり、常に `UnsetValue` が返ります。

**修正案**:

```csharp
public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
{
    if (value is not Enum || parameter is not string parameterString)
        return DependencyProperty.UnsetValue;

    return Enum.TryParse(value.GetType(), parameterString, out var parameterValue)
           ? value.Equals(parameterValue)
           : DependencyProperty.UnsetValue;
}

public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
{
    var enumType = Nullable.GetUnderlyingType(targetType) ?? targetType;
    return true.Equals(value) && parameter is string parameterString
           && Enum.TryParse(enumType, parameterString, out var result)
           ? result!
           : Binding.DoNothing;
}
```

- `ConvertBack` でラジオボタンが OFF になったときは `Binding.DoNothing` を返す方が意図が明確です。
- このファイルには `#nullable enable` がありませんが、プロジェクトで `Nullable` が有効なので、`IValueConverter` の `object?` シグネチャと合わずに警告 (CS8767) が出ます。シグネチャを `object?` にそろえてください。
- `ConverterParameter` に enum 値そのもの (`{x:Static local:FontSizeKind.Small}`) を渡せるようにすると、タイプミスをコンパイル時に検出できるようになります。

## 4. プロジェクト / ビルド構成

### 🟠 M-6. Roslynator.Analyzers の二重参照

`Directory.Build.props` で全プロジェクトに `Roslynator.Analyzers 4.12.0` を追加しているのに、`Shos.Windows.csproj:31-34` と `Shos.Windows.Sample.csproj:20-23` でも同じパッケージを参照しています。重複した `PackageReference` は NuGet の警告 (NU1504) の対象になり、バージョン更新漏れの原因にもなります。各 csproj 側の参照は削除してください (コミット `e7e98a6` で props を追加したときの消し忘れと思われます)。

### 🟠 M-7. 非推奨の SDK 指定

両 csproj の `Sdk="Microsoft.NET.Sdk.WindowsDesktop"` は .NET 5 以降非推奨です (NETSDK1137 警告)。`Sdk="Microsoft.NET.Sdk"` と `<UseWPF>true</UseWPF>` の組み合わせに変更してください。

### 🟡 L-2. パッケージ設定 (`Shos.Windows.csproj`)

- `Description` が「Control Library (FontComboBox)」となっていて、`EnumToBooleanConverter` が含まれていることが書かれていません。
- `PackageRequireLicenseAcceptance=true` は MIT ライセンスとしては一般的でなく、利用者のインストール時に同意ダイアログが毎回出ます。`false` を推奨します。
- `GeneratePackageOnBuild=true` だと通常のビルドのたびにパックが走ります。リリース時に `dotnet pack` するか、CI でパックする方が効率的です。
- `PackageTags`、`PackageIcon` がなく、NuGet 上で見つけにくくなっています。
- `GenerateDocumentationFile` がないため、IntelliSense 用の XML ドキュメントがパッケージに含まれません。
- シンボルパッケージ (`IncludeSymbols` + `SymbolPackageFormat=snupkg`)、SourceLink、`ContinuousIntegrationBuild` を追加すると、デバッグ体験と再現性が上がります。
- `AssemblyVersion` / `FileVersion` / `Version` を三重に管理しています。`Version` だけにすれば、ほかの 2 つは自動的に導出されます。
- `Properties/launchSettings.json` は `executablePath` のない "Executable" プロファイルで、ライブラリには不要です。削除してかまいません。
- 共通のメタデータ (`Authors`、`Company`、`Copyright`、`RepositoryUrl`、`LangVersion` など) は `Directory.Build.props` に集約できます。

### 🟡 L-3. サンプルプロジェクト

- `IsPackable=false` がないため、ソリューションで `dotnet pack` するとサンプルまでパックされます。パッケージ用メタデータ (`PackageLicenseExpression` など) も不要です。
- 名前が統一されていません。
  - フォルダー: `Shos.Windows.Sample`
  - プロジェクト: `Shos.Windows.Sample`
  - `App` の名前空間: `FontComboBoxSample` (`App.xaml:1`, `App.xaml.cs:3`)
  - `MainWindow` / `MainViewModel` の名前空間: `Shos.Windows.Sample`

  フォルダー名、プロジェクト名、名前空間をそろえることを勧めます。
- `Prism.Core 8.1.97` は MIT ライセンスの最終系列なので問題はありませんが、サンプル程度なら `CommunityToolkit.Mvvm` に置き換えると、将来ライセンス (Prism 9 以降は商用ライセンス) を気にせずに済みます。
- `MainViewModel.cs:23-24` では値が変わらなくても `RaisePropertyChanged(nameof(FontSize))` が呼ばれます。`if (SetProperty(ref fontSizeKind, value)) RaisePropertyChanged(nameof(FontSize));` にしてください。
- `#nullable enable` はプロジェクト設定で有効になっているので冗長です (`MainViewModel.cs`、`MainWindow.xaml.cs`、`FontComboBox.xaml.cs`)。
- `MainWindow.xaml:21-23` の `ConverterParameter=Small ,` は末尾に空白を含みます。動作はしますが、整形として紛らわしいです。

## 5. ドキュメント

### `README.md`

- 「Projects」節に `Shos.Windows.Controls` と `Shos.Windows.Data` という**存在しないプロジェクト**が書かれています。実際は `Shos.Windows` という 1 つのプロジェクト内の名前空間です。現状に合わせて書き直してください。
- `**` で始まる行 (`:14`, `:19`) は Markdown の入れ子リストとして解釈されません。インデント (`  -`) を使ってください。
- `:6` の `(for .NET 8.0 or later)` がリスト項目の続きとして描画されます。前に空行を入れてください。
- プロパティ表の Description 列が空です。XAML の使用例 (`FontComboBox` と `EnumToBooleanConverter` のバインド例) もありません。
- `EnumToBooleanConverter` の説明節がありません。
- `docs.microsoft.com ... view=netcore-3.0` は古いリンクです。`learn.microsoft.com` の `net-8.0` 版に更新してください。
- NuGet のリンクが 2 か所 (`:8`, `:48`) で重複しています。
- ブログ・Web サイトへのリンクが `http://` です。可能であれば `https://` にしてください。
- README は NuGet パッケージに同梱されるので、パッケージ利用者向けの使い方を先頭に置くと親切です。

### `license`

- `Copyright © 2017` ですが、csproj では `2019-2026` です。年表記をそろえることを検討してください。

## 6. リポジトリ全般

- **テストがありません**。`EnumToBooleanConverter` は純粋関数なので、単体テストを簡単に書けます。`FontComboBox` も STA スレッド上でインスタンスを作れば、プロパティ同期 (H-1〜H-4) の回帰テストが可能です。
- **CI がありません**。GitHub Actions (`windows-latest`) でビルド・テスト・パックを自動化し、タグ push で NuGet 公開する構成を勧めます。
- 改行コード: 作業ツリーは CRLF、`.gitattributes` は `* text=auto` です。WSL など LF 環境の Git から見ると、全ファイルに CRLF→LF の変換警告が出ます。また、`Shos.Windows.csproj` が内容差分なし (改行コードのみ) で modified になっています。`.gitattributes` で `*.cs`、`*.xaml`、`*.csproj`、`*.sln` に `eol=crlf` を明示すると、環境による揺れがなくなります。
- `.gitattributes` / `.gitignore` は Visual Studio テンプレートのままで問題ありません。

---

## 推奨対応順

1. **H-1〜H-4, M-3**: `FontComboBox` の同期ロジックを「`SelectedItem` を唯一のソース」とする形に作り直す (識別名と表示フォントを分離、`Equals` 比較、`BindsTwoWayByDefault` + `SetCurrentValue`、初期同期)。
2. **M-5**: `EnumToBooleanConverter` を例外の出ない実装にする。
3. **M-1, M-4**: フォント列挙の防御と `TextSearch.TextPath` を追加する。
4. **M-6, M-7**: csproj を整理する (Roslynator の重複削除、SDK 指定の変更)。
5. README を現状に合わせて修正する。
6. テストと CI を追加する。

---

## 対応状況 (2026-10-10 追記)

| 項目 | 状況 | 内容 |
|---|---|---|
| H-1 | 対応済 | `FontFamilyName` は常に `FontFamily.Source` を返すように変更。表示用に `DisplayFontFamily` を追加 |
| H-2 | 対応済 | `FontFamily.Source` で比較 (大文字小文字は区別しない) |
| H-3 | 対応済 | `FrameworkPropertyMetadataOptions.BindsTwoWayByDefault` に変更。内部からの更新は `SetCurrentValue` に統一 |
| H-4 | 対応済 | コンストラクターで既定値から `SelectedItem` を同期 |
| M-1 | 対応済 | `IsSymbol` を `FirstOrDefault` + `try/catch` で防御。項目一覧を `Lazy` にして、型初期化時に読み込まないように変更 |
| M-2 | 対応済 | 一致しない値が設定されたら選択を解除し、ほかのプロパティもクリア (設定されたプロパティ自体の値は保持) |
| M-3 | 対応済 | 同期処理を `Select` メソッドに一本化し、再入防止フラグを追加 |
| M-4 | 対応済 | `TextSearch.TextPath="LocalFontFamilyName"` と `ToString()` を追加 |
| M-5 | 対応済 | 例外を投げない実装に変更。enum 値パラメーター、`int` 以外を基にする enum、`Nullable<TEnum>` に対応。OFF 時は `Binding.DoNothing` を返す |
| M-6 | 対応済 | Roslynator の重複参照を削除。共通設定を `Directory.Build.props` に集約 |
| M-7 | 対応済 | `Sdk="Microsoft.NET.Sdk"` に変更 |
| L-1 | 一部対応 | null 条件演算子の整理、`nameof`、`OnSelectionChanged` のオーバーライド、XML ドキュメントコメント。ローカル名のカルチャ (`CurrentCulture`) と `Language` の設定は、グリフの描画にも影響するため従来どおりとした |
| §2 XAML | 対応済 | `FontSize="16"` をルートへ移動 (利用者が上書き可能)、スクロールバーを `Auto` に、`CanVerticallyScroll` を削除 |
| L-2 | 対応済 | Description、PackageTags、ライセンス同意不要、XML ドキュメント生成、snupkg、バージョン一元化、launchSettings.json の削除。バージョンを 1.0.0 に上げた。`GeneratePackageOnBuild` は CI でパッケージを集めるのに使うため残した |
| L-3 | 対応済 | `IsPackable=false`、名前空間の統一、`SetProperty` の戻り値を使用、`x:Static` パラメーター、冗長な `#nullable` の削除。フォルダー名を `Shos.Windows.Sample` に変更。Prism は 8.1.97 (MIT) のまま |
| §5 README | 対応済 | 構成・使用例・プロパティ説明を更新し、リンクを https に変更 |
| license | 対応済 | `license` と `Directory.Build.props` の著作権表記を `2017-2026` にそろえた |
| §6 テスト | 対応済 | `Shos.Windows.Tests` (xUnit) を追加 |
| §6 CI | 対応済 | `.github/workflows/build.yml` (windows-latest でビルド・テスト・パッケージのアップロード) を追加 |
| §6 改行 | 対応済 | `.gitattributes` を `* text=auto eol=crlf` に変更 |
