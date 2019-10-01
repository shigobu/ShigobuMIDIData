using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shigobu.MIDI.DataLib
{
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
			if (this.Data.Length > 0)
			{
				newEvent.Data = new byte[this.Data.Length];
				this.Data.CopyTo(newEvent.Data, 0);
			}
			else
			{
				newEvent.Data = null;
			}
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


	}
}
