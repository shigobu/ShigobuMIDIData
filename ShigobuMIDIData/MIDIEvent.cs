using System;
using System.Text;
using System.Linq;
using System.Diagnostics;

namespace Shigobu.MIDI.DataLib
{
	/// <summary>
	/// MIDIイベントの種類
	/// </summary>
	public enum Kind
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
	public enum CharCode
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
		UTF16BE = 1201,
		/// <summary>
		/// ！！内部処理用、使用しないでください。
		/// </summary>
		NOCHARCODELATIN = 0x10000 | 1252,
		/// <summary>
		/// ！！内部処理用、使用しないでください。
		/// </summary>
		NOCHARCODEJP = 0x10000 | 932 ,
		/// <summary>
		/// ！！内部処理用、使用しないでください。
		/// </summary>
		NOCHARCODEUTF16LE = 0x10000 | 1200,
		/// <summary>
		/// ！！内部処理用、使用しないでください。
		/// </summary>
		NOCHARCODEUTF16BE = 0x10000 | 1201,
	}

	/// <summary>
	/// SMPTEフレームモード
	/// </summary>
	public enum SMPTEMode
	{
		SMPTE24 = 0x00,
		SMPTE25 = 0x01,
		SMPTE30D = 0x02,
		SMPTE30N = 0x03
	}

	/// <summary>
	/// 長調か短調かを表します。
	/// </summary>
	public enum Key
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

		#region プロパティ
		/// <summary>
		/// このイベントの一時的なインデックス(0から始まる)
		/// </summary>
		internal int TempIndex { get; set; }

		/// <summary>
		/// 絶対時刻[Tick]又はSMPTEサブフレーム単位（内部用）
		/// </summary>
		internal int _time;
		/// <summary>
		/// 絶対時刻[Tick]又はSMPTEサブフレーム単位
		/// </summary>
		public int Time
		{
			get
			{
				return _time;
			}
			set
			{
				int time = Clip(0, value, 0x7FFFFFFF);
				int deltaTime = time - this._time;
				Event moveEvent = FirstCombinedEvent;
				while (moveEvent != null)
				{
					int targetTime = moveEvent._time + deltaTime;
					targetTime = Clip(0, targetTime, 0x7FFFFFFF);
					moveEvent.SetTimeSingle(targetTime);
					moveEvent = moveEvent.NextCombinedEvent;
				}
			}
		}

		/// <summary>
		/// イベントの種類(チャンネルイベントの場合、チャンネル番号を含む)
		/// （生データ）
		/// </summary>
		public int KindRaw { get; set; }

		/// <summary>
		/// イベントの種類
		/// </summary>
		public Kind Kind
		{
			get
			{
				if (IsMIDIEvent)
				{
					//MIDIイベントの場合、チャンネル情報を削除
					return (Kind)Enum.ToObject(typeof(Kind), KindRaw & 0xF0);
				}
				else
				{
					return (Kind)Enum.ToObject(typeof(Kind), KindRaw);
				}
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

		/// <summary>
		/// 結合イベントの最初のイベントを返す。
		/// </summary>
		/// <returns>結合イベントの最初のイベント</returns>
		/// <remarks>結合イベントでない場合、自分自身を返す。</remarks>
		public Event FirstCombinedEvent
		{
			get
			{
				return GetFirstCombinedEvent();
			}
		}

		/// <summary>
		/// 結合イベントの最後のイベントを返す。
		/// </summary>
		/// <returns>結合イベントの最後のイベント</returns>
		/// <remarks>結合イベントでない場合、自分自身を返す。</remarks>
		public Event LastCombinedEvent
		{
			get
			{
				return GetLastCombinedEvent();
			}
		}

		/// <summary>
		/// 親(MIDITrackオブジェクト)
		/// </summary>
		public Track Parent { get; set; }

		#region Is系プロパティ
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
				return KindRaw <= (int)Kind.SequenceNumber && (int)Kind.SequencerSpecific <= KindRaw;
			}
		}

		/// <summary>
		/// シーケンス番号であるかどうかを調べる
		/// </summary>
		public bool IsSequenceNumber
		{
			get
			{
				return KindRaw == (int)Kind.SequenceNumber;
			}
		}

		/// <summary>
		/// テキストイベントであるかどうかを調べる
		/// </summary>
		public bool IsTextEvent
		{
			get
			{
				return KindRaw == (int)Kind.TextEvent;
			}
		}

		/// <summary>
		/// 著作権イベントであるかどうかを調べる
		/// </summary>
		public bool IsCopyrightNotice
		{
			get
			{
				return KindRaw == (int)Kind.CopyrightNotice;
			}
		}

		/// <summary>
		/// トラック名イベントであるかどうかを調べる
		/// </summary>
		public bool IsTrackName
		{
			get
			{
				return KindRaw == (int)Kind.TrackName;
			}
		}

		/// <summary>
		/// インストゥルメント名であるかどうかを調べる
		/// </summary>
		public bool IsInstrumentName
		{
			get
			{
				return KindRaw == (int)Kind.InstrumentName;
			}
		}

		/// <summary>
		/// 歌詞イベントであるかどうかを調べる
		/// </summary>
		public bool IsLyric
		{
			get
			{
				return KindRaw == (int)Kind.Lyric;
			}
		}

		/// <summary>
		/// マーカーイベントであるかどうかを調べる
		/// </summary>
		public bool IsMarker
		{
			get
			{
				return KindRaw == (int)Kind.Marker;
			}
		}

		/// <summary>
		/// キューポイントイベントであるかどうかを調べる
		/// </summary>
		public bool IsCuePoint
		{
			get
			{
				return KindRaw == (int)Kind.CuePoint;
			}
		}

		/// <summary>
		/// プログラム名であるかどうかを調べる
		/// </summary>
		public bool IsProgramName
		{
			get
			{
				return KindRaw == (int)Kind.ProgramName;
			}
		}

		/// <summary>
		/// デバイス名であるかどうかを調べる
		/// </summary>
		public bool IsDeviceName
		{
			get
			{
				return KindRaw == (int)Kind.DeviceName;
			}
		}

		/// <summary>
		/// チャンネルプレフィックスであるかどうかを調べる
		/// </summary>
		public bool IsChannelPrefix
		{
			get
			{
				return KindRaw == (int)Kind.ChannelPrefix;
			}
		}

		/// <summary>
		/// ポートプレフィックスであるかどうかを調べる
		/// </summary>
		public bool IsPortPrefix
		{
			get
			{
				return KindRaw == (int)Kind.PortPrefix;
			}
		}

		/// <summary>
		/// エンドオブトラックであるかどうかを調べる
		/// </summary>
		public bool IsEndofTrack
		{
			get
			{
				return KindRaw == (int)Kind.EndofTrack;
			}
		}

		/// <summary>
		/// テンポイベントであるかどうかを調べる
		/// </summary>
		public bool IsTempo
		{
			get
			{
				return KindRaw == (int)Kind.Tempo;
			}
		}

		/// <summary>
		/// SMPTEオフセットイベントであるかどうかを調べる
		/// </summary>
		public bool IsSMPTEOffset
		{
			get
			{
				return KindRaw == (int)Kind.SMPTEOffset;
			}
		}

		/// <summary>
		/// 拍子記号イベントであるかどうかを調べる
		/// </summary>
		public bool IsTimeSignature
		{
			get
			{
				return KindRaw == (int)Kind.TimeSignature;
			}
		}

		/// <summary>
		/// 調性記号イベントであるかどうかを調べる
		/// </summary>
		public bool IsKeySignature
		{
			get
			{
				return KindRaw == (int)Kind.KeySignature;
			}
		}

		/// <summary>
		/// シーケンサ独自のイベントであるかどうかを調べる
		/// </summary>
		public bool IsSequencerSpecific
		{
			get
			{
				return KindRaw == (int)Kind.SequencerSpecific;
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
		#endregion

		/// <summary>
		/// イベントのキーを取得、設定します。
		/// </summary>
		public int Key
		{
			get
			{
				if (0x80 <= KindRaw && KindRaw <= 0xAF)
				{
					return Data[1];
				}
				return 0;
			}
			set
			{
				Event tempEvent = this.FirstCombinedEvent;
				while (tempEvent != null)
				{
					if (0x80 <= tempEvent.KindRaw && tempEvent.KindRaw <= 0xAF)
					{
						tempEvent.Data[1] = (byte)Clip(0, value, 127);
					}
					tempEvent = tempEvent.NextCombinedEvent;
				}
			}
		}

		/// <summary>
		/// イベントのベロシティを取得、設定します。
		/// </summary>
		public int Velocity
		{
			get
			{
				if (0x80 <= KindRaw && KindRaw <= 0x9F)
				{
					return Data[2];
				}
				return 0;
			}
			set
			{
				if (0x80 <= KindRaw && KindRaw <= 0x8F)
				{
					Data[2] = (byte)Clip(0, value, 127);
				}
				else if (0x90 <= KindRaw && KindRaw <= 0x9F)
				{
					if (Data[2] >= 1)
					{
						Data[2] = (byte)Clip(1, value, 127);
					}
				}
			}
		}

		/// <summary>
		/// 結合イベントの音長さを取得、設定します。
		/// </summary>
		public int Duration
		{
			get
			{
				if (!IsNote)
				{
					return 0;
				}
				int duration = 0;
				if (IsNoteOn)
				{
					Event noteOnEvent = this;
					Event noteOffEvent = this.NextCombinedEvent;
					duration = noteOffEvent._time - noteOnEvent._time;
				}
				else if (IsNoteOff)
				{
					Event noteOffEvent = this;
					Event noteOnEvent = this.PrevCombinedEvent;
					duration = noteOnEvent._time - noteOffEvent._time;
				}
				return duration;
			}
			set
			{
				if (!IsNote)
				{
					return;
				}
				if (IsNoteOn)
				{
					if (value < 0)
					{
						return;
					}
					Event noteOnEvent = this;
					Event noteOffEvent = this.NextCombinedEvent;
					int time = Clip(0, noteOnEvent._time + value, 0x7FFFFFFF);
					noteOffEvent.SetTimeSingle(time);
				}
				else if (IsNoteOff)
				{
					if (value > 0)
					{
						return;
					}
					Event noteOffEvent = this;
					Event noteOnEvent = this.PrevCombinedEvent;
					int time = Clip(0, noteOffEvent._time + value, 0x7FFFFFFF);
					/* TODO:lDuration==0のとき、NoteOnのほうが後に来てしまう。*/
					noteOnEvent.SetTimeSingle(time);
				}
			}
		}

		/// <summary>
		/// イベントの番号を取得、設定します。
		/// (シーケンス番号・チャンネルプリフィックス・ポートプリフィックス・コントロールチェンジ・プログラムチェンジ)
		/// </summary>
		public int Number
		{
			get
			{
				/* シーケンス番号の場合 */
				if (IsSequenceNumber)
				{
					return ((Data[0]) << 8) + Data[1];
				}
				/* チャンネルプリフィックス、ポートプリフィックスの場合 */
				else if (IsChannelPrefix || IsPortPrefix)
				{
					return Data[0];
				}
				/* コントロールチェンジの場合 */
				else if (IsControlChange)
				{
					return Data[1];
				}
				/* プログラムチェンジ */
				else if (IsProgramChange)
				{
					return Data[1];
				}
				return 0;
			}
			set
			{
				/* シーケンス番号の場合 */
				if (IsSequenceNumber)
				{
					Data[0] = (byte)(Clip(0, value, 65535) >> 8);
					Data[1] = (byte)(Clip(0, value, 65535) & 0xFF);
				}
				/* チャンネルプリフィックスの場合 */
				else if (IsChannelPrefix)
				{
					Data[0] = (byte)Clip(0, value, 15);
				}
				/* ポートプリフィックスの場合 */
				else if (IsPortPrefix)
				{
					Data[0] = (byte)Clip(0, value, 255);
				}
				/* コントロールチェンジの場合 */
				else if (IsControlChange)
				{
					Data[1] = (byte)(Clip(0, value, 127));
				}
				/* プログラムチェンジの場合 */
				else if (IsProgramChange)
				{
					Data[1] = (byte)(Clip(0, value, 127));
				}
			}
		}

		/// <summary>
		/// イベントの値を取得、設定します。
		/// (シーケンス番号・チャンネルプリフィックス・ポートプリフィックス・キーアフター・コントロールチェンジ・プログラムチェンジ・チャンネルアフター・ピッチベンド)
		/// </summary>
		public int Value
		{
			get
			{
				/* シーケンス番号の場合 */
				if (IsSequenceNumber)
				{
					return (Data[0] << 8) + Data[1];
				}
				/* チャンネルプリフィックス・ポートプリフィックスの場合 */
				else if (IsChannelPrefix || IsPortPrefix)
				{
					return Data[0];
				}
				/* キーアフタータッチ・コントロールチェンジの場合 */
				else if (IsKeyAftertouch || IsControlChange)
				{
					return Data[2];
				}
				/* プログラムチェンジ・チャンネルアフタータッチの場合 */
				else if (IsProgramChange || IsChannelAftertouch)
				{
					return Data[1];
				}
				/* ピッチベンドの場合 */
				else if (IsPitchBend)
				{
					return Data[1] + (Data[2] << 7);
				}
				return 0;
			}
			set
			{
				/* シーケンス番号の場合 */
				if (IsSequenceNumber)
				{
					Data[0] = (byte)(Clip(0, value, 65535) >> 8);
					Data[1] = (byte)(Clip(0, value, 65535) & 0x00FF);
				}
				/* チャンネルプリフィックス */
				else if (IsChannelPrefix)
				{
					Data[0] = (byte)(Clip(0, value, 15));
				}
				/* ポートプリフィックスの場合 */
				else if (IsPortPrefix)
				{
					Data[0] = (byte)(Clip(0, value, 255));
				}
				/* キーアフタータッチ・コントロールチェンジの場合 */
				else if (IsKeyAftertouch || IsControlChange)
				{
					Data[2] = (byte)(Clip(0, value, 127));
				}
				/* プログラムチェンジ・チャンネルアフタータッチの場合 */
				else if (IsProgramChange || IsChannelAftertouch)
				{
					Data[1] = (byte)(Clip(0, value, 127));
				}
				/* ピッチベンドの場合 */
				else if (IsPitchBend)
				{
					Data[1] = (byte)(Clip(0, value, 16383) & 0x007F);
					Data[2] = (byte)(Clip(0, value, 16383) >> 7);
				}
			}
		}

		/// <summary>
		/// 文字コードを取得、設定します。
		/// 設定時は、文字列のエンコードを含みます。
		/// </summary>
		public CharCode CharCode
		{
			get
			{
				if (KindRaw <= 0x00 || KindRaw >= 0x1F)
				{
					return CharCode.NoCharCod;
				}
				CharCode charCode = GetCharCodeSingle();
				/* データ部に文字コード指定のある場合は、それを返す。 */
				if (charCode != CharCode.NoCharCod)
				{
					return charCode;
				}
				/* データ部に文字コード指定のない場合、直近の同種のイベントの文字コードを探索する。 */
				return FindCharCode();
			}
			set
			{
				CharCode oldCharCode = CharCode;
				CharCode newCharCode = value;
				//変更無しの場合は、何もしない。
				if (oldCharCode == newCharCode)
				{
					return;
				}
				//文字を含むイベントで無い。
				if (KindRaw <= 0x00 || KindRaw >= 0x1F)
				{
					return;
				}

				if (oldCharCode == CharCode.NoCharCod && MIDIDataLib.DefaultCharCode != CharCode.NoCharCod)
				{
					//デフォルト文字コードの取得
					oldCharCode = (CharCode)Enum.ToObject(typeof(CharCode), (int)MIDIDataLib.DefaultCharCode | 0x10000);
				}

				string oldString;
				Encoding encoding;
				//今までの文字コードで文字列作成
				switch (oldCharCode)
				{
					case CharCode.NoCharCod:
						encoding = Encoding.Default;
						oldString = encoding.GetString(Data);
						break;
					case CharCode.LATIN:
						encoding = Encoding.GetEncoding((int)oldCharCode);
						oldString = encoding.GetString(Data.Skip(8).ToArray());
						break;
					case CharCode.JP:
						encoding = Encoding.GetEncoding((int)oldCharCode);
						oldString = encoding.GetString(Data.Skip(5).ToArray());
						break;
					case CharCode.UTF16LE:
						encoding = Encoding.GetEncoding((int)oldCharCode);
						oldString = encoding.GetString(Data.Skip(2).ToArray());
						break;
					case CharCode.UTF16BE:
						encoding = Encoding.GetEncoding((int)oldCharCode);
						oldString = encoding.GetString(Data.Skip(2).ToArray());
						break;
					default:
						//Dataに文字コード識別子がないから、推測された文字コードですべてエンコードする。
						encoding = Encoding.GetEncoding((int)oldCharCode & 0xFFFF);
						oldString = encoding.GetString(Data);
						break;
				}

				string newString;
				byte[] temp;
				//新しい文字コードでバイト配列設定
				switch (newCharCode)
				{
					case CharCode.LATIN:
						newString = "{@LATIN}" + oldString;
						Data = encoding.GetBytes(newString);
						break;
					case CharCode.JP:
						newString = "{@JP}" + oldString;
						Data = encoding.GetBytes(newString);
						break;
					case CharCode.UTF16LE:
						temp = encoding.GetBytes(oldString);
						Data = new byte[temp.Length + 2];
						Data[0] = 0xFF;
						Data[1] = 0xFE;
						temp.CopyTo(Data, 2);						
						break;
					case CharCode.UTF16BE:
						temp = encoding.GetBytes(oldString);
						Data = new byte[temp.Length + 2];
						Data[0] = 0xFE;
						Data[1] = 0xFF;
						temp.CopyTo(Data, 2);
						break;
					default:
						Data = encoding.GetBytes(oldString);
						break;
				}
			}
		}

		/// <summary>
		/// イベントの文字列を取得、設定します。
		/// </summary>
		public string Text
		{
			get
			{
				if (KindRaw <= 0x00 || KindRaw >= 0x1F)
				{
					return "";
				}
				CharCode charCode = CharCode;
				if (charCode == CharCode.NoCharCod && MIDIDataLib.DefaultCharCode != CharCode.NoCharCod)
				{
					//デフォルト文字コードの取得
					charCode = (CharCode)Enum.ToObject(typeof(CharCode), (int)MIDIDataLib.DefaultCharCode | 0x10000);
				}

				string encString;
				Encoding encoding;
				//今までの文字コードで文字列作成
				switch (charCode)
				{
					case CharCode.NoCharCod:
						encoding = Encoding.Default;
						encString = encoding.GetString(Data);
						break;
					case CharCode.LATIN:
						encoding = Encoding.GetEncoding((int)charCode);
						encString = encoding.GetString(Data);
						break;
					case CharCode.JP:
						encoding = Encoding.GetEncoding((int)charCode);
						encString = encoding.GetString(Data);
						break;
					case CharCode.UTF16LE:
						encoding = Encoding.GetEncoding((int)charCode);
						encString = encoding.GetString(Data.Skip(2).ToArray());
						encString = "{@UTF16-LE}" + encString;
						break;
					case CharCode.UTF16BE:
						encoding = Encoding.GetEncoding((int)charCode);
						encString = encoding.GetString(Data.Skip(2).ToArray());
						encString = "{@UTF16-BE}" + encString;
						break;
					default:
						//Dataに文字コード識別子がないから、推測された文字コードですべてエンコードする。
						encoding = Encoding.GetEncoding((int)charCode & 0xFFFF);
						encString = encoding.GetString(Data);
						break;
				}
				return encString;
			}
			set
			{
				if (KindRaw <= 0x00 || KindRaw >= 0x1F)
				{
					throw new MIDIDataLibException("文字列を格納しているイベントではありません。文字列の設定はできません。");
				}
				string encString;
				byte[] temp;
				CharCode charCode = GetTextCharCode(value);
				if (charCode == CharCode.NoCharCod && MIDIDataLib.DefaultCharCode != CharCode.NoCharCod)
				{
					//デフォルト文字コードの取得
					charCode = (CharCode)Enum.ToObject(typeof(CharCode), (int)MIDIDataLib.DefaultCharCode | 0x10000);
				}

				Encoding encoding;
				//バイト配列設定
				switch (charCode)
				{
					case CharCode.NoCharCod:
						encoding = Encoding.Default;
						Data = encoding.GetBytes(value);
						break;
					case CharCode.LATIN:
						encoding = Encoding.GetEncoding((int)charCode);
						encString = "{@LATIN}" + value;
						Data = encoding.GetBytes(encString);
						break;
					case CharCode.JP:
						encoding = Encoding.GetEncoding((int)charCode);
						encString = "{@JP}" + value;
						Data = encoding.GetBytes(encString);
						break;
					case CharCode.UTF16LE:
						encoding = Encoding.GetEncoding((int)charCode);
						temp = encoding.GetBytes(value);
						Data = new byte[temp.Length + 2];
						Data[0] = 0xFF;
						Data[1] = 0xFE;
						temp.CopyTo(Data, 2);
						break;
					case CharCode.UTF16BE:
						encoding = Encoding.GetEncoding((int)charCode);
						temp = encoding.GetBytes(value);
						Data = new byte[temp.Length + 2];
						Data[0] = 0xFE;
						Data[1] = 0xFF;
						temp.CopyTo(Data, 2);
						break;
					default:
						//valueから文字コードが特定できなかった場合で、デフォルト文字コードが指定してあった場合、デフォルト文字コードでエンコード
						encoding = Encoding.GetEncoding((int)charCode & 0xFFFF);
						Data = encoding.GetBytes(value);
						break;
				}
			}
		}

		/// <summary>
		/// SMPTEオフセットを取得、設定します。
		/// </summary>
		public SMPTEOffset SMPTEOffset
		{
			get
			{
				if (Kind != Kind.SMPTEOffset)
				{
					return null;
				}

				SMPTEOffset sMPTEOffset = new SMPTEOffset();
				sMPTEOffset.Mode = (SMPTEMode)Enum.ToObject(typeof(SMPTEMode), Data[0] >> 5);
				sMPTEOffset.Hour = Data[0] & 0x1F;
				sMPTEOffset.Min = Data[1];
				sMPTEOffset.Sec = Data[2];
				sMPTEOffset.Frame = Data[3];
				sMPTEOffset.SubFrame = Data[4];
				return sMPTEOffset;
			}
			set
			{
				if (Kind != Kind.SMPTEOffset)
				{
					throw new MIDIDataLibException("SMPTEオフセットイベントではありません。SMPTEオフセットの設定はできません。");
				}

				int[] maxFrame = { 23, 24, 29, 29 };
				Data = new byte[5];
				Data[0] = (byte)((((int)value.Mode & 0x03) << 5) | (Clip(0, value.Hour, 23)));
				Data[1] = (byte)Clip(0, value.Min, 59);
				Data[2] = (byte)Clip(0, value.Sec, 59);
				Data[3] = (byte)Clip(0, value.Frame, maxFrame[(int)value.Mode & 0x03]);
				Data[4] = (byte)Clip(0, value.SubFrame, 99);
			}
		}

		/// <summary>
		/// テンポを取得、設定します。
		/// </summary>
		public int Tempo
		{
			get
			{
				if (Kind != Kind.Tempo)
				{
					return 0;
				}
				return Data[0] << 16 | Data[1] << 8 | Data[2];
			}
			set
			{
				if (Kind != Kind.Tempo)
				{
					throw new MIDIDataLibException("テンポイベントではありません。テンポの設定はできません。");
				}
				Data = new byte[3];
				Data[0] = (byte)((Clip(MinTempo, value, MaxTempo) & 0xFF0000) >> 16);
				Data[1] = (byte)((Clip(MinTempo, value, MaxTempo) & 0x00FF00) >> 8);
				Data[2] = (byte)((Clip(MinTempo, value, MaxTempo) & 0x0000FF) >> 0);
			}
		}

		/// <summary>
		/// 拍子の取得、設定します。
		/// </summary>
		public TimeSignature TimeSignature
		{
			get
			{
				if (Kind != Kind.TimeSignature)
				{
					return null;
				}
				int nn = Data[0];
				int dd = Data[1];
				int cc = Data[2];
				int bb = Data[3];
				return new TimeSignature(nn, dd, cc, bb);
			}
			set
			{
				if (Kind != Kind.TimeSignature)
				{
					throw new MIDIDataLibException("拍子イベントではありません。拍子の設定はできません。");
				}
				Data = new byte[4];
				Data[0] = (byte)value.nn;
				Data[1] = (byte)value.dd;
				Data[2] = (byte)value.cc;
				Data[3] = (byte)value.bb;
			}
		}

		/// <summary>
		/// 調性記号の取得、設定します。
		/// </summary>
		public KeySignature KeySignature
		{
			get
			{
				if (Kind != Kind.KeySignature)
				{
					throw new MIDIDataLibException("調性記号イベントではありません。調性記号の取得はできません。");
				}
				int sf = Data[0];
				Key mi = (Key)Enum.ToObject(typeof(Key), Data[1]);
				return new KeySignature(sf, mi);
			}
			set
			{
				if (Kind != Kind.KeySignature)
				{
					throw new MIDIDataLibException("調性記号イベントではありません。調性記号の設定はできません。");
				}
				Data = new byte[2];
				Data[0] = (byte)Clip(-7, value.sf, 7);
				Data[1] = (byte)Clip(0, (int)value.mi, 1);
			}
		}

		/// <summary>
		/// MIDIメッセージの取得、設定をします。
		/// </summary>
		public byte[] MIDIMessage
		{
			get
			{
				if (IsMIDIEvent || IsSysExEvent)
				{
					return Data;
				}
				return null;
			}
			set
			{
				Debug.Assert(IsMIDIEvent || IsSysExEvent);
				if (IsMIDIEvent || IsSysExEvent)
				{
					Data = value;
				}
			}
		}

		/// <summary>
		/// チャンネルを取得します。
		/// </summary>
		public int Channel
		{
			get
			{
				if (IsMIDIEvent)
				{
					Debug.Assert(KindRaw == Data[0]);
					return KindRaw & 0x0F;
				}
				return 0;
			}
			set
			{
				if (0x80 <= KindRaw && KindRaw <= 0xEF)
				{
					return;
				}
				Event tempEvent = FirstCombinedEvent;
				while (tempEvent != null)
				{
					if (tempEvent.IsMIDIEvent)
					{
						tempEvent.KindRaw &= 0xF0;
						tempEvent.KindRaw |= (byte)Clip(0, value, 15);
						tempEvent.Data[0] &= 0xF0;
						tempEvent.Data[0] |= (byte)Clip(0, value, 15);
						Debug.Assert(tempEvent.KindRaw == tempEvent.Data[0]);
						/* 前後の同種イベントのポインタのつなぎ替え */
						if (tempEvent.PrevSameKindEvent != null)
						{
							tempEvent.PrevSameKindEvent.NextSameKindEvent =
								tempEvent.PrevSameKindEvent.SearchNextSameKindEvent();
						}
						if (tempEvent.NextSameKindEvent != null)
						{
							tempEvent.NextSameKindEvent.PrevSameKindEvent =
								tempEvent.NextSameKindEvent.SearchPrevSameKindEvent();
						}
						/* 前後の同種イベントポインタ設定 */
						tempEvent.PrevSameKindEvent = tempEvent.SearchPrevSameKindEvent();
						if (tempEvent.PrevSameKindEvent != null)
						{
							tempEvent.PrevSameKindEvent.NextSameKindEvent = tempEvent;
						}
						tempEvent.NextSameKindEvent = tempEvent.SearchNextSameKindEvent();
						if (tempEvent.NextSameKindEvent != null)
						{
							tempEvent.NextSameKindEvent.PrevSameKindEvent = tempEvent;
						}
					}
					tempEvent = tempEvent.NextCombinedEvent;
				}
			}
		}
		#endregion

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

			_time = time;
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
		public Event(int time, Kind kind, byte[] data) : this(time, (int)kind, data) { }
		#endregion

		#region メソッド
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
		private Event GetFirstCombinedEvent()
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
		private Event GetLastCombinedEvent()
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
			if (ReferenceEquals(this, insertEvent))
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
				if (this._time < insertEvent._time)
				{ /* 20080622追加 */
					this._time = insertEvent._time;
				}
			}
			/* 時刻の整合性がとれていない場合、自動的に挿入イベントの時刻を補正する */
			if (insertEvent._time > this._time)
			{
				insertEvent._time = this._time;
			}
			if (this.PrevEvent != null)
			{
				if (insertEvent._time < this.PrevEvent._time)
				{
					insertEvent._time = this.PrevEvent._time;
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
			if (ReferenceEquals(this, insertEvent))
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
			if (insertEvent._time < this._time)
			{
				insertEvent._time = this._time;
			}
			if (this.NextEvent != null)
			{
				if (insertEvent._time > this.NextEvent._time)
				{
					insertEvent._time = this.NextEvent._time;
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
			newEvent._time = this._time;
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
		/// <returns>クローンされたイベント</returns>
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
						Event deleteEvent = prevEvent.FirstCombinedEvent;
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
			newEvent = newEvent.FirstCombinedEvent;
			for (i = 0; i < position; i++)
			{
				newEvent = newEvent.NextCombinedEvent;
			}
			return newEvent;
		}

		#region Create系性的メソッド
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
			return new Event(time, Kind.SequenceNumber, c);
		}

		/// <summary>
		/// テキストベースのイベントの生成(文字コード指定)
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="kind">イベント種類</param>
		/// <param name="charCode">文字コード</param>
		/// <param name="text">テキスト</param>
		/// <returns>テキストベースのイベント</returns>
		static private Event CreateTextBasedEvent(int time, int kind, CharCode charCode, string text)
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
		static private Event CreateTextBasedEvent(int time, Kind kind, CharCode charCode, string text)
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
			return Event.CreateTextBasedEvent(time, kind, CharCode.NoCharCod, text);
		}

		/// <summary>
		/// テキストイベントの生成(文字コード指定あり)
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="charCode">文字コード</param>
		/// <param name="text">テキスト</param>
		/// <returns>テキストイベント</returns>
		static public Event CreateTextEvent(int time, CharCode charCode, string text)
		{
			return CreateTextBasedEvent(time, Kind.TextEvent , charCode, text);
		}

		/// <summary>
		/// テキストイベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="text">テキスト</param>
		/// <returns>テキストイベント</returns>
		static public Event  CreateTextEvent(int time, string text) 
		{
			return CreateTextEvent(time, CharCode.NoCharCod, text);
		}

		/// <summary>
		/// 著作権イベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="charCode">文字コード</param>
		/// <param name="text">著作権情報</param>
		/// <returns>著作権イベント</returns>
		static public Event CreateCopyrightNotice(int time, CharCode charCode, string text)
		{
			return CreateTextBasedEvent(time, Kind.CopyrightNotice, charCode, text);
		}

		/// <summary>
		/// 著作権イベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="text">著作権情報</param>
		/// <returns>著作権イベント</returns>
		static public Event CreateCopyrightNotice(int time, string text) 
		{
			return CreateCopyrightNotice(time, CharCode.NoCharCod, text);
		}

		/// <summary>
		/// トラック名イベントの生成(文字コード指定あり)
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="charCode">文字コード</param>
		/// <param name="text">トラック名</param>
		/// <returns>トラック名イベント</returns>
		static public　Event CreateTrackName(int time, CharCode charCode, string text)
		{
			return CreateTextBasedEvent(time, Kind.TrackName, charCode, text);
		}

		/// <summary>
		/// トラック名イベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="text">トラック名</param>
		/// <returns>トラック名イベント</returns>
		static public Event CreateTrackName(int time, string text)
		{
			return CreateTrackName(time, CharCode.NoCharCod, text);
		}

		/// <summary>
		/// インストゥルメントイベントの生成(文字コード指定あり)
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="charCode">文字コード</param>
		/// <param name="text">インストゥルメント名</param>
		/// <returns>インストゥルメントイベント</returns>
		static public Event CreateInstrumentName(int time, CharCode charCode, string text)
		{
			return CreateTextBasedEvent(time, Kind.InstrumentName, charCode, text);
		}

		/// <summary>
		/// インストゥルメントイベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="text">インストゥルメント名</param>
		/// <returns>インストゥルメントイベント</returns>
		static public Event CreateInstrumentName(int time, string text)
		{
			return CreateInstrumentName(time, CharCode.NoCharCod, text);
		}

		/// <summary>
		/// 歌詞イベントの生成(文字コード指定あり)
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="charCode">文字コード</param>
		/// <param name="text">歌詞</param>
		/// <returns>歌詞イベント</returns>
		static public Event CreateLyric(int time, CharCode charCode, string text)
		{
			return CreateTextBasedEvent(time, Kind.Lyric, charCode, text);
		}

		/// <summary>
		/// 歌詞イベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="text">歌詞</param>
		/// <returns>歌詞イベント</returns>
		static public Event CreateLyric(int time, string text)
		{
			return CreateLyric(time, CharCode.NoCharCod, text);
		}

		/// <summary>
		/// マーカーイベントの生成(文字コード指定あり)
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="charCode">文字コード</param>
		/// <param name="text">マーカー情報</param>
		/// <returns>マーカーイベント</returns>
		static public Event CreateMarker(int time, CharCode charCode, string text)
		{
			return CreateTextBasedEvent(time, Kind.Marker, charCode, text);
		}

		/// <summary>
		/// マーカーイベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="text">マーカー情報</param>
		/// <returns>マーカーイベント</returns>
		static public Event CreateMarker(int time, string text)
		{
			return CreateMarker(time, CharCode.NoCharCod, text);
		}

		/// <summary>
		/// キューポイントイベントの生成(文字コード指定あり)
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="charCode">文字コード</param>
		/// <param name="text">キューポイント情報</param>
		/// <returns>キューポイントイベント</returns>
		static public Event CreateCuePoint(int time, CharCode charCode, string text)
		{
			return CreateTextBasedEvent(time, Kind.CuePoint, charCode, text);
		}

		/// <summary>
		/// キューポイントイベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="text">キューポイント情報</param>
		/// <returns>キューポイントイベント</returns>
		static public Event CreateCuePoint(int time, string text)
		{
			return CreateCuePoint(time, CharCode.NoCharCod, text);
		}

		/// <summary>
		/// プログラム名イベントの生成(文字コード指定あり)
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="charCode">文字コード</param>
		/// <param name="text">プログラム名</param>
		/// <returns>プログラム名イベント</returns>
		static public Event CreateProgramName(int time, CharCode charCode, string text)
		{
			return CreateTextBasedEvent(time, Kind.ProgramName, charCode, text);
		}

		/// <summary>
		/// プログラム名イベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="text">プログラム名</param>
		/// <returns>プログラム名イベント</returns>
		static public Event CreateProgramName(int time, string text)
		{
			return CreateProgramName(time, CharCode.NoCharCod, text);
		}

		/// <summary>
		/// デバイス名イベント生成(文字コード指定あり)
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="charCode">文字コード</param>
		/// <param name="text">デバイス名</param>
		/// <returns>デバイス名イベント</returns>
		static public Event CreateDeviceName(int time, CharCode charCode, string text) 
		{
			return CreateTextBasedEvent(time, Kind.DeviceName, charCode, text);
		}

		/// <summary>
		/// デバイス名イベント生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="text">デバイス名</param>
		/// <returns>デバイス名イベント</returns>
		static public Event CreateDeviceName(int time, string text)
		{
			return CreateDeviceName(time, CharCode.NoCharCod, text);
		}

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
			return new Event(time, Kind.ChannelPrefix, c);
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
			return new Event(time, Kind.PortPrefix, c);
		}

		/// <summary>
		/// エンドオブトラックイベントの生成
		/// </summary>
		/// <param name="time">時刻</param>
		/// <returns>エンドオブトラックイベント</returns>
		static public Event CreateEndofTrack(int time)
		{
			return new Event(time, Kind.EndofTrack, new byte[0]);
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
			return new Event(time, Kind.Tempo, c);
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
		static private Event CreateSMPTEOffset(int time, SMPTEMode mode, int hour, int min, int sec, int frame, int subFrame)
		{
			int[] maxFrame = { 23, 24, 29, 29 };
			byte[] c = new byte[5];
			c[0] = (byte)((((int)mode & 0x03) << 5) | (Clip(0, hour, 23)));
			c[1] = (byte)Clip(0, min, 59);
			c[2] = (byte)Clip(0, sec, 59);
			c[3] = (byte)Clip(0, frame, maxFrame[(int)mode & 0x03]);
			c[4] = (byte)Clip(0, subFrame, 99);
			return new Event(time, Kind.SMPTEOffset, c);
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
			return new Event(time, Kind.TimeSignature, c);
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
			return new Event(time, Kind.KeySignature, c);
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
			return new Event(time, Kind.SequencerSpecific, buf);
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
			c[0] = (byte)((int)Kind.NoteOff | (ch & 0x0F));
			c[1] = (byte)Clip(0, key, 127);
			c[2] = (byte)Clip(0, vel, 127);
			return new Event(time, (int)Kind.NoteOff | (ch & 0x0F), c);
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
			c[0] = (byte)((int)Kind.NoteOn | (ch & 0x0F));
			//!!Clipの範囲が間違っている??
			//c[1] = (byte)(Clip(1, key, 127));
			c[1] = (byte)Clip(0, key, 127);
			c[2] = (byte)Clip(0, vel, 127);
			return new Event(time, (int)Kind.NoteOn | (ch & 0x0F), c);
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
			c[0] = (byte)((int)Kind.KeyAfterTouch | (ch & 0x0F));
			c[1] = (byte)(Clip(0, key, 127));
			c[2] = (byte)(Clip(0, val, 127));
			return new Event(time, (int)Kind.KeyAfterTouch | c[0], c);
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
			c[0] = (byte)((int)Kind.ControlChange | (ch & 0x0F));
			c[1] = (byte)(Clip(0, num, 127));
			c[2] = (byte)(Clip(0, val, 127));
			return new Event(time, (int)Kind.ControlChange | c[0], c);
		}

		/// <summary>
		/// プログラムチェンジイベントを生成する。
		/// </summary>
		/// <param name="time">絶対時刻</param>
		/// <param name="ch">チャンネル番号</param>
		/// <param name="num">プログラムナンバー(0～127)</param>
		/// <returns>プログラムチェンジイベント</returns>
		static public Event CreateProgramChange(int time, int ch, int val)
		{
			byte[] c = new byte[2];
			c[0] = (byte)((int)Kind.ProgramChange | (ch & 0x0F));
			c[1] = (byte)(Clip(0, val, 127));
			return new Event(time, (int)Kind.ProgramChange | c[0], c);
		}

		/// <summary>
		/// チャンネルアフタータッチイベントを生成する。
		/// </summary>
		/// <param name="time">絶対時刻</param>
		/// <param name="ch">チャンネル番号</param>
		/// <param name="val">値(0～127)</param>
		/// <returns>チャンネルアフタータッチイベント</returns>
		static public Event CreateChannelAftertouch(int time, int ch, int val)
		{
			byte[] c = new byte[2];
			c[0] = (byte)((int)Kind.ChannelAfterTouch | (ch & 0x0F));
			c[1] = (byte)(Clip(0, val, 127));
			return new Event(time, (int)Kind.ChannelAfterTouch | c[0], c);
		}

		/// <summary>
		/// ピッチベンドイベントを生成する。
		/// </summary>
		/// <param name="time">絶対時刻</param>
		/// <param name="ch">チャンネル番号</param>
		/// <param name="val">値(0～16383)</param>
		/// <returns>ピッチベンドイベント</returns>
		static public Event CreatePitchBend(int time, int ch, int val)
		{
			byte[] c = new byte[3];
			c[0] = (byte)((int)Kind.PitchBend | (ch & 0x0F));
			c[1] = (byte)(Clip(0, val, 16383) & 0x7F);
			c[2] = (byte)((Clip(0, val, 16383) >> 7) & 0x7F);
			return new Event(time, (int)Kind.PitchBend | c[0], c);
		}

		/// <summary>
		/// システムエクスクルーシブイベントを生成する。
		/// </summary>
		/// <param name="time">絶対時刻</param>
		/// <param name="buf">データ部</param>
		/// <returns>システムエクスクルーシブイベント</returns>
		static public Event CreateSysExEvent(int time, byte[] buf)
		{
			if (buf[0] == 0xF0)
			{
				return new Event(time, Kind.SysExStart, buf);
			}
			else
			{
				return new Event(time, Kind.SysExContinue, buf);
			}
		}
		#endregion

		/// <summary>
		/// 文字列の文字コードを判別
		/// </summary>
		/// <param name="data">文字列</param>
		/// <returns>文字コード</returns>
		private CharCode GetTextCharCode(string data) 
		{
			/* データ部に文字コード指定のある場合は、それを返す。 */
			if (data != null) {
				if (data.Length >= 11 && data.StartsWith("{@UTF-16LE}"))
				{
					return CharCode.UTF16LE;
				}
				if (data.Length >= 11 && data.StartsWith("{@UTF-16BE}"))
				{
					return CharCode.UTF16BE;
				}
				if (data.Length >= 8 && data.StartsWith("{@LATIN}"))
				{
					return CharCode.LATIN;
				}
				if (data.Length >= 5 && data.StartsWith("{@JP}"))
				{
					return CharCode.JP;
				}
			}
			/* データ部に文字コード指定のない場合、CharCodes.NoCharCodを返す。 */
			return CharCode.NoCharCod;
		}

		/// <summary>
		/// イベントの文字コードを取得
		/// </summary>
		/// <returns>文字コード</returns>
		private CharCode GetCharCodeSingle()
		{
			if (KindRaw <= 0x00 || KindRaw >= 0x1F)
			{
				return CharCode.NoCharCod;
			}
			/* データ部に文字コード指定のある場合は、それを返す。 */
			if (Data != null)
			{
				if (Data.Length >= 2 && Data[0] == 0xFF && Data[1] == 0xFE)
				{
					return CharCode.UTF16LE;
				}
				if (Data.Length >= 2 && Data[0] == 0xFE && Data[1] == 0xFF)
				{
					return CharCode.UTF16BE;
				}
				if (Data.Length >= 8 && Encoding.ASCII.GetString(Data.Take(8).ToArray()).StartsWith("{@LATIN}"))
				{
					return CharCode.LATIN;
				}
				if (Data.Length >= 5 && Encoding.ASCII.GetString(Data.Take(5).ToArray()).StartsWith("{@JP}"))
				{
					return CharCode.JP;
				}
			}
			/* データ部に文字コード指定のない場合、CharCodes.NoCharCodを返す。 */
			return CharCode.NoCharCod;
		}

		/// <summary>
		/// 直近の同種のイベントの文字コードを返す
		/// </summary>
		/// <returns>文字コード</returns>
		private CharCode FindCharCode()
		{
			if (KindRaw <= 0x00 || KindRaw >= 0x1F)
			{
				return CharCode.NoCharCod;
			}
			Event prevEvent = PrevEvent;
			while (prevEvent != null)
			{
				if (prevEvent.Kind == Kind)
				{
					CharCode charCode = prevEvent.GetCharCodeSingle();
					switch (charCode)
					{
						case CharCode.LATIN:
							return CharCode.NOCHARCODELATIN;
						case CharCode.JP:
							return CharCode.NOCHARCODEJP;
						case CharCode.UTF16LE:
							return CharCode.NOCHARCODEUTF16LE;
						case CharCode.UTF16BE:
							return CharCode.NOCHARCODEUTF16BE;
					}
				}
				prevEvent = prevEvent.PrevEvent;
			}
			return CharCode.NoCharCod;
		}

		/// <summary>
		/// 単体イベントの時刻設定
		/// イベントがリストの要素の場合、ポインタをつなぎ変えて時刻順序を正しく保ちます。
		/// </summary>
		/// <param name="time">時刻</param>
		public void SetTimeSingle(int time)
		{
			int currentTime = _time;
			Track track = Parent;

			/* 浮遊イベントの場合は単純に時刻設定 */
			if (IsFloating)
			{
				_time = Clip(0, time, 0x7FFFFFFF);
				return;
			}

			/* 以下は浮遊イベントでない場合の処理 */
			/* EOTイベントを動かす場合の特殊処理 */
			if (Kind == Kind.EndofTrack && NextEvent == null)
			{
				/* EOTイベントの前に別のイベントがある場合 */
				if (PrevEvent != null)
				{
					/* EOTイベントはそのイベントより前には移動しない。 */
					if (PrevEvent._time > time)
					{
						_time = PrevEvent._time;
					}
					else
					{
						_time = time;
					}
				}
				/* EOTイベントの前に別のイベントが無い場合 */
				else
				{
					/* タイムスタンプ0より前には移動しない。 */
					_time = Clip(0, time, 0x7FFFFFFF);
				}
				return;
			}

			/* エンドオブトラック以外のイベントの場合 */
			/* 現在のタイムより後方へ動かす場合 */
			if (time >= currentTime)
			{
				/* pTempEventの直前に挿入する。pTempEventがなければ最後に挿入する。 */
				Event tempEvent = this;
				Event lastEvent = null;
				/* ノートオフイベントの場合 */
				if (tempEvent.IsNoteOff)
				{
					Event pNoteOnEvent = tempEvent.PrevCombinedEvent;
					/* 対応するノートオンイベントがある場合(20090713追加) */
					if (pNoteOnEvent != null)
					{
						/* 音長さ=0以下の場合(20090713追加) */
						/* 対応するノートオンイベントの直後に確定 */
						if (time <= pNoteOnEvent._time)
						{
							time = pNoteOnEvent._time;
							lastEvent = pNoteOnEvent;
							/* 注:SetTimeSingleから呼ばれた場合とSetDurationから呼ばれた場合でNoteOn-NoteOff順序が異なる。 */
							/* NoteOnを先に移動済みの場合(SetTimeSingle) */
							if (pNoteOnEvent.NextEvent != this)
							{
								tempEvent = pNoteOnEvent.NextEvent;
							}
							/* NoteOnを移動していない場合(SetDuration) */
							else
							{
								tempEvent = this.NextEvent; /* 20190101:pNoteOnEventをpEventに修正 */
							}
							if (tempEvent != null)
							{
								if (tempEvent.Kind == Kind.EndofTrack &&
									tempEvent.NextEvent == null)
								{
									tempEvent._time = time;
								}
							}
						}
						/* 音長さ=0以上の場合(20090713追加) */
						else
						{
							while (tempEvent != null)
							{
								if (tempEvent._time > time ||
									(tempEvent._time == time && !tempEvent.IsNoteOff))
								{
									break;
								}
								/* EOTよりも後に来る場合はEOTを後ろへ追い込む */
								if (tempEvent.Kind == Kind.EndofTrack &&
									tempEvent.NextEvent == null)
								{
									tempEvent._time = time;
									break;
								}
								lastEvent = tempEvent;
								tempEvent = tempEvent.NextEvent;
							}
						}
					}
					/* 対応するノートオンイベントがない場合 */
					else
					{
						while (tempEvent != null)
						{
							if (tempEvent._time > time ||
								(tempEvent._time == time && !tempEvent.IsNoteOff))
							{
								break;
							}
							/* EOTよりも後に来る場合はEOTを後ろへ追い込む */
							if (tempEvent.Kind == Kind.EndofTrack &&
								tempEvent.NextEvent == null)
							{
								tempEvent._time = time;
								break;
							}
							lastEvent = tempEvent;
							tempEvent = tempEvent.NextEvent;
						}
					}
				}
				/* その他の場合 */
				else
				{
					while (tempEvent != null)
					{
						if (tempEvent._time > time)
						{
							break;
						}
						/* EOTよりも後に来る場合はEOTを後ろへ追い込む */
						if (tempEvent.Kind == Kind.EndofTrack &&
							tempEvent.NextEvent == null)
						{
							tempEvent._time = time;
							break;
						}
						lastEvent = tempEvent;
						tempEvent = tempEvent.NextEvent;
					}
				}
				/* pTempEventの直前にpEventを挿入する場合 */
				if (tempEvent != null)
				{
					if (tempEvent != this &&
						tempEvent.PrevEvent != this)
					{ 
						this.SetFloating();
						this._time = time;
						tempEvent.PrevEvent = this;
					}
					else
					{
						this._time = time;
					}
				}
				/* リンクリストの最後にpEventを挿入する場合 */
				else if (lastEvent != null)
				{
					if (lastEvent != this &&
						lastEvent.NextEvent != this)
					{ /* 20190407修正 */
						this.SetFloating();
						this._time = time;
						lastEvent.NextEvent = this;
					}
					else
					{
						this._time = time;
					}
				}
				/* 空のリストに挿入する場合 */
				else if (track != null)
				{
					this._time = time;
					this.Parent = track;
					this.NextEvent = null;
					this.PrevEvent = null;
					this.NextSameKindEvent = null;
					this.PrevSameKindEvent = null;
					track.FirstEvent = this;
					track.LastEvent = this;
					track.NumEvent++;
				}

			}
			/* 現在のタイムより前方へ動かす場合 */
			else if (time < currentTime)
			{
				/* pTempEventの直後に挿入する。pTempEventがなければ最初に挿入する。 */
				Event tempEvent = this;
				Event firstEvent = null;
				/* ノートオフイベントの場合 */
				if (IsNoteOff)
				{
					Event pNoteOnEvent = tempEvent.PrevCombinedEvent;
					/* 対応するノートオンイベントがある場合(20090713追加) */
					if (pNoteOnEvent != null)
					{
						/* 音長さ=0以下の場合(20090713追加) */
						/* 対応するノートオンイベントの直後に確定 */
						if (time <= pNoteOnEvent._time)
						{
							time = pNoteOnEvent._time;
							firstEvent = null;
							tempEvent = pNoteOnEvent;
						}
						/* 音長さ=0以上の場合(20090713追加) */
						else
						{
							while (tempEvent != null)
							{
								if (tempEvent._time < time ||
									(tempEvent._time == time && tempEvent.IsNoteOff))
								{
									break;
								}
								/* 対応するノートオンイベントより前には行かない */
								if (ReferenceEquals(tempEvent, pNoteOnEvent))
								{
									break;
								}
								firstEvent = tempEvent;
								tempEvent = tempEvent.PrevEvent;
							}
						}
					}
					/* 対応するノートオンイベントがない場合 */
					else
					{
						while (tempEvent != null)
						{
							if (tempEvent._time < time ||
								(tempEvent._time == time && tempEvent.IsNoteOff))
							{
								break;
							}
							firstEvent = tempEvent;
							tempEvent = tempEvent.PrevEvent;
						}
					}
				}
				/* その他のイベントの場合 */
				else
				{
					while (tempEvent != null)
					{
						if (tempEvent._time <= time)
						{
							break;
						}
						firstEvent = tempEvent;
						tempEvent = tempEvent.PrevEvent;
					}
				}
				/* pTempEventの直後にpEventを挿入する場合 */
				if (tempEvent != null)
				{
					if (tempEvent != this &&
						tempEvent.NextEvent != this)
					{ /* 20080721修正 */
						this.SetFloating();
						this._time = time;
						tempEvent.NextEvent = this;
					}
					else
					{
						this._time = time;
					}
				}
				/* リンクリストの最初にpEventを挿入する場合 */
				else if (firstEvent != null)
				{
					if (firstEvent != this &&
						firstEvent.PrevEvent != this)
					{ /* 20080721追加 */
						this.SetFloating();
						this._time = time;
						firstEvent.PrevEvent = this;
					}
					else
					{
						this._time = time;
					}
				}
				/* 空のリストに挿入する場合 */
				else if (track != null)
				{
					this._time = time;
					this.Parent = track;
					this.NextEvent = null;
					this.PrevEvent = null;
					this.NextSameKindEvent = null;
					this.PrevSameKindEvent = null;
					track.FirstEvent = this;
					track.LastEvent = this;
					track.NumEvent++;
				}
			}
		}

		#region ToString関連

		/* イベントの種類文字列表現表(メタイベント)(ANSI) */
		private string[] g_szMetaKindNameA = new string[] {
			"SequenceNumber", "TextEvent", "CopyrightNotice", "TrackName",
			"InstrumentName", "Lyric", "Marker", "CuePoint",
			"ProgramName", "DeviceName", "", "", "", "", "", "", /* 0x00 ～ 0x0F */
			"", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", /* 0x10 ～ 0x1F */
			"ChannelPrefix", "PortPrefix", "", "", "", "", "", "",
			"", "", "", "", "", "", "", "EndofTrack", /* 0x20 ～ 0x2F */
			"", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", /* 0x30 ～ 0x3F */
			"", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", /* 0x40 ～ 0x4F */
			"", "Tempo", "", "", "SMPTEOffset", "", "", "",
			"TimeSignature", "KeySignature", "", "", "", "", "", "", /* 0x50 ～ 0x5F */
			"", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", /* 0x60 ～ 0x6F */
			"", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "SequencerSpec", /* 0x70 ～ 0x7F */
			"UnknownMetaEvent"};

		/* イベントの種類文字列表現表(MIDIイベント)(ANSI) */
		private string[] g_szMIDIKindNameA = new string[] {
			"NoteOff", "NoteOn", "KeyAftertouch", "ControlChange",
			"ProgramChange", "ChannelAftertouch", "PitchBend"};

		/* イベントの種類文字列表現表(SYSEXイベント)(ANSI) */
		string[] g_szSysExKindNameA = new string[] {"SysExStart", "", "", "", "", "", "", "SysExContinue"};

		/* ノートキー文字列表現表(ANSI) */
		string[] g_szKeyNameA = new string[]{"C_", "C#", "D_", "D#", "E_", "F_", "F#", "G_", "G#", "A_", "Bb", "B_"};

		/// <summary>
		/// イベントの内容を文字列で返します。
		/// </summary>
		/// <returns>イベントの文字列</returns>
		public override string ToString()
		{
			throw new NotImplementedException();

			string retVal;
			//時刻の作成
			Track track;
			Data data;
			if ((track = this.Parent) != null)
			{
				if ((data = track.Parent) != null)
				{
					
				}
			}

			return retVal;
		}

		#endregion
		#endregion


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
