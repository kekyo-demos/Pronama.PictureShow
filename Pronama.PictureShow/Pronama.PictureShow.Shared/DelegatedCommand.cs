using System;
using System.Windows.Input;

namespace Pronama.PictureShow
{
	/// <summary>
	/// ICommandインターフェイスを実装して、ボタンイベントをバインディング出来るようにします。
	/// </summary>
	/// <remarks>ICommandで定義される、その他の依存関係プロパティでも応用できます。</remarks>
	public sealed class DelegatedCommand : ICommand
	{
		private readonly Action action_;

		/// <summary>
		/// コンストラクタです。
		/// </summary>
		/// <param name="action">処理をバイパスするデリゲート</param>
		public DelegatedCommand(Action action)
		{
			action_ = action;
		}

#pragma warning disable 67	// イベントが使われていないという警告を抑制しています
		/// <summary>
		/// CanExecuteの状態が変化した事を通知するイベントです。
		/// </summary>
		public event EventHandler CanExecuteChanged;
#pragma warning restore 67

		/// <summary>
		/// このコマンドが実行可能かどうかを確認します。
		/// </summary>
		/// <param name="parameter">追加パラメーター</param>
		/// <returns>実行可能ならtrue</returns>
		public bool CanExecute(object parameter)
		{
			// 常に実行可能
			return true;
		}

		/// <summary>
		/// このコマンドを実行します。
		/// </summary>
		/// <param name="parameter">追加パラメーター</param>
		/// <remarks>コンストラクタに与えたデリゲートにバイパスします。</remarks>
		public void Execute(object parameter)
		{
			action_();
		}
	}
}
