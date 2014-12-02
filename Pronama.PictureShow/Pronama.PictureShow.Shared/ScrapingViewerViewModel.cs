using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml.Media;

namespace Pronama.PictureShow
{
	/// <summary>
	/// メインビューに対応するビューモデルのクラスです。
	/// </summary>
	/// <remarks>このクラスにロジックを書きます。
	/// 本当はロジックはモデルクラスに書くべきですが、このサンプルコードでは単純化しています。</remarks>
	public sealed class ScrapingViewerViewModel
		: INotifyPropertyChanged
	{
		#region Fields
		/// <summary>
		/// プロ生ちゃんサイトの壁紙ページURLです。
		/// </summary>
		private static readonly Uri wallpaperUrl_ = new Uri("https://onedrive.live.com/?cid=623F2C273E554172&id=623F2C273E554172!11581&ft=8&tagFilter=portrait");

		/// <summary>
		/// 実行可能かどうかを格納するフィールドです。
		/// </summary>
		private bool isReady_ = true;
		#endregion

		#region Constructors
		/// <summary>
		/// コンストラクタです。
		/// </summary>
		public ScrapingViewerViewModel()
		{
			// イメージを格納するコレクションを準備
			this.Images = new ObservableCollection<ImageViewModel>();

			// コマンドを準備
			this.FireLoad = new DelegatedCommand(this.OnFireLoad);
		}
		#endregion

		#region Properties
		/// <summary>
		/// ビューにバインディングする、イメージのコレクションです。
		/// </summary>
		public ObservableCollection<ImageViewModel> Images
		{
			get;
			private set;
		}

		/// <summary>
		/// ビューにバインディングする、実行可能である事を示すプロパティです。
		/// </summary>
		public bool IsReady
		{
			get
			{
				return isReady_;
			}
			set
			{
				// 値が変わったときだけ
				if (value != isReady_)
				{
					// 保存
					isReady_ = value;

					// イベントをフックしているインスタンスに、このプロパティが変更されたことを通知するよ。
					// もっと複雑なアプリを作るときには、こんなインフラが整ったフレームワークを使ったほうがいいね。
					var propertyChanged = this.PropertyChanged;
					if (propertyChanged != null)
					{
						propertyChanged(this, new PropertyChangedEventArgs("IsReady"));
					}
				}
			}
		}

		/// <summary>
		/// ビューにバインディングする、コマンドです。
		/// </summary>
		public ICommand FireLoad
		{
			get;
			private set;
		}
		#endregion

		#region Events
		/// <summary>
		/// プロパティが変更されたことを通知するイベントです。
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		#region OnFireLoad
		/// <summary>
		/// コマンド（ビューのボタン）の実行時に、ここに遷移します。
		/// </summary>
		private async void OnFireLoad()
		{
			// 実行中は準備完了状態を落としておくと、ボタンが無効化されるよ
			this.IsReady = false;

			this.Images.Clear();

			try
			{
				// プロ生ちゃん壁紙サイトから、HTMLを非同期でダウンロードするよ
				var document = await Utilities.FetchHtmlFromUrlAsync(wallpaperUrl_);

				// LINQで抽出しよう！
				// WPF版ではネストしたノードを順番に列挙したけど、OneDriveではノードが深いので、TraverseByAttributesという
				// ユーティリティを作って、楽に探索できるようにしたよ
				var urls =
					from html in document.Elements("html")						// htmlタグを全部抽出（1コだけ）
					from body in html.Elements("body")							// html配下のbodyタグを全部抽出（1コだけ）
					from fillTable in Utilities.TraverseByAttributes(
						body,
						Utilities.CreateMatch("id", "c_base"),					// 「id="c_base"」があれば、次へ
						Utilities.CreateMatch("id", "c_content"),				// 「id="c_content"」があれば、次へ
						Utilities.CreateMatch("id", "filesPageContent"),		// 「id="filesPageContent"」があれば、次へ
						Utilities.CreateMatch("class", "c-SkyDriveApp"),		// 「class="c-SkyDriveApp"」があれば、次へ
						Utilities.CreateMatch("class", "mainContent"),			// 「class="mainContent"」があれば、次へ
						Utilities.CreateMatch("class", "centerColumn"),			// 「class="centerColumn"」があれば、次へ
						Utilities.CreateMatch("class", "content"),				// 「class="content"」があれば、次へ
						Utilities.CreateMatch("class", "contentArea"),			// 「class="contentArea"」があれば、次へ
						Utilities.CreateMatch("class", "fillTable"))			// 「class="fillTable"」があれば、次へ
					from table in fillTable.Elements("table")
					from tbody in table.Elements("tbody")
					from tr in tbody.Elements("tr")
					from td in tr.Elements("td")
					from div in td.Elements("div")
					from setItemTile in Utilities.TraverseByAttributes(
						div,
						Utilities.CreateMatch("class", "c-ListView"),			// 「class="c-ListView"」があれば、次へ
						Utilities.CreateMatch("class", "surface"),				// 「class="surface"」があれば、次へ
						Utilities.CreateMatch("class", "child"),				// 「class="child"」があれば、次へ
						Utilities.CreateMatch("class", "c-SetItemTile"))		// 「class="c-SetItemTile"」があれば、次へ
					from a in setItemTile.Elements("a")												// 上のdiv配下のaタグを全部抽出
					where
						(Utilities.SafeGetAttribute(a, "class") == "liimagelink") &&		// 上のaタグに「class="liimagelink"」があり、
						(a.Elements("img").Any() == true)									// かつ、aタグ配下に一つ以上imgタグがあれば、次へ
					let href = Utilities.SafeGetAttribute(a, "href")						// 上のaタグの「href="・・・"」を取得するよ
					let url = Utilities.ParseUrl(wallpaperUrl_, href)						// Uriクラスに変換してみる
					where url != null														// 変換出来たら
					select url;																// 変換したURLを返すよ

#if DEBUG
				urls = urls.ToList();
#endif

#if true
				// ザックザックと全部非同期でダウンロードしちゃう！
				await Task.WhenAll(urls.Select(async url =>
					{
						// URLを指定してダウンロードするよ
						var image = await Utilities.FetchImageFromUrlAsync(url);

						// コレクションに追加すれば、データバインディングで自動的に表示される！
						this.Images.Add(new ImageViewModel { ImageData = image });
					}));
#else
				// シーケンシャルにダウンロードするとどうなるか、試してみて。
				foreach (var url in urls)
				{
					// URLを指定してダウンロードするよ
					var image = await Utilities.FetchImageFromUrlAsync(url);

					// コレクションに追加すれば、データバインディングで自動的に表示される！
					this.Images.Add(new ImageViewModel { ImageData = image });
				}
#endif
			}
			finally
			{
				// 全部終わったら、準備完了状態に戻すよ
				this.IsReady = true;
			}
		}
		#endregion

		#region ImageViewModel
		/// <summary>
		/// コレクションが保持する、イメージ用のビューモデルです。
		/// </summary>
		/// <remarks>取得対象がイメージデータだけなので、ビューモデルを定義する必要はないのですが、
		/// XAMLのバインディング式と対比しやすくするため、敢えて定義しました。</remarks>
		public sealed class ImageViewModel
		{
			/// <summary>
			/// イメージを取得・設定します。
			/// </summary>
			public ImageSource ImageData
			{
				get;
				set;
			}
		}
		#endregion
	}
}
