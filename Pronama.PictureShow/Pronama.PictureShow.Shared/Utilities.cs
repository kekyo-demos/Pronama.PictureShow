using System.Linq;
using System.Text.RegularExpressions;
using Sgml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Pronama.PictureShow
{
	/// <summary>
	/// ユーティリティクラスです。
	/// </summary>
	public static class Utilities
	{
		private static readonly  char[] separators_ = { ' ', '\t' };

		#region FetchHtmlFromUrlAsync
		/// <summary>
		/// 指定されたURLからHTMLをダウンロードしてXDocumentとして取得する、非同期メソッドです。
		/// </summary>
		/// <param name="url">URL</param>
		/// <returns>XDocumentの結果を返すタスク</returns>
		public static async Task<XDocument> FetchHtmlFromUrlAsync(Uri url)
		{
			// HttpClientを使って非同期でダウンロードします。
			using (var httpClient = new HttpClient())
			{
				// 非同期でダウンロードするよ
				using (var stream = await httpClient.GetStreamAsync(url).
					ConfigureAwait(false))	// ←この後の処理をワーカースレッドで実行するおまじない
				{
					// ストリームをUTF8のテキストとして読めるようにするよ
					using (var tr = new StreamReader(stream, Encoding.UTF8, true))
					{
						// スクレイピングの主役「SgmlReader」
						using (var sgmlReader = new SgmlReader(tr)
						{
							CaseFolding = CaseFolding.ToLower,	// タグ名とか、常に小文字にするよ
							DocType = "HTML",					// 常にHTMLとして読み取るよ
							IgnoreDtd = true,					// DTDを無視するよ
							WhitespaceHandling = false			// 空白を無視するよ
						})
						{
							// SgmlReaderを使って、XDocumentに変換！
							return XDocument.Load(sgmlReader);
						}
					}
				}
			}
		}
		#endregion

		#region SafeGetAttribute
		/// <summary>
		/// XElementに定義されているXML属性の取得を試みます。
		/// </summary>
		/// <param name="element">対象のXElement</param>
		/// <param name="attributeName">XML属性名</param>
		/// <returns>値が存在しない場合はnull</returns>
		public static string SafeGetAttribute(XElement element, string attributeName)
		{
			var attribute = element.Attribute(attributeName);
			if (attribute == null)
			{
				return null;
			}

			return attribute.Value;
		}
		#endregion

		public static KeyValuePair<string, string> CreateMatch(string attributeName, string attributeValue)
		{
			return new KeyValuePair<string, string>(attributeName, attributeValue);
		}

		#region TraverseByAttributes
		/// <summary>
		/// 指定されたエレメントから、XML属性で定義された階層構造を辿るクエリを取得します。
		/// </summary>
		/// <param name="beginElement">起点となるエレメント</param>
		/// <param name="matches">XML属性名と値の組の群</param>
		/// <returns>クエリ</returns>
		public static IEnumerable<XElement> TraverseByAttributes(XElement beginElement, params KeyValuePair<string, string>[] matches)
		{
			var currentFilter = (IEnumerable<XElement>)new[] { beginElement };
			return matches.Aggregate(
				currentFilter,
				(current, match) =>
					from element in current.Elements("div")
					where
						SafeGetAttribute(element, match.Key).
						Split(separators_, StringSplitOptions.RemoveEmptyEntries).
						Any(word => StringComparer.OrdinalIgnoreCase.Compare(word, match.Value) == 0) == true
					select element);
		}
		#endregion

		#region ParseUrl
		/// <summary>
		/// URL文字列をパースしてUriに変換します。
		/// </summary>
		/// <param name="baseUrl">基準となるURLを示すUri</param>
		/// <param name="urlString">相対、又は絶対URLを示す文字列</param>
		/// <returns>パース出来ない場合はnull</returns>
		public static Uri ParseUrl(Uri baseUrl, string urlString)
		{
			Uri url;
			Uri.TryCreate(baseUrl, urlString, out url);

			return url;
		}
		#endregion

		#region ToImageSourceAsync
		private static async Task<ImageSource> ToImageSourceAsync(BitmapFrame bitmapFrame)
		{
			// Get the pixels
			var dataProvider = await bitmapFrame.GetPixelDataAsync(
				BitmapPixelFormat.Bgra8,
				BitmapAlphaMode.Premultiplied,
				new BitmapTransform(),
				ExifOrientationMode.RespectExifOrientation,
				ColorManagementMode.ColorManageToSRgb);

			var pixels = dataProvider.DetachPixelData();

			var bitmap = new WriteableBitmap((int)bitmapFrame.PixelWidth, (int)bitmapFrame.PixelHeight);

			using (var pixelStream = bitmap.PixelBuffer.AsStream())
			{
				await pixelStream.WriteAsync(pixels, 0, pixels.Length);
			}

			bitmap.Invalidate();

			return bitmap;
		}
		#endregion

		#region FetchImageFromUrlAsync
		/// <summary>
		/// 指定されたURLからイメージをダウンロードしてBitmapFrameとして取得する、非同期メソッドです。
		/// </summary>
		/// <param name="url">URL</param>
		/// <returns>ImageSourceの結果を返すタスク</returns>
		public static async Task<ImageSource> FetchImageFromUrlAsync(Uri url)
		{
			// HttpClientを使って非同期でダウンロードします。
			using (var httpClient = new HttpClient())
			{
				// プロ生ちゃんサイトの壁紙コーナーからダウンロードするよ
				using (var stream = await httpClient.GetStreamAsync(url))
				{
					// デコーダーを使って、データをイメージに変換するよ
					var decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());
					var frame0 = await decoder.GetFrameAsync(0);
					
					return await ToImageSourceAsync(frame0);
				}
			}
		}
		#endregion
	}
}
