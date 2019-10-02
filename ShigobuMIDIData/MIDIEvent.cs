using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
		EndOfTrack = 0x2F,
		Tempo = 0x51,
		SmpteOffSet = 0x54,
		TimeSignature = 0x58,
		KeySignature = 0x59,
		SequencerSpecific = 0x7F,
		//以下、チャンネルイベント
		NoteOff = 0x80,
		NoteOff1,
		NoteOff2,
		NoteOff3,
		NoteOff4,
		NoteOff5,
		NoteOff6,
		NoteOff7,
		NoteOff8,
		NoteOff9,
		NoteOff10,
		NoteOff11,
		NoteOff12,
		NoteOff13,
		NoteOff14,
		NoteOff15,
		NoteOn = 0x90,
		NoteOn1,
		NoteOn2,
		NoteOn3,
		NoteOn4,
		NoteOn5,
		NoteOn6,
		NoteOn7,
		NoteOn8,
		NoteOn9,
		NoteOn10,
		NoteOn11,
		NoteOn12,
		NoteOn13,
		NoteOn14,
		NoteOn15,
		KeyAfterTouch = 0xA0,
		KeyAfterTouch1,
		KeyAfterTouch2,
		KeyAfterTouch3,
		KeyAfterTouch4,
		KeyAfterTouch5,
		KeyAfterTouch6,
		KeyAfterTouch7,
		KeyAfterTouch8,
		KeyAfterTouch9,
		KeyAfterTouch10,
		KeyAfterTouch11,
		KeyAfterTouch12,
		KeyAfterTouch13,
		KeyAfterTouch14,
		KeyAfterTouch15,
		ControlChange = 0xB0,
		ControlChange1,
		ControlChange2,
		ControlChange3,
		ControlChange4,
		ControlChange5,
		ControlChange6,
		ControlChange7,
		ControlChange8,
		ControlChange9,
		ControlChange10,
		ControlChange11,
		ControlChange12,
		ControlChange13,
		ControlChange14,
		ControlChange15,
		ProgramChange = 0xC0,
		ProgramChange1,
		ProgramChange2,
		ProgramChange3,
		ProgramChange4,
		ProgramChange5,
		ProgramChange6,
		ProgramChange7,
		ProgramChange8,
		ProgramChange9,
		ProgramChange10,
		ProgramChange11,
		ProgramChange12,
		ProgramChange13,
		ProgramChange14,
		ProgramChange15,
		ChannelAfterTouch = 0xD0,
		ChannelAfterTouch1,
		ChannelAfterTouch2,
		ChannelAfterTouch3,
		ChannelAfterTouch4,
		ChannelAfterTouch5,
		ChannelAfterTouch6,
		ChannelAfterTouch7,
		ChannelAfterTouch8,
		ChannelAfterTouch9,
		ChannelAfterTouch10,
		ChannelAfterTouch11,
		ChannelAfterTouch12,
		ChannelAfterTouch13,
		ChannelAfterTouch14,
		ChannelAfterTouch15,
		PitchBend = 0xE0,
		PitchBend1,
		PitchBend2,
		PitchBend3,
		PitchBend4,
		PitchBend5,
		PitchBend6,
		PitchBend7,
		PitchBend8,
		PitchBend9,
		PitchBend10,
		PitchBend11,
		PitchBend12,
		PitchBend13,
		PitchBend14,
		PitchBend15,
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

	public class Event
	{
		/// <summary>
		/// このイベントの一時的なインデックス(0から始まる)
		/// </summary>
		internal int TempIndex { get; set; }
		/// <summary>
		/// 絶対時刻[Tick]又はSMPTEサブフレーム単位
		/// </summary>
		public int Time { get; set; }
		/// <summary>
		/// イベントの種類(0x00～0xFF)
		/// </summary>
		public int Kind { get; set; }
		/// <summary>
		/// イベントのデータ
		/// </summary>
		public byte[] Data { get; set; }
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
		public bool IsFloating { get; private set; }
		public bool IsEndofTrack { get; private set; }
		public bool IsMIDIEvent { get; private set; }
		public bool IsCombined { get; private set; }
		public bool IsNoteOn { get; private set; }
		public bool IsNoteOff { get; private set; }
		public int Key { get; private set; }
		public int Channel { get; private set; }
		public CharCodes CharCode { get; private set; }
		public string Text { get; private set; }


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
		public Event(int time, int kind, byte[] data)
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
			Kind = kind;
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
				if (0x80 <= this.Kind && this.Kind <= 0xEF)
				{
					this.Kind &= 0xF0;
					this.Kind |= data[0] & 0x0F;
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

		/// <summary>
		/// 次の同じ種類のイベントを探索
		/// </summary>
		/// <returns>次の同種のイベント</returns>
		internal Event SearchNextSameKindEvent()
		{
			Event sameKindEvent = this.NextEvent;
			while (sameKindEvent != null)
			{
				if (this.Kind == sameKindEvent.Kind)
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
				if (this.Kind == sameKindEvent.Kind)
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
			newEvent.Kind = this.Kind;

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
			number = Math.Min(Math.Max(0, number), 65535);

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
			return CreateTextBasedEvent(time, (int)Kinds.TextEvent , charCode, text);
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
			return CreateTextBasedEvent(time, (int)Kinds.CopyrightNotice, charCode, text);
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
			return CreateTextBasedEvent(time, (int)Kinds.TrackName, charCode, text);
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
			return CreateTextBasedEvent(time, (int)Kinds.InstrumentName, charCode, text);
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

	}
}
