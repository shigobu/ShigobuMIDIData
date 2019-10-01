using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shigobu.MIDI.DataLib
{
	class Event
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
		/// <summary>
		/// 親(MIDITrackオブジェクト)
		/// </summary>
		public Event Parent { get; set; }
	}
}
