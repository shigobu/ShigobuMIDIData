using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shigobu.MIDI.DataLib
{
	/// <summary>
	/// 小節：拍：ティックを表します
	/// </summary>
	public struct MeasureBeatTick
	{
		/// <summary>
		/// 小節：拍：ティックを指定して、オブジェクトの初期化をします。
		/// </summary>
		/// <param name="measure">小節</param>
		/// <param name="beat">拍</param>
		/// <param name="tick">ティック</param>
		public MeasureBeatTick(int measure, int beat, int tick) : this()
		{
			Measure = measure;
			Beat = beat;
			Tick = tick;
		}
		/// <summary>
		/// 小節
		/// </summary>
		public int Measure { get; set; }
		/// <summary>
		/// 拍
		/// </summary>
		public int Beat { get; set; }
		/// <summary>
		/// ティック
		/// </summary>
		public int Tick { get; set; }

		/// <summary>
		/// 小節:拍:ティックの書式で文字列化します。
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return $"{Measure, 5}:{Beat, 2}:{Tick, 3}";
		}

		/// <summary>
		/// 文字列を解析して、小節:拍:ティックオブジェクトを返します。
		/// </summary>
		/// <param name="s">解析対象の文字列</param>
		/// <returns>MeasureBeatTickオブジェクト</returns>
		public static MeasureBeatTick Parse(string s)
		{
			string[] splited = s.Split(':');
			MeasureBeatTick retVal = new MeasureBeatTick();
			retVal.Measure = int.Parse(splited[0]);
			retVal.Beat = int.Parse(splited[1]);
			retVal.Tick = int.Parse(splited[2]);
			return retVal;
		}
	}

	/// <summary>
	/// 拍子記号を表します。
	/// </summary>
	/// <remarks>引数なしのコンストラクタを使われたくないからクラスにした。</remarks>
	public class TimeSignature
	{
		/// <summary>
		/// 具体的な数値を指定して、オブジェクトを初期化します。
		/// </summary>
		/// <param name="nn">拍子記号の分子</param>
		/// <param name="dd">拍子記号の分母の指数部分</param>
		/// <param name="cc">1拍あたりのMIDIクロック数</param>
		/// <param name="bb">1拍の長さを32分音符の数で表す</param>
		public TimeSignature(int nn, int dd, int cc, int bb)
		{
			this.nn = nn;
			this.dd = dd;
			this.cc = cc;
			this.bb = bb;
		}

		/// <summary>
		/// 拍子記号の分子
		/// </summary>
		public int nn { get; set; }
		/// <summary>
		/// 拍子記号の分母の指数部分
		/// </summary>
		public int dd { get; set; }
		/// <summary>
		/// 1拍あたりのMIDIクロック数
		/// </summary>
		public int cc { get; set; }
		/// <summary>
		/// 1拍の長さを32分音符の数で表す
		/// </summary>
		public int bb { get; set; }
	}

	/// <summary>
	/// 調性記号を表します。
	/// </summary>
	public struct KeySignature
	{
		/// <summary>
		/// 値を使用してオブジェクトを初期化します。
		/// </summary>
		/// <param name="sf">#又は♭の数</param>
		/// <param name="mi">長調か短調か</param>
		public KeySignature(int sf, Keys mi) : this()
		{
			this.sf = sf;
			this.mi = mi;
		}

		public int sf { get; set; }
		public Keys mi { get; set; }
	}

	/// <summary>
	/// SMPTEオフセットを表します。
	/// </summary>
	public class SMPTEOffset
	{
		/// <summary>
		/// すべて0で初期化します。
		/// </summary>
		public SMPTEOffset() : this(0, 0, 0, 0, 0, 0)
		{
		}

		/// <summary>
		/// 値を指定してオブジェクトの初期化をします。
		/// </summary>
		/// <param name="mode">モード</param>
		/// <param name="hour">時間(0～23)</param>
		/// <param name="min">分(0～59)</param>
		/// <param name="sec">秒(0～59)</param>
		/// <param name="frame">フレーム(0～30※)</param>
		/// <param name="subFrame">サブフレーム(0～99)</param>
		public SMPTEOffset(SMPTEMode mode, int hour, int min, int sec, int frame, int subFrame)
		{
			Mode = mode;
			Hour = hour;
			Min = min;
			Sec = sec;
			Frame = frame;
			SubFrame = subFrame;
		}

		public SMPTEMode Mode { get; set; }
		public int Hour { get; set; }
		public int Min { get; set; }
		public int Sec { get; set; }
		public int Frame { get; set; }
		public int SubFrame { get; set; }
	}
}
