using System;
using System.Collections.Generic;
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
