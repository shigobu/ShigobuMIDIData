using System;

namespace Shigobu.MIDI.DataLib
{
	/// <summary>
	/// MIDIイベントの種類
	/// </summary>
	public enum Kinds
	{
		SequenceNumber = 0x00,
		TextEvent = 0x01,
		CopyrightNotice = 0x02,
		TrackName = 0x03,
		InstrumentName = 0x04,
		Lyric = 0x05,
		Marker = 0x06,
		CuePoint = 0x07,
		ProgramName = 0x08,
		DeviceName = 0x09,
		ChannelPrefix = 0x20,
		PortPrefix = 0x21,
		EndofTrack = 0x2F,
		Tempo = 0x51,
		SMPTEOffset = 0x54,
		TimeSignature = 0x58,
		KeySignature = 0x59,
		SequencerSpecific = 0x7F,
		//以下、チャンネルイベント
		NoteOff = 0x80,
		NoteOn = 0x90,
		KeyAfterTouch = 0xA0,
		ControlChange = 0xB0,
		ProgramChange = 0xC0,
		ChannelAfterTouch = 0xD0,
		PitchBend = 0xE0,
		//チャンネルイベント終了
		SysExStart = 0xF0,
		SysExContinue = 0xF7
	}

	/// <summary>
	/// 文字コードを表す列挙体
	/// </summary>
	public enum CharCodes
	{
		/// <summary>
		/// 指定なし。
		/// Windowsのコントロールパネルで指定されている文字コード(ANSI Code Page)でテキストエンコードするものとみなされる。
		/// </summary>
		NoCharCod = 0,
		/// <summary>
		/// {@LATIN} (ANSI)
		/// </summary>
		LATIN = 1252,
		/// <summary>
		/// {@JP} (Shift-JIS)
		/// </summary>
		JP = 932,
		/// <summary>
		/// UTF16リトルエンディアン
		/// </summary>
		UTF16LE = 1200,
		/// <summary>
		/// UTF16ビッグエンディアン
		/// </summary>
		UTF16BE = 1201
	}

	/// <summary>
	/// SMPTEフレームモード
	/// </summary>
	public enum SMPTE
	{
		SMPTE24 = 0x00,
		SMPTE25 = 0x01,
		SMPTE30D = 0x02,
		SMPTE30N = 0x03
	}

	/// <summary>
	/// 長調か短調かを表します。
	/// </summary>
	public enum Keys
	{
		/// <summary>
		/// 長調
		/// </summary>
		Major = 0,
		/// <summary>
		/// 短調
		/// </summary>
		Minor = 1
	}


	public class Event
	{
		private static int MaxTempo = 16777216;
		private static int MinTempo = 1;

		/// <summary>
		/// このイベントの一時的なインデックス(0から始まる)
		/// </summary>
		internal int TempIndex { get; set; }

		/// <summary>
		/// 絶対時刻[Tick]又はSMPTEサブフレーム単位
		/// </summary>
		public int Time { get; set; }

		/// <summary>
		/// イベントの種類(チャンネルイベントの場合、チャンネル番号を含む)
		/// （生データ）
		/// </summary>
		public int KindRaw { get; set; }

		/// <summary>
		/// イベントの種類
		/// </summary>
		public Kinds Kind
		{
			get
			{				
				return (Kinds)Enum.ToObject(typeof(Kinds), KindRaw & 0xF0);
			}
		}

		/// <summary>
		/// イベントのデータ
		/// </summary>
		public byte[] Data { get;　private set; }

		/// <summary>
		/// 次のイベント(なければNULL)
		/// </summary>
		public Event NextEvent { get; set; }

		/// <summary>
		/// 前のイベント(なければNULL)
		/// </summary>
		public Event PrevEvent { get; set; }

		/// <summary>
		/// 次の同じ種類のイベント
		/// </summary>
		public Event NextSameKindEvent{ get; set; }

		/// <summary>
		/// 前の同じ種類のイベント
		/// </summary>
		public Event PrevSameKindEvent{ get; set; }

		/// <summary>
		/// 次の結合イベント保持用
		/// </summary>
		public Event NextCombinedEvent{ get; set; }

		/// <summary>
		/// 前の結合イベント保持用
		/// </summary>
		public Event PrevCombinedEvent { get; set; }

		public Event FirstCombinedEvent { get; private set; }

		/// <summary>
		/// 親(MIDITrackオブジェクト)
		/// </summary>
		public Track Parent { get; set; }

		/// <summary>
		/// メタイベントであるかどうかを調べる
		/// </summary>
		/// <remarks>
		/// メタイベントとは、イベントの種類が0x00～0x7Fのもの、すなわち、
		/// シーケンス番号・テキストイベント・著作権・トラック名・
		/// インストゥルメント名・歌詞・マーカー・キューポイント・
		/// プログラム名・デバイス名・チャンネルプリフィックス・ポートプリフィックス・
		/// エンドオブトラック・テンポ・SMPTEオフセット・拍子記号・調性記号・シーケンサー独自のイベント
		/// などを示す。これらは主に表記メモのためのイベントであり、演奏に影響を与えるものではない。
		/// </remarks>
		public bool IsMetaEvent
		{
			get
			{
				return KindRaw <= (int)Kinds.SequenceNumber && (int)Kinds.SequencerSpecific <= KindRaw;
			}
		}

		/// <summary>
		/// シーケンス番号であるかどうかを調べる
		/// </summary>
		public bool IsSequenceNumber
		{
			get
			{
				return KindRaw == (int)Kinds.SequenceNumber;
			}
		}

		/// <summary>
		/// テキストイベントであるかどうかを調べる
		/// </summary>
		public bool IsTextEvent
		{
			get
			{
				return KindRaw == (int)Kinds.TextEvent;
			}
		}

		/// <summary>
		/// 著作権イベントであるかどうかを調べる
		/// </summary>
		public bool IsCopyrightNotice
		{
			get
			{
				return KindRaw == (int)Kinds.CopyrightNotice;
			}
		}

		/// <summary>
		/// トラック名イベントであるかどうかを調べる
		/// </summary>
		public bool IsTrackName
		{
			get
			{
				return KindRaw == (int)Kinds.TrackName;
			}
		}

		/// <summary>
		/// インストゥルメント名であるかどうかを調べる
		/// </summary>
		public bool IsInstrumentName
		{
			get
			{
				return KindRaw == (int)Kinds.InstrumentName;
			}
		}

		/// <summary>
		/// 歌詞イベントであるかどうかを調べる
		/// </summary>
		public bool IsLyric
		{
			get
			{
				return KindRaw == (int)Kinds.Lyric;
			}
		}

		/// <summary>
		/// マーカーイベントであるかどうかを調べる
		/// </summary>
		public bool IsMarker
		{
			get
			{
				return KindRaw == (int)Kinds.Marker;
			}
		}

		/// <summary>
		/// キューポイントイベントであるかどうかを調べる
		/// </summary>
		public bool IsCuePoint
		{
			get
			{
				return KindRaw == (int)Kinds.CuePoint;
			}
		}

		/// <summary>
		/// プログラム名であるかどうかを調べる
		/// </summary>
		public bool IsProgramName
		{
			get
			{
				return KindRaw == (int)Kinds.ProgramName;
			}
		}

		/// <summary>
		/// デバイス名であるかどうかを調べる
		/// </summary>
		public bool IsDeviceName
		{
			get
			{
				return KindRaw == (int)Kinds.DeviceName;
			}
		}

		/// <summary>
		/// チャンネルプレフィックスであるかどうかを調べる
		/// </summary>
		public bool IsChannelPrefix
		{
			get
			{
				return KindRaw == (int)Kinds.ChannelPrefix;
			}
		}

		/// <summary>
		/// ポートプレフィックスであるかどうかを調べる
		/// </summary>
		public bool IsPortPrefix
		{
			get
			{
				return KindRaw == (int)Kinds.PortPrefix;
			}
		}

		/// <summary>
		/// エンドオブトラックであるかどうかを調べる
		/// </summary>
		public bool IsEndofTrack
		{
			get
			{
				return KindRaw == (int)Kinds.EndofTrack;
			}
		}

		/// <summary>
		/// テンポイベントであるかどうかを調べる
		/// </summary>
		public bool IsTempo
		{
			get
			{
				return KindRaw == (int)Kinds.Tempo;
			}
		}

		/// <summary>
		/// SMPTEオフセットイベントであるかどうかを調べる
		/// </summary>
		public bool IsSMPTEOffset
		{
			get
			{
				return KindRaw == (int)Kinds.SMPTEOffset;
			}
		}

		/// <summary>
		/// 拍子記号イベントであるかどうかを調べる
		/// </summary>
		public bool IsTimeSignature
		{
			get
			{
				return KindRaw == (int)Kinds.TimeSignature;
			}
		}

		/// <summary>
		/// 調性記号イベントであるかどうかを調べる
		/// </summary>
		public bool IsKeySignature
		{
			get
			{
				return KindRaw == (int)Kinds.KeySignature;
			}
		}

		/// <summary>
		/// シーケンサ独自のイベントであるかどうかを調べる
		/// </summary>
		public bool IsSequencerSpecific
		{
			get
			{
				return KindRaw == (int)Kinds.SequencerSpecific;
			}
		}

		/// <summary>
		/// MIDIイベントであるかどうかを調べる
		/// </summary>
		public bool IsMIDIEvent
		{
			get
			{
				return 0x80 <= KindRaw && KindRaw <= 0xEF;
			}
		}

		/// <summary>
		/// NOTEイベントであるかどうかを調べる。
		/// これはノートオンとノートオフが結合イベントしたイベントでなければならない。
		/// </summary>
		public bool IsNote
		{
			get
			{
				if (!IsCombined)
				{
					return false;
				}
				Event noteOnEvent = FirstCombinedEvent;
				if (noteOnEvent == null)
				{
					return false;
				}
				if (!noteOnEvent.IsNoteOn)
				{
					return false;
				}
				Event noteOffEvent = noteOnEvent.NextCombinedEvent;
				if (noteOffEvent == null)
				{
					return false;
				}
				if (!noteOffEvent.IsNoteOff)
				{
					return false;
				}
				if (noteOffEvent.NextCombinedEvent != null)
				{
					return false;
				}
				return true;
			}
		}

		/// <summary>
		/// NOTEONOTEOFFイベントであるかどうかを調べる。
		/// これはノートオン(0x9n)とノートオフ(0x8n)が結合イベントしたイベントでなければならない。
		/// </summary>
		public bool IsNoteOnNoteOff
		{
			get
			{
				if (!IsCombined)
				{
					return false;
				}
				Event noteOnEvent = FirstCombinedEvent;
				if (noteOnEvent == null)
				{
					return false;
				}
				if (!noteOnEvent.IsNoteOn)
				{
					return false;
				}
				Event noteOffEvent = noteOnEvent.NextCombinedEvent;
				if (noteOffEvent == null)
				{
					return false;
				}
				if (!(0x80 <= noteOffEvent.KindRaw && noteOffEvent.KindRaw <= 0x8F))
				{
					return false;
				}
				if (noteOffEvent.NextCombinedEvent != null)
				{
					return false;
				}
				return true;
			}
		}

		/// <summary>
		/// NOTEONNOTEON0イベントであるかどうかを調べる。
		/// これはノートオン(0x9n)とノートオフ(0x9n,vel==0)が結合イベントしたイベントでなければならない。
		/// </summary>
		public bool IsNoteOnNoteOn0
		{
			get
			{
				if (!IsCombined)
				{
					return false;
				}
				Event noteOnEvent = FirstCombinedEvent;
				if (noteOnEvent == null)
				{
					return false;
				}
				if (!noteOnEvent.IsNoteOn)
				{
					return false;
				}
				Event noteOffEvent = noteOnEvent.NextCombinedEvent;
				if (noteOffEvent == null)
				{
					return false;
				}
				if (!(0x90 <= noteOffEvent.KindRaw && noteOffEvent.KindRaw <= 0x9F))
				{
					return false;
				}
				if (noteOffEvent.Data[2] != 0)
				{
					return false;
				}
				if (noteOffEvent.NextCombinedEvent != null)
				{
					return false;
				}
				return true;
			}
		}

		public bool IsNoteOff
		{
			get
			{
				if (0x80 <= KindRaw && KindRaw <= 0x8F)
				{
					return true;
				}
				if (0x90 <= KindRaw && KindRaw <= 0x9F)
				{
					if (Data[2] > 0)
					{
						return false;
					}
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// ノートオンイベントであるかどうかを調べる。
		/// (ノートオンイベントでもベロシティ0のものはノートオフイベントとみなす。) 
		/// </summary>
		public bool IsNoteOn
		{
			get
			{
				if (0x90 <= KindRaw && KindRaw <= 0x9F)
				{
					if (Data[2] > 0)
					{
						return true;
					}
					return false;
				}
				return false;
			}
		}

		/// <summary>
		/// キーアフタータッチイベントであるかどうかを調べる
		/// </summary>
		public bool IsKeyAftertouch
		{
			get
			{
				return 0xA0 <= KindRaw && KindRaw <= 0xAF;
			}
		}

		/// <summary>
		/// コントロールチェンジイベントであるかどうかを調べる
		/// </summary>
		public bool IsControlChange
		{
			get
			{
				return 0xB0 <= KindRaw && KindRaw <= 0xBF;
			}
		}

		/// <summary>
		/// プログラムチェンジイベントであるかどうかを調べる
		/// </summary>
		public bool IsProgramChange
		{
			get
			{
				return 0xC0 <= KindRaw && KindRaw <= 0xCF;
			}
		}

		/// <summary>
		/// チャンネルアフターイベントであるかどうかを調べる
		/// </summary>
		public bool IsChannelAftertouch
		{
			get
			{
				return 0xD0 <= KindRaw && KindRaw <= 0xDF;
			}
		}

		/// <summary>
		/// ピッチベンドイベントであるかどうかを調べる
		/// </summary>
		public bool IsPitchBend
		{
			get
			{
				return 0xE0 <= KindRaw && KindRaw <= 0xEF;
			}
		}

		/// <summary>
		/// システムエクスクルーシヴイベントであるかどうかを調べる
		/// </summary>
		public bool IsSysExEvent
		{
			get
			{
				return KindRaw == 0xF0 || KindRaw == 0xF7;
			}
		}

		public bool IsFloating
		{
			get
			{
				return Parent == null;
			}
		}

		/// <summary>
		/// 結合イベントであるかどうか調べる
		/// </summary>
		public bool IsCombined
		{
			get
			{
				return PrevCombinedEvent != null || NextCombinedEvent != null;
			}
		}

		public int Key { get; private set; }

		public int Channel { get; private set; }

		public CharCodes CharCode { get; private set; }

		public string Text { get; private set; }


		#region コンストラクタ
		/// <summary>
		/// デフォルトコンストラクタ
		/// </summary>
		private Event() { }

		/// <summary>
		/// MIDIイベント(任意)を生成する。
		/// </summary>
		/// <param name="time">挿入時刻[tick]</param>
		/// <param name="kind">イベントの種類</param>
		/// <param name="data">初期データ(null禁止)</param>
		private Event(int time, int kind, byte[] data)
		{
			/* 引数の正当性チェック */
			if (time < 0)
			{
				throw new ArgumentOutOfRangeException(null, "時刻は、0以上である必要があります。");
			}
			if (kind < 0 || 256 <= kind)
			{
				throw new ArgumentOutOfRangeException(null, "種類は、0から255の範囲内である必要があります。");
			}
			if (data == null)
			{
				throw new ArgumentNullException(nameof(data));
			}
			/* MIDIチャンネルイベントは3バイト以下でなければならない */
			if (0x80 <= kind && kind <= 0xEF && data.Length >= 4)
			{
				throw new ArgumentException("MIDIチャンネルイベントは3バイト以下である必要があります。");
			}

			Time = time;
			KindRaw = kind;
			int len = data.Length;
			/* pDataにランニングステータスが含まれていない場合の措置 */
			if (((0x80 <= kind && kind <= 0xEF) && (0 <= data[0] && data[0] <= 127)) ||
				((kind == 0xF0) && (0 <= data[0] && data[0] <= 127)))
			{
				len++;
			}

			this.Data = new byte[len];

			/* 接続ポインタの初期化 */
			NextEvent = null;
			PrevEvent = null;
			NextSameKindEvent = null;
			PrevSameKindEvent = null;
			NextCombinedEvent = null;
			PrevCombinedEvent = null;
			Parent = null;

			/* pDataにランニングステータスが含まれていない場合の措置 */
			if ((0x80 <= kind && kind <= 0xEF && 0 <= data[0] && data[0] <= 127) ||
				((kind == 0xF0) && 0 <= data[0] && data[0] <= 127))
			{
				if (this.Data != null)
				{
					this.Data[0] = (byte)kind;
				}
				if (this.Data != null && len - 1 > 0)
				{
					data.CopyTo(this.Data, 1);
				}
			}
			/* 通常の場合 */
			else
			{
				/* MIDIチャンネルイベントのイベントの種類のチャンネル情報は、データ部に合わせる */
				if (0x80 <= this.KindRaw && this.KindRaw <= 0xEF)
				{
					this.KindRaw &= 0xF0;
					this.KindRaw |= data[0] & 0x0F;
				}
				if (this.Data != null && len > 0)
				{ 
					data.CopyTo(this.Data, 0);
				}
			}
		}

		/// <summary>
		/// MIDIイベント(任意)を生成する。
		/// </summary>
		/// <param name="time">挿入時刻[tick]</param>
		/// <param name="kind">イベントの種類</param>
		/// <param name="data">初期データ</param>
		public Event(int time, Kinds kind, byte[] data) : this(time, (int)kind, data) { }
		#endregion


		/// <summary>
		/// 次の同じ種類のイベントを探索
		/// </summary>
		/// <returns>次の同種のイベント</returns>
		internal Event SearchNextSameKindEvent()
		{
			Event sameKindEvent = this.NextEvent;
			while (sameKindEvent != null)
			{
				if (this.KindRaw == sameKindEvent.KindRaw)
				{
					break;
				}
				sameKindEvent = sameKindEvent.NextEvent;
			}
			return sameKindEvent;
		}

		/// <summary>
		/// 前の同じ種類のイベントを探索
		/// </summary>
		/// <returns>前の同種のイベント</returns>
		internal Event SearchPrevSameKindEvent()
		{
			Event sameKindEvent = this.PrevEvent;
			while (sameKindEvent != null)
			{
				if (this.KindRaw == sameKindEvent.KindRaw)
				{
					break;
				}
				sameKindEvent = sameKindEvent.PrevEvent;
			}
			return sameKindEvent;
		}

		/// <summary>
		/// 結合イベントの最初のイベントを返す。
		/// </summary>
		/// <returns>結合イベントの最初のイベント</returns>
		/// <remarks>結合イベントでない場合、自分自身を返す。</remarks>
		public Event GetFirstCombinedEvent()
		{
			Event tempEvent = this;
			while (tempEvent.PrevCombinedEvent != null)
			{
				tempEvent = tempEvent.PrevCombinedEvent;
			}
			return tempEvent;
		}

		/// <summary>
		/// 結合イベントの最後のイベントを返す。
		/// </summary>
		/// <returns>結合イベントの最後のイベント</returns>
		/// <remarks>結合イベントでない場合、自分自身を返す。</remarks>
		public Event GetLastCombinedEvent()
		{
			Event tempEvent = this;
			while (tempEvent.NextCombinedEvent != null)
			{
				tempEvent = tempEvent.NextCombinedEvent;
			}
			return tempEvent;
		}

		/// <summary>
		/// イベントを一時的に浮遊させる
		/// </summary>
		internal void SetFloating()
		{
			/* ただし、結合イベントの解除は行わないことに要注意 */
			/* 前後のイベントのポインタのつなぎ替え */
			if (this.PrevEvent != null)
			{
				this.PrevEvent.NextEvent = this.NextEvent;
			}
			else if (this.Parent != null)
			{
				this.Parent.FirstEvent = this.NextEvent;
			}
			if (this.NextEvent != null)
			{
				this.NextEvent.PrevEvent = this.PrevEvent;
			}
			else if (this.Parent != null)
			{
				this.Parent.LastEvent = this.PrevEvent;
			}
			/* 前後の同種イベントのポインタのつなぎ替え */
			if (this.NextSameKindEvent != null)
			{
				this.NextSameKindEvent.PrevSameKindEvent = this.PrevSameKindEvent;
			}
			if (this.PrevSameKindEvent != null)
			{
				this.PrevSameKindEvent.NextSameKindEvent = this.NextSameKindEvent;
			}
			/* 前後ポインタのnull化 */
			this.NextEvent = null;
			this.PrevEvent = null;
			/* 前後の同種イベントポインタnull化 */
			this.NextSameKindEvent = null;
			this.PrevSameKindEvent = null;
			/* 親トラックのイベント数を1減らす。 */
			if (this.Parent != null)
			{
				this.Parent.NumEvent--;
			}
			this.Parent = null;
		}

		/// <summary>
		/// 前のイベントを設定する
		/// </summary>
		/// <param name="insertEvent">前のイベント</param>
		internal void SetPrevEvent(Event insertEvent)
		{
			if (this == insertEvent)
			{
				throw new MIDIDataLibException("同じイベントオブジェクトを追加することはできません。");
			}
			/* pInsertEventが既にどこかのトラックに属している場合、異常終了 */
			if (!insertEvent.IsFloating)
			{
				throw new MIDIDataLibException("insertEventは既にどこかのトラックに属しています。");
			}
			/* EOTイベントの前に挿入する場合、EOTイベントの時刻を補正する */
			if (this.IsEndofTrack && this.NextEvent == null)
			{
				if (this.Time < insertEvent.Time)
				{ /* 20080622追加 */
					this.Time = insertEvent.Time;
				}
			}
			/* 時刻の整合性がとれていない場合、自動的に挿入イベントの時刻を補正する */
			if (insertEvent.Time > this.Time)
			{
				insertEvent.Time = this.Time;
			}
			if (this.PrevEvent != null)
			{
				if (insertEvent.Time < this.PrevEvent.Time)
				{
					insertEvent.Time = this.PrevEvent.Time;
				}
			}
			/* 前後のイベントのポインタのつなぎかえ */
			insertEvent.NextEvent = this;
			insertEvent.PrevEvent = this.PrevEvent;
			if (this.PrevEvent != null)
			{
				this.PrevEvent.NextEvent = insertEvent;
			}
			else if (this.Parent != null)
			{
				this.Parent.FirstEvent = insertEvent;
			}
			this.PrevEvent = insertEvent;
			/* 前後の同種イベントのポインタのつなぎかえ */
			if (insertEvent.PrevSameKindEvent != null)
			{
				insertEvent.PrevSameKindEvent.NextSameKindEvent =
					insertEvent.PrevSameKindEvent.SearchNextSameKindEvent();
			}
			if (insertEvent.NextSameKindEvent != null)
			{
				insertEvent.NextSameKindEvent.PrevSameKindEvent =
					insertEvent.NextSameKindEvent.SearchPrevSameKindEvent();
			}
			/* 前後の同種イベントポインタ設定 */
			insertEvent.PrevSameKindEvent = insertEvent.SearchPrevSameKindEvent();
			if (insertEvent.PrevSameKindEvent != null)
			{
				insertEvent.PrevSameKindEvent.NextSameKindEvent = insertEvent;
			}
			insertEvent.NextSameKindEvent = insertEvent.SearchNextSameKindEvent();
			if (insertEvent.NextSameKindEvent != null)
			{
				insertEvent.NextSameKindEvent.PrevSameKindEvent = insertEvent;
			}
			/* 親トラックのイベント数を1多くする */
			insertEvent.Parent = this.Parent;
			if (this.Parent != null)
			{
				this.Parent.NumEvent++;
			}
		}

		/// <summary>
		/// 次のイベントを設定する
		/// </summary>
		/// <param name="insertEvent">次のイベント</param>
		internal void SetNextEvent(Event insertEvent)
		{
			if (this == insertEvent)
			{
				throw new MIDIDataLibException("同じイベントオブジェクトを追加することはできません。");
			}
			/* pInsertEventが既にどこかのトラックに属している場合、異常終了 */
			if (!insertEvent.IsFloating)
			{
				throw new MIDIDataLibException("insertEventは既にどこかのトラックに属しています。");
			}
			/* EOTの後にイベントを入れようとした場合、EOTが後ろに移動しない。 */
			if (this.IsEndofTrack && this.NextEvent == null)
			{
				throw new MIDIDataLibException("エンドオブトラックのあとに、イベントを挿入することはできません。");
			}
			/* 時刻の整合性がとれていない場合、自動的に挿入イベントの時刻を補正する */
			if (insertEvent.Time < this.Time)
			{
				insertEvent.Time = this.Time;
			}
			if (this.NextEvent != null)
			{
				if (insertEvent.Time > this.NextEvent.Time)
				{
					insertEvent.Time = this.NextEvent.Time;
				}
			}
			/* 前後のイベントのポインタのつなぎかえ */
			insertEvent.NextEvent = this.NextEvent;
			insertEvent.PrevEvent = this;
			if (this.NextEvent != null)
			{
				this.NextEvent.PrevEvent = insertEvent;
			}
			else if (this.Parent != null)
			{ /* 最後 */
				this.Parent.LastEvent = insertEvent;
			}
			this.NextEvent = insertEvent;
			/* 前後の同種イベントのポインタのつなぎかえ */
			if (insertEvent.PrevSameKindEvent != null)
			{
				insertEvent.PrevSameKindEvent.NextSameKindEvent =
					insertEvent.PrevSameKindEvent.SearchNextSameKindEvent();
			}
			if (insertEvent.NextSameKindEvent != null)
			{
				insertEvent.NextSameKindEvent.PrevSameKindEvent =
					insertEvent.NextSameKindEvent.SearchPrevSameKindEvent();
			}
			/* 前後の同種イベントポインタ設定 */
			insertEvent.PrevSameKindEvent = insertEvent.SearchPrevSameKindEvent();
			if (insertEvent.PrevSameKindEvent != null)
			{
				insertEvent.PrevSameKindEvent.NextSameKindEvent = insertEvent;
			}
			insertEvent.NextSameKindEvent = insertEvent.SearchNextSameKindEvent();
			if (insertEvent.NextSameKindEvent != null)
			{
				insertEvent.NextSameKindEvent.PrevSameKindEvent = insertEvent;
			}
			/* 親トラックのイベント数を1多くする。 */
			insertEvent.Parent = this.Parent;
			if (this.Parent != null)
			{
				this.Parent.NumEvent++;
			}
		}

		/// <summary>
		/// 単一のクローンイベントの作成
		/// </summary>
		/// <returns>クローンされたイベント</returns>
		internal　Event CreateCloneSingle()
		{
			Event newEvent = new Event();
			newEvent.Time = this.Time;
			newEvent.KindRaw = this.KindRaw;

			newEvent.Data = new byte[this.Data.Length];
			this.Data.CopyTo(newEvent.Data, 0);

			newEvent.Parent = null;
			newEvent.NextEvent = null;
			newEvent.PrevEvent = null;
			newEvent.NextSameKindEvent = null;
			newEvent.PrevSameKindEvent = null;
			newEvent.NextCombinedEvent = null;
			newEvent.PrevCombinedEvent = null;
			return newEvent;
		}

		/// <summary>
		/// イベントを結合する
		/// </summary>
		public void Combine()
		{
			/* ノート化：ノートオン+ノートオフ */
			/* 既に結合されてる場合は異常終了 */
			if (this.IsCombined)
			{
				//例外を投げない
				return;
			}
			/* 次の(a)と(b)は同一ループ内では混用しないでください。 */
			/* 次の(a)か(b)によっていったん結合したら、chopしない限りそれ以上は結合できません。 */
			/* ノートオンイベントにノートオフイベントを結合(a) */
			if (this.IsNoteOn)
			{
				Event noteOff = this;
				while ((noteOff = noteOff.NextEvent) != null)
				{
					if (noteOff.IsNoteOff && !noteOff.IsCombined)
					{
						if (noteOff.Key == this.Key &&
							noteOff.Channel == this.Channel)
						{
							this.NextCombinedEvent = noteOff;
							noteOff.PrevCombinedEvent = this;
						}
					}
				}
			}
			/* ノートオフイベントにノートオンイベントを結合(b) */
			else if (this.IsNoteOff)
			{
				Event noteOn = this;
				while ((noteOn = noteOn.PrevEvent) != null)
				{
					if (noteOn.IsNoteOn && !noteOn.IsCombined)
					{
						if (noteOn.Key == this.Key &&
							noteOn.Channel == this.Channel)
						{
							this.PrevCombinedEvent = noteOn;
							noteOn.NextCombinedEvent = this;
						}
					}
				}
			}
		}

		/// <summary>
		/// 結合イベントを切り離す
		/// </summary>
		public void Chop()
		{
			Event tempEvent = null;
			Event explodeEvent = null;
			/* 結合イベントでない場合は異常終了 */
			if (!this.IsCombined)
			{
				//例外を投げない。
				return;
			}
			/* 最初の結合から順番に切り離す */
			explodeEvent = this.FirstCombinedEvent;
			while (explodeEvent != null)
			{
				tempEvent = explodeEvent.NextCombinedEvent;
				explodeEvent.PrevCombinedEvent = null;
				explodeEvent.NextCombinedEvent = null;
				explodeEvent = tempEvent;
			}
		}

		/// <summary>
		/// MIDIイベントの削除(結合している場合でも単一のMIDIイベントを削除)
		/// </summary>
		public void DeleteSingle()
		{
			/* 結合イベントの切り離し */
			if (this.NextCombinedEvent != null)
			{
				this.NextCombinedEvent.PrevCombinedEvent = this.PrevCombinedEvent;
			}
			if (this.PrevCombinedEvent != null)
			{
				this.PrevCombinedEvent.NextCombinedEvent = this.NextCombinedEvent;
			}
			/* 前後接続ポインタのつなぎ替え */
			if (this.NextEvent != null)
			{
				this.NextEvent.PrevEvent = this.PrevEvent;
			}
			else if (this.Parent != null)
			{
				this.Parent.LastEvent = this.PrevEvent;
			}
			if (this.PrevEvent != null)
			{
				this.PrevEvent.NextEvent = this.NextEvent;
			}
			else if (this.Parent != null)
			{
				this.Parent.FirstEvent = this.NextEvent;
			}
			/* 前後同種イベント接続ポインタのつなぎ替え */
			if (this.NextSameKindEvent != null)
			{
				this.NextSameKindEvent.PrevSameKindEvent = this.PrevSameKindEvent;
			}
			if (this.PrevSameKindEvent != null)
			{
				this.PrevSameKindEvent.NextSameKindEvent = this.NextSameKindEvent;
			}
			/* 前後結合イベントポインタのつなぎ替え */
			if (this.NextCombinedEvent != null)
			{
				this.NextCombinedEvent.PrevCombinedEvent = this.PrevCombinedEvent;
			}
			if (this.PrevCombinedEvent != null)
			{
				this.PrevCombinedEvent.NextCombinedEvent = this.NextCombinedEvent;
			}
			/* このイベントの他のイベントへの参照をすべてnull化 */
			this.NextEvent = null;
			this.PrevEvent = null;
			this.NextSameKindEvent = null;
			this.PrevSameKindEvent = null;
			this.NextCombinedEvent = null;
			this.PrevCombinedEvent = null;
			/* 親トラックのイベント数デクリメント */
			if (this.Parent != null)
			{
				this.Parent.NumEvent--;
			}
			this.Parent = null;
		}

		/// <summary>
		/// MIDIイベントの削除(結合している場合、結合しているMIDIイベントも削除)
		/// </summary>
		public void Delete()
		{
			Event deleteEvent = this;
			Event tempEvent = null;
			deleteEvent = this.FirstCombinedEvent;
			while (deleteEvent != null)
			{
				tempEvent = deleteEvent.NextCombinedEvent;
				deleteEvent.DeleteSingle();
				deleteEvent = tempEvent;
			}
		}

		/// <summary>
		/// 指定イベントに結合しているイベントの削除
		/// </summary>
		internal void DeleteCombinedEvent()
		{
			Event deleteEvent = null;
			Event tempEvent = null;
			/* このイベントより前の結合イベントを削除 */
			deleteEvent = this.PrevCombinedEvent;
			while (deleteEvent != null)
			{
				tempEvent = deleteEvent.PrevCombinedEvent;
				deleteEvent.DeleteSingle();
				deleteEvent = tempEvent;
			}
			/* このイベントより後の結合イベントを削除 */
			deleteEvent = this.NextCombinedEvent;
			while (deleteEvent != null)
			{
				tempEvent = deleteEvent.NextCombinedEvent;
				deleteEvent.DeleteSingle();
				deleteEvent = tempEvent;
			}
		}

		/// <summary>
		/// クローンイベントの作成
		/// </summary>
		/// <returns>クローンサれたイベント</returns>
		/// <remarks>結合イベントの場合、全く同じ結合イベントを作成する。</remarks>
		public Event CreateClone()
		{
			int i = 0;
			int position = 0;
			Event newEvent = null;
			Event sourceEvent = null;
			Event prevEvent = null;

			/* 結合イベントの場合最初のイベントを取得 */
			sourceEvent = this;
			while (sourceEvent.PrevCombinedEvent != null)
			{
				sourceEvent = sourceEvent.PrevCombinedEvent;
				position++;
			}
			/* 最初のイベントから順にひとつづつクローンを作成 */
			while (sourceEvent != null)
			{
				newEvent = sourceEvent.CreateCloneSingle();
				if (newEvent == null)
				{
					if (prevEvent != null)
					{
						Event deleteEvent = prevEvent.GetFirstCombinedEvent();
						deleteEvent.Delete();
					}
					return null;
				}
				/* 結合イベントポインタの処理 */
				if (prevEvent != null)
				{
					prevEvent.NextCombinedEvent = newEvent;
				}
				newEvent.PrevCombinedEvent = prevEvent;
				newEvent.NextCombinedEvent = null;
				/* 次のイベントへ進める */
				sourceEvent = sourceEvent.NextCombinedEvent;
				prevEvent = newEvent;
			}
			/* 戻り値は新しく作成した結合イベントのthisに対応するイベント(20081124変更) */
			newEvent = newEvent.GetFirstCombinedEvent();
			for (i = 0; i < position; i++)
			{
				newEvent = newEvent.NextCombinedEvent;
			}
			return newEvent;
		}

		/// <summary>
		/// シーケンスナンバーイベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="number">シーケンスナンバー</param>
		/// <returns>シーケンスナンバーイベント</returns>
		static public Event CreateSequenceNumber(int time, int number)
		{
			number = Clip(0, number, 65535);

			byte[] c = new byte[2];
			c[0] = (byte)((number & 0xFF00) >> 8);
			c[1] = (byte)(number & 0x00FF);
			return new Event(time, Kinds.SequenceNumber, c);
		}

		/// <summary>
		/// テキストベースのイベントの生成(文字コード指定)
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="kind">イベント種類</param>
		/// <param name="charCode">文字コード</param>
		/// <param name="text">テキスト</param>
		/// <returns>テキストベースのイベント</returns>
		static private Event CreateTextBasedEvent(int time, int kind, CharCodes charCode, string text)
		{
			if (kind <= 0x00 || kind >= 0x1F)
			{
				throw new MIDIDataLibException("指定のイベント種類は、テキストイベントではありません。");
			}
			Event CreateEvent = new Event(time, kind, new byte[0]);
			CreateEvent.CharCode = charCode;
			CreateEvent.Text =  text;
			return CreateEvent;
		}

		/// <summary>
		/// テキストベースのイベントの生成(文字コード指定)
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="kind">イベント種類</param>
		/// <param name="charCode">文字コード</param>
		/// <param name="text">テキスト</param>
		/// <returns>テキストベースのイベント</returns>
		static private Event CreateTextBasedEvent(int time, Kinds kind, CharCodes charCode, string text)
		{
			return CreateTextBasedEvent(time, (int)kind, charCode, text);
		}

		/// <summary>
		/// テキストベースのイベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="kind">イベント種類</param>
		/// <param name="text">テキスト</param>
		/// <returns>テキストベースのイベント</returns>
		static private Event CreateTextBasedEvent(int time, int kind, string text)
		{
			return Event.CreateTextBasedEvent(time, kind, CharCodes.NoCharCod, text);
		}

		/// <summary>
		/// テキストイベントの生成(文字コード指定あり)
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="charCode">文字コード</param>
		/// <param name="text">テキスト</param>
		/// <returns>テキストイベント</returns>
		static public Event CreateTextEvent(int time, CharCodes charCode, string text)
		{
			return CreateTextBasedEvent(time, Kinds.TextEvent , charCode, text);
		}

		/// <summary>
		/// テキストイベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="text">テキスト</param>
		/// <returns>テキストイベント</returns>
		static public Event  CreateTextEvent(int time, string text) 
		{
			return CreateTextEvent(time, CharCodes.NoCharCod, text);
		}

		/// <summary>
		/// 著作権イベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="charCode">文字コード</param>
		/// <param name="text">著作権情報</param>
		/// <returns>著作権イベント</returns>
		static public Event CreateCopyrightNotice(int time, CharCodes charCode, string text)
		{
			return CreateTextBasedEvent(time, Kinds.CopyrightNotice, charCode, text);
		}

		/// <summary>
		/// 著作権イベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="text">著作権情報</param>
		/// <returns>著作権イベント</returns>
		static public Event CreateCopyrightNotice(int time, string text) 
		{
			return CreateCopyrightNotice(time, CharCodes.NoCharCod, text);
		}

		/// <summary>
		/// トラック名イベントの生成(文字コード指定あり)
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="charCode">文字コード</param>
		/// <param name="text">トラック名</param>
		/// <returns>トラック名イベント</returns>
		static public　Event CreateTrackName(int time, CharCodes charCode, string text)
		{
			return CreateTextBasedEvent(time, Kinds.TrackName, charCode, text);
		}

		/// <summary>
		/// トラック名イベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="text">トラック名</param>
		/// <returns>トラック名イベント</returns>
		static public Event CreateTrackName(int time, string text)
		{
			return CreateTrackName(time, CharCodes.NoCharCod, text);
		}

		/// <summary>
		/// インストゥルメントイベントの生成(文字コード指定あり)
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="charCode">文字コード</param>
		/// <param name="text">インストゥルメント名</param>
		/// <returns>インストゥルメントイベント</returns>
		static public Event CreateInstrumentName(int time, CharCodes charCode, string text)
		{
			return CreateTextBasedEvent(time, Kinds.InstrumentName, charCode, text);
		}

		/// <summary>
		/// インストゥルメントイベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="text">インストゥルメント名</param>
		/// <returns>インストゥルメントイベント</returns>
		static public Event CreateInstrumentName(int time, string text)
		{
			return CreateInstrumentName(time, CharCodes.NoCharCod, text);
		}

		/// <summary>
		/// 歌詞イベントの生成(文字コード指定あり)
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="charCode">文字コード</param>
		/// <param name="text">歌詞</param>
		/// <returns>歌詞イベント</returns>
		static public Event CreateLyric(int time, CharCodes charCode, string text)
		{
			return CreateTextBasedEvent(time, Kinds.Lyric, charCode, text);
		}

		/// <summary>
		/// 歌詞イベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="text">歌詞</param>
		/// <returns>歌詞イベント</returns>
		static public Event CreateLyric(int time, string text)
		{
			return CreateLyric(time, CharCodes.NoCharCod, text);
		}

		/// <summary>
		/// マーカーイベントの生成(文字コード指定あり)
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="charCode">文字コード</param>
		/// <param name="text">マーカー情報</param>
		/// <returns>マーカーイベント</returns>
		static public Event CreateMarker(int time, CharCodes charCode, string text)
		{
			return CreateTextBasedEvent(time, Kinds.Marker, charCode, text);
		}

		/// <summary>
		/// マーカーイベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="text">マーカー情報</param>
		/// <returns>マーカーイベント</returns>
		static public Event CreateMarker(int time, string text)
		{
			return CreateMarker(time, CharCodes.NoCharCod, text);
		}

		/// <summary>
		/// キューポイントイベントの生成(文字コード指定あり)
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="charCode">文字コード</param>
		/// <param name="text">キューポイント情報</param>
		/// <returns>キューポイントイベント</returns>
		static public Event CreateCuePoint(int time, CharCodes charCode, string text)
		{
			return CreateTextBasedEvent(time, Kinds.CuePoint, charCode, text);
		}

		/// <summary>
		/// キューポイントイベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="text">キューポイント情報</param>
		/// <returns>キューポイントイベント</returns>
		static public Event CreateCuePoint(int time, string text)
		{
			return CreateCuePoint(time, CharCodes.NoCharCod, text);
		}

		/// <summary>
		/// プログラム名イベントの生成(文字コード指定あり)
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="charCode">文字コード</param>
		/// <param name="text">プログラム名</param>
		/// <returns>プログラム名イベント</returns>
		static public Event CreateProgramName(int time, CharCodes charCode, string text)
		{
			return CreateTextBasedEvent(time, Kinds.ProgramName, charCode, text);
		}

		/// <summary>
		/// プログラム名イベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="text">プログラム名</param>
		/// <returns>プログラム名イベント</returns>
		static public Event CreateProgramName(int time, string text)
		{
			return CreateProgramName(time, CharCodes.NoCharCod, text);
		}

		/// <summary>
		/// デバイス名イベント生成(文字コード指定あり)
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="charCode">文字コード</param>
		/// <param name="text">デバイス名</param>
		/// <returns>デバイス名イベント</returns>
		static public Event CreateDeviceName(int time, CharCodes charCode, string text) 
		{
			return CreateTextBasedEvent(time, Kinds.DeviceName, charCode, text);
		}

		/// <summary>
		/// デバイス名イベント生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="text">デバイス名</param>
		/// <returns>デバイス名イベント</returns>
		static public Event CreateDeviceName(int time, string text)
		{
			return CreateDeviceName(time, CharCodes.NoCharCod, text);
		}

		/*  */
		/// <summary>
		/// チャンネルプレフィックスの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="ch">チャンネル</param>
		/// <returns>チャンネルプレフィックス</returns>
		static public Event CreateChannelPrefix(int time, int ch)
		{
			byte[] c = new byte[1];
			c[0] = (byte)Clip(0, ch, 16);
			return new Event(time, Kinds.ChannelPrefix, c);
		}

		/// <summary>
		/// ポートプレフィックスの生成 
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="num">ポートプレフィックス</param>
		/// <returns>ポートプレフィックス</returns>
		static public Event CreatePortPrefix(int time, int num)
		{
			byte[] c = new byte[1];
			c[0] = (byte)Clip(0, num, 255);
			return new Event(time, Kinds.PortPrefix, c);
		}

		/// <summary>
		/// エンドオブトラックイベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <returns>エンドオブトラックイベント</returns>
		static public Event CreateEndofTrack(int time)
		{
			return new Event(time, Kinds.EndofTrack, new byte[0]);
		}

		/// <summary>
		/// テンポイベントの生成(lTempo = 60000000/BPMとする)
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="tempo">テンポ</param>
		/// <returns>テンポイベント</returns>
		static public Event CreateTempo(int time, int tempo)
		{
			byte[] c = new byte[3];
			c[0] = (byte)((Clip(MinTempo, tempo, MaxTempo) & 0xFF0000) >> 16);
			c[1] = (byte)((Clip(MinTempo, tempo, MaxTempo) & 0x00FF00) >> 8);
			c[2] = (byte)((Clip(MinTempo, tempo, MaxTempo) & 0x0000FF) >> 0);
			return new Event(time, Kinds.Tempo, c);
		}

		/// <summary>
		/// SMPTEオフセットイベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="mode">モード</param>
		/// <param name="hour">時間(0～23)</param>
		/// <param name="min">分(0～59)</param>
		/// <param name="sec">秒(0～59)</param>
		/// <param name="frame">フレーム(0～30※)</param>
		/// <param name="subFrame">サブフレーム(0～99)</param>
		/// <returns>SMPTEオフセットイベント</returns>
		static private Event CreateSMPTEOffset(int time, SMPTE mode, int hour, int min, int sec, int frame, int subFrame)
		{
			int[] maxFrame = { 23, 24, 29, 29 };
			byte[] c = new byte[5];
			c[0] = (byte)((((int)mode & 0x03) << 5) | (Clip(0, hour, 23)));
			c[1] = (byte)Clip(0, min, 59);
			c[2] = (byte)Clip(0, sec, 59);
			c[3] = (byte)Clip(0, frame, maxFrame[(int)mode & 0x03]);
			c[4] = (byte)Clip(0, subFrame, 99);
			return new Event(time, Kinds.SMPTEOffset, c);
		}

		/// <summary>
		/// SMPTEオフセットイベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="offset">SMPTEOffsetオブジェクト</param>
		/// <returns>SMPTEオフセットイベント</returns>
		static public Event CreateSMPTEOffset(int time,SMPTEOffset offset)
		{
			return CreateSMPTEOffset(time, offset.Mode, offset.Hour, offset.Min, offset.Sec, offset.Frame, offset.SubFrame);
		}

		/// <summary>
		/// 拍子イベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="nn">拍子記号の分子</param>
		/// <param name="dd">拍子記号の分母の指数部分</param>
		/// <param name="cc">1拍あたりのMIDIクロック数</param>
		/// <param name="bb">1拍の長さを32分音符の数で表す</param>
		/// <returns>拍子イベント</returns>
		static private Event CreateTimeSignature(int time, int nn, int dd, int cc, int bb)
		{
			byte[] c = new byte[4];
			c[0] = (byte)Clip(0, nn, 255);
			c[1] = (byte)Clip(0, dd, 255);
			c[2] = (byte)Clip(0, cc, 255);
			c[3] = (byte)Clip(0, bb, 255);
			return new Event(time, Kinds.TimeSignature, c);
		}

		/// <summary>
		/// 拍子イベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="timeSignature">拍子オブジェクト</param>
		/// <returns>拍子イベント</returns>
		static public Event CreateTimeSignature(int time,TimeSignature timeSignature)
		{
			return CreateTimeSignature(time, timeSignature.nn, timeSignature.dd, timeSignature.cc, timeSignature.bb);
		}

		/*  */
		/// <summary>
		/// 調性イベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="sf">#又は♭の数</param>
		/// <param name="mi">長調か短調か</param>
		/// <returns>調性イベント</returns>
		static private Event CreateKeySignature(int time, int sf, int mi)
		{
			byte[] c = new byte[2];
			c[0] = (byte)(Clip(-7, sf, +7));
			c[1] = (byte)(Clip(0, mi, 1));
			return new Event(time, Kinds.KeySignature, c);
		}

		/// <summary>
		/// 調性イベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="keySignature">調性オブジェクト</param>
		/// <returns>調性イベント</returns>
		static public Event CreateKeySignature(int time, KeySignature keySignature)
		{
			return CreateKeySignature(time, keySignature.sf, (int)keySignature.mi);
		}

		/// <summary>
		/// シーケンサ独自のイベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="buf">値</param>
		/// <returns>シーケンサ独自のイベント</returns>
		static public Event CreateSequencerSpecific(int time, byte[] buf)
		{
			return new Event(time, Kinds.SequencerSpecific, buf);
		}

		/// <summary>
		/// ノートオフイベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="ch">チャンネル</param>
		/// <param name="key">キーナンバー</param>
		/// <param name="vel">ベロシティ</param>
		/// <returns>ノートオフイベント</returns>
		static public Event CreateNoteOff(int time, int ch, int key, int vel)
		{
			byte[] c = new byte[3];
			c[0] = (byte)((int)Kinds.NoteOff | (ch & 0x0F));
			c[1] = (byte)Clip(0, key, 127);
			c[2] = (byte)Clip(0, vel, 127);
			return new Event(time, (int)Kinds.NoteOff | (ch & 0x0F), c);
		}

		/// <summary>
		/// ノートオンイベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="ch">チャンネル</param>
		/// <param name="key">キーナンバー</param>
		/// <param name="vel">ベロシティ</param>
		/// <returns>ノートオンイベント</returns>
		static public Event CreateNoteOn(int time, int ch, int key, int vel)
		{
			byte[] c = new byte[3];
			c[0] = (byte)((int)Kinds.NoteOn | (ch & 0x0F));
			//!!Clipの範囲が間違っている??
			//c[1] = (byte)(Clip(1, key, 127));
			c[1] = (byte)Clip(0, key, 127);
			c[2] = (byte)Clip(0, vel, 127);
			return new Event(time, (int)Kinds.NoteOn | (ch & 0x0F), c);
		}

		/// <summary>
		/// ノートオン(0x9n)・ノートオフ(0x8n)の2イベントを生成し、NoteOnを返す。
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="ch">チャンネル</param>
		/// <param name="key">キーナンバー</param>
		/// <param name="vel1">ノートオンイベントのベロシティ(打鍵速度)(1～127)</param>
		/// <param name="vel2">ノートオフイベントのベロシティ(離鍵速度)(0～127)</param>
		/// <param name="dur">長さ(1～)</param>
		/// <returns>ノートイベント</returns>
		static public Event CreateNoteOnNoteOff(int time, int ch, int key, int vel1, int vel2, int dur)
		{
			Event noteOnEvent;
			Event noteOffEvent;
			/* ノートオン(0x9n)イベントの生成 */
			noteOnEvent = CreateNoteOn(time, ch, key, Clip(1, vel1, 127));

			/* ノートオフ(0x8n)イベントの生成 */
			noteOffEvent = CreateNoteOff(time + dur, ch, key, vel2);

			/* 上の2イベントの結合 */
			noteOnEvent.PrevCombinedEvent = null;
			noteOnEvent.NextCombinedEvent = noteOffEvent;
			noteOffEvent.PrevCombinedEvent = noteOnEvent;
			noteOffEvent.NextCombinedEvent = null;
			return noteOnEvent;
		}

		/// <summary>
		/// ノートオン(0x9n)・ノートオン(0x9n(vel==0))の2イベントを生成し、NoteOnを返す
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="ch">チャンネル</param>
		/// <param name="key">キーナンバー</param>
		/// <param name="vel">ノートオンイベントのベロシティ(打鍵速度)(1～127)</param>
		/// <param name="dur">長さ(1～)</param>
		/// <returns>ノートイベント</returns>
		static public Event CreateNoteOnNoteOn0(int time, int ch, int key, int vel, int dur)
		{
			Event noteOnEvent;
			Event noteOffEvent;
			/* ノートオン(0x9n)イベントの生成 */
			noteOnEvent = CreateNoteOn(time, ch, key, Clip(1, vel, 127));

			/* ノートオン(0x9n, vel==0)イベントの生成 */
			noteOffEvent = CreateNoteOn(time + dur, ch, key, 0);

			/* 上の2イベントの結合 */
			noteOnEvent.PrevCombinedEvent = null;
			noteOnEvent.NextCombinedEvent = noteOffEvent;
			noteOffEvent.PrevCombinedEvent = noteOnEvent;
			noteOffEvent.NextCombinedEvent = null;
			return noteOnEvent;
		}

		/// <summary>
		/// ノートイベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="ch">チャンネル</param>
		/// <param name="key">キーナンバー</param>
		/// <param name="vel">ノートオンイベントのベロシティ(打鍵速度)(1～127)</param>
		/// <param name="dur">長さ(1～)</param>
		/// <returns>ノートイベント</returns>
		static public Event CreateNote(int time, int ch, int key, int vel, int dur)
		{
			return CreateNoteOnNoteOn0(time, ch, key, vel, dur);
		}

		/// <summary>
		/// キーアフターイベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="ch">チャンネル番号</param>
		/// <param name="key">キー値(0～127)</param>
		/// <param name="val">値(0～127)</param>
		/// <returns>キーアフタータッチイベント</returns>
		static public Event CreateKeyAftertouch(int time, int ch, int key, int val)
		{
			byte[] c = new byte[3];
			c[0] = (byte)((int)Kinds.KeyAfterTouch | (ch & 0x0F));
			c[1] = (byte)(Clip(0, key, 127));
			c[2] = (byte)(Clip(0, val, 127));
			return new Event(time, (int)Kinds.KeyAfterTouch | c[0], c);
		}

		/// <summary>
		/// コントロールチェンジイベントを生成する。
		/// </summary>
		/// <param name="time">絶対時刻</param>
		/// <param name="ch">チャンネル番号</param>
		/// <param name="num">コントロールナンバー(0～127)</param>
		/// <param name="val">値(0～127)</param>
		/// <returns>コントロールチェンジイベント</returns>
		static public Event CreateControlChange(int time, int ch, int num, int val)
		{
			byte[] c = new byte[3];
			c[0] = (byte)((int)Kinds.ControlChange | (ch & 0x0F));
			c[1] = (byte)(Clip(0, num, 127));
			c[2] = (byte)(Clip(0, val, 127));
			return new Event(time, (int)Kinds.ControlChange | c[0], c);
		}

		/// <summary>
		/// プログラムチェンジイベントを生成する。
		/// </summary>
		/// <param name="time">絶対時刻</param>
		/// <param name="ch">チャンネル番号</param>
		/// <param name="num">プログラムナンバー(0～127)</param>
		/// <returns>プログラムチェンジイベント</returns>
		Event CreateProgramChange(int time, int ch, int val)
		{
			byte[] c = new byte[2];
			c[0] = (byte)((int)Kinds.ProgramChange | (ch & 0x0F));
			c[1] = (byte)(Clip(0, val, 127));
			return new Event(time, (int)Kinds.ProgramChange | c[0], c);
		}

		/// <summary>
		/// チャンネルアフタータッチイベントを生成する。
		/// </summary>
		/// <param name="time">絶対時刻</param>
		/// <param name="ch">チャンネル番号</param>
		/// <param name="val">値(0～127)</param>
		/// <returns>チャンネルアフタータッチイベント</returns>
		Event CreateChannelAftertouch(int time, int ch, int val)
		{
			byte[] c = new byte[2];
			c[0] = (byte)((int)Kinds.ChannelAfterTouch | (ch & 0x0F));
			c[1] = (byte)(Clip(0, val, 127));
			return new Event(time, (int)Kinds.ChannelAfterTouch | c[0], c);
		}

		/// <summary>
		/// ピッチベンドイベントを生成する。
		/// </summary>
		/// <param name="time">絶対時刻</param>
		/// <param name="ch">チャンネル番号</param>
		/// <param name="val">値(0～16383)</param>
		/// <returns>ピッチベンドイベント</returns>
		Event CreatePitchBend(int time, int ch, int val)
		{
			byte[] c = new byte[3];
			c[0] = (byte)((int)Kinds.PitchBend | (ch & 0x0F));
			c[1] = (byte)(Clip(0, val, 16383) & 0x7F);
			c[2] = (byte)((Clip(0, val, 16383) >> 7) & 0x7F);
			return new Event(time, (int)Kinds.PitchBend | c[0], c);
		}

		/// <summary>
		/// システムエクスクルーシブイベントを生成する。
		/// </summary>
		/// <param name="time">絶対時刻</param>
		/// <param name="buf">データ部</param>
		/// <returns>システムエクスクルーシブイベント</returns>
		Event CreateSysExEvent(int time, byte[] buf)
		{
			if (buf[0] == 0xF0)
			{
				return new Event(time, Kinds.SysExStart, buf);
			}
			else
			{
				return new Event(time, Kinds.SysExContinue, buf);
			}
		}


		/// <summary>
		/// valをminとmaxの範囲内に収めます。
		/// </summary>
		/// <param name="min">下限</param>
		/// <param name="val">値</param>
		/// <param name="max">上限</param>
		/// <returns>
		/// valがminより小さい場合、minが返ります。
		/// valがmaxより大きい場合、maxが返ります。
		/// それ以外は、valが返ります。
		/// </returns>
		static private int Clip(int min, int val, int max)
		{
			return Math.Min(Math.Max(min, val), max);
		}
	}
}
