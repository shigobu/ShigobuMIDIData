using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shigobu.MIDI.DataLib
{
	public class Track
	{
		#region コンストラクタ

		/// <summary>
		/// 空のトラックを生成します。
		/// </summary>
		public Track()
		{
			NumEvent = 0;
			FirstEvent = null;
			LastEvent = null;
			NextTrack = null;
			PrevTrack = null;
			Parent = null;
		}

		#endregion

		#region プロパティ

		/// <summary>
		/// このトラックの一時的なインデックス(0から始まる)
		/// </summary>
		private int TempIndex { get; set; }

		/// <summary>
		/// トラック内のイベント数
		/// </summary>
		public int NumEvent { get; internal set; }

		/// <summary>
		/// 最初のイベント(なければNULL)
		/// </summary>
		public Event FirstEvent { get; internal set; }

		/// <summary>
		/// 最後のイベント(なければNULL)
		/// </summary>
		public Event LastEvent { get; internal set; }

		/// <summary>
		/// 前のトラック(なければNULL)
		/// </summary>
		public Track PrevTrack { get; internal set; }

		/// <summary>
		/// 次のトラック(なければNULL)
		/// </summary>
		public Track NextTrack { get; internal set; }

		/// <summary>
		/// 親(MIDIDataオブジェクト)
		/// </summary>
		public Data Parent { get; internal set; }

		/// <summary>
		/// トラックの開始時刻(最初のイベントの時刻)[Tick]を取得
		/// </summary>
		public int BeginTime
		{
			get
			{
				if (FirstEvent != null)
				{
					return FirstEvent.Time;
				}
				return 0;
			}
		}

		/// <summary>
		/// トラックの終了時刻(最後のイベントの時刻)[Tick]を取得
		/// </summary>
		public int EndTime
		{
			get
			{
				if (LastEvent != null)
				{
					return LastEvent.Time;
				}
				return 0;
			}
		}

		/// <summary>
		/// トラック名を取得、設定します。
		/// </summary>
		public string Name
		{
			get
			{
				foreach (Event @event in this)
				{
					if (@event.Kind == Kinds.TrackName)
					{
						return @event.Text;
					}
				}
				return null;
			}
			set
			{
				foreach (Event @event in this)
				{
					if (@event.Kind == Kinds.TrackName)
					{
						@event.Text = value;
						return;
					}
				}
				InsertTrackName(0, value);
			}
		}

		#endregion

		#region メソッド

		/// <summary>
		/// トラック内の指定種類の最初のイベント取得します。
		/// </summary>
		/// <param name="kind">イベントの種類</param>
		/// <returns>イベント</returns>
		public Event GetFirstKindEvent(Kinds kind)
		{
			for(Event @event = FirstEvent; @event != null; @event = @event.NextEvent)
			{
				if (@event.Kind == kind)
				{
					return @event;
				}
			}
			return null;
		}

		/// <summary>
		/// トラック内の指定種類の最後のイベント取得します。
		/// </summary>
		/// <param name="kind">イベントの種類</param>
		/// <returns>イベント</returns>
		public Event GetLastKindEvent(Kinds kind)
		{
			for (Event @event = LastEvent; @event != null; @event = @event.PrevEvent)
			{
				if (@event.Kind == kind)
				{
					return @event;
				}
			}
			return null;
		}

		/// <summary>
		/// トラック内のイベント数をカウントし、各イベントのインデックスと総イベント数を更新し、イベント数を返す。
		/// </summary>
		/// <returns></returns>
		public int CountEvent()
		{
			int i = 0;
			foreach (Event @event in this)
			{
				@event.TempIndex = i;
				i++;
			}
			return NumEvent = i;
		}

		/// <summary>
		/// トラックの削除(トラック内に含まれるイベントオブジェクトも削除されます)
		/// </summary>
		public void Delete()
		{
			/* トラック内のイベント削除 */
			Event @event = FirstEvent;
			while (@event != null)
			{
				Event nextEvent = @event.NextEvent;
				@event.DeleteSingle();
				@event = nextEvent;
			}
			/* 双方向リストポインタのつなぎかえ */
			if (NextTrack != null)
			{
				NextTrack.PrevTrack = PrevTrack;
			}
			else if (Parent != null)
			{
				Parent.LastTrack = PrevTrack;
			}

			if (PrevTrack != null)
			{
				PrevTrack.NextTrack = NextTrack;
			}
			else if (Parent != null)
			{
				Parent.FirstTrack = NextTrack;
			}

			if (Parent != null)
			{
				Parent.NumTrack--;
				Parent = null;
			}

			FirstEvent = null;
			LastEvent = null;
			NextTrack = null;
			PrevTrack = null;
			Parent = null;
		}

		/// <summary>
		/// MIDIトラックのクローンを生成
		/// </summary>
		/// <returns></returns>
		public Track CreateClone()
		{
			Track　cloneTrack = new Track();
			foreach (Event srcEvent in this)
			{
				if (srcEvent.PrevCombinedEvent == null)
				{
					Event cloneEvent = srcEvent.CreateClone();
					if (cloneEvent == null)
					{
						cloneTrack.Delete();
						return null;
					}
					cloneTrack.InsertEvent(cloneEvent);
				}
			}
			return cloneTrack;
		}

		/// <summary>
		/// トラックにノートオフイベントを正しく挿入
		/// 結合しているノートオンイベントは既に挿入済みとする。
		/// 同時刻にノートオフイベントがある場合はそれらの直前に挿入する
		/// </summary>
		/// <param name="noteOffEvent"></param>
		internal void InsertNoteOffEventBefore(Event noteOffEvent)
		{
			Event noteOnEvent = noteOffEvent.PrevCombinedEvent;
			Debug.Assert(!noteOnEvent.IsFloating);

			Event oldEvent = noteOnEvent;
			Event tempEvent = noteOnEvent.NextEvent;
			while (tempEvent != null)
			{
				if (tempEvent.Kind == Kinds.EndofTrack && tempEvent.NextEvent == null)
				{
					tempEvent._time = noteOffEvent._time;
					break;
				}
				else if (tempEvent._time >= noteOffEvent._time)
				{
					break;
				}
				oldEvent = tempEvent;
				tempEvent = tempEvent.NextEvent;
			}
			oldEvent.SetNextEvent(noteOffEvent);
		}

		/// <summary>
		/// トラックにノートオフイベントを正しく挿入 
		/// 結合しているノートオンイベントは既に挿入済みとする。
		/// 同時刻にノートオフイベントがある場合はそれらの直後に挿入する
		/// </summary>
		/// <param name="noteOffEvent"></param>
		internal void InsertNoteOffEventAfter(Event noteOffEvent)
		{
			Event noteOnEvent = noteOffEvent.PrevCombinedEvent;
			Debug.Assert(!noteOnEvent.IsFloating);

			Event oldEvent = noteOnEvent;
			Event tempEvent = noteOnEvent.NextEvent;
			while (tempEvent != null)
			{
				if (tempEvent.Kind == Kinds.EndofTrack &&
					tempEvent.NextEvent == null)
				{
					tempEvent._time = noteOffEvent._time;
					break;
				}
				else if (tempEvent._time > noteOffEvent._time ||
					(tempEvent._time == noteOffEvent._time &&
					!tempEvent.IsNoteOff))
				{
					break;
				}
				oldEvent = tempEvent;
				tempEvent = tempEvent.NextEvent;
			}
			oldEvent.SetNextEvent(noteOffEvent);
		}

		/// <summary>
		/// トラックに単一のイベントを挿入
		/// insertEventをtargetEventの直前に入れる。時刻が不正な場合、自動訂正する。
		/// targetEvent==NULLの場合、トラックの最後に入れる。
		/// </summary>
		/// <param name="insertEvent">挿入するイベント</param>
		/// <param name="targetEvent">挿入ターゲット</param>
		internal void InsertSingleEventBefore(Event insertEvent, Event targetEvent)
		{
			/* イベントが既に他のトラックに属している場合、却下する */
			if (insertEvent.Parent != null || insertEvent.PrevEvent != null || insertEvent.NextEvent != null)
			{
				throw new MIDIDataLibException("イベントは既に他のトラックに属しています。");
			}
			/* EOTを二重に入れるのを防止 */
			if (LastEvent != null)
			{
				if (LastEvent.Kind == Kinds.EndofTrack &&
					insertEvent.Kind == Kinds.EndofTrack)
				{
					return;
				}
			}
			/* SMFフォーマット1の場合 */
			if (Parent != null)
			{
				if (Parent.Format == 1)
				{
					/* コンダクタートラックにMIDIEventを入れるのを防止 */
					if (Parent.FirstTrack == this)
					{
						if (insertEvent.IsMIDIEvent)
						{
							throw new MIDIDataLibException("コンダクタートラックにMIDIEventを挿入することはできません。");
						}
					}
					/* 非コンダクタートラックにテンポ・拍子などを入れるのを防止 */
					else
					{
						if (insertEvent.Kind == Kinds.Tempo ||
							insertEvent.Kind == Kinds.SMPTEOffset ||
							insertEvent.Kind == Kinds.TimeSignature ||
							insertEvent.Kind == Kinds.KeySignature)
						{
							throw new MIDIDataLibException("非コンダクタートラックにテンポ・拍子などを挿入することはできません。");
						}
					}
				}
			}
			/* pTargetの直前に挿入する場合 */
			if (targetEvent != null)
			{
				/* ターゲットの所属トラックが異なる場合却下 */
				if (targetEvent.Parent != this)
				{
					throw new MIDIDataLibException("ターゲットの所属トラックが異なります。");
				}
				targetEvent.SetPrevEvent(insertEvent);
			}
			/* トラックの最後に挿入する場合(pTarget==NULL) */
			else if (LastEvent != null)
			{
				/* EOTの後に挿入しようとした場合、EOTを後ろに移動しEOTの直前に挿入 */
				if (LastEvent.Kind == Kinds.EndofTrack)
				{
					/* EOTを正しく移動するため、先に時刻の整合調整 */
					if (LastEvent._time < insertEvent._time)
					{
						LastEvent._time = insertEvent._time;
					}
					LastEvent.SetPrevEvent(insertEvent);
				}
				/* EOT以外の後に挿入しようとした場合、普通に挿入 */
				else
				{
					LastEvent.SetNextEvent(insertEvent);
				}
			}
			/* 空トラックに挿入する場合 */
			else
			{
				insertEvent.Parent = this;
				insertEvent.NextEvent = null;
				insertEvent.PrevEvent = null;
				insertEvent.NextSameKindEvent = null;
				insertEvent.PrevSameKindEvent = null;
				FirstEvent = insertEvent;
				LastEvent = insertEvent;
				NumEvent++;
			}
		}

		/// <summary>
		/// トラックにイベントを挿入(結合イベントにも対応)
		/// insertEventをtargetEventの直前に入れる。時刻が不正な場合、自動訂正する。
		/// targetEvent==NULLの場合、トラックの最後に入れる。
		/// </summary>
		/// <param name="insertEvent">挿入するイベント</param>
		/// <param name="targetEvent">挿入ターゲット</param>
		public void InsertEventBefore(Event insertEvent, Event targetEvent)
		{
			/* 非浮遊イベントは挿入できない。 */
			if (!insertEvent.IsFloating)
			{
				throw new MIDIDataLibException("挿入するイベントは、浮遊イベントである必要があります。");
			}
			insertEvent = insertEvent.FirstCombinedEvent;
			/* ノートイベント以外の結合イベントの間には挿入できない */
			if (targetEvent != null)
			{
				if (!targetEvent.IsNote)
				{
					targetEvent = targetEvent.FirstCombinedEvent;
				}
			}
			/* 単独のイベントの場合 */
			if (!insertEvent.IsCombined)
			{
				InsertSingleEventBefore(insertEvent, targetEvent);
			}
			/* ノートイベントの場合 */
			else if (insertEvent.IsNote)
			{
				InsertSingleEventBefore(insertEvent, targetEvent);
				InsertNoteOffEventBefore(insertEvent.NextCombinedEvent);
			}
			else
			{
				/*何もしない*/
			}
		}

		/// <summary>
		/// トラックにイベントを挿入(イベントはあらかじめ生成しておく) 
		/// pEventをpTargetの直後に入れる。時刻が不正な場合、自動訂正する。
		/// pTarget==NULLの場合、トラックの最初に入れる。
		/// </summary>
		/// <param name="insertEvent">挿入するイベント</param>
		/// <param name="targetEvent">挿入ターゲット</param>
		public void InsertSingleEventAfter(Event insertEvent, Event targetEvent)
		{
			/* イベントが既に他のトラックに属している場合、却下する */
			if (insertEvent.Parent != null || insertEvent.PrevEvent != null || insertEvent.NextEvent != null)
			{
				throw new MIDIDataLibException("イベントは既に他のトラックに属しています。");
			}
			/* EOTを二重に入れるのを防止 */
			if (LastEvent != null)
			{
				if (LastEvent.Kind == Kinds.EndofTrack &&
					insertEvent.Kind == Kinds.EndofTrack)
				{
					return;
				}
			}
			/* SMFフォーマット1の場合 */
			if (Parent != null)
			{
				if (Parent.Format == 1)
				{
					/* コンダクタートラックにMIDIEventを入れるのを防止 */
					if (Parent.FirstTrack == this)
					{
						if (insertEvent.IsMIDIEvent)
						{
							throw new MIDIDataLibException("コンダクタートラックにMIDIEventを挿入することはできません。");
						}
					}
					/* 非コンダクタートラックにテンポ・拍子などを入れるのを防止 */
					else
					{
						if (insertEvent.Kind == Kinds.Tempo ||
							insertEvent.Kind == Kinds.SMPTEOffset ||
							insertEvent.Kind == Kinds.TimeSignature ||
							insertEvent.Kind == Kinds.KeySignature)
						{
							throw new MIDIDataLibException("非コンダクタートラックにテンポ・拍子などを挿入することはできません。");
						}
					}
				}
			}

			/* pTargetの直後に挿入する場合 */
			if (targetEvent != null)
			{
				/* ターゲットが所属トラックが異なる場合却下 */
				if (targetEvent.Parent != this)
				{
					throw new MIDIDataLibException("ターゲットの所属トラックが異なります。");
				}
				/* EOTの直後に挿入しようとした場合、EOTを移動しEOTの直前に挿入 */
				if (targetEvent.Kind == Kinds.EndofTrack &&
					targetEvent.NextEvent == null)
				{
					/* EOTを正しく移動するため、先に時刻の整合調整 */
					if (targetEvent._time < insertEvent._time)
					{
						targetEvent._time = insertEvent._time;
					}
					targetEvent.SetPrevEvent(insertEvent);
				}
				/* EOT以外の直後に挿入しようとした場合、時刻の整合さえすれば可能(pTarget==NULL) */
				else
				{
					if (LastEvent.Kind == Kinds.EndofTrack)
					{
						if (LastEvent._time < insertEvent._time)
						{
							LastEvent._time = insertEvent._time;
						}
					}
					targetEvent.SetNextEvent(insertEvent);
				}
			}
			/* トラックの最初に挿入する場合(pTarget==NULL) */
			else if (FirstEvent != null)
			{
				/* EOTの直前となる場合は、EOTの時刻を調整する */
				if (FirstEvent.Kind == Kinds.EndofTrack &&
					FirstEvent.NextEvent == null)
				{
					if (FirstEvent._time < insertEvent._time)
					{
						FirstEvent._time = insertEvent._time;
					}
				}
				FirstEvent.SetPrevEvent(insertEvent);
			}
			/* 空トラックに挿入する場合 */
			else
			{
				insertEvent.Parent = this;
				insertEvent.NextEvent = null;
				insertEvent.PrevEvent = null;
				insertEvent.NextSameKindEvent = null;
				insertEvent.PrevSameKindEvent = null;
				FirstEvent = insertEvent;
				LastEvent = insertEvent;
				NumEvent++;
			}
		}



		/// <summary>
		/// トラックにトラック名イベントを生成して挿入
		/// </summary>
		/// <param name="time">時刻</param>
		/// <param name="text">トラック名</param>
		public void InsertTrackName(int time, string text)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// トラックにイベントを挿入します。
		/// </summary>
		/// <param name="event"></param>
		public void InsertEvent(Event @event)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region イテレータ

		/// <summary>
		/// イテレータ
		/// </summary>
		/// <returns>イベント</returns>
		public IEnumerator<Event> GetEnumerator()
		{
			for (Event @event = FirstEvent; @event != null; @event = @event.NextEvent)
			{
				yield return @event;
			}
		}

		#endregion
	}
}
